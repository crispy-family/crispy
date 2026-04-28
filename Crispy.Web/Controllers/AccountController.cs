using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Crispy.Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Crispy.Web.Filters;

namespace Crispy.Web.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService, UserManager<User> userManager)
            : base(userManager)
        {
            _authService = authService;
        }

        [HttpGet]
        public IActionResult Register() => View();

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.RegisterAsync(model.Email, model.Username, model.Password);
            if (result.Succeeded)
            {
                await _authService.LoginAsync(model.Email, model.Password, false);
                return RedirectToAction("Index", "Home");
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        [IpRateLimitFilter(MaxRequests = 5, TimeWindowInSeconds = 60)] // Додаємо на GET
        public IActionResult Login() => View();

        [HttpPost]
        [IpRateLimitFilter(MaxRequests = 5, TimeWindowInSeconds = 60)] // Залишається на POST
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.LoginAsync(model.Email, model.Password, model.RememberMe);
            if (result.Succeeded)
                return RedirectToAction("Index", "Home");

            return ViewWithError(model, "Невірний логін або пароль.");
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await _authService.LogoutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult ForgotPassword() => View();

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var token = await _authService.GeneratePasswordResetTokenAsync(model.Email);
            if (token != null)
            {
                var callbackUrl = Url.Action("ResetPassword", "Account",
                    new { token = token, email = model.Email }, Request.Scheme);

                Console.WriteLine("\n=== ПОСИЛАННЯ ДЛЯ ВІДНОВЛЕННЯ ПАРОЛЮ ===");
                Console.WriteLine(callbackUrl);
                Console.WriteLine("========================================\n");
            }

            return View("ForgotPasswordConfirmation");
        }

        [HttpGet]
        public IActionResult ResetPassword(string token, string email)
        {
            if (token == null || email == null)
                return BadRequest("Invalid request");

            var model = new ResetPasswordViewModel { Token = token, Email = email };
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _authService.ResetPasswordAsync(model.Email, model.Token, model.NewPassword);
            if (result.Succeeded)
                return RedirectToAction("Login");

            foreach (var error in result.Errors)
                ModelState.AddModelError(string.Empty, error.Description);

            return View(model);
        }

        [HttpGet]
        public IActionResult Index() => View();
    }
}
