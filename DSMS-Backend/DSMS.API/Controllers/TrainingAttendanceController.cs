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
    [Route("api/attendance")]
    [Authorize]
    public class TrainingAttendanceController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public TrainingAttendanceController(DsmsDbContext context) => _context = context;

        // GET /api/attendance/{sprId} — get all attendance for a student package registration
        [HttpGet("{sprId}")]
        public async Task<IActionResult> GetBySpr(int sprId)
        {
            var records = await _context.TrainingAttendances
                .Where(t => t.StudentPackageRegistrationId == sprId)
                .OrderBy(t => t.DayNumber)
                .Select(t => new {
                    t.Id, t.DayNumber, t.AttendanceDate, t.Notes,
                    t.IsReadyForPracticalTest, t.InstructorId, t.CreatedBy
                })
                .ToListAsync();
            return Ok(records);
        }

        // POST /api/attendance — instructor records attendance
        [HttpPost]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> Create([FromBody] TrainingAttendanceCreateDto dto)
        {
            var record = new TrainingAttendance
            {
                StudentPackageRegistrationId = dto.StudentPackageRegistrationId,
                InstructorId = dto.InstructorId,
                AttendanceDate = DateTime.Parse(dto.AttendanceDate),
                DayNumber = dto.DayNumber,
                Notes = dto.Notes,
                IsReadyForPracticalTest = dto.IsReadyForPracticalTest,
                CreatedBy = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.TrainingAttendances.Add(record);

            // If instructor marks student ready, update the SPR flag
            if (dto.IsReadyForPracticalTest)
            {
                var spr = await _context.StudentPackageRegistrations.FindAsync(dto.StudentPackageRegistrationId);
                if (spr != null)
                {
                    spr.IsRecommendForTrial = true;
                    spr.LastModifiedBy = User.Identity?.Name ?? "system";
                    spr.LastModifiedDateTime = DateTime.Now;
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Attendance recorded", id = record.Id });
        }

        // PUT /api/attendance/{id}/approve-practical — instructor marks ready for practical test
        [HttpPut("{id}/approve-practical")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> ApprovePractical(int id)
        {
            var record = await _context.TrainingAttendances.FindAsync(id);
            if (record == null) return NotFound();

            record.IsReadyForPracticalTest = true;

            var spr = await _context.StudentPackageRegistrations.FindAsync(record.StudentPackageRegistrationId);
            if (spr != null)
            {
                spr.IsRecommendForTrial = true;
                spr.LastModifiedBy = User.Identity?.Name ?? "system";
                spr.LastModifiedDateTime = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "Student approved for practical test" });
        }
    }
}
