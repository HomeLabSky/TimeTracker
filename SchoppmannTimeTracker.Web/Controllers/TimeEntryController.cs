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

        public TimeEntryController(
            ITimeEntryService timeEntryService,
            ISettingsService settingsService,
            UserManager<ApplicationUser> userManager)
        {
            _timeEntryService = timeEntryService;
            _settingsService = settingsService;
            _userManager = userManager;
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
                if (model.EndTime <= model.StartTime)
                {
                    ModelState.AddModelError("EndTime", "Die Endzeit muss nach der Startzeit liegen.");
                    return View(model);
                }

                var userId = _userManager.GetUserId(User);

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

            var settings = await _settingsService.GetUserSettingsAsync(userId);

            var viewModel = new TimeEntryViewModel
            {
                Id = timeEntry.Id,
                WorkDate = timeEntry.WorkDate,
                StartTime = timeEntry.StartTime,
                EndTime = timeEntry.EndTime,
                Earnings = _timeEntryService.CalculateEarnings(timeEntry, settings)
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
                if (model.EndTime <= model.StartTime)
                {
                    ModelState.AddModelError("EndTime", "Die Endzeit muss nach der Startzeit liegen.");
                    return View(model);
                }

                var existingTimeEntry = await _timeEntryService.GetTimeEntryAsync(id);

                if (existingTimeEntry == null)
                {
                    return NotFound();
                }

                var userId = _userManager.GetUserId(User);

                // Prüfen, ob der Eintrag dem aktuellen Benutzer gehört
                if (existingTimeEntry.UserId != userId)
                {
                    return Forbid();
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