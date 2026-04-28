using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Crispy.Application.DTOs;

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

        public string? ImageUrl { get; set; }

        [Display(Name = "Категорія")]
        public int? CategoryId { get; set; }

        public List<RecipeIngredientDto> Ingredients { get; set; } = new List<RecipeIngredientDto>();

        [Display(Name = "Кількість порцій")]
        [Required(ErrorMessage = "Будь ласка, вкажіть кількість порцій")]
        [Range(1, 100, ErrorMessage = "Кількість порцій має бути від 1 до 100")]
        public int Servings { get; set; } = 2; // За замовчуванням 2 порції
    }
}
