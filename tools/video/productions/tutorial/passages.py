"""List every passage the model wrote during a take, placed on the take's timeline.

    python passages.py <take dir> <llm session dir> [--json out.json]

A take's timeline starts at the wall-clock time in its first event ("started"); each request folder in
the session log is named after the wall-clock time it was sent, and its timing.txt says how long it
streamed. A passage is placed at the moment it finished, inside the section (the last `mark`) that was
running then. Judges (bounded choices read from log-probabilities) write no passage and are skipped.

The model is told stand-in names for the people of the world, and the game swaps the real names back
in before anything is shown; the swaps are read from the game log so the text listed is the text on
screen.
"""
import json
import re
import sys
from datetime import datetime, timedelta
from pathlib import Path


def load_timeline(take: Path):
    events = [json.loads(l) for l in take.joinpath("timeline.jsonl").read_text().splitlines() if l.strip()]
    started = datetime.fromisoformat(events[0]["started"])
    marks = [(e["t"], e["label"]) for e in events if e.get("type") == "mark"]
    return started, marks


def section_at(marks, t):
    label = "before"
    for mt, ml in marks:
        if mt <= t:
            label = ml
    return label


def persona(system: str) -> str:
    m = re.match(r"You are the inner voice of ([A-Za-z' -]+?)[,.]", system)
    if m:
        return m.group(1).strip().upper()
    m = re.match(r"You are (?:the )?([A-Z][A-Z '-]+)\b", system)
    return m.group(1).strip() if m else system[:40]


def name_swaps(take: Path):
    """Stand-in name -> real name, as the game logged them."""
    swaps = {}
    log = take.joinpath("game.log").read_text(errors="replace")
    for m in re.finditer(r"alias '([^']+)' (?:->|→) '([^']+)'", log):
        swaps[m.group(1)] = m.group(2)
    return swaps


def main():
    take, session = Path(sys.argv[1]), Path(sys.argv[2])
    started, marks = load_timeline(take)
    day = started.date()
    swaps = name_swaps(take)
    rows = []
    for req in sorted(session.glob("slot_*/request_*")):
        resp = req / "llm_response.txt"
        if not resp.exists():
            continue
        ctx = json.loads((req / "full_context.json").read_text())
        system = ctx[0]["content"] if ctx and ctx[0]["role"] == "system" else ""
        if "JUDGE" in system[:80]:
            continue
        hh, mm, ss, ms = req.name.split("_")[-1].split("-")
        sent = datetime.combine(day, datetime.min.time()) + timedelta(
            hours=int(hh), minutes=int(mm), seconds=int(ss), milliseconds=int(ms))
        if sent < started - timedelta(hours=1):
            sent += timedelta(days=1)
        dur = 0.0
        m = re.search(r"Duration:\s*(\d+)ms", (req / "timing.txt").read_text())
        if m:
            dur = int(m.group(1)) / 1000
        t0 = (sent - started).total_seconds()
        text = resp.read_text().strip()
        for a, b in swaps.items():
            text = re.sub(rf"\b{re.escape(a)}\b", b, text)
        rows.append({"t0": round(t0, 1), "t1": round(t0 + dur, 1), "section": section_at(marks, t0 + dur),
                     "voice": persona(system), "slot": req.parent.name, "request": req.name,
                     "prompt": (req / "user_message.txt").read_text()[:160], "text": text})
    rows.sort(key=lambda r: r["t1"])
    if "--json" in sys.argv:
        Path(sys.argv[sys.argv.index("--json") + 1]).write_text(json.dumps(rows, indent=1, ensure_ascii=False))
    for r in rows:
        print(f"{r['t1']:8.1f}  {r['section']:<16} {r['voice'][:22]:<22} {r['text']}")


if __name__ == "__main__":
    main()
