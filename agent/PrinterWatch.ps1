<#
    PrinterWatch 1.0 - shop printer inspector          AB#1326
    Sunflower Technologies

    WHY THIS EXISTS
    The Print Monitor dashboard can see WHICH machines have stopped printing,
    because that is recorded server-side. It cannot see anything about the
    printers themselves - no IP, no model, no "out of paper" - because nothing
    on the shop floor has ever reported it. This is the piece that looks.

    It answers, per PC:
      * what printers are installed, and which is the default
      * each printer's ADDRESS  (an IP for network printers, USBxxx for local)
      * each printer's MODEL / DRIVER
      * whether Windows thinks it is OFFLINE
      * its FAULT state - out of paper, door open, jammed, toner
      * how many jobs are stuck in its queue right now
      * whether a given network printer actually answers on the network

    A USB printer has no IP and will never appear in a network scan. Only the
    Windows spooler can see it. That is why this reads the spooler first and
    treats the network probe as a secondary check.

    SAFE BY DESIGN
    Reads only. It never changes a printer, a setting, a queue or a file other
    than its own log. No credentials. No inbound connections. Nothing is sent
    anywhere unless -ReportUrl is given, and that is off by default.

    USAGE
      Look at this PC now:
        powershell -ExecutionPolicy Bypass -File PrinterWatch.ps1

      Also check specific network printers:
        ... -PrinterIPs 192.168.1.219,192.168.1.119

      Check the other tills too (needs admin rights across them):
        ... -Computers BFS-HP-1,BFS-HP-3,BFS-HP-14

      Keep a log:
        ... -LogPath C:\QFPrintApp\printerwatch.log
#>

[CmdletBinding()]
param(
    # Extra network printers to probe by address. Optional.
    [string[]] $PrinterIPs = @(),

    # Other PCs to inspect as well as this one. Needs rights on them.
    [string[]] $Computers = @(),

    # Append each run to this file as one JSON line. Optional.
    [string] $LogPath = "",

    # Send the result somewhere. Off unless given.
    [string] $ReportUrl = "",

    # Shop identity, only used when reporting.
    [string] $CompanyID = "",

    # Quiet mode: no table, just the log/report. For scheduled runs.
    [switch] $Silent
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"
$AgentVersion = "1.0.0"

# Win32_Printer.DetectedErrorState, mapped to words a florist would use.
$ErrorText = @{
    0="Unknown"; 1="Other"; 2="OK"; 3="Low paper"; 4="OUT OF PAPER"; 5="Low toner"
    6="OUT OF TONER"; 7="DOOR OPEN"; 8="JAMMED"; 9="OFFLINE"; 10="Service needed"
    11="Output bin full"; 12="Paper problem"; 13="Cannot print page"; 14="User intervention"
    15="Out of memory"; 16="Door open"
}
# Win32_Printer.PrinterStatus
$StatusText = @{
    1="Other"; 2="Unknown"; 3="Ready"; 4="Printing"; 5="Warming up"
    6="Stopped"; 7="OFFLINE"
}

function Get-PrinterPortAddress {
    <#  The port name is where the address hides. For a network printer it is
        usually the IP itself, or a named port whose HostAddress holds it.
        For USB it will be USB001 and there is no address to find. #>
    param($PortName, $ComputerName)

    if ([string]::IsNullOrWhiteSpace($PortName)) { return $null }
    # Already an IP?
    if ($PortName -match '^\d{1,3}(\.\d{1,3}){3}$') { return $PortName }
    # Named TCP/IP port - ask Windows what address it points at.
    try {
        $p = Get-WmiObject -Class Win32_TCPIPPrinterPort -ComputerName $ComputerName `
             -Filter "Name='$($PortName -replace "'","''")'" -ErrorAction Stop
        if ($p -and $p.HostAddress) { return $p.HostAddress }
    } catch { }
    return $null
}

function Test-PrinterAlive {
    <#  Does anything answer on a printing port?
        9100 = RAW/JetDirect, 631 = IPP, 515 = LPD.
        Short timeout: this must never hang a till. #>
    param([string] $IPAddress, [int] $TimeoutMs = 1200)

    foreach ($port in 9100, 631, 515) {
        $client = $null
        try {
            $client = New-Object System.Net.Sockets.TcpClient
            $async = $client.BeginConnect($IPAddress, $port, $null, $null)
            if ($async.AsyncWaitHandle.WaitOne($TimeoutMs, $false) -and $client.Connected) {
                $client.EndConnect($async)
                return @{ Alive = $true; Port = $port }
            }
        } catch {
        } finally {
            if ($null -ne $client) { $client.Close() }
        }
    }
    return @{ Alive = $false; Port = 0 }
}

function Get-PrinterModelFromWeb {
    <#  Most network printers serve a status page whose <title> names the model.
        Best effort only - plenty of printers have no web page. #>
    param([string] $IPAddress)
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        $r = Invoke-WebRequest -Uri ("http://" + $IPAddress) -UseBasicParsing -TimeoutSec 5
        if ($r.Content -match '(?s)<title>(.*?)</title>') {
            return ($matches[1] -replace '\s+', ' ').Trim()
        }
    } catch { }
    return $null
}

function Get-PrintersOn {
    param([string] $ComputerName)

    $rows = @()
    try {
        $printers = @(Get-WmiObject -Class Win32_Printer -ComputerName $ComputerName -ErrorAction Stop)
    } catch {
        return ,@([PSCustomObject]@{
            Computer = $ComputerName; Printer = "(could not read this PC)"
            Address = $null; Model = $null; Default = $false; Offline = $null
            State = "UNREACHABLE: $($_.Exception.Message -replace '\s+',' ')"
            Queued = $null; NetworkCheck = $null
        })
    }

    # One query for all jobs, then match - far quicker than asking per printer.
    $jobs = @()
    try { $jobs = @(Get-WmiObject -Class Win32_PrintJob -ComputerName $ComputerName -ErrorAction Stop) } catch { }

    foreach ($p in $printers) {
        $queued = $null
        try { $queued = @($jobs | Where-Object { $_.Name -like ($p.Name + ",*") }).Count } catch { $queued = $null }

        $err = "OK"
        try {
            if ($null -ne $p.DetectedErrorState) {
                $k = [int]$p.DetectedErrorState
                if ($ErrorText.ContainsKey($k)) { $err = $ErrorText[$k] }
            }
        } catch { $err = "Unknown" }

        $st = "Unknown"
        try {
            if ($null -ne $p.PrinterStatus) {
                $k = [int]$p.PrinterStatus
                if ($StatusText.ContainsKey($k)) { $st = $StatusText[$k] }
            }
        } catch { }

        $addr = Get-PrinterPortAddress -PortName $p.PortName -ComputerName $ComputerName

        $net = $null
        if ($addr -and $addr -match '^\d{1,3}(\.\d{1,3}){3}$') {
            $probe = Test-PrinterAlive -IPAddress $addr
            $net = if ($probe.Alive) { "answers on $($probe.Port)" } else { "NO ANSWER" }
        } elseif ($p.PortName -match '^USB|^DOT4|^LPT|^COM') {
            $net = "USB / local"
        }

        $rows += [PSCustomObject]@{
            Computer     = $ComputerName
            Printer      = $p.Name
            Address      = if ($addr) { $addr } else { $p.PortName }
            Model        = $p.DriverName
            Default      = [bool]$p.Default
            Offline      = [bool]$p.WorkOffline
            State        = if ($err -ne "OK") { $err } else { $st }
            Queued       = $queued
            NetworkCheck = $net
        }
    }
    return ,$rows
}

# ------------------------------------------------------------------ main

$targets = @($env:COMPUTERNAME)
foreach ($c in $Computers) { if ($c -and $targets -notcontains $c) { $targets += $c } }

$all = @()
foreach ($t in $targets) { $all += Get-PrintersOn -ComputerName $t }

# Any extra addresses that no PC has installed - genuinely unknown printers.
$known = @($all | Where-Object { $_.Address } | ForEach-Object { $_.Address })
foreach ($ip in $PrinterIPs) {
    if (-not $ip) { continue }
    if ($known -contains $ip) { continue }
    $probe = Test-PrinterAlive -IPAddress $ip
    $all += [PSCustomObject]@{
        Computer     = "(not installed on any PC checked)"
        Printer      = $ip
        Address      = $ip
        Model        = if ($probe.Alive) { Get-PrinterModelFromWeb -IPAddress $ip } else { $null }
        Default      = $false
        Offline      = (-not $probe.Alive)
        State        = if ($probe.Alive) { "Answers on network" } else { "NO ANSWER" }
        Queued       = $null
        NetworkCheck = if ($probe.Alive) { "answers on $($probe.Port)" } else { "NO ANSWER" }
    }
}

if (-not $Silent) {
    Write-Host ""
    Write-Host "PrinterWatch $AgentVersion   $env:COMPUTERNAME   $(Get-Date -Format 'ddd dd MMM HH:mm')" -ForegroundColor Cyan
    Write-Host ("=" * 100)
    if ($all.Count -eq 0) {
        Write-Host "No printers found. If this PC should have one, it is not installed in Windows." -ForegroundColor Yellow
    } else {
        $all | Format-Table -AutoSize `
            @{L="PC";      E={$_.Computer}},
            @{L="Printer"; E={ if ($_.Printer.Length -gt 30) { $_.Printer.Substring(0,29) + [char]0x2026 } else { $_.Printer } }},
            @{L="Address"; E={$_.Address}},
            @{L="Model";   E={ if ($_.Model -and $_.Model.Length -gt 30) { $_.Model.Substring(0,29) + [char]0x2026 } else { $_.Model } }},
            @{L="State";   E={$_.State}},
            @{L="Offline"; E={ if ($_.Offline) { "YES" } else { "" } }},
            @{L="Queued";  E={ if ($null -eq $_.Queued) { "?" } else { $_.Queued } }},
            @{L="Network"; E={$_.NetworkCheck}}
    }

    # Say plainly what looks wrong, rather than leaving it to be spotted.
    $bad = @($all | Where-Object {
        $_.Offline -or $_.State -match 'OUT OF|JAMMED|DOOR|OFFLINE|NO ANSWER|UNREACHABLE' -or
        ($null -ne $_.Queued -and $_.Queued -gt 5) })
    Write-Host ""
    if ($bad.Count -eq 0) {
        Write-Host "Nothing looks wrong on the printers checked." -ForegroundColor Green
    } else {
        Write-Host "NEEDS ATTENTION:" -ForegroundColor Red
        foreach ($b in $bad) {
            $why = @()
            if ($b.Offline) { $why += "Windows says offline" }
            if ($b.State -match 'OUT OF|JAMMED|DOOR|OFFLINE|UNREACHABLE') { $why += $b.State }
            if ($b.NetworkCheck -eq "NO ANSWER") { $why += "printer not answering at $($b.Address)" }
            if ($null -ne $b.Queued -and $b.Queued -gt 5) { $why += "$($b.Queued) jobs stuck in the queue" }
            Write-Host ("  - {0} on {1}: {2}" -f $b.Printer, $b.Computer, ($why -join "; ")) -ForegroundColor Red
        }
    }
    Write-Host ""
}

$report = [PSCustomObject]@{
    agentVersion = $AgentVersion
    companyID    = $CompanyID
    computer     = $env:COMPUTERNAME
    localTime    = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss")
    utcTime      = (Get-Date).ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss")
    printers     = $all
}

if ($LogPath) {
    try {
        $dir = Split-Path -Parent $LogPath
        if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Force -Path $dir | Out-Null }
        # One JSON object per line: easy to append, easy to read back later.
        ($report | ConvertTo-Json -Depth 5 -Compress) | Out-File -Append -Encoding utf8 $LogPath
        if (-not $Silent) { Write-Host "Logged to $LogPath" -ForegroundColor DarkGray }
    } catch {
        Write-Warning "Could not write the log: $($_.Exception.Message)"
    }
}

if ($ReportUrl) {
    try {
        [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
        Invoke-RestMethod -Uri $ReportUrl -Method POST -TimeoutSec 30 `
            -ContentType "application/json" `
            -Body ($report | ConvertTo-Json -Depth 5 -Compress) | Out-Null
        if (-not $Silent) { Write-Host "Reported to the dashboard." -ForegroundColor DarkGray }
    } catch {
        # Never shout at a florist mid-sale. Leave a trail and move on.
        $f = Join-Path $env:ProgramData "PrinterWatch-errors.log"
        "$((Get-Date).ToString('s'))  send failed: $($_.Exception.Message)" | Out-File -Append -Encoding utf8 $f
        if (-not $Silent) { Write-Warning "Could not send the report - noted in $f" }
    }
}
