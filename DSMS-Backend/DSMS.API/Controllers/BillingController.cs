using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;
using DSMS.API.DTOs;
using DSMS.API.Services;

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
            var bills = await _context.Bills
                .Include(b => b.Student)
                .Where(b => b.Active == true)
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
            var bills = await _context.Bills
                .Include(b => b.Payments)
                .Where(b => b.StudentId == studentId && b.Active == true)
                .OrderByDescending(b => b.BillDate)
                .ToListAsync();
            return Ok(bills);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] BillCreateDto dto)
        {
            // Auto-create package registration if none exists
            var registration = await _context.StudentPackageRegistrations
                .FirstOrDefaultAsync(r => r.StudentId == dto.StudentId && r.Active == true);

            if (registration == null)
            {
                // Get first available package
                var package = await _context.Packages.FirstOrDefaultAsync(p => p.Active == true);
                int packageId = package?.Id ?? 1;

                registration = new StudentPackageRegistration
                {
                    StudentId = dto.StudentId,
                    PackageHeaderId = packageId,
                    TotalAmount = dto.TotalAmount,
                    DiscountAmount = dto.DiscountAmount,
                    BalanceAmount = dto.NetAmount,
                    TotalTrainingHours = 0,
                    CompletedTrainingHours = 0,
                    TotalLectureHours = 0,
                    CompletedLectureHours = 0,
                    ExamAttempts = 0,
                    Active = true,
                    CreatedBy = User.Identity?.Name ?? "system",
                    CreatedDateTime = DateTime.Now
                };
                _context.StudentPackageRegistrations.Add(registration);
                await _context.SaveChangesAsync();
            }

            var lastBill = await _context.Bills.OrderByDescending(b => b.Id).FirstOrDefaultAsync();
            int nextNumber = (lastBill == null) ? 1 : lastBill.Id + 1;

            var bill = new Bill
            {
                BillNumber = "BILL-" + nextNumber.ToString("D5"),
                StudentId = dto.StudentId,
                StudentPackageRegistrationId = registration.Id,
                BillDate = DateTime.Now,
                TotalAmount = dto.TotalAmount,
                DiscountAmount = dto.DiscountAmount,
                NetAmount = dto.NetAmount,
                PaidAmount = dto.PaidAmount,
                BalanceAmount = dto.NetAmount - dto.PaidAmount,
                Status = (dto.NetAmount - dto.PaidAmount) <= 0 ? "Paid" :
                         dto.PaidAmount > 0 ? "Partial" : "Pending",
                Remarks = dto.Remarks,
                Active = true,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Bill created successfully", id = bill.Id, billNumber = bill.BillNumber });
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

        [HttpGet("pending")]
        public async Task<IActionResult> GetPending()
        {
            var bills = await _context.Bills
                .Include(b => b.Student)
                .Where(b => b.Active == true && b.BalanceAmount > 0)
                .OrderByDescending(b => b.BillDate)
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
            var bills = await _context.Bills.Where(b => b.Active == true).ToListAsync();

            return Ok(new {
                totalAmount = bills.Sum(b => b.NetAmount),
                paidAmount = bills.Sum(b => b.PaidAmount),
                pendingAmount = bills.Sum(b => b.BalanceAmount),
                totalBills = bills.Count
            });
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