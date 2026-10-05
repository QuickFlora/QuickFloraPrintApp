# AB#3102 - Upgrade a shop PC from the old bin\Release print app to QuickFlora Print 3.4.0.
# Paste into Tactical RMM > agent > Send Command > PowerShell, run as SYSTEM (Run As User OFF).
# Only when the shop is closed. Keeps Config.txt (station + printer). Leaves the old bin\Release
# copy on disk for rollback, and points the user's Startup shortcut at 3.4 so only 3.4 starts.
$ErrorActionPreference='Stop'
try {
  [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
  $inst = 'C:\ProgramData\QFPrintSetup\QuickFloraPrintSetup-v3.4.0.exe'
  New-Item -ItemType Directory -Force (Split-Path $inst) | Out-Null
  Invoke-WebRequest -UseBasicParsing 'https://github.com/QuickFlora/QuickFloraPrintApp/releases/download/v3.4-installer/QuickFloraPrintSetup-v3.4.0.exe' -OutFile $inst
  'downloaded ' + (Get-Item $inst).Length + ' bytes'
  $cfg = 'C:\QFPrintApp\QuickfloraPrinting\Config.txt'
  $cfgHash = (Get-FileHash $cfg).Hash
  $p = Start-Process $inst -ArgumentList '/VERYSILENT','/SUPPRESSMSGBOXES','/NORESTART','/SP-','/LOG=C:\ProgramData\QFPrintSetup\install.log' -Wait -PassThru
  'installer exit code ' + $p.ExitCode + ' (0 = OK)'
  $new = 'C:\QFPrintApp\QuickfloraPrinting\QuickfloraPrinting.exe'
  'new exe version: ' + (Get-Item $new).VersionInfo.FileVersion
  'Config.txt unchanged: ' + ((Get-FileHash $cfg).Hash -eq $cfgHash)
  $sh = New-Object -ComObject WScript.Shell
  Get-ChildItem 'C:\Users\*\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\*.lnk' -ErrorAction SilentlyContinue | ForEach-Object {
    $l = $sh.CreateShortcut($_.FullName)
    if ($l.TargetPath -like '*QuickfloraPrinting.exe') {
      'startup shortcut ' + $_.FullName + ' was -> ' + $l.TargetPath
      $l.TargetPath = $new; $l.WorkingDirectory = Split-Path $new; $l.Arguments = '/autostart'; $l.Save()
      '   now -> ' + $l.TargetPath + ' /autostart'
    }
  }
  'print app running now: ' + ((Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | ForEach-Object { $_.Path }) -join ', ')
  'NEXT: send the second command with Run As User ON to start 3.4 for the logged-on user.'
} catch { 'FAILED: ' + $_.Exception.Message }

# ---- SECOND COMMAND (separate Send Command, Run As User = ON):
# Start-Process 'C:\QFPrintApp\QuickfloraPrinting\QuickfloraPrinting.exe' -ArgumentList '/autostart'
#
# ---- ROLLBACK (Run As User OFF): point the Startup shortcut back at the old copy:
# $sh=New-Object -ComObject WScript.Shell; Get-ChildItem 'C:\Users\*\AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup\*.lnk' | % { $l=$sh.CreateShortcut($_.FullName); if ($l.TargetPath -like '*QuickfloraPrinting.exe') { $l.TargetPath='C:\QFPrintApp\QuickfloraPrinting\bin\Release\QuickfloraPrinting.exe'; $l.Arguments=''; $l.Save() } }
