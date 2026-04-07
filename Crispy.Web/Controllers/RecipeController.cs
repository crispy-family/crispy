using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Crispy.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    [Authorize]
    public class RecipeController : BaseApiController
    {
        private readonly IRecipeService _recipeService;

        public RecipeController(IRecipeService recipeService, UserManager<User> userManager)
            : base(userManager)
        {
            _recipeService = recipeService;
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(CreateRecipeViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Challenge();

            var success = await _recipeService.CreateRecipeAsync(model.Title, model.Description, user.Id);

            if (success)
                return RedirectToAction("Index", "Home");

            return ViewWithError(model, "Помилка при створенні рецепту.");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int recipeId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Challenge();

            await _recipeService.ToggleFavoriteAsync(user.Id, recipeId);
            return RedirectToReferer();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _recipeService.GetRecipeByIdAsync(id);
            if (recipe == null)
                return NotFound();

            return View(recipe);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await GetCurrentUserAsync();
            var recipe = await _recipeService.GetRecipeByIdAsync(id);

            if (recipe == null)
                return NotFound();

            if (!IsOwner(recipe.UserId))
                return Forbid();

            return View(recipe);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string title, string description)
        {
            var user = await GetCurrentUserAsync();
            var success = await _recipeService.UpdateRecipeAsync(id, title, description, user!.Id);

            if (!success)
                return Forbid();

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await GetCurrentUserAsync();
            var success = await _recipeService.DeleteRecipeAsync(id, user!.Id);

            if (!success)
                return Forbid();

            return RedirectToAction("Index", "Profile");
        }
    }
}
