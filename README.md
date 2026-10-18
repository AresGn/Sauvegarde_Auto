# AutosavePro - Logiciel de Sauvegarde Automatique

AutosavePro est une application Windows qui surveille et sauvegarde automatiquement les fichiers ouverts dans différents logiciels, protégeant ainsi le travail des utilisateurs contre les pertes de données accidentelles.

- 🌐 **Site officiel** : [autosavepro.vercel.app](https://autosavepro.vercel.app) (code du site : [AresGn/Autosave_site](https://github.com/AresGn/Autosave_site))
- ⬇️ **Télécharger l'installateur Windows** : [AutosavePro_Setup.exe (v1.0.0)](https://github.com/AresGn/Autosave_site/releases/tag/autosave)

## Structure du Projet

### Fichiers Principaux

- **Program.cs** : Point d'entrée de l'application, initialise l'application et gère les exceptions non traitées.
- **MainForm.cs** : Formulaire principal de l'application qui contient l'interface utilisateur principale.
- **BackupManager.cs** : Gère toute la logique de sauvegarde automatique des fichiers, incluant la détection des modifications et la création des copies de sauvegarde.
- **ApplicationConfig.cs** : Gère la configuration de l'application (préférences utilisateur, applications surveillées, etc.).
- **ApplicationRegistry.cs** : Gère l'enregistrement et le suivi des applications à surveiller.
- **ApplicationWatcher.cs** : Surveille une application spécifique et ses fichiers associés pour détecter les modifications.
- **InstalledAppsManager.cs** : Détecte et gère les applications installées sur le système.
- **Logger.cs** : Fournit des fonctionnalités de journalisation pour l'application.
- **WelcomeDialog.cs** : Dialogue de bienvenue affiché au premier démarrage.
- **ApplicationConfigForm.cs** : Formulaire permettant à l'utilisateur de configurer les paramètres de l'application.

### Dossier Helpers

Contient des classes utilitaires qui aident au fonctionnement de l'application :

- **AutoSaveTimer.cs** : Gère les minuteurs pour les sauvegardes programmées.
- **FileWatcherManager.cs** : Surveille les modifications de fichiers dans le système de fichiers.
- **Win32Api.cs** : Fournit des wrappers pour les API Windows nécessaires.
- **TitleParser.cs** : Analyse les titres des fenêtres pour extraire des informations sur les fichiers ouverts.
- **PathResolver.cs** : Résout et gère les chemins de fichiers.
- **WindowHelper.cs** : Aide à interagir avec les fenêtres Windows.
- **KeyboardManager.cs** : Gère les raccourcis clavier et les entrées clavier.
- **UIHelpers.cs** : Fournit des méthodes d'aide pour l'interface utilisateur.

### Dossier Models

Contient les modèles de données utilisés dans l'application :

- **SaveInterval.cs** : Modèle pour les intervalles de sauvegarde automatique.

### Dossier UI

Contient des composants d'interface utilisateur organisés en sous-dossiers :

#### UI/Managers
- **TabControlManager.cs** : Gère les onglets dans l'interface utilisateur.
- **FormConfigManager.cs** : Gère la configuration des formulaires.
- **SplitterManager.cs** : Gère les séparateurs redimensionnables.
- **TrayManager.cs** : Gère l'icône dans la barre d'état système (system tray).
- **IconManager.cs** : Gère les icônes utilisées dans l'application.
- **UIStateManager.cs** : Gère l'état de l'interface utilisateur.

#### UI/Controls
- **LogViewerControl.cs** : Contrôle pour afficher les journaux de l'application.
- **ApplicationListControl.cs** : Contrôle pour afficher et gérer la liste des applications surveillées.

### Fichiers de Projet

- **SauvegardeAuto.csproj** : Fichier de projet C#.
- **SauvegardeAuto.sln** et **Sauvegarde_Auto.sln** : Fichiers de solution Visual Studio.
- **installer.nsi** : Script NSIS pour créer l'installateur Windows.
- **AutosavePro_Setup.exe** : Installateur compilé pour l'application.
- **autosave.ico** : Icône de l'application.
- **LICENSE.txt** : Fichier de licence pour l'application.

## Fonctionnalités Principales

1. **Sauvegarde automatique** : Sauvegarde les fichiers à intervalles réguliers configurable par l'utilisateur.
2. **Surveillance d'applications** : Surveille les applications spécifiques et détecte les fichiers ouverts.
3. **Historique des versions** : Maintient un historique des versions sauvegardées des fichiers.
4. **Configuration flexible** : Interface utilisateur permettant de configurer les applications à surveiller et les paramètres de sauvegarde.
5. **Notification système** : Notifications via la barre d'état système pour informer l'utilisateur des sauvegardes. 
## Licence

Distribué sous licence MIT. Voir [LICENSE](LICENSE).
