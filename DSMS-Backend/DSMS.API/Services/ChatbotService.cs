using System.Text;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;

namespace DSMS.API.Services;

public class ChatbotService : IChatbotService
{
    private readonly DsmsDbContext _db;
    private readonly ILogger<ChatbotService> _logger;

    public ChatbotService(DsmsDbContext db, ILogger<ChatbotService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Intent keyword map ──────────────────────────────────────────────────────
    private static readonly Dictionary<string, string[]> Intents = new()
    {
        ["greeting"]          = new[] { "hi", "hello", "hey", "good morning", "good evening", "good afternoon", "howdy", "greetings" },
        ["help"]              = new[] { "help", "what can you do", "commands", "options", "assist", "capabilities", "what do you know" },
        ["student_balance"]   = new[] { "balance", "owe", "outstanding", "due", "fee", "how much", "amount" },
        ["student_profile"]   = new[] { "profile", "find", "search", "details", "info", "who is", "show student", "tell me about", "contact" },
        ["pending_payments"]  = new[] { "pending", "unpaid", "overdue", "not paid", "defaulter", "haven't paid", "who owes" },
        ["training_progress"] = new[] { "training", "session", "lesson", "progress", "completed", "how many sessions", "training day" },
        ["today_sessions"]    = new[] { "today", "today's", "schedule today", "sessions today", "this morning" },
        ["student_count"]     = new[] { "how many students", "total students", "number of students", "student count", "how many registered" },
        ["income_summary"]    = new[] { "income", "revenue", "collected", "earnings", "total money", "financial", "how much earned" },
        ["exam_results"]      = new[] { "exam", "passed", "failed", "practical test", "test result", "exam result", "written exam" },
        ["instructor_list"]   = new[] { "instructor", "teacher", "who teaches", "list instructor", "our instructors" },
    };

    // ── Role → allowed intents ──────────────────────────────────────────────────
    private static readonly Dictionary<string, HashSet<string>> RolePermissions = new()
    {
        ["Company Admin"] = new() { "greeting","help","student_balance","student_profile","pending_payments","training_progress","today_sessions","student_count","income_summary","exam_results","instructor_list" },
        ["Admin"]         = new() { "greeting","help","student_balance","student_profile","pending_payments","training_progress","today_sessions","student_count","income_summary","exam_results","instructor_list" },
        ["Branch Admin"]  = new() { "greeting","help","student_balance","student_profile","pending_payments","training_progress","today_sessions","student_count","income_summary","exam_results","instructor_list" },
        ["Staff"]         = new() { "greeting","help","student_balance","student_profile","pending_payments","training_progress","today_sessions","student_count" },
        ["OfficeStaff"]   = new() { "greeting","help","student_balance","student_profile","pending_payments","training_progress","today_sessions","student_count" },
        ["Instructor"]    = new() { "greeting","help","training_progress","today_sessions" },
    };

    // ═══════════════════════════════════════════════════════════════════════════
    public async Task<string> ProcessMessageAsync(string message, string role, int? branchId, string userName)
    {
        var msg    = message.Trim();
        var msgLow = msg.ToLower();

        var intent = MatchIntent(msgLow);

        // Role permission check
        var allowed = RolePermissions.TryGetValue(role, out var perms) ? perms : new HashSet<string>();
        if (!allowed.Contains(intent) && intent != "greeting" && intent != "help" && intent != "unknown")
            return $"⛔ You don't have permission to access that information.\n" +
                   $"Your role ({role}) is not allowed to view this data. Please contact your administrator.";

        _logger.LogInformation("Chatbot — user:{User} role:{Role} intent:{Intent}", userName, role, intent);

        return intent switch
        {
            "greeting"          => BuildGreeting(userName),
            "help"              => BuildHelp(role),
            "student_balance"   => await HandleStudentBalance(msg, msgLow, branchId),
            "student_profile"   => await HandleStudentProfile(msg, msgLow, branchId),
            "pending_payments"  => await HandlePendingPayments(branchId),
            "training_progress" => await HandleTrainingProgress(msg, msgLow, branchId),
            "today_sessions"    => await HandleTodaySessions(branchId),
            "student_count"     => await HandleStudentCount(branchId),
            "income_summary"    => await HandleIncomeSummary(branchId),
            "exam_results"      => await HandleExamResults(branchId),
            "instructor_list"   => await HandleInstructorList(branchId),
            _                   => BuildUnknown()
        };
    }

    // ── Intent matching ─────────────────────────────────────────────────────────
    private static string MatchIntent(string msgLow)
    {
        var scores = new Dictionary<string, int>();
        foreach (var (intent, keywords) in Intents)
            scores[intent] = keywords.Count(k => msgLow.Contains(k));

        var best = scores.MaxBy(s => s.Value);
        return best.Value > 0 ? best.Key : "unknown";
    }

    // ── Entity extraction ───────────────────────────────────────────────────────
    private static string? ExtractName(string originalMsg)
    {
        // Try patterns: "of John Silva", "for John", "John's", "show John Silva"
        var patterns = new[]
        {
            @"(?:of|for|about|check|find|show|balance|training|progress|profile|details|info|tell me about|who is)\s+([A-Za-z][a-zA-Z]*(?:\s+[A-Za-z][a-zA-Z]*){0,3})",
            @"([A-Za-z][a-zA-Z]*(?:\s+[A-Za-z][a-zA-Z]*){0,3})'s",
        };

        var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "student","the","a","an","my","our","all","every","today","pending","training","session","exam","balance","payment" };

        foreach (var pattern in patterns)
        {
            var match = Regex.Match(originalMsg, pattern, RegexOptions.IgnoreCase);
            if (!match.Success) continue;

            var candidate = match.Groups[1].Value.Trim();
            var words     = candidate.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Filter stop words from the start
            while (words.Length > 0 && stopWords.Contains(words[0]))
                words = words.Skip(1).ToArray();

            if (words.Length > 0)
                return string.Join(" ", words);
        }
        return null;
    }

    private static string? ExtractNic(string msgLow)
    {
        var m = Regex.Match(msgLow, @"\b([0-9]{9}[vVxX]|[0-9]{12})\b");
        return m.Success ? m.Value.ToUpper() : null;
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  INTENT HANDLERS
    // ═══════════════════════════════════════════════════════════════════════════

    // ── Greeting ────────────────────────────────────────────────────────────────
    private static string BuildGreeting(string name)
        => $"👋 Hello, {name}! I'm the DSMS Assistant.\n" +
           "I can help you look up student information, payments, training progress, and more.\n" +
           "Type **help** to see what I can do for you.";

    // ── Help ────────────────────────────────────────────────────────────────────
    private static string BuildHelp(string role)
    {
        var sb = new StringBuilder();
        sb.AppendLine("📋 **Here's what you can ask me:**\n");
        sb.AppendLine("💰 \"balance of [student name]\" — Check a student's payment balance");
        sb.AppendLine("🔍 \"find student [name or NIC]\" — View student profile details");
        sb.AppendLine("⚠️  \"show pending payments\" — List students with outstanding balances");
        sb.AppendLine("📅 \"today's sessions\" — See today's training schedule");
        sb.AppendLine("🏋️  \"training progress of [name]\" — Check completed sessions vs required");
        sb.AppendLine("👥 \"how many students\" — Total student count");

        if (role is "Branch Admin" or "Company Admin" or "Admin")
        {
            sb.AppendLine("💵 \"this month's income\" — Financial summary");
            sb.AppendLine("📝 \"exam results summary\" — Pass/fail statistics");
            sb.AppendLine("👨‍🏫 \"list instructors\" — View instructor list");
        }

        sb.AppendLine("\n💡 Tip: Use a student's full name or NIC for the best results.");
        return sb.ToString().TrimEnd();
    }

    private static string BuildUnknown()
        => "🤔 I'm not sure I understand that.\n" +
           "Try asking something like:\n" +
           "• \"balance of Kamal Silva\"\n" +
           "• \"today's sessions\"\n" +
           "• \"pending payments\"\n\n" +
           "Type **help** for the full list of things I can do.";

    // ── Student Balance ──────────────────────────────────────────────────────────
    private async Task<string> HandleStudentBalance(string msg, string msgLow, int? branchId)
    {
        var nic  = ExtractNic(msgLow);
        var name = ExtractName(msg);

        if (nic == null && string.IsNullOrWhiteSpace(name))
            return "💬 Please include the student's **name or NIC**.\nExample: *\"balance of Kamal Silva\"*";

        var query = _db.Students
            .Include(s => s.Bills)
            .Where(s => s.Active == true);

        if (branchId.HasValue) query = query.Where(s => s.BranchId == branchId);

        var student = nic != null
            ? await query.FirstOrDefaultAsync(s => s.Nic == nic)
            : await query.FirstOrDefaultAsync(s =>
                EF.Functions.Like(s.StudentName, $"%{name}%"));

        if (student == null)
            return $"❌ No student found matching **\"{name ?? nic}\"**.\nCheck the spelling or try their NIC number.";

        var totalBilled  = student.Bills.Where(b => b.Active != false).Sum(b => b.NetAmount);
        var totalPaid    = student.Bills.Where(b => b.Active != false).Sum(b => b.PaidAmount);
        var totalBalance = student.Bills.Where(b => b.Active != false).Sum(b => b.BalanceAmount);
        var pendingBills = student.Bills.Count(b => b.BalanceAmount > 0 && b.Active != false);

        if (!student.Bills.Any())
            return $"📋 **{student.StudentName}** has no billing records yet.";

        var sb = new StringBuilder();
        sb.AppendLine($"💳 **{student.StudentName}**");
        sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"Total Billed   : Rs. {totalBilled:N0}");
        sb.AppendLine($"Total Paid     : Rs. {totalPaid:N0}");
        sb.AppendLine($"Balance Due    : Rs. {totalBalance:N0}");
        sb.AppendLine($"Pending Bills  : {pendingBills}");
        sb.Append(totalBalance == 0
            ? "\n✅ All payments are cleared."
            : $"\n⚠️ Rs. {totalBalance:N0} is still outstanding.");
        return sb.ToString();
    }

    // ── Student Profile ──────────────────────────────────────────────────────────
    private async Task<string> HandleStudentProfile(string msg, string msgLow, int? branchId)
    {
        var nic  = ExtractNic(msgLow);
        var name = ExtractName(msg);

        if (nic == null && string.IsNullOrWhiteSpace(name))
            return "💬 Please include the student's **name or NIC**.\nExample: *\"find student Kamal Silva\"*";

        var query = _db.Students
            .Include(s => s.Branch)
            .Include(s => s.CoursePackage)
            .Where(s => s.Active == true);

        if (branchId.HasValue) query = query.Where(s => s.BranchId == branchId);

        var student = nic != null
            ? await query.FirstOrDefaultAsync(s => s.Nic == nic)
            : await query.FirstOrDefaultAsync(s =>
                EF.Functions.Like(s.StudentName, $"%{name}%"));

        if (student == null)
            return $"❌ No student found matching **\"{name ?? nic}\"**.";

        var sb = new StringBuilder();
        sb.AppendLine($"👤 **{student.StudentName}**");
        sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"NIC            : {student.Nic}");
        sb.AppendLine($"Phone          : {student.PhoneNumber}");
        sb.AppendLine($"Branch         : {student.Branch?.Name ?? "—"}");
        sb.AppendLine($"Package        : {student.CoursePackage?.PackageName ?? "—"}");
        sb.AppendLine($"Registered     : {student.RegistrationDate?.ToString("dd MMM yyyy") ?? "—"}");
        sb.Append($"Status         : {(student.Active == true ? "✅ Active" : "🔴 Inactive")}");
        return sb.ToString();
    }

    // ── Pending Payments ─────────────────────────────────────────────────────────
    private async Task<string> HandlePendingPayments(int? branchId)
    {
        var query = _db.Bills
            .Include(b => b.Student)
            .Where(b => b.BalanceAmount > 0 && b.Active != false);

        if (branchId.HasValue) query = query.Where(b => b.Student.BranchId == branchId);

        var pending = await query
            .OrderByDescending(b => b.BalanceAmount)
            .Take(10)
            .ToListAsync();

        var totalCount  = await query.CountAsync();
        var totalAmount = await query.SumAsync(b => b.BalanceAmount);

        if (!pending.Any())
            return "✅ Great news! There are no pending payments at this time.";

        var sb = new StringBuilder();
        sb.AppendLine($"⚠️ **{totalCount} pending payment{(totalCount != 1 ? "s" : "")}**");
        sb.AppendLine($"Total outstanding: **Rs. {totalAmount:N0}**");
        sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━");

        foreach (var bill in pending.Take(8))
            sb.AppendLine($"• {bill.Student.StudentName} — Rs. {bill.BalanceAmount:N0}");

        if (totalCount > 8)
            sb.Append($"  ...and {totalCount - 8} more.");

        return sb.ToString().TrimEnd();
    }

    // ── Training Progress ────────────────────────────────────────────────────────
    private async Task<string> HandleTrainingProgress(string msg, string msgLow, int? branchId)
    {
        var nic  = ExtractNic(msgLow);
        var name = ExtractName(msg);

        if (nic == null && string.IsNullOrWhiteSpace(name))
            return "💬 Please include the student's **name**.\nExample: *\"training progress of Kamal Silva\"*";

        var query = _db.Students
            .Include(s => s.CoursePackage)
            .Where(s => s.Active == true);

        if (branchId.HasValue) query = query.Where(s => s.BranchId == branchId);

        var student = nic != null
            ? await query.FirstOrDefaultAsync(s => s.Nic == nic)
            : await query.FirstOrDefaultAsync(s =>
                EF.Functions.Like(s.StudentName, $"%{name}%"));

        if (student == null)
            return $"❌ No student found matching **\"{name ?? nic}\"**.";

        var completedSessions = await _db.TrainingSessions
            .CountAsync(ts => ts.StudentId == student.Id && ts.Status == "Completed");

        var requiredDays = student.CoursePackage?.TrainingDays ?? 15;
        var pct          = requiredDays > 0
            ? Math.Min(100, Math.Round((double)completedSessions / requiredDays * 100))
            : 0;

        var progressBar = BuildProgressBar((int)pct);
        var status      = completedSessions >= requiredDays ? "✅ Completed"
                        : completedSessions  > 0           ? "🔄 In Progress"
                        :                                    "⏸️ Not Started";

        var sb = new StringBuilder();
        sb.AppendLine($"🏋️ **{student.StudentName}** — Training Progress");
        sb.AppendLine($"━━━━━━━━━━━━━━━━━━━━━");
        sb.AppendLine($"Sessions Done  : {completedSessions} / {requiredDays}");
        sb.AppendLine($"Progress       : {progressBar} {pct}%");
        sb.AppendLine($"Package        : {student.CoursePackage?.PackageName ?? "—"}");
        sb.Append($"Status         : {status}");
        return sb.ToString();
    }

    // ── Today's Sessions ─────────────────────────────────────────────────────────
    private async Task<string> HandleTodaySessions(int? branchId)
    {
        var today    = DateTime.Today;
        var tomorrow = today.AddDays(1);

        var query = _db.TrainingSessions
            .Include(ts => ts.Student)
            .Include(ts => ts.Instructor)
            .Where(ts => ts.ScheduledDate >= today
                      && ts.ScheduledDate < tomorrow
                      && ts.Status != "Cancelled");

        if (branchId.HasValue) query = query.Where(ts => ts.BranchId == branchId);

        var sessions = await query.OrderBy(ts => ts.ScheduledTime).ToListAsync();

        if (!sessions.Any())
            return $"📅 No training sessions scheduled for today ({today:dd MMM yyyy}).";

        var sb = new StringBuilder();
        sb.AppendLine($"📅 **{sessions.Count} session{(sessions.Count != 1 ? "s" : "")} today** ({today:dd MMM yyyy})");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━");

        foreach (var s in sessions)
        {
            var statusIcon = s.Status switch { "Completed" => "✅", "NoShow" => "❌", _ => "⏰" };
            sb.AppendLine($"{statusIcon} {s.ScheduledTime} — {s.Student.StudentName} ({s.VehicleClass}) | {s.Instructor.InstructorName ?? "—"}");
        }

        return sb.ToString().TrimEnd();
    }

    // ── Student Count ────────────────────────────────────────────────────────────
    private async Task<string> HandleStudentCount(int? branchId)
    {
        var query = _db.Students.Where(s => s.Active == true);
        if (branchId.HasValue) query = query.Where(s => s.BranchId == branchId);

        var total    = await query.CountAsync();
        var thisMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var newMonth  = await query.CountAsync(s => s.RegistrationDate >= thisMonth);

        return $"👥 **Student Count**\n" +
               $"━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Total Active   : {total} students\n" +
               $"Joined This Month : {newMonth} students";
    }

    // ── Income Summary ───────────────────────────────────────────────────────────
    private async Task<string> HandleIncomeSummary(int? branchId)
    {
        var firstOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        var today        = DateTime.Today;
        var tomorrow     = today.AddDays(1);

        var billQ = _db.Bills
            .Include(b => b.Student)
            .Where(b => b.Active != false);

        if (branchId.HasValue) billQ = billQ.Where(b => b.Student.BranchId == branchId);

        var monthBills    = await billQ.Where(b => b.BillDate >= firstOfMonth).ToListAsync();
        var todayPayments = await _db.Payments
            .Where(p => p.PaymentDate >= today && p.PaymentDate < tomorrow)
            .SumAsync(p => (decimal?)p.Amount) ?? 0;

        var monthBilled   = monthBills.Sum(b => b.NetAmount);
        var monthCollected = monthBills.Sum(b => b.PaidAmount);
        var monthPending  = monthBills.Sum(b => b.BalanceAmount);

        return $"💵 **Income Summary — {DateTime.Now:MMMM yyyy}**\n" +
               $"━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Total Billed   : Rs. {monthBilled:N0}\n" +
               $"Total Collected: Rs. {monthCollected:N0}\n" +
               $"Still Pending  : Rs. {monthPending:N0}\n" +
               $"━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Today's Collections: Rs. {todayPayments:N0}";
    }

    // ── Exam Results ─────────────────────────────────────────────────────────────
    private async Task<string> HandleExamResults(int? branchId)
    {
        var query = _db.StudentPackageRegistrations
            .Include(s => s.Student)
            .Where(s => s.Active == true && s.IsRecommendForTrial == true);

        if (branchId.HasValue) query = query.Where(s => s.Student.BranchId == branchId);

        var records = await query.ToListAsync();
        var total   = records.Count;
        var passed  = records.Count(r => r.ExamStatus == "Pass");
        var failed  = records.Count(r => r.ExamStatus == "Fail");
        var pending = records.Count(r => string.IsNullOrEmpty(r.ExamStatus));
        var rate    = total > 0 ? Math.Round((double)passed / total * 100, 1) : 0;

        return $"📝 **Exam Results Summary**\n" +
               $"━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Approved for Practical : {total}\n" +
               $"✅ Passed              : {passed}\n" +
               $"❌ Failed              : {failed}\n" +
               $"⏳ Awaiting Result     : {pending}\n" +
               $"━━━━━━━━━━━━━━━━━━━━━\n" +
               $"Pass Rate              : {rate}%";
    }

    // ── Instructor List ───────────────────────────────────────────────────────────
    private async Task<string> HandleInstructorList(int? branchId)
    {
        var query = _db.Instructors.Where(i => i.Active == true);
        if (branchId.HasValue) query = query.Where(i => i.BranchId == branchId);

        var instructors = await query.OrderBy(i => i.InstructorName).ToListAsync();

        if (!instructors.Any())
            return "ℹ️ No active instructors found.";

        var sb = new StringBuilder();
        sb.AppendLine($"👨‍🏫 **Instructors ({instructors.Count})**");
        sb.AppendLine("━━━━━━━━━━━━━━━━━━━━━");

        foreach (var inst in instructors)
            sb.AppendLine($"• {inst.InstructorName} | {inst.Phone ?? "—"}");

        return sb.ToString().TrimEnd();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────────
    private static string BuildProgressBar(int pct)
    {
        var filled = (int)Math.Round(pct / 10.0);
        return new string('█', filled) + new string('░', 10 - filled);
    }
}
