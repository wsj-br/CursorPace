# CursorPace

**Lire dans d'autres langues :**

<small id="lang-list">[English](../README.md) · [Português (Brasil)](./README.pt-BR.md) · [Deutsch](./README.de.md) · [Español](./README.es.md) · [Français](./README.fr.md) · [हिन्दी](./README.hi.md) · [日本語](./README.ja.md) · [한국어](./README.ko.md) · [简体中文](./README.zh-Hans.md) · [繁體中文](./README.zh-Hant.md)</small>

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="400">
</p>

Application de bureau pour Windows, Linux et macOS qui suit le quota de modèles Cursor sur un cycle de facturation. Connectez-vous avec votre compte Cursor pour récupérer automatiquement l'utilisation. Le graphique affiche deux pourcentages indépendants : **Modèles Cursor** et **Autres modèles**.

La ligne pointillée **Utilisation prévue** représente un rythme linéaire de 0 % au début du cycle à 100 % au prochain renouvellement. Cursor et les autres modèles suivent vos échantillons. Une estimation distincte projette la consommation quotidienne et l'épuisement. [QUICKSTART.md](QUICKSTART.fr.md) couvre le graphique, la zone de notification et les paramètres.

La connexion utilise un WebView natif intégré (WebView2 sur Windows, WKWebView sur macOS, WebKitGTK sur Linux) et votre session de tableau de bord Cursor. Il n'y a pas d'API officielle pour les plans personnels ni de clé d'API d'équipe.

## Exigences

- Windows 10 ou 11 x64, Linux x64 ou ARM64, ou macOS (Intel ou Apple Silicon)
- Pour une version Windows : la charge utile de l'application est autonome. La **connexion** nécessite également le [Microsoft Edge WebView2 Runtime](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (déjà présent sur la plupart des PC Windows 11 ; l'installateur propose la page de téléchargement s'il est manquant)
- Linux : WebKitGTK 4.1 (`libwebkit2gtk-4.1-0` ; WPE peut fonctionner selon la version d'Avalonia WebView). Les icônes de la zone de notification GNOME peuvent nécessiter l'extension AppIndicator. L'application utilise les polices Inter intégrées et ignore les polices système `.woff`, de sorte qu'une police WOFF par défaut ne peut pas bloquer ou faire planter le démarrage.
- macOS : WKWebView (intégré). Les versions ne sont pas signées ; Gatekeeper peut nécessiter **Ouvrir quand même** dans **Réglages Système → Confidentialité et sécurité**
- Pour la compilation à partir des sources : [.NET 10 SDK](https://dotnet.microsoft.com/download)

## Installer

1. Téléchargez la version pour votre plateforme depuis la page des versions de ce dépôt :
   - **Windows** : `CursorPace-*-win-x64-setup.exe`
   - **Linux** : `CursorPace-*-linux-x64.AppImage` ou `*-linux-x64.deb` (x86_64), ou les fichiers `linux-arm64` correspondants (ARM64)
   - **macOS** : `CursorPace-*-osx-arm64.dmg` (Apple Silicon) ou `*-osx-x64.dmg` (Intel)
2. **Windows** : exécutez l'installateur. Si SmartScreen avertit que l'application n'est pas signée, choisissez **Plus d'informations**, puis **Exécuter quand même**. Si WebView2 Runtime est manquant, ouvrez la page de téléchargement proposée par l'installateur.
3. **Linux** : rendez l'AppImage exécutable (`chmod +x`), puis exécutez-la. Le premier lancement peut prendre un moment pendant l'extraction du paquet. Sur Debian ou Ubuntu, `sudo apt install ./CursorPace-*.deb` installe le paquet et tire les dépendances GTK/WebKit.
4. **macOS** : ouvrez le DMG, faites glisser `CursorPace.app` vers Applications, puis ouvrez-le. Si Gatekeeper bloque l'application non signée, faites un clic droit sur **Ouvrir** une fois, puis choisissez **Ouvrir quand même** dans **Réglages Système → Confidentialité et sécurité**.
5. Connectez-vous à Cursor. Si **Démarrer dans la zone de notification** est activé (par défaut), ouvrez d'abord la fenêtre depuis l'icône de la zone de notification, ou lancez avec `--show`.

Consultez [QUICKSTART.md](QUICKSTART.fr.md) pour la configuration initiale, la connexion au compte Cursor, le graphique d'utilisation, le comportement de la zone de notification et le dépannage.

## Fonctionnalités

- Se connecter à Cursor et actualiser l'utilisation à la demande, ou automatiquement toutes les 1, 2, 4, 6 ou 12 heures
- Le début du cycle de facturation et le prochain renouvellement proviennent de Cursor. Précédent et Suivant ouvrent les cycles stockés précédemment
- Cartes récapitulatives pour les dates du cycle, la prévision de chaque modèle et la dernière lecture par rapport au rythme prévu
- Plages de graphique **1J**, **2J**, **7J**, **1S**, **2S** et **1M**, avec zoom par glisser-déposer et lecture au pointeur
- Se réduit dans la barre de notification. Lancement facultatif à la connexion. Un second lancement ouvre la fenêtre déjà en cours d'exécution.
- Thème clair, sombre ou système, et un choix de langue tiré du `locales/ui-languages.json` intégré à l'application. L'anglais est le seul choix lorsque ce manifeste ne peut pas être lu. La fenêtre mémorise sa taille, sa position et son état maximisé.
- Exporter l'utilisation au format CSV, et sauvegarder ou restaurer les paramètres et les échantillons.
- **Facultatif.** Le [Serveur de synchronisation CursorPace](https://github.com/wsj-br/CursorPace-SyncServer) maintient les installations de CursorPace sur un réseau local synchronisées. Il partage les échantillons d'utilisation et les limites de cycle de facturation, afin qu'un Mac, un PC Windows et une machine Linux dans le même environnement de développement affichent le même graphique. Installez et exécutez le serveur à partir de ce dépôt.

## Compiler à partir des sources

Sous Windows (PowerShell) ou Linux/macOS (bash) :

```bash
dotnet restore
dotnet build
dotnet test ./Tests/CursorPace.Tests.csproj
dotnet run --project ./CursorPace.csproj
```

Ou utilisez les scripts de maintenance : `.\scripts\dev.ps1` / `./scripts/dev.sh`. Ces scripts affichent la fenêtre par défaut ; utilisez `.\scripts\dev.ps1 -NoShow` ou `./scripts/dev.sh --no-show` pour utiliser la visibilité de démarrage normale de l'application.

Publication autonome (garder `PublishSingleFile=false`) :

```bash
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r linux-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-arm64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r osx-x64 --self-contained -p:PublishSingleFile=false
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=false
```

La connexion Linux nécessite l'installation de WebKitGTK 4.1 sur la machine qui exécute le binaire publié.
Une actualisation de l'utilisation en arrière-plan sur Linux utilise un hôte WebView transparent de 1x1 afin de ne pas faire clignoter la fenêtre de connexion ; macOS et Windows maintiennent cet hôte à la taille de connexion hors écran.

Le projet inclut une petite version de compatibilité d'Avalonia.Controls.WebView 12.1.0 sous
`vendor/` pour la correction de l'ABI macOS `CGRect`/`CGSize`. Aucun runtime WebView macOS séparé n'est
requis.

Empaquetage de la version :

```powershell
.\scripts\build.ps1          # Windows: Inno Setup installer
```

```bash
./scripts/build.sh             # Linux: AppImage and .deb; macOS: DMG
```

Publier uniquement (ignorer l'empaquetage) : ajouter `--skip-installer` / `-SkipInstaller`.

Le flux de travail complet du contributeur se trouve dans [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md).

## Documentation

| Document | Public |
| --- | --- |
| [QUICKSTART.md](QUICKSTART.fr.md) | Installation, connexion, utilisation quotidienne, dépannage |
| [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md) | Compiler, tester, exécuter, empaqueter, contribuer |
| [dev/CHANGELOG.md](../dev/CHANGELOG.md) | Modifications visibles par l'utilisateur |

## Avertissement

[Cursor](https://cursor.com/home) est un produit d'Anysphere, Inc. CursorPace est un projet indépendant et n'est ni associé à ni approuvé par Cursor ou Anysphere.

## Licence

Copyright 2026 Waldemar Scudeller Jr.

Ce logiciel est sous licence MIT. Consultez le fichier [LICENCE](../LICENSE) pour plus de détails.

Les traductions de cette page sont produites automatiquement. Préférez cette page en anglais lorsqu'une traduction ne correspond pas à l'application.
