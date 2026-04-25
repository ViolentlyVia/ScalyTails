; ScalyTails Inno Setup installer script
; Requires Inno Setup 6: https://jrsoftware.org/isinfo.php
;
; Run build-installer.ps1 from the project root to produce dist\ScalyTailsSetup.exe.

#define MyAppName      "ScalyTails"
#define MyAppVersion   "1.0.0"
#define MyAppPublisher "ViolentlyVia"
#define MyAppURL       "https://github.com/ViolentlyVia/ScalyTails"
#define MyAppExeName   "ScalyTails.exe"
#define MyAppSourceDir "..\publish"

[Setup]
; Stable GUID identifies this app in the Windows uninstall registry — do not change
AppId={{A7F9E2B3-4C1D-5E6F-8A9B-0C2D3E4F5A6B}}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={autopf}\{#MyAppName}
DefaultGroupName={#MyAppName}
AllowNoIcons=yes
OutputDir=..\dist
OutputBaseFilename=ScalyTailsSetup
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
; Requires admin so we can write to Program Files
PrivilegesRequired=admin
; x64 only — .NET 8 WPF does not support 32-bit
ArchitecturesInstallIn64BitMode=x64compatible
ArchitecturesAllowed=x64compatible
; Windows 10 1809 minimum (.NET 8 requirement)
MinVersion=10.0.17763
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a &desktop shortcut"; GroupDescription: "Additional shortcuts:"; Flags: unchecked

[Files]
; Recursively bundle the entire publish folder (includes runtime + all DLLs)
Source: "{#MyAppSourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#MyAppName}";                  Filename: "{app}\{#MyAppExeName}"
Name: "{group}\Uninstall {#MyAppName}";        Filename: "{uninstallexe}"
Name: "{autodesktop}\{#MyAppName}";            Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#MyAppExeName}"; \
  Description: "Launch {#MyAppName} now"; \
  Flags: nowait postinstall skipifsilent

[Code]
// Warn (not block) if Tailscale is not found, since ScalyTails can't do anything without it
procedure InitializeWizard;
var
  Found: Boolean;
begin
  Found := FileExists('C:\Program Files\Tailscale\tailscale.exe')
        or FileExists('C:\Program Files (x86)\Tailscale\tailscale.exe');

  if not Found then
    MsgBox(
      'Tailscale does not appear to be installed.' + #13#10#13#10 +
      'ScalyTails is a GUI for Tailscale and requires it to function.' + #13#10 +
      'You can download Tailscale from: https://tailscale.com/download/windows' + #13#10#13#10 +
      'Installation will continue, but ScalyTails will show an error until Tailscale is installed.',
      mbInformation, MB_OK);
end;
