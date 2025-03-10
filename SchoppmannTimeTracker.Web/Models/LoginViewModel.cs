using System.ComponentModel.DataAnnotations;

namespace SchoppmannTimeTracker.Web.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "E-Mail-Adresse ist erforderlich")]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Passwort ist erforderlich")]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; }

        [Display(Name = "Angemeldet bleiben?")]
        public bool RememberMe { get; set; }
    }
}