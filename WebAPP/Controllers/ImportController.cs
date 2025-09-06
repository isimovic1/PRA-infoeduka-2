using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using WebApp.ViewModels.Import;

namespace WebApp.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ImportController : Controller
    {
        private readonly IHttpClientFactory _http;
        public ImportController(IHttpClientFactory http) => _http = http;

        private HttpClient ApiWithBearer(out string? token)
        {
            token = HttpContext.Session.GetString("API_JWT");
            var api = _http.CreateClient("API");
            if (!string.IsNullOrWhiteSpace(token))
                api.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return api;
        }

        // GET: /Import/Users
        [HttpGet]
        public IActionResult Users() => View(new UploadUsersVM());

        // POST: /Import/Users
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Users(UploadUsersVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var api = ApiWithBearer(out var token);
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            using var form = new MultipartFormDataContent();
            form.Add(new StreamContent(vm.File.OpenReadStream()), "file", vm.File.FileName);

            var req = new HttpRequestMessage(HttpMethod.Post, "api/import/users")
            {
                Content = form
            };

            var resp = await api.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var detail = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Import failed: {detail}");
                return View(vm);
            }

            var result = await resp.Content.ReadFromJsonAsync<ImportResultVM>();
            return View("UsersResult", result);
        }

        // Optional: quick raw JSON batch view from API
        [HttpGet]
        public async Task<IActionResult> Batch(int id)
        {
            var api = ApiWithBearer(out var token);
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Auth");

            var resp = await api.GetAsync($"api/import/batches/{id}");
            var json = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                TempData["Err"] = json;
                return RedirectToAction("Users");
            }
            return Content(json, "application/json");
        }
    }
}
