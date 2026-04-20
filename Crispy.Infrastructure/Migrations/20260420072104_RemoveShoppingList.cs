using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crispy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShoppingList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Спочатку видаляємо таблицю, яка має зовнішній ключ (якщо була)
            migrationBuilder.DropTable(
                name: "ShoppingListItems");

            // Потім видаляємо головну таблицю
            migrationBuilder.DropTable(
                name: "ShoppingLists");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
