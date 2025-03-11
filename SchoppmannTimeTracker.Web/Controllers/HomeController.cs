using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Web.Models;
using System.Diagnostics;

namespace SchoppmannTimeTracker.Web.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ISettingsService _settingsService;
        private readonly IPdfService _pdfService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEarningsCarryoverService _earningsCarryoverService;
        private readonly IMinijobSettingsService _minijobSettingsService;

        public HomeController(
            ITimeEntryService timeEntryService,
            ISettingsService settingsService,
            IPdfService pdfService,
            UserManager<ApplicationUser> userManager,
            IEarningsCarryoverService earningsCarryoverService,
            IMinijobSettingsService minijobSettingsService)
        {
            _timeEntryService = timeEntryService;
            _settingsService = settingsService;
            _pdfService = pdfService;
            _userManager = userManager;
            _earningsCarryoverService = earningsCarryoverService;
            _minijobSettingsService = minijobSettingsService;
        }

        public async Task<IActionResult> Index(int? year, int? month)
        {
            var userId = _userManager.GetUserId(User);

            // Wenn kein Jahr/Monat angegeben wurde, den aktuellen Abrechnungszeitraum verwenden
            DateTime selectedDate;
            if (year.HasValue && month.HasValue)
            {
                selectedDate = new DateTime(year.Value, month.Value, 1);
            }
            else
            {
                selectedDate = DateTime.Today;
            }

            // Abrechnungszeitraum ermitteln
            var (startDate, endDate) = await GetBillingPeriodDates(userId, selectedDate);

            // Zeiteinträge für den Zeitraum laden
            var timeEntries = await _timeEntryService.GetTimeEntriesForPeriodAsync(userId, startDate, endDate);

            // Einstellungen des Benutzers laden
            var settings = await _settingsService.GetUserSettingsAsync(userId);

            // Minijob-Einstellungen für den Zeitraum
            var minijobSettings = await _minijobSettingsService.GetSettingsForDateAsync(startDate);

            // Übertragung von/zu Folgemonat berechnen
            var periodYear = startDate.Year;
            var periodMonth = startDate.Month;
            var earningsSummary = await _earningsCarryoverService.GetEarningsSummaryAsync(userId, periodYear, periodMonth);

            decimal totalEarnings = 0;
            TimeSpan totalWorkHours = TimeSpan.Zero;

            // Zeiteinträge mit Earnings anreichern
            var timeEntryViewModels = new List<TimeEntryListItemViewModel>();
            foreach (var entry in timeEntries)
            {
                var workHours = entry.WorkingHours;
                var earnings = await _timeEntryService.CalculateEarningsAsync(entry);

                timeEntryViewModels.Add(new TimeEntryListItemViewModel
                {
                    Id = entry.Id,
                    WorkDate = entry.WorkDate,
                    StartTime = entry.StartTime,
                    EndTime = entry.EndTime,
                    Earnings = earnings
                });

                totalWorkHours += workHours;
                totalEarnings += earnings;
            }

            var viewModel = new TimeOverviewViewModel
            {
                TimeEntries = timeEntryViewModels.OrderBy(x => x.WorkDate).ThenBy(x => x.StartTime),
                StartDate = startDate,
                EndDate = endDate,
                TotalEarnings = totalEarnings,
                TotalWorkingHours = totalWorkHours,
                MinijobLimit = minijobSettings.MonthlyLimit,
                CarryoverIn = earningsSummary.carryoverIn,
                CarryoverOut = earningsSummary.carryoverOut,
                ReportedEarnings = earningsSummary.reportedEarnings,
                IsOverMinijobLimit = earningsSummary.carryoverOut > 0,
                CurrentYear = selectedDate.Year,
                CurrentMonth = selectedDate.Month,
                BillingPeriods = await GetAvailableBillingPeriods(userId)
            };

            return View(viewModel);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> GeneratePdf()
        {
            var userId = _userManager.GetUserId(User);
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            var pdfBytes = await _pdfService.GenerateTimeSheetPdfAsync(userId, startDate, endDate);
            var fileName = $"Zeiterfassung_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GenerateAllUsersPdf()
        {
            var userId = _userManager.GetUserId(User);
            var (startDate, endDate) = await _timeEntryService.GetCurrentBillingPeriodAsync(userId);

            var pdfBytes = await _pdfService.GenerateAllUsersTimeSheetPdfAsync(startDate, endDate);
            var fileName = $"Zeiterfassung_Alle_Benutzer_{startDate:yyyy-MM-dd}_{endDate:yyyy-MM-dd}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        private async Task<(DateTime StartDate, DateTime EndDate)> GetBillingPeriodDates(string userId, DateTime referenceDate)
        {
            // Benutzereinstellungen laden
            var settings = await _settingsService.GetUserSettingsAsync(userId);

            int startDay = settings.BillingPeriodStartDay;
            int endDay = settings.BillingPeriodEndDay;

            // Wenn der Endtag vor dem Starttag liegt, dann geht der Zeitraum über einen Monatswechsel
            DateTime startDate, endDate;

            // Wenn wir innerhalb eines Monats abrechnen (z.B. 1-31)
            if (startDay <= endDay)
            {
                // Wir sind im aktuellen Monat
                startDate = new DateTime(referenceDate.Year, referenceDate.Month, startDay);
                endDate = new DateTime(referenceDate.Year, referenceDate.Month, Math.Min(endDay, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month)));
            }
            else
            {
                // Wir sind über einen Monatswechsel (z.B. 15-14)
                // Bestimmen, ob wir uns im ersten oder zweiten Teil der Periode befinden
                if (referenceDate.Day >= startDay || referenceDate.Day < endDay)
                {
                    // Wenn der Tag nach dem Starttag oder vor dem Endtag liegt, beginnen wir im Vormonat
                    DateTime prevMonth = referenceDate.AddMonths(-1);
                    startDate = new DateTime(prevMonth.Year, prevMonth.Month, startDay);
                    endDate = new DateTime(referenceDate.Year, referenceDate.Month, Math.Min(endDay, DateTime.DaysInMonth(referenceDate.Year, referenceDate.Month)));
                }
                else
                {
                    // Sonst beginnen wir im aktuellen Monat
                    startDate = new DateTime(referenceDate.Year, referenceDate.Month, startDay);
                    DateTime nextMonth = referenceDate.AddMonths(1);
                    endDate = new DateTime(nextMonth.Year, nextMonth.Month, Math.Min(endDay, DateTime.DaysInMonth(nextMonth.Year, nextMonth.Month)));
                }
            }

            return (startDate, endDate);
        }

        private async Task<List<BillingPeriodViewModel>> GetAvailableBillingPeriods(string userId)
        {
            var periods = new List<BillingPeriodViewModel>();
            var today = DateTime.Today;
            var settings = await _settingsService.GetUserSettingsAsync(userId);

            // Finde das früheste Datum, an dem der Benutzer einen Zeiteintrag hat
            var allTimeEntries = await _timeEntryService.GetTimeEntriesForUserAsync(userId);

            // Wenn keine Einträge vorhanden sind, zeige nur den aktuellen Monat an
            if (allTimeEntries == null || !allTimeEntries.Any())
            {
                var (startDate, endDate) = await GetBillingPeriodDates(userId, today);

                periods.Add(new BillingPeriodViewModel
                {
                    Year = today.Year,
                    Month = today.Month,
                    StartDate = startDate,
                    EndDate = endDate,
                    DisplayName = $"{today:MMMM yyyy}"
                });

                return periods;
            }

            // Das früheste Datum ermitteln
            var earliestDate = allTimeEntries.Min(e => e.WorkDate);

            // Abgerundet auf den ersten Tag des Monats
            earliestDate = new DateTime(earliestDate.Year, earliestDate.Month, 1);

            // Erstelle Perioden vom frühesten Datum bis heute
            var currentDate = today;

            // Berechne die Anzahl der Monate zwischen dem frühesten Datum und heute
            int totalMonths = ((today.Year - earliestDate.Year) * 12) + today.Month - earliestDate.Month + 1;

            for (int i = 0; i < totalMonths; i++)
            {
                var date = today.AddMonths(-i);

                // Überspringe Zeiträume, die vor dem frühesten Datum liegen
                if (date.Year < earliestDate.Year || (date.Year == earliestDate.Year && date.Month < earliestDate.Month))
                    continue;

                var (startDate, endDate) = await GetBillingPeriodDates(userId, date);

                periods.Add(new BillingPeriodViewModel
                {
                    Year = date.Year,
                    Month = date.Month,
                    StartDate = startDate,
                    EndDate = endDate,
                    DisplayName = $"{date:MMMM yyyy}"
                });
            }

            return periods.OrderByDescending(p => p.Year).ThenByDescending(p => p.Month).ToList();
        }
    }
}