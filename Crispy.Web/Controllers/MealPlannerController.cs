using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Crispy.Core.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    [Authorize]
    public class MealPlannerController : BaseApiController
    {
        private readonly IRecipeService _recipeService;

        public MealPlannerController(IRecipeService recipeService, UserManager<User> userManager) 
            : base(userManager)
        {
            _recipeService = recipeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var plans = await _recipeService.GetWeeklyPlanAsync(userId);
            
            // Завантажуємо улюблені рецепти користувача для випадаючого списку при додаванні
            var favoriteRecipes = await _recipeService.GetFavoriteRecipesAsync(userId);
            ViewBag.FavoriteRecipes = favoriteRecipes;

            return View(plans);
        }

        [HttpPost]
        public async Task<IActionResult> Add(int recipeId, string dayOfWeek, string mealType)
        {
            if(recipeId <= 0 || string.IsNullOrEmpty(dayOfWeek) || string.IsNullOrEmpty(mealType))
                return BadRequest("Invalid data");

            // Парсимо енуми
            if(Enum.TryParse<DayOfWeek>(dayOfWeek, out var parsedDay) && 
               Enum.TryParse<MealType>(mealType, out var parsedMeal))
            {
                var userId = GetCurrentUserId();
                await _recipeService.AddMealToPlanAsync(userId, recipeId, parsedDay, parsedMeal);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Remove(int planId)
        {
            var userId = GetCurrentUserId();
            await _recipeService.RemoveMealFromPlanAsync(planId, userId);
            
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> SearchRecipes(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new List<object>());

            var recipes = await _recipeService.SearchRecipesAsync(query);
            
            // Повертаємо лише необхідні поля для мінімізації трафіку
            var result = recipes.Take(10).Select(r => new 
            { 
                id = r.Id, 
                title = r.Title 
            });

            return Json(result);
        }
    }
}
