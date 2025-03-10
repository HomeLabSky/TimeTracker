using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Web.Models;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Web.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly ISettingsService _settingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public SettingsController(
            ISettingsService settingsService,
            UserManager<ApplicationUser> userManager)
        {
            _settingsService = settingsService;
            _userManager = userManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string userId = null)
        {
            // Wenn userId nicht angegeben ist, verwende den aktuellen Benutzer
            var currentUserId = _userManager.GetUserId(User);

            // Prüfen, ob ein Administrator Einstellungen eines anderen Benutzers bearbeitet
            if (!string.IsNullOrEmpty(userId) && userId != currentUserId)
            {
                // Nur Administratoren dürfen Einstellungen anderer Benutzer bearbeiten
                if (!User.IsInRole("Admin"))
                {
                    return Forbid();
                }
            }
            else
            {
                // Wenn kein userId angegeben wurde oder es der aktuelle Benutzer ist,
                // verwende den aktuellen Benutzer
                userId = currentUserId;
            }

            var settings = await _settingsService.GetUserSettingsAsync(userId);
            var user = await _userManager.FindByIdAsync(userId);

            // Wenn InvoiceEmail leer ist, setze Email als Standard
            if (string.IsNullOrEmpty(settings.InvoiceEmail))
            {
                settings.InvoiceEmail = user.Email;
            }

            var viewModel = new SettingsViewModel
            {
                Id = settings.Id,
                UserId = settings.UserId,
                HourlyRate = settings.HourlyRate,
                HourlyRateValidFrom = DateTime.Today, // Standardwert: heute
                BillingPeriodStartDay = settings.BillingPeriodStartDay,
                BillingPeriodEndDay = settings.BillingPeriodEndDay,
                InvoiceEmail = settings.InvoiceEmail,
                IsAdminEdit = userId != currentUserId,
                UserFullName = $"{user.FirstName} {user.LastName}"
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SettingsViewModel model)
        {
            var currentUserId = _userManager.GetUserId(User);

            // Überprüfe, ob der Benutzer berechtigt ist, diese Einstellungen zu bearbeiten
            if (model.UserId != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Bestehende Einstellungen laden
            var existingSettings = await _settingsService.GetUserSettingsAsync(model.UserId);

            // Prüfen, ob der Stundenlohn geändert wurde
            bool hourlyRateChanged = existingSettings.HourlyRate != model.HourlyRate;

            // Für normale Benutzer nur die InvoiceEmail validieren und aktualisieren
            if (!User.IsInRole("Admin") && model.UserId == currentUserId)
            {
                // Nur die E-Mail-Validierung beibehalten
                ModelState.Clear();

                if (string.IsNullOrEmpty(model.InvoiceEmail))
                {
                    ModelState.AddModelError("InvoiceEmail", "E-Mail für Lohnzettel ist erforderlich");
                    return View(model);
                }

                if (!new EmailAddressAttribute().IsValid(model.InvoiceEmail))
                {
                    ModelState.AddModelError("InvoiceEmail", "Keine gültige E-Mail-Adresse");
                    return View(model);
                }

                // Nur InvoiceEmail aktualisieren
                existingSettings.InvoiceEmail = model.InvoiceEmail;

                await _settingsService.CreateOrUpdateSettingsAsync(existingSettings);

                TempData["StatusMessage"] = "E-Mail für Lohnzettel wurde aktualisiert.";
                return RedirectToAction(nameof(Index));
            }

            // Für Administratoren validiere alle Felder
            if (ModelState.IsValid)
            {

                DateTime hourlyRateValidFrom = existingSettings.HourlyRateValidFrom;
                if (hourlyRateChanged)
                {
                    hourlyRateValidFrom = model.HourlyRateValidFrom;
                }

                var settings = new UserSettings
                {
                    Id = model.Id,
                    UserId = model.UserId,
                    HourlyRate = model.HourlyRate,
                    BillingPeriodStartDay = model.BillingPeriodStartDay,
                    BillingPeriodEndDay = model.BillingPeriodEndDay,
                    InvoiceEmail = model.InvoiceEmail,
                    HourlyRateValidFrom = hourlyRateValidFrom
                };

                await _settingsService.CreateOrUpdateSettingsAsync(settings);

                // Wenn sich der Stundenlohn geändert hat
                if (hourlyRateChanged)
                {
                    TempData["StatusMessage"] = $"Einstellungen wurden aktualisiert. Der neue Stundenlohn gilt ab {model.HourlyRateValidFrom:dd.MM.yyyy}.";
                }
                else
                {
                    TempData["StatusMessage"] = "Einstellungen wurden aktualisiert.";
                }

                // Wenn ein Admin die Einstellungen eines anderen Benutzers bearbeitet hat,
                // leite auf die Benutzerübersicht zurück
                if (model.UserId != currentUserId)
                {
                    return RedirectToAction("Index", "UserManager", new { area = "Admin" });
                }

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // Für AJAX-Anfrage, um Stundenlohn-Datum-Feld anzuzeigen
        [HttpGet]
        public IActionResult CheckHourlyRateChanged(decimal currentRate, decimal newRate)
        {
            return Json(new { changed = currentRate != newRate });
        }
    }
}