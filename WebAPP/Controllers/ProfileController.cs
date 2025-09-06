// WebApp/Controllers/ProfileController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.Auth;

[Authorize]
public class ProfileController : Controller
{
    private readonly IHttpClientFactory _http;
    public ProfileController(IHttpClientFactory http) => _http = http;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");
        var req = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await api.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return Content(await resp.Content.ReadAsStringAsync());
        var me = await resp.Content.ReadFromJsonAsync<UserDto>();
        return View(me);
    }
}
