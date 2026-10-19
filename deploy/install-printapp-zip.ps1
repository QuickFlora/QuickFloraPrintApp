# QuickFlora Print App - install a built app zip (no setup exe) for Tactical RMM, run as SYSTEM.
# AB#3967 / AB#3961. Pair with start-printapp-hub.ps1 (run as the logged-in user) afterwards.
#
# Placeholders filled in by whoever runs it:
#   __ZIP_URL__   https download of QuickFloraPrint-<version>-bin.zip (a short-lived S3 link is fine)
#   __ZIP_MD5__   MD5 of that zip
#   __VERSION__   expected version after install, e.g. 6.1.1
#
# Same safety rules as install-printapp.ps1 - it changes NOTHING and stops if:
#   - the app on this PC runs from somewhere other than C:\QFPrintApp\QuickfloraPrinting,
#   - there is no Config.txt, .NET Framework 4.8 is missing, or the download does not match the MD5.
# Unlike the setup exe it keeps THIS PC's server addresses: the new exe.config (which carries the
# 6.1 binding redirects) gets the addresses the old exe.config had.
# Backup: C:\QFPrintApp\backup-<old version>-<date>; rollback-printapp.ps1 restores it.
$ErrorActionPreference = 'Stop'
$zipUrl = '__ZIP_URL__'
$zipMd5 = '__ZIP_MD5__'
$want   = '__VERSION__'
$dir    = 'C:\QFPrintApp\QuickfloraPrinting'
$exe    = Join-Path $dir 'QuickfloraPrinting.exe'
$xc     = Join-Path $dir 'QuickfloraPrinting.exe.config'
$cfg    = Join-Path $dir 'Config.txt'

function Md5($p) { (Get-FileHash $p -Algorithm MD5).Hash }
'PC ' + $env:COMPUTERNAME + '  ' + (Get-Date -Format s)

# 1. What is on this PC now
$running = @(Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | ForEach-Object { $_.Path })
'running now: ' + $(if ($running.Count) { ($running | Sort-Object -Unique) -join ', ' } else { 'none' })
$elsewhere = @($running | Where-Object { $_ -and ($_ -notlike "$dir\QuickfloraPrinting.exe") })
if ($elsewhere.Count) { 'STOP: the print app on this PC runs from another folder: ' + ($elsewhere -join ', ') + '. Nothing was changed.'; exit 2 }
$old = if (Test-Path $exe) { (Get-Item $exe).VersionInfo.FileVersion } else { 'not installed' }
'installed version: ' + $old
if (-not (Test-Path $cfg)) { 'STOP: no Config.txt in ' + $dir + '. Nothing was changed.'; exit 5 }
$cfgBefore = Md5 $cfg
'Config.txt: company ' + (Get-Content $cfg -TotalCount 1) + ', terminal ' + ((Get-Content $cfg -TotalCount 4) | Select-Object -Last 1)
$rel = (Get-ItemProperty 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -ErrorAction SilentlyContinue).Release
if (-not $rel -or $rel -lt 528040) { 'STOP: .NET Framework 4.8 not found (Release=' + $rel + '). Nothing was changed.'; exit 6 }
$oldEndpoint = ''; $oldSetting = ''
if (Test-Path $xc) {
    $t = Get-Content $xc -Raw
    $oldEndpoint = [regex]::Match($t, '<endpoint\s+address="([^"]+)"').Groups[1].Value
    $oldSetting  = [regex]::Match($t, '(?s)QuickfloraPrinting_QFPrintService_QFPrintService".*?<value>([^<]+)</value>').Groups[1].Value
}
'server addresses now: ' + $oldEndpoint + ' , ' + $oldSetting

# 2. Download and check
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$work = 'C:\QFPrintApp\_install'
New-Item -ItemType Directory -Force $work | Out-Null
$zip = Join-Path $work 'app.zip'
(New-Object Net.WebClient).DownloadFile($zipUrl, $zip)
$got = Md5 $zip
if ($got -ne $zipMd5.ToUpper()) { Remove-Item $zip -Force; 'STOP: zip MD5 ' + $got + ' does not match ' + $zipMd5 + '. Nothing was changed.'; exit 3 }
'zip verified (MD5 ' + $got + ')'
$unz = Join-Path $work 'unzipped'
if (Test-Path $unz) { Remove-Item $unz -Recurse -Force }
Expand-Archive $zip -DestinationPath $unz -Force
if (Test-Path (Join-Path $unz 'Config.txt')) { Remove-Item (Join-Path $unz 'Config.txt') -Force }

# 2b. Shop may be open: never stop the app right after it picked up a ticket (a ticket taken off the
#     server but not yet printed would be lost). Wait until no PICKUP for 90 s; give up after 5 min.
$log = Join-Path $dir ('Logs\AppLog_' + (Get-Date -Format 'yyyy_MM_dd') + '.txt')
$deadline = (Get-Date).AddMinutes(5)
while ($true) {
    $lastPick = $null
    if (Test-Path $log) {
        $l = Get-Content $log -Tail 300 | Where-Object { $_ -cmatch '^\d\d:\d\d:\d\d  PICKUP ' } | Select-Object -Last 1
        if ($l) { $lastPick = [datetime]::ParseExact((Get-Date -Format 'yyyy-MM-dd') + ' ' + $l.Substring(0, 8), 'yyyy-MM-dd HH:mm:ss', $null) }
    }
    $quiet = if ($lastPick) { ((Get-Date) - $lastPick).TotalSeconds } else { 9999 }
    if ($quiet -ge 90) { 'quiet for ' + [int]$quiet + ' s - safe to restart'; break }
    if ((Get-Date) -gt $deadline) { 'STOP: still printing after 5 minutes (last pickup ' + [int]$quiet + ' s ago). Nothing was changed - try again later.'; Remove-Item $work -Recurse -Force; exit 7 }
    'printing right now (last pickup ' + [int]$quiet + ' s ago) - waiting'
    Start-Sleep 15
}

# 3. Back up the current app (files only; Logs stay where they are)
$bk = 'C:\QFPrintApp\backup-' + ($old -replace '[^0-9.]', '') + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
New-Item -ItemType Directory -Force $bk | Out-Null
Get-ChildItem $dir -File | Copy-Item -Destination $bk
'backup: ' + $bk + ' (' + (Get-ChildItem $bk).Count + ' files)'

# 4. Replace the app (the user's copy is restarted by start-printapp-hub.ps1). From here the shop is not
#    printing, so ANY failure puts the backup back and says so - the caller then restarts the old app.
Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | ForEach-Object { Stop-Process -Id $_.Id -Force }
for ($i = 0; $i -lt 20 -and (Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue); $i++) { Start-Sleep 1 }
$copied = $false
for ($try = 1; $try -le 5 -and -not $copied; $try++) {
    try { Copy-Item (Join-Path $unz '*') $dir -Recurse -Force -ErrorAction Stop; $copied = $true }
    catch { 'copy attempt ' + $try + ' failed: ' + $_.Exception.Message; Start-Sleep 3 }
}
if (-not $copied) {
    Get-ChildItem $bk -File | Copy-Item -Destination $dir -Force
    'RESULT: FAILED-RESTORED - could not replace the app files; the old version ' + $old + ' was put back from ' + $bk + '. Start it as the user.'
    exit 8
}
if ($oldEndpoint -or $oldSetting) {
    $t = Get-Content $xc -Raw
    if ($oldEndpoint) { $t = [regex]::Replace($t, '(<endpoint\s+address=")[^"]+(")', '${1}' + $oldEndpoint + '${2}') }
    if ($oldSetting)  { $t = [regex]::Replace($t, '(?s)(QuickfloraPrinting_QFPrintService_QFPrintService".*?<value>)[^<]+(</value>)', '${1}' + $oldSetting + '${2}') }
    Set-Content $xc $t -Encoding UTF8
}
# The edge store lives in C:\QFPrintApp\edge; the app runs unelevated, so Users need modify rights.
$edge = 'C:\QFPrintApp\edge'
New-Item -ItemType Directory -Force $edge | Out-Null
icacls $edge /grant '*S-1-5-32-545:(OI)(CI)M' | Out-Null

# 5. Print Monitor key, as install-printapp.ps1 does
$pwKey = 'C:\ProgramData\PrinterWatch\ingest.key'
if (Test-Path $pwKey) {
    $appKey = Join-Path $dir 'monitor.key'
    Copy-Item $pwKey $appKey -Force
    icacls $appKey /inheritance:r /grant:r 'SYSTEM:F' 'Administrators:F' 'Users:R' | Out-Null
}
Remove-Item $work -Recurse -Force

# 6. Check the result
$now = (Get-Item $exe).VersionInfo.FileVersion
'version now: ' + $now
$cfgSame = ((Md5 $cfg) -eq $cfgBefore)
'Config.txt unchanged: ' + $cfgSame
'server addresses: ' + ((Select-String -Path $xc -Pattern 'https?://[^<"]+' -AllMatches | ForEach-Object { $_.Matches.Value } | Sort-Object -Unique) -join ', ')
if (-not $now.StartsWith($want) -or -not $cfgSame) { 'RESULT: PROBLEM - check the lines above; rollback-printapp.ps1 restores ' + $bk; exit 4 }
'RESULT: OK - now run start-printapp-hub.ps1 as the logged-in user'
