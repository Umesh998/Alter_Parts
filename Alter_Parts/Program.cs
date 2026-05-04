using Alter_Parts.Data;
using Alter_Parts.Services;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);


// This must be set before any Excel operations occur
// Use the full path so C# knows exactly which 'LicenseType' we mean
OfficeOpenXml.ExcelPackage.License.SetNonCommercialOrganization("Alter Parts");

// Add services to the container.
builder.Services.AddControllersWithViews();
// Add this near your other builder.Services lines
//builder.Services.AddHttpClient<NexarService>();
//Add these alongside your existing registrations
builder.Services.AddHttpClient<DigiKeyService>();
builder.Services.AddHttpClient<MouserService>();
builder.Services.AddHttpClient<LCSCService>();
builder.Services.AddScoped<PartLookupService>();
builder.Services.AddMemoryCache(); // ← ADD THIS
builder.Services.AddSession();     // ← ADD THIS if not already there

builder.Services.AddScoped<ExcelExportService>();

IConfiguration configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .Build();

builder.Services.AddDbContext<DB>(options =>
    options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Component}/{action=More_Fruits}/{id?}");

app.Run();
