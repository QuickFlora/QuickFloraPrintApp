# PATCHLOG - QuickFloraPrintApp

`date · AB#xxxx · client (CompanyID) · WEB/DB · R-level · downtime · patched by · rollback`

- 2026-10-18 · AB#3958 · QuickfloraDemo / LENOVO-i5 (Lenovo test PC only) · WEB (print app 6.1.0 installed over 6.0.0 in C:\QFPrintApp\QuickfloraPrinting, pointed at WEB1TEST) · R1 · app restart on that PC · Claude for Alex · C:\QFPrintApp\backup-6.0.0.0-AB3958test-20261018-164430
- 2026-10-18 · AB#3958 · QuickfloraDemo / LENOVO-i5 (Lenovo test PC only) · WEB (print app 6.1.1, Warm push handler, built on the PC with MSBuild from the branch zip in C:\QFPrintApp\v611-build) · R1 · app restart on that PC · Claude for Alex · C:\QFPrintApp\backup-6.1.0.0-AB3958test-20261018-1729
- 2026-10-19 · AB#3958 · QuickfloraDemo (staging only) · WEB on WEB1TEST i-0367713c255aeb175 (staging print service NewPOSQFPrintWS App_Code Service.vb + clsAppPrinting.vb: report/receipt URLs prod -> web1test; QFReports_STG WTReportPrint/EnclosureCardReportPrint: QR image falls back to barcode; hosts 127.0.0.1 web1test.quickflora-dev.com; new D:\WebApps\QuickFloraFrontEnd\PDF + IIS vdirs /PDF and /FAX; placeholder D:\WebApps\QFreports\images\tiptonqr.png) · R1 · none · Claude for Alex · restore *.20261019-1417.bak (4 .vb + hosts), remove /PDF and /FAX vdirs
