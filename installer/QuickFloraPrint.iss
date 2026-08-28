; ============================================================================
; QuickFlora POS Print App - installer
; AB#1326  (parent AB#1320)
;
; Replaces the IExpress self-extracting package used for v3.0-v3.3. IExpress
; produced no Start Menu entry, no uninstaller, no Add/Remove Programs entry,
; no upgrade handling and no silent-install switch - and, because the recipe
; lived only on whoever's machine built it, no way to reproduce a build.
; This script is that recipe, in version control.
;
; Build:  iscc installer\QuickFloraPrint.iss
; Output: installer\Output\QuickFloraPrintSetup-<version>.exe
;
; ---------------------------------------------------------------------------
; INSTALL PATH IS NOT A FREE CHOICE.
;
; PrintHome.cs still hardcodes these at runtime:
;     C:\QFPrintApp\Receipts\      (downloaded receipts)
;     C:\QFPrintApp\PDF\           (worksheets and card messages)
;     C:\QFPrintApp\Config.txt     (also searched next to the exe)
;
; Moving the install to Program Files would compile and install perfectly and
; then silently fail to print, because the app would still be reading and
; writing C:\QFPrintApp. Until those paths are made relative, this installer
; must put the app exactly where the old one did.
; ============================================================================

#define AppName        "QuickFlora Print"
#define AppVersion     "3.4.0"
#define AppPublisher   "Sunflower Technologies"
#define AppURL         "https://quickflora.com"
#define ExeName        "QuickfloraPrinting.exe"

; Built binaries are expected here (see BUILDING.md).
#define BuildDir       "..\QuickfloraPrinting\bin\Release"

[Setup]
; AppId must never change - it is what lets a new version upgrade the old one
; in place and keeps a single Add/Remove Programs entry instead of stacking up.
AppId={{8F3A6C21-4D7E-4B90-9E12-2A5B7C1D9E40}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppURL}
AppSupportURL={#AppURL}
VersionInfoVersion={#AppVersion}
VersionInfoCompany={#AppPublisher}
VersionInfoDescription={#AppName} receipt and worksheet printer

; See the header note - this path is load-bearing.
DefaultDirName={sd}\QFPrintApp\QuickfloraPrinting
DisableDirPage=yes
UsePreviousAppDir=yes

DefaultGroupName={#AppName}
DisableProgramGroupPage=yes

; Writing to the root of C: and registering an uninstaller both need admin.
PrivilegesRequired=admin

OutputDir=Output
OutputBaseFilename=QuickFloraPrintSetup-v{#AppVersion}
SetupIconFile=..\QuickfloraPrinting\QFIconNew.ico
UninstallDisplayIcon={app}\{#ExeName}
UninstallDisplayName={#AppName} {#AppVersion}
Compression=lzma2
SolidCompression=yes
WizardStyle=modern

; The app is a .NET 4.0 WinExe and installs in 32-bit mode on every Windows,
; which is what keeps {sd}\QFPrintApp resolving to C:\QFPrintApp.

; NOT CODE-SIGNED. Windows SmartScreen will show "Windows protected your PC"
; and the florist has to click More info -> Run anyway. This is a known,
; accepted gap for this release - see the install guide. To sign later, add:
;   SignTool=signtool sign /fd sha256 /tr <timestamp-url> /td sha256 $f
[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "Create a shortcut on the desktop"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#BuildDir}\{#ExeName}";                        DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\{#ExeName}.config";                 DestDir: "{app}"; Flags: ignoreversion
Source: "{#BuildDir}\Microsoft.VisualBasic.PowerPacks.Vs.dll"; DestDir: "{app}"; Flags: ignoreversion skipifsourcedoesntexist

; Config.txt holds the shop's own settings (company, division, department,
; terminal, Adobe path, printer). onlyifdoesntexist means an upgrade NEVER
; overwrites a working shop's configuration - the single most damaging thing
; an installer could do here, because a wrong terminal name means the app
; polls a queue that raises no orders and nobody sees an error.
Source: "..\QuickfloraPrinting\Config.txt"; DestDir: "{app}"; Flags: onlyifdoesntexist

[Dirs]
; The app downloads into these before printing. On a fresh install they did not
; exist, DownloadFile threw, the empty catch swallowed it and the job was
; reported to the server as printed - a silently lost receipt. AB#1326 made the
; app create them too; the installer creating them up front is the belt to that
; braces. Users needs modify rights because the app runs unelevated.
Name: "{sd}\QFPrintApp\Receipts"; Permissions: users-modify
Name: "{sd}\QFPrintApp\PDF";      Permissions: users-modify
Name: "{app}";                    Permissions: users-modify

[Icons]
Name: "{group}\{#AppName}";          Filename: "{app}\{#ExeName}"
Name: "{group}\Uninstall {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}";    Filename: "{app}\{#ExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#ExeName}"; Description: "Start {#AppName} now"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Leave Receipts, PDF and Config.txt alone on uninstall - they are the shop's
; records and settings, not ours to delete.
Type: files; Name: "{app}\printlog.txt"

[Code]
const
  DotNetKey = 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full';

{ The app targets .NET Framework 4.0. Windows 10 and 11 ship with 4.8, which
  runs 4.0 apps, so this check almost never fires - but on an old Windows 7
  till it is the difference between a clear message and a crash on launch. }
function IsDotNet4Present: Boolean;
var
  Release: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM, DotNetKey, 'Release', Release) or
            RegKeyExists(HKLM, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Client');
end;

function InitializeSetup: Boolean;
begin
  Result := True;
  if not IsDotNet4Present then
  begin
    if MsgBox('QuickFlora Print needs Microsoft .NET Framework 4 and it was not found ' +
              'on this PC.' + #13#10#13#10 +
              'Install .NET Framework 4 first, then run this again.' + #13#10#13#10 +
              'Continue anyway?', mbConfirmation, MB_YESNO) = IDNO then
      Result := False;
  end;
end;

{ An upgrade cannot replace QuickfloraPrinting.exe while the old copy is still
  running in the system tray, and it is always running - it auto-starts at
  login. Without this the upgrade fails with a file-in-use error that a florist
  has no way to interpret. }
procedure CurStepChanged(CurStep: TSetupStep);
var
  ResultCode: Integer;
begin
  if CurStep = ssInstall then
  begin
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#ExeName}', '',
         SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(1200);
  end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
begin
  if CurUninstallStep = usUninstall then
  begin
    Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM {#ExeName}', '',
         SW_HIDE, ewWaitUntilTerminated, ResultCode);
    Sleep(800);
    { Remove the auto-start entry the app writes to HKCU on every launch. }
    RegDeleteValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run',
                   'QuickfloraPrinting');
  end;
end;
