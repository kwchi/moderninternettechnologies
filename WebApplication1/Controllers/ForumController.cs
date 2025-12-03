using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers
{
    [Authorize(Policy = "ForumPolicy")]
    public class ForumController : Controller
    {
        public IActionResult Index()
        {
            return Content("Ласкаво просимо на Форум! Ви маєте одне з необхідних прав доступу.");
        }
    }
}