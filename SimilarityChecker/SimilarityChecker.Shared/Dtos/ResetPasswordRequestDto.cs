using System.ComponentModel.DataAnnotations;

namespace SimilarityChecker.Shared.Dtos
{
    public sealed class ResetPasswordRequestDto
    {
        [Required(ErrorMessage = "Email-ul este obligatoriu.")]
        [EmailAddress(ErrorMessage = "Email invalid.")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Tokenul este obligatoriu.")]
        public string Token { get; set; } = "";

        [Required(ErrorMessage = "Parola nouă este obligatorie.")]
        [MinLength(6, ErrorMessage = "Parola trebuie să aibă minim 6 caractere.")]
        public string NewPassword { get; set; } = "";
    }
}