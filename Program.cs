using Microsoft.AspNetCore.Authentication.Cookies;
using Platform.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<PlatformDbContext>();

builder.Services.AddSingleton<Platform.Models.PlatformData>();

builder.Services.AddScoped<Platform.Services.FileManager>(provider =>
{
    var platformData = provider.GetRequiredService<Platform.Models.PlatformData>();
    var env = provider.GetRequiredService<IWebHostEnvironment>();

    // Вказуємо папку wwwroot/uploads для зберігання картинок
    string uploadPath = Path.Combine(env.WebRootPath ?? env.ContentRootPath, "uploads");

    // Дозволяємо картинки до 5 Мегабайт
    return new Platform.Services.FileManager(
        uploadPath,
        new List<string> { ".jpg", ".jpeg", ".png", ".webp", ".gif" },
        5 * 1024 * 1024,
        platformData
    );
});

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<Platform.Services.NotificationService>();

builder.Services.AddOpenApi();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        options.LoginPath = "/Account/Login";
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    Platform.Data.DatabaseInitializer.SeedData();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
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

app.Run();