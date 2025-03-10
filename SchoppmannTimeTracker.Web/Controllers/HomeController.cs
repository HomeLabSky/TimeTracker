using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Web.Models;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ISettingsService _settingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            ITimeEntryService timeEntryService,
            ISettingsService settingsService,
            UserManager<ApplicationUser> userManager)
        {
            _timeEntryService = timeEntryService;
            _settingsService = settingsService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);

            // Abrechnungszeitraum ermitteln
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            // Zeiteinträge für den aktuellen Abrechnungszeitraum laden
            var timeEntries = await _timeEntryService.GetTimeEntriesForPeriodAsync(userId, startDate, endDate);

            // Benutzereinstellungen laden
            var settings = await _settingsService.GetUserSettingsAsync(userId);

            // ViewModel erstellen
            var viewModel = new TimeOverviewViewModel
            {
                StartDate = startDate,
                EndDate = endDate,
                TimeEntries = timeEntries.Select(entry => new TimeEntryListItemViewModel
                {
                    Id = entry.Id,
                    WorkDate = entry.WorkDate,
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    Earnings = _timeEntryService.CalculateEarnings(entry, settings)
                }).OrderByDescending(e => e.WorkDate).ThenBy(e => e.StartTime),
                TotalEarnings = await _timeEntryService.GetTotalEarningsForPeriodAsync(userId, startDate, endDate)
            };

            // Gesamtarbeitszeit berechnen
            TimeSpan totalWorkingHours = TimeSpan.Zero;
            foreach (var entry in timeEntries)
            {
                totalWorkingHours += _timeEntryService.CalculateWorkHours(entry);
            }
            viewModel.TotalWorkingHours = totalWorkingHours;

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GeneratePdf()
        {
            var userId = _userManager.GetUserId(User);

            // Abrechnungszeitraum ermitteln
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            // PDF-Service ist in der Infrastructure-Schicht
            var pdfService = HttpContext.RequestServices.GetService(typeof(IPdfService)) as IPdfService;

            if (pdfService != null)
            {
                var pdfBytes = await pdfService.GenerateTimeSheetPdfAsync(userId, startDate, endDate);

                return File(pdfBytes, "application/pdf", $"Zeiterfassung_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.pdf");
            }

            return BadRequest("PDF-Service ist nicht verfügbar.");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateAllUsersPdf()
        {
            // Abrechnungszeitraum ermitteln (standardmäßig aktueller Monat)
            var now = DateTime.Now;
            var startDate = new DateTime(now.Year, now.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var pdfService = HttpContext.RequestServices.GetService(typeof(IPdfService)) as IPdfService;

            if (pdfService != null)
            {
                var pdfBytes = await pdfService.GenerateAllUsersTimeSheetPdfAsync(startDate, endDate);

                return File(pdfBytes, "application/pdf", $"Alle_Mitarbeiter_Zeiterfassung_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.pdf");
            }

            return BadRequest("PDF-Service ist nicht verfügbar.");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class ErrorViewModel
    {
        public string RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}