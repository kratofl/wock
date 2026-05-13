Software-Plan: Moderne Arbeitszeiterfassung für Kundenprojekte
Ziel

Entwickle eine Software zur Erfassung, Verwaltung, Prüfung und Abrechnung von Arbeitszeiten für Kundenprojekte. Die Software soll für interne Mitarbeitende, Projektleiter/Admins und optional auch für Kunden nutzbar sein.

Die Anwendung soll ermöglichen:

Arbeitszeiten kunden- und projektbezogen zu erfassen
Zeiten als abrechenbar oder nicht abrechenbar zu markieren
Projekte, Kunden, Mitarbeitende und Aufgaben zu verwalten
Zeiten zu prüfen und freizugeben
Kundenreports und abrechnungsfähige Übersichten zu erzeugen
später Rechnungen oder Exportdaten für Buchhaltungssysteme bereitzustellen
1. Nutzerrollen
Mitarbeiter

Mitarbeiter können:

eigene Arbeitszeiten erfassen
Timer starten und stoppen
manuelle Zeiteinträge erstellen
Kunde, Projekt und Aufgabe auswählen
Tätigkeit beschreiben
Zeiten als Entwurf speichern
Zeiten zur Freigabe einreichen
eigene Zeithistorie einsehen
Projektleiter / Manager

Projektleiter können:

Zeiten ihrer Projekte prüfen
Zeiten freigeben oder ablehnen
Kommentare bei Korrekturen hinterlassen
Projektbudgets überwachen
Reports für Kunden und interne Auswertung erstellen
Projektaufgaben verwalten
Admin

Admins können:

Kunden verwalten
Projekte verwalten
Mitarbeitende verwalten
Stundensätze pflegen
Rollen und Rechte vergeben
Abrechnungsmodelle konfigurieren
Exporte und Rechnungsdaten erzeugen
Kunde optional

Kunden können optional:

freigegebene Zeiten einsehen
Monatsreports öffnen
Zeiten nach Projekt oder Zeitraum filtern
PDF-Reports herunterladen
optional Zeiten bestätigen
2. Hauptmodule
Modul 1: Dashboard

Das Dashboard soll je nach Rolle unterschiedliche Informationen anzeigen.

Mitarbeiter-Dashboard

Anzeigen:

Heute erfasste Stunden
Diese Woche erfasste Stunden
Offene Entwürfe
Eingereichte Zeiten
Abgelehnte Zeiten
Schnellzugriff auf Timer
Manager-Dashboard

Anzeigen:

Offene Freigaben
Stunden je Kunde
Stunden je Projekt
Abrechenbare Stunden im aktuellen Monat
Nicht abrechenbare Stunden
Budgetverbrauch je Projekt
Admin-Dashboard

Anzeigen:

Aktive Kunden
Aktive Projekte
Offene Abrechnungen
Stunden nach Status
Monatsumsatz auf Basis freigegebener abrechenbarer Zeiten
3. Zeiterfassung
Funktionen

Die Software soll zwei Arten der Zeiterfassung unterstützen:

Timer-Erfassung
Mitarbeiter startet einen Timer
Auswahl von Kunde, Projekt und Aufgabe
Beschreibung der Tätigkeit
Timer kann pausiert, gestoppt und gespeichert werden
Nach dem Stoppen wird ein Zeiteintrag erzeugt
Manuelle Erfassung

Mitarbeiter kann einen Zeiteintrag manuell erstellen mit:

Datum
Startzeit
Endzeit
Dauer
Kunde
Projekt
Aufgabe
Tätigkeit/Kategorie
Beschreibung
Abrechenbar ja/nein
Status
Pflichtfelder

Für jeden Zeiteintrag sollen mindestens diese Felder Pflicht sein:

Datum
Mitarbeiter
Kunde
Projekt
Dauer oder Start-/Endzeit
Tätigkeit/Kategorie
Beschreibung
Abrechenbar ja/nein
4. Datenstruktur
Kunden

Ein Kunde hat folgende Felder:

ID
Name
Ansprechpartner
E-Mail
Telefonnummer
Rechnungsadresse
Standard-Stundensatz
Status: aktiv / inaktiv
Erstellt am
Aktualisiert am
Projekte

Ein Projekt hat folgende Felder:

ID
Kunden-ID
Projektname
Beschreibung
Startdatum
Enddatum
Budget in Stunden
Budget in Euro
Abrechnungsmodell
Standard-Stundensatz
Status: geplant / aktiv / pausiert / abgeschlossen / archiviert
Erstellt am
Aktualisiert am
Aufgaben

Eine Aufgabe hat folgende Felder:

ID
Projekt-ID
Titel
Beschreibung
Kategorie
Verantwortlicher Mitarbeiter
Status: offen / in Arbeit / erledigt / archiviert
Erstellt am
Aktualisiert am
Zeiteinträge

Ein Zeiteintrag hat folgende Felder:

ID
Mitarbeiter-ID
Kunden-ID
Projekt-ID
Aufgaben-ID optional
Datum
Startzeit
Endzeit
Dauer in Minuten
Kategorie
Beschreibung
Abrechenbar: ja / nein
Abrechnungskategorie
Stundensatz
Betrag berechnet aus Dauer × Stundensatz
Status
Freigegeben von
Freigegeben am
Ablehnungsgrund optional
Rechnungs-ID optional
Erstellt am
Aktualisiert am
Mitarbeiter

Ein Mitarbeiter hat folgende Felder:

ID
Name
E-Mail
Rolle
Interner Kostensatz optional
Standard-Stundensatz optional
Status: aktiv / inaktiv
Erstellt am
Aktualisiert am
Rechnungen / Abrechnungsperioden

Eine Rechnung oder Abrechnungsperiode hat folgende Felder:

ID
Kunde-ID
Zeitraum von
Zeitraum bis
Summe Stunden
Summe abrechenbare Stunden
Betrag netto
Status: Entwurf / erstellt / exportiert / bezahlt
Zugeordnete Zeiteinträge
Erstellt am
Aktualisiert am
5. Status-Workflow für Zeiteinträge

Jeder Zeiteintrag soll einen Status haben.

Statuswerte
Entwurf
Eingereicht
In Prüfung
Freigegeben
Abgelehnt
Abgerechnet
Archiviert
Workflow
Mitarbeiter erstellt Zeiteintrag als Entwurf.
Mitarbeiter reicht Zeiteintrag ein.
Projektleiter prüft Zeiteintrag.
Projektleiter gibt den Eintrag frei oder lehnt ihn ab.
Freigegebene und abrechenbare Zeiten können in eine Abrechnung übernommen werden.
Nach der Abrechnung erhält der Eintrag den Status „Abgerechnet“.
6. Abrechnungsmodelle

Die Software soll verschiedene Abrechnungsmodelle unterstützen.

Stundenbasis

Berechnung:

Dauer × Stundensatz = Betrag

Retainer / Monatskontingent
Kunde hat monatliches Stundenkontingent
Zeiten werden gegen Kontingent gebucht
Überschreitungen werden sichtbar gemacht
Optional: Mehrstunden separat abrechnen
Festpreis
Zeiten werden intern getrackt
Zeiten werden nicht automatisch dem Kunden berechnet
Projektfortschritt und Rentabilität können intern ausgewertet werden
Nicht abrechenbar

Beispiele:

interne Meetings
Kulanz
Akquise
Nacharbeit
Support ohne Berechnung
Kulanz
Zeit wird sichtbar erfasst
Zeit erscheint optional im Kundenreport
Betrag wird nicht berechnet
7. Tätigkeitskategorien

Die Software soll feste Kategorien unterstützen, damit Auswertungen möglich sind.

Standard-Kategorien:

Beratung
Entwicklung
Design
Projektmanagement
Support
Testing
Dokumentation
Administration
Vertrieb
Meeting
Sonstiges

Admins sollen Kategorien später bearbeiten oder ergänzen können.

8. Kundenansicht

Die Kundenansicht darf nur freigegebene, kundenrelevante Daten anzeigen.

Sichtbare Felder für Kunden
Datum
Projekt
Tätigkeit
Beschreibung
Stunden
Abrechnungsstatus optional
Nicht sichtbare Felder für Kunden
interne Kostensätze
interne Kommentare
Margen
Ablehnungsgründe
Mitarbeiterkosten
interne Projektbewertungen
Funktionen für Kunden
Zeitraum filtern
Projekt filtern
Monatsreport ansehen
PDF herunterladen
optional Zeiten bestätigen
9. Reports und Auswertungen

Die Software soll folgende Reports bieten:

Interne Reports
Stunden je Mitarbeiter
Stunden je Kunde
Stunden je Projekt
abrechenbare vs. nicht abrechenbare Stunden
Umsatz nach Kunde
Budgetverbrauch je Projekt
Rentabilität je Projekt
offene Freigaben
abgelehnte Einträge
Kundenreports
Zeitraum
Kunde
Projekt
Tätigkeiten
Beschreibung
Stunden
Summe Stunden
Summe Betrag, falls abrechenbar
Exportformate

Die Software soll Exporte unterstützen als:

PDF
CSV
Excel
optional DATEV-/Buchhaltungs-Export später
10. Wichtige UI-Seiten
Login
E-Mail
Passwort
Passwort vergessen
optional 2FA später
Dashboard
rollenbasierte Übersicht
Schnellzugriffe
aktuelle Kennzahlen
Zeiterfassung
Timer
manuelle Eingabe
Liste eigener Einträge
Filter nach Datum, Kunde, Projekt und Status
Kundenverwaltung
Kundenliste
Kunde erstellen
Kunde bearbeiten
Kundendetails
zugehörige Projekte
zugehörige Reports
Projektverwaltung
Projektliste
Projekt erstellen
Projekt bearbeiten
Budgetübersicht
Projektzeiten
Projektaufgaben
Aufgabenverwaltung
Aufgabenliste je Projekt
Aufgabe erstellen
Aufgabe bearbeiten
Status ändern
Freigabe-Center
Liste eingereichter Zeiten
Bulk-Freigabe
Ablehnung mit Kommentar
Filter nach Mitarbeiter, Kunde, Projekt und Zeitraum
Abrechnung
Zeitraum auswählen
Kunde auswählen
freigegebene abrechenbare Zeiten laden
Abrechnungsentwurf erstellen
PDF/CSV/Excel exportieren
Zeiten als abgerechnet markieren
Einstellungen
Rollen und Rechte
Stundensätze
Kategorien
Abrechnungsmodelle
Kundenportal aktivieren/deaktivieren
11. Rechte- und Zugriffssystem
Grundregel

Jeder Nutzer darf nur Daten sehen, die für seine Rolle relevant sind.

Berechtigungen

Mitarbeiter:

eigene Zeiten sehen und bearbeiten
keine fremden Zeiten sehen
keine Abrechnung sehen

Projektleiter:

Zeiten seiner Projekte sehen
Zeiten freigeben oder ablehnen
Projektreports sehen

Admin:

alle Daten sehen und bearbeiten
Nutzer verwalten
Abrechnung erstellen
Systemeinstellungen bearbeiten

Kunde:

nur freigegebene Zeiten eigener Projekte sehen
keine internen Informationen sehen
12. MVP-Version

Die erste Version der Software soll bewusst schlank sein.

MVP-Funktionen
Login
Rollen: Mitarbeiter, Admin
Kunden anlegen
Projekte anlegen
Zeiten manuell erfassen
Timer-Funktion
Abrechenbar ja/nein
Status: Entwurf, Eingereicht, Freigegeben, Abgelehnt
Freigabe durch Admin
einfache Reports nach Kunde, Projekt und Zeitraum
CSV-Export
PDF-Report für Kunden
Noch nicht im MVP
Kundenportal
automatische Rechnungserstellung
Buchhaltungsintegration
Retainer-Logik
interne Kostensätze
komplexe Rollenrechte
mobile App
API-Integrationen
13. Spätere Erweiterungen

Nach dem MVP können folgende Funktionen ergänzt werden:

Kundenportal
Rechnungsmodul
DATEV-Export
Lexoffice-/SevDesk-/FastBill-Integration
Kalenderintegration
Slack-/Teams-Erinnerungen
automatische Pausenerkennung
Arbeitszeitgesetz-Checks
mobile App
KI-Vorschläge für Tätigkeitsbeschreibungen
Budgetwarnungen
automatische Monatsreports
Genehmigung durch Kunden
Projektprofitabilität mit internen Kostensätzen
14. Technische Empfehlung
Frontend

Empfohlen:

React oder Next.js
modernes Dashboard-UI
responsive Design
Tabellen mit Filter- und Sortierfunktion
Rollenabhängige Navigation
Backend

Empfohlen:

Node.js mit NestJS oder Express
alternativ Laravel oder Django
REST API oder GraphQL
Authentifizierung mit JWT oder Session-System
Datenbank

Empfohlen:

PostgreSQL
Wichtige Tabellen
users
customers
projects
tasks
time_entries
invoices
roles
permissions
activity_categories
audit_logs
Sicherheit
Passwort-Hashing
rollenbasierte Zugriffe
Audit-Log für Änderungen
sichere Kundenansicht
DSGVO-konforme Datenspeicherung
Export- und Löschfunktionen für personenbezogene Daten
15. Erwartetes Ergebnis

Erstelle eine webbasierte Software zur modernen Arbeitszeiterfassung für Kundenprojekte. Die Software soll übersichtlich, schnell und skalierbar sein. Der Fokus der ersten Version liegt auf sauberer Zeiterfassung, Kunden- und Projektzuordnung, Freigabeprozess und einfachen Reports für Abrechnung und Kundenübersicht.

Die Software soll später erweiterbar sein für Kundenportal, Rechnungen, Buchhaltungsintegrationen und detaillierte Projektprofitabilität.
