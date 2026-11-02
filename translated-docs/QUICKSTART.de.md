# Schnellstart

**In anderen Sprachen lesen:**

<small id="lang-list">[English](../QUICKSTART.md) · [Português (Brasil)](./QUICKSTART.pt-BR.md) · [Deutsch](./QUICKSTART.de.md) · [Español](./QUICKSTART.es.md) · [Français](./QUICKSTART.fr.md) · [हिन्दी](./QUICKSTART.hi.md) · [日本語](./QUICKSTART.ja.md) · [한국어](./QUICKSTART.ko.md) · [简体中文](./QUICKSTART.zh-Hans.md) · [繁體中文](./QUICKSTART.zh-Hant.md)</small>

Endbenutzerhandbuch für CursorPace. Informationen zum Erstellen aus dem Quellcode finden Sie unter [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md).

## Installieren

Laden Sie einen Build von der Releases-Seite dieses Repositorys herunter.

**Windows.** Führen Sie `CursorPace-*-win-x64-setup.exe` aus. Wenn SmartScreen warnt, dass die App nicht signiert ist, wählen Sie **Weitere Informationen** und dann **Trotzdem ausführen**. Für die **Anmeldung** ist die [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) erforderlich. Der Installer bietet die Download-Seite an, wenn diese fehlt.

**Linux.** Für die meisten Distributionen `chmod +x CursorPace-*-linux-x64.AppImage` (oder die Datei `linux-arm64`) und führen Sie sie aus. Unter Debian oder Ubuntu zieht `sudo apt install ./CursorPace-*-linux-x64.deb` (oder `linux-arm64`) GTK und WebKit an. Die Google-Anmeldung kann in WebKit blockiert sein; verwenden Sie GitHub, E-Mail oder melden Sie sich unter Windows an.

**macOS.** Öffnen Sie `CursorPace-*-osx-arm64.dmg` (Apple Silicon) oder `*-osx-x64.dmg` (Intel) und ziehen Sie `CursorPace.app` in den Ordner „Programme“. Wenn Gatekeeper die unsignierte App blockiert, klicken Sie einmal mit der rechten Maustaste auf **Öffnen** und wählen Sie dann in den **Systemeinstellungen → Datenschutz & Sicherheit** die Option **Trotzdem öffnen**.

## Erster Start

**Im Infobereich starten** ist standardmäßig aktiviert, sodass beim ersten Start möglicherweise nur das Taskleistensymbol angezeigt wird. Öffnen Sie das Fenster über dieses Symbol oder starten Sie es mit `--show`.

1. Wählen Sie **Anmelden**.
2. Schließen Sie die Google-, GitHub- oder Zwei-Faktor-Anmeldung im eingebetteten Fenster ab. Es schließt sich, wenn Cursor die Sitzung akzeptiert. Wenn Sie Ihr Konto bereits sehen, wählen Sie **Weiter**.
3. Der Abrechnungszyklus und die Nutzung werden im Diagramm angezeigt.

Sie können sich später über **Einstellungen** → **Cursor Konto** anmelden. Nach einer erfolgreichen Aktualisierung lautet diese Überschrift **Cursor Konto (verbunden)**. **Aktualisieren** in der Titelleiste oder **Jetzt aktualisieren** in den Einstellungen ruft die Daten sofort ab.

**Abmelden** löscht die Cursor-Sitzung. Unter Windows und macOS kann ein gespeicherter Google- oder GitHub-Login bestehen bleiben, sodass die nächste Anmeldung das Passwort überspringen kann. Unter Linux löscht **Abmelden** das gesamte Browserprofil. Nutzungsbeispiele bleiben auf der Festplatte.

Wenn **Nutzung automatisch aktualisieren** aktiviert ist, erfolgen Aktualisierungen zu einer 1-, 2-, 4-, 6- oder 12-Stunden-Marke ab Mitternacht Ortszeit. Beim Start wird eine Aktualisierung nur ausgeführt, wenn der letzte Erfolg mindestens 20 Minuten alt ist und ein geplanter Slot verpasst wurde oder das volle Intervall bereits abgelaufen ist.

## Hauptfenster

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="600">
</p>

Drei Karten befinden sich über dem Diagramm:

- **Zyklusbeginn** und **Nächste Verlängerung**. Im aktuellen Zyklus wird die verbleibende Zeit neben dem Verlängerungsdatum angezeigt.
- Eine Prognose für jedes Modell. Innerhalb des Zyklus ist dies die Laufzeit. Oberhalb der gestrichelten erwarteten Linie ist es der Zeitpunkt, zu dem diese Linie den Messwert erreicht. Auf oder unter der Linie ist es der prognostizierte Prozentsatz bei der Verlängerung. Im aktuellen Zyklus enthält ein Datum die daneben liegende Wartezeit.
- **Letzte Messung**: die neueste Stichprobe in diesem Zyklus, mit Cursor %, Andere %, Erwartete Nutzung % und wie weit jede Serie von diesem Tempo entfernt ist.

„Zurück“ und „Weiter“ in der Monatsüberschrift bewegen sich durch gespeicherte Zyklen. Unter dieser Überschrift ist `Cursor dd-MMM HH:mm` die letzte Aktualisierung. Ein roter Punkt bedeutet, dass das Konto abgemeldet ist oder die Aktualisierung fehlgeschlagen ist. Klicken Sie darauf, um **Cursor Konto** zu öffnen. Wenn ein Sync-Server konfiguriert ist, werden dort auch **Sync-Server** und die letzte Erfolgszeit angezeigt. Ein roter Punkt bedeutet, dass der letzte Versuch fehlgeschlagen ist. Klicken Sie darauf, um **Sync-Server** zu öffnen.

| Bereich | Was es zeigt |
| --- | --- |
| **1T** / **2D** | Die letzten 24 oder 48 Stunden |
| **7D** / **1W** | Die letzten sieben Tage |
| **2W** | Die letzten 14 Tage |
| **1M** | Der gesamte angezeigte Zyklus, eine Stichprobe pro lokalem Tag |

Bei einem aktuellen Zyklus endet ein kurzer Bereich zur aktuellen Uhrzeit. Bei einem früheren Zyklus endet er mit der Verlängerung dieses Zyklus. Der Start bleibt beim Zyklusbeginn, wenn das Fenster andernfalls früher beginnen würde.

Ziehen Sie über das Diagramm, um zu zoomen. Rechtsklick kehrt zu **1M** zurück. Die Auswahl einer Bereichsschaltfläche, einschließlich der bereits ausgewählten, beendet den Zoom und zeigt diesen Bereich an. **Jede Stichprobe bis zu** (**2D**, **4D**, **7D** oder **14D**, Standard **14D**) legt fest, wie weit das Diagramm jede Stichprobe darstellt und die lokale Zeit beschriftet. Längere Bereiche behalten die letzte Stichprobe jedes Tages und beschriften den Tag des Monats. Die vertikale Achse passt die Werte auf dem Bildschirm an, bis zu 120 %.

Bewegen Sie den Zeiger, um Cursor, Andere Modelle und Erwartete Nutzung zu diesem Zeitpunkt abzulesen. Nach der letzten Stichprobe zeigen Cursor und Andere die Schätzung an, gekennzeichnet `estimated`. Während eines Zooms zeigt die Anzeige auch jeden Wert am Zoomstart und die Änderung von dort an.

| Steuerung | Aktion |
| --- | --- |
| **Aktualisieren** | Cursor-Nutzung jetzt abrufen. Auf der Einstellungsseite ausgeblendet |
| **Einstellungen** | Einstellungen in diesem Fenster öffnen |
| **Zurück** | Zum Diagramm zurückkehren |
| **Minimieren** / **Maximieren** | Standard-Fenstersteuerungen. Die App bleibt im Infobereich |
| Schließen | Fenster ausblenden. Die App bleibt im Infobereich |

Das Fenster merkt sich seine Größe, Position und den maximierten Zustand. Informationsbeschriftungen können ausgewählt und kopiert werden.

## Erwartet und geschätzt

- **Erwartete Nutzung** im Diagramm ist die gestrichelte Linie von 0 % beim Zyklusbeginn bis 100 % bei der nächsten Verlängerung. Beim Überfahren mit der Maus wird diese Linie verwendet.
- **Cursor** und **Andere Modelle** sind die gemessenen Stichproben.
- **Erwartet** in einer Zyklus-CSV folgt jeder Stichprobe und passt dann das verbleibende Kontingent auf 100 % bei der Verlängerung an.
- **Geschätzt** ist die rund-gepunktete Projektion von der letzten Stichprobe zur Verlängerung. Nach der letzten Stichprobe wird beim Überfahren mit der Maus diese Linie verwendet.

Cursor Modelle und Andere Modelle bleiben unabhängig.

## Einstellungen

Die Einstellungen ersetzen das Diagramm. **Zurück** oder die Überschrift **Einstellungen** kehrt dorthin zurück. Die zuletzt geöffnete Registerkarte wird beim nächsten Mal, auch nach einem Neustart, wieder geöffnet.

**Sync-Server** ist optional. [CursorPace-Sync-Server](https://github.com/wsj-br/CursorPace-SyncServer) teilt Nutzungsstichproben und Abrechnungszyklusgrenzen über Maschinen in einem lokalen Netzwerk, sodass ein Mac, ein Windows-PC und eine Linux-Maschine in derselben Entwicklungsumgebung dasselbe Diagramm anzeigen. Installieren und führen Sie den Server von diesem Repository aus. Design, Sprache, Fensterplatzierung und Startoptionen bleiben auf jedem Computer.

| Registerkarte | Was Sie einstellen können |
| --- | --- |
| Start & Anzeige | **Beim Anmelden starten**, **Im Infobereich starten**, Design (System, Hell oder Dunkel), **Sprache** (die Gebietsschemas in `locales/ui-languages.json`, oder Englisch, wenn diese Datei fehlt), und **Jede Stichprobe bis zu** |
| Cursor Konto | **Anmelden** (ausgeblendet, während verbunden), **Jetzt aktualisieren**, **Abmelden**, und automatische Aktualisierung alle 1, 2, 4, 6 oder 12 Stunden |
| Sync-Server | **Mit Server synchronisieren**, Server-URL, API-Token, Computername (standardmäßig der Hostname) und **Jetzt erneut synchronisieren**. Der Link öffnet das Sync-Server-Repository |
| Export & Sicherung | **Zyklus-CSV exportieren** für den angezeigten Zyklus, **Nutzung exportieren** für jede gespeicherte Stichprobe, während Sie angemeldet sind, und **Sicherung** / **Wiederherstellen** von Einstellungen und Stichproben als eine Zip-Datei. **Ordner öffnen** öffnet das Ziel |
| Über | Version, UTC-Erstellungsdatum und -Uhrzeit, Urheberrecht, MIT-Lizenz und das GitHub-Repository |

Sicherung und Wiederherstellen belassen die Cursor-Anmeldung auf diesem Computer.

## Infobereich

Klicken Sie auf das Symbol oder wählen Sie **Öffnen**, um das Fenster anzuzeigen. **Beenden** beendet die Anwendung. Der Tooltip zeigt den heutigen Prozentsatz und, sobald genügend Daten vorhanden sind, den prognostizierten Prozentsatz bei der Verlängerung.

Unter macOS öffnet ein Klick das Infobereich-Menü, und das Dock-Symbol ist ausgeblendet, während das Fenster ausgeblendet oder minimiert ist. Zeigen Sie das Fenster aus dem Infobereich an, mit `--show`, oder indem Sie die App erneut starten.

Wenn das Symbol fehlt, öffnen Sie den Benachrichtigungsüberlauf. Unter GNOME ist möglicherweise die AppIndicator-Erweiterung erforderlich.

Ein zweiter Start zeigt das bereits laufende Fenster an. Ein zweiter `--background`-Start lässt es ausgeblendet. `--show` erzwingt das Öffnen des Fensters. **Beim Anmelden starten** folgt **Im Infobereich starten**.

## Daten

```text
Windows: %LocalAppData%\CursorPace\
Linux:   ~/.local/share/CursorPace/
macOS:   ~/Library/Application Support/CursorPace/
```

`settings.json` enthält Einstellungen, Fensterplatzierung und Zyklusgrenzen. `usage-samples.json` enthält die Stichproben. Die Cursor-Anmeldung befindet sich in `WebView2` unter Windows, `WebView` unter Linux und macOS oder `WebView-AppImage` beim Ausführen eines Linux AppImage. Eine Datei, die nicht geparst werden kann, wird nach `*.corrupt.json` kopiert und durch eine leere ersetzt. Eine gesperrte oder unlesbare Datei bleibt unverändert.

Löschen Sie den Ordner, um neu zu beginnen. Unter Windows wird er durch die Deinstallation der App entfernt. Unter Linux und macOS löschen Sie ihn selbst.

## Verlängerung

Ein Abruf, der einen neuen Zyklusbeginn meldet, archiviert den vorherigen Zyklus und behält dessen Stichproben. Vorheriger und Nächster öffnen diese Zyklen. Die App aktualisiert sich auch, wenn das lokale Datum die gespeicherte Verlängerung erreicht. Das Fenster kann ausgeblendet bleiben. Der Prozess muss für diese Überprüfung und für automatische Updates laufen.

## Deinstallieren

Beenden Sie über das Infobereich-Menü. Unter Windows deinstallieren Sie **CursorPace** aus den **Installierten Apps**. Unter Linux und macOS löschen Sie die App und, für einen sauberen Start, den oben genannten Datenordner.

## Fehlerbehebung

**Die App startet nicht.** Beenden Sie alle `CursorPace`-Prozesse und starten Sie erneut. Unter Windows überprüfen Sie die Ereignisanzeige. Unter Linux versuchen Sie `journalctl --user -xe`. Unter macOS überprüfen Sie Console.app. Wenn Linux bei 100 % CPU-Auslastung liegt oder `crash.log` sagt `Could not create glyphTypeface`, installieren Sie eine neuere Version als 0.3.0. Wenn **Beim Anmelden starten** immer noch auf ein gelöschtes AppImage verweist, führen Sie das neue AppImage einmal aus.

**Die Anmeldung schlägt fehl, oder „Das angegebene Modul konnte nicht gefunden werden“.** Unter Windows installieren Sie die [WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64). Unter Linux installieren Sie `libwebkit2gtk-4.1-0`. Wenn Google den eingebetteten Browser blockiert, verwenden Sie GitHub oder E-Mail. Wählen Sie **Weiter**, wenn Ihr Cursor Konto bereits angezeigt wird.

**Die Nutzung wird nicht aktualisiert.** Bestätigen Sie **Cursor Konto (verbunden)** und automatische Updates, oder wählen Sie **Aktualisieren**. Überprüfen Sie das Systemdatum, die Uhrzeit und die Zeitzone. Nach einer Cursor-Ratenbegrenzung wartet die App auf das nächste Intervall.

**Das Tray- oder Taskleistensymbol fehlt.** Öffnen Sie den Benachrichtigungsüberlauf. Unter Linux starten Sie die App neu, damit sie `~/.local/share/applications/CursorPace.desktop` schreiben kann. Wenn das Taskleistensymbol immer noch ein generisches Zahnrad ist, melden Sie sich einmal ab, oder drücken Sie unter X11 Alt+F2, geben Sie `r` ein und drücken Sie die Eingabetaste.

**Einstellungen werden nicht gespeichert, oder der heutige Prozentsatz sieht falsch aus.** Bestätigen Sie, dass Sie den Datenordner schreiben können. Wenn `settings.corrupt.json` existiert, löschen Sie es und `settings.json`, um zurückzusetzen. Überprüfen Sie dann die Uhrzeit und wählen Sie **Aktualisieren**.

**Beim Anmelden starten funktioniert nicht.** Bestätigen Sie, dass die Einstellung aktiviert ist. Windows speichert `CursorPace` im Run-Schlüssel des aktuellen Benutzers und fügt `--background` hinzu, wenn der Tray-Start aktiviert ist. Eine signierte macOS-App erscheint unter **Beim Anmelden öffnen**. Ein unsignierter Build verwendet `~/Library/LaunchAgents/com.cursorpace.app.plist` und startet die `.app` mit `/usr/bin/open`. Linux verwendet `~/.config/autostart/cursor-pace.desktop`. Ein AppImage-Eintrag muss auf die Datei `.AppImage` verweisen. Deaktivieren und aktivieren Sie die Einstellung, oder starten Sie das neue AppImage einmal, um es neu zu schreiben.

**Zwei Tray-Symbole, oder nach einem Linux-Neustart abgemeldet.** Beenden Sie die App über das Tray und starten Sie sie einmal. Melden Sie sich unter Linux bei einem aktuellen Build erneut an, damit die Sitzung in `cookies.sqlite` gespeichert wird.

Übersetzungen dieser Seite werden automatisch erstellt. Bevorzugen Sie diese englische Seite, wenn eine Übersetzung nicht mit der App übereinstimmt.
