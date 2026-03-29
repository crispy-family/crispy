using Crispy.Application.Services;
using Crispy.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;
using Xunit;

namespace Crispy.Tests.BLL
{
    public class AuthServiceTests
    {
        // Допоміжний метод для мокування UserManager
        private Mock<UserManager<User>> MockUserManager()
        {
            var store = new Mock<IUserStore<User>>();
            return new Mock<UserManager<User>>(store.Object, null, null, null, null, null, null, null, null);
        }

        // Допоміжний метод для мокування SignInManager
        private Mock<SignInManager<User>> MockSignInManager(UserManager<User> userManager)
        {
            var contextAccessor = new Mock<Microsoft.AspNetCore.Http.IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<User>>();
            return new Mock<SignInManager<User>>(userManager, contextAccessor.Object, claimsFactory.Object, null, null, null, null);
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
        {
            var user = new User { Email = "test@test.com", UserName = "tester" };
            var mockUserManager = MockUserManager();
            mockUserManager.Setup(x => x.FindByEmailAsync("test@test.com"))
                           .ReturnsAsync(user);

            var mockSignInManager = MockSignInManager(mockUserManager.Object);
            mockSignInManager.Setup(x => x.PasswordSignInAsync(user, "ValidPass123!", false, false))
                             .ReturnsAsync(SignInResult.Success);

            var authService = new AuthService(mockUserManager.Object, mockSignInManager.Object);

            var result = await authService.LoginAsync("test@test.com", "ValidPass123!", false);

            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ReturnsFailed()
        {
            var mockUserManager = MockUserManager();
            // Імітуємо ситуацію, коли користувача немає в БД
            mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync((User?)null);

            var mockSignInManager = MockSignInManager(mockUserManager.Object);

            var authService = new AuthService(mockUserManager.Object, mockSignInManager.Object);

            var result = await authService.LoginAsync("wrong@test.com", "AnyPass123", false);

            Assert.False(result.Succeeded);
            // Перевіряємо, що метод SignIn навіть не викликався
            mockSignInManager.Verify(x => x.PasswordSignInAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }

        [Fact]
        public async Task RegisterAsync_ValidData_ReturnsSuccess()
        {
            var mockUserManager = MockUserManager();
            mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                           .ReturnsAsync(IdentityResult.Success);

            var mockSignInManager = MockSignInManager(mockUserManager.Object);
            var authService = new AuthService(mockUserManager.Object, mockSignInManager.Object);

            // викликаємо наш метод реєстрації
            var result = await authService.RegisterAsync("new@test.com", "newuser", "StrongPass1!");

            //  результат має бути успішним
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task RegisterAsync_DuplicateEmail_ReturnsFailed()
        {
            var mockUserManager = MockUserManager();
            var error = new IdentityError { Description = "Email already exists" };
            mockUserManager.Setup(x => x.CreateAsync(It.IsAny<User>(), It.IsAny<string>()))
                           .ReturnsAsync(IdentityResult.Failed(error));

            var mockSignInManager = MockSignInManager(mockUserManager.Object);
            var authService = new AuthService(mockUserManager.Object, mockSignInManager.Object);

            var result = await authService.RegisterAsync("exist@test.com", "user", "Pass123!");

            //  реєстрація провалена
            Assert.False(result.Succeeded);
            Assert.Contains(result.Errors, e => e.Description == "Email already exists");
        }

        [Fact]
        public async Task GeneratePasswordResetTokenAsync_ExistingUser_ReturnsToken()
        {
            //  користувач існує в БД
            var user = new User { Email = "exist@test.com" };
            var mockUserManager = MockUserManager();

            mockUserManager.Setup(x => x.FindByEmailAsync("exist@test.com"))
                           .ReturnsAsync(user);
            mockUserManager.Setup(x => x.GeneratePasswordResetTokenAsync(user))
                           .ReturnsAsync("valid-fake-token");

            var mockSignInManager = MockSignInManager(mockUserManager.Object);
            var authService = new AuthService(mockUserManager.Object, mockSignInManager.Object);

            var result = await authService.GeneratePasswordResetTokenAsync("exist@test.com");

            // токен успішно згенеровано)
            Assert.NotNull(result);
            Assert.Equal("valid-fake-token", result);
        }

        [Fact]
        public async Task GeneratePasswordResetTokenAsync_NonExistingUser_ReturnsNull()
        {
            // Arrange (Підготовка: користувача з таким email немає)
            var mockUserManager = MockUserManager();
            mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync((User)null); // Імітуємо відсутність юзера

            var mockSignInManager = MockSignInManager(mockUserManager.Object);
            var authService = new AuthService(mockUserManager.Object, mockSignInManager.Object);

            var result = await authService.GeneratePasswordResetTokenAsync("nobody@test.com");

            // Assert (Перевірка: для неіснуючого юзера токен не генерується)
            Assert.Null(result);
        }
    }
}