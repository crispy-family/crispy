using System.Net.Http;
using System.Threading.Tasks;
using Crispy.Application.Interfaces;

namespace Crispy.Infrastructure.HttpClients
{
    public class MealDbClient : IMealDbClient
    {
        private readonly HttpClient _httpClient;

        // HttpClient інжектиться автоматично завдяки налаштуванням у Program.cs
        public MealDbClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GetRandomRecipeAsync()
        {
            // Виконуємо запит до конкретного ендпоінту
            var response = await _httpClient.GetAsync("random.php");
            response.EnsureSuccessStatusCode(); // Викине помилку, якщо статус не 200 OK

            return await response.Content.ReadAsStringAsync();
        }
    }
}