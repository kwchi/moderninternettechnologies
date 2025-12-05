using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using WebApplication1.Models;
using WebApplicationData.Data;
using WebApplicationData.Interfaces;
using WebApplicationData.Models.Configurations;
using WebApplicationData.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Localization;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IWebRepository _repository;
        private readonly MyConfiguration _config;
        private readonly IStringLocalizer<HomeController> _localizer;

        public HomeController(ILogger<HomeController> logger, IWebRepository repository, MyConfiguration config, IStringLocalizer<HomeController> localizer)
        {
            _logger = logger;
            _repository = repository;
            _config = config;
            _localizer = localizer;
        }

        [AllowAnonymous]
        public async Task<IActionResult> IndexAsync()
        {
            ViewData["MessageFromController"] = _localizer["WelcomeMessage"];
            var users = await _repository.ReadAll<WebApplicationUser>().ToListAsync();
            return View(users);
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
