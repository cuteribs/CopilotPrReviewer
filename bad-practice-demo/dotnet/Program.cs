// [PROJ-5] Multiple public types in single file
// [PROJ-7] Inconsistent member ordering throughout
// [SEC-10] Missing HTTP security headers
// [CONFIG-1] Direct IConfiguration injection everywhere
// [ARCH-1] Captive dependencies, wrong DI lifetimes
// [SEC-7] CORS misconfiguration: AllowAnyOrigin + AllowCredentials
// [CLEAN-4] Business logic in presentation layer
// [SER-3] Newtonsoft default settings, TypeNameHandling.All
// [MODERN] Not using modern .NET 8+ features

using App;
using App.BackgroundServices;
using App.Middleware;
using App.Models;
using App.Repositories;
using App.Services;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

var builder = WebApplication.CreateBuilder(args);

// [SEC-7] CORS: AllowAnyOrigin + AllowCredentials = Critical vulnerability
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// [ARCH-1] Captive dependency: Singleton depending on Scoped DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseInMemoryDatabase("BadPracticeDb"));

// [ARCH-1] Wrong lifetime registrations
builder.Services.AddSingleton<UserService>();       // Should be scoped (uses DbContext)
builder.Services.AddTransient<App.Repositories.UserRepository>();     // Should be scoped
builder.Services.AddSingleton<EmailService>();       // Singleton with scoped dependency
builder.Services.AddSingleton<GenericRepository>();  // Singleton with scoped dependency

// [CONFIG-1] No Options pattern, direct IConfiguration injection
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);

// [SER-3][SEC-5] Insecure Newtonsoft settings + TypeNameHandling.All = RCE risk
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.TypeNameHandling = TypeNameHandling.All;
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Serialize;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// [BG-SVC] Background service with issues
builder.Services.AddHostedService<DataSyncService>();

var app = builder.Build();

// [MIDDLEWARE] Wrong middleware order - Auth before Exception handling!
// Correct order: Exception → HSTS → HTTPS → Static → Routing → CORS → Auth → Authz → Custom → Endpoints
app.UseAuthentication();        // Too early!
app.UseAuthorization();         // Too early!
app.UseCors();                  // Wrong position
app.UseMiddleware<BusinessLogicMiddleware>();  // Custom before routing
app.UseRouting();               // Should be before CORS/Auth

// [SEC-10] No HSTS, no HTTPS redirection, no security headers

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    // [API-2] Developer exception page exposes stack traces
    app.UseDeveloperExceptionPage();
}

app.MapControllers();
app.Run();

// [PROJ-5] Multiple types in same file
// [CLEAN-3] DbContext in presentation layer
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<User> Users { get; set; }
    public DbSet<Order> Orders { get; set; }
    // [DA-3] No index configuration
    // No OnModelCreating override for configurations
}

// [PROJ-5] Yet another type in Program.cs
public static class GlobalState
{
    // [CORR-3][ANTI-PATTERN: Static abuse] Shared mutable static state
    public static List<string> ActiveUsers = new List<string>();
    public static Dictionary<string, object> Cache = new Dictionary<string, object>();
    public static int RequestCount = 0;
}
