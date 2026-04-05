import os

content = '''using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;

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
                .Include(s => s.SpecialRequirementType)
                .FirstOrDefaultAsync(s => s.Id == id && s.Active == true);

            if (student == null)
                return NotFound(new { message = "Student not found" });

            return Ok(student);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Student student)
        {
            var existingNic = await _context.Students
                .FirstOrDefaultAsync(s => s.Nic == student.Nic && s.Active == true);
            if (existingNic != null)
                return BadRequest(new { message = "A student with this NIC already exists" });

            student.Active = true;
            student.RegistrationDate = DateTime.Now;
            student.CreatedDateTime = DateTime.Now;
            student.CreatedBy = User.Identity?.Name ?? "system";

            _context.Students.Add(student);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Student registered successfully", id = student.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] Student updated)
        {
            var student = await _context.Students.FindAsync(id);
            if (student == null)
                return NotFound(new { message = "Student not found" });

            student.StudentName = updated.StudentName;
            student.Email = updated.Email;
            student.PhoneNumber = updated.PhoneNumber;
            student.WhatsAppNumber = updated.WhatsAppNumber;
            student.Address = updated.Address;
            student.Gender = updated.Gender;
            student.NearestPoliceStation = updated.NearestPoliceStation;
            student.NearestDivisionalSecretariat = updated.NearestDivisionalSecretariat;
            student.ExistingLicenseNo = updated.ExistingLicenseNo;
            student.IsSpecialRequirements = updated.IsSpecialRequirements;
            student.SpecialRequirementTypeId = updated.SpecialRequirementTypeId;
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
'''

os.makedirs("Controllers", exist_ok=True)
with open("Controllers/StudentController.cs", "w", encoding="utf-8") as f:
    f.write(content)
print("StudentController.cs created successfully!")
