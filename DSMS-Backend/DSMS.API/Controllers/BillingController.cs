using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public BillingController(DsmsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var bills = await _context.Bills
                .Include(b => b.Student)
                .Where(b => b.Active == true)
                .Select(b => new {
                    b.Id,
                    b.BillNumber,
                    b.BillDate,
                    b.TotalAmount,
                    b.DiscountAmount,
                    b.NetAmount,
                    b.PaidAmount,
                    b.BalanceAmount,
                    b.Status,
                    b.Remarks,
                    StudentName = b.Student.StudentName,
                    StudentNic = b.Student.Nic
                })
                .ToListAsync();
            return Ok(bills);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.Student)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id && b.Active == true);

            if (bill == null)
                return NotFound(new { message = "Bill not found" });

            return Ok(bill);
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var bills = await _context.Bills
                .Include(b => b.Payments)
                .Where(b => b.StudentId == studentId && b.Active == true)
                .ToListAsync();
            return Ok(bills);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Bill bill)
        {
            var lastBill = await _context.Bills
                .OrderByDescending(b => b.Id)
                .FirstOrDefaultAsync();

            int nextNumber = (lastBill == null) ? 1 : lastBill.Id + 1;
            bill.BillNumber = "BILL-" + nextNumber.ToString("D5");
            bill.BillDate = DateTime.Now;
            bill.BalanceAmount = bill.NetAmount - bill.PaidAmount;
            bill.Status = bill.BalanceAmount <= 0 ? "Paid" : "Pending";
            bill.Active = true;
            bill.CreatedBy = User.Identity?.Name ?? "system";
            bill.CreatedDateTime = DateTime.Now;

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bill created successfully", id = bill.Id, billNumber = bill.BillNumber });
        }

        [HttpPost("{id}/payment")]
        public async Task<IActionResult> AddPayment(int id, [FromBody] Payment payment)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill == null)
                return NotFound(new { message = "Bill not found" });

            if (payment.Amount > bill.BalanceAmount)
                return BadRequest(new { message = "Payment amount exceeds balance" });

            payment.BillId = id;
            payment.PaymentDate = DateTime.Now;
            payment.CreatedBy = User.Identity?.Name ?? "system";
            payment.CreatedDateTime = DateTime.Now;

            _context.Payments.Add(payment);

            bill.PaidAmount += payment.Amount;
            bill.BalanceAmount -= payment.Amount;
            bill.Status = bill.BalanceAmount <= 0 ? "Paid" : "Partial";
            bill.LastModifiedBy = User.Identity?.Name ?? "system";
            bill.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Payment recorded successfully" });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var bills = await _context.Bills
                .Include(b => b.Student)
                .Where(b => b.Active == true && b.BalanceAmount > 0)
                .Select(b => new {
                    b.Id,
                    b.BillNumber,
                    b.BillDate,
                    b.NetAmount,
                    b.PaidAmount,
                    b.BalanceAmount,
                    b.Status,
                    StudentName = b.Student.StudentName,
                    StudentNic = b.Student.Nic,
                    StudentPhone = b.Student.PhoneNumber
                })
                .ToListAsync();
            return Ok(bills);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var total = await _context.Bills.Where(b => b.Active == true).SumAsync(b => b.NetAmount);
            var paid = await _context.Bills.Where(b => b.Active == true).SumAsync(b => b.PaidAmount);
            var pending = await _context.Bills.Where(b => b.Active == true).SumAsync(b => b.BalanceAmount);
            var count = await _context.Bills.CountAsync(b => b.Active == true);

            return Ok(new { totalAmount = total, paidAmount = paid, pendingAmount = pending, totalBills = count });
        }
    }
}
