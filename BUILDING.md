# Building QuickFlora Print App

No Visual Studio required. Windows ships with an MSBuild that can build this project.

## On a Windows 10/11 PC

```cmd
git clone https://github.com/QuickFlora/QuickFloraPrintApp.git
cd QuickFloraPrintApp
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe QuickfloraPrinting.sln /p:Configuration=Release
```

The build output lands in `QuickfloraPrinting\bin\Release\` — `QuickfloraPrinting.exe` is the app.

### To install and run it

1. Copy the whole `bin\Release` folder to `C:\QFPrintApp\` (**that path is hardcoded** — the app writes
   receipts to `C:\QFPrintApp\Receipts\` regardless of where the exe lives).
2. Create the `Receipts` and `PDF` subfolders if they don't exist.
3. Edit `Config.txt`. It is read **by line number** — order matters, blank lines break it:
   ```
   line 1  CompanyID          e.g. QuickfloraDemo
   line 2  DivisionID         e.g. DEFAULT
   line 3  DepartmentID       e.g. DEFAULT
   line 4  TerminalID         e.g. TEST
   line 5  full path to Adobe Reader (AcroRd32.exe)
   line 6  default printer name, exactly as Windows shows it
   ```
4. Run `QuickfloraPrinting.exe`. It sits in the system tray and polls every 5 seconds.

### Verifying a receipt actually fires the cash drawer

The app sends the receipt file to the printer byte-for-byte. The last bytes decide the drawer:

| Bytes | Meaning |
|---|---|
| `07 1b 64 30` | `BEL` + `ESC d 0` — Star: **kick the drawer**, then cut |
| `1d 56 41 00` | `GS V A 0` — cut only, **no drawer** |

## Why there is no CI build yet

A GitHub Actions workflow exists at `.github/workflows/build.yml` and is registered and active, but runs
fail with `startup_failure` before any job is scheduled. Actions are enabled at both org and repo level,
the YAML is valid, and org Actions usage is zero — so the cause is most likely an org billing/spending-limit
setting, which has to be resolved in the GitHub billing UI:
https://github.com/organizations/QuickFlora/settings/billing

Until that is sorted, build locally with the command above.
