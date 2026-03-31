using Crispy.Core.Entities;

namespace Crispy.Web.Models
{
    public class UserProfileViewModel
    {
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public IEnumerable<Recipe> MyRecipes { get; set; } = new List<Recipe>();
        public IEnumerable<Recipe> FavoriteRecipes { get; set; } = new List<Recipe>();
    }
}
