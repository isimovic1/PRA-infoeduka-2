using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.Submissions;
using WebApp.ViewModels.Courses;
[Authorize]
public class SubmissionsController : Controller
{
    private readonly IHttpClientFactory _http;
    public SubmissionsController(IHttpClientFactory http) => _http = http;

    // helper to get an HttpClient with Bearer, plus quick 401/403 handling
    private HttpClient ApiWithBearer(out string? token)
    {
        token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");
        if (!string.IsNullOrWhiteSpace(token))
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return api;
    }

    [HttpGet]
    public async Task<IActionResult> ForCourse(int id)
    {
        var token = HttpContext.Session.GetString("API_JWT");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var api = _http.CreateClient("API");
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Get the course as a typed VM (NOT dynamic/JsonElement)
        var course = await api.GetFromJsonAsync<CourseVM>($"api/courses/{id}");
        if (course == null) return NotFound();

        // Staff see all; students see only theirs
        var isStaff = User.IsInRole("Admin") || User.IsInRole("Professor");
        var url = isStaff ? $"api/submissions/course/{id}" : $"api/submissions/my?courseId={id}";

        var list = await api.GetFromJsonAsync<List<WebApp.ViewModels.Submissions.SubmissionItemVM>>(url)
                   ?? new();

        ViewBag.Course = course; // strongly-typed object now
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(SubmissionUploadVM vm) // CourseId + File
    {
        if (!ModelState.IsValid) return RedirectToAction("ForCourse", new { id = vm.CourseId });

        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        using var form = new MultipartFormDataContent();
        form.Add(new StreamContent(vm.File.OpenReadStream()), "File", vm.File.FileName);
        form.Add(new StringContent(vm.CourseId.ToString()), "CourseId"); // matches SubmissionUploadForm

        var req = new HttpRequestMessage(HttpMethod.Post, "api/submissions/upload");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = form;

        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode) TempData["Err"] = await resp.Content.ReadAsStringAsync();
        else TempData["Msg"] = "Submission uploaded.";

        return RedirectToAction("ForCourse", new { id = vm.CourseId });
    }

    [Authorize(Roles = "Admin,Professor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(int id, ReviewSubmissionVM vm) // id = courseId (for redirect)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Post, $"api/submissions/{vm.SubmissionId}/review")
        {
            Content = JsonContent.Create(new { reviewed = vm.Reviewed, comment = vm.Comment })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode) TempData["Err"] = await resp.Content.ReadAsStringAsync();
        else TempData["Msg"] = "Marked as reviewed.";

        return RedirectToAction("ForCourse", new { id });
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int courseId)
    {
        var token = HttpContext.Session.GetString("API_JWT");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var api = _http.CreateClient("API");
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/submissions/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req);
        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] = resp.IsSuccessStatusCode
            ? "Submission deleted."
            : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(ForCourse), new { id = courseId });
    }


}
