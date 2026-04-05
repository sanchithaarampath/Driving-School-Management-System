using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;
using DSMS.API.DTOs;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public StudentController(DsmsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var students = await _context.Students
                .Include(s => s.Branch)
                .Where(s => s.Active == true)
                .Select(s => new {
                    s.Id,
                    s.StudentName,
                    s.Nic,
                    s.PhoneNumber,
                    s.Email,
                    s.Address,
                    s.Gender,
                    s.RegistrationDate,
                    s.ExistingLicenseNo,
                    BranchName = s.Branch.Name
                })
                .ToListAsync();
            return Ok(students);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var student = await _context.Students
                .Include(s => s.Branch)
                .FirstOrDefaultAsync(s => s.Id == id && s.Active == true);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            return Ok(new {
                student.Id,
                student.BranchId,
                student.StudentName,
                student.Email,
                student.PhoneNumber,
                student.WhatsAppNumber,
                student.Address,
                student.Nic,
                Dob = student.Dob.ToString("yyyy-MM-dd"),
                student.Gender,
                student.NearestPoliceStation,
                student.NearestDivisionalSecretariat,
                student.ExistingLicenseNo,
                student.IsSpecialRequirements,
                student.SpecialRequirementTypeId,
                student.RegistrationDate,
                BranchName = student.Branch.Name
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] StudentCreateDto dto)
        {
            var existingNic = await _context.Students
                .FirstOrDefaultAsync(s => s.Nic == dto.Nic && s.Active == true);
            if (existingNic != null)
                return BadRequest(new { message = "A student with this NIC already exists" });

            var student = new Student
            {
                BranchId = dto.BranchId,
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
                ExistingLicenseNo = dto.ExistingLicenseNo,
                IsSpecialRequirements = dto.IsSpecialRequirements,
                SpecialRequirementTypeId = dto.SpecialRequirementTypeId,
                Active = true,
                RegistrationDate = DateTime.Now,
                CreatedDateTime = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "system"
            };

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Student registered successfully", id = student.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] StudentUpdateDto dto)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            student.StudentName = dto.StudentName;
            student.Email = dto.Email;
            student.PhoneNumber = dto.PhoneNumber;
            student.WhatsAppNumber = dto.WhatsAppNumber;
            student.Address = dto.Address;
            student.Gender = dto.Gender;
            student.NearestPoliceStation = dto.NearestPoliceStation;
            student.NearestDivisionalSecretariat = dto.NearestDivisionalSecretariat;
            student.ExistingLicenseNo = dto.ExistingLicenseNo;
            student.IsSpecialRequirements = dto.IsSpecialRequirements;
            student.SpecialRequirementTypeId = dto.SpecialRequirementTypeId;
            student.LastModifiedBy = User.Identity?.Name ?? "system";
            student.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Student updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            student.Active = false;
            student.LastModifiedBy = User.Identity?.Name ?? "system";
            student.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Student deleted successfully" });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? name, [FromQuery] string? nic, [FromQuery] string? phone)
        {
            var query = _context.Students
                .Include(s => s.Branch)
                .Where(s => s.Active == true);

            if (!string.IsNullOrEmpty(name))
                query = query.Where(s => s.StudentName.Contains(name));
            if (!string.IsNullOrEmpty(nic))
                query = query.Where(s => s.Nic.Contains(nic));
            if (!string.IsNullOrEmpty(phone))
                query = query.Where(s => s.PhoneNumber.Contains(phone));

            var students = await query.Select(s => new {
                s.Id,
                s.StudentName,
                s.Nic,
                s.PhoneNumber,
                s.Email,
                s.Address,
                s.Gender,
                s.RegistrationDate,
                BranchName = s.Branch.Name
            }).ToListAsync();

            return Ok(students);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _context.Students.CountAsync(s => s.Active == true);
            return Ok(new { count });
        }
    }
}