using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Crispy.Core.Entities;
using Moq;
using Xunit; 

namespace Crispy.Tests
{
    public class RecipeServiceTests
    {
        // USE-CASE 1: Профіль користувача
        [Fact]
        public async Task GetUserRecipesAsync_ShouldReturnRecipes_ForSpecificUser()
        {
            // Arrange (Підготовка)
            var mockRepo = new Mock<IRecipeRepository>();
            int testUserId = 5;
            var expectedRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Мій рецепт", UserId = testUserId }
            };

            // Вчимо мок повертати наш список, коли запитують рецепти юзера 5
            mockRepo.Setup(repo => repo.GetByUserIdAsync(testUserId))
                    .ReturnsAsync(expectedRecipes);

            var recipeService = new RecipeService(mockRepo.Object);

            // Act (Дія)
            var result = await recipeService.GetUserRecipesAsync(testUserId);

            // Assert (Перевірка)
            Assert.NotNull(result);
            Assert.Single(result); // Перевіряємо, що повернувся рівно 1 рецепт
            Assert.Equal(testUserId, result.First().UserId);
        }

        // USE-CASE 2: Улюблені рецепти (Лайки)
        [Fact]
        public async Task ToggleFavoriteAsync_ShouldAdd_WhenRecipeIsNotFavorite()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            int userId = 1;
            int recipeId = 10;

            // Імітуємо ситуацію, що рецепт ЩЕ НЕ в улюблених (повертаємо false)
            mockRepo.Setup(repo => repo.IsFavoriteAsync(userId, recipeId))
                    .ReturnsAsync(false);

            var recipeService = new RecipeService(mockRepo.Object);

            // Act
            await recipeService.ToggleFavoriteAsync(userId, recipeId);

            // Assert
            // Перевіряємо, що сервіс викликав метод ДОДАВАННЯ рівно 1 раз
            mockRepo.Verify(repo => repo.AddToFavoritesAsync(userId, recipeId), Times.Once);
            // І перевіряємо, що метод ВИДАЛЕННЯ не викликався жодного разу
            mockRepo.Verify(repo => repo.RemoveFromFavoritesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldRemove_WhenRecipeIsAlreadyFavorite()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            int userId = 1;
            int recipeId = 10;

            // Імітуємо ситуацію, що рецепт ВЖЕ Є в улюблених (повертаємо true)
            mockRepo.Setup(repo => repo.IsFavoriteAsync(userId, recipeId))
                    .ReturnsAsync(true);

            var recipeService = new RecipeService(mockRepo.Object);

            // Act
            await recipeService.ToggleFavoriteAsync(userId, recipeId);

            // Assert
            // Перевіряємо, що сервіс викликав метод ВИДАЛЕННЯ рівно 1 раз
            mockRepo.Verify(repo => repo.RemoveFromFavoritesAsync(userId, recipeId), Times.Once);
            mockRepo.Verify(repo => repo.AddToFavoritesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        // USE-CASE 3: Пошук
        [Fact]
        public async Task SearchRecipesAsync_ShouldReturnMatchingRecipes()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            string searchTerm = "Борщ";
            var expectedRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Український борщ", Description = "Смачний" }
            };

            mockRepo.Setup(repo => repo.SearchAsync(searchTerm))
                    .ReturnsAsync(expectedRecipes);

            var recipeService = new RecipeService(mockRepo.Object);

            // Act
            var result = await recipeService.SearchRecipesAsync(searchTerm);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal("Український борщ", result.First().Title);
        }
    }
}