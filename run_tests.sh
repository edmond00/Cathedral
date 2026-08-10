#!/usr/bin/env bash
#
# The whole test suite: every headless audit, then every CLI verb script.
#
#   ./run_tests.sh                 everything
#   ./run_tests.sh audits          the audits only (fast — no window, no LLM)
#   ./run_tests.sh cli             the CLI scripts only
#   ./run_tests.sh cli gather      the CLI scripts under cli/gather/
#
# Each CLI script carries its own flags on a `# FLAGS:` header line, so a test that needs a
# particular biome, room, hour or object says so in the one place somebody reading it will look.
# --playground makes every animation instant (see Config.AnimationsAreInstant), which is what keeps
# a hundred-odd scripts to a tolerable wall-clock.
#
# Exit code is the number of failures, so CI can gate on it.

set -uo pipefail
cd "$(dirname "$0")"

AUDITS=(crime mm verb building dialogue npc item outcome)
CLI_TIMEOUT=200
LOG_DIR="${TMPDIR:-/tmp}/cathedral-tests"
mkdir -p "$LOG_DIR"
LOCK="$LOG_DIR/run_tests.lock"

# ── One suite at a time, and take the game down with us ──────────────────────
#
# Two runs at once fight over the same build output and open competing game windows, so both sets of
# results are worthless. Worse, killing the runner does NOT stop the run: the shell dies, and
# `dotnet run` and Cathedral.exe carry on looping. A suite that had been "stopped" kept spawning
# instances for several minutes, and the tests started in that window were racing it.
#
# So: refuse to start if anything is already running, and trap every exit path to clean up. Because
# the guard proves no Cathedral.exe existed when we started, any that exists at exit is ours — which
# is what makes it safe to kill them all without touching a game somebody opened to play.

game_count() {
    powershell.exe -NoProfile -NonInteractive -Command \
        "@(Get-Process -Name Cathedral -ErrorAction SilentlyContinue).Count" 2>/dev/null | tr -d '\r\n '
}

cleanup() {
    local code=$?
    powershell.exe -NoProfile -NonInteractive -Command \
        "Get-Process -Name Cathedral -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1
    rm -f "$LOCK"
    exit $code
}

if [ -f "$LOCK" ] && kill -0 "$(cat "$LOCK" 2>/dev/null)" 2>/dev/null; then
    echo "run_tests.sh is already running (pid $(cat "$LOCK")). Stop it first:" >&2
    echo "  kill $(cat "$LOCK")   # then re-run; the trap takes the game processes with it" >&2
    exit 3
fi

running=$(game_count)
if [ "${running:-0}" != "0" ]; then
    echo "$running Cathedral.exe instance(s) already running — a leftover suite, or the game is open." >&2
    echo "Close them before testing; results from a shared build output are not trustworthy:" >&2
    echo "  powershell -c \"Get-Process Cathedral | Stop-Process -Force\"" >&2
    exit 3
fi

# Clear the previous run's logs. A log from a script that no longer exists outlives it, and reading
# the log directory then reports failures for tests that were not run — which cost real time to
# untangle when a green suite appeared to contain two failing scripts.
rm -f "$LOG_DIR"/cli-*.log "$LOG_DIR"/audit-*.log

echo $$ > "$LOCK"
trap cleanup EXIT INT TERM

pass=0; fail=0; failed_names=()

green() { printf '\033[32m%s\033[0m' "$1"; }
red()   { printf '\033[31m%s\033[0m' "$1"; }

record() { # name, ok, logfile
    if [ "$2" = "ok" ]; then
        pass=$((pass+1)); printf '  %s %s\n' "$(green PASS)" "$1"
    else
        fail=$((fail+1)); failed_names+=("$1|$3")
        printf '  %s %s\n' "$(red FAIL)" "$1"
    fi
}

run_audits() {
    echo "── AUDITS ──"
    for a in "${AUDITS[@]}"; do
        local log="$LOG_DIR/audit-$a.log"
        dotnet run --no-build -- "--$a-audit" > "$log" 2>&1
        # An audit reports its own findings; a clean run has no ✗ line and no FAILURE/WARNING block.
        if grep -qiE '^ *✗|--- [0-9]+ (FAILURE|WARNING)' "$log"; then
            record "$a-audit" bad "$log"
        else
            record "$a-audit" ok "$log"
        fi
    done
}

run_cli() {
    local filter="${1:-}"
    echo "── CLI VERB SCRIPTS ──"
    local scripts
    mapfile -t scripts < <(find cli -name '*.cli' | sort)
    for s in "${scripts[@]}"; do
        # A filter matches either a whole range (`cli verb`, `cli outcome`, `cli system`) or one
        # folder inside a range (`cli gather` → cli/verb/gather/, `cli item_acquisition` →
        # cli/outcome/item_acquisition/). Naming the range is the common case for a partial run;
        # naming the folder is what you type while writing one test.
        if [ -n "$filter" ] && [[ "$s" != cli/$filter/* ]] && [[ "$s" != cli/*/$filter/* ]]; then
            continue
        fi

        local flags
        flags=$(grep -m1 '^# FLAGS:' "$s" | sed 's/^# FLAGS: *//')
        if [ -z "$flags" ]; then
            echo "  $(red SKIP) $s — no '# FLAGS:' header"
            fail=$((fail+1)); failed_names+=("$s|no FLAGS header")
            continue
        fi

        local log="$LOG_DIR/$(echo "$s" | tr '/' '-').log"
        # Split the FLAGS line the way a shell would, so quoted multi-word values survive:
        # --start-area "Craft Row" is two arguments, not three.
        local -a argv
        eval "argv=($flags)"
        # --silent is added here rather than in every header: a full run launches the game a
        # hundred-odd times, and without it that is an hour of overlapping music from windows
        # nobody is watching. No script has a reason to want sound, so none of them gets a say.
        timeout $((CLI_TIMEOUT + 60)) dotnet run --no-build -- "${argv[@]}" --silent \
            --cli-timeout "$CLI_TIMEOUT" --cli-script "$s" > "$log" 2>&1
        local code=$?

        # Kill anything this script left behind before starting the next one. `timeout` kills the
        # `dotnet run` wrapper, but Cathedral.exe is a GRANDCHILD and survives it — so a script that
        # hangs and gets timed out leaves a live game holding the build output, and every test after
        # it races that. The startup guard proved none was running when the suite began, so any alive
        # now is this script's.
        powershell.exe -NoProfile -NonInteractive -Command \
            "Get-Process -Name Cathedral -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1

        # The driver's own verdict line is authoritative, not the exit code. The game can crash in
        # WinForms teardown *after* printing "Cleanup complete" — long after every assertion has been
        # decided — and gating on $? alone reports a wholly passing script as a failure. A run that
        # never printed a verdict at all did crash before finishing, and that is a failure.
        if grep -q 'all assertions passed' "$log"; then
            record "$s" ok "$log"
            if [ $code -ne 0 ]; then
                echo "         (note: exit $code after the verdict — teardown crash, see log)"
            fi
        elif grep -q 'assertions FAILED' "$log"; then
            record "$s" bad "$log"
        else
            record "$s" bad "$log (no verdict — crashed or timed out)"
        fi
    done
}

echo "Building…"
dotnet build -v q --nologo > "$LOG_DIR/build.log" 2>&1 || {
    echo "$(red 'BUILD FAILED') — see $LOG_DIR/build.log"; exit 1; }

case "${1:-all}" in
    audits) run_audits ;;
    cli)    run_cli "${2:-}" ;;
    all)    run_audits; run_cli ;;
    *)      echo "usage: $0 [all|audits|cli [verb]]"; exit 2 ;;
esac

echo
echo "── $((pass+fail)) test(s): $(green "$pass passed"), $([ $fail -gt 0 ] && red "$fail failed" || echo '0 failed') ──"
if [ $fail -gt 0 ]; then
    echo
    echo "Failures (log path after the pipe):"
    for f in "${failed_names[@]}"; do echo "  ${f%%|*}"; echo "      ${f#*|}"; done
fi
exit $fail
