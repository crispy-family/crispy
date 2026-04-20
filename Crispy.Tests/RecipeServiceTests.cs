using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Crispy.Core.Entities;
using Moq;
using Xunit;
using Crispy.Application.DTOs;

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

        [Fact]
        public async Task CreateRecipeAsync_ShouldSetImageUrl_WhenProvided()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);

            string title = "Тірамісу";
            string desc = "Рецепт італійського десерту";
            int userId = 1;
            string imageUrl = "/uploads/recipes/tiramisu.jpg";

            // Act
            var result = await recipeService.CreateRecipeAsync(title, desc, userId, imageUrl, null);

            // Assert
            Assert.True(result);
            // Перевіряємо, що в репозиторій передано об'єкт Recipe зі збереженим URL зображення
            mockRepo.Verify(repo => repo.AddAsync(It.Is<Recipe>(r => r.ImageUrl == imageUrl)), Times.Once);
        }

        [Fact]
        public async Task CreateRecipeAsync_ShouldSetCategoryId_WhenProvided()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);

            string title = "Борщ";
            string desc = "Класичний рецепт";
            int userId = 1;
            int categoryId = 3; // Наприклад, ID для "Перші страви"

            // Act
            var result = await recipeService.CreateRecipeAsync(title, desc, userId, null, categoryId);

            // Assert
            Assert.True(result);
            // Перевіряємо, що категорія була успішно додана до рецепту перед збереженням
            mockRepo.Verify(repo => repo.AddAsync(It.Is<Recipe>(r => r.CategoryId == categoryId)), Times.Once);
        }

        [Fact]
        public async Task GetCategoriesAsync_ShouldReturnListOfCategories()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var expectedCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Десерти" },
                new Category { Id = 2, Name = "Перші страви" }
            };

            mockRepo.Setup(repo => repo.GetCategoriesAsync())
                    .ReturnsAsync(expectedCategories);

            var recipeService = new RecipeService(mockRepo.Object);

            // Act
            var result = await recipeService.GetCategoriesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.Name == "Десерти");
        }

        [Fact]
        public async Task DeleteRecipeAsync_ShouldReturnTrue_WhenUserIsAdmin_EvenIfNotOwner()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            int testRecipeId = 15;
            int ownerUserId = 5;      // Власник рецепта
            int adminUserId = 1;      // Адміністратор намагається видалити не свій рецепт
            bool isAdmin = true;

            var existingRecipe = new Recipe { Id = testRecipeId, UserId = ownerUserId };

            mockRepo.Setup(repo => repo.GetByIdAsync(testRecipeId))
                    .ReturnsAsync(existingRecipe);

            var recipeService = new RecipeService(mockRepo.Object);

            // Act
            // Викликаємо перевантажений метод із піднятим прапорцем адміністратора
            var result = await recipeService.DeleteRecipeAsync(testRecipeId, adminUserId, isAdmin);

            // Assert
            Assert.True(result);
            // Видалення має бути дозволено і викликати DeleteAsync
            mockRepo.Verify(repo => repo.DeleteAsync(existingRecipe), Times.Once);
        }

        [Fact]
        public async Task CreateRecipeAsync_ShouldProcessIngredients_Correctly()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);

            var ingredientsDto = new List<RecipeIngredientDto>
            {
                new RecipeIngredientDto { Name = "Молоко", Quantity = 1, Unit = "л" },
                new RecipeIngredientDto { Name = "Новий Екзотичний Фрукт", Quantity = 2, Unit = "шт" }
            };

            // Імітуємо: Молоко вже є в базі
            mockRepo.Setup(repo => repo.GetIngredientByNameAsync("Молоко"))
                    .ReturnsAsync(new Ingredient { Id = 1, Name = "Молоко" });

            // Імітуємо: Екзотичного фрукта ще немає (повертаємо null)
            mockRepo.Setup(repo => repo.GetIngredientByNameAsync("Новий Екзотичний Фрукт"))
                    .ReturnsAsync((Ingredient?)null);

            mockRepo.Setup(repo => repo.AddIngredientAsync(It.IsAny<Ingredient>()))
                    .ReturnsAsync(new Ingredient { Id = 2, Name = "Новий Екзотичний Фрут" });

            // Act (передаємо 6 параметрів, як ми робили в фінальній версії)
            var result = await recipeService.CreateRecipeAsync("Назва", "Опис", 1, null, null, ingredientsDto);

            // Assert
            Assert.True(result);

            // Перевіряємо, що метод AddIngredientAsync викликався ТІЛЬКИ для нового фрукта (1 раз)
            mockRepo.Verify(repo => repo.AddIngredientAsync(It.Is<Ingredient>(i => i.Name == "Новий Екзотичний Фрут")), Times.Once);

            // Перевіряємо, що рецепт зберігся з рівно двома зв'язками RecipeIngredients
            mockRepo.Verify(repo => repo.AddAsync(It.Is<Recipe>(r => r.RecipeIngredients.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task AddRecipeToShoppingListAsync_ShouldAddItems_WhenRecipeHasIngredients()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);

            int recipeId = 5;
            int userId = 10;
            var mockIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient { Quantity = 2, Unit = "шт", Ingredient = new Ingredient { Name = "Яйце" } }
            };

            // Репозиторій повертає інгредієнти
            mockRepo.Setup(repo => repo.GetRecipeIngredientsAsync(recipeId))
                    .ReturnsAsync(mockIngredients);

            // Act
            await recipeService.AddRecipeToShoppingListAsync(recipeId, userId);

            // Assert
            // Перевіряємо, чи сформувався список покупок з правильними даними
            mockRepo.Verify(repo => repo.AddToShoppingListAsync(It.Is<IEnumerable<ShoppingListItem>>(list =>
                list.Count() == 1 && list.First().IngredientName == "Яйце")), Times.Once);
        }

        [Fact]
        public async Task AddRecipeToShoppingListAsync_ShouldNotCallRepo_WhenRecipeIsEmpty()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);

            // Репозиторій повертає ПОРОЖНІЙ список інгредієнтів
            mockRepo.Setup(repo => repo.GetRecipeIngredientsAsync(It.IsAny<int>()))
                    .ReturnsAsync(new List<RecipeIngredient>());

            // Act
            await recipeService.AddRecipeToShoppingListAsync(1, 1);

            // Assert
            // Збереження в кошик НЕ мало відбутися
            mockRepo.Verify(repo => repo.AddToShoppingListAsync(It.IsAny<IEnumerable<ShoppingListItem>>()), Times.Never);
        }
        [Fact]
        public async Task ToggleFollowUserAsync_ShouldNotCallRepo_WhenUserTriesToFollowSelf()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);
            int sameUserId = 1;

            // Act
            await recipeService.ToggleFollowUserAsync(sameUserId, sameUserId);

            // Assert
            // Репозиторій не повинен був викликатися
            mockRepo.Verify(repo => repo.ToggleFollowAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFollowUserAsync_ShouldCallRepo_WhenUsersAreDifferent()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);

            // Act
            await recipeService.ToggleFollowUserAsync(1, 2);

            // Assert
            mockRepo.Verify(repo => repo.ToggleFollowAsync(1, 2), Times.Once);
        }

        [Fact]
        public async Task GetUserFeedAsync_ShouldReturnFeedFromRepository()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object);

            var expectedFeed = new List<Recipe>
            {
                new Recipe { Id = 10, Title = "Рецепт з підписки" }
            };

            mockRepo.Setup(repo => repo.GetFeedRecipesAsync(1))
                    .ReturnsAsync(expectedFeed);

            // Act
            var result = await recipeService.GetUserFeedAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Рецепт з підписки", result.First().Title);
        }
    }
}