# QuickFlora Print App - start in the logged-in user's session (Tactical RMM: "run as user").
# Run after install-printapp.ps1 or rollback-printapp.ps1. Expected version: __VERSION__
$exe = 'C:\QFPrintApp\QuickfloraPrinting\QuickfloraPrinting.exe'
Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | ForEach-Object { 'already running: ' + $_.Path + ' - restarting'; Stop-Process -Id $_.Id -Force }
Start-Sleep 2
Start-Process $exe -WorkingDirectory (Split-Path $exe)
Start-Sleep 20
$p = Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { 'RESULT: PROBLEM - the print app did not stay running'; exit 1 }
$v = (Get-Item $p.Path).VersionInfo.FileVersion
'running: ' + $p.Path + ' v' + $v + ' [' + $p.MainWindowTitle + ']'
'auto-start: ' + (Get-ItemProperty 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -ErrorAction SilentlyContinue).QuickfloraPrinting
# today's log only (older logs are from the previous version)
$log = Join-Path (Split-Path $exe) ('Logs\AppLog_' + (Get-Date -Format 'yyyy_MM_dd') + '.txt')
if (Test-Path $log) { 'today''s log:'; Get-Content $log -Tail 5 | ForEach-Object { '  ' + $_.Substring(0, [Math]::Min(200, $_.Length)) } } else { 'no print jobs yet today' }
if ($v.StartsWith('__VERSION__')) { 'RESULT: OK' } else { 'RESULT: PROBLEM - running version ' + $v + ', expected __VERSION__' }
