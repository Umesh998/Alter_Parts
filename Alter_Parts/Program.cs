//using Alter_Parts.Data;
//using Alter_Parts.Services;
//using Microsoft.EntityFrameworkCore;


//var builder = WebApplication.CreateBuilder(args);


//// This must be set before any Excel operations occur
//// Use the full path so C# knows exactly which 'LicenseType' we mean
//OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("Alter Parts");

//// Add services to the container.
//builder.Services.AddControllersWithViews();
//// Add this near your other builder.Services lines
////builder.Services.AddHttpClient<NexarService>();
////Add these alongside your existing registrations
//builder.Services.AddHttpClient<DigiKeyService>();
//builder.Services.AddHttpClient<MouserService>();
//builder.Services.AddHttpClient<LCSCService>();
//builder.Services.AddScoped<PartLookupService>();
//builder.Services.AddMemoryCache(); // ← ADD THIS
//builder.Services.AddSession();     // ← ADD THIS if not already there

//builder.Services.AddScoped<ExcelExportService>();

//IConfiguration configuration = new ConfigurationBuilder()
//    .AddJsonFile("appsettings.json")
//    .Build();

//builder.Services.AddDbContext<DB>(options =>
//    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

//var app = builder.Build();

//// Configure the HTTP request pipeline.
//if (!app.Environment.IsDevelopment())
//{
//    app.UseExceptionHandler("/Home/Error");
//    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
//    app.UseHsts();
//}

//app.UseHttpsRedirection();
//app.UseStaticFiles();

//app.UseRouting();

//app.UseAuthorization();

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Component}/{action=More_Fruits}/{id?}");

//app.Run();











using Alter_Parts.Data;
using Alter_Parts.Models;
using Alter_Parts.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// This must be set before any Excel operations occur
OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("Alter Parts");

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── Your existing HTTP clients & services ─────────────────────────────────────
builder.Services.AddHttpClient<DigiKeyService>();
builder.Services.AddHttpClient<MouserService>();
builder.Services.AddHttpClient<LCSCService>();
builder.Services.AddScoped<PartLookupService>();
builder.Services.AddMemoryCache();
builder.Services.AddSession();
builder.Services.AddScoped<ExcelExportService>();

// ── Configuration ─────────────────────────────────────────────────────────────
IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

// ── Database ──────────────────────────────────────────────────────────────────
// DB must now inherit from IdentityDbContext<ApplicationUser> — update that class
builder.Services.AddDbContext<DB>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));



// ── Identity ──────────────────────────────────────────────────────────────────
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Password complexity
        options.Password.RequiredLength = 12;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireDigit = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredUniqueChars = 4;

        // Lockout
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // User
        options.User.RequireUniqueEmail = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<DB>()           // ← uses your existing DB context
    .AddDefaultTokenProviders();

// ── Application cookie ────────────────────────────────────────────────────────
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Name = "__Host-AlterParts-Auth";
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── HTTP pipeline ─────────────────────────────────────────────────────────────
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseSession();         // ← keep before Auth if you use session-based state

app.UseAuthentication();  // ← ADDED — must come before UseAuthorization
app.UseAuthorization();   // ← already existed

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Component}/{action=More_Fruits}/{id?}");

// ── Seed roles on startup ─────────────────────────────────────────────────────
await SeedRolesAsync(app.Services);

app.Run();

// ─────────────────────────────────────────────────────────────────────────────
static async Task SeedRolesAsync(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    foreach (var role in new[] { "Admin", "Manager", "User" })
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }
}