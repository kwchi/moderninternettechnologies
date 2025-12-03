using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebApplicationData.Data;
using WebApplicationData.Interfaces;
using WebApplicationData.Repositories;

namespace WebApplication1.Controllers
{
    [Authorize]
    public class ResourceController : Controller
    {
        private readonly IWebRepository _repository;
        private readonly IAuthorizationService _authorizationService;
        private readonly UserManager<WebApplicationUser> _userManager;

        public ResourceController(
            IWebRepository repository,
            IAuthorizationService authorizationService,
            UserManager<WebApplicationUser> userManager)
        {
            _repository = repository;
            _authorizationService = authorizationService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var resources = await _repository.ReadAll<AppResource>().Include(r => r.Author).ToListAsync();
            return View(resources);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(AppResource model)
        {
            var user = await _userManager.GetUserAsync(User);

            model.AuthorId = user.Id;

            await _repository.AddAsync(model);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Edit(int id)
        {
            var resource = await _repository.ReadSingleAsync<AppResource>(r => r.Id == id);

            if (resource == null)
            {
                return NotFound();
            }


            var authorizationResult = await _authorizationService
                .AuthorizeAsync(User, resource, "ResourceOwner");

            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            return View(resource);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(AppResource model)
        {
            var resource = await _repository.SingleAsync<AppResource>(r => r.Id == model.Id);

            if (resource == null) return NotFound();

            var authorizationResult = await _authorizationService
                .AuthorizeAsync(User, resource, "ResourceOwner");

            if (!authorizationResult.Succeeded)
            {
                return Forbid();
            }

            resource.Title = model.Title;

            await _repository.UpdateAsync(resource);

            return RedirectToAction("Index");
        }
    }
}