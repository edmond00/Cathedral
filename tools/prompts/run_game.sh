#!/usr/bin/env bash
# Plays one .cli script against the real model (CPU, headless), for collecting the prompt corpus.
#   bash tools/prompts/run_game.sh <script.cli> <out-dir> [game flags...]
# The game writes logs/llm_session_*/ (rewrite_inputs.jsonl, judge_inputs.jsonl, per-request dirs);
# this copies that session directory into <out-dir>/session and keeps the [cli] transcript.
set -uo pipefail
cd "$(dirname "$0")/../.."
SCRIPT=$1; OUT=$2; shift 2
mkdir -p "$OUT"
before=$(ls -d logs/llm_session_* 2>/dev/null | sort | tail -1)
xvfb-run -a -s "-screen 0 1280x1024x24" env ALSOFT_DRIVERS=null \
    bin/Debug/net8.0/Cathedral --silent --hidden --allow-reentry --no-encounters --cli-timeout 7000 \
    --cli-script "$SCRIPT" --log-file "$OUT/game.log" "$@" > "$OUT/stdout.txt" 2>&1
code=$?
grep '^\[cli\]' "$OUT/stdout.txt" > "$OUT/cli.txt"
after=$(ls -d logs/llm_session_* 2>/dev/null | sort | tail -1)
[ -n "$after" ] && [ "$after" != "$before" ] && cp -r "$after" "$OUT/session"
echo "exit=$code  $(grep -c FAIL "$OUT/cli.txt") FAIL lines  session=${after:-none}"
