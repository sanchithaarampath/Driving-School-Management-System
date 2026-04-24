using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;
using DSMS.API.DTOs;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public StudentController(DsmsDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.Students
                .Include(s => s.Branch)
                .Where(s => s.Active == true);

            if (callerBranchId.HasValue)
                query = query.Where(s => s.BranchId == callerBranchId);

            var students = await query.Select(s => new {
                s.Id, s.StudentName, s.Nic, s.PhoneNumber, s.Email,
                s.Address, s.Gender, s.RegistrationDate, s.ExistingLicenseNo,
                s.PackageType, BranchName = s.Branch.Name
            }).ToListAsync();

            return Ok(students);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var student = await _context.Students
                .Include(s => s.Branch)
                .FirstOrDefaultAsync(s => s.Id == id && s.Active == true);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            if (callerBranchId.HasValue && student.BranchId != callerBranchId)
                return Forbid();

            var vehicleClasses = await _context.StudentVehicleClasses
                .Where(v => v.StudentId == id)
                .Select(v => v.VehicleClassCode)
                .ToListAsync();

            var payments = await _context.Payments
                .Where(p => p.StudentId == id)
                .OrderByDescending(p => p.PaymentDate)
                .Select(p => new { p.Id, p.Amount, p.PaymentMethod, p.PaymentDate, p.ReferenceNo, p.Remarks })
                .ToListAsync();

            var sprs = await _context.StudentPackageRegistrations
                .Where(s => s.StudentId == id && s.Active == true)
                .Select(s => new {
                    s.Id, PackageName = s.PackageHeader.PackageName,
                    s.TotalAmount, s.DiscountAmount, s.BalanceAmount,
                    s.ExamStatus, s.ExamAttempts, s.ExamDate,
                    s.IsRecommendForTrial, s.CompletedTrainingHours, s.TotalTrainingHours
                }).ToListAsync();

            return Ok(new {
                student.Id, student.BranchId, student.StudentName, student.Email,
                student.PhoneNumber, student.WhatsAppNumber, student.Address,
                student.Nic, Dob = student.Dob.ToString("yyyy-MM-dd"), student.Gender,
                student.NearestPoliceStation, student.NearestDivisionalSecretariat,
                student.PostalCode, student.ExistingLicenseNo, student.PackageType,
                student.IsSpecialRequirements, student.SpecialRequirementTypeId,
                student.HasBirthCertificate, student.HasNtmiMedical, student.HasNicCopy,
                student.RegistrationDate, BranchName = student.Branch.Name,
                vehicleClasses, payments, registrations = sprs
            });
        }

        [HttpPost]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin,Staff")]
        public async Task<IActionResult> Create([FromBody] StudentCreateExtDto dto)
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);
            var branchId = callerBranchId ?? dto.BranchId;

            var existingNic = await _context.Students
                .FirstOrDefaultAsync(s => s.Nic == dto.Nic && s.Active == true);
            if (existingNic != null)
                return BadRequest(new { message = "A student with this NIC already exists" });

            var student = new Student
            {
                BranchId = branchId,
                StudentName = dto.StudentName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                WhatsAppNumber = dto.WhatsAppNumber,
                Address = dto.Address,
                Nic = dto.Nic,
                Dob = DateTime.Parse(dto.Dob),
                Gender = dto.Gender,
                NearestPoliceStation = dto.NearestPoliceStation,
                NearestDivisionalSecretariat = dto.NearestDivisionalSecretariat,
                PostalCode = dto.PostalCode,
                ExistingLicenseNo = dto.ExistingLicenseNo,
                PackageType = dto.PackageType,
                IsSpecialRequirements = dto.IsSpecialRequirements,
                SpecialRequirementTypeId = dto.SpecialRequirementTypeId,
                HasBirthCertificate = dto.HasBirthCertificate,
                HasNtmiMedical = dto.HasNtmiMedical,
                HasNicCopy = dto.HasNicCopy,
                Active = true,
                RegistrationDate = DateTime.Now,
                CreatedDateTime = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "system"
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            // G1 is automatically added for all applicants
            var autoClasses = new List<string>(dto.VehicleClasses ?? new List<string>());
            if (!autoClasses.Contains("G1")) autoClasses.Add("G1");

            foreach (var code in autoClasses)
            {
                _context.StudentVehicleClasses.Add(new StudentVehicleClass
                {
                    StudentId = student.Id,
                    VehicleClassCode = code
                });
            }
            await _context.SaveChangesAsync();

            return Ok(new { message = "Student registered successfully", id = student.Id });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin,Staff")]
        public async Task<IActionResult> Update(int id, [FromBody] StudentCreateExtDto dto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            var callerBranchId = ClaimsHelper.GetBranchId(User);
            if (callerBranchId.HasValue && student.BranchId != callerBranchId)
                return Forbid();

            // Staff cannot edit
            if (ClaimsHelper.IsStaff(User))
                return Forbid();

            student.StudentName = dto.StudentName;
            student.Email = dto.Email;
            student.PhoneNumber = dto.PhoneNumber;
            student.WhatsAppNumber = dto.WhatsAppNumber;
            student.Address = dto.Address;
            student.Gender = dto.Gender;
            student.NearestPoliceStation = dto.NearestPoliceStation;
            student.NearestDivisionalSecretariat = dto.NearestDivisionalSecretariat;
            student.PostalCode = dto.PostalCode;
            student.ExistingLicenseNo = dto.ExistingLicenseNo;
            student.PackageType = dto.PackageType;
            student.IsSpecialRequirements = dto.IsSpecialRequirements;
            student.SpecialRequirementTypeId = dto.SpecialRequirementTypeId;
            student.HasBirthCertificate = dto.HasBirthCertificate;
            student.HasNtmiMedical = dto.HasNtmiMedical;
            student.HasNicCopy = dto.HasNicCopy;
            student.LastModifiedBy = User.Identity?.Name ?? "system";
            student.LastModifiedDateTime = DateTime.Now;

            // Update vehicle classes
            if (dto.VehicleClasses != null && dto.VehicleClasses.Any())
            {
                var existing = _context.StudentVehicleClasses.Where(v => v.StudentId == id);
                _context.StudentVehicleClasses.RemoveRange(existing);
                var classes = new List<string>(dto.VehicleClasses);
                if (!classes.Contains("G1")) classes.Add("G1");
                foreach (var code in classes)
                    _context.StudentVehicleClasses.Add(new StudentVehicleClass { StudentId = id, VehicleClassCode = code });
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Student updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            var callerBranchId = ClaimsHelper.GetBranchId(User);
            if (callerBranchId.HasValue && student.BranchId != callerBranchId)
                return Forbid();

            student.Active = false;
            student.LastModifiedBy = User.Identity?.Name ?? "system";
            student.LastModifiedDateTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Student deleted successfully" });
        }

        // GET /api/student/search?q=  — search by name, NIC, phone, student ID, or bill number
        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] string? name, [FromQuery] string? nic, [FromQuery] string? phone)
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.Students.Include(s => s.Branch).Where(s => s.Active == true);
            if (callerBranchId.HasValue)
                query = query.Where(s => s.BranchId == callerBranchId);

            // If query looks like a bill number, find student via billing
            if (!string.IsNullOrEmpty(q) && q.ToUpper().StartsWith("BILL-"))
            {
                var bill = await _context.Bills
                    .FirstOrDefaultAsync(b => b.BillNumber.Contains(q));
                if (bill != null)
                    query = query.Where(s => s.Id == bill.StudentId);
                else
                    return Ok(new List<object>());
            }
            else if (!string.IsNullOrEmpty(q))
            {
                query = query.Where(s =>
                    s.StudentName.Contains(q) ||
                    s.Nic.Contains(q) ||
                    s.PhoneNumber.Contains(q) ||
                    s.Id.ToString() == q);
            }

            if (!string.IsNullOrEmpty(name)) query = query.Where(s => s.StudentName.Contains(name));
            if (!string.IsNullOrEmpty(nic)) query = query.Where(s => s.Nic.Contains(nic));
            if (!string.IsNullOrEmpty(phone)) query = query.Where(s => s.PhoneNumber.Contains(phone));

            var students = await query.Select(s => new {
                s.Id, s.StudentName, s.Nic, s.PhoneNumber, s.Email,
                s.Gender, s.RegistrationDate, s.PackageType,
                BranchName = s.Branch.Name
            }).ToListAsync();

            return Ok(students);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);
            var query = _context.Students.Where(s => s.Active == true);
            if (callerBranchId.HasValue)
                query = query.Where(s => s.BranchId == callerBranchId);
            var count = await query.CountAsync();
            return Ok(new { count });
        }
    }
}
