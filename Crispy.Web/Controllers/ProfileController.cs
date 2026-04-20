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
        private readonly UserManager<User> _userManager;

        public ProfileController(IRecipeService recipeService, UserManager<User> userManager)
            : base(userManager)
        {
            _recipeService = recipeService;
            _userManager = userManager;
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

        // Новий метод для перегляду профілю іншого користувача
        [HttpGet("Profile/User/{id}")]
        [Authorize] // Або залиште [Authorize], якщо тільки зареєстровані можуть дивитись
        public async Task<IActionResult> UserProfile(int id)
        {
            var userProfile = await _userManager.FindByIdAsync(id.ToString());
            if (userProfile == null)
                return NotFound();

            var currentUser = await GetCurrentUserAsync();
            bool isFollowing = false;

            if (currentUser != null && currentUser.Id != id)
            {
                isFollowing = await _recipeService.IsFollowingUserAsync(currentUser.Id, id);
            }

            var userRecipes = await _recipeService.GetUserRecipesAsync(id);

            var model = new UserProfileViewModel
            {
                 // Якщо ви не маєте UserId у в'ю моделі, додайте його туди:
                 UserId = id, 
                 UserName = userProfile.UserName ?? "Шеф-кухар",
                 Email = userProfile.Email ?? "",
                 MyRecipes = userRecipes.ToList(),
                 IsFollowing = isFollowing,
                 IsOwnProfile = (currentUser?.Id == id)
            };

            return View("Index", model); // Можна відмалювати те ж саме представлення
        }

        // Action для підписки/відписки
        [HttpPost]
        public async Task<IActionResult> ToggleFollow(int followedUserId, string? returnUrl = null)
        {
            var currentUserId = int.Parse(_userManager.GetUserId(User)!);
            
            await _recipeService.ToggleFollowUserAsync(currentUserId, followedUserId);
            
            // Якщо прийшли зі сторінки детального рецепту, повертаємось туди
            if (!string.IsNullOrEmpty(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }
            
            // Інакше повертаємось на сторінку профілю
            return RedirectToAction("Details", new { id = followedUserId });
        }
    }
}