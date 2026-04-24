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

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var branchId = ClaimsHelper.GetBranchId(User);

            var studentQ = _context.Students.Where(s => s.Active == true);
            var billQ    = _context.Bills.Where(b => b.Active == true);

            if (branchId.HasValue)
            {
                studentQ = studentQ.Where(s => s.BranchId == branchId);
                billQ    = billQ.Where(b => b.Student.BranchId == branchId);
            }

            var totalStudents  = await studentQ.CountAsync();
            var totalBills     = await billQ.CountAsync();
            var totalIncome    = await billQ.SumAsync(b => (decimal?)b.PaidAmount) ?? 0;
            var pendingAmount  = await billQ.SumAsync(b => (decimal?)b.BalanceAmount) ?? 0;
            var pendingBills   = await billQ.CountAsync(b => b.BalanceAmount > 0);

            // New students this month
            var firstOfMonth   = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var newThisMonth   = await studentQ.CountAsync(s => s.RegistrationDate >= firstOfMonth);

            return Ok(new { totalStudents, totalBills, totalIncome, pendingAmount, pendingBills, newThisMonth });
        }

        [HttpGet("recent-students")]
        public async Task<IActionResult> GetRecentStudents()
        {
            var branchId = ClaimsHelper.GetBranchId(User);
            var query = _context.Students.Include(s => s.Branch).Where(s => s.Active == true);
            if (branchId.HasValue) query = query.Where(s => s.BranchId == branchId);

            var students = await query
                .OrderByDescending(s => s.RegistrationDate)
                .Take(8)
                .Select(s => new {
                    s.Id, s.StudentName, s.Nic, s.PhoneNumber,
                    s.PackageType, s.RegistrationDate, BranchName = s.Branch.Name
                }).ToListAsync();

            return Ok(students);
        }

        [HttpGet("recent-payments")]
        public async Task<IActionResult> GetRecentPayments()
        {
            var branchId = ClaimsHelper.GetBranchId(User);
            var query = _context.Payments.Include(p => p.Student).Include(p => p.Bill)
                .Where(p => p.Student != null);

            if (branchId.HasValue)
                query = query.Where(p => p.Student.BranchId == branchId);

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
