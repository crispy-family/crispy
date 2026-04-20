namespace Crispy.Application.DTOs
{
    public class RecipeIngredientDto
    {
        public string Name { get; set; } = string.Empty;
        public float Quantity { get; set; }
        public string Unit { get; set; } = string.Empty;
    }
}
