using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.DTOs;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/exam")]
    [Authorize]
    public class ExamResultController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public ExamResultController(DsmsDbContext context) => _context = context;

        // GET /api/exam/student/{studentId} — get all exam results for a student
        [HttpGet("student/{studentId}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var student = await _context.Students.FindAsync(studentId);
            if (student == null) return NotFound();

            // Branch-scoped check
            if (callerBranchId.HasValue && student.BranchId != callerBranchId)
                return Forbid();

            var sprs = await _context.StudentPackageRegistrations
                .Where(s => s.StudentId == studentId)
                .Select(s => new {
                    s.Id, s.ExamStatus, s.ExamDate, s.ExamAttempts,
                    s.IsRecommendForTrial, PackageName = s.PackageHeader.PackageName
                })
                .ToListAsync();

            return Ok(sprs);
        }

        // PUT /api/exam/update — Branch Admin updates exam result
        [HttpPut("update")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> UpdateResult([FromBody] ExamResultUpdateDto dto)
        {
            var spr = await _context.StudentPackageRegistrations
                .Include(s => s.Student)
                .FirstOrDefaultAsync(s => s.Id == dto.StudentPackageRegistrationId);

            if (spr == null) return NotFound(new { message = "Registration not found" });

            var callerBranchId = ClaimsHelper.GetBranchId(User);
            if (callerBranchId.HasValue && spr.Student.BranchId != callerBranchId)
                return Forbid();

            spr.ExamStatus = dto.ExamStatus;
            spr.ExamAttempts += 1;
            if (!string.IsNullOrEmpty(dto.ExamDate))
                spr.ExamDate = DateTime.Parse(dto.ExamDate);
            spr.LastModifiedBy = User.Identity?.Name ?? "system";
            spr.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Exam result updated: {dto.ExamStatus}" });
        }

        // GET /api/exam/pending-practical — students approved for practical test
        [HttpGet("pending-practical")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> GetPendingPractical()
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);

            var query = _context.StudentPackageRegistrations
                .Include(s => s.Student).ThenInclude(st => st.Branch)
                .Where(s => s.IsRecommendForTrial == true && s.Active == true);

            if (callerBranchId.HasValue)
                query = query.Where(s => s.Student.BranchId == callerBranchId);

            var results = await query.Select(s => new {
                s.Id,
                s.Student.StudentName,
                s.Student.Nic,
                BranchName = s.Student.Branch.Name,
                s.ExamStatus,
                s.ExamAttempts,
                s.IsRecommendForTrial
            }).ToListAsync();

            return Ok(results);
        }
    }
}
