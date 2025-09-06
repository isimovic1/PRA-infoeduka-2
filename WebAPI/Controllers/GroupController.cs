using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;
using WebAPI.Models;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "NotFirstLogin")]
    public sealed class GroupsController : ControllerBase
    {
        private readonly Infoeduka2Context _db;
        private readonly IMapper _mapper;

        public GroupsController(Infoeduka2Context db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        /// <summary>List all groups (optional search by name: ?q=)</summary>
        [HttpGet]
        [Authorize] // require any logged-in user; remove if you want it public
        public async Task<ActionResult<IEnumerable<GroupDto>>> GetAll([FromQuery] string? q)
        {
            var query = _db.Groups.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(q))
            {
                var needle = q.Trim();
                query = query.Where(g => g.Name.Contains(needle));
            }

            var groups = await query.OrderBy(g => g.Name).ToListAsync();
            return Ok(_mapper.Map<List<GroupDto>>(groups));
        }

        /// <summary>Get a single group</summary>
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<GroupDto>> Get(int id)
        {
            var g = await _db.Groups.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (g is null) return NotFound();

            return Ok(_mapper.Map<GroupDto>(g));
        }

        /// <summary>Create a new group (Admin only)</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<GroupDto>> Create([FromBody] GroupCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var name = dto.Name.Trim();
            var exists = await _db.Groups.AnyAsync(g => g.Name == name);
            if (exists) return Conflict("Group name already exists.");

            var entity = _mapper.Map<Group>(dto);
            _db.Groups.Add(entity);
            await _db.SaveChangesAsync();

            var read = _mapper.Map<GroupDto>(entity);
            return CreatedAtAction(nameof(Get), new { id = entity.Id }, read);
        }

        /// <summary>Update group name (Admin only)</summary>
        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] GroupUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = await _db.Groups.FirstOrDefaultAsync(g => g.Id == id);
            if (entity is null) return NotFound();

            var newName = dto.Name.Trim();
            var taken = await _db.Groups.AnyAsync(g => g.Id != id && g.Name == newName);
            if (taken) return Conflict("Another group with this name already exists.");

            entity.Name = newName;
            await _db.SaveChangesAsync();
            return NoContent();
        }

        /// <summary>Delete a group (Admin only). Block if users still belong to it.</summary>
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Groups.FirstOrDefaultAsync(g => g.Id == id);
            if (entity is null) return NotFound();

            // Protect referential usage: don't delete if users still reference this group
            var hasUsers = await _db.Users.AnyAsync(u => u.GroupId == id);
            if (hasUsers)
                return BadRequest("Cannot delete this group because some users belong to it. Move them first.");

            _db.Groups.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET: /api/groups/5/members  (students in the group)
        [HttpGet("{id:int}/members")]
        [Authorize]
        public async Task<IActionResult> GetMembers(int id)
        {
            var exists = await _db.Groups.AnyAsync(g => g.Id == id);
            if (!exists) return NotFound();

            var members = await _db.Users.AsNoTracking()
                .Where(u => u.GroupId == id && u.Role == 0)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    Email = u.Email,
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    Role = u.Role,
                    RoleName = "Student",
                    GroupId = u.GroupId,
                    IsFirstLogin = u.IsFirstLogin
                })
                .OrderBy(u => u.LastName).ThenBy(u => u.FirstName)
                .ToListAsync();
            return Ok(members);
        }
    }
}
