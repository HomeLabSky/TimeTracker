using System;
using System.ComponentModel.DataAnnotations;

namespace SchoppmannTimeTracker.Web.Models
{
    public class TimeEntryViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Datum ist erforderlich")]
        [DataType(DataType.Date)]
        [Display(Name = "Datum")]
        public DateTime WorkDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Kommen-Zeit ist erforderlich")]
        [DataType(DataType.Time)]
        [Display(Name = "Kommen")]
        public TimeSpan StartTime { get; set; } = new TimeSpan(8, 0, 0); // Standardwert: 8:00 Uhr

        [Required(ErrorMessage = "Gehen-Zeit ist erforderlich")]
        [DataType(DataType.Time)]
        [Display(Name = "Gehen")]
        public TimeSpan EndTime { get; set; } = new TimeSpan(16, 30, 0); // Standardwert: 16:30 Uhr

        // Berechnete Eigenschaften (nur für die Anzeige)
        [Display(Name = "Arbeitszeit")]
        public TimeSpan WorkingHours => EndTime - StartTime;

        [Display(Name = "Lohn")]
        public decimal Earnings { get; set; }
    }
}