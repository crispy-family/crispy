namespace Crispy.Core.Entities
{
    public class FavoriteRecipe
    {
        public int UserId { get; set; }
        public User? User { get; set; }

        public int RecipeId { get; set; }
        public Recipe? Recipe { get; set; }
    }
}