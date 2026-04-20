using System.Security.Claims;
using System.Text;

namespace Crispy.Web.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Дозволяємо зчитувати тіло запиту кілька разів
            context.Request.EnableBuffering();

            var request = context.Request;
            var method = request.Method;
            var url = $"{request.Scheme}://{request.Host}{request.Path}{request.QueryString}";
            var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "Unknown IP";

            // Отримуємо Id користувача, якщо він залогінений
            var userId = context.User.Identity?.IsAuthenticated == true
                ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Unknown User ID"
                : "Anonymous";

            // Збираємо заголовки
            var headersBuilder = new StringBuilder();
            foreach (var header in request.Headers)
            {
                headersBuilder.AppendLine($"{header.Key}: {header.Value}");
            }

            // Зчитуємо тіло запиту
            var body = string.Empty;
            if (request.ContentLength > 0)
            {
                using var reader = new StreamReader(
                    request.Body, 
                    encoding: Encoding.UTF8, 
                    detectEncodingFromByteOrderMarks: false, 
                    bufferSize: 1024, 
                    leaveOpen: true); // leaveOpen: true дозволяє не закривати stream

                body = await reader.ReadToEndAsync();

                // Обов'язково повертаємо позицію потоку на початок, 
                // щоб наступні middleware або контролери могли його прочитати
                request.Body.Position = 0; 
            }

            // Логуємо зібрану інформацію
            _logger.LogInformation(
                "--- Incoming Request ---\n" +
                "Method: {Method}\n" +
                "URL: {Url}\n" +
                "IP: {IpAddress}\n" +
                "User ID: {UserId}\n" +
                "Headers:\n{Headers}" +
                "Body: {Body}\n" +
                "------------------------",
                method, url, ipAddress, userId, headersBuilder.ToString(), body);

            // Передаємо запит далі по pipeline
            await _next(context);
        }
    }

    // Зручний метод розширення для реєстрації Middleware
    public static class RequestLoggingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RequestLoggingMiddleware>();
        }
    }
}