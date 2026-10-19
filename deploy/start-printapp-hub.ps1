# QuickFlora Print App 6.1+ - start in the logged-in user's session (Tactical RMM: "run as user").
# Run after install-printapp-zip.ps1. Expected version: __VERSION__
# __HUB__ = off : no production hub yet - the app uses the long-poll only, exactly as 6.0.0
#               (HKCU\Software\QuickfloraPrinting\EdgeHubDisabled = 1).
# __HUB__ = on  : the app connects to <print web service host>/edgehub (EdgeHubDisabled = 0).
$exe = 'C:\QFPrintApp\QuickfloraPrinting\QuickfloraPrinting.exe'
$hub = '__HUB__'
$key = 'HKCU:\Software\QuickfloraPrinting'
New-Item -Path $key -Force | Out-Null
Set-ItemProperty -Path $key -Name 'EdgeHubDisabled' -Value $(if ($hub -eq 'on') { 0 } else { 1 }) -Type DWord
'hub: ' + $hub + ' (EdgeHubDisabled=' + (Get-ItemProperty $key).EdgeHubDisabled + ')'
Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | ForEach-Object { 'already running: ' + $_.Path + ' - restarting'; Stop-Process -Id $_.Id -Force }
Start-Sleep 2
Start-Process $exe -WorkingDirectory (Split-Path $exe)
Start-Sleep 25
$p = Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | Select-Object -First 1
if (-not $p) { 'RESULT: PROBLEM - the print app did not stay running'; exit 1 }
$v = (Get-Item $p.Path).VersionInfo.FileVersion
'running: ' + $p.Path + ' v' + $v + ' [' + $p.MainWindowTitle + ']'
$log = Join-Path (Split-Path $exe) ('Logs\AppLog_' + (Get-Date -Format 'yyyy_MM_dd') + '.txt')
if (Test-Path $log) { 'today''s log:'; Get-Content $log -Tail 6 | ForEach-Object { '  ' + $_.Substring(0, [Math]::Min(200, $_.Length)) } }
if ($v.StartsWith('__VERSION__')) { 'RESULT: OK' } else { 'RESULT: PROBLEM - running version ' + $v + ', expected __VERSION__' }
