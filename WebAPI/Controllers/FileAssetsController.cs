using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;
using WebAPI.Models;
using AutoMapper;
using WebAPI.Services; 

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/files")]
    [Authorize] // all endpoints require a logged-in user
    [Authorize(Policy = "NotFirstLogin")]
    public sealed class FileAssetsController : ControllerBase
    {
        private readonly Infoeduka2Context _db;
        private readonly IConfiguration _cfg;
        private readonly IMapper _mapper;
        private readonly INotificationService _notifier; 

        public FileAssetsController(
            Infoeduka2Context db,
            IConfiguration cfg,
            IMapper mapper,
            INotificationService notifier) 
        {
            _db = db;
            _cfg = cfg;
            _mapper = mapper;
            _notifier = notifier; 
        }

        // POST /api/files/{courseId}/upload
        [HttpPost("{courseId:int}/upload")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(100_000_000)] // 100MB default cap; adjust as needed
        public async Task<IActionResult> Upload(int courseId, [FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("File is required.");

            // Resolve current user
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();

            // Course must exist
            var courseExists = await _db.Courses.AnyAsync(c => c.Id == courseId);
            if (!courseExists) return NotFound("Course not found.");

            // Permissions:
            var isAdmin = me.Role == 2;
            var isTeacher = await _db.CourseTeachers.AnyAsync(ct => ct.CourseId == courseId && ct.TeacherId == me.Id);
            var isStudentEnrolled = await _db.CourseStudents.AnyAsync(cs => cs.CourseId == courseId && cs.StudentId == me.Id);

            if (!(isAdmin || isTeacher || isStudentEnrolled))
                return Forbid(); // not related to the course

            // Optional: basic content checks
            var maxSize = GetMaxUploadSize();
            if (file.Length > maxSize)
                return BadRequest($"File too large. Max {maxSize} bytes.");

            // Build physical path
            var basePath = GetBaseUploadPath();
            var now = DateTime.UtcNow;
            var safeExt = Path.GetExtension(file.FileName);
            var newName = $"{Guid.NewGuid():N}{safeExt}";
            var subFolder = Path.Combine(courseId.ToString(), now.Year.ToString(), now.Month.ToString("D2"));
            var fullFolder = Path.Combine(basePath, subFolder);
            Directory.CreateDirectory(fullFolder);

            var fullPath = Path.Combine(fullFolder, newName);
            await using (var fs = System.IO.File.Create(fullPath))
            {
                await file.CopyToAsync(fs);
            }

            // Save DB row
            var fa = new FileAsset
            {
                FileName = Path.GetFileName(file.FileName),
                StoredPath = fullPath, // storing absolute path for simplicity
                //ContentType = file.ContentType ?? "application/octet-stream",
                ContentType = string.IsNullOrWhiteSpace(file.ContentType)
                             ? "application/octet-stream"
                              : file.ContentType,
                SizeBytes = file.Length,
                CourseId = courseId,
                UploadedById = me.Id,
                UploadedAt = now
            };

            _db.FileAssets.Add(fa);
            await _db.SaveChangesAsync();

            // ==== NEW: notify course teachers if a STUDENT uploaded ====
            if (me.Role == 0)
            {
                var teacherIds = await _db.CourseTeachers
                    .Where(ct => ct.CourseId == courseId)
                    .Select(ct => ct.TeacherId)
                    .ToListAsync();

                if (teacherIds.Count > 0)
                {
                    var courseName = await _db.Courses
                        .Where(c => c.Id == courseId)
                        .Select(c => c.Name)
                        .FirstOrDefaultAsync() ?? $"Course #{courseId}";

                    var title = $"New upload to {courseName}";
                    var body = $"{me.FirstName} {me.LastName} Has uploaded \"{fa.FileName}\".";
                    //var link = $"/api/files/{fa.Id}"; // adjust if you have a UI route
                    var link = $"/Submissions/ForCourse?id={courseId}";
                    await _notifier.SendToUsersAsync(teacherIds, me.Id, title, body, link);
                }
            }
            // ===========================================================

            return CreatedAtAction(nameof(GetById), new { id = fa.Id }, _mapper.Map<FileAssetDto>(fa));
        }

        // GET /api/files/course/{courseId}
        [HttpGet("course/{courseId:int}")]
        public async Task<IActionResult> ListByCourse(int courseId)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();

            // must be related to the course to see files
            var isAdmin = me.Role == 2;
            var isTeacher = await _db.CourseTeachers.AnyAsync(ct => ct.CourseId == courseId && ct.TeacherId == me.Id);
            var isStudentEnrolled = await _db.CourseStudents.AnyAsync(cs => cs.CourseId == courseId && cs.StudentId == me.Id);

            if (!(isAdmin || isTeacher || isStudentEnrolled))
                return Forbid();

            var files = await _db.FileAssets
                .AsNoTracking()
                .Include(f => f.UploadedBy)
                .Where(f => f.CourseId == courseId)
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
            return Ok(files.Select(f => _mapper.Map<FileAssetDto>(f)));
            // return Ok(files.Select(_mapper.Map<FileAssetDto>));
        }

        // GET /api/files/{id}/download
        [HttpGet("{id:int}/download")]
        public async Task<IActionResult> Download(int id)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();

            var fa = await _db.FileAssets.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id);
            if (fa is null) return NotFound();

            // must be related to the course
            var isAdmin = me.Role == 2;
            var isTeacher = await _db.CourseTeachers.AnyAsync(ct => ct.CourseId == fa.CourseId && ct.TeacherId == me.Id);
            var isStudentEnrolled = await _db.CourseStudents.AnyAsync(cs => cs.CourseId == fa.CourseId && cs.StudentId == me.Id);
            var isUploader = fa.UploadedById == me.Id;

            if (!(isAdmin || isTeacher || isStudentEnrolled || isUploader))
                return Forbid();

            if (!System.IO.File.Exists(fa.StoredPath))
                return NotFound("File not found on disk.");


            var contentType = string.IsNullOrWhiteSpace(fa.ContentType)
                ? "application/octet-stream"
                : fa.ContentType;

            
            var stream = System.IO.File.OpenRead(fa.StoredPath);
            return File(stream, contentType, Path.GetFileName(fa.FileName));

            //var stream = System.IO.File.OpenRead(fa.StoredPath);
            //return File(stream, fa.ContentType, fa.FileName);
        }

        // DELETE /api/files/{id}
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();

            var fa = await _db.FileAssets.FirstOrDefaultAsync(f => f.Id == id);
            if (fa is null) return NotFound();

            // Prevent delete if used by a submission (FK will also block)
            var used = await _db.Submissions.AnyAsync(s => s.FileAssetId == fa.Id);
            if (used) return BadRequest("File is used in a submission and cannot be deleted.");

            var isAdmin = me.Role == 2;
            var isTeacher = await _db.CourseTeachers.AnyAsync(ct => ct.CourseId == fa.CourseId && ct.TeacherId == me.Id);
            var isUploader = fa.UploadedById == me.Id;

            if (!(isAdmin || isTeacher || isUploader))
                return Forbid();

            // Delete file on disk (best-effort)
            try
            {
                if (System.IO.File.Exists(fa.StoredPath))
                    System.IO.File.Delete(fa.StoredPath);
            }
            catch { /* swallow disk errors, DB will still delete row */ }

            _db.FileAssets.Remove(fa);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // GET /api/files/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var me = await GetCurrentUserAsync();
            if (me is null) return Unauthorized();

            var fa = await _db.FileAssets.AsNoTracking().Include(f => f.UploadedBy).FirstOrDefaultAsync(f => f.Id == id);
            if (fa is null) return NotFound();

            // same access rule as download
            var isAdmin = me.Role == 2;
            var isTeacher = await _db.CourseTeachers.AnyAsync(ct => ct.CourseId == fa.CourseId && ct.TeacherId == me.Id);
            var isStudentEnrolled = await _db.CourseStudents.AnyAsync(cs => cs.CourseId == fa.CourseId && cs.StudentId == me.Id);
            var isUploader = fa.UploadedById == me.Id;

            if (!(isAdmin || isTeacher || isStudentEnrolled || isUploader))
                return Forbid();

            return Ok(_mapper.Map<FileAssetDto>(fa));
        }

        // -------- helpers --------
        private async Task<User?> GetCurrentUserAsync()
        {
            var email = User?.Identity?.Name;
            if (string.IsNullOrWhiteSpace(email)) return null;
            return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        private string GetBaseUploadPath()
        {
            var cfgPath = _cfg["Uploads:BasePath"];
            if (!string.IsNullOrWhiteSpace(cfgPath)) return cfgPath;
            // default: ./uploads
            var def = Path.Combine(Directory.GetCurrentDirectory(), "uploads");
            Directory.CreateDirectory(def);
            return def;
        }

        private long GetMaxUploadSize()
        {
            // default 100MB
            if (long.TryParse(_cfg["Uploads:MaxSizeBytes"], out var b) && b > 0) return b;
            return 100_000_000;
        }
    }
}
