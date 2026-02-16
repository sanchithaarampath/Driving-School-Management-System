using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LookupController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public LookupController(DsmsDbContext context)
        {
            _context = context;
        }

        [HttpGet("branches")]
        public async Task<IActionResult> GetBranches()
        {
            var branches = await _context.Branches.ToListAsync();
            return Ok(branches);
        }

        [HttpGet("roles")]
        public async Task<IActionResult> GetRoles()
        {
            var roles = await _context.Roles.Where(r => r.Active == true).ToListAsync();
            return Ok(roles);
        }

        [HttpGet("vehicle-types")]
        public async Task<IActionResult> GetVehicleTypes()
        {
            var types = await _context.VehicleTypes.Where(v => v.Active == true).ToListAsync();
            return Ok(types);
        }

        [HttpGet("vehicle-classes")]
        public async Task<IActionResult> GetVehicleClasses()
        {
            var classes = await _context.VehicleClasses.Where(v => v.Active == true).ToListAsync();
            return Ok(classes);
        }

        [HttpGet("packages")]
        public async Task<IActionResult> GetPackages()
        {
            var packages = await _context.Packages.Where(p => p.Active == true).ToListAsync();
            return Ok(packages);
        }

        [HttpGet("special-requirement-types")]
        public async Task<IActionResult> GetSpecialRequirementTypes()
        {
            var types = await _context.SpecialRequirementTypes.Where(s => s.Active == true).ToListAsync();
            return Ok(types);
        }

        [HttpGet("required-documents")]
        public async Task<IActionResult> GetRequiredDocuments()
        {
            var docs = await _context.RequiredDocuments.Where(d => d.Active == true).ToListAsync();
            return Ok(docs);
        }

        // Static vehicle class definitions used during student registration
        [HttpGet("vehicle-class-options")]
        public IActionResult GetVehicleClassOptions()
        {
            var options = new[]
            {
                new { Code = "A1", Label = "A1 - Light Bicycle" },
                new { Code = "A",  Label = "A - Bicycle" },
                new { Code = "B1", Label = "B1 - Three Wheeler" },
                new { Code = "B_Auto",   Label = "B (Auto) - Dual Purpose Auto Gear" },
                new { Code = "B_Manual", Label = "B (Manual) - Dual Purpose Manual" },
                new { Code = "C1", Label = "C1 - Light Lorry" },
                new { Code = "C",  Label = "C - Lorry" },
                new { Code = "CE", Label = "CE - Prime Mover" },
                new { Code = "D1", Label = "D1 - Light Bus" },
                new { Code = "D",  Label = "D - Bus" },
                new { Code = "G1", Label = "G1 - Auto-added for all applicants" },
                new { Code = "G",  Label = "G - Tractor with Trailer" },
                new { Code = "J",  Label = "J - Special Vehicles" }
            };
            return Ok(options);
        }
    }
}
