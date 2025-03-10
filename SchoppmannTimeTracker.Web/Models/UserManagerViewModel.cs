using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoppmannTimeTracker.Web.Areas.Admin.Models
{
    public class UserListViewModel
    {
        public IEnumerable<UserViewModel> Users { get; set; }
    }

    public class UserViewModel
    {
        public string Id { get; set; }

        [Display(Name = "Vorname")]
        public string FirstName { get; set; }

        [Display(Name = "Nachname")]
        public string LastName { get; set; }

        [Display(Name = "E-Mail")]
        public string Email { get; set; }

        [Display(Name = "Rolle")]
        public string Role { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Vorname ist erforderlich")]
        [Display(Name = "Vorname")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Nachname ist erforderlich")]
        [Display(Name = "Nachname")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "E-Mail ist erforderlich")]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Passwort ist erforderlich")]
        [StringLength(100, ErrorMessage = "Passwort muss mindestens {2} Zeichen lang sein.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Passwort")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Passwort bestätigen")]
        [Compare("Password", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Rolle ist erforderlich")]
        [Display(Name = "Rolle")]
        public string Role { get; set; }

        [ValidateNever]
        public List<SelectListItem> AvailableRoles { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vorname ist erforderlich")]
        [Display(Name = "Vorname")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Nachname ist erforderlich")]
        [Display(Name = "Nachname")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "E-Mail ist erforderlich")]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse")]
        [Display(Name = "E-Mail")]
        public string Email { get; set; }

        [StringLength(100, ErrorMessage = "Passwort muss mindestens {2} Zeichen lang sein.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Neues Passwort (leer lassen, um Passwort beizubehalten)")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Passwort bestätigen")]
        [Compare("Password", ErrorMessage = "Die Passwörter stimmen nicht überein.")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Rolle ist erforderlich")]
        [Display(Name = "Rolle")]
        public string Role { get; set; }

        public List<SelectListItem> AvailableRoles { get; set; }
    }
}