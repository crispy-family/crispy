using System.ComponentModel.DataAnnotations;

namespace Crispy.Web.Models
{
    public class ResetPasswordViewModel
    {
        [Required][EmailAddress] public string Email { get; set; } = string.Empty;
        [Required] public string Token { get; set; } = string.Empty;
        [Required][DataType(DataType.Password)] public string NewPassword { get; set; } = string.Empty;
    }
}
