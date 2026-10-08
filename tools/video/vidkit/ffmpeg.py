"""The ffmpeg the pipeline uses, and the few things it is asked to do.

imageio-ffmpeg ships a static ffmpeg in the virtualenv, so nothing has to be installed system-wide.
It ships no ffprobe; `duration` reads ffmpeg's own banner instead.
"""

from __future__ import annotations

import os
import re
import subprocess
from pathlib import Path

import imageio_ffmpeg

FFMPEG = os.environ.get("CATHEDRAL_FFMPEG") or imageio_ffmpeg.get_ffmpeg_exe()


def run(*args: str | Path, quiet: bool = True) -> None:
    """Runs ffmpeg with -y, raising with its stderr on failure."""
    cmd = [FFMPEG, "-y", "-hide_banner"] + (["-loglevel", "error"] if quiet else []) + [str(a) for a in args]
    proc = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if proc.returncode != 0:
        raise RuntimeError(f"ffmpeg failed ({proc.returncode}):\n{' '.join(cmd)}\n{proc.stderr[-4000:]}")


def duration(path: str | Path) -> float:
    """A media file's duration in seconds."""
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True,
                          encoding="utf-8", errors="replace")
    m = re.search(r"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)", proc.stderr)
    if not m:
        raise RuntimeError(f"could not read the duration of {path}:\n{proc.stderr[-1500:]}")
    h, mnt, s = m.groups()
    return int(h) * 3600 + int(mnt) * 60 + float(s)


def video_size(path: str | Path) -> tuple[int, int]:
    proc = subprocess.run([FFMPEG, "-hide_banner", "-i", str(path)], capture_output=True, text=True,
                          encoding="utf-8", errors="replace")
    m = re.search(r"Video:.*?(\d{2,5})x(\d{2,5})", proc.stderr)
    if not m:
        raise RuntimeError(f"no video stream in {path}")
    return int(m.group(1)), int(m.group(2))


# Intermediate segments: near-lossless, 4:4:4, so the one-pixel dither survives until the final encode.
INTERMEDIATE = ["-c:v", "libx264", "-preset", "medium", "-crf", "10", "-pix_fmt", "yuv444p"]

# The deliverable: what every player and every site opens. A dithered picture is all high-frequency
# detail, so it is given more bits than a usual 1080p encode would need.
DELIVERY = ["-c:v", "libx264", "-preset", "medium", "-crf", "17", "-pix_fmt", "yuv420p",
            "-movflags", "+faststart"]
