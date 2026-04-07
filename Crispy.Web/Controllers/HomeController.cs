using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    public class HomeController : BaseApiController
    {
        private readonly IRecipeService _recipeService;

        public HomeController(IRecipeService recipeService, UserManager<User> userManager)
            : base(userManager)
        {
            _recipeService = recipeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? searchQuery)
        {
            IEnumerable<Recipe> recipes;

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                recipes = await _recipeService.SearchRecipesAsync(searchQuery);
                ViewBag.SearchQuery = searchQuery;
            }
            else
            {
                recipes = await _recipeService.GetAllRecipesAsync();
            }

            // Фільтруємо власні рецепти поточного користувача
            if (User.Identity!.IsAuthenticated && string.IsNullOrWhiteSpace(searchQuery))
            {
                var currentUserId = GetCurrentUserId();
                if (currentUserId > 0)
                {
                    recipes = recipes.Where(r => r.UserId != currentUserId);
                }
            }

            return View(recipes);
        }

        [HttpGet]
        public IActionResult Privacy() => View();
    }
}