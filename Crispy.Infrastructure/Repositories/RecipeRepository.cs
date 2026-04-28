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
            return await _context.Recipes
                .Include(r => r.User)
                .Include(r => r.RecipeIngredients)
                    .ThenInclude(ri => ri.Ingredient)
                .FirstOrDefaultAsync(r => r.Id == id);
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
        public async Task<IEnumerable<Category>> GetCategoriesAsync()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<IEnumerable<RecipeIngredient>> GetRecipeIngredientsAsync(int recipeId)
        {
            // Дістаємо всі записи для конкретного рецепту і обов'язково підтягуємо назву самого інгредієнта
            return await _context.RecipeIngredients
                .Include(ri => ri.Ingredient)
                .Where(ri => ri.RecipeId == recipeId)
                .ToListAsync();
        }

        public async Task AddToShoppingListAsync(IEnumerable<ShoppingListItem> items)
        {
            await _context.ShoppingListItems.AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }
        public async Task<IEnumerable<ShoppingListItem>> GetShoppingListAsync(int userId)
        {
            return await _context.ShoppingListItems
                .Where(s => s.UserId == userId)
                .OrderBy(s => s.IsBought) // Спочатку некуплені, куплені в кінці
                .ThenBy(s => s.Id)
                .ToListAsync();
        }

        public async Task ToggleShoppingItemAsync(int itemId, int userId)
        {
            var item = await _context.ShoppingListItems.FirstOrDefaultAsync(s => s.Id == itemId && s.UserId == userId);
            if (item != null)
            {
                item.IsBought = !item.IsBought; // Змінюємо статус на протилежний
                await _context.SaveChangesAsync();
            }
        }

        public async Task ClearBoughtItemsAsync(int userId)
        {
            var boughtItems = await _context.ShoppingListItems
                .Where(s => s.UserId == userId && s.IsBought)
                .ToListAsync();

            if (boughtItems.Any())
            {
                _context.ShoppingListItems.RemoveRange(boughtItems);
                await _context.SaveChangesAsync();
            }
        }
        public async Task<Ingredient?> GetIngredientByNameAsync(string name)
        {
            // Шукаємо без врахування регістру (щоб "Цукор" і "цукор" були одним і тим же)
            return await _context.Ingredients
                .FirstOrDefaultAsync(i => i.Name.ToLower() == name.ToLower());
        }

        public async Task<Ingredient> AddIngredientAsync(Ingredient ingredient)
        {
            await _context.Ingredients.AddAsync(ingredient);
            await _context.SaveChangesAsync();
            return ingredient; // Повертаємо, бо після збереження EF Core заповнить йому Id
        }

        public async Task ToggleFollowAsync(int followerId, int followedUserId)
        {
            if (followerId == followedUserId)
                return; // Не можна підписатись самому на себе

            var existingFollow = await _context.UserFollowers
                .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowedUserId == followedUserId);

            if (existingFollow != null)
            {
                // Відписатися
                _context.UserFollowers.Remove(existingFollow);
            }
            else
            {
                // Підписатися
                var newFollow = new UserFollower
                {
                    FollowerId = followerId,
                    FollowedUserId = followedUserId
                };
                await _context.UserFollowers.AddAsync(newFollow);
            }

            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsFollowingAsync(int followerId, int followedUserId)
        {
            return await _context.UserFollowers
                .AnyAsync(f => f.FollowerId == followerId && f.FollowedUserId == followedUserId);
        }

        public async Task<IEnumerable<Recipe>> GetFeedRecipesAsync(int followerId)
        {
            // Беремо ID авторів, на яких підписаний користувач
            var followedUserIds = await _context.UserFollowers
                .Where(f => f.FollowerId == followerId)
                .Select(f => f.FollowedUserId)
                .ToListAsync();

            if (!followedUserIds.Any())
                return Enumerable.Empty<Recipe>();

            // Беремо їхні рецепти, відсортовані за датою додавання
            return await _context.Recipes
                .Include(r => r.User) // Залежить від того, як ви називаєте автора: User або Author
                .Where(r => followedUserIds.Contains(r.UserId))
                .OrderByDescending(r => r.Id)
                .ToListAsync();
        }

        public async Task AddMealToPlanAsync(MealPlan mealPlan)
        {
            await _context.MealPlans.AddAsync(mealPlan);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveMealFromPlanAsync(int planId, int userId)
        {
            var plan = await _context.MealPlans.FirstOrDefaultAsync(m => m.Id == planId && m.UserId == userId);
            if (plan != null)
            {
                _context.MealPlans.Remove(plan);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<MealPlan>> GetWeeklyPlanAsync(int userId)
        {
            return await _context.MealPlans
                .Include(m => m.Recipe) // Обов'язково підвантажуємо дані рецепту, щоб вивести назву
                .Where(m => m.UserId == userId)
                // Сортуємо по дню тижня, а потім по типу прийому їжі
                .OrderBy(m => m.DayOfWeek)
                .ThenBy(m => m.MealType)
                .ToListAsync();
        }
    }
}
