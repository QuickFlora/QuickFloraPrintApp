; ============================================================================
; QuickFlora Print App + Tactical RMM agent - one-file setup for a shop PC
; AB#3171 (Carmel pilot and rollout), AB#3164 (Print App 4.x)
;
; One .exe per shop. It:
;   1. installs the Tactical RMM agent for that shop (silent), so support can reach the PC and
;      future updates go through RMM;
;   2. upgrades the print app to the bundled version, but ONLY if this PC already has the print
;      app set up (C:\QFPrintApp\QuickfloraPrinting\Config.txt). The current app folder is backed
;      up first to C:\QFPrintApp\backup-before-<version>-<date>. Without a Config.txt it installs
;      the RMM agent only, because the print installer would otherwise drop in sample settings
;      (wrong shop and terminal);
;   3. starts the print app for the signed-in user and shows a plain summary.
; Everything it does is written to C:\QFPrintApp\bundle-install.log.
;
; Build (on a machine with Inno Setup 6), passing the shop's files:
;   ISCC.exe /DShop="Carmel Flower Shop" /DShopFile="Carmel" ^
;            /DAgentExe="C:\path\trmm-carmelflowershop-printpcs-workstation-amd64.exe" ^
;            /DPrintSetup="C:\path\QuickFloraPrintSetup-v4.0.2.exe" /DPrintVersion="4.0.2" ^
;            installer\QuickFloraPrintBundle.iss
;
; The agent .exe comes from the shop's Tactical RMM deployment link and contains an enrollment
; key: anyone holding the bundle can add a PC to that shop in RMM until the deployment expires.
; Never commit agent .exe files or bundles to git; share bundles only privately (ADO case).
; ============================================================================

#ifndef Shop
  #error Pass /DShop="Shop name"
#endif
#ifndef ShopFile
  #error Pass /DShopFile="ShortName" (used in the output file name)
#endif
#ifndef AgentExe
  #error Pass /DAgentExe="path to the shop's trmm-...-amd64.exe"
#endif
#ifndef PrintSetup
  #error Pass /DPrintSetup="path to QuickFloraPrintSetup-vX.Y.Z.exe"
#endif
#ifndef PrintVersion
  #error Pass /DPrintVersion="X.Y.Z"
#endif

[Setup]
AppId={{5B1E7C40-9A2D-4F63-8C1B-7E3D2A9F0B51}
AppName=QuickFlora Print setup for {#Shop}
AppVersion={#PrintVersion}
AppPublisher=Sunflower Technologies
VersionInfoVersion={#PrintVersion}
CreateAppDir=no
Uninstallable=no
DisableProgramGroupPage=yes
DisableReadyPage=no
PrivilegesRequired=admin
OutputDir=Output
OutputBaseFilename=QuickFloraPrintBundle-{#ShopFile}-v{#PrintVersion}
SetupIconFile=..\QuickfloraPrinting\QFIconNew.ico
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
; NOT CODE-SIGNED yet: Windows shows "Windows protected your PC" -> More info -> Run anyway.

[Messages]
ReadyLabel1=This will set up QuickFlora printing on this computer for {#Shop}.
ReadyLabel2a=It installs QuickFlora remote support (Tactical RMM) and updates the QuickFlora Print App to version {#PrintVersion}. Your shop's print settings are kept. Click Install to continue.

[Files]
Source: "{#AgentExe}";   DestDir: "{tmp}"; DestName: "trmm-agent.exe";    Flags: deleteafterinstall
Source: "{#PrintSetup}"; DestDir: "{tmp}"; DestName: "printsetup.exe";    Flags: deleteafterinstall

[Code]
const
  AppDir  = 'C:\QFPrintApp\QuickfloraPrinting';
  LogFile = 'C:\QFPrintApp\bundle-install.log';
var
  Summary: String;

procedure Log2(const S: String);
begin
  Log(S);
  SaveStringToFile(LogFile, GetDateTimeString('yyyy-mm-dd hh:nn:ss', '-', ':') + '  ' + S + #13#10, True);
end;

function ExeVersion(const Path: String): String;
var
  V: String;
begin
  if GetVersionNumbersString(Path, V) then Result := V else Result := 'not installed';
end;

{ Copy the files (not sub-folders, so Logs stay where they are) of the app folder to a backup. }
function BackupAppFolder(const Dest: String): Integer;
var
  F: TFindRec;
begin
  Result := 0;
  ForceDirectories(Dest);
  if FindFirst(AppDir + '\*', F) then
  try
    repeat
      if (F.Attributes and FILE_ATTRIBUTE_DIRECTORY) = 0 then
        if FileCopy(AppDir + '\' + F.Name, Dest + '\' + F.Name, False) then Result := Result + 1;
    until not FindNext(F);
  finally
    FindClose(F);
  end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  Code: Integer;
  Before, After, Backup: String;
begin
  if CurStep <> ssPostInstall then Exit;
  ForceDirectories('C:\QFPrintApp');
  Log2('=== QuickFlora Print bundle for {#Shop}, print app {#PrintVersion}, PC ' + GetComputerNameString);

  { 1. Remote support (Tactical RMM agent) }
  WizardForm.StatusLabel.Caption := 'Installing QuickFlora remote support...';
  if Exec(ExpandConstant('{tmp}\trmm-agent.exe'), '', '', SW_HIDE, ewWaitUntilTerminated, Code) and (Code = 0) then
  begin
    Log2('RMM agent: installed (exit 0)');
    Summary := 'Remote support: installed.';
  end else begin
    Log2('RMM agent: PROBLEM (exit ' + IntToStr(Code) + ')');
    Summary := 'Remote support: did NOT install (code ' + IntToStr(Code) + '). Please tell QuickFlora support.';
  end;

  { 2. Print app - only where it is already set up for this shop }
  Before := ExeVersion(AppDir + '\QuickfloraPrinting.exe');
  Log2('print app before: ' + Before);
  if not FileExists(AppDir + '\Config.txt') then
  begin
    Log2('print app: SKIPPED - no ' + AppDir + '\Config.txt on this PC');
    Summary := Summary + #13#10#13#10 + 'Print app: not changed. This computer does not have the QuickFlora print app set up yet, so QuickFlora support will set it up remotely.';
    Exit;
  end;
  Backup := 'C:\QFPrintApp\backup-before-{#PrintVersion}-' + GetDateTimeString('yyyymmdd-hhnnss', '-', ':');
  Log2('backup: ' + Backup + ' (' + IntToStr(BackupAppFolder(Backup)) + ' files)');

  WizardForm.StatusLabel.Caption := 'Updating the QuickFlora Print App...';
  Exec(ExpandConstant('{tmp}\printsetup.exe'), '/VERYSILENT /SUPPRESSMSGBOXES /NORESTART', '', SW_HIDE, ewWaitUntilTerminated, Code);
  After := ExeVersion(AppDir + '\QuickfloraPrinting.exe');
  Log2('print setup exit ' + IntToStr(Code) + ', print app now: ' + After);

  if (Code = 0) and (Pos('{#PrintVersion}', After) = 1) then
  begin
    { Start it for the person signed in, not as the administrator who ran this. }
    ExecAsOriginalUser(AppDir + '\QuickfloraPrinting.exe', '', AppDir, SW_SHOWNORMAL, ewNoWait, Code);
    Log2('print app started');
    Summary := Summary + #13#10#13#10 + 'Print app: updated from ' + Before + ' to ' + After + ' and started. Your print settings were kept.'
      + #13#10 + 'Check the window says "QuickFlora Print App v{#PrintVersion}" and "Connected".';
  end else begin
    Summary := Summary + #13#10#13#10 + 'Print app: the update did NOT finish (code ' + IntToStr(Code) + ', version ' + After + ').'
      + #13#10 + 'Your previous version is saved in ' + Backup + '. Please tell QuickFlora support.';
  end;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
  if CurPageID = wpFinished then
    WizardForm.FinishedLabel.Caption := Summary + #13#10#13#10 + 'Details: ' + LogFile;
end;
