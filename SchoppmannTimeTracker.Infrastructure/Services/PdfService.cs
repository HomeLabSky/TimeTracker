using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.IO.Font.Constants;
using Microsoft.AspNetCore.Identity;
using SchoppmannTimeTracker.Core.Entities;
using SchoppmannTimeTracker.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SchoppmannTimeTracker.Infrastructure.Services
{
    public class PdfService : IPdfService
    {
        private readonly ITimeEntryService _timeEntryService;
        private readonly ISettingsService _settingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PdfService(
            ITimeEntryService timeEntryService,
            ISettingsService settingsService,
            UserManager<ApplicationUser> userManager)
        {
            _timeEntryService = timeEntryService;
            _settingsService = settingsService;
            _userManager = userManager;
        }

        public async Task<byte[]> GenerateTimeSheetPdfAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("Benutzer nicht gefunden");

            var timeEntries = await _timeEntryService.GetTimeEntriesForPeriodAsync(userId, startDate, endDate);
            var settings = await _settingsService.GetUserSettingsAsync(userId);

            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Schriftarten definieren
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                // Titel
                document.Add(new Paragraph($"Zeiterfassung für {user.FirstName} {user.LastName}")
                    .SetFont(boldFont)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Abrechnungszeitraum: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}")
                    .SetFont(normalFont)
                    .SetTextAlignment(TextAlignment.CENTER));

                // Tabelle erstellen
                Table table = new Table(5).UseAllAvailableWidth();

                // Header-Zellen
                table.AddHeaderCell(new Cell().Add(new Paragraph("Datum").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Kommen").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Gehen").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Arbeitszeit").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                table.AddHeaderCell(new Cell().Add(new Paragraph("Lohn (€)").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                decimal totalEarnings = 0;
                TimeSpan totalWorkHours = TimeSpan.Zero;

                foreach (var entry in timeEntries)
                {
                    var workHours = _timeEntryService.CalculateWorkHours(entry);
                    var earnings = _timeEntryService.CalculateEarnings(entry, settings);

                    table.AddCell(new Cell().Add(new Paragraph(entry.WorkDate.ToString("dd.MM.yyyy")).SetFont(normalFont)));
                    table.AddCell(new Cell().Add(new Paragraph(entry.StartTime.ToString(@"hh\:mm")).SetFont(normalFont)));
                    table.AddCell(new Cell().Add(new Paragraph(entry.EndTime.ToString(@"hh\:mm")).SetFont(normalFont)));
                    table.AddCell(new Cell().Add(new Paragraph(workHours.ToString(@"hh\:mm")).SetFont(normalFont)));
                    table.AddCell(new Cell().Add(new Paragraph($"{earnings:N2}").SetFont(normalFont)));

                    totalWorkHours += workHours;
                    totalEarnings += earnings;
                }

                // Zusammenfassung
                table.AddCell(new Cell().Add(new Paragraph("Gesamt").SetFont(boldFont)));
                table.AddCell(new Cell().Add(new Paragraph("")));
                table.AddCell(new Cell().Add(new Paragraph("")));
                table.AddCell(new Cell().Add(new Paragraph(totalWorkHours.ToString(@"hh\:mm")).SetFont(boldFont)));
                table.AddCell(new Cell().Add(new Paragraph($"{totalEarnings:N2}").SetFont(boldFont)));

                document.Add(table);

                // Zusätzliche Informationen
                document.Add(new Paragraph($"Stundenlohn: {settings.HourlyRate:N2} €")
                    .SetFont(normalFont)
                    .SetMarginTop(20));
                document.Add(new Paragraph($"Gesamtverdienst: {totalEarnings:N2} €")
                    .SetFont(boldFont));

                document.Close();
                return memoryStream.ToArray();
            }
        }

        public async Task<byte[]> GenerateAllUsersTimeSheetPdfAsync(DateTime startDate, DateTime endDate)
        {
            var users = _userManager.Users.ToList();

            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Schriftarten definieren
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

                document.Add(new Paragraph("Zeiterfassung aller Mitarbeiter")
                    .SetFont(boldFont)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Abrechnungszeitraum: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}")
                    .SetFont(normalFont)
                    .SetTextAlignment(TextAlignment.CENTER));

                decimal totalCompanyEarnings = 0;

                foreach (var user in users)
                {
                    // Zeiteinträge und Einstellungen für jeden Benutzer laden
                    var timeEntries = await _timeEntryService.GetTimeEntriesForPeriodAsync(user.Id, startDate, endDate);
                    var settings = await _settingsService.GetUserSettingsAsync(user.Id);

                    if (timeEntries.Count == 0)
                        continue; // Keine Einträge für diesen Benutzer

                    // Benutzer-Überschrift
                    document.Add(new Paragraph($"Mitarbeiter: {user.FirstName} {user.LastName}")
                        .SetFont(boldFont)
                        .SetFontSize(14)
                        .SetMarginTop(20));

                    // Tabelle für jeden Benutzer erstellen
                    Table table = new Table(5).UseAllAvailableWidth();

                    // Header-Zellen
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Datum").SetFont(boldFont))
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Kommen").SetFont(boldFont))
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Gehen").SetFont(boldFont))
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Arbeitszeit").SetFont(boldFont))
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));
                    table.AddHeaderCell(new Cell().Add(new Paragraph("Lohn (€)").SetFont(boldFont))
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY));

                    decimal totalUserEarnings = 0;
                    TimeSpan totalUserWorkHours = TimeSpan.Zero;

                    foreach (var entry in timeEntries)
                    {
                        var workHours = _timeEntryService.CalculateWorkHours(entry);
                        var earnings = _timeEntryService.CalculateEarnings(entry, settings);

                        table.AddCell(new Cell().Add(new Paragraph(entry.WorkDate.ToString("dd.MM.yyyy")).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph(entry.StartTime.ToString(@"hh\:mm")).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph(entry.EndTime.ToString(@"hh\:mm")).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph(workHours.ToString(@"hh\:mm")).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph($"{earnings:N2}").SetFont(normalFont)));

                        totalUserWorkHours += workHours;
                        totalUserEarnings += earnings;
                    }

                    // Zusammenfassung für Benutzer
                    table.AddCell(new Cell().Add(new Paragraph("Gesamt").SetFont(boldFont)));
                    table.AddCell(new Cell().Add(new Paragraph("")));
                    table.AddCell(new Cell().Add(new Paragraph("")));
                    table.AddCell(new Cell().Add(new Paragraph(totalUserWorkHours.ToString(@"hh\:mm")).SetFont(boldFont)));
                    table.AddCell(new Cell().Add(new Paragraph($"{totalUserEarnings:N2}").SetFont(boldFont)));

                    document.Add(table);

                    // Zusätzliche Informationen pro Benutzer
                    document.Add(new Paragraph($"Stundenlohn: {settings.HourlyRate:N2} €")
                        .SetFont(normalFont));
                    document.Add(new Paragraph($"Gesamtverdienst: {totalUserEarnings:N2} €")
                        .SetFont(boldFont));

                    totalCompanyEarnings += totalUserEarnings;
                }

                // Gesamtzusammenfassung für alle Benutzer
                document.Add(new Paragraph("Zusammenfassung für alle Mitarbeiter")
                    .SetFont(boldFont)
                    .SetFontSize(16)
                    .SetMarginTop(30));
                document.Add(new Paragraph($"Gesamtkosten für den Zeitraum: {totalCompanyEarnings:N2} €")
                    .SetFont(boldFont));

                document.Close();
                return memoryStream.ToArray();
            }
        }
    }
}