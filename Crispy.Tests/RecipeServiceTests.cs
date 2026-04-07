using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Crispy.Core.Entities;
using Moq;
using Xunit; 

namespace Crispy.Tests
{
    public class RecipeServiceTests
    {
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

        [Fact]
        public async Task UpdateRecipeAsync_ShouldReturnTrue_WhenUserIsOwner()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            int testRecipeId = 1;
            int ownerUserId = 5;
            var existingRecipe = new Recipe { Id = testRecipeId, Title = "Стара назва", UserId = ownerUserId };

            mockRepo.Setup(repo => repo.GetByIdAsync(testRecipeId))
                    .ReturnsAsync(existingRecipe);

            var recipeService = new RecipeService(mockRepo.Object);

            var result = await recipeService.UpdateRecipeAsync(testRecipeId, "Нова назва", "Новий опис", ownerUserId);

            Assert.True(result); 
            mockRepo.Verify(repo => repo.UpdateAsync(It.IsAny<Recipe>()), Times.Once); 
        }

        [Fact]
        public async Task UpdateRecipeAsync_ShouldReturnFalse_WhenUserIsNotOwner()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            int testRecipeId = 1;
            int ownerUserId = 5;
            int hackerUserId = 99; 

            var existingRecipe = new Recipe { Id = testRecipeId, UserId = ownerUserId };

            mockRepo.Setup(repo => repo.GetByIdAsync(testRecipeId))
                    .ReturnsAsync(existingRecipe);

            var recipeService = new RecipeService(mockRepo.Object);

            var result = await recipeService.UpdateRecipeAsync(testRecipeId, "Зламана назва", "Опис", hackerUserId);

            Assert.False(result); 
            mockRepo.Verify(repo => repo.UpdateAsync(It.IsAny<Recipe>()), Times.Never); 
        }

        [Fact]
        public async Task DeleteRecipeAsync_ShouldReturnTrue_WhenUserIsOwner()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            int testRecipeId = 1;
            int ownerUserId = 3;
            var existingRecipe = new Recipe { Id = testRecipeId, UserId = ownerUserId };

            mockRepo.Setup(repo => repo.GetByIdAsync(testRecipeId))
                    .ReturnsAsync(existingRecipe);

            var recipeService = new RecipeService(mockRepo.Object);

            var result = await recipeService.DeleteRecipeAsync(testRecipeId, ownerUserId);

            Assert.True(result);
            mockRepo.Verify(repo => repo.DeleteAsync(existingRecipe), Times.Once);
        }

        [Fact]
        public async Task DeleteRecipeAsync_ShouldReturnFalse_WhenRecipeDoesNotExist()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            int testRecipeId = 999; 
            int userId = 3;

            mockRepo.Setup(repo => repo.GetByIdAsync(testRecipeId))
                    .ReturnsAsync((Recipe?)null);

            var recipeService = new RecipeService(mockRepo.Object);

            var result = await recipeService.DeleteRecipeAsync(testRecipeId, userId);

            Assert.False(result);
            mockRepo.Verify(repo => repo.DeleteAsync(It.IsAny<Recipe>()), Times.Never);
        }


        [Fact]
        public async Task AddCommentAsync_ShouldCallRepository_WithCorrectData()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);

            int recipeId = 1;
            int userId = 2;
            string text = "Дуже смачно!";

            await recipeService.AddCommentAsync(recipeId, userId, text);

            mockRepo.Verify(repo => repo.AddCommentAsync(It.Is<Comment>(c =>
                c.RecipeId == recipeId &&
                c.UserId == userId &&
                c.Text == text)), Times.Once);
        }

        [Fact]
        public async Task GetRecipeCommentsAsync_ShouldReturnCommentsList()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            int recipeId = 1;
            var expectedComments = new List<Comment>
            {
                new Comment { Id = 1, Text = "Перший!", RecipeId = recipeId },
                new Comment { Id = 2, Text = "Круто", RecipeId = recipeId }
            };

            mockRepo.Setup(repo => repo.GetCommentsByRecipeIdAsync(recipeId))
                    .ReturnsAsync(expectedComments);

            var recipeService = new RecipeService(mockRepo.Object);

            var result = await recipeService.GetRecipeCommentsAsync(recipeId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Перший!", result.First().Text);
        }
    }
}