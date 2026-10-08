# Installing QuickFlora Print App on shop PCs with Tactical RMM

For Rajesh / AB#3171. Tested 2026-10-08 on the Lenovo test PC, which had a real 3.4.0 shop install:
3.4.0 → 4.0.2 → rolled back to 3.4.0 → 4.0.2 again, all OK, with the shop's Config.txt unchanged.

## Per PC, in this order

1. **install-printapp.ps1**: Tactical RMM → the PC → Run Script → **run as SYSTEM**, timeout 300 s.
   Fill in the three placeholders at the top (`__SETUP_URL__`, `__SETUP_MD5__`, `__VERSION__`)
   with the values Alex/Claude give you for the release.
   Read the last line:
   - `RESULT: OK` → go to step 2.
   - `STOP: ...` → nothing was changed on the PC. Send the output on the ADO case.
   - `RESULT: PROBLEM` → run step 3 (rollback), then post the output on the ADO case.
2. **start-printapp.ps1**: Run Script → **run as the logged-in user** (not SYSTEM), timeout 90 s.
   Fill in `__VERSION__` with the same value. `RESULT: OK` = the app is running the new version and
   will start by itself after a reboot.
3. **Only if something is wrong**: **rollback-printapp.ps1** as SYSTEM, then start-printapp.ps1 as the user
   with `__VERSION__` set to the old version (shown as "installed version" in step 1's output).

## What the install script protects

- It stops without changing anything if the print app on the PC runs from a folder other than
  `C:\QFPrintApp\QuickfloraPrinting`, if there is no `Config.txt`, or if the download's MD5 is wrong.
- It backs up the app and `Config.txt` to `C:\QFPrintApp\backup-<old version>-<date>` first.
- The installer never overwrites `Config.txt` (company, terminal, printers stay as they are).

## After installing

Check the shop on POSN → Order Print Logs: the PC should show **Connected**, and after the update its
printers are listed. Print App 4.0.2 also reports any page that did not print, with the reason.
