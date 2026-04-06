using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DSMS.API.Data;
using DSMS.API.DTOs;

namespace DSMS.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly DsmsDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(DsmsDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            var user = await _context.UserSecurities
                .FirstOrDefaultAsync(u => u.UserName == request.UserName && u.Active == true);

            if (user == null)
                return Unauthorized(new { message = "Invalid username or password" });

            bool isValid;
            if (user.Password!.StartsWith("$") || user.Password.StartsWith("$"))
                isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            else
                isValid = user.Password == request.Password;

            if (!isValid)
                return Unauthorized(new { message = "Invalid username or password" });

            var role = await _context.Roles.FindAsync(user.RoleId);
            var token = GenerateJwtToken(user, role?.RoleName ?? "");

            return Ok(new LoginResponseDto
            {
                Token = token,
                UserFullName = user.UserFullName ?? "",
                UserName = user.UserName ?? "",
                Role = role?.RoleName ?? "",
                UserId = user.UserId ?? 0,
                RoleId = user.RoleId,
                BranchId = user.BranchId,
                FirstTimeLogin = user.FirstTimeLogin ?? false
            });
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
        {
            var user = await _context.UserSecurities
                .FirstOrDefaultAsync(u => u.UserId == request.UserId && u.Active == true);

            if (user == null)
                return NotFound(new { message = "User not found" });

            bool isValid;
            if (user.Password!.StartsWith("$") || user.Password.StartsWith("$"))
                isValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.Password);
            else
                isValid = user.Password == request.CurrentPassword;

            if (!isValid)
                return BadRequest(new { message = "Current password is incorrect" });

            user.Password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.FirstTimeLogin = false;
            user.LastModifiedBy = user.UserName;
            user.LastModifiedDateTime = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        [HttpPost("setup-admin")]
        public async Task<IActionResult> SetupAdmin()
        {
            var admin = await _context.UserSecurities
                .FirstOrDefaultAsync(u => u.UserName == "admin");

            if (admin == null)
                return NotFound(new { message = "Admin user not found" });

            admin.Password = BCrypt.Net.BCrypt.HashPassword("Admin@1234");
            admin.FirstTimeLogin = true;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Admin password set to Admin@1234" });
        }

        private string GenerateJwtToken(DSMS.API.Models.UserSecurity user, string roleName)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString() ?? ""),
                new Claim(ClaimTypes.Name, user.UserName ?? ""),
                new Claim(ClaimTypes.Role, roleName),
                new Claim("FullName", user.UserFullName ?? ""),
                new Claim("RoleId", user.RoleId.ToString()),
                new Claim("BranchId", user.BranchId?.ToString() ?? ""),
                new Claim("UserSecurityId", user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.Now.AddMinutes(
                    double.Parse(jwtSettings["ExpiryMinutes"]!)),
                signingCredentials: new SigningCredentials(
                    key, SecurityAlgorithms.HmacSha256)
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
