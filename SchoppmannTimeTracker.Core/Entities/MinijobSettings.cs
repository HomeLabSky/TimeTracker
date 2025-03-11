using System;

namespace SchoppmannTimeTracker.Core.Entities
{
    public class MinijobSettings : BaseEntity
    {
        public decimal MonthlyLimit { get; set; } = 538.0m;
        public string Description { get; set; } = "Minijob-Grenze";
        public DateTime ValidFrom { get; set; } = DateTime.Today;
        public DateTime? ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}