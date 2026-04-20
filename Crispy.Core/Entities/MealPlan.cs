using System;
using Crispy.Core.Enums;

namespace Crispy.Core.Entities
{
    public class MealPlan
    {
        public int Id { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }

        // Використовуємо вбудований DayOfWeek (0 - Неділя, 1 - Понеділок тощо)
        public DayOfWeek DayOfWeek { get; set; } 

        public MealType MealType { get; set; }
    }
}