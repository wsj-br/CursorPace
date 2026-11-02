# CursorPace

**In anderen Sprachen lesen:**

<small id="lang-list">[English](../README.md) · [Português (Brasil)](./README.pt-BR.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Français](./README.fr.md) · [हिन्दी](./README.hi.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [简体中文](./README.zh-Hans.md) · [繁體中文](./README.zh-Hant.md)</small>

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="400">
</p>

Desktop-App für Windows, Linux und macOS, die das Cursor-Modellkontingent über einen Abrechnungszyklus hinweg verfolgt. Melden Sie sich mit Ihrem Cursor Konto an, um die Nutzung automatisch abzurufen. Das Diagramm zeigt zwei unabhängige Prozentsätze: **Cursor Modelle** und **Andere Modelle**.

Die gestrichelte Linie für die **Erwartete Nutzung** ist ein geradliniger Verlauf von 0 % zu Beginn des Zyklus bis 100 % bei der nächsten Verlängerung. Cursor und Andere Modelle folgen Ihren Stichproben. Eine separate Schätzung prognostiziert den täglichen Verbrauch und das Aufbrauchen. [QUICKSTART.md](QUICKSTART.de.md) behandelt das Diagramm, den Infobereich und die Einstellungen.

Die Anmeldung verwendet eine eingebettete native WebView (WebView2 unter Windows, WKWebView unter macOS, WebKitGTK unter Linux) und Ihre Cursor-Dashboard-Sitzung. Es gibt keine offizielle API für persönliche Pläne und keinen Team-API-Schlüssel.

## Anforderungen

- Windows 10 oder 11 x64, Linux x64 oder ARM64, oder macOS (Intel oder Apple Silicon)
- Für eine Windows-Version: Die App-Nutzlast ist eigenständig. **Anmelden** benötigt außerdem die [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (bereits auf den meisten Windows 11 PCs vorhanden; der Installer bietet die Download-Seite an, falls sie fehlt)
- Linux: WebKitGTK 4.1 (`libwebkit2gtk-4.1-0`; WPE kann je nach Avalonia WebView-Build funktionieren). GNOME-Infobereichsymbole benötigen möglicherweise die AppIndicator-Erweiterung. Die App verwendet gebündelte Inter-Schriftarten und ignoriert System-`.woff`-Schriftarten, sodass eine WOFF-Standardschriftart den Start nicht blockieren oder zum Absturz bringen kann.
- macOS: WKWebView (integriert). Releases sind nicht signiert; Gatekeeper erfordert möglicherweise **Trotzdem öffnen** in den **Systemeinstellungen → Datenschutz & Sicherheit**
- Zum Kompilieren aus dem Quellcode: [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Installieren

1. Laden Sie den Build für Ihre Plattform von der Releases-Seite dieses Repositorys herunter:
   - **Windows**: `CursorPace-*-win-x64-setup.exe`
   - **Linux**: `CursorPace-*-linux-x64.AppImage` oder `*-linux-x64.deb` (x86_64), oder die passenden `linux-arm64`-Dateien (ARM64)
   - **macOS**: `CursorPace-*-osx-arm64.dmg` (Apple Silicon) oder `*-osx-x64.dmg` (Intel)
2. **Windows**: Führen Sie das Installationsprogramm aus. Wenn SmartScreen warnt, dass die App nicht signiert ist, wählen Sie **Weitere Informationen** und dann **Trotzdem ausführen**. Wenn die WebView2 Runtime fehlt, öffnen Sie die vom Installationsprogramm angebotene Download-Seite.
3. **Linux**: Machen Sie das AppImage ausführbar (`chmod +x`) und führen Sie es dann aus. Der erste Start kann einen Moment dauern, während das Bundle extrahiert wird. Unter Debian oder Ubuntu installiert `sudo apt install ./CursorPace-*.deb` das Paket und zieht GTK/WebKit-Abhängigkeiten.
4. **macOS**: Öffnen Sie das DMG, ziehen Sie `CursorPace.app` in den Ordner „Programme“ und öffnen Sie es dann. Wenn Gatekeeper die unsignierte App blockiert, klicken Sie einmal mit der rechten Maustaste auf **Öffnen** und wählen Sie dann **Trotzdem öffnen** in den **Systemeinstellungen → Datenschutz & Sicherheit**.
5. Melden Sie sich bei Cursor an. Wenn **Im Infobereich starten** aktiviert ist (Standard), öffnen Sie das Fenster zuerst über das Infobereichsymbol oder starten Sie es mit `--show`.

Siehe [QUICKSTART.md](QUICKSTART.de.md) für die Ersteinrichtung, die Anmeldung beim Cursor Konto, das Nutzungsdiagramm, das Verhalten des Infobereichs und die Fehlerbehebung.

## Funktionen

- Bei Cursor anmelden und die Nutzung bei Bedarf oder automatisch alle 1, 2, 4, 6 oder 12 Stunden aktualisieren
- Zyklusbeginn und nächste Verlängerung stammen von Cursor. Vorherige und Nächste öffnen früher gespeicherte Zyklen
- Übersichts-Karten für die Zyklusdaten, die Prognose jedes Modells und den neuesten Wert im Vergleich zum erwarteten Tempo
- Diagrammbereiche **1T**, **2T**, **7T**, **1W**, **2W** und **1M**, mit Drag-to-Zoom und einer Zeigeranzeige
- Schließt in den Infobereich. Optionaler Start beim Anmelden. Ein zweiter Start öffnet das bereits laufende Fenster
- Helles, dunkles oder System-Design und eine Sprachauswahl, die aus dem in der Anwendung eingebetteten `locales/ui-languages.json` entnommen wird. Englisch ist die einzige Wahl, wenn dieses Manifest nicht gelesen werden kann. Das Fenster merkt sich seine Größe, Position und seinen maximierten Zustand
- Nutzung als CSV exportieren und Einstellungen und Beispiele sichern oder wiederherstellen
- **Optional.** [CursorPace-Sync-Server](https://github.com/wsj-br/CursorPace-SyncServer) hält CursorPace-Installationen in einem lokalen Netzwerk synchron. Er teilt Nutzungsbeispiele und Abrechnungszyklusgrenzen, sodass ein Mac, ein Windows-PC und eine Linux-Maschine in derselben Entwicklungsumgebung dasselbe Diagramm anzeigen. Installieren und führen Sie den Server von diesem Repository aus.

## Aus dem Quellcode erstellen

Unter Windows (PowerShell) oder Linux/macOS (bash):

```bash
dotnet restore
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

Oder verwenden Sie die Maintainer-Skripte: `.\scripts\dev.ps1` / `./scripts/dev.sh`. Diese Skripte zeigen das Fenster standardmäßig an; verwenden Sie `.\scripts\dev.ps1 -NoShow` oder `./scripts/dev.sh --no-show`, um die normale Start-Sichtbarkeit der App zu verwenden.

Eigenständige Veröffentlichung (`PublishSingleFile=false` beibehalten):

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

Die Linux-Anmeldung erfordert die Installation von WebKitGTK 4.1 auf der Maschine, die die veröffentlichte Binärdatei ausführt. Eine Hintergrundaktualisierung der Nutzung unter Linux verwendet einen 1x1 transparenten WebView-Host, damit das Anmeldefenster nicht aufblinkt; macOS und Windows halten diesen Host in Anmeldegröße außerhalb des Bildschirms.

Das Projekt enthält einen kleinen Kompatibilitäts-Build von Avalonia.Controls.WebView 12.1.0 unter `vendor/` für die ABI-Korrektheit von macOS `CGRect`/`CGSize`. Es ist keine separate macOS WebView-Laufzeitumgebung erforderlich.

Release-Paketierung:

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

Nur veröffentlichen (Paketierung überspringen): fügen Sie `--skip-installer` / `-SkipInstaller` hinzu.

Der vollständige Workflow für Mitwirkende befindet sich in [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md).

## Dokumentation

| Dokument | Zielgruppe |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.de.md) | Installation, Anmeldung, tägliche Nutzung, Fehlerbehebung |
| [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) | Erstellen, Testen, Ausführen, Paketieren, Mitwirken |
| [dev/CHANGELOG.md](../dev/CHANGELOG.md) | Für Benutzer sichtbare Änderungen |

## Haftungsausschluss

[Cursor](https://cursor.com/home) ist ein Produkt von Anysphere, Inc. CursorPace ist ein unabhängiges Projekt und ist weder mit Cursor oder Anysphere verbunden noch wird es von diesen unterstützt.

## Lizenz

Copyright 2026 Waldemar Scudeller Jr.

Diese Software ist unter der MIT-Lizenz lizenziert. Details finden Sie in der Datei [LICENSE](../LICENSE).

Übersetzungen dieser Seite werden automatisch erstellt. Bevorzugen Sie diese englische Seite, wenn eine Übersetzung nicht mit der App übereinstimmt.
