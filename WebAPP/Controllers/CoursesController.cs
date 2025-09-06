using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.Courses;

[Authorize]
public class CoursesController : Controller
{
    private readonly IHttpClientFactory _http;
    public CoursesController(IHttpClientFactory http) => _http = http;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, int page = 1, int pageSize = 50)
    {
        var api = _http.CreateClient("API");
        var token = HttpContext.Session.GetString("API_JWT");
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Your API returns CourseDto list; map the needed fields.
        var list = await api.GetFromJsonAsync<List<CourseVM>>($"api/courses?q={Uri.EscapeDataString(q ?? "")}&page={page}&pageSize={pageSize}")
                   ?? new List<CourseVM>();

        return View(list);
    }


    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var api = _http.CreateClient("API");
        var token = HttpContext.Session.GetString("API_JWT");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");
        api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // Course header is public to any authenticated user
        var course = await api.GetFromJsonAsync<CourseVM>($"api/courses/{id}");
        if (course is null) return NotFound();
        ViewBag.Course = course;

        // Files require you to be related to the course -> may return 403
        var resp = await api.GetAsync($"api/files/course/{id}");
        if (resp.StatusCode == HttpStatusCode.Forbidden)
        {
            // Show a friendly page instead of crashing
            return View("NotRelated");        // <— add this view (below)
        }
        if (resp.StatusCode == HttpStatusCode.NotFound)
            return NotFound();

        if (!resp.IsSuccessStatusCode)
        {
            TempData["Err"] = $"Couldn't load files (HTTP {(int)resp.StatusCode}).";
            return View(new List<FileAssetVM>());
        }

        var files = await resp.Content.ReadFromJsonAsync<List<FileAssetVM>>()
                    ?? new List<FileAssetVM>();
        return View(files);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Err"] = "Please choose a file.";
            return RedirectToAction("Details", new { id });
        }

        var token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");

        using var form = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        form.Add(new StreamContent(stream), "file", file.FileName);

        var req = new HttpRequestMessage(HttpMethod.Post, $"api/files/{id}/upload");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = form;

        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            TempData["Err"] = await resp.Content.ReadAsStringAsync();
        }
        else
        {
            TempData["Msg"] = "File uploaded.";
        }

        return RedirectToAction("Details", new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        var token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");

        var req = new HttpRequestMessage(HttpMethod.Get, $"api/files/{id}/download");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req, HttpCompletionOption.ResponseHeadersRead);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            return Content(err);
        }

        var stream = await resp.Content.ReadAsStreamAsync();
        var ct = resp.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var name = resp.Content.Headers.ContentDisposition?.FileNameStar
                   ?? resp.Content.Headers.ContentDisposition?.FileName
                   ?? "file";
        return File(stream, ct, name);
    }


    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFile(int id, int courseId)
    {
        var token = HttpContext.Session.GetString("API_JWT");
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var api = _http.CreateClient("API");
        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/files/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req);
        if (resp.IsSuccessStatusCode)
            TempData["Msg"] = "File deleted.";
        else
            TempData["Err"] = await resp.Content.ReadAsStringAsync();

        return RedirectToAction("Details", new { id = courseId });
    }


}
