"""vidkit — the video pipeline for Proscribed Palimpsest.

Recordings come from the game (`--record`), voice from Piper, music from the game's own composer
(`--export-music`) rendered by `synth`, animations from manim; `build` assembles a storyboard of them
into one video in the game's look: its palette, its font, its Bayer dither.

The modules, in the order a video passes through them:

    ffmpeg    -- the bundled ffmpeg, and probing/encoding helpers
    palette   -- the game's colours and font, and a check that a colour is on brand
    dither    -- the game's post-process dither, bit for bit, applied to frames or whole videos
    tts       -- Piper voice-over, with the slight grit that fits the game
    synth     -- a small offline synthesiser for the game's MIDI
    audio     -- mixing: placing voice, clicks and music, ducking, loudness
    recording -- reading a --record directory: timeline, marks, clips, clicks
    manimkit  -- the brand scene for manim: colours, text, bar charts, graphs, diagrams
    build     -- the storyboard runner
"""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]          # the repository
TOOLS = Path(__file__).resolve().parents[1]         # tools/video
VOICES = TOOLS / "voices"
