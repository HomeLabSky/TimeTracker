// Automatisches Ausblenden von Alerts nach 5 Sekunden
document.addEventListener('DOMContentLoaded', function () {
    // Alle Alert-Elemente auswählen
    const alerts = document.querySelectorAll('.alert');

    // Für jedes Alert-Element einen Timeout setzen
    alerts.forEach(function (alert) {
        setTimeout(function () {
            // Bootstrap 5 Fade-Out mit dispose
            const bsAlert = new bootstrap.Alert(alert);
            alert.classList.remove('show');
            // Nach dem Ausblenden entfernen
            alert.addEventListener('transitionend', function () {
                bsAlert.dispose();
                if (alert.parentNode) {
                    alert.parentNode.removeChild(alert);
                }
            });
        }, 5000); // 5 Sekunden
    });
});