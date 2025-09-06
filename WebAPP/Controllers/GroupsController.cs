using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.Common;
using WebApp.ViewModels.Groups;

[Authorize]  // everyone can view; Admin-only actions are annotated individually
public class GroupsController : Controller
{
    private readonly IHttpClientFactory _http;
    public GroupsController(IHttpClientFactory http) => _http = http;

    private HttpClient ApiWithBearer(out string? token)
    {
        token = HttpContext.Session.GetString("API_JWT");
        var api = _http.CreateClient("API");
        if (!string.IsNullOrWhiteSpace(token))
            api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return api;
    }

    // GET /Groups?q=
    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var list = await api.GetFromJsonAsync<List<GroupVM>>($"api/groups?q={Uri.EscapeDataString(q ?? "")}")
                   ?? new List<GroupVM>();
        ViewBag.Q = q;
        return View(list);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, string? q)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token))
            return RedirectToAction("Login", "Auth");

        // 1) Group
        var group = await api.GetFromJsonAsync<GroupVM>($"api/groups/{id}");
        if (group is null) return NotFound();

        // 2) Members of this group
        var members = await api.GetFromJsonAsync<List<GroupMemberVM>>($"api/groups/{id}/members")
                      ?? new List<GroupMemberVM>();

        // 3) Admin-only: search student candidates to add (uses paged /api/users)
        var candidates = new List<GroupMemberVM>();
        if (User.IsInRole("Admin") && !string.IsNullOrWhiteSpace(q))
        {
            var url = $"api/users?search={Uri.EscapeDataString(q)}&role=0&pageSize=20";
            var page = await api.GetFromJsonAsync<PagedResult<GroupMemberVM>>(url)
                       ?? new PagedResult<GroupMemberVM>();
            candidates = page.Items;
        }

        // 4) Admin-only: other groups to support "Move" dropdown
        var otherGroups = new List<GroupVM>();
        if (User.IsInRole("Admin"))
        {
            var all = await api.GetFromJsonAsync<List<GroupVM>>("api/groups") ?? new List<GroupVM>();
            otherGroups = all.Where(g => g.Id != id).ToList();
        }

        // 5) Compose VM
        var vm = new ManageGroupVM
        {
            Group = group,
            Members = members,
            Q = q,
            SearchResults = candidates,
            OtherGroups = otherGroups
        };

        return View(vm);
    }
    // Admin: create group
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            TempData["Err"] = "Group name is required.";
            return RedirectToAction(nameof(Index));
        }

        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var resp = await api.PostAsJsonAsync("api/groups", new { name });
        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] =
            resp.IsSuccessStatusCode ? "Group created." : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(Index));
    }

    // Admin: delete group
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var req = new HttpRequestMessage(HttpMethod.Delete, $"api/groups/{id}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var resp = await api.SendAsync(req);

        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] =
            resp.IsSuccessStatusCode ? "Group deleted." : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(Index));
    }

    // Admin: add/move student to this group (uses UsersController: PUT /api/users/{id}/group)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddMember(int id, int userId) // id = groupId
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var resp = await api.PutAsJsonAsync($"api/users/{userId}/group", new { groupId = id });
        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] =
            resp.IsSuccessStatusCode ? "Student assigned to group." : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    // Admin: move student from this group to another group
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MoveMember(int id, int userId, int newGroupId)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var resp = await api.PutAsJsonAsync($"api/users/{userId}/group", new { groupId = newGroupId });
        TempData[resp.IsSuccessStatusCode ? "Msg" : "Err"] =
            resp.IsSuccessStatusCode ? "Student moved." : await resp.Content.ReadAsStringAsync();

        return RedirectToAction(nameof(Details), new { id });
    }

    // Admin: remove student from group (NOTE: your API currently requires students to have a GroupId)
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveMember(int id, int userId)
    {
        var api = ApiWithBearer(out var token);
        if (string.IsNullOrWhiteSpace(token)) return RedirectToAction("Login", "Auth");

        var resp = await api.PutAsJsonAsync($"api/users/{userId}/group", new { groupId = (int?)null });
        if (!resp.IsSuccessStatusCode)
        {
            // Your API returns: "Student must have GroupId."
            TempData["Err"] = await resp.Content.ReadAsStringAsync();
        }
        else
        {
            TempData["Msg"] = "Student removed from group.";
        }
        return RedirectToAction(nameof(Details), new { id });
    }
}
