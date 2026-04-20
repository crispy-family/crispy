using Microsoft.EntityFrameworkCore;
using Crispy.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Crispy.Core.Entities;
using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Serilog;
using Crispy.Infrastructure.Repositories;
using Crispy.Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();



// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<CrispyDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;

    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<Crispy.Infrastructure.Data.CrispyDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.Name = "CrispyAuthCookie";
    options.LoginPath = "/Account/Login"; // Куди відправляти, якщо не авторизований
    options.AccessDeniedPath = "/Account/AccessDenied"; // Куди відправляти, якщо немає прав
    options.ExpireTimeSpan = TimeSpan.FromDays(7); // Скільки пам'ятати користувача
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddScoped<IRecipeService, RecipeService>();

//  Memory Cache
builder.Services.AddMemoryCache();

var app = builder.Build();

// Ініціалізація бази даних та ролей
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await Crispy.Infrastructure.Data.RolesInitializer.InitializeAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding roles the database.");
    }
}

app.UseSerilogRequestLogging();

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

//  middleware для логування часу виконання
app.UseMiddleware<Crispy.Web.Middleware.RequestTimingMiddleware>();

app.UseMiddleware<Crispy.Web.Middleware.RequestLoggingMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

//  middleware для логування запитів
app.UseRequestLogging(); 

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapControllers();

app.Run();
