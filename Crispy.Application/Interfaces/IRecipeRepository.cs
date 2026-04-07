using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crispy.Core.Entities;

namespace Crispy.Application.Interfaces
{
    public interface IRecipeRepository
    {
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
    }

    public interface IRecipeService
    {
        Task<bool> CreateRecipeAsync(string title, string description, int userId);
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
    }
}
