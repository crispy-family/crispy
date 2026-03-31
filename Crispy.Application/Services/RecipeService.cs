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

        public async Task<bool> CreateRecipeAsync(string title, string description, int userId)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) || userId <= 0)
                return false;

            var recipe = new Recipe
            {
                Title = title,
                Description = description,
                UserId = userId
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
    }
}
