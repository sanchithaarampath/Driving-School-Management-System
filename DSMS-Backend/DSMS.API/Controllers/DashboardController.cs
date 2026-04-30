using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public DashboardController(DsmsDbContext context) => _context = context;

        // ── Stats ─────────────────────────────────────────────────────────────
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats([FromQuery] int? branchId = null)
        {
            // Company admins (no branch claim) may pass an optional branchId filter.
            // Branch admins / staff are always scoped to their own branch.
            var callerBranchId = ClaimsHelper.GetBranchId(User);
            var effectiveBranchId = callerBranchId ?? (branchId > 0 ? branchId : null);

            var studentQ = _context.Students.Where(s => s.Active == true);
            var billQ    = _context.Bills.Where(b => b.Active == true);
            var payQ     = _context.Payments.Where(p => p.Student != null);

            if (effectiveBranchId.HasValue)
            {
                studentQ = studentQ.Where(s => s.BranchId == effectiveBranchId);
                billQ    = billQ.Where(b => b.Student.BranchId == effectiveBranchId);
                payQ     = payQ.Where(p => p.Student.BranchId == effectiveBranchId);
            }

            var totalStudents = await studentQ.CountAsync();
            var totalBills    = await billQ.CountAsync();
            var totalIncome   = await billQ.SumAsync(b => (decimal?)b.PaidAmount) ?? 0;
            var pendingAmount = await billQ.SumAsync(b => (decimal?)b.BalanceAmount) ?? 0;
            var pendingBills  = await billQ.CountAsync(b => b.BalanceAmount > 0);

            var firstOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var newThisMonth = await studentQ.CountAsync(s => s.RegistrationDate >= firstOfMonth);

            // Today's collections
            var today            = DateTime.Today;
            var todayCollections = await payQ
                .Where(p => p.PaymentDate >= today && p.PaymentDate < today.AddDays(1))
                .SumAsync(p => (decimal?)p.Amount) ?? 0;

            return Ok(new { totalStudents, totalBills, totalIncome, pendingAmount, pendingBills, newThisMonth, todayCollections });
        }

        // ── Monthly Revenue (last 6 months) ───────────────────────────────────
        [HttpGet("monthly-revenue")]
        public async Task<IActionResult> GetMonthlyRevenue([FromQuery] int? branchId = null)
        {
            var callerBranchId    = ClaimsHelper.GetBranchId(User);
            var effectiveBranchId = callerBranchId ?? (branchId > 0 ? branchId : null);
            var now               = DateTime.Now;
            var result            = new List<object>();

            for (int i = 5; i >= 0; i--)
            {
                var month     = new DateTime(now.Year, now.Month, 1).AddMonths(-i);
                var nextMonth = month.AddMonths(1);

                var payQ  = _context.Payments.Where(p =>
                    p.PaymentDate >= month && p.PaymentDate < nextMonth && p.Student != null);
                var studQ = _context.Students.Where(s =>
                    s.Active == true && s.RegistrationDate >= month && s.RegistrationDate < nextMonth);

                if (effectiveBranchId.HasValue)
                {
                    payQ  = payQ.Where(p => p.Student.BranchId == effectiveBranchId);
                    studQ = studQ.Where(s => s.BranchId == effectiveBranchId);
                }

                var revenue  = await payQ.SumAsync(p => (decimal?)p.Amount) ?? 0;
                var students = await studQ.CountAsync();

                result.Add(new { month = month.ToString("MMM"), year = month.Year, revenue, students });
            }

            return Ok(result);
        }

        // ── Package Breakdown ─────────────────────────────────────────────────
        [HttpGet("package-breakdown")]
        public async Task<IActionResult> GetPackageBreakdown([FromQuery] int? branchId = null)
        {
            var callerBranchId    = ClaimsHelper.GetBranchId(User);
            var effectiveBranchId = callerBranchId ?? (branchId > 0 ? branchId : null);

            var query = _context.Students
                .Where(s => s.Active == true && s.CoursePackageId != null);

            if (effectiveBranchId.HasValue)
                query = query.Where(s => s.BranchId == effectiveBranchId);

            // Fetch into memory then group (avoids EF GroupBy translation issues)
            var rows = await query
                .Select(s => new
                {
                    s.CoursePackageId,
                    PackageName = s.CoursePackage != null ? s.CoursePackage.PackageName : "Unknown",
                    Price       = s.CoursePackage != null ? s.CoursePackage.Price : 0m
                })
                .ToListAsync();

            var grouped = rows
                .GroupBy(r => new { r.CoursePackageId, r.PackageName, r.Price })
                .Select(g => new
                {
                    packageId   = g.Key.CoursePackageId,
                    packageName = g.Key.PackageName,
                    price       = g.Key.Price,
                    count       = g.Count()
                })
                .OrderByDescending(x => x.count)
                .Take(6)
                .ToList();

            return Ok(grouped);
        }

        // ── Recent Students ───────────────────────────────────────────────────
        [HttpGet("recent-students")]
        public async Task<IActionResult> GetRecentStudents([FromQuery] int? branchId = null)
        {
            var callerBranchId    = ClaimsHelper.GetBranchId(User);
            var effectiveBranchId = callerBranchId ?? (branchId > 0 ? branchId : null);
            var query             = _context.Students.Include(s => s.Branch).Where(s => s.Active == true);
            if (effectiveBranchId.HasValue) query = query.Where(s => s.BranchId == effectiveBranchId);

            var students = await query
                .OrderByDescending(s => s.RegistrationDate)
                .Take(8)
                .Select(s => new {
                    s.Id, s.StudentName, s.Nic, s.PhoneNumber,
                    s.PackageType, s.RegistrationDate,
                    BranchName = s.Branch.Name
                }).ToListAsync();

            return Ok(students);
        }

        // ── Recent Payments ───────────────────────────────────────────────────
        [HttpGet("recent-payments")]
        public async Task<IActionResult> GetRecentPayments([FromQuery] int? branchId = null)
        {
            var callerBranchId    = ClaimsHelper.GetBranchId(User);
            var effectiveBranchId = callerBranchId ?? (branchId > 0 ? branchId : null);
            var query = _context.Payments
                .Include(p => p.Student)
                .Include(p => p.Bill)
                .Where(p => p.Student != null);

            if (effectiveBranchId.HasValue)
                query = query.Where(p => p.Student.BranchId == effectiveBranchId);

            var payments = await query
                .OrderByDescending(p => p.PaymentDate)
                .Take(8)
                .Select(p => new {
                    p.Id, p.Amount, p.PaymentDate, p.PaymentMethod,
                    StudentName = p.Student.StudentName,
                    BillNumber  = p.Bill != null ? p.Bill.BillNumber : ""
                }).ToListAsync();

            return Ok(payments);
        }
    }
}
