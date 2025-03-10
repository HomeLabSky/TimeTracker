using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using SchoppmannTimeTracker.Web.Areas.Admin.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class UserManagerController : Controller
    {
        private readonly IUserService _userService;
        private readonly ISettingsService _settingsService;
        private readonly RoleManager<IdentityRole> _roleManager;

        public UserManagerController(
            IUserService userService,
            ISettingsService settingsService,
            RoleManager<IdentityRole> roleManager)
        {
            _userService = userService;
            _settingsService = settingsService;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var users = await _userService.GetAllUsersAsync();

            // Alle Benutzer-IDs sammeln
            var userIds = users.Select(u => u.Id).ToList();

            // Rollen für alle Benutzer in einem Durchgang abrufen
            var userRolesMapping = await _userService.GetUserRolesMappingAsync(userIds);

            var viewModel = new UserListViewModel
            {
                Users = users.Select(user => new UserViewModel
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Role = userRolesMapping.ContainsKey(user.Id) ?
                           userRolesMapping[user.Id].FirstOrDefault() ?? "Keine Rolle" :
                           "Keine Rolle"
                })
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var availableRoles = await GetAvailableRolesAsync();

            var viewModel = new CreateUserViewModel
            {
                AvailableRoles = availableRoles
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email, // E-Mail als Benutzername verwenden
                    Email = model.Email,
                    FirstName = string.IsNullOrEmpty(model.FirstName) ? "" : model.FirstName,
                    LastName = string.IsNullOrEmpty(model.LastName) ? "" : model.LastName,
                    EmailConfirmed = true // Keine E-Mail-Bestätigung erforderlich
                };

                var result = await _userService.CreateUserAsync(user, model.Password, model.Role);

                if (result)
                {
                    // Standardeinstellungen für den neuen Benutzer erstellen
                    await _settingsService.GetUserSettingsAsync(user.Id);

                    TempData["StatusMessage"] = "Benutzer wurde erfolgreich erstellt.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "Fehler beim Erstellen des Benutzers.");
            }

            // Wenn wir hier ankommen, ist etwas schief gelaufen
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userService.GetUserRolesAsync(id);
            var availableRoles = await GetAvailableRolesAsync();

            var viewModel = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Role = roles.FirstOrDefault() ?? "",
                AvailableRoles = availableRoles
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            // Passwort-Validierungsfehler entfernen, wenn das Feld leer ist
            if (string.IsNullOrEmpty(model.Password))
            {
                // Gründliche Bereinigung des ModelState für Passwort-bezogene Felder
                foreach (var key in ModelState.Keys.ToList())
                {
                    if (key.Contains("Password") || key.Contains("ConfirmPassword"))
                    {
                        ModelState.Remove(key);
                    }
                }
            }

            // Entferne auch AvailableRoles aus dem ModelState
            ModelState.Remove("AvailableRoles");

            // Debug-Ausgabe aller verbleibenden Fehler
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Count > 0)
                    {
                        System.Diagnostics.Debug.WriteLine($"Fehler in {state.Key}: {string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage))}");
                    }
                }
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _userService.GetUserByIdAsync(id);

                if (existingUser == null)
                {
                    return NotFound();
                }

                existingUser.FirstName = model.FirstName;
                existingUser.LastName = model.LastName;
                existingUser.Email = model.Email;
                existingUser.UserName = model.Email; // E-Mail als Benutzername verwenden

                var updateResult = await _userService.UpdateUserAsync(existingUser);

                if (updateResult)
                {
                    // Rollen aktualisieren
                    var currentRoles = await _userService.GetUserRolesAsync(id);

                    // Alle aktuellen Rollen entfernen
                    foreach (var role in currentRoles)
                    {
                        await _userService.RemoveUserFromRoleAsync(id, role);
                    }

                    // Neue Rolle hinzufügen
                    if (!string.IsNullOrEmpty(model.Role))
                    {
                        await _userService.AddUserToRoleAsync(id, model.Role);
                    }

                    // Passwort aktualisieren, wenn eines eingegeben wurde
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        var userManager = HttpContext.RequestServices.GetService(typeof(UserManager<ApplicationUser>)) as UserManager<ApplicationUser>;
                        if (userManager != null)
                        {
                            var token = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                            await userManager.ResetPasswordAsync(existingUser, token, model.Password);
                        }
                    }

                    TempData["StatusMessage"] = "Benutzer wurde erfolgreich aktualisiert.";
                    return RedirectToAction(nameof(Index));
                }

                ModelState.AddModelError(string.Empty, "Fehler beim Aktualisieren des Benutzers.");
            }

            // Wenn wir hier ankommen, ist etwas schief gelaufen
            model.AvailableRoles = await GetAvailableRolesAsync();
            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            var user = await _userService.GetUserByIdAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            // Verhindern, dass der aktuell angemeldete Benutzer gelöscht wird
            if (User.Identity.Name == user.Email)
            {
                TempData["ErrorMessage"] = "Sie können Ihren eigenen Benutzer nicht löschen.";
                return RedirectToAction(nameof(Index));
            }

            var result = await _userService.DeleteUserAsync(id);

            if (result)
            {
                TempData["StatusMessage"] = "Benutzer wurde erfolgreich gelöscht.";
            }
            else
            {
                TempData["ErrorMessage"] = "Fehler beim Löschen des Benutzers.";
            }

            return RedirectToAction(nameof(Index));
        }

        // Hilfsmethoden
        private async Task<List<SelectListItem>> GetAvailableRolesAsync()
        {
            var roles = new List<SelectListItem>();

            // Admin und User Rollen hinzufügen
            var adminRoleExists = await _roleManager.RoleExistsAsync("Admin");
            var userRoleExists = await _roleManager.RoleExistsAsync("User");

            if (!adminRoleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole("Admin"));
            }

            if (!userRoleExists)
            {
                await _roleManager.CreateAsync(new IdentityRole("User"));
            }

            roles.Add(new SelectListItem { Value = "Admin", Text = "Administrator" });
            roles.Add(new SelectListItem { Value = "User", Text = "Benutzer" });

            return roles;
        }
    }
}