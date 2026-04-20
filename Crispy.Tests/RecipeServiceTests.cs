using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Crispy.Core.Entities;
using Moq;
using Xunit;
using Crispy.Application.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace Crispy.Tests
{
    public class RecipeServiceTests
    {
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IConfiguration> _mockConfig;

        public RecipeServiceTests()
        {
            // Ініціалізуємо моки для кешу та конфігурації, щоб використовувати їх у всіх тестах
            _mockCache = new Mock<IMemoryCache>();
            _mockConfig = new Mock<IConfiguration>();
            
            // Налаштовуємо мок IMemoryCache, щоб метод TryGetValue завжди повертав false
            object expectedValue = null;
            _mockCache.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out expectedValue))
                .Returns(false);

            _mockCache.Setup(mc => mc.CreateEntry(It.IsAny<object>()))
                .Returns(Mock.Of<ICacheEntry>);
        }

        [Fact]
        public async Task GetUserRecipesAsync_ShouldReturnRecipes_ForSpecificUser()
        {
            // Arrange
            var mockRepo = new Mock<IRecipeRepository>();
            int testUserId = 5;
            var expectedRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Мій рецепт", UserId = testUserId }
            };

            mockRepo.Setup(repo => repo.GetByUserIdAsync(testUserId))
                    .ReturnsAsync(expectedRecipes);

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            // Act
            var result = await recipeService.GetUserRecipesAsync(testUserId);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(testUserId, result.First().UserId);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldAdd_WhenRecipeIsNotFavorite()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            int userId = 1;
            int recipeId = 10;

            mockRepo.Setup(repo => repo.IsFavoriteAsync(userId, recipeId))
                    .ReturnsAsync(false);

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            await recipeService.ToggleFavoriteAsync(userId, recipeId);

            mockRepo.Verify(repo => repo.AddToFavoritesAsync(userId, recipeId), Times.Once);
            mockRepo.Verify(repo => repo.RemoveFromFavoritesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFavoriteAsync_ShouldRemove_WhenRecipeIsAlreadyFavorite()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            int userId = 1;
            int recipeId = 10;

            mockRepo.Setup(repo => repo.IsFavoriteAsync(userId, recipeId))
                    .ReturnsAsync(true);

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            await recipeService.ToggleFavoriteAsync(userId, recipeId);

            mockRepo.Verify(repo => repo.RemoveFromFavoritesAsync(userId, recipeId), Times.Once);
            mockRepo.Verify(repo => repo.AddToFavoritesAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task SearchRecipesAsync_ShouldReturnMatchingRecipes()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            string searchTerm = "Борщ";
            var expectedRecipes = new List<Recipe>
            {
                new Recipe { Id = 1, Title = "Український борщ", Description = "Смачний" }
            };

            mockRepo.Setup(repo => repo.SearchAsync(searchTerm))
                    .ReturnsAsync(expectedRecipes);

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            var result = await recipeService.SearchRecipesAsync(searchTerm);

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

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

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

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

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

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

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

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            var result = await recipeService.DeleteRecipeAsync(testRecipeId, userId);

            Assert.False(result);
            mockRepo.Verify(repo => repo.DeleteAsync(It.IsAny<Recipe>()), Times.Never);
        }

        [Fact]
        public async Task AddCommentAsync_ShouldCallRepository_WithCorrectData()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

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

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            var result = await recipeService.GetRecipeCommentsAsync(recipeId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("Перший!", result.First().Text);
        }

        [Fact]
        public async Task CreateRecipeAsync_ShouldSetImageUrl_WhenProvided()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            string title = "Тірамісу";
            string desc = "Рецепт італійського десерту";
            int userId = 1;
            string imageUrl = "/uploads/recipes/tiramisu.jpg";

            var result = await recipeService.CreateRecipeAsync(title, desc, userId, imageUrl, null);

            Assert.True(result);
            mockRepo.Verify(repo => repo.AddAsync(It.Is<Recipe>(r => r.ImageUrl == imageUrl)), Times.Once);
        }

        [Fact]
        public async Task CreateRecipeAsync_ShouldSetCategoryId_WhenProvided()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            string title = "Борщ";
            string desc = "Класичний рецепт";
            int userId = 1;
            int categoryId = 3; 

            var result = await recipeService.CreateRecipeAsync(title, desc, userId, null, categoryId);

            Assert.True(result);
            mockRepo.Verify(repo => repo.AddAsync(It.Is<Recipe>(r => r.CategoryId == categoryId)), Times.Once);
        }

        [Fact]
        public async Task GetCategoriesAsync_ShouldReturnListOfCategories()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var expectedCategories = new List<Category>
            {
                new Category { Id = 1, Name = "Десерти" },
                new Category { Id = 2, Name = "Перші страви" }
            };

            mockRepo.Setup(repo => repo.GetCategoriesAsync())
                    .ReturnsAsync(expectedCategories);

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            var result = await recipeService.GetCategoriesAsync();

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, c => c.Name == "Десерти");
        }

        [Fact]
        public async Task DeleteRecipeAsync_ShouldReturnTrue_WhenUserIsAdmin_EvenIfNotOwner()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            int testRecipeId = 15;
            int ownerUserId = 5;      
            int adminUserId = 1;      
            bool isAdmin = true;

            var existingRecipe = new Recipe { Id = testRecipeId, UserId = ownerUserId };

            mockRepo.Setup(repo => repo.GetByIdAsync(testRecipeId))
                    .ReturnsAsync(existingRecipe);

            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            var result = await recipeService.DeleteRecipeAsync(testRecipeId, adminUserId, isAdmin);

            Assert.True(result);
            mockRepo.Verify(repo => repo.DeleteAsync(existingRecipe), Times.Once);
        }

        [Fact]
        public async Task CreateRecipeAsync_ShouldProcessIngredients_Correctly()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            var ingredientsDto = new List<RecipeIngredientDto>
            {
                new RecipeIngredientDto { Name = "Молоко", Quantity = 1, Unit = "л" },
                new RecipeIngredientDto { Name = "Новий Екзотичний Фрукт", Quantity = 2, Unit = "шт" }
            };

            mockRepo.Setup(repo => repo.GetIngredientByNameAsync("Молоко"))
                    .ReturnsAsync(new Ingredient { Id = 1, Name = "Молоко" });

            mockRepo.Setup(repo => repo.GetIngredientByNameAsync("Новий Екзотичний Фрукт"))
                    .ReturnsAsync((Ingredient?)null);

            mockRepo.Setup(repo => repo.AddIngredientAsync(It.IsAny<Ingredient>()))
                    .ReturnsAsync(new Ingredient { Id = 2, Name = "Новий Екзотичний Фрут" });

            var result = await recipeService.CreateRecipeAsync("Назва", "Опис", 1, null, null, ingredientsDto);

            Assert.True(result);
            mockRepo.Verify(repo => repo.AddIngredientAsync(It.Is<Ingredient>(i => i.Name == "Новий Екзотичний Фрут")), Times.Once);
            mockRepo.Verify(repo => repo.AddAsync(It.Is<Recipe>(r => r.RecipeIngredients.Count == 2)), Times.Once);
        }

        [Fact]
        public async Task AddRecipeToShoppingListAsync_ShouldAddItems_WhenRecipeHasIngredients()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            int recipeId = 5;
            int userId = 10;
            var mockIngredients = new List<RecipeIngredient>
            {
                new RecipeIngredient { Quantity = 2, Unit = "шт", Ingredient = new Ingredient { Name = "Яйце" } }
            };

            mockRepo.Setup(repo => repo.GetRecipeIngredientsAsync(recipeId))
                    .ReturnsAsync(mockIngredients);

            await recipeService.AddRecipeToShoppingListAsync(recipeId, userId);

            mockRepo.Verify(repo => repo.AddToShoppingListAsync(It.Is<IEnumerable<ShoppingListItem>>(list =>
                list.Count() == 1 && list.First().IngredientName == "Яйце")), Times.Once);
        }

        [Fact]
        public async Task AddRecipeToShoppingListAsync_ShouldNotCallRepo_WhenRecipeIsEmpty()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            mockRepo.Setup(repo => repo.GetRecipeIngredientsAsync(It.IsAny<int>()))
                    .ReturnsAsync(new List<RecipeIngredient>());

            await recipeService.AddRecipeToShoppingListAsync(1, 1);

            mockRepo.Verify(repo => repo.AddToShoppingListAsync(It.IsAny<IEnumerable<ShoppingListItem>>()), Times.Never);
        }
        
        [Fact]
        public async Task ToggleFollowUserAsync_ShouldNotCallRepo_WhenUserTriesToFollowSelf()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);
            int sameUserId = 1;

            await recipeService.ToggleFollowUserAsync(sameUserId, sameUserId);

            mockRepo.Verify(repo => repo.ToggleFollowAsync(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task ToggleFollowUserAsync_ShouldCallRepo_WhenUsersAreDifferent()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            await recipeService.ToggleFollowUserAsync(1, 2);

            mockRepo.Verify(repo => repo.ToggleFollowAsync(1, 2), Times.Once);
        }

        [Fact]
        public async Task GetUserFeedAsync_ShouldReturnFeedFromRepository()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            var expectedFeed = new List<Recipe>
            {
                new Recipe { Id = 10, Title = "Рецепт з підписки" }
            };

            mockRepo.Setup(repo => repo.GetFeedRecipesAsync(1))
                    .ReturnsAsync(expectedFeed);

            var result = await recipeService.GetUserFeedAsync(1);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal("Рецепт з підписки", result.First().Title);
        }

        [Fact]
        public async Task GetUserShoppingListAsync_ShouldReturnItemsFromRepository()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            int userId = 1;
            var expectedList = new List<ShoppingListItem>
            {
                new ShoppingListItem { Id = 1, IngredientName = "Молоко", Quantity = "1", Unit = "л" },
                new ShoppingListItem { Id = 2, IngredientName = "Хліб", Quantity = "1", Unit = "шт" }
            };

            mockRepo.Setup(repo => repo.GetShoppingListAsync(userId))
                    .ReturnsAsync(expectedList);

            var result = await recipeService.GetUserShoppingListAsync(userId);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Contains(result, i => i.IngredientName == "Молоко");
        }

        [Fact]
        public async Task ToggleShoppingItemStatusAsync_ShouldCallRepo()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            await recipeService.ToggleShoppingItemStatusAsync(10, 5);

            mockRepo.Verify(repo => repo.ToggleShoppingItemAsync(10, 5), Times.Once);
        }

        [Fact]
        public async Task ClearBoughtShoppingItemsAsync_ShouldCallRepo()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            await recipeService.ClearBoughtShoppingItemsAsync(5);

            mockRepo.Verify(repo => repo.ClearBoughtItemsAsync(5), Times.Once);
        }

        // ==========================================
        //         FOLLOWERS TESTS
        // ==========================================

        [Fact]
        public async Task IsFollowingUserAsync_ShouldReturnRepositoryResult()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            mockRepo.Setup(repo => repo.IsFollowingAsync(1, 2)).ReturnsAsync(true);
            mockRepo.Setup(repo => repo.IsFollowingAsync(1, 3)).ReturnsAsync(false);

            var isFollowing2 = await recipeService.IsFollowingUserAsync(1, 2);
            var isFollowing3 = await recipeService.IsFollowingUserAsync(1, 3);

            Assert.True(isFollowing2);
            Assert.False(isFollowing3);
        }


        [Fact]
        public async Task AddMealToPlanAsync_ShouldReturnTrue_WhenRecipeExists()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            int userId = 1;
            int recipeId = 5;
            
            // Мок: рецепт знайдено в БД
            mockRepo.Setup(repo => repo.GetByIdAsync(recipeId))
                    .ReturnsAsync(new Recipe { Id = recipeId, Title = "Салат" });

            var result = await recipeService.AddMealToPlanAsync(userId, recipeId, DayOfWeek.Monday, Crispy.Core.Enums.MealType.Breakfast);

            Assert.True(result);
            mockRepo.Verify(repo => repo.AddMealToPlanAsync(It.Is<MealPlan>(mp => 
                mp.UserId == userId && 
                mp.RecipeId == recipeId && 
                mp.DayOfWeek == DayOfWeek.Monday && 
                mp.MealType == Crispy.Core.Enums.MealType.Breakfast)), Times.Once);
        }

        [Fact]
        public async Task AddMealToPlanAsync_ShouldReturnFalse_WhenRecipeDoesNotExist()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            int recipeId = 99;
            
            // Мок: рецепт НЕ знайдено (null)
            mockRepo.Setup(repo => repo.GetByIdAsync(recipeId))
                    .ReturnsAsync((Recipe?)null);

            var result = await recipeService.AddMealToPlanAsync(1, recipeId, DayOfWeek.Monday, Crispy.Core.Enums.MealType.Breakfast);

            Assert.False(result);
            mockRepo.Verify(repo => repo.AddMealToPlanAsync(It.IsAny<MealPlan>()), Times.Never);
        }

        [Fact]
        public async Task RemoveMealFromPlanAsync_ShouldCallRepoAndReturnTrue()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            // Якщо нічого не ламається в репозиторії, повертає true
            var result = await recipeService.RemoveMealFromPlanAsync(10, 5);

            Assert.True(result);
            mockRepo.Verify(repo => repo.RemoveMealFromPlanAsync(10, 5), Times.Once);
        }

        [Fact]
        public async Task RemoveMealFromPlanAsync_ShouldReturnFalse_OnException()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            // Змушуємо репозиторій викинути виняток
            mockRepo.Setup(repo => repo.RemoveMealFromPlanAsync(It.IsAny<int>(), It.IsAny<int>()))
                    .ThrowsAsync(new System.Exception("DB Error"));

            var result = await recipeService.RemoveMealFromPlanAsync(10, 5);

            Assert.False(result);
        }

        [Fact]
        public async Task GetWeeklyPlanAsync_ShouldReturnPlansFromRepository()
        {
            var mockRepo = new Mock<IRecipeRepository>();
            var recipeService = new RecipeService(mockRepo.Object, _mockCache.Object, _mockConfig.Object);

            int userId = 1;
            var expectedPlans = new List<MealPlan>
            {
                new MealPlan { Id = 1, DayOfWeek = DayOfWeek.Monday, MealType = Crispy.Core.Enums.MealType.Dinner }
            };

            mockRepo.Setup(repo => repo.GetWeeklyPlanAsync(userId))
                    .ReturnsAsync(expectedPlans);

            var result = await recipeService.GetWeeklyPlanAsync(userId);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(DayOfWeek.Monday, result.First().DayOfWeek);
        }
    }
}