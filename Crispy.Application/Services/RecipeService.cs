using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crispy.Application.Interfaces;
using Crispy.Core.Entities;

namespace Crispy.Application.Services
{
    public class RecipeService : IRecipeService
    {
        private readonly IRecipeRepository _repository;

        public RecipeService(IRecipeRepository repository)
        {
            _repository = repository;
        }

        public async Task<bool> CreateRecipeAsync(string title, string description, int userId, string? imageUrl = null, int? categoryId = null)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) || userId <= 0)
                return false;

            var recipe = new Recipe
            {
                Title = title,
                Description = description,
                UserId = userId,
                ImageUrl = imageUrl,
                CategoryId = categoryId // Зберігаємо категорію
            };

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
            return await _repository.GetCategoriesAsync();
        }
    }
}
