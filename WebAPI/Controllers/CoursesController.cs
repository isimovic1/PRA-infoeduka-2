// WebAPI/Controllers/CoursesController.cs
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
    public class CoursesController : ControllerBase
    {
        private readonly Infoeduka2Context _db;
        private readonly IMapper _mapper;

        public CoursesController(Infoeduka2Context db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        // GET: /api/courses?q=&page=1&pageSize=50
        [Authorize] // adjust to [AllowAnonymous] if you want it public
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CourseDto>>> GetAll(
            [FromQuery] string? q,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 200) pageSize = 50;

            var query = _db.Courses.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(q))
            {
                var term = q.Trim();
                query = query.Where(c => c.Name.Contains(term) || (c.ShortDescription ?? "").Contains(term));
            }

            var items = await query
                .OrderBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(_mapper.Map<List<CourseDto>>(items));
        }

        // GET: /api/courses/5
        [Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CourseDto>> GetById(int id)
        {
            var course = await _db.Courses.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
            if (course is null) return NotFound();
            return Ok(_mapper.Map<CourseDto>(course));
        }

        // POST: /api/courses
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult<CourseDto>> Create([FromBody] CourseCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var name = dto.Name.Trim();
            var exists = await _db.Courses.AnyAsync(c => c.Name == name);
            if (exists) return Conflict("Course with this name already exists.");

            var entity = _mapper.Map<Course>(dto);
            entity.Name = name;

            _db.Courses.Add(entity);
            await _db.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, _mapper.Map<CourseDto>(entity));
        }

        // PUT: /api/courses/5
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] CourseUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course is null) return NotFound();

            var newName = dto.Name.Trim();
            var nameTaken = await _db.Courses.AnyAsync(c => c.Id != id && c.Name == newName);
            if (nameTaken) return Conflict("Course with this name already exists.");

            _mapper.Map(dto, course);
            course.Name = newName;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/courses/5
        //[Authorize(Roles = "Admin")]
        //[HttpDelete("{id:int}")]
        //public async Task<IActionResult> Delete(int id)
        //{
        //    var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
        //    if (course is null) return NotFound();

        //    _db.Courses.Remove(course);
        //    await _db.SaveChangesAsync();
        //    return NoContent();
        //}

        // DELETE: /api/courses/5
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course is null) return NotFound();

            // Pre-flight dependency check (so we control the error message)
            var blockers = new List<string>();

            var hasFiles = await _db.FileAssets.AnyAsync(f => f.CourseId == id);
            var hasSubs = await _db.Submissions.AnyAsync(s => s.CourseId == id);
            var hasTeachers = await _db.CourseTeachers.AnyAsync(ct => ct.CourseId == id);
            var hasStudents = await _db.CourseStudents.AnyAsync(cs => cs.CourseId == id);

            if (hasFiles) blockers.Add("file uploads");
            if (hasSubs) blockers.Add("submissions");
            if (hasTeachers) blockers.Add("assigned professors");
            if (hasStudents) blockers.Add("enrolled students");

            if (blockers.Count > 0)
            {
                var msg = $"Course cannot be deleted. Remove related data first: {string.Join(", ", blockers)}.";
                return Conflict(msg);
            }

            _db.Courses.Remove(course);
            await _db.SaveChangesAsync();
            return NoContent();
        }


        // ---------- Teachers ----------
        // GET: /api/courses/5/teachers
        [Authorize]
        [HttpGet("{id:int}/teachers")]
        public async Task<ActionResult<IEnumerable<CourseTeacherDto>>> GetTeachers(int id)
        {
            var exists = await _db.Courses.AnyAsync(c => c.Id == id);
            if (!exists) return NotFound();

            var list = await _db.CourseTeachers
                .AsNoTracking()
                .Where(ct => ct.CourseId == id)
                .Select(ct => new CourseTeacherDto
                {
                    UserId = ct.TeacherId,
                    IsAssistant = ct.IsAssistant,
                    Email = ct.Teacher.Email,
                    FirstName = ct.Teacher.FirstName,
                    LastName = ct.Teacher.LastName
                })
                .ToListAsync();

            return Ok(list);
        }

        // POST: /api/courses/5/teachers
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/teachers")]
        public async Task<IActionResult> AddTeacher(int id, [FromBody] CourseTeacherAssignDto dto)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course is null) return NotFound("Course not found.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
            if (user is null) return BadRequest("User not found.");
            if (user.Role != 1) return BadRequest("User must have Professor role.");

            var exists = await _db.CourseTeachers
                .AnyAsync(ct => ct.CourseId == id && ct.TeacherId == dto.UserId);
            if (exists) return Conflict("This professor is already assigned to the course.");

            _db.CourseTeachers.Add(new CourseTeacher
            {
                CourseId = id,
                TeacherId = dto.UserId,
                IsAssistant = dto.IsAssistant
            });

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/courses/5/teachers/7
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/teachers/{userId:int}")]
        public async Task<IActionResult> RemoveTeacher(int id, int userId)
        {
            var link = await _db.CourseTeachers
                .FirstOrDefaultAsync(ct => ct.CourseId == id && ct.TeacherId == userId);
            if (link is null) return NotFound();

            _db.CourseTeachers.Remove(link);
            await _db.SaveChangesAsync();
            return NoContent();
        }

        // ---------- Students ----------
        // GET: /api/courses/5/students
        [Authorize]
        [HttpGet("{id:int}/students")]
        public async Task<ActionResult<IEnumerable<CourseStudentDto>>> GetStudents(int id)
        {
            var exists = await _db.Courses.AnyAsync(c => c.Id == id);
            if (!exists) return NotFound();

            var list = await _db.CourseStudents
                .AsNoTracking()
                .Where(cs => cs.CourseId == id)
                .Select(cs => new CourseStudentDto
                {
                    UserId = cs.StudentId,
                    Email = cs.Student.Email,
                    FirstName = cs.Student.FirstName,
                    LastName = cs.Student.LastName,
                    GroupId = cs.Student.GroupId
                })
                .ToListAsync();

            return Ok(list);
        }

        // POST: /api/courses/5/students
        [Authorize(Roles = "Admin")]
        [HttpPost("{id:int}/students")]
        public async Task<IActionResult> EnrollStudent(int id, [FromBody] CourseStudentEnrollDto dto)
        {
            var course = await _db.Courses.FirstOrDefaultAsync(c => c.Id == id);
            if (course is null) return NotFound("Course not found.");

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == dto.UserId);
            if (user is null) return BadRequest("User not found.");
            if (user.Role != 0) return BadRequest("User must have Student role.");

            var exists = await _db.CourseStudents
                .AnyAsync(cs => cs.CourseId == id && cs.StudentId == dto.UserId);
            if (exists) return Conflict("This student is already enrolled in the course.");

            _db.CourseStudents.Add(new CourseStudent
            {
                CourseId = id,
                StudentId = dto.UserId
            });

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: /api/courses/5/students/12
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}/students/{userId:int}")]
        public async Task<IActionResult> UnenrollStudent(int id, int userId)
        {
            var link = await _db.CourseStudents
                .FirstOrDefaultAsync(cs => cs.CourseId == id && cs.StudentId == userId);
            if (link is null) return NotFound();

            _db.CourseStudents.Remove(link);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

/* Course name is unique (DB has UQ_Course_Name), so controller checks for duplicates and returns 409 Conflict.

Only Admin can create/update/delete courses and manage enrollments/assignments.

Role checks guard misuse: only Role=1 can be assigned as teacher; only Role=0 can be enrolled as student.

Listing endpoints are [Authorize]; change to [AllowAnonymous] if you want public read. */
