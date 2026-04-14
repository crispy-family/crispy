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
        public async Task<IActionResult> Index(string? searchQuery, int? categoryId)
        {
            ViewBag.Categories = await _recipeService.GetCategoriesAsync();
            ViewBag.SelectedCategory = categoryId;
            ViewBag.SearchQuery = searchQuery;

            IEnumerable<Recipe> recipes;

            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                recipes = await _recipeService.SearchRecipesAsync(searchQuery);
            }
            else
            {
                recipes = await _recipeService.GetAllRecipesAsync();
            }

            // Застосовуємо фільтр по категорії, якщо клікнули на категорію
            if (categoryId.HasValue)
            {
                recipes = recipes.Where(r => r.CategoryId == categoryId.Value);
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