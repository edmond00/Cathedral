"""An HTML page for one or more run folders: each render's audio, its sheet and its metrics.

    eval.py report runs/round7 [runs/baseline …] -o runs/report.html

Written for a person listening locally: audio and images are linked relative to the page, so keep
the page beside the runs it reads (the default path does). Out-of-band values are marked.
"""

from __future__ import annotations

import html
import json
import os
from pathlib import Path

from . import targets


def _fmt(v) -> str:
    if isinstance(v, float):
        return f"{v:.3g}"
    return html.escape(str(v))


def build(dirs: list[Path], out: Path) -> None:
    renders = []
    for d in dirs:
        for m in sorted(d.glob("*.metrics.json")):
            res = json.loads(m.read_text())
            renders.append((d.name, m, res))
    names = sorted({k for _, _, r in renders for k in r["scores"]["rows"]})
    rel = lambda p: os.path.relpath(p, out.parent)  # noqa: E731
    parts = ["""<title>Music runs</title><style>
:root{--bg:#f6f5f2;--fg:#1d1c1a;--mute:#6b675f;--bad:#a8432a;--line:#d8d4cc}
@media (prefers-color-scheme: dark){:root{--bg:#161514;--fg:#e8e4dc;--mute:#9a958b;--bad:#e0805f;--line:#33302c}}
body{background:var(--bg);color:var(--fg);font:14px/1.45 system-ui,sans-serif;margin:0;padding:16px max(16px,3vw)}
h1{font-size:20px}h2{font-size:15px;margin:28px 0 6px}table{border-collapse:collapse;font-variant-numeric:tabular-nums}
td,th{border-bottom:1px solid var(--line);padding:3px 8px;text-align:right}th:first-child,td:first-child{text-align:left}
.bad{color:var(--bad);font-weight:600}.mute{color:var(--mute)}.wrap{overflow-x:auto}img{max-width:100%}
</style><h1>Music runs</h1>"""]
    parts.append('<div class="wrap"><table><tr><th>metric</th><th>band</th>' +
                 "".join(f"<th>{html.escape(rn)}<br><span class=mute>{html.escape(m.name.split('.')[0])}</span></th>" for rn, m, _ in renders) + "</tr>")
    for k in names:
        b = targets.band(k)
        cells = []
        for _, _, r in renders:
            row = r["scores"]["rows"].get(k)
            cells.append("<td></td>" if row is None else f"<td class={'' if row['ok'] else 'bad'}>{_fmt(row['value'])}</td>")
        parts.append(f"<tr><td>{k}</td><td class=mute>{b[0]}..{b[1]}</td>{''.join(cells)}</tr>")
    parts.append("<tr><th>score</th><th></th>" + "".join(f"<th>{r['scores']['total']:.3f}</th>" for _, _, r in renders) + "</tr></table></div>")
    for rn, m, r in renders:
        wav = Path(r["wav"])
        parts.append(f"<h2>{html.escape(rn)} / {html.escape(wav.name)} <span class=mute>score {r['scores']['total']:.3f}</span></h2>")
        parts.append(f'<audio controls preload="none" src="{html.escape(rel(wav))}"></audio>')
        if r.get("sheet") and Path(r["sheet"]).exists():
            parts.append(f'<div><img loading="lazy" src="{html.escape(rel(Path(r["sheet"])))}" alt="sheet for {html.escape(wav.name)}"></div>')
    out.write_text("\n".join(parts))
