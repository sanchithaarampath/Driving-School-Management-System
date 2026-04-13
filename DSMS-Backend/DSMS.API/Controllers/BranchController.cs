using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSMS.API.Data;
using DSMS.API.Models;
using DSMS.API.Helpers;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BranchController : ControllerBase
    {
        private readonly DsmsDbContext _context;

        public BranchController(DsmsDbContext context) => _context = context;

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var branches = await _context.Branches.ToListAsync();
            return Ok(branches);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound(new { message = "Branch not found" });

            // Branch-scoped users can only see their own branch
            var callerBranchId = ClaimsHelper.GetBranchId(User);
            if (callerBranchId.HasValue && callerBranchId != id)
                return Forbid();

            // Enrich with stats
            var studentCount = await _context.Students.CountAsync(s => s.BranchId == id && s.Active == true);
            var staffCount = await _context.UserSecurities.CountAsync(u => u.BranchId == id && u.Active == true);

            return Ok(new { branch.Id, branch.Name, branch.Code, branch.Address, branch.Phone, studentCount, staffCount });
        }

        [HttpPost]
        [Authorize(Roles = "Company Admin,Admin")]
        public async Task<IActionResult> Create([FromBody] Branch dto)
        {
            _context.Branches.Add(dto);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Branch created", id = dto.Id });
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Company Admin,Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] Branch dto)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound(new { message = "Branch not found" });

            branch.Name = dto.Name;
            branch.Code = dto.Code;
            branch.Address = dto.Address;
            branch.Phone = dto.Phone;
            await _context.SaveChangesAsync();
            return Ok(new { message = "Branch updated" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Company Admin,Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var branch = await _context.Branches.FindAsync(id);
            if (branch == null) return NotFound(new { message = "Branch not found" });
            _context.Branches.Remove(branch);
            await _context.SaveChangesAsync();
            return Ok(new { message = "Branch deleted" });
        }

        // GET /api/branch/{id}/stats - revenue, students, staff for branch
        [HttpGet("{id}/stats")]
        public async Task<IActionResult> GetStats(int id)
        {
            var callerBranchId = ClaimsHelper.GetBranchId(User);
            if (callerBranchId.HasValue && callerBranchId != id)
                return Forbid();

            var students = await _context.Students.CountAsync(s => s.BranchId == id && s.Active == true);
            var revenue = await _context.Payments
                .Where(p => _context.Students.Any(s => s.Id == p.StudentId && s.BranchId == id))
                .SumAsync(p => p.Amount);
            var pending = await _context.Bills
                .Where(b => _context.Students.Any(s => s.Id == b.StudentId && s.BranchId == id) && b.BalanceAmount > 0)
                .SumAsync(b => b.BalanceAmount);

            return Ok(new { branchId = id, students, totalRevenue = revenue, pendingAmount = pending });
        }
    }
}
