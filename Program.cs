using Microsoft.EntityFrameworkCore;
using SDP1.Data;
using SDP1.Helpers;
using SDP1.Hubs;
using SDP1.Models;
using SDP1.Services;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

builder.Services.AddHttpClient<IAIService, GeminiAIService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();

    var admin = db.Admins.FirstOrDefault(a => a.Email == "admin@abilityconnect.com");

    if (admin == null)
    {
        admin = new Admin
        {
            Email = "admin@abilityconnect.com",
            PasswordHash = PasswordHelper.HashPassword("Admin@123"),
            FullName = "Administrator",
            CreatedAt = DateTime.UtcNow
        };
        db.Admins.Add(admin);
        db.SaveChanges();
        Console.WriteLine("=== Admin Created with hashed password ===");
    }
    else if (!admin.PasswordHash.StartsWith("$2"))
    {
        admin.PasswordHash = PasswordHelper.HashPassword("Admin@123");
        admin.UpdatedAt = DateTime.UtcNow;
        db.SaveChanges();
        Console.WriteLine("=== Admin Password Hash Updated ===");
    }
    else
    {
        bool isValid = PasswordHelper.VerifyPassword("Admin@123", admin.PasswordHash);
        Console.WriteLine($"=== Admin hash verify: {isValid} ===");

        if (!isValid)
        {
            admin.PasswordHash = PasswordHelper.HashPassword("Admin@123");
            admin.UpdatedAt = DateTime.UtcNow;
            db.SaveChanges();
            Console.WriteLine("=== Admin Password Reset ===");
        }
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<VolunteerChatHub>("/hubs/volunteerChat");

app.Run();