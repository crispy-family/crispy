using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Crispy.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly UserManager<User> _userManager; // Додаємо UserManager

        // Інжектимо обидва сервіси через конструктор
        public HomeController(IRecipeService recipeService, UserManager<User> userManager)
        {
            _recipeService = recipeService;
            _userManager = userManager;
        }

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

            if (User.Identity!.IsAuthenticated && string.IsNullOrWhiteSpace(searchQuery))
            {
                var currentUser = await _userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    recipes = recipes.Where(r => r.UserId != currentUser.Id);
                }
            }

            return View(recipes);
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}