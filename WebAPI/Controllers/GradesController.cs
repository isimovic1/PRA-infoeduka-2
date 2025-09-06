using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebAPI.DTOs;
using WebAPI.Models;
using WebAPI.Services; 

[ApiController]
[Route("api/grades")]
[Authorize(Policy = "NotFirstLogin")]
public class GradesController : ControllerBase
{
    private readonly Infoeduka2Context _db;
    private readonly IMapper _mapper;
    private readonly INotificationService _notifier; 

    public GradesController(Infoeduka2Context db, IMapper mapper, INotificationService notifier) 
    {
        _db = db;
        _mapper = mapper;
        _notifier = notifier; 
    }

    // --- HELPERS -------------------------------------------------------------
    private async Task<User?> GetCurrentUserAsync()
    {
        var email = User?.Identity?.Name;
        if (string.IsNullOrWhiteSpace(email)) return null;
        return await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    private async Task<bool> IsAssignedTeacherAsync(int courseId, int teacherUserId)
        => await _db.CourseTeachers
            .AsNoTracking()
            .AnyAsync(ct => ct.CourseId == courseId && ct.TeacherId == teacherUserId);

    // --- CREATE / UPDATE GRADE -----------------------------------------------
    [HttpPost("{submissionId:int}")]
    [Authorize(Roles = "Admin,Professor")]
    public async Task<IActionResult> Create(int submissionId, [FromBody] GradeCreateDTO dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var me = await GetCurrentUserAsync();
        if (me is null) return Unauthorized();

        var sub = await _db.Submissions
            .Include(s => s.FileAsset) // for file name + course id
            .FirstOrDefaultAsync(s => s.Id == submissionId);
        if (sub is null) return NotFound();

        var isAdmin = User.IsInRole("Admin");
        var teaches = await IsAssignedTeacherAsync(sub.CourseId, me.Id);

        // Only admins or assigned teachers may grade
        if (!isAdmin && !teaches) return Forbid();

        var existing = await _db.Grades.FirstOrDefaultAsync(g => g.SubmissionId == submissionId);
        if (existing is null)
        {
            _db.Grades.Add(new Grade
            {
                SubmissionId = submissionId,
                TeacherId = me.Id,
                Points = dto.Points,
                GradedAt = DateTime.UtcNow
            });
        }
        else
        {
            // (optional policy) require same teacher to update:
            // if (!isAdmin && existing.TeacherId != me.Id && !teaches) return Forbid();

            existing.Points = dto.Points;
            existing.TeacherId = me.Id;
            existing.GradedAt = DateTime.UtcNow;
        }

        sub.Reviewed = true;
        await _db.SaveChangesAsync();

        // ==== NEW: notify the student that their work was graded ====
        var courseName = await _db.Courses
            .Where(c => c.Id == sub.CourseId)
            .Select(c => c.Name)
            .FirstOrDefaultAsync() ?? $"Kolegij #{sub.CourseId}";

        var title = $"Grade in {courseName}";
        var body = $"Your Submission \"{sub.FileAsset.FileName}\" has been graded with {dto.Points} points.";
        var link = $"/Submissions/ForCourse?id={sub.CourseId}";
        await _notifier.SendToUserAsync(sub.StudentId, me.Id, title, body, link);


        /* var link = $"/api/submissions/my?courseId={sub.CourseId}"; // adapt to UI route if needed
         await _notifier.SendToUserAsync(sub.StudentId, me.Id, title, body, link);*/
        // ============================================================

        return NoContent();
    }

    // --- READ GRADE FOR A SUBMISSION ----------------------------------------
    [HttpGet("submission/{submissionId:int}")]
    [Authorize(Roles = "Student,Admin,Professor")]
    public async Task<IActionResult> GetForSubmission(int submissionId)
    {
        var me = await GetCurrentUserAsync();
        if (me is null) return Unauthorized();

        var sub = await _db.Submissions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == submissionId);
        if (sub is null) return NotFound();

        // Students: only their own submission
        if (User.IsInRole("Student") && sub.StudentId != me.Id)
            return Forbid();

        // Professors: only courses they teach
        if (User.IsInRole("Professor"))
        {
            var teaches = await IsAssignedTeacherAsync(sub.CourseId, me.Id);
            if (!teaches) return Forbid();
        }

        var g = await _db.Grades.AsNoTracking().FirstOrDefaultAsync(x => x.SubmissionId == submissionId);
        if (g is null) return NotFound();

        return Ok(_mapper.Map<GradeDTO>(g));
    }

    // DELETE /api/grades/{submissionId}
    [HttpDelete("{submissionId:int}")]
    [Authorize(Roles = "Admin,Professor")]
    public async Task<IActionResult> DeleteGrade(int submissionId)
    {
        var me = await GetCurrentUserAsync();
        if (me is null) return Unauthorized();

        var sub = await _db.Submissions.FirstOrDefaultAsync(s => s.Id == submissionId);
        if (sub is null) return NotFound();

        var isAdmin = me.Role == 2;
        var teaches = await IsAssignedTeacherAsync(sub.CourseId, me.Id);
        if (!isAdmin && !teaches) return Forbid();

        var g = await _db.Grades.FirstOrDefaultAsync(x => x.SubmissionId == submissionId);
        if (g == null) return NoContent(); // nothing to delete

        _db.Grades.Remove(g);

        // optional: mark submission as not reviewed again
        sub.Reviewed = false;

        await _db.SaveChangesAsync();

        // (optional) notify the student that the grade was removed
        // await _notifier.SendToUserAsync(sub.StudentId, me.Id, "Grade removed", "Your grade was removed.", $"/Submissions/ForCourse?id={sub.CourseId}");

        return NoContent();
    }


}
