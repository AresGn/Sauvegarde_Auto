# Définition du nom de l'application et de la version
!define APP_NAME "AutosavePro"
!define APP_VERSION "1.0.0"
!define APP_PUBLISHER "Arès GNIMAGNON"
!define EXE_NAME "SauvegardeAuto.exe"
!define BUILD_DIR "bin\Release\net9.0-windows\win-x64\publish"

# Paramètres généraux de l'installateur
Name "${APP_NAME}"
OutFile "${APP_NAME}_Setup.exe"
InstallDir "$PROGRAMFILES64\${APP_NAME}"
RequestExecutionLevel admin

# Inclure les bibliothèques modernes de NSIS
!include "MUI2.nsh"
!include "FileFunc.nsh"

# Définir l'icône de l'installateur
!define MUI_ICON "autosave.ico"
!define MUI_UNICON "autosave.ico"

# Définir les pages de l'interface moderne
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_LICENSE "LICENSE.txt"
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

# Définir les pages de désinstallation
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

# Définir la langue
!insertmacro MUI_LANGUAGE "French"

# Section principale d'installation
Section "Programme principal" SecMain
    SetOutPath "$INSTDIR"
    
    # Copier les fichiers du programme
    File "${BUILD_DIR}\${EXE_NAME}"
    File "autosave.ico"
    
    # Créer le dossier de sauvegarde par défaut
    CreateDirectory "$DOCUMENTS\AutosavePro"
    CreateDirectory "$DOCUMENTS\AutosavePro\FichiersNonSauvegardes"
    
    # Créer les raccourcis avec l'icône personnalisée
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${EXE_NAME}" "" "$INSTDIR\autosave.ico"
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${EXE_NAME}" "" "$INSTDIR\autosave.ico"
    
    # Démarrage automatique avec Windows
    WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "${APP_NAME}" "$INSTDIR\${EXE_NAME}"
    
    # Informations de désinstallation
    WriteUninstaller "$INSTDIR\uninstall.exe"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayName" "${APP_NAME}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "UninstallString" "$INSTDIR\uninstall.exe"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayVersion" "${APP_VERSION}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "Publisher" "${APP_PUBLISHER}"
    WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}" "DisplayIcon" "$INSTDIR\autosave.ico"
    
    # Lancer l'application après l'installation
    ExecShell "" "$INSTDIR\${EXE_NAME}"
SectionEnd

# Section de désinstallation
Section "Uninstall"
    # Arrêter l'application si elle est en cours d'exécution
    FindWindow $0 "" "${APP_NAME}"
    IsWindow $0 0 +3
        SendMessage $0 16 0 0 ; WM_CLOSE
        Sleep 1000
    
    # Supprimer les fichiers du programme
    Delete "$INSTDIR\${EXE_NAME}"
    Delete "$INSTDIR\autosave.ico"
    Delete "$INSTDIR\uninstall.exe"
    RMDir "$INSTDIR"
    
    # Supprimer les raccourcis
    Delete "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk"
    RMDir "$SMPROGRAMS\${APP_NAME}"
    Delete "$DESKTOP\${APP_NAME}.lnk"
    
    # Supprimer l'entrée de démarrage automatique
    DeleteRegValue HKCU "Software\Microsoft\Windows\CurrentVersion\Run" "${APP_NAME}"
    
    # Supprimer les informations de désinstallation
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_NAME}"
    
    # Note : Ne pas supprimer le dossier de sauvegarde pour préserver les données
    MessageBox MB_YESNO|MB_ICONQUESTION "Voulez-vous supprimer tous les fichiers de sauvegarde ?" IDNO NoDelete
        RMDir /r "$DOCUMENTS\AutosavePro"
    NoDelete:
SectionEnd

# Fonctions personnalisées
Function .onInit
    # Vérifier si une instance est déjà en cours d'exécution
    FindWindow $0 "" "${APP_NAME}"
    StrCmp $0 0 continueInstallation
        MessageBox MB_OK|MB_ICONEXCLAMATION "L'application est en cours d'exécution. Veuillez la fermer avant de continuer l'installation."
        Abort
    continueInstallation:
FunctionEnd 