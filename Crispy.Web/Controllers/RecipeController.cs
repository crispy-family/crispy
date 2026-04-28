using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Crispy.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Crispy.Web.Filters;

namespace Crispy.Web.Controllers
{
    [Authorize]
    public class RecipeController : BaseApiController
    {
        private readonly IRecipeService _recipeService;
        private readonly IRecipeImportService _recipeImportService;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly UserManager<User> _userManager;

        public RecipeController(
            IRecipeService recipeService,
            IRecipeImportService recipeImportService,
            UserManager<User> userManager,
            IWebHostEnvironment webHostEnvironment)
            : base(userManager)
        {
            _recipeService = recipeService;
            _recipeImportService = recipeImportService;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var categories = await _recipeService.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            return View(new CreateRecipeViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Import(string url)
        {
            var categories = await _recipeService.GetCategoriesAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
            ViewBag.ImportUrl = url;

            if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out _))
            {
                ViewBag.ImportError = "Вкажіть коректне URL-посилання.";
                return View("Create", new CreateRecipeViewModel());
            }

            var imported = await _recipeImportService.ImportAsync(url);
            if (imported == null || string.IsNullOrWhiteSpace(imported.Title))
            {
                ViewBag.ImportError = "Не вдалося імпортувати рецепт із цього посилання.";
                return View("Create", new CreateRecipeViewModel());
            }

            var model = new CreateRecipeViewModel
            {
                Title = imported.Title,
                Description = imported.Description,
                ImageUrl = imported.ImageUrl,
                Ingredients = imported.Ingredients
            };

            return View("Create", model);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRecipeViewModel model)
        {
            // 1. Перевірка валідації
            if (!ModelState.IsValid)
            {
                var categories = await _recipeService.GetCategoriesAsync();
                ViewBag.Categories = new SelectList(categories, "Id", "Name");
                return View(model);
            }

            // 2. Перевірка користувача
            var user = await GetCurrentUserAsync();
            if (user == null)
                return Challenge();

            // 3. Збереження картинки
            string? imagePath = null; // Змінив назву змінної для зручності
            if (model.ImageFile != null)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "images", "recipes");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Path.GetFileName захищає від специфічних символів у назві файлу
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ImageFile.CopyToAsync(fileStream);
                }

                imagePath = "/images/recipes/" + uniqueFileName;
            }
            else if (!string.IsNullOrWhiteSpace(model.ImageUrl) && Uri.TryCreate(model.ImageUrl, UriKind.Absolute, out var imageUri))
            {
                imagePath = imageUri.ToString();
            }

            
            var success = await _recipeService.CreateRecipeAsync(
                model.Title,
                model.Description,
                user.Id,
                model.Servings,    // <--- Передаємо введену кількість порцій
                imagePath,
                model.CategoryId,  
                model.Ingredients  
            );

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

        [AllowAnonymous] // Додаємо, щоб анонімні теж могли дивитись (якщо ви хочете цього)
        [HttpGet]
        [IpRateLimitFilter(MaxRequests = 20, TimeWindowInSeconds = 60)]
        public async Task<IActionResult> Details(int id)
        {
            var recipe = await _recipeService.GetRecipeByIdAsync(id);
            if (recipe == null)
                return NotFound();

            // Завантажуємо коментарі до рецепту та передаємо у ViewBag
            ViewBag.Comments = await _recipeService.GetRecipeCommentsAsync(id);

            // Перевіряємо, чи підписаний поточний користувач на автора рецепту
            bool isFollowing = false;
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                var currentUserIdString = _userManager.GetUserId(User);
                if (int.TryParse(currentUserIdString, out int currentUserId))
                {
                    isFollowing = await _recipeService.IsFollowingUserAsync(currentUserId, recipe.UserId);
                }
            }
            ViewBag.IsFollowing = isFollowing;

            return View(recipe);
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int recipeId, string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                // Якщо коментар порожній, просто повертаємо назад
                return RedirectToAction("Details", new { id = recipeId });
            }

            var currentUserId = GetCurrentUserId(); // Або user.Id якщо у вас вже лежить метод
            
            // Якщо метод GetCurrentUserId() не працює коректно, можна використати вашу стандартну перевірку:
            // var user = await GetCurrentUserAsync();
            // if (user == null) return Challenge();
            // var userId = user.Id;

            await _recipeService.AddCommentAsync(recipeId, currentUserId, text);

            return RedirectToAction("Details", new { id = recipeId });
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

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddToShoppingList(int id, int requestedServings)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Передаємо requestedServings далі в сервіс
            await _recipeService.AddRecipeToShoppingListAsync(id, user.Id, requestedServings);

            TempData["SuccessMessage"] = "🛒 Інгредієнти успішно додані до вашого списку покупок!";

            return RedirectToAction("Details", new { id = id });
        }
    }
}
