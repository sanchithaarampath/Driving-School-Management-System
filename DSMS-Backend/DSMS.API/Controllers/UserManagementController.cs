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
    [Route("api/users")]
    [Authorize]
    public class UserManagementController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public UserManagementController(DsmsDbContext context) => _context = context;

        // GET /api/users — Company Admin sees all, Branch Admin sees own branch
        [HttpGet]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> GetAll()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.UserSecurities
                .Include(u => u.Role)
                .AsQueryable();

            if (callerBranchId.HasValue)
                query = query.Where(u => u.BranchId == callerBranchId);

            var users = await query.Select(u => new {
                u.Id, u.UserName, u.UserFullName, u.RoleId,
                RoleName = u.Role.RoleName,
                u.BranchId, u.Active, u.FirstTimeLogin
            }).ToListAsync();

            return Ok(users);
        }

        // GET /api/users/roles — roles the caller can assign
        [HttpGet("roles")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> GetAssignableRoles()
        {
            var isCompanyAdmin = ClaimsHelper.IsCompanyAdmin(User);
            var roles = await _context.Roles.Where(r => r.Active == true).ToListAsync();

            if (!isCompanyAdmin)
                // Branch Admin can only create Staff and Instructor
                roles = roles.Where(r => r.RoleName == "Staff" || r.RoleName == "Instructor").ToList();

            return Ok(roles);
        }

        // POST /api/users — create user
        [HttpPost]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> Create([FromBody] CreateUserDto dto)
        {
            var isCompanyAdmin = ClaimsHelper.IsCompanyAdmin(User);
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            // Branch Admin cannot create Company Admin or Branch Admin
            if (!isCompanyAdmin)
            {
                var role = await _context.Roles.FindAsync(dto.RoleId);
                if (role?.RoleName == "Company Admin" || role?.RoleName == "Branch Admin")
                    return Forbid();
                // Force branch to be the caller's branch
                dto.BranchId = callerBranchId;
            }

            var exists = await _context.UserSecurities.AnyAsync(u => u.UserName == dto.UserName);
            if (exists) return BadRequest(new { message = "Username already exists" });

            var userSec = new UserSecurity
            {
                UserName = dto.UserName,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                UserFullName = dto.UserFullName,
                RoleId = dto.RoleId,
                BranchId = dto.BranchId,
                Active = true,
                FirstTimeLogin = true,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.UserSecurities.Add(userSec);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User created successfully", id = userSec.Id });
        }

        // PUT /api/users/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto)
        {
            var user = await _context.UserSecurities.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            var isCompanyAdmin = ClaimsHelper.IsCompanyAdmin(User);
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            if (!isCompanyAdmin && user.BranchId != callerBranchId)
                return Forbid();

            user.UserFullName = dto.UserFullName;
            user.RoleId = dto.RoleId;
            user.Active = dto.Active;
            if (isCompanyAdmin) user.BranchId = dto.BranchId;
            user.LastModifiedBy = User.Identity?.Name ?? "system";
            user.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "User updated" });
        }

        // POST /api/users/{id}/reset-password
        [HttpPost("{id}/reset-password")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] SetPasswordDto dto)
        {
            var user = await _context.UserSecurities.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            var callerBranchId = ClaimsHelper.GetBranchId(User);
            if (callerBranchId.HasValue && user.BranchId != callerBranchId)
                return Forbid();

            if (string.IsNullOrWhiteSpace(dto?.NewPassword) || dto.NewPassword.Length < 6)
                return BadRequest(new { message = "Password must be at least 6 characters." });

            user.Password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            user.FirstTimeLogin = true;
            user.LastModifiedBy = User.Identity?.Name ?? "system";
            user.LastModifiedDateTime = DateTime.Now;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Password updated successfully." });
        }

        // DELETE /api/users/{id} — permanent hard delete (Company Admin only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Company Admin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.UserSecurities.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found" });

            // Prevent self-deletion
            var callerId = int.TryParse(User.FindFirst("UserSecurityId")?.Value, out var cid) ? cid : 0;
            if (user.Id == callerId)
                return BadRequest(new { message = "You cannot delete your own account." });

            // Unlink from Employee and Instructor records (no FK constraint, but keeps data clean)
            var linkedEmployees = await _context.Employees.Where(e => e.UserId == id).ToListAsync();
            foreach (var emp in linkedEmployees) emp.UserId = null;

            var linkedInstructors = await _context.Instructors.Where(i => i.UserId == id).ToListAsync();
            foreach (var inst in linkedInstructors) inst.UserId = null;

            // Hard delete — remove permanently
            _context.UserSecurities.Remove(user);
            await _context.SaveChangesAsync();
            return Ok(new { message = "User permanently deleted." });
        }

        // GET /api/users/seed-roles — seed the 4 system roles
        [HttpPost("seed-roles")]
        [AllowAnonymous]
        public async Task<IActionResult> SeedRoles()
        {
            var roleNames = new[] { "Company Admin", "Branch Admin", "Staff", "Instructor" };
            foreach (var name in roleNames)
            {
                if (!await _context.Roles.AnyAsync(r => r.RoleName == name))
                {
                    _context.Roles.Add(new Role { RoleName = name, Description = name, Active = true, CreatedDateTime = DateTime.Now });
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new { message = "Roles seeded" });
        }
    }
}
