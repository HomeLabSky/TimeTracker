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
        private readonly IHourlyRateService _hourlyRateService;
        private readonly IEarningsCarryoverService _earningsCarryoverService;
        private readonly IMinijobSettingsService _minijobSettingsService;
        private readonly UserManager<ApplicationUser> _userManager;

        public PdfService(
            ITimeEntryService timeEntryService,
            ISettingsService settingsService,
            IHourlyRateService hourlyRateService,
            IEarningsCarryoverService earningsCarryoverService,
            IMinijobSettingsService minijobSettingsService,
            UserManager<ApplicationUser> userManager)
        {
            _timeEntryService = timeEntryService;
            _settingsService = settingsService;
            _hourlyRateService = hourlyRateService;
            _earningsCarryoverService = earningsCarryoverService;
            _minijobSettingsService = minijobSettingsService;
            _userManager = userManager;
        }

        public async Task<byte[]> GenerateTimeSheetPdfAsync(string userId, DateTime startDate, DateTime endDate)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                throw new ArgumentException("Benutzer nicht gefunden");

            var timeEntries = await _timeEntryService.GetTimeEntriesForPeriodAsync(userId, startDate, endDate);
            var settings = await _settingsService.GetUserSettingsAsync(userId);

            // Get minijob settings for the period
            var minijobSettings = await _minijobSettingsService.GetSettingsForDateAsync(startDate);

            // Get carryover information
            var year = startDate.Year;
            var month = startDate.Month;
            var carryoverInformation = await _earningsCarryoverService.GetEarningsSummaryAsync(userId, year, month);

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
                    // Verwende die asynchrone Methode für die Berechnung des historischen Stundenlohns
                    var earnings = await _timeEntryService.CalculateEarningsAsync(entry);

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
                document.Add(new Paragraph($"Aktueller Stundenlohn: {settings.HourlyRate:N2} €")
                    .SetFont(normalFont)
                    .SetMarginTop(20));

                // Minijob-Informationen
                Table minijobTable = new Table(2).UseAllAvailableWidth();
                minijobTable.SetMarginTop(10);

                // Header
                minijobTable.AddHeaderCell(new Cell(1, 2).Add(new Paragraph("Minijob-Übersicht").SetFont(boldFont))
                    .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                    .SetTextAlignment(TextAlignment.CENTER));

                // Monatliches Limit
                minijobTable.AddCell(new Cell().Add(new Paragraph("Minijob-Grenze:").SetFont(boldFont)));
                minijobTable.AddCell(new Cell().Add(new Paragraph($"{minijobSettings.MonthlyLimit:N2} €").SetFont(normalFont)));

                // Übertrag aus dem Vormonat
                minijobTable.AddCell(new Cell().Add(new Paragraph("Übertrag aus Vormonat:").SetFont(boldFont)));
                minijobTable.AddCell(new Cell().Add(new Paragraph($"{carryoverInformation.carryoverIn:N2} €").SetFont(normalFont)));

                // Verdienstanspruch
                minijobTable.AddCell(new Cell().Add(new Paragraph("Tatsächlicher Verdienstanspruch:").SetFont(boldFont)));
                minijobTable.AddCell(new Cell().Add(new Paragraph($"{totalEarnings:N2} €").SetFont(normalFont)));

                // Ausgezahlter Betrag
                minijobTable.AddCell(new Cell().Add(new Paragraph("Ausgezahlter Betrag:").SetFont(boldFont)));
                minijobTable.AddCell(new Cell().Add(new Paragraph($"{carryoverInformation.reportedEarnings:N2} €").SetFont(normalFont)));

                // Übertrag in den Folgemonat
                minijobTable.AddCell(new Cell().Add(new Paragraph("Übertrag in Folgemonat:").SetFont(boldFont)));
                var carryoverOutCell = new Cell().Add(new Paragraph($"{carryoverInformation.carryoverOut:N2} €").SetFont(normalFont));

                // If there's carryover out, highlight it
                if (carryoverInformation.carryoverOut > 0)
                {
                    carryoverOutCell.SetBackgroundColor(new DeviceRgb(255, 240, 240));
                }

                minijobTable.AddCell(carryoverOutCell);

                document.Add(minijobTable);

                // Hinweis bei Überschreitung des Limits
                if (carryoverInformation.carryoverOut > 0)
                {
                    PdfFont italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
                    document.Add(new Paragraph("Hinweis: Die Minijob-Grenze wurde überschritten. Der Überschuss wird in den nächsten Monat übertragen.")
                        .SetFont(italicFont)
                        .SetFontSize(9)
                        .SetMarginTop(5));
                }

                // Hinweis auf historische Stundenlöhne
                if (await HasHistoricalRatesAsync(userId))
                {
                    PdfFont italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                    document.Add(new Paragraph("Hinweis: Die Berechnungen berücksichtigen historische Stundenlohnsätze.")
                        .SetFont(italicFont)
                        .SetFontSize(9)
                        .SetMarginTop(5));
                }

                document.Close();
                return memoryStream.ToArray();
            }
        }

        private async Task<bool> HasHistoricalRatesAsync(string userId)
        {
            var history = await _hourlyRateService.GetRateHistoryAsync(userId);
            return history.Count > 1; // Mehr als 1 Eintrag bedeutet, dass es historische Änderungen gibt
        }

        public async Task<byte[]> GenerateAllUsersTimeSheetPdfAsync(DateTime startDate, DateTime endDate)
        {
            var users = _userManager.Users.ToList();
            var minijobSettings = await _minijobSettingsService.GetSettingsForDateAsync(startDate);

            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // Schriftarten definieren
                PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                PdfFont italicFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);

                document.Add(new Paragraph("Zeiterfassung aller Mitarbeiter")
                    .SetFont(boldFont)
                    .SetFontSize(18)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Abrechnungszeitraum: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}")
                    .SetFont(normalFont)
                    .SetTextAlignment(TextAlignment.CENTER));

                document.Add(new Paragraph($"Aktuelle Minijob-Grenze: {minijobSettings.MonthlyLimit:N2} €")
                    .SetFont(normalFont)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetMarginBottom(10));

                decimal totalCompanyEarnings = 0;
                decimal totalReportedEarnings = 0;

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
                        // Verwende die asynchrone Methode für die Berechnung des historischen Stundenlohns
                        var earnings = await _timeEntryService.CalculateEarningsAsync(entry);

                        table.AddCell(new Cell().Add(new Paragraph(entry.WorkDate.ToString("dd.MM.yyyy")).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph(entry.StartTime.ToString(@"hh\:mm")).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph(entry.EndTime.ToString(@"hh\:mm")).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph(workHours.ToString(@"hh\:mm")).SetFont(normalFont)));
                        table.AddCell(new Cell().Add(new Paragraph($"{earnings:N2}").SetFont(normalFont)));

                        totalUserWorkHours += workHours;
                        totalUserEarnings += earnings;
                    }

                    // Get carryover information for this user
                    var year = startDate.Year;
                    var month = startDate.Month;
                    var carryoverInformation = await _earningsCarryoverService.GetEarningsSummaryAsync(user.Id, year, month);

                    // Zusammenfassung für Benutzer
                    table.AddCell(new Cell().Add(new Paragraph("Gesamt").SetFont(boldFont)));
                    table.AddCell(new Cell().Add(new Paragraph("")));
                    table.AddCell(new Cell().Add(new Paragraph("")));
                    table.AddCell(new Cell().Add(new Paragraph(totalUserWorkHours.ToString(@"hh\:mm")).SetFont(boldFont)));
                    table.AddCell(new Cell().Add(new Paragraph($"{totalUserEarnings:N2}").SetFont(boldFont)));

                    document.Add(table);

                    // Minijob-Informationen pro Benutzer
                    Table minijobTable = new Table(2).UseAllAvailableWidth();
                    minijobTable.SetMarginTop(10);

                    // Minijob-Infos
                    minijobTable.AddHeaderCell(new Cell(1, 2).Add(new Paragraph("Minijob-Übersicht").SetFont(boldFont))
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetTextAlignment(TextAlignment.CENTER));

                    // Monatliches Limit
                    minijobTable.AddCell(new Cell().Add(new Paragraph("Minijob-Grenze:").SetFont(boldFont)));
                    minijobTable.AddCell(new Cell().Add(new Paragraph($"{minijobSettings.MonthlyLimit:N2} €").SetFont(normalFont)));

                    // Übertrag aus dem Vormonat
                    minijobTable.AddCell(new Cell().Add(new Paragraph("Übertrag aus Vormonat:").SetFont(boldFont)));
                    minijobTable.AddCell(new Cell().Add(new Paragraph($"{carryoverInformation.carryoverIn:N2} €").SetFont(normalFont)));

                    // Verdienstanspruch
                    minijobTable.AddCell(new Cell().Add(new Paragraph("Tatsächlicher Verdienstanspruch:").SetFont(boldFont)));
                    minijobTable.AddCell(new Cell().Add(new Paragraph($"{totalUserEarnings:N2} €").SetFont(normalFont)));

                    // Ausgezahlter Betrag
                    minijobTable.AddCell(new Cell().Add(new Paragraph("Ausgezahlter Betrag:").SetFont(boldFont)));
                    minijobTable.AddCell(new Cell().Add(new Paragraph($"{carryoverInformation.reportedEarnings:N2} €").SetFont(normalFont)));

                    // Übertrag in den Folgemonat
                    minijobTable.AddCell(new Cell().Add(new Paragraph("Übertrag in Folgemonat:").SetFont(boldFont)));
                    var carryoverOutCell = new Cell().Add(new Paragraph($"{carryoverInformation.carryoverOut:N2} €").SetFont(normalFont));

                    // If there's carryover out, highlight it
                    if (carryoverInformation.carryoverOut > 0)
                    {
                        carryoverOutCell.SetBackgroundColor(new DeviceRgb(255, 240, 240));
                    }

                    minijobTable.AddCell(carryoverOutCell);

                    document.Add(minijobTable);

                    // Hinweis bei Überschreitung des Limits
                    if (carryoverInformation.carryoverOut > 0)
                    {
                        document.Add(new Paragraph("Hinweis: Die Minijob-Grenze wurde überschritten. Der Überschuss wird in den nächsten Monat übertragen.")
                            .SetFont(italicFont)
                            .SetFontSize(9)
                            .SetMarginTop(5));
                    }

                    // Zusätzliche Informationen pro Benutzer
                    document.Add(new Paragraph($"Aktueller Stundenlohn: {settings.HourlyRate:N2} €")
                        .SetFont(normalFont)
                        .SetMarginTop(5));

                    // Hinweis auf historische Stundenlöhne
                    if (await HasHistoricalRatesAsync(user.Id))
                    {
                        document.Add(new Paragraph("Hinweis: Die Berechnungen berücksichtigen historische Stundenlohnsätze.")
                            .SetFont(italicFont)
                            .SetFontSize(9));
                    }

                    totalCompanyEarnings += totalUserEarnings;
                    totalReportedEarnings += carryoverInformation.reportedEarnings;
                }

                // Gesamtzusammenfassung für alle Benutzer
                document.Add(new Paragraph("Zusammenfassung für alle Mitarbeiter")
                    .SetFont(boldFont)
                    .SetFontSize(16)
                    .SetMarginTop(30));

                Table summaryTable = new Table(2).UseAllAvailableWidth();

                summaryTable.AddCell(new Cell().Add(new Paragraph("Gesamter Verdienstanspruch:").SetFont(boldFont)));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{totalCompanyEarnings:N2} €").SetFont(boldFont)));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Tatsächliche Auszahlung:").SetFont(boldFont)));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{totalReportedEarnings:N2} €").SetFont(boldFont)));

                summaryTable.AddCell(new Cell().Add(new Paragraph("Differenz (Übertrag gesamt):").SetFont(boldFont)));
                summaryTable.AddCell(new Cell().Add(new Paragraph($"{(totalCompanyEarnings - totalReportedEarnings):N2} €").SetFont(boldFont)));

                document.Add(summaryTable);

                document.Close();
                return memoryStream.ToArray();
            }
        }
    }
}