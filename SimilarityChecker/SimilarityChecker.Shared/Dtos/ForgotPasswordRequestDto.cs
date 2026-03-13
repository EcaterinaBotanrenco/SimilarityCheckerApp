using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Shared.Dtos
{
    public sealed class ForgotPasswordRequestDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu.")]
        [EmailAddress(ErrorMessage = "Email invalid.")]
        public string Email { get; set; } = "";
    }
}