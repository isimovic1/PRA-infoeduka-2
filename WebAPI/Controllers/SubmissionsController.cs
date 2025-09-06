using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;
using WebAPI.Models;
using AutoMapper;
using WebAPI.Services; // <-- add

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/submissions")]
    [Authorize]
    [Authorize(Policy = "NotFirstLogin")]
    public sealed class SubmissionsController : ControllerBase
    {
        private readonly Infoeduka2Context _db;
        private readonly IMapper _mapper;
        private readonly INotificationService _notifier;  // <-- add

        public SubmissionsController(Infoeduka2Context db, IMapper mapper, INotificationService notifier) // <-- add
        {
            _db = db;
            _mapper = mapper;
            _notifier = notifier; // <-- add
        }

        // POST /api/submissions
        // Student links an already uploaded file to a course.
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] SubmissionCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();
            if (me.Role != 0) return Forbid(); // only students create submissions

            // must be enrolled
            var enrolled = await _db.CourseStudents.AnyAsync(cs => cs.CourseId == dto.CourseId && cs.StudentId == me.Id);
            if (!enrolled) return Forbid();

            // file must exist, belong to this course and be uploaded by this student
            var file = await _db.FileAssets.FirstOrDefaultAsync(f => f.Id == dto.FileAssetId);
            if (file is null) return NotFound("FileAsset not found.");
            if (file.CourseId != dto.CourseId) return BadRequest("FileAsset course mismatch.");
            if (file.UploadedById != me.Id) return Forbid();

            var sub = new Submission
            {
                CourseId = dto.CourseId,
                FileAssetId = dto.FileAssetId,
                StudentId = me.Id,
                Reviewed = false
            };

            _db.Submissions.Add(sub);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Conflict("You already submitted this file for this course.");
            }

            // ---- NEW: notify course teachers about the new submission ----
            var teacherIds = await _db.CourseTeachers
                .Where(ct => ct.CourseId == sub.CourseId)
                .Select(ct => ct.TeacherId)
                .ToListAsync();

            if (teacherIds.Count > 0)
            {
                var courseName = await _db.Courses
                    .Where(c => c.Id == sub.CourseId)
                    .Select(c => c.Name)
                    .FirstOrDefaultAsync() ?? $"Course #{sub.CourseId}";

                var title = $"New Submission to {courseName}";
                var body = $"{me.FirstName} {me.LastName} has submitted \"{file.FileName}\".";
                var link = $"/Submissions/ForCourse?id={sub.CourseId}";
                await _notifier.SendToUsersAsync(teacherIds, me.Id, title, body, link);

                // var link = $"/api/submissions/course/{sub.CourseId}"; // adjust for your UI route if you have one

                //await _notifier.SendToUsersAsync(teacherIds, me.Id, title, body, link);
            }
            // ----------------------------------------------------------------

            // join file for DTO
            var created = await _db.Submissions
                .Include(s => s.FileAsset)
                .FirstAsync(s => s.Id == sub.Id);

            return CreatedAtAction(nameof(GetMy), new { courseId = sub.CourseId }, _mapper.Map<SubmissionDto>(created));
        }

        // POST /api/submissions/upload
        // Student uploads a file and creates a submission in one step
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAndSubmit([FromForm] SubmissionUploadForm form,
                                                      [FromServices] FileAssetsController files)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();
            if (me.Role != 0) return Forbid(); // students only

            var enrolled = await _db.CourseStudents
                .AnyAsync(cs => cs.CourseId == form.CourseId && cs.StudentId == me.Id);
            if (!enrolled) return Forbid();

            // Carry over HttpContext so the inner Upload sees the same user
            files.ControllerContext = new ControllerContext { HttpContext = HttpContext };

            var result = await files.Upload(form.CourseId, form.File);
            if (result is not CreatedAtActionResult created || created.Value is not FileAssetDto fa)
                return result;

            // This calls Create(...) above, which sends teacher notifications
            return await Create(new SubmissionCreateDto { CourseId = form.CourseId, FileAssetId = fa.Id });
        }

        // GET /api/submissions/my?courseId=123
        [HttpGet("my")]
        public async Task<IActionResult> GetMy([FromQuery] int? courseId = null)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();
            if (me.Role != 0) return Forbid();

            var q = _db.Submissions
                .AsNoTracking()
                .Include(s => s.FileAsset)
                .Where(s => s.StudentId == me.Id);

            if (courseId.HasValue)
                q = q.Where(s => s.CourseId == courseId.Value);

            var data = await q
                .OrderByDescending(s => s.FileAsset.UploadedAt)
                .ToListAsync();

            return Ok(data.Select(_mapper.Map<SubmissionDto>));
        }

        // GET /api/submissions/course/{courseId}
        [HttpGet("course/{courseId:int}")]
        [Authorize(Roles = "Admin,Professor")]
        public async Task<IActionResult> GetByCourse(int courseId)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();

            var isAdmin = me.Role == 2;
            var isTeacher = await _db.CourseTeachers.AnyAsync(ct => ct.CourseId == courseId && ct.TeacherId == me.Id);

            if (!(isAdmin || isTeacher)) return Forbid();

            var data = await _db.Submissions
              .AsNoTracking()
              .Include(s => s.FileAsset)
              .Include(s => s.Student)
              .Where(s => s.CourseId == courseId)
              .OrderByDescending(s => s.FileAsset.UploadedAt)
              .ToListAsync();

            return Ok(data.Select(s => new SubmissionDto
            {
                Id = s.Id,
                FileAssetId = s.FileAssetId,
                CourseId = s.CourseId,
                StudentId = s.StudentId,
                Reviewed = s.Reviewed,
                UploadedAt = s.FileAsset.UploadedAt,
                FileName = s.FileAsset.FileName,
                FileUrl = null,
                StudentName = s.Student.FirstName + " " + s.Student.LastName
            }));


            //var data = await _db.Submissions
            //    .AsNoTracking()
            //    .Include(s => s.FileAsset)
            //    .Where(s => s.CourseId == courseId)
            //    .OrderByDescending(s => s.FileAsset.UploadedAt)
            //    .ToListAsync();

            //return Ok(data.Select(_mapper.Map<SubmissionDto>));
        }

        // DELETE /api/submissions/{id}
        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var me = await GetCurrentUserAsync();
        //    if (me is null) return Unauthorized();

        //    var sub = await _db.Submissions
        //        .Include(s => s.FileAsset)
        //        .FirstOrDefaultAsync(s => s.Id == id);

        //    if (sub is null) return NotFound();

        //    var isAdmin = me.Role == 2;
        //    var isOwner = me.Role == 0 && sub.StudentId == me.Id; // student who submitted
        //    var isTeacher = await _db.CourseTeachers
        //        .AnyAsync(ct => ct.CourseId == sub.CourseId && ct.TeacherId == me.Id);

        //    // Students: can delete own submission only if not reviewed; teachers/admins anytime.
        //    if (!(isAdmin || isTeacher || (isOwner && !sub.Reviewed)))
        //        return Forbid();

        //    _db.Submissions.Remove(sub);
        //    await _db.SaveChangesAsync();
        //    return NoContent();
        //}

        // DELETE /api/submissions/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();

            var sub = await _db.Submissions
                .Include(s => s.FileAsset)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sub is null) return NotFound();

            var isAdmin = me.Role == 2;
            var isOwner = me.Role == 0 && sub.StudentId == me.Id; // student who submitted
            var isTeacher = await _db.CourseTeachers
                .AnyAsync(ct => ct.CourseId == sub.CourseId && ct.TeacherId == me.Id);

            // Students: can delete own submission only if not reviewed; teachers/admins anytime.
            if (!(isAdmin || isTeacher || (isOwner && !sub.Reviewed)))
                return Forbid();

            // If a grade exists, either remove it (staff) or block (student).
            var grade = await _db.Grades.FirstOrDefaultAsync(g => g.SubmissionId == id);
            if (grade != null)
            {
                if (isAdmin || isTeacher)
                {
                    _db.Grades.Remove(grade);
                    // Optional: also flip reviewed back, but not required for delete
                    // sub.Reviewed = false;
                }
                else
                {
                    return BadRequest("You cannot delete a graded submission. Ask a teacher/admin to remove the grade first.");
                }
            }

            _db.Submissions.Remove(sub);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        [HttpPost("{id:int}/review")]
        [Authorize(Roles = "Admin,Professor")]
        public async Task<IActionResult> Review(int id, [FromBody] ReviewSubmissionDto dto, [FromServices] INotificationService notifier)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();

            var s = await _db.Submissions
                .Include(x => x.Student)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (s is null) return NotFound();

            // only teacher of this course or admin
            var isTeacher = await _db.CourseTeachers.AnyAsync(ct => ct.CourseId == s.CourseId && ct.TeacherId == me.Id);
            var isAdmin = me.Role == 2;
            if (!(isAdmin || isTeacher)) return Forbid();

            s.Reviewed = dto.Reviewed;
            await _db.SaveChangesAsync();

            // notify student that the work was reviewed
            var courseName = await _db.Courses.Where(c => c.Id == s.CourseId).Select(c => c.Name).FirstOrDefaultAsync()
                             ?? $"Course #{s.CourseId}";
            var title = $"Submission Reviewed ({courseName})";
            var body = "Your submission has been reviewed!";
            var link = $"/Submissions/ForCourse?id={s.CourseId}";
            await notifier.SendToUsersAsync(new[] { s.StudentId }, me.Id, title, body, link);

            /*
            var link = $"/api/submissions/my?courseId={s.CourseId}";
            await notifier.SendToUsersAsync(new[] { s.StudentId }, me.Id, title, body, link);  */

            return NoContent();
        }

        // ------- helpers -------
        private async Task<User?> GetCurrentUserAsync()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}
