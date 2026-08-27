using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.EntityFrameworkCore;
using FurShield.Data;
using FurShield.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC services
builder.Services.AddControllersWithViews();

// Dev-only: use in-memory (ephemeral) Data Protection keys.
// This means every time the app restarts, previously issued login/auth
// cookies automatically become invalid - so you always start from a
// logged-out state instead of staying logged in from a previous run,
// and it avoids stale-token errors from old cookies. This should stay
// Development-only; production apps need persisted keys.
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDataProtection()
        .SetApplicationName("FurShield")
        .UseEphemeralDataProtectionProvider();
}

// Adds "Views/Partials" as an additional Razor view-location
builder.Services.Configure<RazorViewEngineOptions>(options =>
{
    options.ViewLocationFormats.Add("/Views/Partials/{0}.cshtml");
});

// Database context + connection string registration
builder.Services.AddDbContext<PetShelterContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("PetShelterContext")));

// Identity, using ApplicationUser instead of the default IdentityUser
builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<PetShelterContext>();

// Background service: daily vaccination/follow-up due-date reminder notifications
builder.Services.AddHostedService<VaccinationReminderService>();

var app = builder.Build();

// Roles + Admin seeding
using (var scope = app.Services.CreateScope())
{
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    // FurShield Project ke Tamam Roles Create Honge
    string[] roles = { "Admin", "PetOwner", "Veterinarian", "Shelter" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole(role));
    }

    // Sirf Admin Default Seed User
    string adminEmail = "admin@demo.com";
    string adminpass = "Admin@123";

    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(adminUser, adminpass);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<PetShelterContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.Run();
