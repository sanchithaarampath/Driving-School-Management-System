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
    [Route("api/training-sessions")]
    [Authorize]
    public class TrainingSessionController : ControllerBase
    {
        private readonly DsmsDbContext _context;
        public TrainingSessionController(DsmsDbContext context) => _context = context;

        // ── GET /api/training-sessions  (branch-scoped, optional ?date=&instructorId=&status=) ──
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? date, [FromQuery] int? instructorId, [FromQuery] string? status)
        {
            var branchId = ClaimsHelper.GetBranchId(User);

            var q = _context.TrainingSessions
                .Include(t => t.Student)
                .Include(t => t.Instructor)
                .Include(t => t.Branch)
                .AsQueryable();

            if (branchId.HasValue)
                q = q.Where(t => t.BranchId == branchId.Value);

            if (!string.IsNullOrEmpty(date) && DateTime.TryParse(date, out var d))
                q = q.Where(t => t.ScheduledDate.Date == d.Date);

            if (instructorId.HasValue && instructorId > 0)
                q = q.Where(t => t.InstructorId == instructorId.Value);

            if (!string.IsNullOrEmpty(status))
                q = q.Where(t => t.Status == status);

            var list = await q.OrderBy(t => t.ScheduledDate).ThenBy(t => t.ScheduledTime).Select(t => new {
                t.Id, t.ScheduledDate, t.ScheduledTime, t.DurationHours, t.VehicleClass,
                t.Status, t.SessionNotes, t.HoursLogged, t.CancelReason, t.CreatedBy, t.CreatedDateTime,
                StudentId   = t.StudentId,   StudentName  = t.Student.StudentName,   StudentNic = t.Student.Nic, StudentPhone = t.Student.PhoneNumber,
                InstructorId= t.InstructorId, InstructorName= t.Instructor.InstructorName,
                BranchId    = t.BranchId,    BranchName   = t.Branch.Name
            }).ToListAsync();

            return Ok(list);
        }

        // ── GET /api/training-sessions/my-schedule  (instructor sees own sessions) ──
        [HttpGet("my-schedule")]
        [Authorize(Roles = "Instructor")]
        public async Task<IActionResult> MySchedule([FromQuery] string? status)
        {
            // "UserSecurityId" = UserSecurity.Id (PK) which is what Instructor.UserId links to
            var userSecurityId = int.Parse(User.FindFirst("UserSecurityId")?.Value ?? "0");
            var instructor = await _context.Instructors.FirstOrDefaultAsync(i => i.UserId == userSecurityId);
            if (instructor == null) return Ok(new List<object>());

            var q = _context.TrainingSessions
                .Include(t => t.Student)
                .Where(t => t.InstructorId == instructor.Id);

            if (!string.IsNullOrEmpty(status))
                q = q.Where(t => t.Status == status);

            var list = await q.OrderBy(t => t.ScheduledDate).ThenBy(t => t.ScheduledTime).Select(t => new {
                t.Id, t.ScheduledDate, t.ScheduledTime, t.DurationHours, t.VehicleClass,
                t.Status, t.SessionNotes, t.HoursLogged, t.CancelReason,
                StudentId = t.StudentId, StudentName = t.Student.StudentName,
                StudentNic = t.Student.Nic, StudentPhone = t.Student.PhoneNumber
            }).ToListAsync();

            return Ok(list);
        }

        // ── GET /api/training-sessions/student/{studentId} ──
        [HttpGet("student/{studentId:int}")]
        public async Task<IActionResult> GetByStudent(int studentId)
        {
            var list = await _context.TrainingSessions
                .Include(t => t.Instructor)
                .Where(t => t.StudentId == studentId)
                .OrderByDescending(t => t.ScheduledDate)
                .Select(t => new {
                    t.Id, t.ScheduledDate, t.ScheduledTime, t.DurationHours, t.VehicleClass,
                    t.Status, t.SessionNotes, t.HoursLogged, t.CancelReason,
                    InstructorName = t.Instructor.InstructorName
                }).ToListAsync();

            return Ok(list);
        }

        // ── GET /api/training-sessions/instructors  (list for branch, for dropdown) ──
        [HttpGet("instructors")]
        public async Task<IActionResult> GetInstructors()
        {
            var branchId = ClaimsHelper.GetBranchId(User);
            var q = _context.Instructors.Where(i => i.Active == true);
            if (branchId.HasValue) q = q.Where(i => i.BranchId == branchId.Value);
            var list = await q.Select(i => new { i.Id, i.InstructorName, i.Phone, i.BranchId }).ToListAsync();
            return Ok(list);
        }

        // ── GET /api/training-sessions/{id} ──
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var t = await _context.TrainingSessions
                .Include(x => x.Student).Include(x => x.Instructor).Include(x => x.Branch)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (t == null) return NotFound();
            return Ok(new {
                t.Id, t.ScheduledDate, t.ScheduledTime, t.DurationHours, t.VehicleClass,
                t.Status, t.SessionNotes, t.HoursLogged, t.CancelReason,
                t.StudentId, StudentName = t.Student.StudentName,
                t.InstructorId, InstructorName = t.Instructor.InstructorName,
                t.BranchId, BranchName = t.Branch.Name
            });
        }

        // ── POST /api/training-sessions  (staff / admin books a session) ──
        [HttpPost]
        [Authorize(Roles = "Staff,OfficeStaff,Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Create([FromBody] TrainingSessionCreateDto dto)
        {
            if (!DateTime.TryParse(dto.ScheduledDate, out var scheduledDate))
                return BadRequest(new { message = "Invalid date format." });

            // derive branch from caller or from instructor's branch
            var callerBranchId = ClaimsHelper.GetBranchId(User);
            var instructor = await _context.Instructors.FindAsync(dto.InstructorId);
            if (instructor == null) return BadRequest(new { message = "Instructor not found." });

            var branchId = callerBranchId ?? instructor.BranchId;

            var session = new TrainingSession
            {
                StudentId     = dto.StudentId,
                InstructorId  = dto.InstructorId,
                BranchId      = branchId,
                ScheduledDate = scheduledDate,
                ScheduledTime = dto.ScheduledTime,
                DurationHours = dto.DurationHours,
                VehicleClass  = dto.VehicleClass,
                Status        = "Scheduled",
                SessionNotes  = dto.Notes,
                CreatedBy     = User.Identity?.Name ?? "system",
                CreatedDateTime = DateTime.Now
            };

            _context.TrainingSessions.Add(session);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Session booked", id = session.Id });
        }

        // ── PUT /api/training-sessions/{id}  (reschedule — staff/admin only) ──
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Staff,OfficeStaff,Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] TrainingSessionUpdateDto dto)
        {
            var session = await _context.TrainingSessions.FindAsync(id);
            if (session == null) return NotFound();
            if (session.Status != "Scheduled")
                return BadRequest(new { message = "Only scheduled sessions can be updated." });

            if (!DateTime.TryParse(dto.ScheduledDate, out var scheduledDate))
                return BadRequest(new { message = "Invalid date format." });

            session.ScheduledDate    = scheduledDate;
            session.ScheduledTime    = dto.ScheduledTime;
            session.DurationHours    = dto.DurationHours;
            session.VehicleClass     = dto.VehicleClass;
            session.InstructorId     = dto.InstructorId;
            session.LastModifiedBy   = User.Identity?.Name ?? "system";
            session.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Session updated" });
        }

        // ── PUT /api/training-sessions/{id}/complete  (instructor marks done) ──
        [HttpPut("{id:int}/complete")]
        [Authorize(Roles = "Instructor,Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Complete(int id, [FromBody] TrainingSessionCompleteDto dto)
        {
            var session = await _context.TrainingSessions.FindAsync(id);
            if (session == null) return NotFound();
            if (session.Status == "Completed")
                return BadRequest(new { message = "Session already marked as completed." });

            session.Status        = "Completed";
            session.HoursLogged   = dto.HoursLogged;
            session.SessionNotes  = dto.SessionNotes;
            session.LastModifiedBy = User.Identity?.Name ?? "system";
            session.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Session marked complete", hoursLogged = dto.HoursLogged });
        }

        // ── PUT /api/training-sessions/{id}/cancel  (cancel or no-show) ──
        [HttpPut("{id:int}/cancel")]
        [Authorize(Roles = "Staff,OfficeStaff,Branch Admin,Company Admin,Admin,Instructor")]
        public async Task<IActionResult> Cancel(int id, [FromBody] TrainingSessionCancelDto dto)
        {
            var session = await _context.TrainingSessions.FindAsync(id);
            if (session == null) return NotFound();
            if (session.Status == "Completed")
                return BadRequest(new { message = "Cannot cancel a completed session." });

            session.Status       = dto.Status;
            session.CancelReason = dto.CancelReason;
            session.LastModifiedBy = User.Identity?.Name ?? "system";
            session.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Session marked as {dto.Status}" });
        }

        // ── DELETE /api/training-sessions/{id}  (admin only) ──
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Branch Admin,Company Admin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var session = await _context.TrainingSessions.FindAsync(id);
            if (session == null) return NotFound();
            _context.TrainingSessions.Remove(session);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Session deleted" });
        }

        // ── GET /api/training-sessions/summary/student/{studentId}  (total hours + counts) ──
        [HttpGet("summary/student/{studentId:int}")]
        public async Task<IActionResult> StudentSummary(int studentId)
        {
            var sessions = await _context.TrainingSessions
                .Where(t => t.StudentId == studentId)
                .ToListAsync();

            return Ok(new {
                totalSessions    = sessions.Count,
                completed        = sessions.Count(s => s.Status == "Completed"),
                scheduled        = sessions.Count(s => s.Status == "Scheduled"),
                cancelled        = sessions.Count(s => s.Status == "Cancelled" || s.Status == "NoShow"),
                totalHoursLogged = sessions.Where(s => s.Status == "Completed").Sum(s => s.HoursLogged ?? 0)
            });
        }
    }
}
