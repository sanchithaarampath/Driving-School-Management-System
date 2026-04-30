using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;
using DSMS.API.DTOs;
using DSMS.API.Services;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BillingController : ControllerBase
    {
        private readonly DsmsDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IWhatsAppService _whatsAppService;
        private readonly IReceiptService _receiptService;

        public BillingController(
            DsmsDbContext context,
            IEmailService emailService,
            IWhatsAppService whatsAppService,
            IReceiptService receiptService)
        {
            _context = context;
            _emailService = emailService;
            _whatsAppService = whatsAppService;
            _receiptService = receiptService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.Bills
                .Where(b => b.Active != false)
                .AsQueryable();

            // Branch Admin / Staff only see bills for their branch's students
            if (callerBranchId.HasValue)
                query = query.Where(b => b.Student.BranchId == callerBranchId);

            var bills = await query
                .OrderByDescending(b => b.BillDate)
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
                    StudentNic  = b.Student.Nic
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

            return Ok(new {
                bill.Id,
                bill.BillNumber,
                bill.BillDate,
                bill.TotalAmount,
                bill.DiscountAmount,
                bill.NetAmount,
                bill.PaidAmount,
                bill.BalanceAmount,
                bill.Status,
                bill.Remarks,
                bill.StudentId,
                StudentName = bill.Student.StudentName,
                StudentNic = bill.Student.Nic,
                Payments = bill.Payments.Select(p => new {
                    p.Id,
                    p.Amount,
                    p.PaymentDate,
                    p.PaymentMethod,
                    p.ReferenceNo,
                    p.Remarks
                })
            });
        }

        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            try
            {
                // Step 1 — raw SQL via FromSqlRaw to bypass any EF translation issue
                // Use direct query: no navigation props, no nullable bool ambiguity
                var bills = await _context.Bills
                    .Where(b => b.StudentId == studentId)
                    .OrderByDescending(b => b.BillDate)
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
                        b.Remarks
                    })
                    .ToListAsync();

                if (!bills.Any())
                    return Ok(new List<object>());

                // Step 2 — fetch payments separately (no navigation, no circular refs)
                var billIds = bills.Select(b => b.Id).ToList();
                var payments = await _context.Payments
                    .Where(p => billIds.Contains(p.BillId))
                    .OrderByDescending(p => p.PaymentDate)
                    .Select(p => new {
                        p.Id,
                        p.BillId,
                        p.Amount,
                        p.PaymentDate,
                        p.PaymentMethod,
                        ReferenceNo = p.ReferenceNo ?? "",
                        Remarks     = p.Remarks ?? ""
                    })
                    .ToListAsync();

                // Step 3 — combine in memory
                var result = bills.Select(b => new {
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
                    Payments = payments.Where(p => p.BillId == b.Id).ToList()
                }).ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                // Return the actual exception so the frontend can show it
                return StatusCode(500, new {
                    message = ex.Message,
                    inner   = ex.InnerException?.Message ?? ""
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BillCreateDto dto)
        {
            // ── Each bill = one instalment payment ────────────────────────────
            // dto.PackagePrice      = student's registered package total (for receipt context)
            // dto.InstallmentAmount = amount the student pays RIGHT NOW

            if (dto.InstallmentAmount <= 0)
                return BadRequest(new { message = "Instalment amount must be greater than zero." });

            // Sum of all previous instalments for this student
            var previouslyPaid = await _context.Bills
                .Where(b => b.StudentId == dto.StudentId && b.Active != false)
                .SumAsync(b => (decimal?)b.PaidAmount) ?? 0;

            var balanceAfter = Math.Max(0, dto.PackagePrice - previouslyPaid - dto.InstallmentAmount);

            if (dto.InstallmentAmount > (dto.PackagePrice - previouslyPaid))
                return BadRequest(new { message = "Instalment exceeds the remaining balance." });

            var lastBill = await _context.Bills.OrderByDescending(b => b.Id).FirstOrDefaultAsync();
            int nextNumber = (lastBill == null) ? 1 : lastBill.Id + 1;

            var bill = new Bill
            {
                BillNumber   = "BILL-" + nextNumber.ToString("D5"),
                StudentId    = dto.StudentId,
                BillDate     = DateTime.Now,
                TotalAmount  = dto.PackagePrice,        // package total — for receipt context
                DiscountAmount = dto.DiscountAmount,
                NetAmount    = dto.InstallmentAmount,   // this instalment
                PaidAmount   = dto.InstallmentAmount,   // fully settled
                BalanceAmount= balanceAfter,             // remaining after this payment
                Status       = balanceAfter <= 0 ? "Paid" : "Partial",
                Remarks      = dto.Remarks,
                Active       = true,
                CreatedBy    = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            // Payment record for this instalment
            var payment = new Payment
            {
                BillId        = bill.Id,
                StudentId     = dto.StudentId,
                PaymentDate   = DateTime.Now,
                Amount        = dto.InstallmentAmount,
                PaymentMethod = dto.PaymentMethod,
                ReferenceNo   = dto.ReferenceNo,
                Remarks       = dto.Remarks,
                CreatedBy     = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Instalment recorded", id = bill.Id, billNumber = bill.BillNumber });
        }

        [HttpPost("{id}/payment")]
        public async Task<IActionResult> AddPayment(int id, [FromBody] PaymentCreateDto dto)
        {
            var bill = await _context.Bills.FindAsync(id);
            if (bill == null)
                return NotFound(new { message = "Bill not found" });

            if (dto.Amount > bill.BalanceAmount)
                return BadRequest(new { message = "Payment amount exceeds balance" });

            var payment = new Payment
            {
                BillId = id,
                StudentId = bill.StudentId,
                PaymentDate = DateTime.Now,
                Amount = dto.Amount,
                PaymentMethod = dto.PaymentMethod,
                ReferenceNo = dto.ReferenceNo,
                Remarks = dto.Remarks,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.Payments.Add(payment);

            bill.PaidAmount += dto.Amount;
            bill.BalanceAmount -= dto.Amount;
            bill.Status = bill.BalanceAmount <= 0 ? "Paid" : "Partial";
            bill.LastModifiedBy = User.Identity?.Name ?? "system";
            bill.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Payment recorded successfully" });
        }

        // DELETE /api/billing/{id} — soft-delete a bill and its payments
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBill(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (bill == null)
                return NotFound(new { message = "Bill not found" });

            // Soft-delete the bill
            bill.Active = false;
            bill.LastModifiedBy       = User.Identity?.Name ?? "system";
            bill.LastModifiedDateTime = DateTime.Now;

            // Also remove associated payments from the Payments table (hard delete)
            if (bill.Payments?.Any() == true)
                _context.Payments.RemoveRange(bill.Payments);

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Bill {bill.BillNumber} deleted successfully." });
        }

        // POST /api/billing/recompute-balances — fix any bills whose PaidAmount was double-counted
        [HttpPost("recompute-balances")]
        public async Task<IActionResult> RecomputeBalances()
        {
            var bills = await _context.Bills.ToListAsync();
            int fixedCount = 0;

            foreach (var bill in bills)
            {
                var actualPaid = await _context.Payments
                    .Where(p => p.BillId == bill.Id)
                    .SumAsync(p => (decimal?)p.Amount) ?? 0;

                var newBalance = bill.NetAmount - actualPaid;
                var newStatus  = newBalance <= 0 ? "Paid" : actualPaid > 0 ? "Partial" : "Pending";

                if (bill.PaidAmount != actualPaid || bill.BalanceAmount != newBalance || bill.Status != newStatus)
                {
                    bill.PaidAmount        = actualPaid;
                    bill.BalanceAmount     = newBalance;
                    bill.Status            = newStatus;
                    bill.LastModifiedBy       = User.Identity?.Name ?? "system";
                    bill.LastModifiedDateTime = DateTime.Now;
                    fixedCount++;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Recomputed {fixedCount} bill(s) from actual payment records." });
        }

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.Bills
                .Where(b => b.Active == true && b.BalanceAmount > 0)
                .AsQueryable();

            if (callerBranchId.HasValue)
                query = query.Where(b => b.Student.BranchId == callerBranchId);

            var bills = await query
                .OrderByDescending(b => b.BillDate)
                .Select(b => new {
                    b.Id,
                    b.BillNumber,
                    b.BillDate,
                    b.NetAmount,
                    b.PaidAmount,
                    b.BalanceAmount,
                    b.Status,
                    StudentName  = b.Student.StudentName,
                    StudentNic   = b.Student.Nic,
                    StudentPhone = b.Student.PhoneNumber
                })
                .ToListAsync();

            return Ok(bills);
        }

        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.Bills.Where(b => b.Active != false).AsQueryable();

            if (callerBranchId.HasValue)
                query = query.Where(b => b.Student.BranchId == callerBranchId);

            var bills = await query
                .Select(b => new { b.PaidAmount, b.BalanceAmount })
                .ToListAsync();

            return Ok(new {
                totalAmount        = bills.Sum(b => b.PaidAmount),   // total collected (correct for all bill types)
                paidAmount         = bills.Sum(b => b.PaidAmount),
                pendingAmount      = bills.Sum(b => b.BalanceAmount),
                totalBills         = bills.Count,
                pendingBillsCount  = bills.Count(b => b.BalanceAmount > 0)
            });
        }

        // GET /api/billing/monthly-summary — last 6 months revenue (branch-scoped)
        [HttpGet("monthly-summary")]
        public async Task<IActionResult> GetMonthlySummary()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);
            var since = DateTime.Now.AddMonths(-5).Date;

            var paymentQuery = _context.Payments.Where(p => p.PaymentDate >= since).AsQueryable();

            if (callerBranchId.HasValue)
                paymentQuery = paymentQuery.Where(p => p.Student.BranchId == callerBranchId);

            var payments = await paymentQuery.ToListAsync();

            var months = Enumerable.Range(0, 6)
                .Select(i => DateTime.Now.AddMonths(-5 + i))
                .Select(d => new {
                    label   = d.ToString("MMM yyyy"),
                    year    = d.Year,
                    month   = d.Month,
                    revenue = payments
                        .Where(p => p.PaymentDate.Year == d.Year && p.PaymentDate.Month == d.Month)
                        .Sum(p => (decimal?)p.Amount) ?? 0
                }).ToList();

            return Ok(months);
        }

        // ─── Send Receipt ─────────────────────────────────────────────────────

        [HttpPost("{id}/send-receipt")]
        public async Task<IActionResult> SendReceipt(int id, [FromBody] SendReceiptDto dto)
        {
            var bill = await _context.Bills
                .Include(b => b.Student)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id && b.Active == true);

            if (bill == null)
                return NotFound(new { message = "Bill not found" });

            var lastPayment = bill.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

            var receiptData = new ReceiptData
            {
                BillNumber    = bill.BillNumber,
                BillDate      = bill.BillDate,
                StudentName   = bill.Student.StudentName,
                StudentNic    = bill.Student.Nic,
                StudentPhone  = bill.Student.PhoneNumber,
                StudentEmail  = bill.Student.Email,
                TotalAmount   = bill.TotalAmount,
                DiscountAmount= bill.DiscountAmount,
                NetAmount     = bill.NetAmount,
                PaidAmount    = bill.PaidAmount,
                BalanceAmount = bill.BalanceAmount,
                Status        = bill.Status,
                PaymentMethod = lastPayment?.PaymentMethod ?? "N/A",
                ReferenceNo   = lastPayment?.ReferenceNo,
                Remarks       = bill.Remarks
            };

            var sent = new List<string>();
            var errors = new List<string>();

            // Email
            if (dto.SendEmail)
            {
                var email = dto.OverrideEmail ?? bill.Student.Email;
                if (string.IsNullOrEmpty(email))
                {
                    errors.Add("No email address found for student.");
                }
                else
                {
                    var html = _receiptService.GenerateReceiptHtml(receiptData);
                    var ok   = await _emailService.SendReceiptEmailAsync(email, bill.Student.StudentName, html, bill.BillNumber);
                    if (ok) sent.Add($"Email sent to {email}");
                    else    errors.Add("Email delivery failed — check SMTP configuration.");
                }
            }

            // WhatsApp
            if (dto.SendWhatsApp)
            {
                var phone = dto.OverridePhone ?? bill.Student.WhatsAppNumber ?? bill.Student.PhoneNumber;
                if (string.IsNullOrEmpty(phone))
                {
                    errors.Add("No phone number found for student.");
                }
                else
                {
                    var text = _receiptService.GenerateReceiptText(receiptData);
                    var ok   = await _whatsAppService.SendReceiptWhatsAppAsync(phone, text);
                    if (ok) sent.Add($"WhatsApp sent to {phone}");
                    else    errors.Add("WhatsApp delivery failed — check Twilio configuration.");
                }
            }

            return Ok(new
            {
                billNumber = bill.BillNumber,
                sent,
                errors,
                receiptHtml = _receiptService.GenerateReceiptHtml(receiptData)
            });
        }

        // ─── Receipt Preview (returns HTML for in-browser preview) ───────────

        [HttpGet("{id}/receipt")]
        public async Task<IActionResult> GetReceiptHtml(int id)
        {
            var bill = await _context.Bills
                .Include(b => b.Student)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.Id == id && b.Active == true);

            if (bill == null) return NotFound();

            var lastPayment = bill.Payments.OrderByDescending(p => p.PaymentDate).FirstOrDefault();

            var receiptData = new ReceiptData
            {
                BillNumber    = bill.BillNumber,
                BillDate      = bill.BillDate,
                StudentName   = bill.Student.StudentName,
                StudentNic    = bill.Student.Nic,
                StudentPhone  = bill.Student.PhoneNumber,
                StudentEmail  = bill.Student.Email,
                TotalAmount   = bill.TotalAmount,
                DiscountAmount= bill.DiscountAmount,
                NetAmount     = bill.NetAmount,
                PaidAmount    = bill.PaidAmount,
                BalanceAmount = bill.BalanceAmount,
                Status        = bill.Status,
                PaymentMethod = lastPayment?.PaymentMethod ?? "N/A",
                ReferenceNo   = lastPayment?.ReferenceNo,
                Remarks       = bill.Remarks
            };

            var html = _receiptService.GenerateReceiptHtml(receiptData);
            return Content(html, "text/html");
        }
    }
}