using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Crispy.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    [Authorize]
    public class ProfileController : BaseApiController
    {
        private readonly IRecipeService _recipeService;

        public ProfileController(IRecipeService recipeService, UserManager<User> userManager)
            : base(userManager)
        {
            _recipeService = recipeService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Challenge();

            var userRecipes = await _recipeService.GetUserRecipesAsync(user.Id);
            var favoriteRecipes = await _recipeService.GetFavoriteRecipesAsync(user.Id);

            var model = new UserProfileViewModel
            {
                UserName = user.UserName ?? "Шеф-кухар",
                Email = user.Email ?? "",
                MyRecipes = userRecipes.ToList(),
                FavoriteRecipes = favoriteRecipes.ToList()  
            };

            return View(model);
        }
    }
}