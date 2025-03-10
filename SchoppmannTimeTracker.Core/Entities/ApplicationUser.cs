using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace SchoppmannTimeTracker.Core.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public virtual ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();
        public virtual UserSettings Settings { get; set; }
    }
}