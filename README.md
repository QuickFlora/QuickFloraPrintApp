# QuickFlora POS Windows Print App (`QuickfloraPrinting`)

The Windows client that every QuickFlora florist installs on their POS PC. It sits in the
system tray, polls the QuickFlora print web service, downloads each receipt, and sends it
**raw** to the receipt printer — including the ESC/POS control bytes that fire the cash
drawer and the auto-cutter.

> **This repository was created on 2026-08-10 from `QFPrintApp.zip`.**
> Until that date this application had **no source repository at all** — production software
> on every florist's PC existing only as a zip file. This first commit is an unmodified
> baseline of that zip so there is an exact, dated record of what we believe is deployed.
> **Nothing in this commit has been changed, formatted or fixed.**

## What it is

| | |
|---|---|
| Type | WinForms desktop app with a system-tray icon (`NotifyIcon`) |
| Framework | **.NET Framework 4.0** — out of Microsoft support since January 2016 |
| Output | `QuickfloraPrinting.exe` (`WinExe`) |
| Install path | `C:\QFPrintApp\` (hardcoded) |
| Receipts written to | `C:\QFPrintApp\Receipts\` (hardcoded) |
| Baseline binary | 496,640 bytes · md5 `24ecfc62920436426fb5943690cac90b` · built **2020-04-21 18:20:50 UTC** |
| Assembly version | `1.0.0.0` — never incremented |

## How it works

```
QuickFlora POS (browser)
   -> writes a row into Enterprise.dbo.POSPrintRequest

QuickfloraPrinting.exe (this app, on the shop's PC)
   -> polls https://secure.quickflora.com/NewQFPrintWebService/QFPrintService.asmx
      (CheckPOSForPrinting / PingPOSForPrinting / UpdatePOSForPrinting)
   -> downloads the receipt .txt from https://secure.quickflora.com/FAX/<name>.txt
   -> RawPrinterHelper.SendFileToPrinter(<printer name>, C:\QFPrintApp\Receipts\<file>)
      via winspool.drv StartDocPrinter / WritePrinter
```

## ⚠ Do not "modernize" the raw printing path

`clsPrinting.cs` uses `winspool.drv` `StartDocPrinter` / `WritePrinter` P/Invoke to send the
payload bytes **verbatim**. This is deliberate and load-bearing. It is the only reason the
ESC/POS control bytes survive:

| Bytes | Meaning |
|---|---|
| `07 1b 64 30` | `BEL` + `ESC d 0` — Star: **kick the cash drawer**, then cut |
| `1d 56 41 00` | `GS V A 0` — Epson-style cut only, **no drawer** |

Switching to `System.Drawing.Printing`, `PrintDocument` or `DrawString` would render the
payload as *text to be drawn* and silently discard those bytes — permanently breaking cash
drawers and auto-cut for every florist.

## Known problems (as of this baseline)

1. **Config is positional.** `Config.txt` is read by **line number** — line 1 CompanyID,
   line 2 DivisionID, line 3 DepartmentID, line 4 TerminalID, line 5 Adobe path,
   line 6 default printer. A blank line or a reorder silently mis-configures a shop with no error.
2. **Assembly version never changes** (`1.0.0.0`), so there is no way to tell which build any
   florist is running.
3. **.NET Framework 4.0** — unsupported since 2016.
4. **Adobe Reader dependency** for PDF worksheets, via a hardcoded path in `Config.txt`.
   Machines in the field have been seen running **Reader 9.0** (2008, unpatched since 2013).
5. **Hardcoded paths** — `C:\QFPrintApp\` and its `Receipts` subfolder.
6. **Two endpoints in `app.config`** — a live one under `Settings`
   (`/NewQFPrintWebService/QFPrintService.asmx`, the one the code actually calls) and a legacy
   WCF `<endpoint>` pointing at `/QFNEWPOSPRINTWS/service.asmx`.

## Related

- Azure DevOps Feature **AB#1299** — PrintApp modernization
- Azure DevOps Task **AB#845** — Greenville cash drawer (the incident that surfaced all of this)
- `github.com/QuickFlora/WindowServices` — contains a *different*, service-based `PrintApp`.
  **That is not the app deployed to florists.** This one is.

## ⚠ Do not run an exe from a source zip

This repository contains **source only**. Downloading a branch as a zip from GitHub gives you the
code, not a runnable build.

Until 2026-08-11 the repo also carried committed build output (`bin/Release/QuickfloraPrinting.exe`).
That binary was the **April 2020 baseline** and was never rebuilt when source changed — so a source
zip looked like a distributable build while containing six-year-old code. This caused a wasted test
cycle: a tester ran it, correctly found the new features absent, and reasonably concluded the work
had achieved nothing.

`bin/` and `obj/` are now git-ignored. **Builds are published as GitHub Releases.** Check the release
notes for the expected md5 and verify it before installing:

```powershell
Get-FileHash .\QuickfloraPrinting.exe -Algorithm MD5
```

Baseline (April 2020, no modern features): `24ecfc62920436426fb5943690cac90b`
