using Crispy.Infrastructure.Data;
using Crispy.Web.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Crispy.Web.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly ILogger<NotificationBackgroundService> _logger;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationBackgroundService(
            ILogger<NotificationBackgroundService> logger, 
            IServiceScopeFactory scopeFactory,
            IHubContext<NotificationHub> hubContext)
        {
            _logger = logger;
            _scopeFactory = scopeFactory;
            _hubContext = hubContext;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service is starting.");

            // Зберігаємо час останньої перевірки
            var lastCheckTime = DateTime.UtcNow;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckForNewEventsAsync(lastCheckTime, stoppingToken);
                    lastCheckTime = DateTime.UtcNow;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing notification check.");
                }

                // Чекаємо 10 секунд перед наступною перевіркою
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }

        private async Task CheckForNewEventsAsync(DateTime since, CancellationToken stoppingToken)
        {
            // Створюємо scope, щоб отримати DbContext
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<CrispyDbContext>();

            // ПОДІЯ 1: Нові коментарі до рецептів користувача
            var newComments = await dbContext.Comments
                .Include(c => c.Recipe)
                .Include(c => c.User)
                .Where(c => c.CreatedAt > since && c.Recipe.UserId != null && c.UserId != c.Recipe.UserId)
                .ToListAsync(stoppingToken);

            foreach (var comment in newComments)
            {
                var authorId = comment.Recipe.UserId.ToString();
                var msg = $"Користувач {comment.User?.UserName ?? "Хтось"} прокоментував ваш рецепт '{comment.Recipe.Title}'.";

                await _hubContext.Clients.User(authorId).SendAsync("ReceiveNotification", msg, cancellationToken: stoppingToken);
            }

            // ПОДІЯ 2: Нові підписники
            var newFollowers = await dbContext.UserFollowers
                .Include(uf => uf.Follower)
                .Where(uf => uf.CreatedAt > since) 
                .ToListAsync(stoppingToken);

            foreach (var follow in newFollowers)
            {
                var followedUserId = follow.FollowedUserId.ToString();
                var msg = $"Користувач {follow.Follower?.UserName ?? "Хтось"} підписався на вас!";

                await _hubContext.Clients.User(followedUserId).SendAsync("ReceiveNotification", msg, cancellationToken: stoppingToken);
            }

            // ПОДІЯ 3: Нові рецепти від людей, на яких ви підписані
            // Знаходимо всі рецепти, створені з моменту lastCheckTime
            var newRecipes = await dbContext.Recipes
                .Include(r => r.User)
                .Where(r => r.CreatedAt > since)
                .ToListAsync(stoppingToken);

            foreach (var recipe in newRecipes)
            {
                // Знаходимо всіх підписників автора цього рецепту
                var followersIds = await dbContext.UserFollowers
                    .Where(uf => uf.FollowedUserId == recipe.UserId)
                    .Select(uf => uf.FollowerId.ToString())
                    .ToListAsync(stoppingToken);

                var msg = $"{recipe.User?.UserName ?? "Шеф-кухар"} щойно додав новий рецепт: '{recipe.Title}'!";

                // Відправляємо кожному підписнику (можна було б на групу, але через Users теж нормально для навчання)
                await _hubContext.Clients.Users(followersIds).SendAsync("ReceiveNotification", msg, cancellationToken: stoppingToken);
            }
        }
    }
}