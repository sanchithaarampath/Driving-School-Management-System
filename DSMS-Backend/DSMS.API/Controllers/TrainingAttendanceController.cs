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

        // GET /api/attendance/students-progress — student progress for the caller
        // For Instructors : returns students who have training sessions with them
        // For Admin/Staff  : returns all students with active SPRs in the branch
        [HttpGet("students-progress")]
        public async Task<IActionResult> GetStudentsProgress()
        {
            var branchId = ClaimsHelper.GetBranchId(User);
            var role           = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? "";
            // "UserSecurityId" = UserSecurity.Id (PK) — this is what Instructor.UserId links to
            var userSecurityId = int.TryParse(User.FindFirst("UserSecurityId")?.Value, out var uid) ? uid : 0;

            // ── Instructor path: build from TrainingSession records ──────────────────
            if (role == "Instructor")
            {
                var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userSecurityId);
                if (instructor == null) return Ok(new List<object>());

                var sessions = await _context.TrainingSessions
                    .Include(t => t.Student).ThenInclude(s => s.Branch)
                    .Where(t => t.InstructorId == instructor.Id)
                    .ToListAsync();

                // Distinct students
                var students = sessions
                    .GroupBy(t => t.StudentId)
                    .Select(g => g.First().Student)
                    .Where(s => s.Active == true)
                    .ToList();

                var result2 = new List<object>();
                foreach (var student in students)
                {
                    var studentSessions = sessions.Where(s => s.StudentId == student.Id).ToList();
                    var completedSessions = studentSessions.Count(s => s.Status == "Completed");
                    var totalHours = studentSessions.Where(s => s.Status == "Completed")
                                        .Sum(s => (decimal)(s.HoursLogged ?? s.DurationHours));
                    var scheduledSessions = studentSessions.Count(s => s.Status == "Scheduled");

                    // Try to get SPR if it exists
                    var spr = await _context.StudentPackageRegistrations
                        .Include(s => s.PackageHeader)
                        .FirstOrDefaultAsync(s => s.StudentId == student.Id && s.Active == true);

                    result2.Add(new {
                        sprId               = spr?.Id ?? 0,
                        studentId           = student.Id,
                        studentName         = student.StudentName,
                        nic                 = student.Nic,
                        phone               = student.PhoneNumber,
                        branchName          = student.Branch?.Name ?? "",
                        packageName         = spr?.PackageHeader?.PackageName ?? student.PackageType ?? "—",
                        attendanceDays      = completedSessions,     // completed practical sessions as "days"
                        totalTrainingHours  = (int)(spr?.TotalTrainingHours ?? totalHours),
                        completedHours      = totalHours,
                        scheduledSessions   = scheduledSessions,
                        totalSessions       = studentSessions.Count,
                        isRecommendForTrial = spr?.IsRecommendForTrial ?? false,
                        examStatus          = spr?.ExamStatus ?? "—"
                    });
                }

                return Ok(result2);
            }

            // ── Admin/Staff path: SPR-based (existing logic) ──────────────────────────
            var sprQuery = _context.StudentPackageRegistrations
                .Include(s => s.Student).ThenInclude(st => st.Branch)
                .Include(s => s.PackageHeader)
                .Where(s => s.Active == true && s.Student.Active == true);

            if (branchId.HasValue)
                sprQuery = sprQuery.Where(s => s.Student.BranchId == branchId);

            var sprs = await sprQuery.ToListAsync();

            // If no SPRs exist, fall back to all active students in branch
            if (!sprs.Any())
            {
                var studentQuery = _context.Students
                    .Include(s => s.Branch)
                    .Where(s => s.Active == true);
                if (branchId.HasValue)
                    studentQuery = studentQuery.Where(s => s.BranchId == branchId);

                var allStudents = await studentQuery.ToListAsync();
                var fallbackResult = new List<object>();
                foreach (var student in allStudents)
                {
                    var sessionCount = await _context.TrainingSessions
                        .CountAsync(t => t.StudentId == student.Id && t.Status == "Completed");
                    fallbackResult.Add(new {
                        sprId               = 0,
                        studentId           = student.Id,
                        studentName         = student.StudentName,
                        nic                 = student.Nic,
                        phone               = student.PhoneNumber,
                        branchName          = student.Branch?.Name ?? "",
                        packageName         = student.PackageType ?? "—",
                        attendanceDays      = sessionCount,
                        totalTrainingHours  = 0,
                        completedHours      = (decimal)0,
                        scheduledSessions   = 0,
                        totalSessions       = 0,
                        isRecommendForTrial = false,
                        examStatus          = "—"
                    });
                }
                return Ok(fallbackResult);
            }

            var mainResult = new List<object>();
            foreach (var spr in sprs)
            {
                var attendanceCount = await _context.TrainingAttendances
                    .CountAsync(t => t.StudentPackageRegistrationId == spr.Id);
                mainResult.Add(new {
                    sprId               = spr.Id,
                    studentId           = spr.StudentId,
                    studentName         = spr.Student.StudentName,
                    nic                 = spr.Student.Nic,
                    phone               = spr.Student.PhoneNumber,
                    branchName          = spr.Student.Branch.Name,
                    packageName         = spr.PackageHeader?.PackageName ?? "—",
                    attendanceDays      = attendanceCount,
                    totalTrainingHours  = spr.TotalTrainingHours,
                    completedHours      = (decimal)0,
                    scheduledSessions   = 0,
                    totalSessions       = 0,
                    isRecommendForTrial = spr.IsRecommendForTrial,
                    examStatus          = spr.ExamStatus
                });
            }

            return Ok(mainResult);
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

        // POST /api/attendance/enroll/{studentId}
        // Auto-creates an SPR for the student using the first active Package (training package).
        // NOTE: Package and CoursePackage are separate tables.
        //   Student.CoursePackageId → CoursePackage (billing/vehicle-class package)
        //   SPR.PackageHeaderId     → Package       (training package with TotTrainingHours)
        [HttpPost("enroll/{studentId:int}")]
        [Authorize(Roles = "Instructor,Branch Admin,Company Admin,Admin,Staff,OfficeStaff")]
        public async Task<IActionResult> EnrollStudent(int studentId)
        {
            var student = await _context.Students
                .Include(s => s.CoursePackage)
                .FirstOrDefaultAsync(s => s.Id == studentId && s.Active == true);

            if (student == null)
                return NotFound(new { message = "Student not found." });

            // Return existing active SPR if already enrolled
            var existing = await _context.StudentPackageRegistrations
                .Include(s => s.PackageHeader)
                .FirstOrDefaultAsync(s => s.StudentId == studentId && s.Active == true);

            if (existing != null)
                return Ok(new {
                    sprId              = existing.Id,
                    packageName        = existing.PackageHeader?.PackageName ?? existing.Student?.StudentName ?? "—",
                    totalTrainingDays  = existing.TotalTrainingHours,
                    message            = "Already enrolled"
                });

            // Resolve training days from the student's linked CoursePackage.TrainingDays
            // CoursePackage is the package admins create with vehicle classes & training days
            int trainingDays = 15; // safe default
            string packageName = "Standard Training";

            if (student.CoursePackage != null)
            {
                trainingDays = student.CoursePackage.TrainingDays > 0 ? student.CoursePackage.TrainingDays : 15;
                packageName  = student.CoursePackage.PackageName;
            }

            // SPR.PackageHeaderId is required (FK to Package table).
            // Find or create a default Package record for the SPR link.
            var pkg = await _context.Packages
                .Where(p => p.Active == true)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();

            if (pkg == null)
            {
                // Auto-create a default Package row so the FK is satisfied
                pkg = new Package
                {
                    PackageName          = "Default Training Package",
                    Description          = "Auto-created for training enrollment",
                    TotTrainingHours     = trainingDays,
                    TotLectureHours      = 0,
                    ChargePerExtraHour   = 0,
                    Rmvcharges           = 0,
                    DownPaymentAmount    = 0,
                    Price                = student.CoursePackage?.Price ?? 0,
                    MaxDiscount          = 0,
                    Active               = true,
                    CreatedBy            = User.Identity?.Name ?? "system",
                    CreatedDateTime      = DateTime.Now
                };
                _context.Packages.Add(pkg);
                await _context.SaveChangesAsync();
            }

            var spr = new StudentPackageRegistration
            {
                StudentId               = studentId,
                PackageHeaderId         = pkg.Id,
                TotalAmount             = student.CoursePackage?.Price ?? pkg.Price,
                DiscountAmount          = 0,
                BalanceAmount           = student.CoursePackage?.Price ?? pkg.Price,
                // Training days come from the CoursePackage the student is registered for
                TotalTrainingHours      = trainingDays,
                CompletedTrainingHours  = 0,
                TotalLectureHours       = 0,
                CompletedLectureHours   = 0,
                Active                  = true,
                CreatedBy               = User.Identity?.Name ?? "system",
                CreatedDateTime         = DateTime.Now
            };

            _context.StudentPackageRegistrations.Add(spr);
            await _context.SaveChangesAsync();

            return Ok(new {
                sprId              = spr.Id,
                packageName        = packageName,
                totalTrainingDays  = spr.TotalTrainingHours,
                message            = "Student enrolled successfully"
            });
        }
    }
}
