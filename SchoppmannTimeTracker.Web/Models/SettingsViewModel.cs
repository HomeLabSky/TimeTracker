using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using System.ComponentModel.DataAnnotations;

namespace SchoppmannTimeTracker.Web.Models
{
    public class SettingsViewModel
    {
        public int Id { get; set; }
        public string UserId { get; set; }

        [Required(ErrorMessage = "Stundenlohn ist erforderlich")]
        [Range(1, 1000, ErrorMessage = "Stundenlohn muss zwischen 1 und 1000 liegen")]
        [DataType(DataType.Currency)]
        [Display(Name = "Stundenlohn (€)")]
        public decimal HourlyRate { get; set; }

        [Required(ErrorMessage = "Gültig ab ist erforderlich")]
        [DataType(DataType.Date)]
        [Display(Name = "Stundenlohn gültig ab")]
        public DateTime HourlyRateValidFrom { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Starttag des Abrechnungszeitraums ist erforderlich")]
        [Range(1, 31, ErrorMessage = "Der Tag muss zwischen 1 und 31 liegen")]
        [Display(Name = "Abrechnungszeitraum Start (Tag)")]
        public int BillingPeriodStartDay { get; set; }

        [Required(ErrorMessage = "Endtag des Abrechnungszeitraums ist erforderlich")]
        [Range(1, 31, ErrorMessage = "Der Tag muss zwischen 1 und 31 liegen")]
        [Display(Name = "Abrechnungszeitraum Ende (Tag)")]
        public int BillingPeriodEndDay { get; set; }

        [Required(ErrorMessage = "E-Mail für Lohnzettel ist erforderlich")]
        [EmailAddress(ErrorMessage = "Keine gültige E-Mail-Adresse")]
        [Display(Name = "E-Mail für Lohnzettel")]
        public string InvoiceEmail { get; set; }

        // Flag, um festzustellen, ob ein Admin die Einstellungen eines anderen Benutzers bearbeitet
        [ValidateNever]
        public bool IsAdminEdit { get; set; }

        [ValidateNever]
        public string UserFullName { get; set; } // Neue Eigenschaft für den Namen

        [ValidateNever]
        public bool HourlyRateChanged { get; set; }
    }
}