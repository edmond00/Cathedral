"""Entry point for the video pipeline. Run it with the video virtualenv's Python:

    tools/video/.venv/Scripts/python tools/video/make_video.py build tools/video/productions/cities/storyboard.json
    tools/video/.venv/Scripts/python tools/video/make_video.py record my.cli build/rec_x --flags "--playground --seed 42"
    tools/video/.venv/Scripts/python tools/video/make_video.py inspect build/rec_x
    tools/video/.venv/Scripts/python tools/video/make_video.py say "A line to audition." out.wav --grit 0.4

See .claude/skills/video/SKILL.md.
"""

import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))

# Game text carries em dashes and box glyphs; a Windows console on a legacy code page (cp932 here)
# cannot print them and would kill the run at its last line.
for stream in (sys.stdout, sys.stderr):
    try:
        stream.reconfigure(encoding="utf-8", errors="replace")
    except AttributeError:
        pass

from vidkit.build import main  # noqa: E402

if __name__ == "__main__":
    sys.exit(main())
