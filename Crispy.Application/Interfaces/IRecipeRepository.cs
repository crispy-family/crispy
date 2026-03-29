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
    }

    public interface IRecipeService
    {
        Task<bool> CreateRecipeAsync(string title, string description, int userId);
        Task<IEnumerable<Recipe>> GetAllRecipesAsync();
    }
}
