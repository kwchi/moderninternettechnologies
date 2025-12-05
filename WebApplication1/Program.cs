using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplicationData.Data;
using WebApplicationData.Interfaces;
using WebApplicationData.Repositories;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using WebApplicationData.Models.Configurations;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using WebApplication1.Authorization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

//1

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("sharedsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>();
}

//2


var myConfig = builder.Configuration.Get<MyConfiguration>();
if (myConfig == null)
{
    throw new InvalidOperationException("Configuration object 'MyConfiguration' could not be loaded.");
}
builder.Services.AddSingleton(myConfig);

//3

var connectionString = myConfig.ConnectionStrings?.DefaultConnection
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found in configuration.");

builder.Services.AddDbContext<WebApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));


builder.Services.AddDatabaseDeveloperPageExceptionFilter();


builder.Services.AddDefaultIdentity<WebApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false; 
})
.AddEntityFrameworkStores<WebApplicationDbContext>();


builder.Services.AddScoped<IWebRepository, WebRepository>();


//builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

//6

builder.Services.AddRateLimiter(options =>
{ 
    options.OnRejected = async (context, _) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsync("Too many requests. Please try again later.");
    };

options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
{
    if (httpContext.User.Identity?.IsAuthenticated == true)
    {
        var userId = httpContext.User.Identity?.Name ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"user:{userId}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    }
    else
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: $"ip:{ip}",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    }
});
});

builder.Services.AddSingleton<IAuthorizationHandler, IsAuthorHandler>();       // Завдання 3
builder.Services.AddSingleton<IAuthorizationHandler, MinHoursHandler>();       // Завдання 4
builder.Services.AddSingleton<IAuthorizationHandler, ForumAccessHandler>();    // Завдання 5

builder.Services.AddAuthorization(options =>
{
    // Л4  2
    options.AddPolicy("ArchivePolicy", policy =>
        policy.RequireClaim("IsVerifiedClient"));

    // Л4 3 
    options.AddPolicy("ResourceOwner", policy =>
        policy.Requirements.Add(new IsAuthorRequirement()));

    // Л4 4 
    options.AddPolicy("PremiumContent", policy =>
        policy.Requirements.Add(new MinHoursRequirement(100)));

    // Л4 5
    options.AddPolicy("ForumPolicy", policy =>
        policy.Requirements.Add(new ForumAccessRequirement()));
});

builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[]
    {
        new CultureInfo("en-US"), // Англійська (США)
        new CultureInfo("uk-UA"), // Українська
        new CultureInfo("es")     // Іспанська
    };

    options.DefaultRequestCulture = new RequestCulture("en-US");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

builder.Services.AddControllersWithViews(options =>
{
    var policy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
    options.Filters.Add(new AuthorizeFilter(policy));
})
.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
.AddDataAnnotationsLocalization();

var app = builder.Build();

app.UseRequestLocalization();

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
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
app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages()
    //.RequireRateLimiting("PartitionedPolicy")
    .AllowAnonymous();

app.Run();