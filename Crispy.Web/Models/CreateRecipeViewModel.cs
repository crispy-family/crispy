using System.ComponentModel.DataAnnotations;

namespace Crispy.Web.Models
{
    public class CreateRecipeViewModel
    {
        [Required(ErrorMessage = "Назва рецепту є обов'язковою")]
        [MaxLength(100, ErrorMessage = "Назва занадто довга")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Опис є обов'язковим")]
        public string Description { get; set; } = string.Empty;
    }
}
