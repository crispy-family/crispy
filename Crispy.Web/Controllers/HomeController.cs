using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Crispy.Web.Controllers
{
    public class HomeController : BaseApiController
    {
        private readonly IRecipeService _recipeService;
        private readonly UserManager<User> _userManager;

        public HomeController(IRecipeService recipeService, UserManager<User> userManager)
            : base(userManager)
        {
            _recipeService = recipeService;
            _userManager = userManager;
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

        [Authorize]
        public async Task<IActionResult> Feed()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var feedRecipes = await _recipeService.GetUserFeedAsync(user.Id);
            return View(feedRecipes);
        }
    }
}