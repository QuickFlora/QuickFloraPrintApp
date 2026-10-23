# QuickFlora - Print app copy check (AB#4157). Tactical RMM script check: every 15 min, as SYSTEM, timeout 180 s.
# Counts QuickFlora print app copies across every Windows user on this PC (any version, any folder).
#   exit 0 = one copy (or none)          exit 1 = more than one copy (check fails, output lists every copy)
# When it fails it also sends "More than one print app" to the print web service (POSPrintAppError), which the
# POSN Alerts bell shows to the shop, and "One print app running again" once it clears. Same events Print App
# 6.1.3 sends, so PCs that only run old copies (1.0 / 3.x / 4.x, Windows 7) are flagged too.
# Windows PowerShell 2 compatible (Get-WmiObject, no Invoke-WebRequest).

function Copies {
    $list = @()
    foreach ($p in @(Get-WmiObject Win32_Process | Where-Object { $_.Name -eq 'QuickfloraPrinting.exe' })) {
        $ver = 'unknown'
        try { $ver = (Get-Item $p.ExecutablePath).VersionInfo.FileVersion } catch { }
        if (-not $ver) { $ver = 'unknown' }
        $user = ''
        try { $user = $p.GetOwner().User } catch { }
        $started = ''
        try { $started = $p.ConvertToDateTime($p.CreationDate).ToString('MM-dd HH:mm') } catch { }
        $list += New-Object PSObject -Property @{ Path = $p.ExecutablePath; Version = $ver; Session = $p.SessionId; User = $user; Started = $started }
    }
    return ,$list
}

function Send-Event($evt, $details) {
    # Terminal and print web service address come from the installed app, like the app itself.
    $app = 'C:\QFPrintApp\QuickfloraPrinting'
    $cfg = @(Get-Content (Join-Path $app 'Config.txt') -ErrorAction SilentlyContinue)
    if ($cfg.Count -lt 4) { return 'not sent: no Config.txt in ' + $app }
    $url = 'https://secure.quickflora.com/NewQFPrintWebService/QFPrintService.asmx'
    try {
        $x = [xml](Get-Content (Join-Path $app 'QuickfloraPrinting.exe.config'))
        foreach ($s in $x.SelectNodes("//setting[@name='QuickfloraPrinting_QFPrintService_QFPrintService']/value")) { if ($s.InnerText) { $url = $s.InnerText.Trim() } }
    } catch { }
    $e = [Security.SecurityElement]
    $soap = '<?xml version="1.0" encoding="utf-8"?><soap:Envelope xmlns:soap="http://schemas.xmlsoap.org/soap/envelope/"><soap:Body>' +
        '<InsertErrorDetails xmlns="https://secure.quickflora.com/"><CompanyID>' + $e::Escape($cfg[0].Trim()) + '</CompanyID><DivisionID>' + $e::Escape($cfg[1].Trim()) +
        '</DivisionID><DepartmentID>' + $e::Escape($cfg[2].Trim()) + '</DepartmentID><TerminalName>' + $e::Escape($cfg[3].Trim()) + '</TerminalName><Order></Order>' +
        '<ErrorsDetails>' + $e::Escape($evt) + '</ErrorsDetails><Details>' + $e::Escape($details) + '</Details></InsertErrorDetails></soap:Body></soap:Envelope>'
    try {
        try { [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor 3072 } catch { }
        $wc = New-Object Net.WebClient
        $wc.Encoding = [Text.Encoding]::UTF8
        $wc.Headers.Add('Content-Type', 'text/xml; charset=utf-8')
        $wc.Headers.Add('SOAPAction', '"https://secure.quickflora.com/InsertErrorDetails"')
        [void]$wc.UploadString($url, $soap)
        return 'sent "' + $evt + '" for ' + $cfg[3].Trim() + ' to ' + $url
    } catch { return 'NOT sent "' + $evt + '": ' + $_.Exception.Message }
}

$stateDir = 'C:\ProgramData\QuickFlora'
$state = Join-Path $stateDir 'PrintAppCopyCheck.txt'
$copies = Copies
if ($copies.Count -gt 1) {
    # An upgrade has two copies for a few seconds: only count it if it is still true 2 minutes later.
    Start-Sleep 120
    $copies = Copies
}
$lines = @()
foreach ($c in $copies) { $lines += ($c.Path + ' v' + $c.Version + ' (session ' + $c.Session + ', user ' + $c.User + ', started ' + $c.Started + ')') }

if ($copies.Count -le 1) {
    if ($copies.Count -eq 1) { 'OK - one print app running: ' + $lines[0] } else { 'OK - no print app running' }
    if (Test-Path $state) { Send-Event 'One print app running again' ('v=rmm-check | pc=' + $env:COMPUTERNAME + ' | count=' + $copies.Count + ' | source=rmm'); Remove-Item $state -Force }
    exit 0
}

'PROBLEM - ' + $copies.Count + ' copies of the QuickFlora print app are running on ' + $env:COMPUTERNAME + ':'
foreach ($l in $lines) { '  ' + $l }
$versions = @(); foreach ($c in $copies) { $versions += $c.Version }
$sig = ($lines | Sort-Object) -join ';'
$send = $true
if (Test-Path $state) {
    $old = @(Get-Content $state)
    if ($old.Count -ge 2 -and $old[1] -eq $sig) {
        $at = [DateTime]::MinValue
        if ([DateTime]::TryParse($old[0], [ref]$at) -and ((Get-Date) - $at).TotalMinutes -lt 55) { $send = $false }
    }
}
if ($send) {
    Send-Event 'More than one print app' ('v=rmm-check | pc=' + $env:COMPUTERNAME + ' | count=' + $copies.Count + ' | versions=' + ($versions -join ', ') + ' | also running=' + ($lines -join '; ') + ' | source=rmm')
    if (-not (Test-Path $stateDir)) { New-Item -ItemType Directory $stateDir | Out-Null }
    Set-Content $state @((Get-Date).ToString('s'), $sig)
} else { 'already reported within the hour' }
exit 1
