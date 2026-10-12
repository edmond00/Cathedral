"""Live takes: play the game a batch of commands at a time, reading the screen in between.

A .cli script is written before the take, but with the real model the take is only known as it
happens — what the protagonist noticed, which goals the thinking offered, whether the acting mind
agreed. A live take lets the agent decide each next move from the screen, the way a player does:

    make_video.py live build/rec_x --flags "--skip-childhood --seed 42 --start-at city"   (run in background)
    make_video.py send build/rec_x "wait mode MainMenu 900" "click menu New" ...
    make_video.py send build/rec_x regions
    make_video.py send build/rec_x "click keyword 1" "choose 0" "regions"
    make_video.py send build/rec_x quit

The game reads `commands.txt` in the recording directory as its stdin (this module feeds it), and
`send` appends to it, then blocks until the game has worked through everything sent and prints the
driver's answers. Each batch ends with a hidden `mark __sync_<n>`, which is how `send` knows the batch
is done; the cutter ignores marks that start with "__".
"""

from __future__ import annotations

import os
import subprocess
import sys
import threading
import time
from pathlib import Path

from . import ROOT


def run(out_dir: Path, flags: str, timeout: int = 7200, gpu: bool = False) -> int:
    """Starts the game recording into `out_dir`, fed from out_dir/commands.txt. Blocks until it exits."""
    import shlex
    out_dir.mkdir(parents=True, exist_ok=True)
    commands = out_dir / "commands.txt"
    commands.write_text("", encoding="utf-8")
    args = shlex.split(flags, posix=True) + ["--cli-timeout", str(timeout), "--record", str(out_dir)]
    if gpu:
        args.append("--gpu")
    cmd = ["dotnet", "run", "--no-build", "--project", str(ROOT / "Cathedral.csproj"), "--"] + args
    with open(out_dir / "game.log", "w", encoding="utf-8", errors="replace") as log:
        proc = subprocess.Popen(cmd, cwd=ROOT, stdin=subprocess.PIPE, stdout=log, stderr=subprocess.STDOUT,
                                text=True, encoding="utf-8")

        def feed():
            sent = 0
            while proc.poll() is None:
                lines = commands.read_text(encoding="utf-8").splitlines()
                for line in lines[sent:]:
                    proc.stdin.write(line + "\n")
                    proc.stdin.flush()
                sent = len(lines)
                time.sleep(0.25)

        threading.Thread(target=feed, daemon=True).start()
        print(f"[video] live take recording into {out_dir} — send commands with `make_video.py send {out_dir} ...`",
              flush=True)
        return proc.wait()


def send(out_dir: Path, lines: list[str], timeout: float = 1800) -> str:
    """Appends `lines` to the take, waits until the game has worked through them, and returns the
    driver's output for this batch (the `[cli]` lines, minus the screen dumps' frame rows)."""
    log = out_dir / "game.log"
    commands = out_dir / "commands.txt"
    if not commands.exists():
        raise FileNotFoundError(f"no live take in {out_dir} (start one with `make_video.py live`)")
    n = len(commands.read_text(encoding="utf-8").splitlines())
    sync = f"__sync_{n}"
    start = log.stat().st_size if log.exists() else 0
    with open(commands, "a", encoding="utf-8") as f:
        for line in lines:
            f.write(line + "\n")
        f.write(f"mark {sync}\n")

    deadline = time.time() + timeout
    while time.time() < deadline:
        text = log.read_bytes()[start:].decode("utf-8", errors="replace")
        if f'mark "{sync}"' in text or "[record] done" in text:
            out = [l for l in text.splitlines() if l.startswith("[cli]") and "__sync_" not in l]
            return "\n".join(out)
        time.sleep(0.5)
    return f"(timed out after {timeout:.0f}s waiting for the game to finish this batch)"
