using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.Notifications;

[Authorize]
public class NotificationsController : Controller
{
    private readonly IHttpClientFactory _http;
    public NotificationsController(IHttpClientFactory http) => _http = http;

    private HttpClient ApiWithBearer(out string? token)
    {
        token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");
        if (!string.IsNullOrWhiteSpace(token))
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return api;
    }

    [HttpGet]
    public async Task<IActionResult> Index(bool unreadOnly = false)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var resp = await api.GetAsync($"api/notifications/my?unreadOnly={(unreadOnly ? "true" : "false")}");
        if (resp.StatusCode == System.Net.HttpStatusCode.Unauthorized) return RedirectToAction("Login", "Auth");
        if (resp.StatusCode == System.Net.HttpStatusCode.Forbidden) return RedirectToAction("ChangePassword", "Auth");
        if (!resp.IsSuccessStatusCode) { TempData["Err"] = await resp.Content.ReadAsStringAsync(); return View(new List<NotificationVM>()); }

        var list = await resp.Content.ReadFromJsonAsync<List<NotificationVM>>() ?? new();
        ViewBag.UnreadOnly = unreadOnly;
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkRead([FromForm] int[] ids, bool unreadOnly = false)
    {
        if (ids is null || ids.Length == 0) return RedirectToAction("Index", new { unreadOnly });

        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Post, "api/notifications/mark-read")
        {
            Content = JsonContent.Create(new { ids })
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode) TempData["Err"] = await resp.Content.ReadAsStringAsync();
        else TempData["Msg"] = "Marked as read.";
        return RedirectToAction("Index", new { unreadOnly });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkAllRead(bool unreadOnly = false)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Post, "api/notifications/mark-all-read");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode) TempData["Err"] = await resp.Content.ReadAsStringAsync();
        else TempData["Msg"] = "All marked as read.";
        return RedirectToAction("Index", new { unreadOnly });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, bool unreadOnly = false)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/notifications/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode) TempData["Err"] = await resp.Content.ReadAsStringAsync();
        else TempData["Msg"] = "Deleted.";
        return RedirectToAction("Index", new { unreadOnly });
    }

    // small utility endpoint to show a badge count
    [HttpGet]
    public async Task<IActionResult> UnreadCount()
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return Json(new { count = 0 });

        var resp = await api.GetAsync("api/notifications/my?unreadOnly=true");
        if (!resp.IsSuccessStatusCode) return Json(new { count = 0 });

        var list = await resp.Content.ReadFromJsonAsync<List<NotificationVM>>() ?? new();
        return Json(new { count = list.Count });
    }
}
