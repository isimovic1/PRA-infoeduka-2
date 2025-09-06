using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using WebAPI.Mappings;
using WebAPI.Models;
using WebAPI.Swagger;
using WebAPI.Security;
using WebAPI.Filters;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers(options =>
{
    options.Filters.Add<FirstLoginProblemDetailsFilter>();
})
.AddControllersAsServices();// enables [FromServices] FileAssetsController
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddScoped<WebAPI.Services.INotificationService, WebAPI.Services.NotificationService>(); //Za service notifikacije

// Swagger + JWT + IFormFile support
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Infoeduka2 API", Version = "v1" });

    // JWT "Authorize" button
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    // Render IFormFile as a real file input in Swagger UI
    c.MapType<IFormFile>(() => new OpenApiSchema
    {
        Type = "string",
        Format = "binary"
    });
  
    c.OperationFilter<FileUploadOperationFilter>();
});

// DbContext
builder.Services.AddDbContext<Infoeduka2Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("ConnString")));

// JWT auth
var jwt = builder.Configuration.GetSection("Jwt");
var keyBytes = Encoding.UTF8.GetBytes(jwt["Key"] ?? throw new InvalidOperationException("Jwt:Key missing."));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name
        };
    });

// AutoMapper (you’re using the DI extension)
builder.Services.AddAutoMapper(typeof(EntityToDtoProfile));

//builder.Services.AddAuthorization();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NotFirstLogin", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new NotFirstLoginRequirement());
    });
});

builder.Services.AddScoped<IAuthorizationHandler, NotFirstLoginHandler>();

var app = builder.Build();

// Swagger UI
app.UseSwagger();
app.UseSwaggerUI();

// Auth middlewares
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
