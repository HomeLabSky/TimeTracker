using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Web.Models;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Web.Controllers
{
    [Authorize]
    public class TimeEntryController : Controller
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ISettingsService _settingsService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHourlyRateService _hourlyRateService; 

        public TimeEntryController(
            ITimeEntryService timeEntryService,
            ISettingsService settingsService,
            UserManager<ApplicationUser> userManager,
            IHourlyRateService hourlyRateService)
        {
            _timeEntryService = timeEntryService;
            _settingsService = settingsService;
            _userManager = userManager;
            _hourlyRateService = hourlyRateService;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new TimeEntryViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimeEntryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);

                // Prüfe auf Zeitüberlappungen
                var overlappingEntries = await _timeEntryService.CheckTimeEntryOverlap(userId, model.WorkDate, model.StartTime, model.EndTime);

                if (overlappingEntries.Any())
                {
                    ModelState.AddModelError(string.Empty, "Für diesen Zeitraum existiert bereits ein Eintrag. Bitte bearbeiten Sie den bestehenden Eintrag.");
                    return View(model);
                }

                var timeEntry = new TimeEntry
                {
                    UserId = userId,
                    WorkDate = model.WorkDate.Date,
                    StartTime = model.StartTime,
                    EndTime = model.EndTime
                };

                try
                {
                    await _timeEntryService.AddTimeEntryAsync(timeEntry);
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
                catch (System.Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Fehler beim Speichern: {ex.Message}");
                }
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var timeEntry = await _timeEntryService.GetTimeEntryAsync(id);

            if (timeEntry == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            // Prüfen, ob der Eintrag dem aktuellen Benutzer gehört
            if (timeEntry.UserId != userId)
            {
                return Forbid();
            }

            // Berechnung des historischen Stundenlohns für diesen Eintrag
            var earnings = await _timeEntryService.CalculateEarningsAsync(timeEntry);

            var viewModel = new TimeEntryViewModel
            {
                Id = timeEntry.Id,
                WorkDate = timeEntry.WorkDate,
                StartTime = timeEntry.StartTime,
                EndTime = timeEntry.EndTime,
                Earnings = earnings
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TimeEntryViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);

                // Prüfe auf Zeitüberlappungen, schließe den aktuellen Eintrag aus
                var overlappingEntries = await _timeEntryService.CheckTimeEntryOverlap(userId, model.WorkDate, model.StartTime, model.EndTime, id);

                if (overlappingEntries.Any())
                {
                    ModelState.AddModelError(string.Empty, "Für diesen Zeitraum existiert bereits ein Eintrag. Bitte passen Sie die Zeiten an.");
                    return View(model);
                }

                var existingTimeEntry = await _timeEntryService.GetTimeEntryAsync(id);

                if (existingTimeEntry == null)
                {
                    return NotFound();
                }

                existingTimeEntry.WorkDate = model.WorkDate.Date;
                existingTimeEntry.StartTime = model.StartTime;
                existingTimeEntry.EndTime = model.EndTime;

                try
                {
                    await _timeEntryService.UpdateTimeEntryAsync(existingTimeEntry);
                    return RedirectToAction(nameof(HomeController.Index), "Home");
                }
                catch (System.Exception ex)
                {
                    ModelState.AddModelError(string.Empty, $"Fehler beim Aktualisieren: {ex.Message}");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var timeEntry = await _timeEntryService.GetTimeEntryAsync(id);

            if (timeEntry == null)
            {
                return NotFound();
            }

            var userId = _userManager.GetUserId(User);

            // Prüfen, ob der Eintrag dem aktuellen Benutzer gehört
            if (timeEntry.UserId != userId)
            {
                return Forbid();
            }

            await _timeEntryService.DeleteTimeEntryAsync(id);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}