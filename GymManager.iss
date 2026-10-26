#define AppName "GymManager"
#define AppVersion "1.0.0"
#define AppPublisher "GymManager"
#define PublishDirectory "artifacts\publish\win-x64"

[Setup]
AppId={{8D20A7CF-64E6-4AF2-9ECA-C3C32B1D7F49}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
UninstallDisplayIcon={app}\GymManager.exe
OutputDir=artifacts\installer
OutputBaseFilename={#AppName}-Setup-{#AppVersion}-x64
SetupIconFile=Assets\app_icon.ico
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
PrivilegesRequired=admin
DisableProgramGroupPage=yes
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Tasks]
Name: "desktopicon"; Description: "Crear un acceso directo en el escritorio"; GroupDescription: "Accesos directos:"; Flags: unchecked

[Files]
Source: "{#PublishDirectory}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{autoprograms}\{#AppName}"; Filename: "{app}\GymManager.exe"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\GymManager.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\GymManager.exe"; Description: "Abrir {#AppName}"; Flags: postinstall nowait skipifsilent
