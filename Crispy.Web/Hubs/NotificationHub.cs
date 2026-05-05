using Microsoft.AspNetCore.SignalR;

namespace Crispy.Web.Hubs
{
    public class NotificationHub : Hub
    {
        // Поки що нам не потрібно додавати кастомні методи сюди,
        // оскільки ми будемо відправляти повідомлення з BackgroundService
        // до конкретних користувачів за їхнім UserIdentifier (зазвичай це ClaimTypes.NameIdentifier).
    }
}