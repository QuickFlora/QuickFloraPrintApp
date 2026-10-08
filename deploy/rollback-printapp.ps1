# QuickFlora Print App - put back the version that was there before install-printapp.ps1
# (Tactical RMM, run as SYSTEM; then run start-printapp.ps1 as the user with the old version number).
# Restores the newest C:\QFPrintApp\backup-* folder. The shop's Config.txt comes back exactly as it was.
$ErrorActionPreference = 'Stop'
$dir = 'C:\QFPrintApp\QuickfloraPrinting'
$bk = Get-ChildItem 'C:\QFPrintApp' -Directory -Filter 'backup-*' | Sort-Object CreationTime | Select-Object -Last 1
if (-not $bk) { 'STOP: no backup folder found in C:\QFPrintApp. Nothing was changed.'; exit 2 }
'restoring from ' + $bk.FullName
Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
Start-Sleep 2
Get-ChildItem $bk.FullName -File | Copy-Item -Destination $dir -Force
'version now: ' + (Get-Item (Join-Path $dir 'QuickfloraPrinting.exe')).VersionInfo.FileVersion
'RESULT: OK - now run start-printapp.ps1 as the logged-in user'
