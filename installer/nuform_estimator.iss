#define AppName "Nuform Estimator"
#define AppVersion "1.0.0"
#define Publisher "Nuform"
#define AppExe "Nuform Estimator.exe"
#define PublishDir "..\publish\win-x64"
#define InstallDir "{pf}\Nuform Estimator"
#define ProgramDataCfg "{commonappdata}\Nuform\Estimator"

[Setup]
AppId={{E2F7C8F3-4B3C-4A5C-A2B3-1F2E9A7B4F10}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#Publisher}
DefaultDirName={#InstallDir}
DefaultGroupName={#AppName}
DisableDirPage=no
DisableProgramGroupPage=no
OutputDir=.
OutputBaseFilename=Nuform-Estimator-Setup-{#AppVersion}
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64
WizardStyle=modern

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
; App files from publish output
Source: "{#PublishDir}\*"; DestDir: "{#InstallDir}"; Flags: ignoreversion recursesubdirs;
; Place default config only if none exists
Source: "config.template.json"; DestDir: "{#ProgramDataCfg}"; DestName: "config.json"; Flags: onlyifdoesntexist;

[Dirs]
Name: "{#ProgramDataCfg}"; Flags: uninsneveruninstall

[Icons]
Name: "{group}\{#AppName}"; Filename: "{#InstallDir}\{#AppExe}"
Name: "{commondesktop}\{#AppName}"; Filename: "{#InstallDir}\{#AppExe}"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional icons:"

[Run]
Filename: "{#InstallDir}\{#AppExe}"; Description: "Launch {#AppName}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; keep ProgramData config across uninstalls (no delete)

[Code]
// No custom code needed; installer does not create server folders.
