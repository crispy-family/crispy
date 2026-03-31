using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Crispy.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    [Authorize] 
    public class RecipeController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly UserManager<User> _userManager;

        public RecipeController(IRecipeService recipeService, UserManager<User> userManager)
        {
            _recipeService = recipeService;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Create() => View();

        [HttpPost]
        public async Task<IActionResult> Create(CreateRecipeViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge(); 

            var success = await _recipeService.CreateRecipeAsync(model.Title, model.Description, user.Id);

            if (success)
            {
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, "Помилка при створенні рецепту.");
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> ToggleFavorite(int recipeId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            await _recipeService.ToggleFavoriteAsync(user.Id, recipeId);

            // Повертаємо користувача на ту сторінку, звідки він натиснув кнопку
            string referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? "/" : referer);
        }
    }
}
