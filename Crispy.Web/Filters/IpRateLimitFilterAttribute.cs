using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Crispy.Web.Filters
{
    // Дозволяємо вішати атрибут як на окремі методи, так і на весь контролер
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class IpRateLimitFilterAttribute : ActionFilterAttribute
    {
        public int MaxRequests { get; set; } = 10; // Скільки запитів дозволено
        public int TimeWindowInSeconds { get; set; } = 60; // За який час (у секундах)

        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            // Отримуємо сервіс кешування
            var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();

            // Отримуємо IP клієнта
            var ipAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";

            // Формуємо унікальний ключ для кешу (IP + назва дії, яку він викликає)
            var cacheKey = $"RateLimit_{ipAddress}_{context.ActionDescriptor.DisplayName}";

            if (cache.TryGetValue(cacheKey, out int requestCount))
            {
                if (requestCount >= MaxRequests)
                {
                    // Якщо ліміт перевищено - перериваємо виконання і робимо редирект
                    context.Result = new RedirectToActionResult("RateLimitExceeded", "Home", null);
                    return;
                }

                // Збільшуємо лічильник
                cache.Set(cacheKey, requestCount + 1, TimeSpan.FromSeconds(TimeWindowInSeconds));
            }
            else
            {
                // Якщо це перший запит, створюємо запис у кеші зі значенням 1
                cache.Set(cacheKey, 1, TimeSpan.FromSeconds(TimeWindowInSeconds));
            }

            // Пропускаємо запит далі до контролера
            await next();
        }
    }
}