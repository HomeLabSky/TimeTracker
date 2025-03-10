using System;

namespace SchoppmannTimeTracker.Core.Entities
{
    public class UserSettings : BaseEntity
    {
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public decimal HourlyRate { get; set; } = 30.0m;
        public int BillingPeriodStartDay { get; set; } = 1;
        public int BillingPeriodEndDay { get; set; } = 31;
        public string InvoiceEmail { get; set; }

        //Gültigkeitsdatum des Stundenlohns
        public DateTime HourlyRateValidFrom { get; set; } = DateTime.Today;
    }
}