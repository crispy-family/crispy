using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Crispy.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Crispy.Web.Controllers
{
    [Authorize]
    public class RecipeController : BaseApiController
    {
        private readonly IRecipeService _recipeService;
        private readonly IWebHostEnvironment _webHostEnvironment;

        // Інжектимо IWebHostEnvironment для збереження файлів
        public RecipeController(IRecipeService recipeService, UserManager<User> userManager, IWebHostEnvironment webHostEnvironment)
            : base(userManager)
        {
            _recipeService = recipeService;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var categories = await _recipeService.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRecipeViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var categories = await _recipeService.GetCategoriesAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(model);
            }

            var user = await GetCurrentUserAsync();
            if (user == null)
                return Challenge();

            // Логіка збереження картинки
            string? uniqueFileName = null;
            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "recipes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Використовуємо Guid, щоб запобігти збігу імен
                uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }
                
                uniqueFileName = "/images/recipes/" + uniqueFileName;
            }

            // В кінці передаємо model.CategoryId
            var success = await _recipeService.CreateRecipeAsync(model.Title, model.Description, user.Id, uniqueFileName, model.CategoryId);

            if (success)
                return RedirectToAction("Index", "Home");

            return ViewWithError(model, "Помилка при створенні рецепту.");
        }

        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int recipeId)
        {
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Challenge();

            await _recipeService.ToggleFavoriteAsync(user.Id, recipeId);
            return RedirectToReferer();
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _recipeService.GetRecipeByIdAsync(id);
            if (recipe == null)
                return NotFound();

            return View(recipe);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var user = await GetCurrentUserAsync();
            var recipe = await _recipeService.GetRecipeByIdAsync(id);

            if (recipe == null)
                return NotFound();

            if (!IsOwner(recipe.UserId))
                return Forbid();

            return View(recipe);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string title, string description)
        {
            var user = await GetCurrentUserAsync();
            var success = await _recipeService.UpdateRecipeAsync(id, title, description, user!.Id);

            if (!success)
                return Forbid();

            return RedirectToAction("Index", "Profile");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Delete(int id)
        {
            var recipe = await _recipeService.GetRecipeByIdAsync(id);
            if (recipe == null) return NotFound();

            var currentUserId = GetCurrentUserId();
            var isAdmin = User.IsInRole("Admin");

            // Видалити може тільки автор або Адмін
            if (recipe.UserId != currentUserId && !isAdmin)
            {
                return Forbid(); // 403 Forbidden
            }

            // Передаємо параметр isAdmin у сервіс, якщо потрібно
            await _recipeService.DeleteRecipeAsync(id, currentUserId, isAdmin);
            
            // Якщо видаляє адмін, краще редиректити на होم або попередню сторінку,
            // оскільки адмін може видаляти з головної або з профілю іншого користувача
            if (isAdmin && recipe.UserId != currentUserId)
            {
                return RedirectToAction("Index", "Home");
            }
            
            return RedirectToAction("Index", "Profile");
        }
    }
}
