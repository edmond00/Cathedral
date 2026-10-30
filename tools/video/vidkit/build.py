"""The storyboard runner: recordings, scenes and narration in, one finished video out.

    python tools/video/make_video.py productions/cities/storyboard.json [--only 3] [--preview] [--sheet]

A storyboard is JSON (see the `video` skill for the full schema):

    {
      "title": "How cities work",
      "output": "build/cities.mp4",
      "voice": {"name": "en_GB-alan-medium", "speed": 1.0, "grit": 0.35},
      "music": {"export": {"mood": "Lament"}, "level_db": -27, "duck": 0.55},
      "recordings": {"city": {"script": "city.cli", "flags": "--playground --seed 42 ...", "timeout": 300}},
      "manim_file": "scenes.py",
      "segments": [
        {"type": "title", "params": {"title": "Cities"}, "vo": "..."},
        {"type": "manim", "scene": "Sprawl", "vo": "...", "params": {...}},
        {"type": "game", "recording": "city", "clip": "arrival", "zoom": {"factor": 2, "center": [480, 300]}, "vo": "..."},
        {"type": "card", "params": {"heading": "...", "lines": ["..."]}, "vo": "..."}
      ]
    }

How long a segment lasts: the longest of its picture's own length, its narration plus a breath either
side, and its "min". A game clip shorter than its narration holds its last frame; a manim scene is
told the narration's length (`self.target`) and paces itself to it.

Order of work, each step cached under build/ so a re-run redoes only what changed:
  1. record any recording whose directory is missing (the game, hidden, in the background)
  2. voice every segment (Piper)
  3. make every segment's picture at the final size, with a short fade at each end
  4. concatenate
  5. mix the soundtrack: voice, the cursor's clicks under game clips, the music ducked beneath
  6. run the game's dither over the whole picture, mux, and encode the deliverable
"""

from __future__ import annotations

import argparse
import hashlib
import json
import os
import shlex
import subprocess
import sys
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np

from . import ROOT, TOOLS, audio, dither, ffmpeg, recording, synth, tts

FADE = 0.35


@dataclass
class Segment:
    index: int
    spec: dict
    picture: Path | None = None
    seconds: float = 0.0
    start: float = 0.0
    clicks: list[float] = field(default_factory=list)
    hovers: list[float] = field(default_factory=list)
    click_wav: Path | None = None
    hover_wav: Path | None = None
    src_t0: float | None = None          # a game segment: where in its recording it starts
    pieces: list = field(default_factory=list)   # and how its footage is retimed: (start, end, speed)
    slowed: list | None = None           # speeds the fit chose instead, when the narration needs longer
    cuts: list = field(default_factory=list)     # the recording moments the narration is anchored to
    # The narration in groups, each starting where its first part is anchored: (wav, seconds, anchor),
    # the anchor None (straight after the group before), "end" (once the picture has played) or a moment
    # of the recording (a game segment). `starts` is where each group starts in the segment.
    groups: list = field(default_factory=list)
    starts: list = field(default_factory=list)


class Build:
    def __init__(self, storyboard: Path, preview: bool = False):
        self.sb_path = storyboard.resolve()
        self.base = self.sb_path.parent
        self.sb = json.loads(self.sb_path.read_text(encoding="utf-8"))
        self.preview = preview
        self.out_dir = self.base / "build"
        self.cache = self.out_dir / "cache"
        self.cache.mkdir(parents=True, exist_ok=True)
        w, h = self.sb.get("size", [1920, 1080])
        self.W, self.H = (w // 2, h // 2) if preview else (w, h)
        self.fps = int(self.sb.get("fps", 30))
        self.voice = {"name": tts.DEFAULT_VOICE, "speed": 1.0, "grit": 0.35, "lead_in": 0.45, "tail": 0.7}
        self.voice.update(self.sb.get("voice", {}))
        self.recordings: dict[str, recording.Recording] = {}

    def log(self, msg: str) -> None:
        print(f"[video] {msg}", flush=True)

    # ── 0. data ───────────────────────────────────────────────────────────────

    def prepare(self) -> None:
        """Runs the storyboard's "prepare" steps whose output is missing: headless game commands that
        write the data a scene draws (`--scene-export`, an audit's CSV…). Numbers in a video come from
        the generator, so they are regenerated from it rather than kept by hand."""
        for step in self.sb.get("prepare", []):
            creates = self.base / step["creates"]
            if creates.exists():
                continue
            creates.parent.mkdir(parents=True, exist_ok=True)
            args = [a.replace("{creates}", str(creates)) for a in shlex.split(step["game"], posix=True)]
            self.log(f"preparing {step['creates']}: {' '.join(args[:3])} ...")
            code = run_game(args, timeout=step.get("timeout", 900), log=creates.with_suffix(".log"))
            if code != 0 or not creates.exists():
                raise RuntimeError(f"prepare step for {creates} failed (exit {code}) — see {creates.with_suffix('.log')}")

    # ── 1. recordings ─────────────────────────────────────────────────────────

    def ensure_recordings(self) -> None:
        for name, spec in self.sb.get("recordings", {}).items():
            if isinstance(spec, str):
                spec = {"dir": spec}
            rec_dir = self.base / spec.get("dir", f"build/rec_{name}")
            if not (rec_dir / "manifest.json").exists():
                if "script" not in spec:
                    raise FileNotFoundError(f"recording '{name}' has no {rec_dir} and no script to make it")
                record(self.base / spec["script"], rec_dir, spec.get("flags", ""), spec.get("timeout", 600),
                       gpu=spec.get("gpu", False))
            self.recordings[name] = recording.load(rec_dir)
            r = self.recordings[name]
            if r.manifest.get("failed"):
                self.log(f"WARNING: recording '{name}' reported a failed or refused command — check {rec_dir}/timeline.jsonl")

    # ── 2. voice ──────────────────────────────────────────────────────────────

    def voice_segment(self, seg: Segment) -> None:
        """A segment's narration: one string, or a list of parts — a string, or {"text", "style"} where
        the style names an entry of the storyboard's "voice_styles" (settings layered over "voice").
        The parts are voiced separately and joined with a short pause: how a narration explains in one
        register and reads the game's own text in another.

        A part of a game segment may be anchored to its footage, and it and the parts after it then start
        no earlier than that: {"at": <seconds of the recording>} at the moment shown (a passage read as it
        starts being written), {"wait": true} once the picture has played through (a result read once it
        is on screen). A group never starts before the one before it has finished."""
        vo = seg.spec.get("vo")
        if not vo:
            return
        parts = [vo] if isinstance(vo, str) else vo
        cuts = [0]
        for i, p in enumerate(parts):
            if i and isinstance(p, dict) and ("at" in p or p.get("wait")):
                cuts.append(i)
        if len(cuts) > 1 and seg.spec["type"] != "game":
            raise ValueError(f"segment {seg.index}: only a game segment's narration can be anchored to its footage")
        cuts.append(len(parts))
        seg.groups = []
        for k in range(len(cuts) - 1):
            group = parts[cuts[k]:cuts[k + 1]]
            head = group[0]
            anchor = None
            if isinstance(head, dict):
                anchor = head["at"] if "at" in head else ("end" if head.get("wait") else None)
            path, secs = self._voice_parts(seg, group, f"g{k}", cuts[k])
            seg.groups.append((path, secs, anchor))

    def _voice_parts(self, seg: Segment, parts: list, name: str, first: int) -> tuple[Path | None, float]:
        if not parts:
            return None, 0.0
        v = self.voice
        clips = []
        for i, part in enumerate(parts, start=first):
            text, style = (part, None) if isinstance(part, str) else (part["text"], part.get("style"))
            cfg = {**v, **(self.sb.get("voice_styles", {}).get(style, {}) if style else {})}
            path, _ = tts.speak(
                text, self.out_dir / f"seg{seg.index:02d}_vo{i}.wav", voice=seg.spec.get("voice", cfg["name"]),
                speed=cfg["speed"], grit=cfg["grit"], cache_dir=self.cache,
                noise_scale=cfg.get("noise_scale"), noise_w=cfg.get("noise_w"),
                pitch=cfg.get("pitch", 1.0), room=cfg.get("room", 0.0))
            clips.append(tts.read_wav(path))
        gap = np.zeros(int(audio.RATE * float(v.get("part_gap", 0.35))), np.float32)
        joined = clips[0]
        for c in clips[1:]:
            joined = np.concatenate([joined, gap, c])
        out = self.out_dir / f"seg{seg.index:02d}_{name}.wav"
        tts.write_wav(out, joined)
        return out, len(joined) / audio.RATE

    def fit_slow(self, seg: Segment) -> float:
        """Slows a game segment's footage where its narration needs more time, stretch by stretch, so that
        no anchor drifts: an explanation that overruns the moment its reading is anchored to slows the
        footage before that moment (the text appears as the explanation ends); narration that outlasts the
        footage slows what comes after the last anchor. The model's waits slow first (down to their real
        pace), then the rest (down to half speed); only what still does not fit is held."""
        g = self.game_timing(seg)
        lead, tail = self.voice["lead_in"], self.voice["tail"]
        gap = float(self.voice.get("part_gap", 0.35))
        pieces = g["pieces"]
        durs = [(b_ - a_) / sp if sp > 0 else 0.0 for a_, b_, sp in pieces]

        def out_at(t: float) -> float:          # output time of recording moment t, with durs as they are
            o = 0.0
            for (a_, b_, sp), d in zip(pieces, durs):
                if sp <= 0:
                    continue
                if t >= b_:
                    o += d
                elif t > a_:
                    return o + d * (t - a_) / (b_ - a_)
                else:
                    return o
            return o

        def stretch(lo: float, hi: float, extra: float) -> None:
            """Adds up to `extra` seconds to the pieces lying within recording moments lo..hi."""
            for floor_speed, pick in ((1.0, lambda sp: sp > 1.0), (0.5, lambda sp: 0 < sp <= 1.0)):
                idx = [i for i, (a_, b_, sp) in enumerate(pieces) if a_ >= lo - 1e-6 and b_ <= hi + 1e-6 and pick(sp)]
                cap = {i: (pieces[i][1] - pieces[i][0]) / floor_speed - durs[i] for i in idx}
                total = sum(c for c in cap.values() if c > 0)
                if total <= 0 or extra <= 0:
                    continue
                take = min(extra, total)
                for i in idx:
                    if cap[i] > 0:
                        durs[i] += take * cap[i] / total
                extra -= take

        t, last = lead, g["t0"]
        for i, (_, secs, anchor) in enumerate(seg.groups):
            want = t + (gap if i else 0.0)
            if anchor not in (None, "end"):
                a = float(anchor)
                if out_at(a) < want:
                    stretch(last, a, want - out_at(a))
                want = max(want, out_at(a))
                last = max(last, a)
            elif anchor == "end":
                # the footage runs until what was said before it is done; this part is then said over
                # the finished frame (a result shown whole), not over yet more slowing
                wait_after = float(self.voice.get("wait_after", 0.3))
                if sum(durs) + wait_after < want:
                    stretch(last, g["t1"] + 1, want - wait_after - sum(durs))
                want = max(want, sum(durs) + wait_after)
                last = g["t1"] + 1
            t = want + secs
        need = t + tail
        if need > sum(durs) and last <= g["t1"]:
            stretch(last, g["t1"] + 1, need - sum(durs))
        seg.slowed = [((b_ - a_) / d if d > 0 else sp) for (a_, b_, sp), d in zip(pieces, durs)]
        return self.place_groups(seg, self.game_timing(seg))

    def place_groups(self, seg: Segment, g: dict | None) -> float:
        """Where each group of the narration starts, given the picture's timing; returns when it ends."""
        lead, tail = self.voice["lead_in"], self.voice["tail"]
        gap = float(self.voice.get("part_gap", 0.35))
        seg.starts = []
        need, t = 0.0, lead
        for i, (_, secs, anchor) in enumerate(seg.groups):
            start = t + (gap if i else 0.0)
            if anchor == "end":
                start = max(start, g["natural"] + float(self.voice.get("wait_after", 0.3)))
            elif anchor is not None:
                start = max(start, g["remap"](float(anchor)))
            seg.starts.append(start)
            t = start + secs
            need = t + tail
        return need

    def target_seconds(self, seg: Segment) -> float:
        """How long the segment lasts: its narration, placed. A game segment whose narration outlasts its
        footage is not frozen on its last frame (unless the storyboard says "fit": "hold"): its footage is
        slowed to fit, the model's waits first (down to their real pace, text streaming as it did), then
        what the player does (down to half speed); only what still does not fit is held."""
        mode = seg.spec.get("fit", self.sb.get("fit", "hold"))
        if seg.spec["type"] == "game" and mode == "slow":
            seg.cuts = sorted({float(a) for _, _, a in seg.groups if a not in (None, "end")})
            seg.slowed = None
            return max(self.fit_slow(seg), float(seg.spec.get("min", 0)))
        g = self.game_timing(seg) if seg.spec["type"] == "game" else None
        need = self.place_groups(seg, g)
        return max(need, float(seg.spec.get("min", 0)))

    # ── 3. pictures ───────────────────────────────────────────────────────────

    def seg_key(self, seg: Segment, *extra) -> str:
        blob = json.dumps([seg.spec, self.W, self.H, self.fps, round(self.target_seconds(seg), 2), *extra],
                          sort_keys=True, default=str)
        return hashlib.sha1(blob.encode()).hexdigest()[:12]

    def picture(self, seg: Segment) -> None:
        kind = seg.spec["type"]
        target = self.target_seconds(seg)
        if kind == "game":
            self.game_picture(seg, target)
        elif kind in ("manim", "title", "card"):
            self.manim_picture(seg, target)
        else:
            raise ValueError(f"segment {seg.index}: unknown type '{kind}'")

    def finish_picture(self, src: Path, dst: Path, natural: float, target: float, pre: list[str],
                       src_size: tuple[int, int], fade: bool = True, whole: bool = True, kind: str = "animation") -> float:
        """Fits `src` into the frame (nearest-neighbour, so the dither stays crisp), holds its last frame
        if the narration outlasts it, and fades both ends. Returns the segment's length."""
        seconds = max(natural, target)
        sw, sh = src_size
        k = min(self.W / sw, self.H / sh)
        if k >= 1 and whole:
            k = float(int(k))                     # whole multiples only: a dither pixel stays a square
        fw, fh = max(2, int(sw * k) // 2 * 2), max(2, int(sh * k) // 2 * 2)
        vf = pre + [f"scale={fw}:{fh}:flags=neighbor",
                    f"pad={self.W}:{self.H}:(ow-iw)/2:(oh-ih)/2:color=black",
                    f"fps={self.fps}",
                    f"tpad=stop_mode=clone:stop_duration={max(0.0, seconds - natural) + 0.5:.3f}"]
        if fade:
            vf += [f"fade=t=in:st=0:d={FADE}", f"fade=t=out:st={seconds - FADE:.3f}:d={FADE}"]
        undithered = dst.with_name(dst.stem + "_clean.mkv")
        ffmpeg.run("-i", src, "-vf", ",".join(vf), "-t", f"{seconds:.3f}", "-an", *ffmpeg.INTERMEDIATE, undithered)
        d = self.dither_for(kind)
        dither.dither_video(undithered, dst, levels=d.get("levels", 6), scale=d.get("scale", 1),
                            strength=d.get("strength", 1.0), encode=ffmpeg.INTERMEDIATE)
        undithered.unlink(missing_ok=True)
        return seconds

    def dither_for(self, kind: str) -> dict:
        """The dither a segment gets. Game footage is already dithered by the game, so its pass only
        catches the fades and the cursor (lattice-aware: the footage itself comes through unchanged).
        Animations are clean vectors and get a coarser, harder dither so they sit beside the footage
        rather than above it."""
        d = self.sb.get("dither", {})
        if kind == "game":
            return {"levels": 6, "scale": 1, **d.get("game", {})}
        return {"levels": 4, "scale": 2, **d.get("animation", {})}

    def game_timing(self, seg: Segment) -> dict:
        """Where a game segment comes from in its take and how it is retimed (model waits fast-forwarded,
        a live take's pauses dropped): what the picture is, and how long it plays, before rendering it."""
        s = seg.spec
        rec = self.recordings[s["recording"]]
        if "clip" in s:
            t0, t1 = rec.clip(s["clip"])
        elif "mark" in s:
            t0 = rec.mark(s["mark"])
            t1 = t0 + float(s.get("length", 6))
        else:
            t0, t1 = float(s.get("from", 0)), float(s.get("to", rec.duration))
        t0 = max(0.0, t0 - float(s.get("pre", 0)))
        # Two frames short of the marker: the next command runs the instant a clip ends (a `pause` opens
        # the menu), and the frame at the marker can already show it — then held under the narration.
        t1 = min(rec.duration, t1 + float(s.get("post", 0))) - 2.0 / rec.manifest["fps"]
        speed = float(s.get("speed", 1.0))

        # Fast-forward: the spans spent waiting on the model play at `ff` times speed (on the CPU a
        # passage can take a minute), everything a player does at `speed`.
        ffcfg = {"speed": 8.0, "min": 1.5, **self.sb.get("fast_forward", {})}
        ff = s.get("fast_forward", ffcfg.get("speed", 8.0))
        busy = rec.busy_spans(t0, t1, float(ffcfg.get("min", 1.5))) if ff else []
        # A live take's pauses (the agent deciding its next move) are cut out entirely: speed 0.
        gaps = rec.live_gaps(t0, t1)
        marks = sorted([(a_, b_, float(ff)) for a_, b_ in busy] + [(a_, b_, 0.0) for a_, b_ in gaps])
        pieces, cursor = [], t0
        for a_, b_, sp in marks:
            a_ = max(a_, cursor)
            if b_ <= a_:
                continue
            if a_ > cursor:
                pieces.append((cursor, a_, speed))
            pieces.append((a_, b_, sp))
            cursor = b_
        if cursor < t1:
            pieces.append((cursor, t1, speed))
        for cut in sorted(seg.cuts):              # split at the narration's anchors, so each stretch slows alone
            out = []
            for a_, b_, sp in pieces:
                if a_ < cut < b_:
                    out += [(a_, cut, sp), (cut, b_, sp)]
                else:
                    out.append((a_, b_, sp))
            pieces = out
        if seg.slowed is not None and len(seg.slowed) == len(pieces):
            pieces = [(a_, b_, sp) for (a_, b_, _), sp in zip(pieces, seg.slowed)]

        def remap(t: float) -> float:
            out = 0.0
            for a_, b_, sp in pieces:
                if sp <= 0:
                    if t < b_:
                        return out
                    continue
                if t >= b_:
                    out += (b_ - a_) / sp
                else:
                    return out + max(0.0, t - a_) / sp
            return out

        natural = sum((b_ - a_) / sp for a_, b_, sp in pieces if sp > 0)
        seg.src_t0 = t0
        seg.pieces = pieces
        seg.click_wav, seg.hover_wav = rec.click_wav, rec.hover_wav
        dropped = lambda t: any(sp <= 0 and a_ <= t < b_ for a_, b_, sp in pieces)  # noqa: E731
        seg.clicks = [remap(t) for t in rec.clicks(t0, t1) if not dropped(t)]
        seg.hovers = [remap(t) for t in rec.hovers(t0, t1)] if s.get("hover_ticks", True) else []
        return {"rec": rec, "t0": t0, "t1": t1, "pieces": pieces, "natural": natural, "busy": busy, "ff": ff,
                "ffcfg": ffcfg, "speed": speed, "remap": remap}

    def game_picture(self, seg: Segment, target: float) -> None:
        s = seg.spec
        g = self.game_timing(seg)
        rec, t0, t1, pieces, natural = g["rec"], g["t0"], g["t1"], g["pieces"], g["natural"]
        busy, ff, ffcfg, speed = g["busy"], g["ff"], g["ffcfg"], g["speed"]
        sw, sh = rec.manifest["width"], rec.manifest["height"]
        pre = []
        if z := s.get("zoom"):
            f = float(z.get("factor", 2))
            cw, ch = int(sw / f) // 2 * 2, int(sh / f) // 2 * 2
            cx, cy = z.get("center", [sw / 2, sh / 2])
            x = int(min(max(cx - cw / 2, 0), sw - cw))
            y = int(min(max(cy - ch / 2, 0), sh - ch))
            pre.append(f"crop={cw}:{ch}:{x}:{y}")
            sw, sh = cw, ch

        dst = self.out_dir / f"seg{seg.index:02d}_{self.seg_key(seg, t0, t1, rec.manifest['duration'], pieces, rec.video_offset)}.mkv"
        if not dst.exists():
            self.log(f"segment {seg.index}: game '{s['recording']}' {t0:.2f}-{t1:.2f}s"
                     + (f" ({len(busy)} model wait(s) at x{ff:g})" if busy else ""))
            trimmed = self.out_dir / f"seg{seg.index:02d}_raw.mkv"
            ffmpeg.run("-ss", f"{rec.video_time(t0):.3f}", "-to", f"{rec.video_time(t1):.3f}", "-i", rec.video,
                       "-an", *ffmpeg.INTERMEDIATE, trimmed)
            timed = self.out_dir / f"seg{seg.index:02d}_timed.mkv"
            self.retime(trimmed, timed, [(a_ - t0, b_ - t0, sp) for a_, b_, sp in pieces], speed,
                        label=bool(ffcfg.get("label", True)))
            self.finish_picture(timed, dst, natural, target, pre, (sw, sh), s.get("fade", True),
                                whole="zoom" not in s, kind="game")
            trimmed.unlink(missing_ok=True)
            timed.unlink(missing_ok=True)
        seg.picture, seg.seconds = dst, max(natural, target)

    def retime(self, src: Path, dst: Path, pieces: list[tuple[float, float, float]], base_speed: float,
               label: bool = True) -> None:
        """Plays each (start, end, speed) piece of `src` at its speed, joined; a fast-forwarded piece
        carries a small ">> xN" in the corner, so nobody mistakes the cut for the game's real pace —
        unless the storyboard's `"fast_forward": {"label": false}` says the sped-up pace is the real one
        (the CPU of the recording machine is slower than a player's)."""
        font = r"C\:/Windows/Fonts/consola.ttf"   # drawtext wants the drive's colon escaped
        parts, labels, t_out = [], [], 0.0
        pieces = [p for p in pieces if p[2] > 0]          # dropped spans (a live take's pauses)
        for i, (a_, b_, sp) in enumerate(pieces):
            chain = f"[0:v]trim=start={a_:.3f}:end={b_:.3f},setpts=(PTS-STARTPTS)/{sp:g}"
            if label and sp > base_speed:
                chain += (f",drawtext=fontfile='{font}':text='>> x{sp:g}':fontsize=26:fontcolor=0xB3B3B3"
                          f":box=1:boxcolor=0x000000:boxborderw=8:x=w-tw-36:y=h-th-30")
            parts.append(chain + f"[p{i}]")
            labels.append(f"[p{i}]")
            t_out += (b_ - a_) / sp
        graph = ";".join(parts) + ";" + "".join(labels) + f"concat=n={len(pieces)}:v=1:a=0[out]"
        ffmpeg.run("-i", src, "-filter_complex", graph, "-map", "[out]", "-an", *ffmpeg.INTERMEDIATE, dst)

    def manim_picture(self, seg: Segment, target: float) -> None:
        s = seg.spec
        if s["type"] == "manim":
            scene_file = self.base / s.get("file", self.sb.get("manim_file", "scenes.py"))
            scene = s["scene"]
        else:
            scene_file = TOOLS / "vidkit" / "cards.py"
            scene = "TitleCard" if s["type"] == "title" else "TextCard"
        def resolve(v):                              # data files are passed as absolute paths
            if isinstance(v, str) and v.startswith("./"):
                return str((self.base / v).resolve())
            if isinstance(v, list):
                return [resolve(x) for x in v]
            return v
        params = {k: resolve(v) for k, v in s.get("params", {}).items()}
        files = [x for v in params.values() for x in (v if isinstance(v, list) else [v])]
        data_stamp = [Path(x).stat().st_mtime for x in files if isinstance(x, str) and Path(x).is_file()]
        key = self.seg_key(seg, scene_file.read_text(encoding="utf-8"), data_stamp,
                           (TOOLS / "vidkit" / "manimkit.py").read_text(encoding="utf-8"))
        dst = self.out_dir / f"seg{seg.index:02d}_{key}.mkv"
        if not dst.exists():
            self.log(f"segment {seg.index}: manim {scene} ({target:.1f}s of narration)")
            media = self.out_dir / "manim"
            env = dict(os.environ, VID_TARGET_SECONDS=f"{target:.3f}", VID_PARAMS=json.dumps(params),
                       PYTHONPATH=str(TOOLS) + os.pathsep + os.environ.get("PYTHONPATH", ""))
            name = f"seg{seg.index:02d}_{key}"
            cmd = [sys.executable, "-m", "manim", "render", str(scene_file), scene,
                   "-r", f"{self.W},{self.H}", "--fps", str(self.fps), "--format", "mp4",
                   "--media_dir", str(media), "-o", name, "--disable_caching", "--progress_bar", "none"]
            proc = subprocess.run(cmd, env=env, capture_output=True, text=True, encoding="utf-8", errors="replace")
            if proc.returncode != 0:
                raise RuntimeError(f"manim failed on {scene}:\n{proc.stdout[-3000:]}\n{proc.stderr[-3000:]}")
            found = list(media.rglob(f"{name}.mp4"))
            if not found:
                raise RuntimeError(f"manim produced no {name}.mp4:\n{proc.stdout[-2000:]}")
            rendered = found[0]
            natural = ffmpeg.duration(rendered)
            self.finish_picture(rendered, dst, natural, target, [], ffmpeg.video_size(rendered), s.get("fade", True),
                                kind="animation")
        seg.picture = dst
        seg.seconds = ffmpeg.duration(dst)

    # ── 5. sound ──────────────────────────────────────────────────────────────

    def take_audio_bed(self, segs: list[Segment], total: float, rec_name: str) -> np.ndarray:
        """The take's own sound (music.wav: the game's music and in-game sounds, as a player heard them)
        under the whole video, following the picture so a sound lands on the frame that made it: where the
        footage plays at its own speed so does the sound; where the picture runs a little faster (a fight
        at twice speed) the sound is time-stretched, its pitch kept; where it races through a model's wait,
        the sound skips ahead with it rather than play sped up. A frame held under the narration keeps the
        sound going under it, and so does a diagram, but not with what comes next in the take (that would play
        the next moment's sounds early, and again when its footage plays): with what the fast-forward skipped
        of the latest model wait instead, music the picture raced past and nobody heard, where the game was
        idle. Neighbouring stretches crossfade."""
        import soundfile as sf
        import subprocess
        import tempfile
        from scipy import signal as sg
        def wav_of(name: str) -> Path:
            rec = self.recordings[name]
            return rec.path / rec.manifest["files"]["music_audio"]

        first = next((s.spec["recording"] for s in segs if s.src_t0 is not None), rec_name)
        path = wav_of(rec_name if rec_name in self.recordings else first)   # each game segment then reads its own

        def read(a: float, length: float, where: Path | None = None) -> np.ndarray:
            where = where or path
            info = sf.info(str(where))
            i = max(0, int(a * info.samplerate))
            win, _ = sf.read(str(where), start=i, stop=i + max(1, int(length * info.samplerate)), dtype="float32",
                             always_2d=True)
            if win.shape[1] == 1:
                win = np.repeat(win, 2, axis=1)
            win = sg.resample_poly(win[:, :2], audio.RATE, info.samplerate, axis=0).astype(np.float32)
            n = int(length * audio.RATE)
            return np.pad(win, ((0, max(0, n - len(win))), (0, 0)))[:n]

        def stretch(x: np.ndarray, speed: float) -> np.ndarray:
            with tempfile.TemporaryDirectory() as d:
                src, dst = Path(d) / "a.wav", Path(d) / "b.wav"
                sf.write(str(src), x, audio.RATE)
                subprocess.run(["ffmpeg", "-nostdin", "-v", "error", "-y", "-i", str(src), "-af", f"atempo={speed:g}",
                                str(dst)], check=True)
                y, _ = sf.read(str(dst), dtype="float32", always_2d=True)
            return y

        xf = int(0.12 * audio.RATE)

        def join(a: np.ndarray, b: np.ndarray) -> np.ndarray:
            if len(a) < xf or len(b) < xf:
                return np.concatenate([a, b])
            ramp = np.linspace(0, 1, xf)[:, None]
            return np.concatenate([a[:-xf], a[-xf:] * (1 - ramp) + b[:xf] * ramp, b[xf:]])

        pool: list[list[float]] = []      # model waits the picture skipped: [start, end, next unheard]

        def filler(length: float) -> np.ndarray:
            if not pool:
                return read(src, length)
            out = np.zeros((0, 2), np.float32)
            while len(out) < int(length * audio.RATE) - xf:
                start, end, cur, where = pool[-1]
                if end - cur < 1.0:                    # used up: the one before, or this one again
                    if len(pool) > 1:
                        pool.pop()
                    else:
                        pool[-1][2] = start
                    continue
                take = min(length - len(out) / audio.RATE + 0.2, end - cur)
                out = join(out, read(cur, take, where))
                pool[-1][2] = cur + take
            return out

        bed = np.zeros((int((total + 1) * audio.RATE), 2), np.float32)
        src = 0.0
        for seg in segs:
            track = np.zeros((0, 2), np.float32)
            if seg.src_t0 is not None:
                src = seg.src_t0
                path = wav_of(seg.spec["recording"])
                for a_, b_, sp in seg.pieces:
                    if sp <= 0:
                        continue
                    out = (b_ - a_) / sp
                    if sp == 1:
                        piece = read(a_, out)
                    elif sp <= 2.5:
                        piece = stretch(read(a_, b_ - a_), sp)
                    else:
                        piece = read(a_, out)          # the sound skips ahead with the picture
                        if b_ - a_ - out > 2.0:
                            pool.append([a_ + out, b_, a_ + out, path])
                    n = int(out * audio.RATE)
                    piece = np.pad(piece, ((0, max(0, n - len(piece))), (0, 0)))[:n]
                    track = join(track, piece)
                    src = b_
            rest = seg.seconds - len(track) / audio.RATE + 0.75
            if rest > 0:
                track = join(track, filler(rest))
            n = int(0.75 * audio.RATE)
            if seg.index and len(track) > n:
                track[:n] *= np.linspace(0, 1, n)[:, None]
            if len(track) > n:
                track[-n:] *= np.linspace(1, 0, n)[:, None]
            at = int(seg.start * audio.RATE)
            m = min(len(track), len(bed) - at)
            bed[at:at + m] += track[:m]
        return bed[: int(total * audio.RATE)]

    def music_bed(self, total: float, segs: list[Segment] | None = None) -> np.ndarray | None:
        m = self.sb.get("music")
        if not m:
            return None
        if "take_audio" in m:
            bed = self.take_audio_bed(segs or [], total, m["take_audio"])
            fade = int(audio.RATE * 2.5)
            bed[-fade:] *= np.linspace(1, 0, fade)[:, None]
            return audio.normalise(bed, m.get("level_db", -34))
        if "midi" in m:
            midi = self.base / m["midi"]
        elif "recording" in m:
            midi = self.recordings[m["recording"]].music
        elif "export" in m:
            if self._composer is not None:
                self.log("waiting for the game's composer to finish...")
                self._composer.join()
            midi = self._music_midi()
            if not midi.exists():
                self.compose(total)
        else:
            raise ValueError("music needs one of: take_audio, midi, recording, export")
        bed = synth.render(midi, length=None, grit=m.get("grit", 0.25))
        reps = int(np.ceil(total * audio.RATE / max(len(bed), 1)))
        bed = np.concatenate([bed] * max(reps, 1))[: int(total * audio.RATE)]
        fade = int(audio.RATE * 2.5)
        bed[-fade:] *= np.linspace(1, 0, fade)[:, None]
        bed[:int(audio.RATE)] *= np.linspace(0, 1, int(audio.RATE))[:, None]
        return audio.normalise(bed, m.get("level_db", -34))

    def soundtrack(self, segs: list[Segment], total: float) -> Path:
        voice = audio.silence(total)
        for seg in segs:
            for (path, _, _), start in zip(seg.groups, seg.starts):
                audio.place(voice, tts.read_wav(path), seg.start + start)
        voice = audio.normalise(voice, self.sb.get("voice_level_db", -18))

        fx = audio.silence(total)
        click_gain = float(self.sb.get("clicks", {}).get("gain", 0.55))
        for seg in segs:
            if seg.clicks and seg.click_wav and seg.click_wav.exists():
                c = tts.read_wav(seg.click_wav)
                c = c / (np.max(np.abs(c)) + 1e-9)
                for t in seg.clicks:
                    audio.place(fx, c, seg.start + t, gain=click_gain)
            if seg.hovers and seg.hover_wav and seg.hover_wav.exists():
                hv = tts.read_wav(seg.hover_wav)
                hv = hv / (np.max(np.abs(hv)) + 1e-9)
                for t in seg.hovers:
                    audio.place(fx, hv, seg.start + t, gain=click_gain * 0.25)

        mix = voice + fx
        bed = self.music_bed(total, segs)
        if bed is not None:
            # same length as the rest of the mix, to the sample
            bed = np.pad(bed, ((0, max(0, len(voice) - len(bed))), (0, 0)))[: len(voice)]
            mix += audio.duck(bed, voice, self.sb["music"].get("duck", 0.65))
        out = self.out_dir / "soundtrack.wav"
        tts.write_wav(out, audio.limiter(mix))
        return out

    def _music_midi(self) -> Path:
        e = self.sb["music"]["export"]
        return self.cache / f"music_{e.get('mood', 'Neutral')}_{e.get('tracks', 4)}.mid"

    def compose(self, seconds: float) -> None:
        """Has the game's own composer write the bed (real time: a minute of music takes a minute)."""
        e = self.sb["music"]["export"]
        midi = self._music_midi()
        seconds = int(seconds)
        self.log(f"composing {seconds}s of {e.get('mood', 'Neutral')} with the game's music engine (real time)...")
        tmp = midi.with_suffix(".part.mid")
        run_game(["--export-music", str(tmp), "--seconds", str(seconds), "--mood", e.get("mood", "Neutral"),
                  "--tracks", str(e.get("tracks", 4))], timeout=seconds + 120)
        tmp.replace(midi)

    _composer = None

    # ── the whole thing ──────────────────────────────────────────────────────

    def run(self, only: set[int] | None = None) -> Path:
        self.prepare()
        self.ensure_recordings()
        segs = [Segment(i, s) for i, s in enumerate(self.sb["segments"])]
        for seg in segs:
            self.voice_segment(seg)
            self.target_seconds(seg)          # where each group of the narration starts

        # The composer runs in real time, so start it now and let it work while the pictures render.
        if "export" in (self.sb.get("music") or {}) and not self._music_midi().exists():
            import threading
            estimate = sum(max(self.target_seconds(s), 8.0) for s in segs) * 1.25 + 30
            self._composer = threading.Thread(target=self.compose, args=(estimate,), daemon=True)
            self._composer.start()
        for seg in segs:
            if only is None or seg.index in only:
                self.picture(seg)
            else:
                # reuse whatever was built last for it
                prev = sorted(self.out_dir.glob(f"seg{seg.index:02d}_*.mkv"), key=lambda p: p.stat().st_mtime)
                prev = [p for p in prev if not p.name.endswith("_raw.mkv")]
                if not prev:
                    self.picture(seg)
                else:
                    seg.picture, seg.seconds = prev[-1], ffmpeg.duration(prev[-1])
                    if seg.spec["type"] == "game":   # the clicks are cheap to recompute
                        self.game_picture(seg, self.target_seconds(seg))

        t = 0.0
        for seg in segs:
            seg.start = t
            t += seg.seconds
        total = t

        listing = self.out_dir / "concat.txt"
        listing.write_text("".join(f"file '{seg.picture.as_posix()}'\n" for seg in segs), encoding="utf-8")
        picture = self.out_dir / "picture.mkv"
        ffmpeg.run("-f", "concat", "-safe", "0", "-i", listing, "-c", "copy", picture)

        self.log(f"mixing {total:.1f}s of sound")
        sound = self.soundtrack(segs, total)

        out = self.base / self.sb.get("output", "build/video.mp4")
        if self.preview:
            out = out.with_name(out.stem + "_preview" + out.suffix)
        out.parent.mkdir(parents=True, exist_ok=True)
        self.log(f"encoding {out}")
        ffmpeg.run("-i", picture, "-i", sound, "-map", "0:v", "-map", "1:a", *ffmpeg.DELIVERY,
                   "-c:a", "aac", "-b:a", "192k", "-shortest", out)

        (self.out_dir / "edit.json").write_text(json.dumps(
            [{"segment": s.index, "type": s.spec["type"], "start": round(s.start, 2), "seconds": round(s.seconds, 2),
              "vo": s.spec.get("vo", "")} for s in segs], indent=2), encoding="utf-8")
        self.log(f"done: {out} ({total:.1f}s)")
        return out


# ── the game, from here ────────────────────────────────────────────────────────

def run_game(args: list[str], timeout: int = 600, log: Path | None = None) -> int:
    """Runs the development build of the game (built already: `dotnet build`) with these arguments."""
    cmd = ["dotnet", "run", "--no-build", "--project", str(ROOT / "Cathedral.csproj"), "--"] + args
    with open(log, "w", encoding="utf-8", errors="replace") if log else open(os.devnull, "w") as out:
        proc = subprocess.run(cmd, cwd=ROOT, stdout=out, stderr=subprocess.STDOUT, timeout=timeout)
    return proc.returncode


def record(script: Path, out_dir: Path, flags: str = "", timeout: int = 600, gpu: bool = False) -> Path:
    """Plays `script` in the game with --record, hidden, into `out_dir`. CPU unless `gpu`."""
    out_dir.mkdir(parents=True, exist_ok=True)
    args = shlex.split(flags, posix=True) + ["--cli-script", str(script), "--cli-timeout", str(timeout),
                                             "--record", str(out_dir)]
    if gpu:
        args.append("--gpu")
    print(f"[video] recording {script.name} -> {out_dir} (this plays the game in the background)", flush=True)
    code = run_game(args, timeout=timeout + 120, log=out_dir / "game.log")
    if not (out_dir / "manifest.json").exists():
        raise RuntimeError(f"recording failed (exit {code}) — see {out_dir / 'game.log'}")
    if code != 0:
        print(f"[video] WARNING: the recording exited {code} — a command failed or was refused; "
              f"grep '^\\[cli\\]' {out_dir / 'game.log'}", flush=True)
    return out_dir


def contact_sheet(video: Path, out: Path, columns: int = 4, rows: int = 4) -> Path:
    """A grid of frames sampled evenly through `video` — for looking at a whole cut at once."""
    total = ffmpeg.duration(video)
    n = columns * rows
    fps = n / max(total, 0.1)
    ffmpeg.run("-i", video, "-vf", f"fps={fps:.5f},scale=480:-2,tile={columns}x{rows}:padding=4:color=black",
               "-frames:v", "1", out)
    return out


def main(argv: list[str] | None = None) -> int:
    ap = argparse.ArgumentParser(prog="make_video", description=__doc__.split("\n\n")[0])
    sub = ap.add_subparsers(dest="cmd")

    b = sub.add_parser("build", help="build a storyboard into a video")
    b.add_argument("storyboard", type=Path)
    b.add_argument("--only", type=str, help="rebuild only these segments (e.g. 2,5); the rest are reused")
    b.add_argument("--preview", action="store_true", help="half resolution, fast")
    b.add_argument("--sheet", action="store_true", help="also write a contact sheet of the result")

    r = sub.add_parser("record", help="play a .cli script in the game and film it")
    r.add_argument("script", type=Path)
    r.add_argument("out", type=Path)
    r.add_argument("--flags", default="--skip-childhood --seed 42 --no-encounters --allow-reentry")
    r.add_argument("--timeout", type=int, default=3600)
    r.add_argument("--gpu", action="store_true", help="run the model on the GPU (it has crashed this machine before)")

    lv = sub.add_parser("live", help="start a live take: the game records, fed from commands you send (run in background)")
    lv.add_argument("out", type=Path)
    lv.add_argument("--flags", default="--skip-childhood --seed 42 --no-encounters --allow-reentry")
    lv.add_argument("--timeout", type=int, default=7200)
    lv.add_argument("--gpu", action="store_true", help="run the model on the GPU (it has crashed this machine before)")

    sd = sub.add_parser("send", help="send commands to a live take and print the game's answers")
    sd.add_argument("out", type=Path)
    sd.add_argument("lines", nargs="+")
    sd.add_argument("--timeout", type=float, default=1800)

    i = sub.add_parser("inspect", help="outline a recording: modes, clicks, marks, clips")
    i.add_argument("recording", type=Path)

    s = sub.add_parser("sheet", help="contact sheet of any video")
    s.add_argument("video", type=Path)
    s.add_argument("out", type=Path)

    v = sub.add_parser("say", help="voice one line, to audition a voice or the grit")
    v.add_argument("text")
    v.add_argument("out", type=Path)
    v.add_argument("--voice", default=tts.DEFAULT_VOICE)
    v.add_argument("--grit", type=float, default=0.35)
    v.add_argument("--speed", type=float, default=1.0)

    m = sub.add_parser("music", help="render a MIDI file (a recording's music.mid, or --export-music's) to WAV")
    m.add_argument("midi", type=Path)
    m.add_argument("out", type=Path)
    m.add_argument("--seconds", type=float)

    a = ap.parse_args(argv)
    if a.cmd == "build":
        only = {int(x) for x in a.only.split(",")} if a.only else None
        out = Build(a.storyboard, preview=a.preview).run(only)
        if a.sheet:
            print(contact_sheet(out, out.with_suffix(".sheet.png")))
    elif a.cmd == "record":
        record(a.script.resolve(), a.out.resolve(), a.flags, a.timeout, a.gpu)
        print(recording.load(a.out).summary())
    elif a.cmd == "live":
        from . import live
        return live.run(a.out.resolve(), a.flags, a.timeout, a.gpu)
    elif a.cmd == "send":
        from . import live
        print(live.send(a.out.resolve(), a.lines, a.timeout))
    elif a.cmd == "inspect":
        print(recording.load(a.recording).summary())
    elif a.cmd == "sheet":
        print(contact_sheet(a.video, a.out))
    elif a.cmd == "say":
        path, secs = tts.speak(a.text, a.out, a.voice, a.speed, a.grit)
        print(f"{path} ({secs:.1f}s)")
    elif a.cmd == "music":
        tts.write_wav(a.out, synth.render(a.midi, a.seconds))
        print(a.out)
    else:
        ap.print_help()
        return 1
    return 0
