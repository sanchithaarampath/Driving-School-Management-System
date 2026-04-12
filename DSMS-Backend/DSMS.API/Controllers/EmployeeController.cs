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
    public class EmployeeController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public EmployeeController(DsmsDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var employees = await _context.Employees
                .Include(e => e.Branch)
                .Where(e => e.Active == true)
                .Select(e => new {
                    e.Id,
                    e.EmployeeName,
                    e.Nic,
                    e.Phone,
                    e.Email,
                    e.Designation,
                    e.Department,
                    e.JoinDate,
                    e.UserId,
                    BranchName = e.Branch.Name
                })
                .ToListAsync();
            return Ok(employees);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var employee = await _context.Employees
                .Include(e => e.Branch)
                .FirstOrDefaultAsync(e => e.Id == id && e.Active == true);

            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            return Ok(new {
                employee.Id,
                employee.BranchId,
                employee.UserId,
                employee.EmployeeName,
                employee.Nic,
                employee.Phone,
                employee.Email,
                employee.Designation,
                employee.Department,
                JoinDate = employee.JoinDate.HasValue ? employee.JoinDate.Value.ToString("yyyy-MM-dd") : null,
                employee.Address,
                employee.EmergencyContact,
                BranchName = employee.Branch.Name
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] EmployeeCreateDto dto)
        {
            var existingNic = await _context.Employees
                .FirstOrDefaultAsync(e => e.Nic == dto.Nic && e.Active == true);
            if (existingNic != null)
                return BadRequest(new { message = "An employee with this NIC already exists" });

            var employee = new Employee
            {
                BranchId = dto.BranchId,
                UserId = dto.UserId,
                EmployeeName = dto.EmployeeName,
                Nic = dto.Nic,
                Phone = dto.Phone,
                Email = dto.Email,
                Designation = dto.Designation,
                Department = dto.Department,
                JoinDate = string.IsNullOrEmpty(dto.JoinDate) ? null : DateTime.Parse(dto.JoinDate),
                Address = dto.Address,
                EmergencyContact = dto.EmergencyContact,
                Active = true,
                CreatedDateTime = DateTime.Now,
                CreatedBy = User.Identity?.Name ?? "system"
            };

            _context.Employees.Add(employee);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Employee added successfully", id = employee.Id });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeUpdateDto dto)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            employee.BranchId = dto.BranchId;
            employee.UserId = dto.UserId;
            employee.EmployeeName = dto.EmployeeName;
            employee.Phone = dto.Phone;
            employee.Email = dto.Email;
            employee.Designation = dto.Designation;
            employee.Department = dto.Department;
            employee.JoinDate = string.IsNullOrEmpty(dto.JoinDate) ? null : DateTime.Parse(dto.JoinDate);
            employee.Address = dto.Address;
            employee.EmergencyContact = dto.EmergencyContact;
            employee.LastModifiedBy = User.Identity?.Name ?? "system";
            employee.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Employee updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
                return NotFound(new { message = "Employee not found" });

            employee.Active = false;
            employee.LastModifiedBy = User.Identity?.Name ?? "system";
            employee.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Employee deleted successfully" });
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? name, [FromQuery] string? nic, [FromQuery] string? department)
        {
            var query = _context.Employees
                .Include(e => e.Branch)
                .Where(e => e.Active == true);

            if (!string.IsNullOrEmpty(name))
                query = query.Where(e => e.EmployeeName.Contains(name));
            if (!string.IsNullOrEmpty(nic))
                query = query.Where(e => e.Nic.Contains(nic));
            if (!string.IsNullOrEmpty(department))
                query = query.Where(e => e.Department != null && e.Department.Contains(department));

            var employees = await query.Select(e => new {
                e.Id,
                e.EmployeeName,
                e.Nic,
                e.Phone,
                e.Email,
                e.Designation,
                e.Department,
                e.JoinDate,
                BranchName = e.Branch.Name
            }).ToListAsync();

            return Ok(employees);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetCount()
        {
            var count = await _context.Employees.CountAsync(e => e.Active == true);
            return Ok(new { count });
        }
    }
}
