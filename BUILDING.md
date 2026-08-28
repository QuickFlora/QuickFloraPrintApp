# Building QuickFlora Print App

No Visual Studio required. Windows ships with an MSBuild that can build this project.

## Quick build (app only)

```cmd
git clone https://github.com/QuickFlora/QuickFloraPrintApp.git
cd QuickFloraPrintApp
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe QuickfloraPrinting.sln /p:Configuration=Release /p:GenerateSerializationAssemblies=Off
```

Output lands in `QuickfloraPrinting\bin\Release\`. `GenerateSerializationAssemblies=Off` is needed on
WEB1STG because `sgen.exe` is not installed there; it is harmless everywhere else.

## Building the installer

The installer is an [Inno Setup](https://jrsoftware.org/isdl.php) script at
`installer\QuickFloraPrint.iss`. Install Inno Setup 6 on the build machine, then:

```cmd
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer\QuickFloraPrint.iss
```

Output: `installer\Output\QuickFloraPrintSetup-v<version>.exe`.

Bump `AppVersion` in the `.iss` **and** `AssemblyVersion`/`AssemblyFileVersion` in
`QuickfloraPrinting\Properties\AssemblyInfo.cs` together — the CI build fails on purpose if the
executable is still stamped `1.0.0.x`.

### What the installer does that the old one did not

v3.0–v3.3 were packaged with **IExpress**, the self-extract tool built into Windows. It worked for
getting a build to a tester and nothing more: no Start Menu entry, no uninstaller, no Add/Remove
Programs entry, no upgrade handling, no silent-install switch — and no recipe in version control, so
nobody but the person who built it could make another one. The Inno Setup script replaces all of that.

| | IExpress (v3.0–v3.3) | Inno Setup (v3.4+) |
|---|---|---|
| Start Menu shortcut | no | yes |
| Uninstaller / Add-Remove entry | no | yes |
| Upgrade over an existing install | splats over the top | in-place, keeps settings |
| Closes the running tray copy first | no | yes (`taskkill` before install) |
| Preserves the shop's `Config.txt` | no | yes (`onlyifdoesntexist`) |
| Silent install for remote push | no | yes (`/SILENT`, `/VERYSILENT`) |
| .NET Framework 4 check | no | yes |
| Reproducible from the repo | **no** | yes |

### The install path is not a free choice

`PrintHome.cs` still hardcodes `C:\QFPrintApp\Receipts\`, `C:\QFPrintApp\PDF\` and
`C:\QFPrintApp\Config.txt`. Installing anywhere else — Program Files, for instance — compiles and
installs perfectly and then silently fails to print, because the app keeps reading and writing
`C:\QFPrintApp`. Until those paths are made relative to the executable, the installer must put the app
exactly where the old one did. That relative-path fix is tracked separately.

### Code signing

**The installer is not code-signed.** Windows SmartScreen shows *"Windows protected your PC"* and the
user must click **More info → Run anyway**. This is a known and accepted gap for v3.4.

When a certificate is bought, add this to `[Setup]` in the `.iss` — no other change is needed:

```
SignTool=signtool sign /fd sha256 /tr http://timestamp.digicert.com /td sha256 $f
```

## Continuous builds

`.github/workflows/build.yml` builds the app **and** the installer on every push to `main` or an `AB*`
branch, and publishes the setup exe as a run artifact.

It runs on a **self-hosted runner on WEB1STG (100.25.165.137, staging)**, not on GitHub's own runners.
GitHub-hosted runs have failed with `startup_failure` at 0 seconds since 10 Aug 2026: the QuickFlora org
is on the Free plan with a private repo, so there are no hosted Actions minutes. A self-hosted runner
costs nothing and uses the machine we already build on.

### Registering the runner on WEB1STG (one time)

1. In GitHub: **Settings → Actions → Runners → New self-hosted runner → Windows**.
2. On WEB1STG, run the commands GitHub shows, into `C:\actions-runner`.
3. When prompted for labels, enter: `windows,web1stg`
4. Install it as a service so it survives reboot:
   ```cmd
   cd C:\actions-runner
   .\svc.cmd install
   .\svc.cmd start
   ```
5. Install Inno Setup 6 on the box, or the installer step fails with a clear message.

The runner needs no inbound firewall rule — it makes an outbound connection to GitHub and polls.

## Configuring an installed copy

The **setup wizard runs on first launch** (AB#1326) and writes `Config.txt` for you: named fields, a
dropdown of printers Windows can actually see, Adobe Reader detected automatically, and Test print /
Test cash drawer buttons before you can press Finish.

Hand-editing is only needed for debugging. The file is read **by line number** — order matters and
blank lines break it:

```
line 1  CompanyID          e.g. QuickfloraDemo
line 2  DivisionID         e.g. DEFAULT
line 3  DepartmentID       e.g. DEFAULT
line 4  TerminalID         e.g. TEST
line 5  full path to Adobe Reader (AcroRd32.exe)
line 6  default printer name, exactly as Windows shows it
```

Setting line 6 to `Microsoft Print to PDF` makes printing testable with no receipt printer — a
successful print pops a Save-as-PDF dialog.

## Verifying a receipt actually fires the cash drawer

The app sends the receipt file to the printer byte-for-byte. The last bytes decide the drawer:

| Bytes | Meaning |
|---|---|
| `07 1b 64 30` | `BEL` + `ESC d 0` — Star: **kick the drawer**, then cut |
| `1d 56 41 00` | `GS V A 0` — cut only, **no drawer** |

**The drawer has never been confirmed on real hardware.** The 15 Aug test ran on a Brother HL-L2360D,
a laser printer with no drawer attached, so it could not have shown a drawer byte under any fix.
Nobody on the team has a receipt printer with a drawer (confirmed with Rajesh, 14 Aug 2026).

## Do not touch

The `winspool` P/Invoke in `clsPrinting.cs` is what gets ESC/POS control bytes (drawer kick and cut)
to the printer unmodified. Printing through the normal .NET print path swallows them.
