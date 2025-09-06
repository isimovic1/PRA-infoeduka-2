using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.Grades;

[Authorize]
public class GradesController : Controller
{
    private readonly IHttpClientFactory _http;
    public GradesController(IHttpClientFactory http) => _http = http;

    private HttpClient ApiWithBearer(out string? token)
    {
        token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");
        if (!string.IsNullOrWhiteSpace(token))
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return api;
    }

    // Shows grade for a submission (student/staff). Staff will see the edit form.
    // route: /Grades/ForSubmission?submissionId=123&courseId=45
    [HttpGet]
    public async Task<IActionResult> ForSubmission(int submissionId, int courseId)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        ViewBag.CourseId = courseId;
        ViewBag.SubmissionId = submissionId;

        // Try get grade
        var resp = await api.GetAsync($"api/grades/submission/{submissionId}");
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized) return RedirectToAction("Login", "Auth");
        if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden) return RedirectToAction("ChangePassword", "Auth");

        GradeVM? grade = null;
        if (resp.IsSuccessStatusCode)
        {
            // API returns GradeDTO
            var dto = await resp.Content.ReadFromJsonAsync<GradeVM>();
            grade = dto;
        }
        else if (resp.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            // if NotFound, it's just not graded yet; otherwise show error
            TempData["Err"] = await resp.Content.ReadAsStringAsync();
        }

        // For staff, prefill Save VM if grade exists
        var save = new GradeSaveVM
        {
            SubmissionId = submissionId,
            CourseId = courseId,
            Points = grade?.Points ?? 0,
            Note = grade?.Note
        };
        ViewBag.Save = save;

        return View(grade);
    }

    // Staff create/update grade
    [Authorize(Roles = "Admin,Professor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(GradeSaveVM vm)
    {
        if (!ModelState.IsValid)
            return RedirectToAction("ForSubmission", new { submissionId = vm.SubmissionId, courseId = vm.CourseId });

        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Post, $"api/grades/{vm.SubmissionId}")
        {
            Content = JsonContent.Create(new { points = vm.Points, note = vm.Note })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            TempData["Err"] = await resp.Content.ReadAsStringAsync();
        }
        else
        {
            TempData["Msg"] = "Grade saved!";
        }

        return RedirectToAction("ForSubmission", new { submissionId = vm.SubmissionId, courseId = vm.CourseId });
    }

    [Authorize(Roles = "Admin,Professor")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveGrade(int submissionId, int courseId)
    {
        var token = HttpContext.Session.GetString("API_JWT");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var api = _http.CreateClient("API");
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/grades/{submissionId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req);
        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] = resp.IsSuccessStatusCode
            ? "Grade removed."
            : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(ForSubmission), new { submissionId, courseId });
    }

}
