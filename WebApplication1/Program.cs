using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplicationData.Data;
using WebApplicationData.Interfaces;
using WebApplicationData.Repositories;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using WebApplicationData.Models.Configurations;
using Microsoft.Extensions.Options;

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


builder.Services.AddControllersWithViews();
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

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();