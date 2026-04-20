using System.Collections.Generic;
using System.Threading.Tasks;
using Crispy.Core.Entities;
using Crispy.Application.DTOs;

namespace Crispy.Application.Interfaces
{
    public interface IRecipeRepository
    {
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task AddAsync(Recipe recipe);
        Task<IEnumerable<Recipe>> GetAllAsync();
        Task<IEnumerable<Recipe>> GetByUserIdAsync(int userId);
        Task AddToFavoritesAsync(int userId, int recipeId);
        Task RemoveFromFavoritesAsync(int userId, int recipeId);
        Task<bool> IsFavoriteAsync(int userId, int recipeId);
        Task<IEnumerable<Recipe>> GetFavoriteRecipesAsync(int userId);
        Task<IEnumerable<Recipe>> SearchAsync(string searchTerm);
        Task<Recipe?> GetByIdAsync(int id);
        Task UpdateAsync(Recipe recipe);
        Task DeleteAsync(Recipe recipe);
        Task AddCommentAsync(Comment comment);
        Task<IEnumerable<Comment>> GetCommentsByRecipeIdAsync(int recipeId);
        Task<IEnumerable<RecipeIngredient>> GetRecipeIngredientsAsync(int recipeId);
        Task AddToShoppingListAsync(IEnumerable<ShoppingListItem> items);
        Task<IEnumerable<ShoppingListItem>> GetShoppingListAsync(int userId);
        Task ToggleShoppingItemAsync(int itemId, int userId);
        Task ClearBoughtItemsAsync(int userId);
        Task<Ingredient?> GetIngredientByNameAsync(string name);
        Task<Ingredient> AddIngredientAsync(Ingredient ingredient);
        Task ToggleFollowAsync(int followerId, int followedUserId);
        Task<bool> IsFollowingAsync(int followerId, int followedUserId);
        Task<IEnumerable<Recipe>> GetFeedRecipesAsync(int followerId);
        Task AddMealToPlanAsync(MealPlan mealPlan);
        Task RemoveMealFromPlanAsync(int planId, int userId);
        Task<IEnumerable<MealPlan>> GetWeeklyPlanAsync(int userId);
    }

    public interface IRecipeService
    {
        Task<bool> CreateRecipeAsync(string title, string description, int userId, string? imagePath = null, int? categoryId = null, List<RecipeIngredientDto>? ingredients = null);
        Task<IEnumerable<Category>> GetCategoriesAsync();
        Task<IEnumerable<Recipe>> GetAllRecipesAsync();
        Task<IEnumerable<Recipe>> GetUserRecipesAsync(int userId);
        Task ToggleFavoriteAsync(int userId, int recipeId);
        Task<IEnumerable<Recipe>> GetFavoriteRecipesAsync(int userId);
        Task<IEnumerable<Recipe>> SearchRecipesAsync(string searchTerm);
        Task<Recipe?> GetRecipeByIdAsync(int id);
        Task<bool> UpdateRecipeAsync(int id, string title, string description, int userId);
        Task<bool> DeleteRecipeAsync(int id, int userId);
        Task AddCommentAsync(int recipeId, int userId, string text);
        Task<IEnumerable<Comment>> GetRecipeCommentsAsync(int recipeId);
        Task<bool> DeleteRecipeAsync(int id, int userId, bool isAdmin = false);
        Task AddRecipeToShoppingListAsync(int recipeId, int userId);
        Task<IEnumerable<ShoppingListItem>> GetUserShoppingListAsync(int userId);
        Task ToggleShoppingItemStatusAsync(int itemId, int userId);
        Task ClearBoughtShoppingItemsAsync(int userId);
        Task ToggleFollowUserAsync(int followerId, int followedUserId);
        Task<bool> IsFollowingUserAsync(int followerId, int followedUserId);
        Task<IEnumerable<Recipe>> GetUserFeedAsync(int followerId);
        Task<bool> AddMealToPlanAsync(int userId, int recipeId, DayOfWeek dayOfWeek, Crispy.Core.Enums.MealType mealType);
        Task<bool> RemoveMealFromPlanAsync(int planId, int userId);
        Task<IEnumerable<MealPlan>> GetWeeklyPlanAsync(int userId);
    }
}
