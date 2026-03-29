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
    }
}
