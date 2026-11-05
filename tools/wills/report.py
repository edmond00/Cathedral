"""Build reports/multi-target-wills.html from two --will-audit JSON files.

    python tools/wills/report.py BEFORE.json AFTER.json [extras.json] > reports/multi-target-wills.html

extras.json (optional) carries what the audits do not write: the catalogue and learned shares from
--mm-audit, the converted modi mentis, the content added, the refusal counts and the notes.
"""
import html, json, sys

before = json.load(open(sys.argv[1]))
after  = json.load(open(sys.argv[2]))
extras = json.load(open(sys.argv[3])) if len(sys.argv) > 3 else {}
B = {r["Id"]: r for r in before["rows"]}
A = {r["Id"]: r for r in after["rows"]}
esc = html.escape

def agg(rows, ids):
    rs = [rows[i] for i in ids if i in rows and not rows[i]["Gated"]]
    s, p = sum(r["RelatedStarts"] for r in rs), sum(r["RelatedPlanned"] for r in rs)
    cs, cp = sum(r["Starts"] for r in rs), sum(r["Planned"] for r in rs)
    k, kp = sum(r["Kinds"] for r in rs), sum(r["KindsPlanned"] for r in rs)
    good = sum(1 for r in rs if r["Related"] is not None and r["Related"] >= 0.6)
    withp = sum(1 for r in rs if r["Related"] is not None)
    return dict(n=len(rs), related=p / s if s else 0, cover=cp / cs if cs else 0, kinds=kp / k if k else 0,
                good=good, withp=withp,
                targets=sum(r["TargetCount"] for r in (rows[i] for i in ids if i in rows)) / max(1, len([i for i in ids if i in rows])))

orig = [i for i in B if i in A]
new  = [i for i in A if i not in B]
hb, ha, ho, hn = agg(B, list(B)), agg(A, list(A)), agg(A, orig), agg(A, new)
pct = lambda x: f"{100 * x:.1f}%"

def row(label, b, a, fmt=pct):
    d = a - b
    cls = "up" if d > 0.0005 else "down" if d < -0.0005 else ""
    return f"<tr><th>{label}</th><td>{fmt(b)}</td><td>{fmt(a)}</td><td class='{cls}'>{'+' if d > 0 else ''}{fmt(d) if fmt is pct else f'{d:.2f}'}</td></tr>"

# Per-will chart: related coverage before (grey) and after (yellow), wills with places, sorted by after.
chart_rows = []
ids = sorted([i for i in A if A[i]["Related"] is not None and not A[i]["Gated"]], key=lambda i: (-A[i]["Related"], i))
W, BAR, GAP, LEFT = 520, 7, 6, 150
for n, i in enumerate(ids):
    y = n * (2 * BAR + GAP)
    b = B.get(i, {}).get("Related")
    a = A[i]["Related"]
    mark = "" if i in B else " ✦"
    chart_rows.append(
        f"<g transform='translate(0,{y})'><text x='{LEFT - 8}' y='{BAR + 3}' class='lbl'>{esc(i)}{mark}</text>"
        + (f"<rect x='{LEFT}' y='0' width='{(W - LEFT) * b:.1f}' height='{BAR}' class='b'/>" if b is not None else "")
        + f"<rect x='{LEFT}' y='{BAR}' width='{(W - LEFT) * a:.1f}' height='{BAR}' class='a'/>"
        + f"<text x='{LEFT + (W - LEFT) * a + 4:.1f}' y='{2 * BAR}' class='val'>{100 * a:.0f}</text></g>")
H = len(ids) * (2 * BAR + GAP) + 24
ticks = "".join(f"<line x1='{LEFT + (W - LEFT) * t}' x2='{LEFT + (W - LEFT) * t}' y1='0' y2='{H - 20}' class='grid'/>"
                f"<text x='{LEFT + (W - LEFT) * t}' y='{H - 6}' class='tick'>{int(100 * t)}%</text>" for t in (0, .25, .5, .75, 1))
chart = f"<svg viewBox='0 0 {W + 40} {H}' width='{W + 40}' role='img' aria-label='Related coverage per will, before and after'>{ticks}{''.join(chart_rows)}</svg>"

def will_table(ids_, cols):
    out = ["<table class='data'><thead><tr>" + "".join(f"<th>{c}</th>" for c in cols) + "</tr></thead><tbody>"]
    for i in ids_:
        a, b = A.get(i), B.get(i)
        rel = lambda r: "—" if not r or r["Related"] is None else f"{100 * r['Related']:.0f}%"
        out.append(f"<tr><td class='id'>{esc(i)}</td><td>{rel(b)}</td><td>{rel(a)}</td>"
                   f"<td>{100 * b['Breadth']:.0f}%</td><td>{100 * a['Breadth']:.0f}%</td><td class='t'>{esc(a['Targets'])}</td></tr>"
                   if b else
                   f"<tr><td class='id'>{esc(i)}</td><td>{rel(a)}</td><td>{100 * a['Coverage']:.0f}%</td><td>{100 * a['Breadth']:.0f}%</td>"
                   f"<td class='t'>{esc(a['Targets'])}</td></tr>")
    out.append("</tbody></table>")
    return "".join(out)

changed = [i for i in orig if A[i]["Targets"] != B[i]["Targets"]]
worse = [i for i in orig if not A[i]["Gated"] and A[i]["Related"] is not None and B[i]["Related"] is not None
         and A[i]["Related"] < B[i]["Related"] - 0.02]

conv = extras.get("converted", {})
def rel_of(i):
    r = A.get(i)
    return "—" if not r or r["Related"] is None else f"{100 * r['Related']:.0f}%"

conv_rows = "".join(
    f"<tr><td class='id'>{esc(i)}</td><td>{esc(conv[i].get('was', ''))}</td>"
    f"<td class='t'>{'<br>'.join(esc(t) for t in conv[i]['targets'])}</td><td>{rel_of(i)}</td></tr>"
    for i in sorted(conv))

shares = extras.get("shares", {})
refusals = extras.get("refusals")
content = extras.get("content", [])
notes = extras.get("notes", [])
kept_ordinary = extras.get("kept_ordinary", [])

page = f"""<title>Multi-target Wills</title>
<meta name="claude:viewport-fit-sides" content="cover">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Cormorant+Garamond:wght@600&family=IBM+Plex+Mono:wght@400;500&family=IBM+Plex+Sans:wght@400;500;600&display=swap">
<style>
/* Layout: one readable column of findings; wide tables and the chart scroll in their own boxes. */
:root {{
  --bg: #f6f5f0; --fg: #1d1b22; --muted: #67626f; --rule: #d9d5cc;
  --gold: #8a6a00; --violet: #5b3f8c; --before: #a9a4b1; --after: #c79a00; --bad: #a3322b; --good: #2e6b3a;
  --mono: "IBM Plex Mono", Consolas, "DejaVu Sans Mono", monospace;
  --serif: "Cormorant Garamond", Georgia, serif; --sans: "IBM Plex Sans", system-ui, sans-serif;
}}
@media (prefers-color-scheme: dark) {{ :root:not([data-theme="light"]) {{
  --bg: #15131a; --fg: #e8e4dc; --muted: #9a94a3; --rule: #34303d;
  --gold: #e7c14a; --violet: #b49be6; --before: #5d5867; --after: #e7c14a; --bad: #e08178; --good: #8fcf9c; color-scheme: dark }} }}
:root[data-theme="dark"] {{
  --bg: #15131a; --fg: #e8e4dc; --muted: #9a94a3; --rule: #34303d;
  --gold: #e7c14a; --violet: #b49be6; --before: #5d5867; --after: #e7c14a; --bad: #e08178; --good: #8fcf9c; color-scheme: dark }}
body {{ background: var(--bg); color: var(--fg); font: 15px/1.55 var(--sans); }}
main {{ max-width: 980px; margin: 0 auto; padding-block: 32px 64px;
  padding-left: max(16px, env(safe-area-inset-left, 0px)); padding-right: max(16px, env(safe-area-inset-right, 0px)); }}
h1 {{ font: 600 2.4rem/1.1 var(--serif); margin: 0 0 4px; text-wrap: balance; color: var(--violet); }}
h2 {{ font: 600 1.5rem/1.2 var(--serif); margin: 40px 0 8px; text-wrap: balance; border-bottom: 1px solid var(--rule); padding-bottom: 4px; }}
.sub {{ color: var(--muted); margin: 0 0 24px; max-width: 70ch; }}
p, li {{ max-width: 75ch; }}
.eyebrow {{ font: 500 0.75rem var(--mono); letter-spacing: 0.08em; text-transform: uppercase; color: var(--gold); }}
.scroll {{ overflow-x: auto; }}
table {{ border-collapse: collapse; font-variant-numeric: tabular-nums; }}
table.head th, table.head td {{ padding: 6px 14px 6px 0; border-bottom: 1px solid var(--rule); text-align: right; }}
table.head th:first-child {{ text-align: left; font-weight: 500; }}
table.head thead th {{ font: 500 0.75rem var(--mono); text-transform: uppercase; letter-spacing: .06em; color: var(--muted); }}
table.data {{ font-size: 13px; }}
table.data th, table.data td {{ padding: 4px 10px 4px 0; border-bottom: 1px solid var(--rule); vertical-align: top; text-align: left; }}
table.data thead th {{ font: 500 0.7rem var(--mono); text-transform: uppercase; letter-spacing: .06em; color: var(--muted); }}
td.id {{ font-family: var(--mono); white-space: nowrap; }}
td.t {{ font-family: var(--mono); font-size: 12px; color: var(--muted); }}
.up {{ color: var(--good); }} .down {{ color: var(--bad); }}
svg {{ max-width: none; }}
svg .lbl {{ font: 10px var(--mono); fill: var(--fg); text-anchor: end; }}
svg .val, svg .tick {{ font: 9px var(--mono); fill: var(--muted); }}
svg .tick {{ text-anchor: middle; }}
svg .grid {{ stroke: var(--rule); stroke-width: 1; }}
svg .b {{ fill: var(--before); }} svg .a {{ fill: var(--after); }}
.key {{ display: flex; gap: 18px; font: 12px var(--mono); color: var(--muted); flex-wrap: wrap; margin: 6px 0 10px; }}
.sw {{ display: inline-block; width: 12px; height: 8px; margin-right: 6px; vertical-align: middle; }}
code {{ font-family: var(--mono); font-size: .9em; }}
</style>
<main>
<div class="eyebrow">Proscribed Palimpsest · branch multi-target-wills</div>
<h1>Wills with several targets</h1>
<p class="sub">A will used to want one kind of act and refuse everything else. Now each will wants one to five things, the planner pursues the nearest, and {len(conv)} specific crafts and lores joined the wills. Measured headless with <code>--will-audit</code>: every location factory the audits sweep, three location ids (one for a town or city), morning and night, an actor carrying nothing.</p>

<h2>Headline</h2>
<div class="scroll"><table class="head"><thead><tr><th></th><th>before</th><th>after</th><th>change</th></tr></thead><tbody>
{row("Related coverage, all measured wills", hb["related"], ha["related"])}
{row(f"Related coverage, the {ho['n']} wills that existed before", hb["related"], ho["related"])}
<tr><th>Related coverage, the {hn['n']} new wills</th><td>—</td><td>{pct(hn["related"])}</td><td></td></tr>
{row("Coverage from a location's entry, all wills", hb["cover"], ha["cover"])}
{row("Location kinds with something to do (mean breadth)", hb["kinds"], ha["kinds"])}
{row("Targets per will", hb["targets"], ha["targets"], fmt=lambda x: f"{x:.2f}")}
{row("Wills in the catalogue", shares.get("catalogue_before", 0), shares.get("catalogue_after", 0))}
{row("Wills among the lessons the offered acts teach", shares.get("learned_before", 0), shares.get("learned_after", 0))}
</tbody></table></div>
<p>Related coverage is the share of starts, from an area of a kind the will declares it belongs in, from which the planner finds a way to something it wants. It is the playtest complaint measured. Wills at 60% or more in their own places: <b>{hb['good']} of {hb['withp']}</b> before, <b>{ha['good']} of {ha['withp']}</b> after. The audit fails under 60% overall, and fails any will with several targets that reaches more than 75% of location kinds.</p>
{"".join(f"<p>{esc(n)}</p>" for n in notes)}

<h2>The playtest complaints</h2>
<div class="scroll">{will_table([i for i in ["herblore", "mycology", "simpling", "apothecary_nose", "brawling", "felling", "berrying", "nutting", "jailcraft"] if i in A and i in B], ["will", "related before", "related after", "breadth before", "breadth after", "targets now"])}</div>

<h2>Related coverage per will</h2>
<div class="key"><span><span class="sw" style="background:var(--before)"></span>before</span><span><span class="sw" style="background:var(--after)"></span>after</span><span>✦ new will</span></div>
<div class="scroll">{chart}</div>

<h2>Modi mentis made wills</h2>
<p>{len(conv)} modi mentis whose persona wants recognisable things. The generic ones stay ordinary minds: {esc(", ".join(kept_ordinary))}.</p>
<div class="scroll"><table class="data"><thead><tr><th>modus mentis</th><th>was</th><th>targets, in order of preference</th><th>related</th></tr></thead><tbody>{conv_rows}</tbody></table></div>

<h2>Existing wills given new targets</h2>
<div class="scroll">{will_table(changed, ["will", "related before", "related after", "breadth before", "breadth after", "targets now"])}</div>

<h2>Content added</h2>
<ul>{"".join(f"<li>{esc(c)}</li>" for c in content)}</ul>

<h2>Refusals in play</h2>
{refusals or "<p>Not measured.</p>"}

<h2>What got worse</h2>
<p>Original wills whose related coverage fell by more than two points:</p>
<div class="scroll">{will_table(worse, ["will", "related before", "related after", "breadth before", "breadth after", "targets now"])}</div>
{"".join(f"<p>{esc(w)}</p>" for w in extras.get("worse_notes", []))}
</main>
"""
print(page)
