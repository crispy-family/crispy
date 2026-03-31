using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Crispy.Core.Entities;
using Crispy.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    [Authorize] 
    public class ProfileController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly IRecipeService _recipeService;

        public ProfileController(UserManager<User> userManager, IRecipeService recipeService)
        {
            _userManager = userManager;
            _recipeService = recipeService;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var userRecipes = await _recipeService.GetUserRecipesAsync(user.Id);

            var favoriteRecipes = await _recipeService.GetFavoriteRecipesAsync(user.Id);

            var model = new UserProfileViewModel
            {
                UserName = user.UserName ?? "Шеф-кухар",
                Email = user.Email ?? "",
                MyRecipes = userRecipes,
                FavoriteRecipes = favoriteRecipes 
            };

            return View(model);
        }
    }
}