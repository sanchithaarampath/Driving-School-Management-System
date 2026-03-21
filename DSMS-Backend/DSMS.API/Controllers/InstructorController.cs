using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.DTOs;
using DSMS.API.Models;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/instructors")]
    [Authorize]
    public class InstructorController : ControllerBase
    {
        private readonly DsmsDbContext _context;
        public InstructorController(DsmsDbContext context) => _context = context;

        // GET /api/instructors — branch-scoped list
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var branchId = ClaimsHelper.GetBranchId(User);
            var q = _context.Instructors
                .Include(i => i.Branch)
                .AsQueryable();

            if (branchId.HasValue)
                q = q.Where(i => i.BranchId == branchId.Value);

            var list = await q
                .OrderBy(i => i.InstructorName)
                .Select(i => new {
                    i.Id, i.InstructorName, i.Nic, i.Phone, i.Email,
                    i.LicenseNo, i.BranchId, BranchName = i.Branch.Name,
                    i.Active, i.UserId, i.CreatedBy, i.CreatedDateTime
                }).ToListAsync();

            return Ok(list);
        }

        // GET /api/instructors/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var i = await _context.Instructors.Include(x => x.Branch).FirstOrDefaultAsync(x => x.Id == id);
            if (i == null) return NotFound();
            return Ok(new {
                i.Id, i.InstructorName, i.Nic, i.Phone, i.Email,
                i.LicenseNo, i.BranchId, BranchName = i.Branch.Name,
                i.Active, i.UserId
            });
        }

        // POST /api/instructors
        [HttpPost]
        [Authorize(Roles = "Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Create([FromBody] InstructorCreateDto dto)
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);
            var branchId = callerBranchId ?? dto.BranchId;

            if (branchId <= 0)
                return BadRequest(new { message = "Branch is required." });

            if (await _context.Instructors.AnyAsync(i => i.Nic == dto.Nic))
                return BadRequest(new { message = "An instructor with this NIC already exists." });

            var instructor = new Instructor
            {
                InstructorName  = dto.InstructorName,
                Nic             = dto.Nic,
                Phone           = dto.Phone,
                Email           = dto.Email,
                LicenseNo       = dto.LicenseNo,
                BranchId        = branchId,
                UserId          = dto.UserId,
                Active          = true,
                CreatedBy       = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.Instructors.Add(instructor);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Instructor created", id = instructor.Id });
        }

        // PUT /api/instructors/{id}
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] InstructorUpdateDto dto)
        {
            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null) return NotFound();

            var callerBranchId = ClaimsHelper.GetBranchId(User);
            if (callerBranchId.HasValue && instructor.BranchId != callerBranchId.Value)
                return Forbid();

            instructor.InstructorName       = dto.InstructorName;
            instructor.Phone                = dto.Phone;
            instructor.Email                = dto.Email;
            instructor.LicenseNo            = dto.LicenseNo;
            instructor.Active               = dto.Active;
            instructor.UserId               = dto.UserId;
            instructor.LastModifiedBy       = User.Identity?.Name ?? "system";
            instructor.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Instructor updated" });
        }

        // DELETE /api/instructors/{id} — soft delete
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var instructor = await _context.Instructors.FindAsync(id);
            if (instructor == null) return NotFound();

            instructor.Active               = false;
            instructor.LastModifiedBy       = User.Identity?.Name ?? "system";
            instructor.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Instructor deactivated" });
        }
    }
}
