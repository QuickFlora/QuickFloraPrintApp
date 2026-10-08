# QuickFlora Print App - silent install / upgrade for Tactical RMM (run as SYSTEM).
# AB#3171 / AB#3164. Pair with start-printapp.ps1 (run as the logged-in user) afterwards.
#
# Placeholders filled in by whoever runs it:
#   __SETUP_URL__   https download of QuickFloraPrintSetup-vX.Y.Z.exe
#   __SETUP_MD5__   MD5 of that file from the CI run's build-info.txt
#   __VERSION__     expected version after install, e.g. 4.0.2
#
# It changes NOTHING and stops if:
#   - the app on this PC is not installed in C:\QFPrintApp\QuickfloraPrinting (the installer
#     can only install there, see installer\QuickFloraPrint.iss), or
#   - the download does not match the expected MD5, or
#   - there is no Config.txt (the installer would put sample settings in its place).
# Before installing it backs up the app folder (exe, configs, Config.txt; not Logs) to
# C:\QFPrintApp\backup-<old version>-<date>, which rollback-printapp.ps1 restores.
$ErrorActionPreference = 'Stop'
$setupUrl = '__SETUP_URL__'
$setupMd5 = '__SETUP_MD5__'
$want     = '__VERSION__'
$dir      = 'C:\QFPrintApp\QuickfloraPrinting'
$exe      = Join-Path $dir 'QuickfloraPrinting.exe'

function Md5($p) { (Get-FileHash $p -Algorithm MD5).Hash }
'PC ' + $env:COMPUTERNAME + '  ' + (Get-Date -Format s)

# 1. What is on this PC now
$running = @(Get-Process QuickfloraPrinting -ErrorAction SilentlyContinue | ForEach-Object { $_.Path })
'running now: ' + $(if ($running.Count) { ($running | Sort-Object -Unique) -join ', ' } else { 'none' })
$runKeys = @()
foreach ($h in Get-ChildItem Registry::HKEY_USERS -ErrorAction SilentlyContinue) {
    $v = (Get-ItemProperty ("Registry::" + $h.Name + '\Software\Microsoft\Windows\CurrentVersion\Run') -ErrorAction SilentlyContinue).QuickfloraPrinting
    if ($v) { $runKeys += $v }
}
'auto-start entries: ' + $(if ($runKeys.Count) { $runKeys -join ' ; ' } else { 'none' })
$elsewhere = @($running + $runKeys | Where-Object { $_ -and ($_ -notlike "*$dir\QuickfloraPrinting.exe*") -and ($_ -notlike '*\v40-test\*') -and ($_ -notlike '*\v35-test\*') })
if ($elsewhere.Count) { 'STOP: the print app on this PC runs from another folder: ' + (($elsewhere | Sort-Object -Unique) -join ', '); 'Nothing was changed.'; exit 2 }
$old = if (Test-Path $exe) { (Get-Item $exe).VersionInfo.FileVersion } else { 'not installed' }
'installed version: ' + $old
$cfg = Join-Path $dir 'Config.txt'
$cfgBefore = if (Test-Path $cfg) { Md5 $cfg } else { '' }
if (-not $cfgBefore) { 'STOP: no Config.txt in ' + $dir + '. Installing would give this PC the sample settings (wrong shop and terminal). Set up Config.txt first. Nothing was changed.'; exit 5 }
'Config.txt: present (company ' + (Get-Content $cfg -TotalCount 1) + ', terminal ' + ((Get-Content $cfg -TotalCount 4) | Select-Object -Last 1) + ')'

# 2. Download and check the installer
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$work = 'C:\QFPrintApp\_install'
New-Item -ItemType Directory -Force $work | Out-Null
$setup = Join-Path $work 'QuickFloraPrintSetup.exe'
(New-Object Net.WebClient).DownloadFile($setupUrl, $setup)
$got = Md5 $setup
if ($got -ne $setupMd5.ToUpper()) { Remove-Item $setup -Force; 'STOP: installer MD5 ' + $got + ' does not match ' + $setupMd5 + '. Nothing was changed.'; exit 3 }
'installer verified (MD5 ' + $got + ')'

# 3. Back up the current app and the shop's settings
if (Test-Path $dir) {
    $bk = 'C:\QFPrintApp\backup-' + ($old -replace '[^0-9.]', '') + '-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
    New-Item -ItemType Directory -Force $bk | Out-Null
    Get-ChildItem $dir -File | Copy-Item -Destination $bk
    'backup: ' + $bk + ' (' + (Get-ChildItem $bk).Count + ' files)'
}

# 4. Install silently (the installer closes the running app itself)
$log = Join-Path $work ('setup-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
$p = Start-Process $setup -ArgumentList '/VERYSILENT', '/SUPPRESSMSGBOXES', '/NORESTART', ('/LOG="' + $log + '"') -Wait -PassThru
'installer exit code: ' + $p.ExitCode

# 5. Check the result
$now = if (Test-Path $exe) { (Get-Item $exe).VersionInfo.FileVersion } else { 'MISSING' }
'version now: ' + $now
$cfgAfter = if (Test-Path $cfg) { Md5 $cfg } else { '' }
'Config.txt unchanged: ' + ($cfgBefore -eq $cfgAfter)
$xc = Join-Path $dir 'QuickfloraPrinting.exe.config'
if (Test-Path $xc) { 'server addresses: ' + ((Select-String -Path $xc -Pattern 'https?://[^<"]+' -AllMatches | ForEach-Object { $_.Matches.Value } | Sort-Object -Unique) -join ', ') }
Remove-Item $setup -Force

# 6. AB#3164: 4.0.3+ sends each ticket's result to the Print Monitor. The app runs as the shop user,
#    who cannot read PrinterWatch's key (SYSTEM/Administrators only), so copy the write-only fleet key
#    beside the exe. No PrinterWatch key on this PC = no reporting; printing is unaffected.
$pwKey = 'C:\ProgramData\PrinterWatch\ingest.key'
if (Test-Path $pwKey) {
    $appKey = Join-Path $dir 'monitor.key'
    Copy-Item $pwKey $appKey -Force
    icacls $appKey /inheritance:r /grant:r 'SYSTEM:F' 'Administrators:F' 'Users:R' | Out-Null
    'Print Monitor key: copied beside the app'
} else { 'Print Monitor key: none on this PC (PrinterWatch not set up) - the app will not report to the Print Monitor' }
if ($p.ExitCode -ne 0 -or -not $now.StartsWith($want) -or ($cfgBefore -and $cfgBefore -ne $cfgAfter)) { 'RESULT: PROBLEM - check the lines above; rollback-printapp.ps1 restores the backup'; exit 4 }
'RESULT: OK - now run start-printapp.ps1 as the logged-in user'
