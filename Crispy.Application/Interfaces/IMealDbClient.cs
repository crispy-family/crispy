using System.Threading.Tasks;

namespace Crispy.Application.Interfaces
{
    public interface IMealDbClient
    {
        // Метод для отримання випадкового рецепту (повертає JSON)
        Task<string> GetRandomRecipeAsync();
    }
}