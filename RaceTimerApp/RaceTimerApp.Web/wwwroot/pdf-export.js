// PDF Export Funktionen
window.exportTableToPdf = function(tableElementId, fileName) {
    // Lade html2pdf Library
    const script = document.createElement('script');
    script.src = 'https://cdnjs.cloudflare.com/ajax/libs/html2pdf.js/0.10.1/html2pdf.bundle.min.js';

    script.onload = function() {
        const element = document.getElementById(tableElementId);
        if (!element) {
            console.error('Element mit ID ' + tableElementId + ' nicht gefunden');
            return;
        }

        // Clone des Elements erstellen für PDF (ohne störende Styles)
        const clonedElement = element.cloneNode(true);

        // PDF Optionen
        const options = {
            margin: [10, 10, 10, 10], // Ränder in mm
            filename: fileName || 'export.pdf',
            image: { type: 'jpeg', quality: 0.98 },
            html2canvas: { scale: 2, useCORS: true },
            jsPDF: { 
                orientation: 'landscape',
                unit: 'mm',
                format: 'a4'
            },
            pagebreak: { mode: 'avoid-all', before: '.no-break' }
        };

        // PDF generieren und herunterladen
        html2pdf().set(options).from(clonedElement).save();
    };

    script.onerror = function() {
        console.error('Fehler beim Laden der html2pdf Library');
        alert('PDF Export fehlgeschlagen. Bitte versuchen Sie es später erneut.');
    };

    document.head.appendChild(script);
};

// Alternativ: CSV Export
window.exportTableToCsv = function(tableElementId, fileName) {
    const element = document.getElementById(tableElementId);
    if (!element) {
        console.error('Element mit ID ' + tableElementId + ' nicht gefunden');
        return;
    }

    // Tabelle zu CSV konvertieren
    const rows = element.querySelectorAll('table tr');
    let csv = [];

    rows.forEach(row => {
        const cols = row.querySelectorAll('td, th');
        const csvRow = [];
        cols.forEach(col => {
            // Text extrahieren und Kommas escapen
            let text = col.innerText.replace(/"/g, '""').trim();
            csvRow.push('"' + text + '"');
        });
        csv.push(csvRow.join(','));
    });

    // CSV als Datei herunterladen
    const csvString = csv.join('\n');
    const blob = new Blob([csvString], { type: 'text/csv;charset=utf-8;' });
    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);

    link.setAttribute('href', url);
    link.setAttribute('download', fileName || 'export.csv');
    link.style.visibility = 'hidden';

    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// XLSX Export (optional, benötigt zusätzliche Library)
window.exportTableToXlsx = function(tableElementId, fileName) {
    // Diese Funktion benötigt: https://cdnjs.cloudflare.com/ajax/libs/xlsx/0.18.5/xlsx.min.js
    const script = document.createElement('script');
    script.src = 'https://cdnjs.cloudflare.com/ajax/libs/xlsx/0.18.5/xlsx.min.js';

    script.onload = function() {
        const element = document.getElementById(tableElementId);
        if (!element) {
            console.error('Element mit ID ' + tableElementId + ' nicht gefunden');
            return;
        }

        // Tabelle als Arbeitsmappe erstellen
        const table = element.querySelector('table');
        const workbook = XLSX.utils.table_to_book(table);

        // Arbeitsmappe speichern
        XLSX.writeFile(workbook, fileName || 'export.xlsx');
    };

    script.onerror = function() {
        console.error('Fehler beim Laden der xlsx Library');
        alert('XLSX Export fehlgeschlagen. Bitte versuchen Sie es später erneut.');
    };

    document.head.appendChild(script);
};
