using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net.Http.Headers;
using WebApp.Filters;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using System.Linq;


var builder = WebApplication.CreateBuilder(args);

// MVC + global first-login redirect filter
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<FirstLoginRedirectFilter>();
})
.ConfigureApplicationPartManager(apm =>
{
    // Remove the WebAPI assembly so MVC doesn't discover its controllers in the WebApp
    var apiPart = apm.ApplicationParts.FirstOrDefault(p => p.Name.Equals("WebAPI", StringComparison.OrdinalIgnoreCase));
    if (apiPart != null) apm.ApplicationParts.Remove(apiPart);
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();



// HttpClient for the API (base URL from appsettings.json)
builder.Services.AddHttpClient("API", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]!);
});

// Cookie auth for the WebApp UI
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
