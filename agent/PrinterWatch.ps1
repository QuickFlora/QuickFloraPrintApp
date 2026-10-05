<#
    PrinterWatch 1.1 - shop printer inspector          AB#1326, AB#3102
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

    And, since 1.1 (AB#3102), the print app itself - so nobody has to remote
    into each PC to check it by hand:
      * QuickFlora Print version and install date, and where it is installed
      * the station (terminal) name and printer typed into its Config.txt
      * whether it is running, and whether it is running MORE THAN ONCE
      * whether it starts with Windows, and for which Windows user
      * who is logged on, and whether that user is a local Administrator
      * the QuickFlora links on desktops and in Chrome/Edge bookmarks, and the
        TerminalID= in each (that is what decides where an invoice prints)
      * the last lines and recent errors of the print app's own log
      * whether this PC can reach the QuickFlora print web service

    A USB printer has no IP and will never appear in a network scan. Only the
    Windows spooler can see it. That is why this reads the spooler first and
    treats the network probe as a secondary check.

    SAFE BY DESIGN
    Reads only. It never changes a printer, a setting, a queue or a file other
    than its own log. No credentials. No inbound connections. Nothing is sent
    anywhere unless -ReportUrl or -IngestUrl is given, and both are off by
    default. Bookmarks are read only for QuickFlora/Florica links; nothing
    else from the browser leaves the PC.

    USAGE
      Look at this PC now:
        powershell -ExecutionPolicy Bypass -File PrinterWatch.ps1

      Also check specific network printers:
        ... -PrinterIPs 192.168.1.219,192.168.1.119

      Check the other tills too (needs admin rights across them):
        ... -Computers BFS-HP-1,BFS-HP-3,BFS-HP-14

      Keep a log:
        ... -LogPath C:\QFPrintApp\printerwatch.log

      Scheduled check-in to the Print Monitor (run by the RMM every 15 min):
        ... -Silent -IngestUrl https://lvybdxahpekdpdvxmyzz.supabase.co/functions/v1/print-ingest `
            -IngestKeyFile C:\ProgramData\PrinterWatch\ingest.key

      See exactly what would be sent, without sending it:
        ... -ShowPayload
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

    # Shop identity, only used when reporting. Read from the print app's
    # Config.txt when not given.
    [string] $CompanyID = "",

    # Print Monitor check-in (Supabase print-ingest, AB#2007). Off unless given.
    [string] $IngestUrl = "",

    # File holding the fleet ingest key. The key is write-only: it can add
    # check-ins but cannot read anything back. Kept out of the command line so
    # it does not show up in RMM job history.
    [string] $IngestKeyFile = "C:\ProgramData\PrinterWatch\ingest.key",

    # Print the check-in that would be sent, and send nothing.
    [switch] $ShowPayload,

    # Quiet mode: no table, just the log/report. For scheduled runs.
    [switch] $Silent
)

Set-StrictMode -Version 2.0
$ErrorActionPreference = "Stop"
$AgentVersion = "1.1.0"

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

# ------------------------------------------------------------------ print app (1.1, AB#3102)
# Everything below looks at THIS PC only, and only reads.

function Get-TcpReach {
    param([string] $HostName, [int] $Port, [int] $TimeoutMs = 3000)
    $client = $null
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $async = $client.BeginConnect($HostName, $Port, $null, $null)
        if ($async.AsyncWaitHandle.WaitOne($TimeoutMs, $false) -and $client.Connected) {
            $client.EndConnect($async)
            return $true
        }
    } catch {
    } finally {
        if ($null -ne $client) { $client.Close() }
    }
    return $false
}

function Get-PrintAppInstalls {
    <#  Where is QuickfloraPrinting.exe? Look where it is normally unpacked, and
        wherever a running copy was started from. Never a whole-disk search. #>
    $dirs = @("C:\QFPrintApp", "C:\QFPrintApp\QuickfloraPrinting")
    try {
        Get-ChildItem -Path "C:\QFPrintApp" -ErrorAction Stop |
            Where-Object { $_.PSIsContainer } | ForEach-Object { $dirs += $_.FullName }
    } catch { }
    foreach ($pf in @($env:ProgramFiles, ${env:ProgramFiles(x86)})) {
        if (-not $pf) { continue }
        try {
            Get-ChildItem -Path $pf -ErrorAction Stop |
                Where-Object { $_.PSIsContainer -and $_.Name -match 'quickflora' } |
                ForEach-Object { $dirs += $_.FullName; $dirs += (Join-Path $_.FullName "QuickfloraPrinting") }
        } catch { }
    }
    try {
        Get-WmiObject -Class Win32_Process -Filter "Name='QuickfloraPrinting.exe'" -ErrorAction Stop |
            Where-Object { $_.ExecutablePath } |
            ForEach-Object { $dirs += (Split-Path -Parent $_.ExecutablePath) }
    } catch { }

    $seen = @{}
    $found = @()
    foreach ($d in $dirs) {
        $exe = Join-Path $d "QuickfloraPrinting.exe"
        $key = $exe.ToLower()
        if ($seen.ContainsKey($key)) { continue }
        $seen[$key] = $true
        if (-not (Test-Path -LiteralPath $exe)) { continue }
        $fi = Get-Item -LiteralPath $exe
        $found += [PSCustomObject]@{
            Path        = $exe
            FileVersion = $fi.VersionInfo.FileVersion
            Version     = $fi.VersionInfo.ProductVersion
            Built       = $fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm")
            Installed   = $fi.CreationTime.ToString("yyyy-MM-dd HH:mm")
        }
    }
    return $found
}

function Get-PrintAppConfig {
    <#  Same search order as the app itself (PrintHome.cs ResolveConfigPath):
        beside the exe, then exe\QuickfloraPrinting, then the two legacy paths.
        Line 1 company, 2 division, 3 department, 4 terminal, 5 Adobe, 6 printer. #>
    param([string] $ExeDir)
    $candidates = @()
    if ($ExeDir) {
        $candidates += (Join-Path $ExeDir "Config.txt")
        $candidates += (Join-Path (Join-Path $ExeDir "QuickfloraPrinting") "Config.txt")
    }
    $candidates += "C:\QFPrintApp\QuickfloraPrinting\Config.txt"
    $candidates += "C:\QFPrintApp\Config.txt"
    foreach ($c in $candidates) {
        if (-not (Test-Path -LiteralPath $c)) { continue }
        $l = @(Get-Content -LiteralPath $c -ErrorAction SilentlyContinue)
        $line = { param($i) if ($l.Count -gt $i) { ([string]$l[$i]).Trim() } else { $null } }
        return [PSCustomObject]@{
            Path       = $c
            Modified   = (Get-Item -LiteralPath $c).LastWriteTime.ToString("yyyy-MM-dd HH:mm")
            CompanyID  = & $line 0
            Division   = & $line 1
            Department = & $line 2
            Terminal   = & $line 3
            Printer    = & $line 5
        }
    }
    return $null
}

function Get-PrintAppProcesses {
    $rows = @()
    try {
        foreach ($p in @(Get-WmiObject -Class Win32_Process -Filter "Name='QuickfloraPrinting.exe'" -ErrorAction Stop)) {
            $owner = $null
            try { $o = $p.GetOwner(); if ($o.User) { $owner = "$($o.Domain)\$($o.User)" } } catch { }
            $started = $null
            try { $started = $p.ConvertToDateTime($p.CreationDate).ToString("yyyy-MM-dd HH:mm") } catch { }
            $rows += [PSCustomObject]@{ Pid = $p.ProcessId; Path = $p.ExecutablePath; User = $owner; Started = $started }
        }
    } catch { }
    return $rows
}

function Get-PrintAppAutoStart {
    <#  The app registers itself under HKCU\...\Run as "QuickfloraPrinting"
        (Program.cs). Running from the RMM we are SYSTEM, so read the hive of
        every user who is logged on, plus the machine-wide places. #>
    $rows = @()
    $runPath = "Software\Microsoft\Windows\CurrentVersion\Run"
    try {
        foreach ($sid in [Microsoft.Win32.Registry]::Users.GetSubKeyNames()) {
            if ($sid -notmatch '^S-1-5-21-[\d-]+$') { continue }
            $k = [Microsoft.Win32.Registry]::Users.OpenSubKey("$sid\$runPath")
            if ($null -eq $k) { continue }
            foreach ($n in $k.GetValueNames()) {
                $v = [string]$k.GetValue($n)
                if ($n -match 'quickflora' -or $v -match 'QuickfloraPrinting') {
                    $who = $sid
                    try { $who = (New-Object Security.Principal.SecurityIdentifier $sid).Translate([Security.Principal.NTAccount]).Value } catch { }
                    $rows += [PSCustomObject]@{ Where = "Run key of $who"; Command = $v }
                }
            }
            $k.Close()
        }
    } catch { }
    try {
        $k = [Microsoft.Win32.Registry]::LocalMachine.OpenSubKey($runPath)
        if ($null -ne $k) {
            foreach ($n in $k.GetValueNames()) {
                $v = [string]$k.GetValue($n)
                if ($n -match 'quickflora' -or $v -match 'QuickfloraPrinting') { $rows += [PSCustomObject]@{ Where = "Run key, all users"; Command = $v } }
            }
            $k.Close()
        }
    } catch { }
    $startup = @("C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp")
    try {
        Get-ChildItem -Path "C:\Users" -ErrorAction Stop | Where-Object { $_.PSIsContainer } | ForEach-Object {
            $startup += (Join-Path $_.FullName "AppData\Roaming\Microsoft\Windows\Start Menu\Programs\Startup")
        }
    } catch { }
    foreach ($s in $startup) {
        if (-not (Test-Path -LiteralPath $s)) { continue }
        Get-ChildItem -LiteralPath $s -Filter "*.lnk" -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -match 'quickflora|print' } |
            ForEach-Object { $rows += [PSCustomObject]@{ Where = "Startup folder $s"; Command = $_.Name } }
    }
    return $rows
}

function Get-WindowsUsers {
    <#  Who is at the keyboard, and who is a local Administrator. The group is
        found by its well-known SID so a Spanish Windows ("Administradores")
        works too. #>
    $console = $null
    try { $console = (Get-WmiObject -Class Win32_ComputerSystem -ErrorAction Stop).UserName } catch { }
    if (-not $console) {
        try {
            $ex = @(Get-WmiObject -Class Win32_Process -Filter "Name='explorer.exe'" -ErrorAction Stop)
            if ($ex.Count -gt 0) { $o = $ex[0].GetOwner(); if ($o.User) { $console = "$($o.Domain)\$($o.User)" } }
        } catch { }
    }
    $admins = @()
    try {
        $grp = (New-Object Security.Principal.SecurityIdentifier "S-1-5-32-544").Translate([Security.Principal.NTAccount]).Value.Split('\')[-1]
        $out = @(& net.exe localgroup "$grp" 2>$null)
        $in = $false
        foreach ($l in $out) {
            if ($l -match '^-{5,}') { $in = $true; continue }
            if (-not $in -or [string]::IsNullOrWhiteSpace($l)) { continue }
            if ($l -match 'command completed|comando se ha completado|se complet') { break }
            $admins += $l.Trim()
        }
    } catch { }
    $isAdmin = $null
    if ($console) {
        $short = $console.Split('\')[-1]
        $isAdmin = [bool](@($admins | Where-Object { $_ -eq $console -or $_.Split('\')[-1] -eq $short }).Count)
    }
    $profiles = @()
    try { Get-ChildItem -Path "C:\Users" -ErrorAction Stop | Where-Object { $_.PSIsContainer -and $_.Name -notmatch '^(Public|Default|Default User|All Users)$' } | ForEach-Object { $profiles += $_.Name } } catch { }
    return [PSCustomObject]@{ Console = $console; ConsoleIsAdmin = $isAdmin; Administrators = $admins; Profiles = $profiles }
}

function Get-TerminalFromUrl {
    param([string] $Url)
    $m = [regex]::Match($Url, '(?i)[?&]TerminalID=([^&#]*)')
    if ($m.Success) { return [uri]::UnescapeDataString($m.Groups[1].Value) }
    return $null
}

function Get-PosLinks {
    <#  How each person opens the POS decides which station their invoices go to
        (the TerminalID= in the link, QFPOSLogin.aspx). Read desktop shortcuts
        and Chrome/Edge bookmarks - QuickFlora/Florica links only. #>
    $rows = @()
    $isPos = '(?i)quickflora|florica|TerminalID='
    $desks = @("C:\Users\Public\Desktop")
    $users = @()
    try { $users = @(Get-ChildItem -Path "C:\Users" -ErrorAction Stop | Where-Object { $_.PSIsContainer }) } catch { }
    foreach ($u in $users) { $desks += (Join-Path $u.FullName "Desktop"); $desks += (Join-Path $u.FullName "OneDrive\Desktop") }

    $shell = $null
    try { $shell = New-Object -ComObject WScript.Shell } catch { }
    foreach ($d in $desks) {
        if (-not (Test-Path -LiteralPath $d)) { continue }
        foreach ($f in @(Get-ChildItem -LiteralPath $d -ErrorAction SilentlyContinue | Where-Object { $_.Extension -match '^\.(url|lnk)$' })) {
            $url = $null
            try {
                if ($f.Extension -eq ".url") {
                    $hit = Select-String -LiteralPath $f.FullName -Pattern '^URL=(.*)$' | Select-Object -First 1
                    if ($hit) { $url = $hit.Matches[0].Groups[1].Value }
                } elseif ($shell) {
                    $s = $shell.CreateShortcut($f.FullName)
                    $url = ("{0} {1}" -f $s.TargetPath, $s.Arguments).Trim()
                }
            } catch { }
            if ($url -and $url -match $isPos) {
                $rows += [PSCustomObject]@{ Source = "Desktop: $($f.FullName)"; Url = $url; TerminalID = (Get-TerminalFromUrl $url) }
            }
        }
    }

    foreach ($u in $users) {
        foreach ($b in @("AppData\Local\Google\Chrome\User Data", "AppData\Local\Microsoft\Edge\User Data")) {
            $root = Join-Path $u.FullName $b
            if (-not (Test-Path -LiteralPath $root)) { continue }
            foreach ($p in @(Get-ChildItem -LiteralPath $root -ErrorAction SilentlyContinue | Where-Object { $_.PSIsContainer })) {
                $bm = Join-Path $p.FullName "Bookmarks"
                if (-not (Test-Path -LiteralPath $bm)) { continue }
                try {
                    $json = (Get-Content -LiteralPath $bm -Raw -ErrorAction Stop) | ConvertFrom-Json
                    $stack = New-Object System.Collections.Stack
                    foreach ($r in $json.roots.PSObject.Properties) { if ($r.Value -is [psobject]) { $stack.Push($r.Value) } }
                    while ($stack.Count -gt 0) {
                        $n = $stack.Pop()
                        if ($n.PSObject.Properties['children']) { foreach ($c in @($n.children)) { $stack.Push($c) } }
                        if ($n.PSObject.Properties['url'] -and ([string]$n.url) -match $isPos) {
                            $browser = if ($b -match 'Edge') { "Edge" } else { "Chrome" }
                            $rows += [PSCustomObject]@{ Source = "$browser bookmark ($($u.Name), $($p.Name)): $($n.name)"; Url = [string]$n.url; TerminalID = (Get-TerminalFromUrl ([string]$n.url)) }
                        }
                    }
                } catch { }
            }
        }
    }
    return $rows
}

function Get-PrintAppLog {
    <#  The app keeps Logs\AppLog_yyyyMMdd.txt beside its exe (AB#1323). #>
    param([string] $ExeDir)
    if (-not $ExeDir) { return $null }
    $dir = Join-Path $ExeDir "Logs"
    if (-not (Test-Path -LiteralPath $dir)) { return [PSCustomObject]@{ File = $null; Note = "no Logs folder - app is older than AB#1323"; Tail = @(); Errors = @() } }
    $f = Get-ChildItem -LiteralPath $dir -Filter "AppLog_*.txt" -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $f) { return [PSCustomObject]@{ File = $null; Note = "Logs folder is empty"; Tail = @(); Errors = @() } }
    # "$l" makes a plain string. Get-Content tags every line with PSPath/PSProvider/...,
    # and Windows PowerShell 5.1's ConvertTo-Json writes all of it out: on the Lenovo
    # test PC three log lines came to 1.1 MB, past print-ingest's 1 MB limit, so every
    # check-in would have been refused. Long lines are also cut to 500 characters.
    $cut = { param($l) $l = "$l"; if ($l.Length -gt 500) { $l.Substring(0, 500) + " ...[" + $l.Length + " chars]" } else { $l } }
    $tail = @(Get-Content -LiteralPath $f.FullName -Tail 20 -ErrorAction SilentlyContinue | ForEach-Object { & $cut $_ })
    $errs = @(Select-String -LiteralPath $f.FullName -Pattern 'error|exception|fail' -ErrorAction SilentlyContinue | Select-Object -Last 20 | ForEach-Object { & $cut $_.Line })
    return [PSCustomObject]@{ File = $f.FullName; Note = "last written " + $f.LastWriteTime.ToString("yyyy-MM-dd HH:mm"); Tail = $tail; Errors = $errs }
}

function Get-PrintServiceReach {
    <#  The app polls a QuickFlora web service named in its .exe.config.
        Can this PC open a connection to it right now? #>
    param([string] $ExeDir)
    $rows = @()
    if (-not $ExeDir) { return $rows }
    $cfg = Join-Path $ExeDir "QuickfloraPrinting.exe.config"
    if (-not (Test-Path -LiteralPath $cfg)) { return $rows }
    $seen = @{}
    foreach ($m in [regex]::Matches((Get-Content -LiteralPath $cfg -Raw), '(?i)https?://[^"''<\s]+')) {
        try { $u = [uri]$m.Value } catch { continue }
        $k = "$($u.Host):$($u.Port)"
        if ($seen.ContainsKey($k)) { continue }
        $seen[$k] = $true
        $rows += [PSCustomObject]@{ Endpoint = $k; Reachable = (Get-TcpReach -HostName $u.Host -Port $u.Port) }
    }
    return $rows
}

function Get-LastPrintFile {
    <#  The app downloads each document into Receipts\ or PDF\ before printing,
        so the newest file there is roughly the last time this PC printed.
        Streamed, so a big folder costs no memory. #>
    $latest = $null
    foreach ($d in @("C:\QFPrintApp\Receipts", "C:\QFPrintApp\PDF")) {
        if (-not [IO.Directory]::Exists($d)) { continue }
        try {
            foreach ($f in [IO.Directory]::EnumerateFiles($d)) {
                $t = [IO.File]::GetLastWriteTime($f)
                if ($null -eq $latest -or $t -gt $latest) { $latest = $t }
            }
        } catch { }
    }
    return $latest
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

# The print app on this PC. Each piece is wrapped so one failure never stops the rest.
function Invoke-Safe { param([scriptblock] $Block) try { & $Block } catch { $null } }
$installs  = @(Invoke-Safe { Get-PrintAppInstalls })
$procs     = @(Invoke-Safe { Get-PrintAppProcesses })
# The copy that is running wins; otherwise the newest one on disk.
$mainExe = $null
if ($procs.Count -gt 0 -and $procs[0].Path) { $mainExe = $procs[0].Path }
elseif ($installs.Count -gt 0) { $mainExe = ($installs | Sort-Object Built -Descending | Select-Object -First 1).Path }
$exeDir    = if ($mainExe) { Split-Path -Parent $mainExe } else { $null }
$appCfg    = Invoke-Safe { Get-PrintAppConfig -ExeDir $exeDir }
$autoStart = @(Invoke-Safe { Get-PrintAppAutoStart })
$winUsers  = Invoke-Safe { Get-WindowsUsers }
$posLinks  = @(Invoke-Safe { Get-PosLinks })
$appLog    = Invoke-Safe { Get-PrintAppLog -ExeDir $exeDir }
$reach     = @(Invoke-Safe { Get-PrintServiceReach -ExeDir $exeDir })
$lastPrint = Invoke-Safe { Get-LastPrintFile }
$bootTime  = Invoke-Safe { $os = Get-WmiObject -Class Win32_OperatingSystem; $os.ConvertToDateTime($os.LastBootUpTime) }
$osName    = Invoke-Safe { (Get-WmiObject -Class Win32_OperatingSystem).Caption }
$appVersion = if ($mainExe) { (@($installs | Where-Object { $_.Path -eq $mainExe }) + @($installs) | Select-Object -First 1).Version } else { $null }
$cfgTerminal = if ($appCfg) { $appCfg.Terminal } else { $null }
if (-not $CompanyID -and $appCfg -and $appCfg.CompanyID) { $CompanyID = $appCfg.CompanyID }

# Plain-English problems with the print app, same idea as the printer list.
$appIssues = @()
if ($installs.Count -eq 0) { $appIssues += "QuickFlora Print is not installed in any usual place" }
if ($installs.Count -gt 1) { $appIssues += "$($installs.Count) copies of QuickFlora Print on this PC" }
if ($installs.Count -gt 0 -and $procs.Count -eq 0) { $appIssues += "QuickFlora Print is NOT RUNNING - nothing will print here" }
if ($procs.Count -gt 1) { $appIssues += "QuickFlora Print is running $($procs.Count) TIMES - jobs can be lost or printed twice" }
if ($installs.Count -gt 0 -and $autoStart.Count -eq 0) { $appIssues += "QuickFlora Print does not start with Windows for the logged-on user" }
if ($installs.Count -gt 0 -and -not $cfgTerminal) { $appIssues += "No station name in the print app's Config.txt" }
if ($winUsers -and $winUsers.ConsoleIsAdmin) { $appIssues += "Logged-on user $($winUsers.Console) is a local Administrator and can change the print settings" }
foreach ($r in $reach) { if (-not $r.Reachable) { $appIssues += "Cannot reach the QuickFlora print service at $($r.Endpoint)" } }
if ($cfgTerminal) {
    foreach ($l in $posLinks) {
        if ($l.TerminalID -and $l.TerminalID -ne $cfgTerminal) {
            $appIssues += "WRONG STATION: '$($l.Source)' logs in as $($l.TerminalID), but this PC's printer listens for $cfgTerminal"
        }
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
    Write-Host "QUICKFLORA PRINT APP" -ForegroundColor Cyan
    Write-Host ("-" * 100)
    foreach ($i in $installs) { Write-Host ("  Installed : {0}  v{1}  (file {2}, installed {3})" -f $i.Path, $i.Version, $i.Built, $i.Installed) }
    if ($appCfg) { Write-Host ("  Config    : {0}  company={1}  station={2}  printer={3}  (changed {4})" -f $appCfg.Path, $appCfg.CompanyID, $appCfg.Terminal, $appCfg.Printer, $appCfg.Modified) }
    Write-Host ("  Running   : {0}" -f $(if ($procs.Count) { ($procs | ForEach-Object { "pid $($_.Pid) as $($_.User) since $($_.Started)" }) -join "; " } else { "no" }))
    Write-Host ("  Auto-start: {0}" -f $(if ($autoStart.Count) { ($autoStart | ForEach-Object { $_.Where }) -join "; " } else { "none found" }))
    if ($winUsers) { Write-Host ("  Logged on : {0}   admin: {1}" -f $winUsers.Console, $winUsers.ConsoleIsAdmin) }
    foreach ($l in $posLinks) { Write-Host ("  POS link  : TerminalID={0}   {1}" -f $l.TerminalID, $l.Source) }
    foreach ($r in $reach) { Write-Host ("  Service   : {0}  reachable={1}" -f $r.Endpoint, $r.Reachable) }
    if ($lastPrint) { Write-Host ("  Last print: {0}" -f $lastPrint.ToString("yyyy-MM-dd HH:mm")) }
    if ($appLog) { Write-Host ("  App log   : {0}  {1}  ({2} recent error lines)" -f $appLog.File, $appLog.Note, @($appLog.Errors).Count) }
    Write-Host ""
    if ($appIssues.Count -eq 0) {
        Write-Host "Nothing looks wrong with the print app." -ForegroundColor Green
    } else {
        Write-Host "PRINT APP NEEDS ATTENTION:" -ForegroundColor Red
        foreach ($i in $appIssues) { Write-Host "  - $i" -ForegroundColor Red }
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
    printApp     = [PSCustomObject]@{
        installs = $installs; config = $appCfg; processes = $procs; autoStart = $autoStart
        users = $winUsers; posLinks = $posLinks; serviceReach = $reach; log = $appLog; issues = $appIssues
    }
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

# ------------------------------------------------------------------ Print Monitor check-in
# Shaped for print-ingest (AB#2007): every check-in is a heartbeat; printers ride along.
if ($IngestUrl -or $ShowPayload) {
    $mine = @($all | Where-Object { $_.Computer -eq $env:COMPUTERNAME -and $_.Printer -ne "(could not read this PC)" })
    $uptime = if ($bootTime) { [int64]((Get-Date) - $bootTime).TotalSeconds } else { $null }
    $reachable = if ($reach.Count) { -not [bool](@($reach | Where-Object { -not $_.Reachable }).Count) } else { $null }
    $defaultPrinter = if ($appCfg -and $appCfg.Printer) { $appCfg.Printer } else { ($mine | Where-Object { $_.Default } | Select-Object -First 1 | ForEach-Object { $_.Printer }) }
    $payload = [ordered]@{
        company_id      = if ($CompanyID) { $CompanyID } else { "UNKNOWN" }
        machine_name    = $env:COMPUTERNAME
        division_id     = if ($appCfg) { $appCfg.Division } else { $null }
        department_id   = if ($appCfg) { $appCfg.Department } else { $null }
        terminal_name   = $cfgTerminal
        app_version     = $appVersion
        os_version      = $osName
        default_printer = $defaultPrinter
        heartbeat       = [ordered]@{
            source           = "PrinterWatch $AgentVersion"
            server_reachable = $reachable
            last_print_at    = if ($lastPrint) { $lastPrint.ToUniversalTime().ToString("o") } else { $null }
            uptime_seconds   = $uptime
            issues           = $appIssues
            installs         = $installs
            config_path      = if ($appCfg) { $appCfg.Path } else { $null }
            config_modified  = if ($appCfg) { $appCfg.Modified } else { $null }
            processes        = $procs
            auto_start       = $autoStart
            logged_on_user   = if ($winUsers) { $winUsers.Console } else { $null }
            user_is_admin    = if ($winUsers) { $winUsers.ConsoleIsAdmin } else { $null }
            administrators   = if ($winUsers) { $winUsers.Administrators } else { @() }
            user_profiles    = if ($winUsers) { $winUsers.Profiles } else { @() }
            pos_links        = $posLinks
            service_reach    = $reach
            app_log_file     = if ($appLog) { $appLog.File } else { $null }
            app_log_note     = if ($appLog) { $appLog.Note } else { $null }
            app_log_tail     = if ($appLog) { $appLog.Tail } else { @() }
            app_log_errors   = if ($appLog) { $appLog.Errors } else { @() }
        }
        printers        = @($mine | ForEach-Object {
            [ordered]@{
                printer_name = $_.Printer
                port_name    = $_.Address
                ip_address   = if ($_.Address -match '^\d{1,3}(\.\d{1,3}){3}$') { $_.Address } else { $null }
                model        = $_.Model
                driver       = $_.Model
                is_default   = $_.Default
                is_offline   = $_.Offline
                paper_out    = [bool]($_.State -match 'OUT OF PAPER')
                jammed       = [bool]($_.State -match 'JAMMED')
                error_state  = $_.State
                queue_depth  = $_.Queued
                network      = $_.NetworkCheck
            }
        })
    }
    $body = $payload | ConvertTo-Json -Depth 6 -Compress

    if ($ShowPayload) {
        $payload | ConvertTo-Json -Depth 6
    } else {
        try {
            $key = $null
            if ($IngestKeyFile -and (Test-Path -LiteralPath $IngestKeyFile)) { $key = (Get-Content -LiteralPath $IngestKeyFile -Raw).Trim() }
            if (-not $key) { throw "no ingest key at $IngestKeyFile" }
            [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
            $r = Invoke-RestMethod -Uri $IngestUrl -Method POST -TimeoutSec 30 -ContentType "application/json" `
                    -Headers @{ "x-ingest-key" = $key } -Body ([Text.Encoding]::UTF8.GetBytes($body))
            if (-not $Silent) { Write-Host "Checked in to the Print Monitor." -ForegroundColor DarkGray }
        } catch {
            $f = Join-Path $env:ProgramData "PrinterWatch-errors.log"
            "$((Get-Date).ToString('s'))  check-in failed: $($_.Exception.Message)" | Out-File -Append -Encoding utf8 $f
            if (-not $Silent) { Write-Warning "Could not check in to the Print Monitor - noted in $f" }
        }
    }
}
