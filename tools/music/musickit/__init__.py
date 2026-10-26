"""The music evaluation harness: measure what the game's composer and synthesiser produce.

Nobody working on this music by machine can hear it, so every judgement goes through a proxy. This
package is those proxies, kept honest by being run the same way on everything: the current engine,
the legacy one, hand-written exemplars, and any reference recordings dropped in `reference/`.

    score     -- the note list behind a render: the engine's own score JSON, or any MIDI file
    symbolic  -- metrics on notes: mode fit, steps and leaps, motif recurrence, harmonic rhythm, form
    acoustic  -- metrics on audio: loudness, spectral tilt, highs, stereo, swells, silence holes
    targets   -- the bands each metric should fall in, and the score against them
    sheets    -- one PNG per render: spectrogram, piano roll, self-similarity, loudness and layers
    report    -- an HTML page comparing renders, with the audio, the sheets and the metric table
"""

from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
TOOLS = ROOT / "tools" / "music"
