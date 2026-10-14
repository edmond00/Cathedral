#!/usr/bin/env bash
# The genuine save → quit → relaunch → Continue check.
#
# Deliberately NOT wired into run_tests.sh: that runner launches the game once per script, so a test
# spanning two processes cannot be written as a `.cli` file. Everything either side of the process
# boundary is covered in-process by cli/system/save_resume.cli and save_roundtrip.cli; this covers the
# boundary itself — that a save written by one process is read by the next, and that Continue resumes
# the run rather than starting one.
#
#   ./tests/save_reload.sh
#
# Exit code is the number of failures.

set -uo pipefail
cd "$(dirname "$0")/.."

SAVE_DIR="${TMPDIR:-/tmp}/cathedral-save-reload"
SAVE="$SAVE_DIR/save.json"
rm -rf "$SAVE_DIR"; mkdir -p "$SAVE_DIR"

# --seed pins the world, which is what lets the second launch load the first's save at all: a save is
# refused if its seed is not the one the process booted with.
COMMON="--playground --skip-childhood --debug --seed 4242 --no-encounters --silent --allow-reentry
        --location-type test --location-id 0 --npc-static --period noon --save-path $SAVE"

fail=0
green() { printf '\033[32m%s\033[0m' "$1"; }
red()   { printf '\033[31m%s\033[0m' "$1"; }
check() { if [ "$1" = "$2" ]; then echo "  $(green PASS) $3"; else echo "  $(red FAIL) $3 — expected '$2', got '$1'"; fail=$((fail+1)); fi; }

cleanup() { pkill -f "Cathedral" 2>/dev/null || true; }
trap cleanup EXIT INT TERM

# ── Launch 1: play into the world, visit a location, quit ────────────────────
echo "── launch 1: play and quit ──"
cat > "$SAVE_DIR/play.cli" <<'EOF'
wait
click menu New
wait mode ProtagonistCreation 20
click continue
wait mode WorldView 45
travel here
wait mode LocationInteraction 45
advance 12 90
click button
wait mode WorldView 45
inspect save
quit
EOF

OUT1=$(timeout 300 dotnet run -- $COMMON --cli-timeout 150 --cli-script "$SAVE_DIR/play.cli" 2>&1 | grep '^\[cli\]')
DAYS1=$(echo "$OUT1" | grep -o 'save days=[0-9.]*' | tail -1 | cut -d= -f2)
VERTEX1=$(echo "$OUT1" | grep -o 'save vertex=[0-9]*' | tail -1 | cut -d= -f2)
NAME1=$(echo "$OUT1" | grep -o 'save name=.*' | tail -1 | cut -d= -f2)

[ -f "$SAVE" ] && check "yes" "yes" "a save file exists after quitting" \
                || check "no"  "yes" "a save file exists after quitting"
echo "  (day $DAYS1, vertex $VERTEX1, $NAME1)"

# ── Launch 2: a fresh process must offer Continue, and resume the same run ───
echo "── launch 2: relaunch and continue ──"
cat > "$SAVE_DIR/resume.cli" <<'EOF'
wait
inspect menu
expect-state menu Continue enabled
click menu Continue
wait mode WorldView 45
inspect save
quit
EOF

OUT2=$(timeout 300 dotnet run -- $COMMON --cli-timeout 150 --cli-script "$SAVE_DIR/resume.cli" 2>&1 | grep '^\[cli\]')
DAYS2=$(echo "$OUT2" | grep -o 'save days=[0-9.]*' | tail -1 | cut -d= -f2)
VERTEX2=$(echo "$OUT2" | grep -o 'save vertex=[0-9]*' | tail -1 | cut -d= -f2)
NAME2=$(echo "$OUT2" | grep -o 'save name=.*' | tail -1 | cut -d= -f2)

echo "$OUT2" | grep -q 'assertions FAILED' && { echo "  $(red FAIL) Continue was not offered"; fail=$((fail+1)); }

# The run is the SAME run: same character, same day, same place. A new game would have re-rolled all
# three, so these three equalities are the whole assertion.
check "$NAME2"   "$NAME1"   "the same protagonist came back"
check "$DAYS2"   "$DAYS1"   "the clock resumed where it stopped"
check "$VERTEX2" "$VERTEX1" "the avatar stood where it was left"

# ── Launch 3: New spends the save ───────────────────────────────────────────
echo "── launch 3: New erases it ──"
cat > "$SAVE_DIR/new.cli" <<'EOF'
wait
click menu New
wait mode ProtagonistCreation 20
expect-state savefile save none
quit
EOF

OUT3=$(timeout 300 dotnet run -- $COMMON --cli-timeout 120 --cli-script "$SAVE_DIR/new.cli" 2>&1 | grep '^\[cli\]')
echo "$OUT3" | grep -q 'all assertions passed' \
  && echo "  $(green PASS) New erased the save" \
  || { echo "  $(red FAIL) New did not erase the save"; fail=$((fail+1)); }

echo
[ $fail -eq 0 ] && echo "── $(green 'all checks passed') ──" || echo "── $(red "$fail check(s) failed") ──"
exit $fail
