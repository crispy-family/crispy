using Crispy.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Crispy.Web.Controllers
{
    public abstract class BaseApiController : Controller
    {
        protected readonly UserManager<User> UserManager;

        protected BaseApiController(UserManager<User> userManager)
        {
            UserManager = userManager;
        }

        protected async Task<User?> GetCurrentUserAsync()
        {
            return await UserManager.GetUserAsync(User);
        }

        protected int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var id) ? id : 0;
        }

        protected bool IsOwner(int resourceOwnerId)
        {
            return GetCurrentUserId() == resourceOwnerId;
        }

        protected IActionResult RedirectToReferer(string fallbackUrl = "/")
        {
            var referer = Request.Headers["Referer"].ToString();
            return Redirect(string.IsNullOrEmpty(referer) ? fallbackUrl : referer);
        }

        protected IActionResult ViewWithError(object? model, string errorMessage)
        {
            ModelState.AddModelError(string.Empty, errorMessage);
            return View(model);
        }
    }
}
