using System.ComponentModel.DataAnnotations;

namespace Crispy.Web.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Email є обов'язковим")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ім'я користувача є обов'язковим")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}
