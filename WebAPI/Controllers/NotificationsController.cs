// Controllers/NotificationsController.cs
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;
using WebAPI.Models;

//KAKO FUNKsionria :
//Student uploads a file (FileAssetsController.Upload):
//---All teachers assigned to that course receive a notification (“Novi upload…”).

//---Student creates a submission (SubmissionsController.Create or SubmissionsController.Upload):
//---All teachers assigned to that course receive a notification (“Nova predaja…”), prompting them to review/grade.

//---Professor grades a submission (GradesController.Create):
//---The student who owns that submission receives a notification (“Ocjena…”).

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    [Authorize(Policy = "NotFirstLogin")]
    public class NotificationsController : ControllerBase
    {
        private readonly Infoeduka2Context _db;
        private readonly IMapper _mapper;

        public NotificationsController(Infoeduka2Context db, IMapper mapper)
        {
            _db = db; _mapper = mapper;
        }

        // GET /api/notifications/my?unreadOnly=false
        [HttpGet("my")]
        public async Task<IActionResult> My([FromQuery] bool unreadOnly = false)
        {
            var meEmail = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(meEmail)) return Unauthorized();

            var myId = await _db.Users.Where(u => u.Email == meEmail).Select(u => u.Id).FirstOrDefaultAsync();
            if (myId == 0) return Unauthorized();

            var q = _db.Notifications
                .AsNoTracking()
                .Include(n => n.FromUser)
                .Where(n => n.ToUserId == myId);

            if (unreadOnly) q = q.Where(n => !n.IsRead);

            var data = await q.OrderByDescending(n => n.CreatedAt).ToListAsync();
            return Ok(data.Select(_mapper.Map<NotificationDto>));
        }

        // POST /api/notifications/mark-read
        [HttpPost("mark-read")]
        public async Task<IActionResult> MarkRead([FromBody] NotificationMarkDto dto)
        {
            var meEmail = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(meEmail)) return Unauthorized();

            var myId = await _db.Users.Where(u => u.Email == meEmail).Select(u => u.Id).FirstOrDefaultAsync();

            var items = await _db.Notifications
                .Where(n => n.ToUserId == myId && dto.Ids.Contains(n.Id))
                .ToListAsync();

            if (items.Count == 0) return NoContent();

            items.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // POST /api/notifications/mark-all-read
        [HttpPost("mark-all-read")]
        public async Task<IActionResult> MarkAllRead()
        {
            var meEmail = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(meEmail)) return Unauthorized();

            var myId = await _db.Users.Where(u => u.Email == meEmail).Select(u => u.Id).FirstOrDefaultAsync();

            var items = await _db.Notifications
                .Where(n => n.ToUserId == myId && !n.IsRead)
                .ToListAsync();

            if (items.Count == 0) return NoContent();

            items.ForEach(n => n.IsRead = true);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE /api/notifications/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var meEmail = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(meEmail)) return Unauthorized();

            var me = await _db.Users.FirstOrDefaultAsync(u => u.Email == meEmail);
            if (me is null) return Unauthorized();

            var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id);
            if (n is null) return NotFound();

            var isAdmin = me.Role == 2;
            if (!(isAdmin || n.ToUserId == me.Id)) return Forbid();

            _db.Notifications.Remove(n);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
