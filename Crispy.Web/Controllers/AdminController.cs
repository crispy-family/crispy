using Crispy.Core.Entities;
using Crispy.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Crispy.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : BaseApiController
    {
        private readonly UserManager<User> _userManager;
        private readonly CrispyDbContext _context; // Використовуємо контекст або Service для адмінських задач

        public AdminController(UserManager<User> userManager, CrispyDbContext context)
            : base(userManager)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userManager.Users.ToListAsync();
            return View(users);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user != null)
            {
                // Для уникнення видалення останнього адміна можна зробити перевірку
                var isAdmin = await _userManager.IsInRoleAsync(user, "Admin");
                if (isAdmin)
                {
                    var adminsCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
                    if (adminsCount <= 1)
                    {
                        TempData["Error"] = "Неможливо видалити єдиного адміністратора.";
                        return RedirectToAction(nameof(Index));
                    }
                }

                await _userManager.DeleteAsync(user);
            }
            return RedirectToAction(nameof(Index));
        }

        // Тут також можна додати методи для видалення рецептів та коментарів
        // ...
    }
}