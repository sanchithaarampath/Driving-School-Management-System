using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public DashboardController(DsmsDbContext context)
        {
            _context = context;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalStudents = await _context.Students.CountAsync(s => s.Active == true);
            var totalBills = await _context.Bills.CountAsync(b => b.Active == true);
            var totalIncome = await _context.Bills.Where(b => b.Active == true).SumAsync(b => b.PaidAmount);
            var pendingAmount = await _context.Bills.Where(b => b.Active == true).SumAsync(b => b.BalanceAmount);
            var pendingBills = await _context.Bills.CountAsync(b => b.Active == true && b.BalanceAmount > 0);

            return Ok(new {
                totalStudents,
                totalBills,
                totalIncome,
                pendingAmount,
                pendingBills
            });
        }

        [HttpGet("recent-students")]
        public async Task<IActionResult> GetRecentStudents()
        {
            var students = await _context.Students
                .Include(s => s.Branch)
                .Where(s => s.Active == true)
                .OrderByDescending(s => s.RegistrationDate)
                .Take(5)
                .Select(s => new {
                    s.Id,
                    s.StudentName,
                    s.Nic,
                    s.PhoneNumber,
                    s.RegistrationDate,
                    BranchName = s.Branch.Name
                })
                .ToListAsync();
            return Ok(students);
        }

        [HttpGet("recent-payments")]
        public async Task<IActionResult> GetRecentPayments()
        {
            var payments = await _context.Payments
                .Include(p => p.Student)
                .OrderByDescending(p => p.PaymentDate)
                .Take(5)
                .Select(p => new {
                    p.Id,
                    p.Amount,
                    p.PaymentDate,
                    p.PaymentMethod,
                    StudentName = p.Student.StudentName
                })
                .ToListAsync();
            return Ok(payments);
        }
    }
}
