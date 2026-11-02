# Démarrage rapide

**Lire dans d'autres langues :**

<small id="lang-list">[English](../QUICKSTART.md) · [Português (Brasil)](./QUICKSTART.pt-BR.md) · [Deutsch](./QUICKSTART.de.md) · [Español](./QUICKSTART.es.md) · [Français](./QUICKSTART.fr.md) · [हिन्दी](./QUICKSTART.hi.md) · [日本語](./QUICKSTART.ja.md) · [한국어](./QUICKSTART.ko.md) · [简体中文](./QUICKSTART.zh-Hans.md) · [繁體中文](./QUICKSTART.zh-Hant.md)</small>

Guide de l'utilisateur final pour CursorPace. Pour la compilation à partir des sources, voir [dev/DEVELOPMENT.md](../dev/DEVELOPMENT.md).

## Installer

Téléchargez une version depuis la page des versions de ce dépôt.

**Windows.** Exécutez `CursorPace-*-win-x64-setup.exe`. Si SmartScreen vous avertit que l'application n'est pas signée, choisissez **Plus d'informations**, puis **Exécuter quand même**. La fonction **Se connecter** nécessite le [runtime Microsoft Edge WebView2](https://go.microsoft.com/fwlink/p/?LinkId=2124703). L'installateur propose la page de téléchargement s'il est manquant.

**Linux.** Pour la plupart des distributions, `chmod +x CursorPace-*-linux-x64.AppImage` (ou le fichier `linux-arm64`) et exécutez-le. Sur Debian ou Ubuntu, `sudo apt install ./CursorPace-*-linux-x64.deb` (ou `linux-arm64`) installe GTK et WebKit. La connexion Google peut être bloquée dans WebKit ; utilisez GitHub, l'e-mail, ou connectez-vous sous Windows.

**macOS.** Ouvrez `CursorPace-*-osx-arm64.dmg` (Apple Silicon) ou `*-osx-x64.dmg` (Intel) et faites glisser `CursorPace.app` vers Applications. Si Gatekeeper bloque l'application non signée, faites un clic droit sur **Ouvrir** une fois, puis choisissez **Ouvrir quand même** dans **Réglages Système → Confidentialité et sécurité**.

## Première exécution

L'option **Démarrer dans la zone de notification** est activée par défaut, de sorte que le premier lancement peut n'afficher que l'icône de la zone de notification. Ouvrez la fenêtre à partir de cette icône, ou lancez avec `--show`.

1. Choisissez **Se connecter**.
2. Terminez la connexion Google, GitHub ou l'authentification à deux facteurs dans la fenêtre intégrée. Elle se ferme lorsque Cursor accepte la session. Si vous voyez déjà votre compte, choisissez **Continuer**.
3. Le cycle de facturation et l'utilisation apparaissent sur le graphique.

Vous pouvez vous connecter plus tard depuis **Paramètres** → **Compte Cursor**. Après une mise à jour réussie, ce titre devient **Compte Cursor (connecté)**. **Actualiser** dans la barre de titre, ou **Actualiser maintenant** dans les Paramètres, récupère les données immédiatement.

**Se déconnecter** efface la session Cursor. Sur Windows et macOS, une connexion Google ou GitHub enregistrée peut subsister, de sorte que la prochaine connexion peut ignorer ce mot de passe. Sur Linux, **Se déconnecter** efface tout le profil du navigateur. Les échantillons d'utilisation restent sur le disque.

Avec l'option **Mettre à jour l'utilisation automatiquement** activée, les actualisations se produisent à 1, 2, 4, 6 ou 12 heures à partir de minuit heure locale. Au lancement, une actualisation ne s'exécute que si la dernière réussite date d'au moins 20 minutes et qu'un créneau programmé a été manqué ou que l'intervalle complet s'est déjà écoulé.

## Fenêtre principale

<p align="center">
  <img src="../Assets/cursor_pace_screenshot.png" alt="CursorPace" width="600">
</p>

Trois cartes se trouvent au-dessus du graphique :

- **Début du cycle** et **Prochain renouvellement**. Sur le cycle en cours, le temps restant est affiché à côté de la date de renouvellement.
- Une prévision pour chaque modèle. Dans le cycle, c'est le temps d'épuisement. Au-dessus de la ligne pointillée d'utilisation prévue, c'est le moment où cette ligne atteint la lecture. Sur ou sous la ligne, c'est le pourcentage projeté au renouvellement. Sur le cycle en cours, une date inclut le temps d'attente à côté.
- **Dernière mesure** : le plus récent échantillon de ce cycle, avec le % Cursor, le % Autre, le % Utilisation prévue, et l'écart de chaque série par rapport à ce rythme.

Les boutons Précédent et Suivant sur l'en-tête du mois permettent de naviguer entre les cycles stockés. Sous cet en-tête, `Cursor dd-MMM HH:mm` est la dernière actualisation. Un point rouge signifie que le compte est déconnecté ou que l'actualisation a échoué. Cliquez dessus pour ouvrir **Compte Cursor**. Lorsqu'un serveur de synchronisation est configuré, **Serveur de synchronisation** et l'heure de sa dernière réussite apparaissent également. Un point rouge signifie que la dernière tentative a échoué. Cliquez dessus pour ouvrir **Serveur de synchronisation**.

| Plage | Ce qu'elle montre |
| --- | --- |
| **1J** / **2J** | Les dernières 24 ou 48 heures |
| **7J** / **1S** | Les sept derniers jours |
| **2S** | Les 14 derniers jours |
| **1M** | L'intégralité du cycle affiché, un échantillon par jour local |

Sur le cycle en cours, une courte période se termine à l'heure actuelle. Sur un cycle antérieur, elle se termine au renouvellement de ce cycle. Le début reste au début du cycle lorsque la fenêtre commencerait autrement plus tôt.

Faites glisser la souris sur le tracé pour zoomer. Un clic droit ramène à **1M**. Choisir un bouton de plage, y compris celui déjà sélectionné, quitte le zoom et affiche cette plage. **Chaque échantillon jusqu'à** (**2J**, **4J**, **7J** ou **14J**, par défaut **14J**) définit jusqu'où le graphique trace chaque échantillon et étiquette l'heure locale. Les plages plus longues conservent le dernier échantillon de chaque jour et étiquettent le jour du mois. L'axe vertical s'adapte aux valeurs à l'écran, jusqu'à 120 %.

Déplacez le pointeur pour lire Cursor, Autres modèles et Utilisation prévue à ce moment-là. Après le dernier échantillon, Cursor et Autres affichent l'estimation, marquée `estimated`. Pendant un zoom, la lecture affiche également chaque valeur au début du zoom et la modification à partir de là.

| Contrôle | Action |
| --- | --- |
| **Actualiser** | Récupérer l'utilisation de Cursor maintenant. Masqué sur la page Paramètres |
| **Paramètres** | Ouvrir les Paramètres dans cette fenêtre |
| **Retour** | Revenir au graphique |
| **Réduire** / **Agrandir** | Contrôles de fenêtre standard. L'application reste dans la zone de notification |
| Fermer | Masquer la fenêtre. L'application reste dans la zone de notification |

La fenêtre mémorise sa taille, sa position et son état agrandi. Les étiquettes d'information peuvent être sélectionnées et copiées.

## Prévu et estimé

- L'**Utilisation prévue** sur le graphique est la ligne en pointillés de 0 % au début du cycle à 100 % au prochain renouvellement. Le survol utilise cette ligne.
- **Cursor** et **Autres modèles** sont les échantillons mesurés.
- **Prévu** dans un CSV de cycle suit chaque échantillon, puis ajuste le quota restant à 100 % au renouvellement.
- **Estimé** est la projection en pointillés ronds du dernier échantillon vers le renouvellement. Après le dernier échantillon, le survol utilise cette ligne.

Modèles Cursor et Autres modèles restent indépendants.

## Paramètres

Les Paramètres remplacent le graphique. **Retour**, ou l'en-tête **Paramètres**, y ramène. Le dernier onglet s'ouvre à nouveau la prochaine fois, y compris après un redémarrage.

Le **Serveur de synchronisation** est facultatif. Le [Serveur de synchronisation CursorPace](https://github.com/wsj-br/CursorPace-SyncServer) partage les échantillons d'utilisation et les limites du cycle de facturation entre les machines d'un réseau local, de sorte qu'un Mac, un PC Windows et une machine Linux dans le même environnement de développement affichent le même graphique. Installez et exécutez le serveur à partir de ce dépôt. Le thème, la langue, le placement de la fenêtre et les options de démarrage restent sur chaque ordinateur.

| Onglet | Ce que vous pouvez définir |
| --- | --- |
| Démarrage et affichage | **Lancer à la connexion**, **Démarrer dans la zone de notification**, thème (Système, Clair ou Sombre), **Langue** (les paramètres régionaux dans `locales/ui-languages.json`, ou l'anglais si ce fichier est manquant), et **Chaque échantillon jusqu'à** |
| Compte Cursor | **Se connecter** (masqué lorsque Connecté), **Actualiser maintenant**, **Se déconnecter**, et actualisation automatique toutes les 1, 2, 4, 6 ou 12 heures |
| Serveur de synchronisation | **Synchroniser avec le serveur**, URL du serveur, jeton API, nom de la machine (le nom d'hôte par défaut) et **Resynchroniser maintenant**. Le lien ouvre le dépôt du serveur de synchronisation |
| Exporter et sauvegarder | **Exporter le CSV du cycle** pour le cycle affiché à l'écran, **Exporter l'utilisation** pour chaque échantillon stocké pendant que vous êtes connecté, et **Sauvegarde** / **Restaurer** des paramètres et des échantillons sous forme d'un fichier zip. **Ouvrir le dossier** ouvre la destination |
| À propos | Version, date et heure de compilation UTC, droits d'auteur, Licence MIT et le dépôt GitHub |

La sauvegarde et la restauration conservent la connexion Cursor sur cette machine.

## Tiroir

Cliquez sur l'icône, ou choisissez **Ouvrir**, pour afficher la fenêtre. **Quitter** ferme l'application. L'info-bulle affiche le pourcentage d'aujourd'hui et, une fois qu'il y a suffisamment de données, le pourcentage projeté au renouvellement.

Sous macOS, un clic ouvre le menu de la zone de notification, et l'icône du Dock est masquée tant que la fenêtre est masquée ou minimisée. Affichez la fenêtre depuis la zone de notification, avec `--show`, ou en lançant l'application à nouveau.

Si l'icône est manquante, ouvrez le débordement des notifications. Sous GNOME, l'extension AppIndicator peut être requise.

Un deuxième lancement affiche la fenêtre déjà en cours d'exécution. Un deuxième lancement de `--background` la laisse masquée. `--show` force l'ouverture de la fenêtre. **Lancer à la connexion** suit **Démarrer dans la zone de notification**.

## Données

```text
Windows: %LocalAppData%\CursorPace\
Linux:   ~/.local/share/CursorPace/
macOS:   ~/Library/Application Support/CursorPace/
```

`settings.json` contient les préférences, le positionnement de la fenêtre et les limites du cycle. `usage-samples.json` contient les échantillons. La connexion Cursor se trouve dans `WebView2` sous Windows, `WebView` sous Linux et macOS, ou `WebView-AppImage` lors de l'exécution d'une AppImage Linux. Un fichier qui ne peut pas être analysé est copié dans `*.corrupt.json` et remplacé par un fichier vide. Un fichier verrouillé ou illisible est laissé tel quel.

Supprimez le dossier pour recommencer. Sous Windows, la désinstallation de l'application le supprime. Sous Linux et macOS, supprimez-le vous-même.

## Renouvellement

Une récupération qui signale un nouveau début du cycle archive le cycle précédent et conserve ses échantillons. Précédent et Suivant ouvrent ces cycles. L'application se rafraîchit également lorsque la date locale atteint le renouvellement stocké. La fenêtre peut rester masquée. Le processus doit être en cours d'exécution pour cette vérification et pour les mises à jour automatiques.

## Désinstallation

Quittez depuis le menu de la zone de notification. Sous Windows, désinstallez **CursorPace** depuis **Applications installées**. Sous Linux et macOS, supprimez l'application et, pour un nouveau départ, le dossier de données ci-dessus.

## Dépannage

**L'application ne démarre pas.** Terminez tout processus `CursorPace` et relancez l'application. Sous Windows, vérifiez l'Observateur d'événements. Sous Linux, essayez `journalctl --user -xe`. Sous macOS, vérifiez Console.app. Si Linux utilise 100 % du processeur, ou si `crash.log` indique `Could not create glyphTypeface`, installez une version plus récente que 0.3.0. Si **Lancer à la connexion** pointe toujours vers une AppImage supprimée, exécutez la nouvelle AppImage une fois.

**La connexion échoue, ou "Le module spécifié est introuvable".** Sous Windows, installez le [runtime WebView2](https://go.microsoft.com/fwlink/p/?LinkId=2124703) (Evergreen x64). Sous Linux, installez `libwebkit2gtk-4.1-0`. Si Google bloque le navigateur intégré, utilisez GitHub ou l'e-mail. Choisissez **Continuer** lorsque votre compte Cursor est déjà affiché à l'écran.

**L'utilisation ne se met pas à jour.** Confirmez **Compte Cursor (connecté)** et les mises à jour automatiques, ou choisissez **Actualiser**. Vérifiez la date, l'heure et le fuseau horaire du système. Après une limite de débit Cursor, l'application attend le prochain intervalle.

**L'icône de la barre d'état système ou de la barre des tâches est manquante.** Ouvrez le débordement des notifications. Sous Linux, redémarrez l'application pour qu'elle puisse écrire `~/.local/share/applications/CursorPace.desktop`. Si l'icône de la barre des tâches est toujours un engrenage générique, déconnectez-vous une fois, ou sous X11, appuyez sur Alt+F2, tapez `r`, puis appuyez sur Entrée.

**Les Paramètres ne sont pas enregistrés, ou le pourcentage d'aujourd'hui semble incorrect.** Confirmez que vous pouvez écrire dans le dossier de Données. Si `settings.corrupt.json` existe, supprimez-le ainsi que `settings.json` pour réinitialiser. Vérifiez ensuite l'horloge et choisissez **Actualiser**.

**Lancer à la connexion ne fait rien.** Confirmez que le paramètre est activé. Windows stocke `CursorPace` dans la clé Run de l'utilisateur actuel, et ajoute `--background` lorsque le démarrage depuis la barre d'état système est activé. Une application macOS signée apparaît sous **Ouvrir à la connexion**. Une version non signée utilise `~/Library/LaunchAgents/com.cursorpace.app.plist` et démarre le `.app` avec `/usr/bin/open`. Linux utilise `~/.config/autostart/cursor-pace.desktop`. Une entrée AppImage doit pointer vers le fichier `.AppImage`. Désactivez et réactivez le paramètre, ou lancez la nouvelle AppImage une fois, pour la réécrire.

**Deux icônes de barre d'état système, ou déconnecté après un redémarrage Linux.** Quittez depuis la barre d'état système et démarrez l'application une fois. Sous Linux, connectez-vous à nouveau sur une version actuelle afin que la session soit stockée dans `cookies.sqlite`.

Les traductions de cette page sont produites automatiquement. Préférez cette page en anglais lorsqu'une traduction ne correspond pas à l'application.
