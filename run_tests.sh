#!/usr/bin/env bash
#
# The whole test suite: every headless audit, then every CLI verb script.
#
#   ./run_tests.sh                 everything
#   ./run_tests.sh audits          the audits only (fast — no window, no LLM)
#   ./run_tests.sh cli             the CLI scripts only
#   ./run_tests.sh cli gather      the CLI scripts under cli/gather/
#   ./run_tests.sh cli gather cli/system/focus_turned_away.cli
#                                  several filters at once; a script path runs that script alone
#
# The full run is the release gate, not the check for every change: while working on one thing, run
# the audits and the scripts that cover it (see CLAUDE.md).
#
# Each CLI script carries its own flags on a `# FLAGS:` header line, so a test that needs a
# particular biome, room, hour or object says so in the one place somebody reading it will look.
# --playground makes every animation instant (see Config.AnimationsAreInstant), which is what keeps
# a hundred-odd scripts to a tolerable wall-clock.
#
# Tests run in parallel, JOBS at a time (default: half the logical cores, at least one):
# `JOBS=1 ./run_tests.sh` runs them one after another. Every instance is independent — each gets its
# own --log-file, the scripts that save carry their own --save-path, and --playground starts no LLM
# server — so nothing they write is shared. The audits go into the same pool first, so the long verb
# audit runs alongside the scripts instead of ahead of them.
#
# Exit code is the number of failures, so CI can gate on it.

set -uo pipefail
cd "$(dirname "$0")"

# Slowest first, so the verb audit is not the last thing still running.
AUDITS=(verb will building biome crime mm dialogue npc item outcome save world-variant history demo forbidden-words)
# Overridable from the environment for slow machines (Mesa's software GL under Xvfb, a CI runner):
# `CLI_TIMEOUT=400 ./run_tests.sh cli`. The default is what a developer's machine needs.
CLI_TIMEOUT="${CLI_TIMEOUT:-200}"
# Extra flags for every CLI script, from the environment — e.g. a container without the GloVe file:
# `CLI_EXTRA_FLAGS=--no-embeddings ./run_tests.sh cli`. Empty by default.
read -r -a CLI_EXTRA <<< "${CLI_EXTRA_FLAGS:-}"
JOBS="${JOBS:-$(( $(nproc 2>/dev/null || echo 2) / 2 ))}"
[ "$JOBS" -ge 1 ] 2>/dev/null || JOBS=1
LOG_DIR="${TMPDIR:-/tmp}/cathedral-tests"
mkdir -p "$LOG_DIR"
LOCK="$LOG_DIR/run_tests.lock"

# ── One suite at a time, and take the game down with us ──────────────────────
#
# Two runs at once fight over the same build output and open competing game windows, so both sets of
# results are worthless. Worse, killing the runner does NOT stop the run: the shell dies, and the
# game processes carry on. A suite that had been "stopped" kept spawning instances for several
# minutes, and the tests started in that window were racing it.
#
# So: refuse to start if anything is already running, and trap every exit path to clean up. Because
# the guard proves no Cathedral.exe existed when we started, any that exists at exit is ours — which
# is what makes it safe to kill them all without touching a game somebody opened to play.
#
# The game process is "Cathedral" on both platforms (Cathedral.exe on Windows, the Cathedral apphost
# on Linux). On Windows (Git Bash / MSYS) it is found through PowerShell, as it always was; anywhere
# else through pgrep/pkill, matching the process name exactly so nothing merely containing the word
# is touched. Note a WSL shell takes the second path: the suite it runs builds a Linux binary.

case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*) ON_WINDOWS=1 ;;
    *)                    ON_WINDOWS=0 ;;
esac

game_count() {
    if [ $ON_WINDOWS = 1 ]; then
        powershell.exe -NoProfile -NonInteractive -Command \
            "@(Get-Process -Name Cathedral -ErrorAction SilentlyContinue).Count" 2>/dev/null | tr -d '\r\n '
    else
        pgrep -x Cathedral | wc -l | tr -d ' '
    fi
}

kill_games() {
    if [ $ON_WINDOWS = 1 ]; then
        powershell.exe -NoProfile -NonInteractive -Command \
            "Get-Process -Name Cathedral -ErrorAction SilentlyContinue | Stop-Process -Force" >/dev/null 2>&1
    else
        pkill -KILL -x Cathedral >/dev/null 2>&1
    fi
}

cleanup() {
    local code=$?
    # Stop queueing first, then take down whatever is still running.
    local p
    for p in $(jobs -p); do kill "$p" 2>/dev/null; done
    kill_games
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
    echo "$running Cathedral instance(s) already running — a leftover suite, or the game is open." >&2
    echo "Close them before testing; results from a shared build output are not trustworthy:" >&2
    if [ $ON_WINDOWS = 1 ]; then
        echo "  powershell -c \"Get-Process Cathedral | Stop-Process -Force\"" >&2
    else
        echo "  pkill -x Cathedral" >&2
    fi
    exit 3
fi

# Clear the previous run's logs. A log from a script that no longer exists outlives it, and reading
# the log directory then reports failures for tests that were not run — which cost real time to
# untangle when a green suite appeared to contain two failing scripts.
rm -rf "$LOG_DIR"/cli-*.log "$LOG_DIR"/audit-*.log "$LOG_DIR"/game "$LOG_DIR"/results
mkdir -p "$LOG_DIR/game" "$LOG_DIR/results"

echo $$ > "$LOCK"
trap cleanup EXIT INT TERM

green() { printf '\033[32m%s\033[0m' "$1"; }
red()   { printf '\033[31m%s\033[0m' "$1"; }

# Tests finish in any order, so each one writes its verdict to a file of its own and prints its own
# line; the totals are read back from the files at the end.
record() { # name, ok, detail
    local f="$LOG_DIR/results/$(echo "$1" | tr '/' '-')"
    if [ "$2" = "ok" ]; then
        echo "ok" > "$f"; printf '  %s %s\n' "$(green PASS)" "$1"
    else
        printf 'bad\n%s|%s\n' "$1" "$3" > "$f"; printf '  %s %s\n' "$(red FAIL)" "$1"
    fi
}

# Waits until fewer than JOBS tests are running.
throttle() {
    while [ "$(jobs -rp | wc -l)" -ge "$JOBS" ]; do wait -n 2>/dev/null || true; done
}

run_audit() { # name
    local a="$1" log="$LOG_DIR/audit-$1.log"
    "$GAME" "--$a-audit" --log-file "$LOG_DIR/game/audit-$a.txt" > "$log" 2>&1
    # An audit reports its own findings; a clean run has no ✗ line and no FAILURE/WARNING block.
    if grep -qiE '^ *✗|--- [0-9]+ (FAILURE|WARNING)' "$log"; then
        record "$a-audit" bad "$log"
    else
        record "$a-audit" ok "$log"
    fi
}

queue_audits() {
    echo "── AUDITS ──"
    for a in "${AUDITS[@]}"; do
        throttle
        run_audit "$a" &
    done
}

# True when script $1 is selected by any of the filters that follow it (none selects everything).
# A filter matches a whole range (`verb`, `outcome`, `system`), one folder inside a range (`gather` →
# cli/verb/gather/, `item_acquisition` → cli/outcome/item_acquisition/), or one script by its path
# (`cli/system/focus_turned_away.cli`, with or without the `cli/` and the `.cli`). Naming a range or a
# folder is the common partial run; naming scripts is the targeted run for one change.
selected() {
    local s="$1"; shift
    [ $# -eq 0 ] && return 0
    local f
    for f in "$@"; do
        f="${f#cli/}"; f="${f%.cli}"
        [[ "$s" == cli/$f/* || "$s" == cli/*/$f/* || "$s" == "cli/$f.cli" ]] && return 0
    done
    return 1
}

run_script() { # script, flags
    local s="$1" flags="$2"
    local name; name=$(echo "$s" | tr '/' '-')
    local log="$LOG_DIR/$name.log"
    # Split the FLAGS line the way a shell would, so quoted multi-word values survive:
    # --start-area "Craft Row" is two arguments, not three.
    local -a argv
    eval "argv=($flags)"
    # --hidden for the same reason as --silent: a full run is a hundred-odd windows appearing
    # on screen, each taking the keyboard focus off whatever else is being done. The window and
    # its GL context are still created, so nothing about what is under test changes.
    #
    # --silent is added here rather than in every header: a full run launches the game a
    # hundred-odd times, and without it that is an hour of overlapping music from windows
    # nobody is watching. No script has a reason to want sound, so none of them gets a say.
    #
    # --allow-reentry likewise. The game refuses a world-map click on the vertex you are already
    # standing on — arriving somewhere opens it, so a visit costs a journey — but every script
    # gets into a location with `travel here`, and the spawn vertex is the only one it can name
    # without knowing what the seed generated. No script is testing that refusal.
    #
    # --log-file gives the run a log of its own: log.txt is opened for exclusive writing, so with
    # several instances at once all but one would run unlogged — and the developer's own log.txt
    # would be overwritten by whichever test happened to start first.
    #
    # The game is run directly, not through `dotnet run`: that re-evaluated the project on every
    # launch, and it put a wrapper between `timeout` and the game, so a timed-out script left its
    # Cathedral.exe running.
    timeout -k 10 $((CLI_TIMEOUT + 60)) "$GAME" "${argv[@]}" --silent --hidden --allow-reentry \
        --log-file "$LOG_DIR/game/$name.txt" \
        ${CLI_EXTRA[@]+"${CLI_EXTRA[@]}"} --cli-timeout "$CLI_TIMEOUT" --cli-script "$s" > "$log" 2>&1
    local code=$?

    # The driver's own verdict line is authoritative, not the exit code. The game can crash in
    # WinForms teardown *after* printing "Cleanup complete" — long after every assertion has been
    # decided — and gating on $? alone reports a wholly passing script as a failure. A run that
    # never printed a verdict at all did crash before finishing, and that is a failure.
    if grep -q 'all assertions passed' "$log"; then
        record "$s" ok "$log"
        if [ $code -ne 0 ]; then
            echo "         (note: $s exited $code after the verdict — teardown crash, see log)"
        fi
    elif grep -q 'assertions FAILED' "$log"; then
        record "$s" bad "$log"
    else
        record "$s" bad "$log (no verdict — crashed or timed out)"
    fi
}

queue_cli() {
    echo "── CLI SCRIPTS ──"
    local scripts
    mapfile -t scripts < <(find cli -name '*.cli' | sort)
    for s in "${scripts[@]}"; do
        selected "$s" "$@" || continue

        local flags
        flags=$(grep -m1 '^# FLAGS:' "$s" | sed 's/^# FLAGS: *//')
        if [ -z "$flags" ]; then
            echo "  $(red SKIP) $s — no '# FLAGS:' header"
            record "$s" bad "no FLAGS header" > /dev/null
            continue
        fi
        throttle
        run_script "$s" "$flags" &
    done
}

echo "Building…"
dotnet build -v q --nologo > "$LOG_DIR/build.log" 2>&1 || {
    echo "$(red 'BUILD FAILED') — see $LOG_DIR/build.log"; exit 1; }

GAME=$(ls bin/Debug/net8.0*/Cathedral.exe bin/Debug/net8.0*/Cathedral 2>/dev/null | head -1)
[ -n "$GAME" ] || { echo "$(red 'NO GAME BINARY') under bin/Debug — see $LOG_DIR/build.log"; exit 1; }
echo "Running $JOBS at a time."

# The parsed word vectors are cached on first load (see WordEmbedding), but a script quits before the
# load finishes, so the copy is made here, once, before the pool starts. Seconds when it already exists.
"$GAME" --playground --seed 42 --silent --hidden --log-file "$LOG_DIR/game/warm-cache.txt" \
    --cli-timeout 240 --cli-script tests/warm_cache.cli > "$LOG_DIR/warm-cache.log" 2>&1

case "${1:-all}" in
    audits) queue_audits ;;
    cli)    shift; queue_cli "$@" ;;
    all)    queue_audits; queue_cli ;;
    *)      echo "usage: $0 [all|audits|cli [range|folder|script ...]]"; exit 2 ;;
esac
wait

pass=0; fail=0; failed_names=()
for f in "$LOG_DIR"/results/*; do
    [ -e "$f" ] || continue
    if [ "$(head -1 "$f")" = "ok" ]; then
        pass=$((pass+1))
    else
        fail=$((fail+1)); failed_names+=("$(sed -n 2p "$f")")
    fi
done

echo
echo "── $((pass+fail)) test(s): $(green "$pass passed"), $([ $fail -gt 0 ] && red "$fail failed" || echo '0 failed') ──"
if [ $fail -gt 0 ]; then
    echo
    echo "Failures (log path after the pipe):"
    for f in "${failed_names[@]}"; do echo "  ${f%%|*}"; echo "      ${f#*|}"; done
fi
exit $fail
