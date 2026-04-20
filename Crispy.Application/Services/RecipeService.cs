using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Crispy.Application.DTOs;
using Crispy.Application.Interfaces;
using Crispy.Core.Entities;

namespace Crispy.Application.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly IRecipeRepository _repository;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        // Додаємо IMemoryCache та IConfiguration в конструктор
        public RecipeService(IRecipeRepository repository, IMemoryCache cache, IConfiguration configuration)
        {
            _repository = repository;
            _cache = cache;
            _configuration = configuration;
        }

        public async Task<bool> CreateRecipeAsync(string title, string description, int userId, string? imagePath = null, int? categoryId = null, List<RecipeIngredientDto>? ingredients = null)
        {
            var recipe = new Recipe
            {
                Title = title,
                Description = description,
                UserId = userId,
                ImageUrl = imagePath,
                CategoryId = categoryId 
            };

            // Обробляємо інгредієнти, якщо вони є
            if (ingredients != null && ingredients.Any())
            {
                foreach (var item in ingredients)
                {
                    if (string.IsNullOrWhiteSpace(item.Name)) continue;

                    var existingIngredient = await _repository.GetIngredientByNameAsync(item.Name.Trim());

                    if (existingIngredient == null)
                    {
                        existingIngredient = new Ingredient
                        {
                            Name = item.Name.Trim(),
                            CaloriesPerUnit = 0
                        };
                        existingIngredient = await _repository.AddIngredientAsync(existingIngredient);
                    }

                    recipe.RecipeIngredients.Add(new RecipeIngredient
                    {
                        IngredientId = existingIngredient.Id,
                        Quantity = item.Quantity,
                        Unit = item.Unit
                    });
                }
            }

            await _repository.AddAsync(recipe);
            return true;
        }

        public async Task<IEnumerable<Recipe>> GetAllRecipesAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<IEnumerable<Recipe>> GetUserRecipesAsync(int userId)
        {
            return await _repository.GetByUserIdAsync(userId);
        }

        public async Task ToggleFavoriteAsync(int userId, int recipeId)
        {
            // Перевіряємо, чи є вже такий рецепт в улюблених
            var isFavorite = await _repository.IsFavoriteAsync(userId, recipeId);

            if (isFavorite)
            {
                await _repository.RemoveFromFavoritesAsync(userId, recipeId); 
            }
            else
            {
                await _repository.AddToFavoritesAsync(userId, recipeId); 
            }
        }

        public async Task<IEnumerable<Recipe>> GetFavoriteRecipesAsync(int userId)
        {
            return await _repository.GetFavoriteRecipesAsync(userId);
        }
        public async Task<IEnumerable<Recipe>> SearchRecipesAsync(string searchTerm)
        {
            return await _repository.SearchAsync(searchTerm);
        }
        public async Task<Recipe?> GetRecipeByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<bool> UpdateRecipeAsync(int id, string title, string description, int userId)
        {
            var recipe = await _repository.GetByIdAsync(id);

            if (recipe == null || recipe.UserId != userId) return false;

            recipe.Title = title;
            recipe.Description = description;

            await _repository.UpdateAsync(recipe);
            return true;
        }

        public async Task<bool> DeleteRecipeAsync(int id, int userId)
        {
            return await DeleteRecipeAsync(id, userId, false);
        }

        public async Task<bool> DeleteRecipeAsync(int id, int userId, bool isAdmin = false)
        {
            var recipe = await _repository.GetByIdAsync(id);
            if (recipe == null) return false;

            // Allow deletion if the user is the author or is admin
            if (!isAdmin && recipe.UserId != userId) return false;

            await _repository.DeleteAsync(recipe);
            return true;
        }
        public async Task AddCommentAsync(int recipeId, int userId, string text)
        {
            var comment = new Comment
            {
                RecipeId = recipeId,
                UserId = userId,
                Text = text,
                CreatedAt = DateTime.UtcNow
            };
            await _repository.AddCommentAsync(comment);
        }

        public async Task<IEnumerable<Comment>> GetRecipeCommentsAsync(int recipeId)
        {
            return await _repository.GetCommentsByRecipeIdAsync(recipeId);
        }

        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            // Унікальний ключ для кешу
            const string cacheKey = "CategoriesList";

            // Спробуємо отримати дані з кешу
            if (!_cache.TryGetValue(cacheKey, out IEnumerable<Category> categories))
            {
                // Якщо в кеші пусто - витягуємо з БД
                categories = await _repository.GetCategoriesAsync();

                // Зчитуємо час життя кешу з appsettings.json (за замовчуванням 30 хвилин, якщо не знайдено)
                int cacheExpirationMinutes = _configuration.GetValue<int>("CacheSettings:CategoriesCacheDurationMinutes", 30);

                // Налаштовуємо параметри збереження в кеші
                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(cacheExpirationMinutes));

                // Зберігаємо в кеш
                _cache.Set(cacheKey, categories, cacheEntryOptions);
            }

            return categories;
        }
        public async Task AddRecipeToShoppingListAsync(int recipeId, int userId)
        {
            // 1. Отримуємо всі інгредієнти цього рецепту
            var ingredients = await _repository.GetRecipeIngredientsAsync(recipeId);

            if (!ingredients.Any()) return; // Якщо інгредієнтів немає - нічого не робимо

            // 2. Створюємо список покупок за допомогою LINQ
            var shoppingItems = ingredients.Select(ri => new ShoppingListItem
            {
                UserId = userId,
                IngredientName = ri.Ingredient?.Name ?? "Невідомий інгредієнт",
                Quantity = ri.Quantity.ToString(), // Перетворюємо float на рядок для кошика
                Unit = ri.Unit,
                IsBought = false
            }).ToList();

            // 3. Зберігаємо все в базу
            await _repository.AddToShoppingListAsync(shoppingItems);
        }
        public async Task<IEnumerable<ShoppingListItem>> GetUserShoppingListAsync(int userId)
        {
            return await _repository.GetShoppingListAsync(userId);
        }

        public async Task ToggleShoppingItemStatusAsync(int itemId, int userId)
        {
            await _repository.ToggleShoppingItemAsync(itemId, userId);
        }

        public async Task ClearBoughtShoppingItemsAsync(int userId)
        {
            await _repository.ClearBoughtItemsAsync(userId);
        }

        public async Task ToggleFollowUserAsync(int followerId, int followedUserId)
        {
            await _repository.ToggleFollowAsync(followerId, followedUserId);
        }

        public async Task<bool> IsFollowingUserAsync(int followerId, int followedUserId)
        {
            return await _repository.IsFollowingAsync(followerId, followedUserId);
        }

        public async Task<IEnumerable<Recipe>> GetUserFeedAsync(int followerId)
        {
            return await _repository.GetFeedRecipesAsync(followerId);
        }

        public async Task<bool> AddMealToPlanAsync(int userId, int recipeId, DayOfWeek dayOfWeek, Crispy.Core.Enums.MealType mealType)
        {
            try
            {
                var recipe = await _repository.GetByIdAsync(recipeId);
                if (recipe == null) return false;

                var mealPlan = new MealPlan
                {
                    UserId = userId,
                    RecipeId = recipeId,
                    DayOfWeek = dayOfWeek,
                    MealType = mealType
                };

                await _repository.AddMealToPlanAsync(mealPlan);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> RemoveMealFromPlanAsync(int planId, int userId)
        {
            try
            {
                await _repository.RemoveMealFromPlanAsync(planId, userId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<IEnumerable<MealPlan>> GetWeeklyPlanAsync(int userId)
        {
            return await _repository.GetWeeklyPlanAsync(userId);
        }
    }
}
