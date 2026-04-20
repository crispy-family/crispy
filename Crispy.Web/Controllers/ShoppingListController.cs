using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Crispy.Core.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Crispy.Web.Controllers
{
    [Authorize] 
    public class ShoppingListController : Controller
    {
        private readonly IRecipeService _recipeService;
        private readonly UserManager<User> _userManager;

        public ShoppingListController(IRecipeService recipeService, UserManager<User> userManager)
        {
            _recipeService = recipeService;
            _userManager = userManager;
        }

        // Відображає список
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var items = await _recipeService.GetUserShoppingListAsync(user.Id);
            return View(items);
        }

        // Перемикає галочку (Куплено / Не куплено)
        [HttpPost]
        public async Task<IActionResult> ToggleItem(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _recipeService.ToggleShoppingItemStatusAsync(id, user.Id);
            }
            return RedirectToAction("Index");
        }

        // Очищає всі куплені товари
        [HttpPost]
        public async Task<IActionResult> ClearBought()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                await _recipeService.ClearBoughtShoppingItemsAsync(user.Id);
            }
            return RedirectToAction("Index");
        }
    }
}