using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Crispy.Web.Models
{
    public class CreateRecipeViewModel
    {
        [Required(ErrorMessage = "Назва рецепту є обов'язковою")]
        [MaxLength(100, ErrorMessage = "Назва занадто довга")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Опис є обов'язковим")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Фото готової страви")]
        public IFormFile? ImageFile { get; set; }

        [Display(Name = "Категорія")]
        public int? CategoryId { get; set; } // Нове поле
    }
}
