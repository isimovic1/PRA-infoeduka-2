// Path: WebAPI/Controllers/AuthController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.Models;     // your scaffolded EF entities (User, Group, Infoeduka2Context)
using WebAPI.DTOs;       // LoginDTO, RegisterDto, UserDto
using WebAPI.Security;   // PasswordHasher, JwtTokenProvider
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly Infoeduka2Context _db;   // <-- adjust if your DbContext class name differs
        private readonly IConfiguration _cfg;

        public AuthController(Infoeduka2Context db, IConfiguration cfg)
        {
            _db = db;
            _cfg = cfg;
        }

        // POST: /api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            // Step: validate DTO
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Step: enforce role↔group rule (mirrors DB check)
            if (dto.Role == 2 && dto.GroupId != null)
                return BadRequest("Admin must not have a group.");
            if (dto.Role == 0 && dto.GroupId == null)
                return BadRequest("Student must have GroupId.");

            // Step: unique email check
            var email = dto.Email.Trim();
            var exists = await _db.Users.AnyAsync(u => u.Email == email);
            if (exists) return BadRequest("A user with this email already exists.");

            // Step: optional group existence check
            if (dto.GroupId != null && !await _db.Groups.AnyAsync(g => g.Id == dto.GroupId))
                return BadRequest("GroupId not found.");

            // Step: create entity (hash password, flag first login)
            var user = new User
            {
                Email = email,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Role = dto.Role,
                GroupId = dto.GroupId,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                IsFirstLogin = true
            };

            _db.Users.Add(user);
            await _db.SaveChangesAsync();


            ///_____AUTOMAPER Implementiraj
            //var userDto = _mapper.Map<UserDto>(user);
            //return CreatedAtAction(nameof(Register), new { id = user.Id }, userDto);

            // Step: return minimal safe payload (no password)
            var roleName = user.Role switch { 2 => "Admin", 1 => "Professor", 0 => "Student", _ => "Student" };
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                RoleName = roleName,
                GroupId = user.GroupId,
                IsFirstLogin = user.IsFirstLogin
            };

            return CreatedAtAction(nameof(Register), new { id = user.Id }, userDto);
        }

        // POST: /api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO dto)
        {
            // Step: validate DTO
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Step: fetch user by email
            var email = dto.Email.Trim();
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null) return Unauthorized("Invalid email or password.");

            // Step: verify password
            if (!PasswordHasher.Verify(dto.Password, user.PasswordHash))
                return Unauthorized("Invalid email or password.");

            // Step: prepare role claim + JWT config
            var roleName = user.Role switch { 2 => "Admin", 1 => "Professor", 0 => "Student", _ => "Student" };
            var key = _cfg["Jwt:Key"]!;
            var issuer = _cfg["Jwt:Issuer"]!;
            var audience = _cfg["Jwt:Audience"]!;
            var expiresMinutes = int.TryParse(_cfg["Jwt:ExpiresMinutes"] ?? _cfg["Jwt:ExpireMinutes"], out var m) ? m : 60;

            // Step: issue token
            var token = JwtTokenProvider.CreateToken(
                secureKey: key,
                expirationMinutes: expiresMinutes,
                subject: user.Email,
                role: roleName,
                issuer: issuer,
                audience: audience
            );

            // Step: return token + essentials (keep it similar to your example)
            return Ok(new
            {
                token,
                email = user.Email,
                role = roleName
            });
        }


        ///Za sve (prijavljene) usere-e change pass
        [Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var email = User?.Identity?.Name; // set by your JWT (ClaimTypes.Name)
            if (string.IsNullOrWhiteSpace(email)) return Unauthorized();

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null) return Unauthorized();

            if (!PasswordHasher.Verify(dto.OldPassword, user.PasswordHash))
                return BadRequest("Old password is incorrect.");

            if (dto.OldPassword == dto.NewPassword)
                return BadRequest("New password must differ from the old password.");

            user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
            user.IsFirstLogin = false; // optionally flip this
            await _db.SaveChangesAsync();

            return NoContent();
        }



        [Authorize]
        [HttpGet("me")]
        [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            // Try to get the authenticated user's email from common claim types
            string? email =
                User.FindFirstValue(ClaimTypes.Name) ??            // if your JwtTokenProvider set Name = email
                User.FindFirstValue(ClaimTypes.Email) ??           // sometimes tokens carry Email explicitly
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??  // fallback
                User.FindFirstValue("sub");                        // JWT subject

            if (string.IsNullOrWhiteSpace(email))
                return Unauthorized();

            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null)
                return Unauthorized();

            // Map numeric role to friendly role name (matches your Login/Register logic)
            var roleName = user.Role switch
            {
                2 => "Admin",
                1 => "Professor",
                0 => "Student",
                _ => "Student"
            };

            var dto = new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role,
                RoleName = roleName,
                GroupId = user.GroupId,
                IsFirstLogin = user.IsFirstLogin
            };

            return Ok(dto);
        }



        //ZA ADMINA DA PO ID-u Korisniku reset-a pass
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/reset-password")]
        public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return NotFound();

            user.PasswordHash = PasswordHasher.Hash(dto.NewPassword);
            user.IsFirstLogin = true; // optional: force password change on next login
            await _db.SaveChangesAsync();

            return NoContent();
        }
    }
}
