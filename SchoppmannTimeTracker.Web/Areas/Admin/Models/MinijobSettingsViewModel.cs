using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoppmannTimeTracker.Web.Areas.Admin.Models
{
    public class MinijobSettingsViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Monatliches Limit ist erforderlich")]
        [Range(1, 2000, ErrorMessage = "Das Limit muss zwischen 1 und 2000 € liegen")]
        [Display(Name = "Monatliches Limit (€)")]
        public decimal MonthlyLimit { get; set; }

        [Required(ErrorMessage = "Beschreibung ist erforderlich")]
        [StringLength(256, ErrorMessage = "Die Beschreibung darf maximal 256 Zeichen lang sein")]
        [Display(Name = "Beschreibung")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Gültig ab ist erforderlich")]
        [DataType(DataType.Date)]
        [Display(Name = "Gültig ab")]
        public DateTime ValidFrom { get; set; } = DateTime.Today;

        [DataType(DataType.Date)]
        [Display(Name = "Gültig bis")]
        public DateTime? ValidTo { get; set; }

        [Display(Name = "Aktiv")]
        public bool IsActive { get; set; } = true;
    }

    public class MinijobSettingsListViewModel
    {
        public IEnumerable<MinijobSettingsViewModel> Settings { get; set; }
        public MinijobSettingsViewModel CurrentSetting { get; set; }
    }
}