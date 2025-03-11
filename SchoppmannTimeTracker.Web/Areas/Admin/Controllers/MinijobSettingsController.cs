using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Web.Areas.Admin.Models;
using System.Linq;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class MinijobSettingsController : Controller
    {
        private readonly IMinijobSettingsService _minijobSettingsService;

        public MinijobSettingsController(IMinijobSettingsService minijobSettingsService)
        {
            _minijobSettingsService = minijobSettingsService;
        }

        public async Task<IActionResult> Index()
        {
            var settingsHistory = await _minijobSettingsService.GetSettingsHistoryAsync();
            var currentSettings = await _minijobSettingsService.GetCurrentSettingsAsync();

            var viewModel = new MinijobSettingsListViewModel
            {
                Settings = settingsHistory.Select(s => new MinijobSettingsViewModel
                {
                    Id = s.Id,
                    MonthlyLimit = s.MonthlyLimit,
                    Description = s.Description,
                    ValidFrom = s.ValidFrom,
                    ValidTo = s.ValidTo,
                    IsActive = s.IsActive
                }),
                CurrentSetting = new MinijobSettingsViewModel
                {
                    Id = currentSettings.Id,
                    MonthlyLimit = currentSettings.MonthlyLimit,
                    Description = currentSettings.Description,
                    ValidFrom = currentSettings.ValidFrom,
                    ValidTo = currentSettings.ValidTo,
                    IsActive = currentSettings.IsActive
                }
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new MinijobSettingsViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MinijobSettingsViewModel model)
        {
            if (ModelState.IsValid)
            {
                var settings = new MinijobSettings
                {
                    MonthlyLimit = model.MonthlyLimit,
                    Description = model.Description,
                    ValidFrom = model.ValidFrom,
                    ValidTo = model.ValidTo,
                    IsActive = model.IsActive
                };

                await _minijobSettingsService.UpdateSettingsAsync(settings);

                TempData["StatusMessage"] = "Die Minijob-Einstellungen wurden erfolgreich gespeichert.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var settingsHistory = await _minijobSettingsService.GetSettingsHistoryAsync();
            var settings = settingsHistory.FirstOrDefault(s => s.Id == id);

            if (settings == null)
            {
                return NotFound();
            }

            var viewModel = new MinijobSettingsViewModel
            {
                Id = settings.Id,
                MonthlyLimit = settings.MonthlyLimit,
                Description = settings.Description,
                ValidFrom = settings.ValidFrom,
                ValidTo = settings.ValidTo,
                IsActive = settings.IsActive
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MinijobSettingsViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var settings = new MinijobSettings
                {
                    Id = model.Id,
                    MonthlyLimit = model.MonthlyLimit,
                    Description = model.Description,
                    ValidFrom = model.ValidFrom,
                    ValidTo = model.ValidTo,
                    IsActive = model.IsActive
                };

                await _minijobSettingsService.UpdateSettingsAsync(settings);

                TempData["StatusMessage"] = "Die Minijob-Einstellungen wurden erfolgreich aktualisiert.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}