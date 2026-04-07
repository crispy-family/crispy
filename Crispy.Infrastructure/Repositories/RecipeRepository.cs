using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Crispy.Application.Interfaces;
using Crispy.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Crispy.Infrastructure.Data;

namespace Crispy.Infrastructure.Repositories
{
    public class RecipeRepository : IRecipeRepository
    {
        private readonly CrispyDbContext _context;

        public RecipeRepository(CrispyDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(Recipe recipe)
        {
            await _context.Recipes.AddAsync(recipe);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Recipe>> GetAllAsync()
        {
            return await _context.Recipes
                                 .OrderByDescending(r => r.Id)
                                 .ToListAsync();
        }
        public async Task<IEnumerable<Recipe>> GetByUserIdAsync(int userId)
        {
            return await _context.Recipes
                                 .Where(r => r.UserId == userId)
                                 .OrderByDescending(r => r.Id)
                                 .ToListAsync();
        }

        public async Task AddToFavoritesAsync(int userId, int recipeId)
        {
            var favorite = new FavoriteRecipe { UserId = userId, RecipeId = recipeId };
            await _context.FavoriteRecipes.AddAsync(favorite);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveFromFavoritesAsync(int userId, int recipeId)
        {
            // Шукаємо запис по двох ключах
            var favorite = await _context.FavoriteRecipes.FindAsync(userId, recipeId);
            if (favorite != null)
            {
                _context.FavoriteRecipes.Remove(favorite);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsFavoriteAsync(int userId, int recipeId)
        {
            return await _context.FavoriteRecipes.AnyAsync(f => f.UserId == userId && f.RecipeId == recipeId);
        }

        public async Task<IEnumerable<Recipe>> GetFavoriteRecipesAsync(int userId)
        {
            return await _context.FavoriteRecipes
                .Where(f => f.UserId == userId)
                .Include(f => f.Recipe) // Завантажуємо дані рецепту
                .Select(f => f.Recipe!)
                .ToListAsync();
        }
        public async Task<IEnumerable<Recipe>> SearchAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await GetAllAsync();

            var lowerTerm = searchTerm.ToLower();

            return await _context.Recipes
                .Where(r => r.Title.ToLower().Contains(lowerTerm) ||
                            r.Description.ToLower().Contains(lowerTerm))
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }
        public async Task<Recipe?> GetByIdAsync(int id)
        {
            return await _context.Recipes.FindAsync(id);
        }

        public async Task UpdateAsync(Recipe recipe)
        {
            _context.Recipes.Update(recipe);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(Recipe recipe)
        {
            _context.Recipes.Remove(recipe);
            await _context.SaveChangesAsync();
        }
        public async Task AddCommentAsync(Comment comment)
        {
            await _context.Comments.AddAsync(comment);
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Comment>> GetCommentsByRecipeIdAsync(int recipeId)
        {
            return await _context.Comments
                .Include(c => c.User) 
                .Where(c => c.RecipeId == recipeId)
                .OrderByDescending(c => c.CreatedAt) 
                .ToListAsync();
        }
    }
}
