using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.DTOs;
using DSMS.API.Models;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/course-package")]
    [Authorize]
    public class CoursePackageController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public CoursePackageController(DsmsDbContext context)
        {
            _context = context;
        }

        // GET all active packages
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var packages = await _context.CoursePackages
                .Where(p => p.Active)
                .OrderBy(p => p.CourseType)
                .ThenBy(p => p.PackageName)
                .ToListAsync();
            return Ok(packages);
        }

        // GET single
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var pkg = await _context.CoursePackages.FindAsync(id);
            if (pkg == null || !pkg.Active) return NotFound();
            return Ok(pkg);
        }

        // GET packages matching a student's vehicle class codes
        // e.g. GET /api/course-package/for-student?codes=A1,A,B_Auto
        [HttpGet("for-student")]
        public async Task<IActionResult> GetForStudent([FromQuery] string codes)
        {
            if (string.IsNullOrEmpty(codes)) return Ok(new List<object>());

            var studentCodes = codes.Split(',').Select(c => c.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var all = await _context.CoursePackages.Where(p => p.Active).ToListAsync();

            // Return packages where ALL the package's required codes are in the student's codes
            var matching = all.Where(p =>
            {
                var required = p.VehicleClassCodes.Split(',').Select(c => c.Trim());
                return required.All(r => studentCodes.Contains(r));
            }).ToList();

            return Ok(matching);
        }

        // POST create
        [HttpPost]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> Create([FromBody] CoursePackageDto dto)
        {
            var pkg = new CoursePackage
            {
                PackageName       = dto.PackageName,
                CourseType        = dto.CourseType,
                VehicleClassCodes = dto.VehicleClassCodes,
                Price             = dto.Price,
                MaxDiscount       = dto.MaxDiscount,
                Description       = dto.Description,
                Active            = true,
                CreatedBy         = User.Identity?.Name ?? "system",
                CreatedDateTime   = DateTime.Now
            };
            _context.CoursePackages.Add(pkg);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Package created", id = pkg.Id });
        }

        // PUT update
        [HttpPut("{id}")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] CoursePackageDto dto)
        {
            var pkg = await _context.CoursePackages.FindAsync(id);
            if (pkg == null) return NotFound();

            pkg.PackageName          = dto.PackageName;
            pkg.CourseType           = dto.CourseType;
            pkg.VehicleClassCodes    = dto.VehicleClassCodes;
            pkg.Price                = dto.Price;
            pkg.MaxDiscount          = dto.MaxDiscount;
            pkg.Description          = dto.Description;
            pkg.LastModifiedBy       = User.Identity?.Name ?? "system";
            pkg.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Package updated" });
        }

        // DELETE (soft)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Company Admin,Admin,Branch Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var pkg = await _context.CoursePackages.FindAsync(id);
            if (pkg == null) return NotFound();

            pkg.Active               = false;
            pkg.LastModifiedBy       = User.Identity?.Name ?? "system";
            pkg.LastModifiedDateTime = DateTime.Now;

            await _context.SaveChangesAsync();
            return Ok(new { message = "Package deleted" });
        }
    }
}
