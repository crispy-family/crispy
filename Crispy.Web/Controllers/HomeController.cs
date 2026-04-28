using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Crispy.Web.Filters;

namespace Crispy.Web.Controllers
{
    public class HomeController : BaseApiController
    {
        private readonly IRecipeService _recipeService;
        private readonly UserManager<User> _userManager;
        private readonly IMealDbClient _mealDbClient;

        public HomeController(IRecipeService recipeService, UserManager<User> userManager, IMealDbClient mealDbClient)
            : base(userManager)
        {
            _recipeService = recipeService;
            _userManager = userManager;
            _mealDbClient = mealDbClient;
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
        [IpRateLimitFilter(MaxRequests = 30, TimeWindowInSeconds = 60)] // До 30 оновлень стрічки на хвилину
        public async Task<IActionResult> Feed()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Challenge();

            var feedRecipes = await _recipeService.GetUserFeedAsync(user.Id);
            return View(feedRecipes);
        }

        // Тестовий метод для перевірки API1
        [HttpGet]
        public async Task<IActionResult> TestExternalApi()
        {
            var recipeJson = await _mealDbClient.GetRandomRecipeAsync();
            return Content(recipeJson, "application/json"); // Повертаємо як звичайний текст/json
        }

        public IActionResult RateLimitExceeded()
        {
            // Не забудьте створити просту сторінку RateLimitExceeded.cshtml у Views/Home/
            // з текстом "Ви робите запити занадто часто. Зачекайте хвилину."
            return View();
        }
    }
}