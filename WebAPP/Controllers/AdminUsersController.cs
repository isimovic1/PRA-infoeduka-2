using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.AdminUsers;

[Authorize(Roles = "Admin")]
public class AdminUsersController : Controller
{
    private readonly IHttpClientFactory _http;
    public AdminUsersController(IHttpClientFactory http) => _http = http;

    private HttpClient ApiWithBearer(out string? token)
    {
        token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");
        if (!string.IsNullOrWhiteSpace(token))
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return api;
    }

    // GET /AdminUsers?search=&role=&page=1&pageSize=20
    [HttpGet]
    public async Task<IActionResult> Index(string? search, byte? role, int page = 1, int pageSize = 20)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var url = $"api/users?search={Uri.EscapeDataString(search ?? "")}&page={page}&pageSize={pageSize}";
        if (role.HasValue) url += $"&role={role.Value}";

        var result = await api.GetFromJsonAsync<UsersPageVM>(url) ?? new UsersPageVM();
        // keep the query in the VM so the view retains filters
        result.Search = search;
        result.Role = role;
        return View(result);
    }

    // GET /AdminUsers/Edit/5
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var dto = await api.GetFromJsonAsync<UserEditVM>($"api/users/{id}");
        if (dto is null) return NotFound();

        return View(dto);
    }

    // POST /AdminUsers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(UserEditVM vm)
    {
        if (!ModelState.IsValid) return View(vm);

        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        // API Update DTO (no email here; API doesn't change it on PUT)
        var payload = new
        {
            firstName = vm.FirstName,
            lastName = vm.LastName,
            role = vm.Role,
            groupId = vm.GroupId,
            isFirstLogin = vm.IsFirstLogin
        };

        var resp = await api.PutAsJsonAsync($"api/users/{vm.Id}", payload);
        if (!resp.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await resp.Content.ReadAsStringAsync());
            return View(vm);
        }

        TempData["Msg"] = "User saved.";
        return RedirectToAction(nameof(Edit), new { id = vm.Id });
    }

    // POST /AdminUsers/Delete/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/users/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await api.SendAsync(req);

        if (!resp.IsSuccessStatusCode)
        {
            TempData["Err"] = await resp.Content.ReadAsStringAsync();
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["Msg"] = "User deleted.";
        return RedirectToAction(nameof(Index));
    }
}
