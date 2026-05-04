using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/reports")]
    [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly DsmsDbContext _context;
        public ReportsController(DsmsDbContext context) => _context = context;

        // GET /api/reports/income?from=2026-01-01&to=2026-01-31
        [HttpGet("income")]
        public async Task<IActionResult> GetIncomeSummary(
            [FromQuery] string from,
            [FromQuery] string to)
        {
            if (!DateTime.TryParse(from, out var fromDate) ||
                !DateTime.TryParse(to,   out var toDate))
                return BadRequest(new { message = "Invalid date range. Use yyyy-MM-dd format." });

            toDate = toDate.Date.AddDays(1).AddSeconds(-1);
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            // Bills in range
            var billsQuery = _context.Bills
                .Include(b => b.Student).ThenInclude(s => s.Branch)
                .Where(b => (b.Active == null || b.Active == true)
                         && b.BillDate >= fromDate
                         && b.BillDate <= toDate);

            if (callerBranchId.HasValue)
                billsQuery = billsQuery.Where(b => b.Student.BranchId == callerBranchId);

            var bills = await billsQuery.ToListAsync();

            // Payments in range (for method breakdown)
            var paymentsQuery = _context.Payments
                .Include(p => p.Student).ThenInclude(s => s.Branch)
                .Where(p => p.PaymentDate >= fromDate && p.PaymentDate <= toDate);

            if (callerBranchId.HasValue)
                paymentsQuery = paymentsQuery.Where(p => p.Student.BranchId == callerBranchId);

            var payments = await paymentsQuery.ToListAsync();

            // Daily bill summary
            var daily = bills
                .GroupBy(b => b.BillDate.Date)
                .Select(g => new {
                    date        = g.Key.ToString("yyyy-MM-dd"),
                    totalBilled = g.Sum(b => b.NetAmount),
                    totalPaid   = g.Sum(b => b.PaidAmount),
                    billCount   = g.Count()
                })
                .OrderBy(d => d.date)
                .ToList();

            // Payment method breakdown
            var byMethod = payments
                .GroupBy(p => p.PaymentMethod)
                .Select(g => new {
                    method = g.Key,
                    total  = g.Sum(p => p.Amount),
                    count  = g.Count()
                })
                .OrderByDescending(m => m.total)
                .ToList();

            // Branch breakdown
            var byBranch = bills
                .GroupBy(b => b.Student?.Branch?.Name ?? "Unknown")
                .Select(g => new {
                    branch  = g.Key,
                    billed  = g.Sum(b => b.NetAmount),
                    paid    = g.Sum(b => b.PaidAmount),
                    pending = g.Sum(b => b.BalanceAmount)
                })
                .OrderByDescending(b => b.paid)
                .ToList();

            // Totals
            var totalBilled  = bills.Sum(b => b.NetAmount);
            var totalPaid    = bills.Sum(b => b.PaidAmount);
            var totalPending = bills.Sum(b => b.BalanceAmount);

            return Ok(new {
                fromDate     = fromDate.ToString("yyyy-MM-dd"),
                toDate       = toDate.Date.ToString("yyyy-MM-dd"),
                totalBilled,
                totalPaid,
                totalPending,
                totalBills   = bills.Count,
                paidCount    = bills.Count(b => b.Status == "Paid"),
                partialCount = bills.Count(b => b.Status == "Partial"),
                pendingCount = bills.Count(b => b.Status == "Pending"),
                daily,
                byMethod,
                byBranch
            });
        }

        // GET /api/reports/students?from=&to=
        [HttpGet("students")]
        public async Task<IActionResult> GetStudentsSummary(
            [FromQuery] string from,
            [FromQuery] string to)
        {
            if (!DateTime.TryParse(from, out var fromDate) ||
                !DateTime.TryParse(to,   out var toDate))
                return BadRequest(new { message = "Invalid date range." });

            toDate = toDate.Date.AddDays(1).AddSeconds(-1);
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.Students
                .Include(s => s.Branch)
                .Where(s => s.Active == true
                         && s.RegistrationDate >= fromDate
                         && s.RegistrationDate <= toDate);

            if (callerBranchId.HasValue)
                query = query.Where(s => s.BranchId == callerBranchId);

            var students = await query.ToListAsync();

            var byBranch = students
                .GroupBy(s => s.Branch?.Name ?? "Unknown")
                .Select(g => new { branch = g.Key, count = g.Count() })
                .OrderByDescending(b => b.count)
                .ToList();

            var byGender = students
                .GroupBy(s => s.Gender ?? "Unknown")
                .Select(g => new { gender = g.Key, count = g.Count() })
                .ToList();

            var daily = students
                .Where(s => s.RegistrationDate.HasValue)
                .GroupBy(s => s.RegistrationDate!.Value.Date)
                .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), count = g.Count() })
                .OrderBy(d => d.date)
                .ToList();

            return Ok(new { total = students.Count, byBranch, byGender, daily });
        }

        // GET /api/reports/training-completion?from=&to=
        [HttpGet("training-completion")]
        public async Task<IActionResult> GetTrainingCompletion(
            [FromQuery] string? from,
            [FromQuery] string? to)
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var studentsQuery = _context.Students
                .Include(s => s.Branch)
                .Include(s => s.CoursePackage)
                .Where(s => s.Active == true);

            if (callerBranchId.HasValue)
                studentsQuery = studentsQuery.Where(s => s.BranchId == callerBranchId);

            // Optional date range filters on registration date
            if (!string.IsNullOrEmpty(from) && DateTime.TryParse(from, out var fromDate))
                studentsQuery = studentsQuery.Where(s => s.RegistrationDate >= fromDate);

            if (!string.IsNullOrEmpty(to) && DateTime.TryParse(to, out var toDate))
            {
                toDate = toDate.Date.AddDays(1).AddSeconds(-1);
                studentsQuery = studentsQuery.Where(s => s.RegistrationDate <= toDate);
            }

            var students   = await studentsQuery.ToListAsync();
            var studentIds = students.Select(s => s.Id).ToList();

            // Count completed training sessions per student
            var sessionCounts = await _context.TrainingSessions
                .Where(ts => studentIds.Contains(ts.StudentId) && ts.Status == "Completed")
                .GroupBy(ts => ts.StudentId)
                .Select(g => new { StudentId = g.Key, Count = g.Count() })
                .ToListAsync();

            var sessionMap = sessionCounts.ToDictionary(x => x.StudentId, x => x.Count);

            // Build per-student summary rows
            var rows = students.Select(s =>
            {
                var required  = s.CoursePackage?.TrainingDays ?? 15;
                var completed = sessionMap.GetValueOrDefault(s.Id, 0);
                var pct       = required > 0
                    ? Math.Min(100, Math.Round((double)completed / required * 100, 1))
                    : 0;
                var status    = completed >= required ? "Completed"
                              : completed  > 0        ? "In Progress"
                              :                         "Not Started";

                return new
                {
                    studentId        = s.Id,
                    studentName      = s.StudentName,
                    branch           = s.Branch?.Name ?? "—",
                    packageName      = s.CoursePackage?.PackageName ?? "—",
                    requiredDays     = required,
                    completedDays    = completed,
                    progressPct      = pct,
                    status,
                    registrationDate = s.RegistrationDate?.ToString("yyyy-MM-dd") ?? "—"
                };
            })
            .OrderByDescending(r => r.completedDays)
            .ToList();

            var total          = rows.Count;
            var completedCount = rows.Count(r => r.status == "Completed");
            var inProgCount    = rows.Count(r => r.status == "In Progress");
            var notStartCount  = rows.Count(r => r.status == "Not Started");
            var completionRate = total > 0
                ? Math.Round((double)completedCount / total * 100, 1)
                : 0.0;

            var byBranch = rows
                .GroupBy(r => r.branch)
                .Select(g => new
                {
                    branch      = g.Key,
                    total       = g.Count(),
                    completed   = g.Count(r => r.status == "Completed"),
                    inProgress  = g.Count(r => r.status == "In Progress"),
                    notStarted  = g.Count(r => r.status == "Not Started")
                })
                .OrderByDescending(b => b.completed)
                .ToList();

            return Ok(new
            {
                total,
                completedCount,
                inProgCount,
                notStartCount,
                completionRate,
                byBranch,
                students = rows
            });
        }

        // GET /api/reports/exam-summary
        [HttpGet("exam-summary")]
        public async Task<IActionResult> GetExamSummary()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.StudentPackageRegistrations
                .Include(s => s.Student).ThenInclude(st => st.Branch)
                .Where(s => s.Active == true && s.IsRecommendForTrial == true);

            if (callerBranchId.HasValue)
                query = query.Where(s => s.Student.BranchId == callerBranchId);

            var sprs    = await query.ToListAsync();
            var passed  = sprs.Count(s => s.ExamStatus == "Pass");
            var failed  = sprs.Count(s => s.ExamStatus == "Fail");
            var pending = sprs.Count(s => string.IsNullOrEmpty(s.ExamStatus));
            var total   = sprs.Count;

            return Ok(new {
                total, passed, failed, pending,
                passRate    = total > 0 ? Math.Round((double)passed / total * 100, 1) : 0,
                avgAttempts = total > 0 ? Math.Round(sprs.Average(s => (double)s.ExamAttempts), 1) : 0
            });
        }
    }
}
