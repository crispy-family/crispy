using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Crispy.Core.Entities;
using Cripy.Application.Services;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace CrispyTests
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
            // Arrange (Підготовка)
            var user = new User { Email = "test@test.com", UserName = "tester" };
            var mockUserManager = MockUserManager();
            mockUserManager.Setup(x => x.FindByEmailAsync("test@test.com"))
                           .ReturnsAsync(user);

            var mockSignInManager = MockSignInManager(mockUserManager.Object);
            mockSignInManager.Setup(x => x.PasswordSignInAsync(user, "ValidPass123!", false, false))
                             .ReturnsAsync(SignInResult.Success);

            var authService = new AuthService(mockUserManager.Object, mockSignInManager.Object);

            // Act (Виконання)
            var result = await authService.LoginAsync("test@test.com", "ValidPass123!", false);

            // Assert (Перевірка)
            Assert.True(result.Succeeded);
        }

        [Fact]
        public async Task LoginAsync_UserNotFound_ReturnsFailed()
        {
            // Arrange (Підготовка)
            var mockUserManager = MockUserManager();
            // Імітуємо ситуацію, коли користувача немає в БД
            mockUserManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
                           .ReturnsAsync((User)null);

            var mockSignInManager = MockSignInManager(mockUserManager.Object);

            var authService = new AuthService(mockUserManager.Object, mockSignInManager.Object);

            // Act (Виконання)
            var result = await authService.LoginAsync("wrong@test.com", "AnyPass123", false);

            // Assert (Перевірка)
            Assert.False(result.Succeeded);
            // Перевіряємо, що метод SignIn навіть не викликався
            mockSignInManager.Verify(x => x.PasswordSignInAsync(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
        }
    }
}
