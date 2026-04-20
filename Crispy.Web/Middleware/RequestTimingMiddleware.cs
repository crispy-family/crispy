using System.Diagnostics;

namespace Crispy.Web.Middleware
{
    public class RequestTimingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestTimingMiddleware> _logger;

        public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Запускаємо таймер
            var stopwatch = Stopwatch.StartNew();

            // Передаємо виконання наступному middleware в пайплайні
            await _next(context);

            // Зупиняємо таймер після завершення обробки запиту
            stopwatch.Stop();

            // Логуємо час виконання
            _logger.LogInformation(
                "Request [{Method}] {Path} executed in {ElapsedMilliseconds} ms",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds);
        }
    }
}