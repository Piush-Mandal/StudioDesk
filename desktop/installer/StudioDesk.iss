; StudioDesk installer (Inno Setup 6). Built by .github/workflows/desktop-release.yml
;   iscc /DAppVersion=0.1.0 /DPublishDir=..\out\publish installer\StudioDesk.iss

#ifndef AppVersion
  #define AppVersion "0.1.0"
#endif
#ifndef PublishDir
  #define PublishDir "..\out\publish"
#endif

[Setup]
AppId={{6F3B2C1E-8A4D-4E2B-9C71-5D0A2F8E4B13}
AppName=StudioDesk
AppVersion={#AppVersion}
AppVerName=StudioDesk {#AppVersion}
AppPublisher=PipoZa
VersionInfoVersion={#AppVersion}
; C:\Program Files\PipoZa\StudioDesk  (Data\ is inside; user data goes to %LOCALAPPDATA%\PipoZa\StudioDesk\UserData)
DefaultDirName={autopf}\PipoZa\StudioDesk
DisableDirPage=yes
DefaultGroupName=StudioDesk
DisableProgramGroupPage=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
OutputDir=..\out\installer
OutputBaseFilename=StudioDesk-Setup-{#AppVersion}
UninstallDisplayIcon={app}\StudioDesk.exe
UninstallDisplayName=StudioDesk
CloseApplications=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\StudioDesk"; Filename: "{app}\StudioDesk.exe"
Name: "{autodesktop}\StudioDesk"; Filename: "{app}\StudioDesk.exe"; Tasks: desktopicon

[Run]
; runasoriginaluser: start the app as the normal user (not admin) so its data and keys belong to that user
Filename: "{app}\StudioDesk.exe"; Description: "{cm:LaunchProgram,StudioDesk}"; Flags: nowait postinstall skipifsilent runasoriginaluser

; Uninstall removes the app and Data\. Each user's UserData and exported files are kept.
