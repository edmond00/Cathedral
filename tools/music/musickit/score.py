"""The notes behind a render.

Two sources. The engine writes its own score beside every offline render (`--render-music out.wav`
writes `out.score.json`), and that carries what a MIDI file cannot: which part a note belongs to, the
section and chord it sits in, the mode and tonic. Anything else (the legacy engine's `--export-music`,
a hand-written exemplar, a transcription) comes in as MIDI, and the parts are read off the channels.
"""

from __future__ import annotations

import json
from dataclasses import dataclass, field
from pathlib import Path

import mido


@dataclass
class Note:
    t: float          # onset, seconds
    d: float          # duration, seconds
    p: int            # MIDI pitch
    v: float          # velocity 0..1
    part: str         # melody, counter, drone, pad, ostinato, perc, sfx, ...
    inst: str = ""


@dataclass
class Score:
    notes: list[Note]
    seconds: float
    sections: list[dict] = field(default_factory=list)   # {t, label, piece, layers}
    chords: list[dict] = field(default_factory=list)     # {t, d, root, pcs}
    pieces: list[dict] = field(default_factory=list)     # {t, mode, tonic, bpm, meter, form}
    meta: dict = field(default_factory=dict)

    def part(self, name: str) -> list[Note]:
        return [n for n in self.notes if n.part == name]

    @property
    def parts(self) -> list[str]:
        return sorted({n.part for n in self.notes})

    def melody(self) -> list[Note]:
        """The lead line: the part called melody, or failing that the top voice of everything pitched."""
        mel = self.part("melody")
        pitched = sorted(mel or (n for n in self.notes if n.part not in ("perc", "sfx", "drone")),
                         key=lambda n: (n.t, -n.p))
        sky: list[Note] = []
        for n in pitched:
            if sky and abs(sky[-1].t - n.t) < 0.03:
                continue  # same onset, lower note
            sky.append(n)
        return sky


def load(path: str | Path) -> Score:
    path = Path(path)
    if path.suffix == ".json":
        return from_json(path)
    return from_midi(path)


def from_json(path: Path) -> Score:
    j = json.loads(path.read_text())
    notes = [Note(n["t"], n["d"], n["p"], n["v"], n["part"], n.get("inst", "")) for n in j["notes"]]
    return Score(notes, j["meta"].get("seconds", max((n.t + n.d for n in notes), default=0.0)),
                 j.get("sections", []), j.get("chords", []), j.get("pieces", []), j.get("meta", {}))


# The legacy AmbianceEngine's channels (ProceduralMidiComposer.GetChannel, and the filters' 5/6/9).
LEGACY_CHANNELS = {0: "drone", 1: "melody", 2: "counter", 3: "ostinato", 4: "pad",
                   5: "fight", 6: "fight", 9: "perc", 14: "sfx"}


def from_midi(path: Path, channels: dict[int, str] | None = None) -> Score:
    channels = channels or LEGACY_CHANNELS
    mid = mido.MidiFile(str(path))
    t = 0.0
    open_: dict[tuple[int, int], tuple[float, int]] = {}
    program = [0] * 16
    notes: list[Note] = []
    for msg in mid:
        t += msg.time
        if msg.type == "program_change":
            program[msg.channel] = msg.program
        elif msg.type == "note_on" and msg.velocity > 0:
            key = (msg.channel, msg.note)
            if key in open_:  # retrigger: close the old one
                st, vel = open_.pop(key)
                notes.append(Note(st, max(0.02, t - st), msg.note, vel / 127, channels.get(msg.channel, f"ch{msg.channel}"), f"gm{program[msg.channel]}"))
            open_[key] = (t, msg.velocity)
        elif msg.type in ("note_off", "note_on"):
            key = (msg.channel, msg.note)
            if key in open_:
                st, vel = open_.pop(key)
                part = channels.get(msg.channel, f"ch{msg.channel}")
                notes.append(Note(st, max(0.02, t - st), msg.note, vel / 127, part, f"gm{program[msg.channel]}"))
    for (ch, p), (st, vel) in open_.items():
        notes.append(Note(st, max(0.02, t - st), p, vel / 127, channels.get(ch, f"ch{ch}"), f"gm{program[ch]}"))
    notes.sort(key=lambda n: n.t)
    return Score(notes, t, meta={"source": str(path)})
