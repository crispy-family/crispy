using System.Diagnostics;
using Crispy.Application.Interfaces;
using Crispy.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IRecipeService _recipeService;

        public HomeController(IRecipeService recipeService)
        {
            _recipeService = recipeService;
        }

        public async Task<IActionResult> Index()
        {
            var recipes = await _recipeService.GetAllRecipesAsync();
            return View(recipes); 
        }

        public IActionResult Privacy()
        {
            return View();
        }
    }
}
