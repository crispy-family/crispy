using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Crispy.Core.Entities
{
    public class ShoppingListItem
    {
        public int Id { get; set; }

        // Зберігаємо те, що треба купити
        public string IngredientName { get; set; } = string.Empty;
        public string Quantity { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public bool IsBought { get; set; } = false; // Чи викреслив користувач це у магазині

        // Зв'язок з користувачем (чий це кошик)
        public int UserId { get; set; }
        public User? User { get; set; }
    }
}
