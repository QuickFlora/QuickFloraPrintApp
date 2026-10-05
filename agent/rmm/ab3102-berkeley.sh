#!/bin/bash
# AB#3102 - Berkeley Florist Supply: every Tactical RMM agent under client "Berkeley Florist Supply"
#   1. gets PrinterWatch + the Print Monitor ingest key in C:\ProgramData\PrinterWatch
#      (folder: SYSTEM/Administrators full, Users read; key: SYSTEM/Administrators only)
#   2. gets the RMM script check "QuickFlora - PrinterWatch check-in (AB#3102)" (script 131), every 15 min
#   3. runs its checks once, so the first check-in arrives straight away
# Same setup as Greenville FRONTFLOWER. No Windows scheduled task, no change to the print app,
# printers or Windows settings, no reboot. Offline PCs are skipped - just run this again later.
# Safe to run more than once: a PC that already has the check is not given a second one.
# The key goes from its file on this Mac to the private deploy bucket (5-minute links) and is deleted at the end.
set -euo pipefail
A=https://api.quickflora.support
KEYHDR="X-API-KEY: $(cat ~/.config/claude-code/tactical-rmm-api-key)"
P=s3://quickflora-ab733-deploy-357589302185/ab3102
PW_COMMIT=45324cb29dd4989fcdd351e819645d7adb62d696
SCRIPT_ID=131
CLIENT="Berkeley Florist Supply"
trap 'aws s3 rm --quiet "$P/ingest.key" 2>/dev/null || true' EXIT

echo "1/4 Finding $CLIENT agents in Tactical RMM..."
AGENTS=$(curl -s -m 30 -H "$KEYHDR" "$A/agents/?detail=false" | python3 -c '
import json,sys
for a in json.load(sys.stdin):
    if (a.get("client") or "").strip().lower() == sys.argv[1].lower():
        print(a["agent_id"] + "|" + a["hostname"])' "$CLIENT")
if [ -z "$AGENTS" ]; then echo "No agents found under $CLIENT - nothing to do."; exit 0; fi
echo "$AGENTS" | cut -d'|' -f2 | sed 's/^/   /'

echo "2/4 Staging the ingest key (private bucket)..."
aws s3 cp --quiet ~/.config/claude-code/print-monitor-ingest-key "$P/ingest.key"

echo "3/4 Setting up each PC..."
DONE=""; SKIPPED=""
while IFS='|' read -r AID HOST; do
  URL=$(aws s3 presign "$P/ingest.key" --expires-in 300)
  PS=$(cat <<EOF
\$ErrorActionPreference='Stop'
try {
  [Net.ServicePointManager]::SecurityProtocol=[Net.SecurityProtocolType]::Tls12
  \$d='C:\\ProgramData\\PrinterWatch'; New-Item -ItemType Directory -Force \$d | Out-Null
  icacls \$d /inheritance:r /grant:r 'SYSTEM:(OI)(CI)F' 'Administrators:(OI)(CI)F' 'Users:(OI)(CI)RX' | Out-Null
  \$k=Join-Path \$d 'ingest.key'
  Invoke-WebRequest -UseBasicParsing -Uri '$URL' -OutFile \$k
  icacls \$k /inheritance:r /grant:r 'SYSTEM:F' 'Administrators:F' | Out-Null
  Invoke-WebRequest -UseBasicParsing 'https://raw.githubusercontent.com/QuickFlora/QuickFloraPrintApp/$PW_COMMIT/agent/PrinterWatch.ps1' -OutFile (Join-Path \$d 'PrinterWatch.ps1')
  'READY'
} catch { 'FAILED: ' + \$_.Exception.Message }
EOF
)
  OUT=$(python3 -c 'import json,sys; print(json.dumps({"shell":"powershell","cmd":sys.stdin.read(),"timeout":90,"run_as_user":False,"custom_shell":None}))' <<<"$PS" \
        | curl -s -m 120 -H "$KEYHDR" -H 'Content-Type: application/json' -X POST "$A/agents/$AID/cmd/" --data @- \
        | python3 -c 'import json,sys; r=sys.stdin.read(); print((json.loads(r) if r.startswith("\"") else r).strip()[:200])')
  if [ "$OUT" != "READY" ]; then
    echo "   $HOST: SKIPPED - $OUT"; SKIPPED="$SKIPPED $HOST"; continue
  fi
  HAS=$(curl -s -m 30 -H "$KEYHDR" "$A/agents/$AID/checks/" | python3 -c '
import json,sys; print(any((c.get("script") or {}).get("id", c.get("script")) == int(sys.argv[1]) if isinstance(c.get("script"), dict) else c.get("script") == int(sys.argv[1]) for c in json.load(sys.stdin)))' "$SCRIPT_ID")
  if [ "$HAS" != "True" ]; then
    curl -s -m 30 -H "$KEYHDR" -H 'Content-Type: application/json' -X POST "$A/checks/" \
      --data "{\"agent\":\"$AID\",\"check_type\":\"script\",\"script\":$SCRIPT_ID,\"script_args\":[],\"env_vars\":[],\"info_return_codes\":[],\"warning_return_codes\":[2],\"timeout\":120,\"run_interval\":900,\"fails_b4_alert\":2}" >/dev/null
  fi
  curl -s -m 30 -H "$KEYHDR" -X POST "$A/checks/$AID/run/" >/dev/null
  echo "   $HOST: done"; DONE="$DONE $HOST"
done <<<"$AGENTS"

echo "4/4 Removing the key from the bucket..."
aws s3 rm --quiet "$P/ingest.key"
echo
echo "Set up:  ${DONE:- none}"
echo "Skipped: ${SKIPPED:- none}"
echo "Tell Claude - it will check that each PC is checking in to the Print Monitor."
