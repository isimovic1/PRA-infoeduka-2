using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Security; // PasswordHasher

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // Admin-only CRUD
    [Authorize(Policy = "NotFirstLogin")]
    public class UsersController : ControllerBase
    {
        private readonly Infoeduka2Context _db;
        private readonly IMapper _mapper;

        public UsersController(Infoeduka2Context db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // GET /api/users?search=&role=&page=1&pageSize=20
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] byte? role,
                                               [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;

            var q = _db.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                q = q.Where(u => u.FirstName.Contains(s) || u.LastName.Contains(s) || u.Email.Contains(s));
            }
            if (role.HasValue)
            {
                q = q.Where(u => u.Role == role.Value);
            }

            var total = await q.CountAsync();
            var items = await q
                .OrderBy(u => u.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider) // server-side projection
                .ToListAsync();

            return Ok(new { total, page, pageSize, items });
        }

        // GET /api/users/5
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOne(int id)
        {
            var dto = await _db.Users.AsNoTracking()
                .Where(u => u.Id == id)
                .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            return dto is null ? NotFound() : Ok(dto);
        }

        // POST /api/users  (Admin creates a user)
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] UserCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var email = dto.Email.Trim();
            if (await _db.Users.AnyAsync(u => u.Email == email))
                return BadRequest("A user with this email already exists.");

            if (dto.GroupId != null && !await _db.Groups.AnyAsync(g => g.Id == dto.GroupId))
                return BadRequest("GroupId not found.");

            var user = _mapper.Map<User>(dto);
            user.Email = email;
            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.PasswordHash = PasswordHasher.Hash(dto.Password); // set hash

            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            var outDto = _mapper.Map<UserDto>(user);
            return CreatedAtAction(nameof(GetOne), new { id = user.Id }, outDto);
        }

        // PUT /api/users/5 (no email/password change here)
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return NotFound();

            if (dto.GroupId != null && !await _db.Groups.AnyAsync(g => g.Id == dto.GroupId))
                return BadRequest("GroupId not found.");

            _mapper.Map(dto, user);
            user.FirstName = user.FirstName.Trim();
            user.LastName = user.LastName.Trim();

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/users/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return NotFound();

            _db.Users.Remove(user);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("Cannot delete this user due to related records.");
            }
            return NoContent();
        }


        //dodavanje u grupe (usera)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}/group")]
        public async Task<IActionResult> SetGroup(int id, [FromBody] AddUserToGroupDTO dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user is null) return NotFound();

            
            if (user.Role == 2 && dto.GroupId != null)
                return BadRequest("Admin must not have GroupId.");
            if (user.Role == 0 && dto.GroupId == null)
                return BadRequest("Student must have GroupId.");
            if (dto.GroupId != null && !await _db.Groups.AnyAsync(g => g.Id == dto.GroupId))
                return BadRequest("GroupId not found.");

            user.GroupId = dto.GroupId;
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
