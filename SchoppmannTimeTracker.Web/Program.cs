using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Core.Services;
using SchoppmannTimeTracker.Infrastructure.Data;
using SchoppmannTimeTracker.Infrastructure.Repositories;
using SchoppmannTimeTracker.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// DbContext und Identity konfigurieren
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<ApplicationUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Repositories registrieren
builder.Services.AddScoped<ITimeEntryRepository, TimeEntryRepository>();
builder.Services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
builder.Services.AddScoped<IHourlyRateRepository, HourlyRateRepository>();

// Services registrieren
builder.Services.AddScoped<ITimeEntryService, TimeEntryService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IPdfService, PdfService>();
builder.Services.AddScoped<IHourlyRateService, HourlyRateService>();

// Authentifizierung und Autorisierung konfigurieren
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// MVC und Controller konfigurieren
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

//// Gleich nach app.UseAuthorization(); und vor app.MapControllerRoute()
//using (var scope = app.Services.CreateScope())
//{
//    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
//    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

//    // Rollen erstellen
//    if (!await roleManager.RoleExistsAsync("Admin"))
//    {
//        await roleManager.CreateAsync(new IdentityRole("Admin"));
//    }
//    if (!await roleManager.RoleExistsAsync("User"))
//    {
//        await roleManager.CreateAsync(new IdentityRole("User"));
//    }

//    // Admin-Benutzer erstellen
//    var adminUser = await userManager.FindByEmailAsync("gates@microsoft.com");
//    if (adminUser == null)
//    {
//        adminUser = new ApplicationUser
//        {
//            UserName = "gates@microsoft.com",
//            Email = "gates@microsoft.com",
//            EmailConfirmed = true,
//            FirstName = "Bill",
//            LastName = "Gates"
//        };

//        var result = await userManager.CreateAsync(adminUser, "Admin123!");
//        if (result.Succeeded)
//        {
//            await userManager.AddToRoleAsync(adminUser, "Admin");
//        }
//    }
//}

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

app.Run();