"""Reading a `--record` directory.

    game.mkv        the footage, real time, at manifest["fps"]
    timeline.jsonl  one event per line, `t` in seconds on the footage's clock
    music.mid       what the game's composer played during the run
    sfx/click.wav   the game's own click
    manifest.json   size, fps, duration, the command line

Timeline event types: start, end, cmd (every command line), mode (from/to), move (pointer set off for
x,y,label), click (x,y,label,cmd), key, wheel, sfx (the game's own UI events), mark (label),
clip-begin / clip-end (name), note (text), hold, cursor, refused (why).
"""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path


@dataclass
class Recording:
    path: Path
    manifest: dict
    events: list[dict]

    @property
    def video(self) -> Path:
        return self.path / self.manifest["files"]["video"]

    @property
    def click_wav(self) -> Path:
        return self.path / self.manifest["files"].get("click", "sfx/click.wav")

    @property
    def hover_wav(self) -> Path:
        return self.path / self.manifest["files"].get("hover", "sfx/hover.wav")

    @property
    def music(self) -> Path:
        return self.path / self.manifest["files"].get("music", "music.mid")

    @property
    def video_offset(self) -> float:
        """Seconds the footage lags the timeline (frame n shows timeline time n/fps + offset). Recordings
        made before the recorder stopped dropping the slots ahead of its first frame lack the field; for
        them it is what is missing from the footage's length, which is where those frames went."""
        if "video_offset" in self.manifest:
            return float(self.manifest["video_offset"])
        lost = float(self.manifest["duration"]) - int(self.manifest["frames"]) / float(self.manifest["fps"])
        return min(max(lost, 0.0), 2.0)

    def video_time(self, t: float) -> float:
        """Where timeline time `t` is in game.mkv."""
        return max(0.0, t - self.video_offset)

    @property
    def duration(self) -> float:
        return float(self.manifest["duration"])

    def of_type(self, *types: str) -> list[dict]:
        return [e for e in self.events if e["type"] in types]

    def mark(self, label: str) -> float:
        for e in self.of_type("mark"):
            if e.get("label") == label:
                return e["t"]
        raise KeyError(f"no mark '{label}' in {self.path} (marks: {[e.get('label') for e in self.of_type('mark')]})")

    def clip(self, name: str) -> tuple[float, float]:
        """The span between `clip <name>` and the `clip end` that closes it (or the end of the run)."""
        start = None
        for e in self.events:
            if e["type"] == "clip-begin" and e.get("name") == name:
                start = e["t"]
            elif e["type"] == "clip-end" and start is not None and e.get("name") in (name, "", None):
                return start, e["t"]
        if start is None:
            raise KeyError(f"no clip '{name}' in {self.path} (clips: {self.clip_names()})")
        return start, self.duration

    def clip_names(self) -> list[str]:
        return [e.get("name") for e in self.of_type("clip-begin")]

    def clicks(self, start: float = 0.0, end: float | None = None) -> list[float]:
        end = self.duration if end is None else end
        return [e["t"] for e in self.of_type("click") if start <= e["t"] < end]

    def hovers(self, start: float = 0.0, end: float | None = None) -> list[float]:
        end = self.duration if end is None else end
        return [e["t"] for e in self.of_type("sfx") if e.get("event") == "SmallInteraction" and start <= e["t"] < end]

    def busy_spans(self, start: float = 0.0, end: float | None = None, min_length: float = 1.5) -> list[tuple[float, float]]:
        """Spans spent waiting on the language model (timeline `model-busy` .. `model-idle`), clipped to
        [start, end] and at least `min_length` long — what a cut fast-forwards."""
        end = self.duration if end is None else end
        spans, opened = [], None
        for e in self.events:
            if e["type"] == "model-busy" and opened is None:
                opened = e["t"]
            elif e["type"] == "model-idle" and opened is not None:
                spans.append((opened, e["t"]))
                opened = None
        if opened is not None:
            spans.append((opened, self.duration))
        out = []
        for a, b in spans:
            a, b = max(a, start), min(b, end)
            if b - a >= min_length:
                out.append((a, b))
        return out

    def live_gaps(self, start: float = 0.0, end: float | None = None, min_length: float = 0.4) -> list[tuple[float, float]]:
        """In a live take, the spans the game sat waiting for the agent's next batch: from a batch's
        closing `mark __sync_n` to the next command. Nothing a player would have done happens in them,
        so the cut drops them."""
        end = self.duration if end is None else end
        gaps, opened = [], None
        for e in self.events:
            if e["type"] == "mark" and str(e.get("label", "")).startswith("__sync"):
                opened = e["t"]
            elif e["type"] == "cmd" and opened is not None and not e.get("line", "").startswith("mark __sync"):
                a, b = max(opened, start), min(e["t"], end)
                if b - a >= min_length:
                    gaps.append((a, b))
                opened = None
        return gaps

    def summary(self) -> str:
        """A readable outline of the run: what to cut."""
        lines = [f"{self.path}  {self.duration:.1f}s  {self.manifest['width']}x{self.manifest['height']}@{self.manifest['fps']}"]
        for e in self.events:
            t = f"{e['t']:7.2f}"
            if e["type"] == "mode":
                lines.append(f"{t}  == {e.get('to')}")
            elif e["type"] == "click":
                lines.append(f"{t}  click  {e.get('label')!r}")
            elif e["type"] == "mark" and str(e.get("label", "")).startswith("__"):
                continue                                  # a live take's batch boundary
            elif e["type"] in ("mark", "clip-begin", "clip-end", "note"):
                lines.append(f"{t}  {e['type']:<10} {e.get('label') or e.get('name') or e.get('text') or ''}")
            elif e["type"] == "refused":
                lines.append(f"{t}  REFUSED {e.get('why')}")
        return "\n".join(lines)


def load(path: str | Path) -> Recording:
    path = Path(path)
    manifest = json.loads((path / "manifest.json").read_text(encoding="utf-8"))
    events = [json.loads(l) for l in (path / "timeline.jsonl").read_text(encoding="utf-8").splitlines() if l.strip()]
    return Recording(path, manifest, events)


if __name__ == "__main__":
    import sys
    print(load(sys.argv[1]).summary())
