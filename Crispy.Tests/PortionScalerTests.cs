using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Crispy.Application.Interfaces;
using Crispy.Application.Services;
using Crispy.Core.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Crispy.Tests
{
    public class PortionScalerTests
    {
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<IRecipeRepository> _mockRepo;
        private readonly RecipeService _recipeService;

        public PortionScalerTests()
        {
            _mockCache = new Mock<IMemoryCache>();
            _mockConfig = new Mock<IConfiguration>();
            _mockRepo = new Mock<IRecipeRepository>();

            object expectedValue = null;
            _mockCache.Setup(mc => mc.TryGetValue(It.IsAny<object>(), out expectedValue))
                .Returns(false);

            _recipeService = new RecipeService(_mockRepo.Object, _mockCache.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task AddRecipeToShoppingListAsync_ShouldScaleQuantities_WhenRequestedServingsIsDifferentFromBase()
        {
            int recipeId = 1;
            int userId = 10;

            var recipe = new Recipe { Id = recipeId, Servings = 2 };
            var ingredients = new List<RecipeIngredient>
            {
                new RecipeIngredient { Quantity = 2, Unit = "шт", Ingredient = new Ingredient { Name = "Яйце" } },
                new RecipeIngredient { Quantity = 50, Unit = "г", Ingredient = new Ingredient { Name = "Масло" } }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(recipeId)).ReturnsAsync(recipe);
            _mockRepo.Setup(r => r.GetRecipeIngredientsAsync(recipeId)).ReturnsAsync(ingredients);

            IEnumerable<ShoppingListItem>? capturedItems = null;
            _mockRepo.Setup(r => r.AddToShoppingListAsync(It.IsAny<IEnumerable<ShoppingListItem>>()))
                     .Callback<IEnumerable<ShoppingListItem>>(items => capturedItems = items)
                     .Returns(Task.CompletedTask);

            await _recipeService.AddRecipeToShoppingListAsync(recipeId, userId, 7);

            Assert.NotNull(capturedItems);
            var itemsList = capturedItems!.ToList();

            Assert.Equal("7", itemsList.First(i => i.IngredientName == "Яйце").Quantity);
            Assert.Equal("175", itemsList.First(i => i.IngredientName == "Масло").Quantity);
        }

        [Fact]
        public async Task AddRecipeToShoppingListAsync_ShouldKeepOriginalQuantities_WhenRequestedServingsIsNull()
        {
            int recipeId = 2;
            int userId = 10;

            var recipe = new Recipe { Id = recipeId, Servings = 4 };
            var ingredients = new List<RecipeIngredient>
            {
                new RecipeIngredient { Quantity = 100, Unit = "г", Ingredient = new Ingredient { Name = "Сир" } }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(recipeId)).ReturnsAsync(recipe);
            _mockRepo.Setup(r => r.GetRecipeIngredientsAsync(recipeId)).ReturnsAsync(ingredients);

            IEnumerable<ShoppingListItem>? capturedItems = null;
            _mockRepo.Setup(r => r.AddToShoppingListAsync(It.IsAny<IEnumerable<ShoppingListItem>>()))
                     .Callback<IEnumerable<ShoppingListItem>>(items => capturedItems = items)
                     .Returns(Task.CompletedTask);

            await _recipeService.AddRecipeToShoppingListAsync(recipeId, userId, null);

            Assert.NotNull(capturedItems);
            Assert.Single(capturedItems!);
            Assert.Equal("100", capturedItems!.First().Quantity);
        }

        [Fact]
        public async Task AddRecipeToShoppingListAsync_ShouldScaleFractionsCorrectly_WhenScalingDown()
        {
            int recipeId = 3;
            int userId = 10;

            var recipe = new Recipe { Id = recipeId, Servings = 4 };
            var ingredients = new List<RecipeIngredient>
            {
                new RecipeIngredient { Quantity = 2, Unit = "шт", Ingredient = new Ingredient { Name = "Авокадо" } }
            };

            _mockRepo.Setup(r => r.GetByIdAsync(recipeId)).ReturnsAsync(recipe);
            _mockRepo.Setup(r => r.GetRecipeIngredientsAsync(recipeId)).ReturnsAsync(ingredients);

            IEnumerable<ShoppingListItem>? capturedItems = null;
            _mockRepo.Setup(r => r.AddToShoppingListAsync(It.IsAny<IEnumerable<ShoppingListItem>>()))
                     .Callback<IEnumerable<ShoppingListItem>>(items => capturedItems = items);

            await _recipeService.AddRecipeToShoppingListAsync(recipeId, userId, 1);

            Assert.NotNull(capturedItems);
            Assert.Equal("0.5", capturedItems!.First().Quantity);
        }
    }
}