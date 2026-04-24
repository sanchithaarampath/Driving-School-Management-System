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

        // GET /api/attendance/students-progress — all students with SPR + attendance count
        [HttpGet("students-progress")]
        public async Task<IActionResult> GetStudentsProgress()
        {
            var branchId = ClaimsHelper.GetBranchId(User);

            var query = _context.StudentPackageRegistrations
                .Include(s => s.Student).ThenInclude(st => st.Branch)
                .Include(s => s.PackageHeader)
                .Where(s => s.Active == true && s.Student.Active == true);

            if (branchId.HasValue)
                query = query.Where(s => s.Student.BranchId == branchId);

            var sprs = await query.ToListAsync();

            var result = new List<object>();
            foreach (var spr in sprs)
            {
                var attendanceCount = await _context.TrainingAttendances
                    .CountAsync(t => t.StudentPackageRegistrationId == spr.Id);
                result.Add(new {
                    sprId             = spr.Id,
                    studentId         = spr.StudentId,
                    studentName       = spr.Student.StudentName,
                    nic               = spr.Student.Nic,
                    phone             = spr.Student.PhoneNumber,
                    branchName        = spr.Student.Branch.Name,
                    packageName       = spr.PackageHeader?.PackageName ?? "—",
                    attendanceDays    = attendanceCount,
                    totalTrainingHours= spr.TotalTrainingHours,
                    isRecommendForTrial = spr.IsRecommendForTrial,
                    examStatus        = spr.ExamStatus
                });
            }

            return Ok(result);
        }

        // GET /api/attendance/{sprId} — all attendance records for a specific SPR
        [HttpGet("{sprId:int}")]
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

        // GET /api/attendance/student/{studentId} — all attendance across all SPRs for a student
        [HttpGet("student/{studentId:int}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var records = await _context.TrainingAttendances
                .Where(t => t.StudentPackageRegistration.StudentId == studentId)
                .OrderBy(t => t.AttendanceDate)
                .Select(t => new {
                    t.Id, t.DayNumber, t.AttendanceDate, t.Notes,
                    t.IsReadyForPracticalTest, t.StudentPackageRegistrationId
                })
                .ToListAsync();
            return Ok(records);
        }

        // POST /api/attendance — instructor records attendance
        [HttpPost]
        [Authorize(Roles = "Instructor,Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Create([FromBody] TrainingAttendanceCreateDto dto)
        {
            // Prevent duplicate day entries
            var exists = await _context.TrainingAttendances
                .AnyAsync(t => t.StudentPackageRegistrationId == dto.StudentPackageRegistrationId
                            && t.DayNumber == dto.DayNumber);
            if (exists)
                return BadRequest(new { message = $"Day {dto.DayNumber} already recorded for this student." });

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

        // DELETE /api/attendance/{id} — remove an attendance record
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Instructor,Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var record = await _context.TrainingAttendances.FindAsync(id);
            if (record == null) return NotFound();
            _context.TrainingAttendances.Remove(record);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Record deleted" });
        }
    }
}
