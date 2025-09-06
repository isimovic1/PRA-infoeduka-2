using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Security.Claims;
using WebApp.ViewModels;
using WebApp.ViewModels.Auth;
using System.Net.Http.Json;

namespace WebApp.Controllers
{
    public class AuthController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _ctx;

        public AuthController(IHttpClientFactory httpClientFactory, IHttpContextAccessor ctx)
        {
            _httpClientFactory = httpClientFactory;
            _ctx = ctx;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login() => View();

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM model)
        {
            if (!ModelState.IsValid) return View(model);

            var api = _httpClientFactory.CreateClient("API");
            var resp = await api.PostAsJsonAsync("api/auth/login", new { email = model.Email, password = model.Password });
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Invalid credentials.");
                return View(model);
            }

            var login = await resp.Content.ReadFromJsonAsync<LoginResultVM>();
            if (login is null || string.IsNullOrWhiteSpace(login.token))
            {
                ModelState.AddModelError("", "Login failed.");
                return View(model);
            }

            // keep JWT for API calls (server-to-server)
            HttpContext.Session.SetString("API_JWT", login.token);

            // fetch /me (needs Bearer)
            var meReq = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", login.token);
            var meResp = await api.SendAsync(meReq);
            if (!meResp.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Failed to retrieve profile.");
                return View(model);
            }
            var me = await meResp.Content.ReadFromJsonAsync<UserDto>();

            // sign-in cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, login.email),
                new Claim(ClaimTypes.Role, login.role),
                new Claim("IsFirstLogin", me!.IsFirstLogin ? "true" : "false"),
                new Claim("FullName", $"{me.FirstName} {me.LastName}"),
                new Claim("GroupId", me.GroupId?.ToString() ?? "")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            if (me.IsFirstLogin) return RedirectToAction("ChangePassword");
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register() => View(new RegisterVM());

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            // Student must have GroupId; Admin/Professor must not
            if (vm.Role == 0 && vm.GroupId is null)
                ModelState.AddModelError(nameof(vm.GroupId), "GroupId is required for students.");
            if (vm.Role != 0)
                vm.GroupId = null;

            if (!ModelState.IsValid)
                return View(vm);

            var api = _httpClientFactory.CreateClient("API");

            var payload = new
            {
                email = vm.Email,
                firstName = vm.FirstName,
                lastName = vm.LastName,
                role = vm.Role,
                groupId = vm.GroupId, // null if not student
                password = vm.Password
            };

            var resp = await api.PostAsJsonAsync("api/auth/register", payload);
            if (!resp.IsSuccessStatusCode)
            {
                // bubble up API validation/errors to the view
                var detail = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Registration failed: {detail}");
                return View(vm);
            }

            TempData["Msg"] = "Registration successful. Please log in.";
            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordVM());

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var token = HttpContext.Session.GetString("API_JWT");
            if (string.IsNullOrWhiteSpace(token))
            {
                await Logout();
                return RedirectToAction("Login");
            }

            var api = _httpClientFactory.CreateClient("API");
            var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password")
            {
                Content = JsonContent.Create(new { oldPassword = vm.OldPassword, newPassword = vm.NewPassword })
            };
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await api.SendAsync(req);
            if (!resp.IsSuccessStatusCode)
            {
                var detail = await resp.Content.ReadAsStringAsync();
                ModelState.AddModelError("", $"Greška: {detail}");
                return View(vm);
            }

            // refresh claims from /me (IsFirstLogin should now be false)
            var meReq = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var meResp = await api.SendAsync(meReq);
            meResp.EnsureSuccessStatusCode();
            var me = await meResp.Content.ReadFromJsonAsync<UserDto>();

            // re-issue cookie with IsFirstLogin=false
            await HttpContext.SignOutAsync();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, me!.Email),
                new Claim(ClaimTypes.Role, me.RoleName),
                new Claim("IsFirstLogin", "false"),
                new Claim("FullName", $"{me.FirstName} {me.LastName}"),
                new Claim("GroupId", me.GroupId?.ToString() ?? "")
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

            TempData["Msg"] = "Password changed.";
            return RedirectToAction("Index", "Home");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Remove("API_JWT");
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => Content("Access denied");
    }
}
