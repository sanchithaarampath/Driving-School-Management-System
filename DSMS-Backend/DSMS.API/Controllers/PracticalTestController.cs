using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/practical-test")]
    [Authorize]
    public class PracticalTestController : ControllerBase
    {
        private readonly DsmsDbContext _context;
        public PracticalTestController(DsmsDbContext context) => _context = context;

        // GET /api/practical-test/student/{studentId}
        // All attempts for a student across all SPRs
        [HttpGet("student/{studentId:int}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var attempts = await _context.StudentPracticalTestAttempts
                .Include(a => a.StudentClassProgress)
                    .ThenInclude(cp => cp.StudentPackageRegistration)
                        .ThenInclude(spr => spr.PackageHeader)
                .Where(a => a.StudentClassProgress.StudentPackageRegistration.StudentId == studentId)
                .OrderByDescending(a => a.TestDate)
                .Select(a => new {
                    a.Id,
                    a.AttemptNumber,
                    TestDate    = a.TestDate.ToString("yyyy-MM-dd"),
                    a.Result,
                    a.Remarks,
                    PackageName = a.StudentClassProgress.StudentPackageRegistration.PackageHeader.PackageName,
                    SprId       = a.StudentClassProgress.StudentPackageRegistrationId
                })
                .ToListAsync();

            return Ok(attempts);
        }

        // GET /api/practical-test/spr/{sprId}
        // All attempts for a specific SPR
        [HttpGet("spr/{sprId:int}")]
        public async Task<IActionResult> GetBySpr(int sprId)
        {
            var attempts = await _context.StudentPracticalTestAttempts
                .Include(a => a.StudentClassProgress)
                .Where(a => a.StudentClassProgress.StudentPackageRegistrationId == sprId)
                .OrderByDescending(a => a.TestDate)
                .Select(a => new {
                    a.Id,
                    a.AttemptNumber,
                    TestDate = a.TestDate.ToString("yyyy-MM-dd"),
                    a.Result,
                    a.Remarks
                })
                .ToListAsync();

            return Ok(attempts);
        }

        // POST /api/practical-test/attempt
        // Record a new practical test attempt
        [HttpPost("attempt")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> RecordAttempt([FromBody] PracticalTestAttemptDto dto)
        {
            // Find active SPR for this student
            var spr = await _context.StudentPackageRegistrations
                .FirstOrDefaultAsync(s => s.StudentId == dto.StudentId && s.Active == true);

            if (spr == null)
                return BadRequest(new { message = "Student has no active package registration. Enrol them first." });

            // Find or auto-create a StudentClassProgress record as the required bridge
            var progress = await _context.StudentClassProgresses
                .FirstOrDefaultAsync(cp => cp.StudentPackageRegistrationId == spr.Id);

            if (progress == null)
            {
                progress = new StudentClassProgress
                {
                    StudentPackageRegistrationId = spr.Id,
                    PackageHeaderId = spr.PackageHeaderId,
                    IsTrialFaced    = true,
                    TrialAttempt    = 0,
                    Status          = 1,
                    IsDeclined      = false,
                    SessionDate     = DateTime.Now
                };
                _context.StudentClassProgresses.Add(progress);
                await _context.SaveChangesAsync();
            }

            // Count existing attempts to auto-number
            var attemptCount = await _context.StudentPracticalTestAttempts
                .CountAsync(a => a.StudentClassProgressId == progress.Id);

            var attempt = new StudentPracticalTestAttempt
            {
                StudentClassProgressId = progress.Id,
                AttemptNumber          = attemptCount + 1,
                TestDate               = DateTime.Parse(dto.TestDate),
                Result                 = dto.Result,
                Remarks                = dto.Remarks
            };

            _context.StudentPracticalTestAttempts.Add(attempt);

            // Sync result back to the SPR's ExamStatus and increment attempts
            spr.ExamStatus  = dto.Result;
            spr.ExamAttempts = attemptCount + 1;
            spr.ExamDate    = DateTime.Parse(dto.TestDate);
            spr.LastModifiedBy = User.Identity?.Name ?? "system";
            spr.LastModifiedDateTime = DateTime.Now;

            // If passed — record the license number if provided
            if (dto.Result == "Pass" && !string.IsNullOrWhiteSpace(dto.LicenseNumber))
            {
                var student = await _context.Students.FindAsync(dto.StudentId);
                if (student != null)
                {
                    student.IssuedLicenseNo    = dto.LicenseNumber;
                    student.LicenseIssuedDate  = DateTime.Parse(dto.TestDate);
                    student.LastModifiedBy     = User.Identity?.Name ?? "system";
                    student.LastModifiedDateTime = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new {
                message       = $"Attempt {attempt.AttemptNumber} recorded — {dto.Result}",
                id            = attempt.Id,
                attemptNumber = attempt.AttemptNumber
            });
        }

        // DELETE /api/practical-test/attempt/{id}
        [HttpDelete("attempt/{id:int}")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> DeleteAttempt(int id)
        {
            var attempt = await _context.StudentPracticalTestAttempts.FindAsync(id);
            if (attempt == null) return NotFound(new { message = "Attempt not found." });

            _context.StudentPracticalTestAttempts.Remove(attempt);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Attempt removed." });
        }
    }

    public class PracticalTestAttemptDto
    {
        public int    StudentId     { get; set; }
        public string TestDate      { get; set; } = string.Empty;
        public string Result        { get; set; } = string.Empty;   // "Pass" | "Fail"
        public string? Remarks      { get; set; }
        public string? LicenseNumber { get; set; }  // Only for Pass
    }
}
