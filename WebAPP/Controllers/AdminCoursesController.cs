using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.Courses;
using WebApp.ViewModels.AdminCourses;

[Authorize(Roles = "Admin")]
public class AdminCoursesController : Controller
{
    private readonly IHttpClientFactory _http;
    public AdminCoursesController(IHttpClientFactory http) => _http = http;

    private HttpClient ApiWithBearer(out string? token)
    {
        token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");
        if (!string.IsNullOrWhiteSpace(token))
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return api;
    }

    // --- List courses + search ---
    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var list = await api.GetFromJsonAsync<List<CourseVM>>($"api/courses?q={Uri.EscapeDataString(q ?? "")}")
                   ?? new List<CourseVM>();
        ViewBag.Q = q;
        return View(list);
    }

    // --- Create course ---
    [HttpGet]
    public IActionResult Create() => View(new CourseVM());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CourseVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var payload = new { name = vm.Name, shortDescription = vm.ShortDescription };
        var resp = await api.PostAsJsonAsync("api/courses", payload);
        if (!resp.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await resp.Content.ReadAsStringAsync());
            return View(vm);
        }
        TempData["Msg"] = "Course created.";
        return RedirectToAction(nameof(Index));
    }

    // --- Edit name/description (inline on Manage) ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(int id, CourseVM vm)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var payload = new { name = vm.Name, shortDescription = vm.ShortDescription };
        var resp = await api.PutAsJsonAsync($"api/courses/{id}", payload);
        if (!resp.IsSuccessStatusCode)
        {
            TempData["Err"] = await resp.Content.ReadAsStringAsync();
        }
        else TempData["Msg"] = "Course saved.";
        return RedirectToAction(nameof(Manage), new { id });
    }

    // --- Delete course ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/courses/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
            TempData["Err"] = await resp.Content.ReadAsStringAsync();
        else
            TempData["Msg"] = "Course deleted.";
        return RedirectToAction(nameof(Index));
    }

    // --- Manage people in a course ---
    [HttpGet]
    public async Task<IActionResult> Manage(int id, string? qProf, string? qStud)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var course = await api.GetFromJsonAsync<CourseVM>($"api/courses/{id}");
        if (course is null) return NotFound();

        var teachers = await api.GetFromJsonAsync<List<CourseTeacherItemVM>>($"api/courses/{id}/teachers")
                       ?? new List<CourseTeacherItemVM>();
        var students = await api.GetFromJsonAsync<List<CourseStudentItemVM>>($"api/courses/{id}/students")
                       ?? new List<CourseStudentItemVM>();

        var vm = new ManageCourseVM
        {
            Course = course,
            Teachers = teachers,
            Students = students,
            QProf = qProf,
            QStud = qStud,
        };

        // Strongly typed search results (no 'dynamic' and no CS1977)
        if (!string.IsNullOrWhiteSpace(qProf))
        {
            var p = await api.GetFromJsonAsync<UsersPage>(
                $"api/users?search={Uri.EscapeDataString(qProf)}&role=1&pageSize=10");
            vm.ProfResults = p?.Items ?? new List<UserSearchResultVM>();
        }

        if (!string.IsNullOrWhiteSpace(qStud))
        {
            var p = await api.GetFromJsonAsync<UsersPage>(
                $"api/users?search={Uri.EscapeDataString(qStud)}&role=0&pageSize=10");
            vm.StudResults = p?.Items ?? new List<UserSearchResultVM>();
        }

        return View(vm);
    }


    // --- Assign / remove professors ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddTeacher(int id, int userId, bool isAssistant = false)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var resp = await api.PostAsJsonAsync($"api/courses/{id}/teachers", new { userId, isAssistant });
        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] = resp.IsSuccessStatusCode
            ? "Professor assigned."
            : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(Manage), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveTeacher(int id, int userId)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/courses/{id}/teachers/{userId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await api.SendAsync(req);

        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] = resp.IsSuccessStatusCode
            ? "Professor removed."
            : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(Manage), new { id });
    }

    // --- Enroll / unenroll students ---
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EnrollStudent(int id, int userId)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var resp = await api.PostAsJsonAsync($"api/courses/{id}/students", new { userId });
        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] = resp.IsSuccessStatusCode
            ? "Student enrolled."
            : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(Manage), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UnenrollStudent(int id, int userId)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/courses/{id}/students/{userId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await api.SendAsync(req);

        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] = resp.IsSuccessStatusCode
            ? "Student removed."
            : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(Manage), new { id });
    }


    //Helper
    private sealed class UsersPage
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<WebApp.ViewModels.AdminCourses.UserSearchResultVM> Items { get; set; } = new();
    }
}
