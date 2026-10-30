"""Builds the before/after report (tools/prompts/report/index.html) from the final measurement.

    python tools/prompts/report.py --samples samples/final.jsonl --scores scores/final.jsonl \
        --old base --new <final version>

The page shows, per request kind, every final case: what the game supplied, the old and the new
outputs side by side (all seeds), each with its blind scores and the facts it invented or lost, and the
prompt's token count and latency. Held-out cases and loosened personas are marked; cases where the new
wording scored worse are marked too. Above that: a summary per criterion, per split, and the judges'
accuracy tables.
"""
import argparse, collections, html, json, os, statistics as st

HERE = os.path.dirname(os.path.abspath(__file__))
KINDS = ["Observation", "Reasoning", "Action", "Outcome", "Emotion", "DialogueReplica", "Speaking"]
KIND_LABEL = {"DialogueReplica": "Dialogue line", "Speaking": "Speaking to a companion"}


def load(p):
    return [json.loads(l) for l in open(p, encoding="utf-8") if l.strip()]


def esc(s):
    return html.escape(str(s if s is not None else ""))


def mean(xs):
    xs = [x for x in xs if x is not None]
    return st.mean(xs) if xs else None


def fmt(x, nd=2):
    return "–" if x is None else f"{x:.{nd}f}"


def summarise(rows):
    return dict(n=len(rows), F=mean(r["F"] for r in rows), F0=sum(r["F"] == 0 for r in rows),
                F2=(sum(r["F"] == 2 for r in rows) / len(rows)) if rows else None,
                P=mean(r["P"] for r in rows), C=mean(r["C"] for r in rows),
                Q=mean(r["Q"] for r in rows), tok=mean(r["user_tokens"] for r in rows),
                ptok=mean(r.get("prompt_tokens") for r in rows), ms=mean(r.get("prompt_ms") for r in rows),
                wall=mean(r.get("wall") for r in rows))


def delta_cell(old, new, better_high=True, nd=2, pct=False):
    if old is None or new is None:
        return f"<td>{fmt(old, nd)}</td><td>{fmt(new, nd)}</td><td></td>"
    d = new - old
    good = (d > 0) == better_high if abs(d) > 1e-9 else None
    cls = "" if good is None else ("up" if good else "down")
    f = (lambda x: f"{100 * x:.0f}%") if pct else (lambda x: fmt(x, nd))
    sign = "+" if d > 0 else ""
    return f"<td>{f(old)}</td><td>{f(new)}</td><td class='{cls}'>{sign}{f(d) if not pct else f'{100 * d:.0f} pts'}</td>"


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--samples", nargs="+", required=True)
    ap.add_argument("--scores", nargs="+", required=True)
    ap.add_argument("--old", default="base")
    ap.add_argument("--new", required=True)
    ap.add_argument("--judge-old", default=os.path.join(HERE, "results", "judge_base.txt"))
    ap.add_argument("--judge-new")
    ap.add_argument("--critic-old", nargs="*", default=[])
    ap.add_argument("--critic-new", nargs="*", default=[])
    ap.add_argument("--timing", help="samples measured on a quiet machine, for latency")
    ap.add_argument("--notes", help="a short HTML fragment for the top of the page")
    ap.add_argument("--out", default=os.path.join(HERE, "report", "index.html"))
    a = ap.parse_args()

    cases = {c["id"]: c for c in load(os.path.join(HERE, "cases.jsonl"))}
    samples = {(s["case"], s["version"], s["seed"]): s for p in a.samples for s in load(p)
               if s["version"] in (a.old, a.new)}
    scores = {(r["case"], r["version"], r["seed"]): r for p in a.scores for r in load(p)
              if r["version"] in (a.old, a.new)}
    rows = []
    for k, s in samples.items():
        if k in scores and k[0] in cases:
            rows.append(dict(s, **{x: scores[k][x] for x in ("F", "P", "C", "Q", "facts")},
                             kind=cases[k[0]]["record"]["kind"], split=cases[k[0]]["split"],
                             variant=cases[k[0]]["variant"], persona=cases[k[0]]["persona"]["id"]))
    by = collections.defaultdict(list)
    for r in rows:
        by[(r["version"], r["split"], r["kind"])].append(r)
        by[(r["version"], r["split"], "ALL")].append(r)
        by[(r["version"], "ALL", r["kind"])].append(r)
        by[(r["version"], "ALL", "ALL")].append(r)
        by[(r["version"], "persona:" + r["variant"], "ALL")].append(r)

    # ── summary tables ─────────────────────────────────────────────────────────────────────────────
    def criteria_table(split):
        o, n = summarise(by[(a.old, split, "ALL")]), summarise(by[(a.new, split, "ALL")])
        if not o["n"] or not n["n"]:
            return ""
        return f"""<table class="crit"><thead><tr><th>Criterion</th><th>old</th><th>new</th><th>change</th></tr></thead><tbody>
<tr><th>1 · Fidelity, mean (0–2)</th>{delta_cell(o['F'], n['F'])}</tr>
<tr><th>1 · Samples with an invented or lost fact that breaks the meaning (F = 0)</th>{delta_cell(o['F0'] / o['n'], n['F0'] / n['n'], better_high=False, pct=True)}</tr>
<tr><th>1 · Samples fully faithful (F = 2)</th>{delta_cell(o['F2'], n['F2'], pct=True)}</tr>
<tr><th>2 · Persona (0–2)</th>{delta_cell(o['P'], n['P'])}</tr>
<tr><th>3 · Prompt tokens per request (user message)</th>{delta_cell(o['tok'], n['tok'], better_high=False, nd=0)}</tr>
<tr><th>4 · Use of optional context (0–1, where it applied)</th>{delta_cell(o['C'], n['C'])}</tr>
<tr><th>5 · Quality of writing (0–2)</th>{delta_cell(o['Q'], n['Q'])}</tr>
</tbody></table><p class="small">{o['n']} old and {n['n']} new samples.</p>"""

    def kind_table():
        out = ["<table class='kinds'><thead><tr><th>Kind</th><th>split</th><th>n</th>"
               "<th colspan=3>Fidelity</th><th colspan=3>F = 0 share</th><th colspan=3>Persona</th>"
               "<th colspan=3>Writing</th><th colspan=3>tokens</th></tr>"
               "<tr><th></th><th></th><th></th>" + "<th>old</th><th>new</th><th>Δ</th>" * 5 + "</tr></thead><tbody>"]
        for kind in KINDS:
            for split in ("dev", "heldout"):
                o, n = summarise(by[(a.old, split, kind)]), summarise(by[(a.new, split, kind)])
                if not o["n"] or not n["n"]:
                    continue
                out.append(f"<tr><th>{esc(KIND_LABEL.get(kind, kind))}</th><td>{split}</td><td>{o['n']}</td>"
                           + delta_cell(o["F"], n["F"])
                           + delta_cell(o["F0"] / o["n"], n["F0"] / n["n"], better_high=False, pct=True)
                           + delta_cell(o["P"], n["P"]) + delta_cell(o["Q"], n["Q"])
                           + delta_cell(o["tok"], n["tok"], better_high=False, nd=0) + "</tr>")
        out.append("</tbody></table>")
        return "".join(out)

    def persona_table():
        out = ["<table class='kinds'><thead><tr><th>Persona</th><th>n</th><th colspan=3>Fidelity</th>"
               "<th colspan=3>Persona</th></tr><tr><th></th><th></th>" + "<th>old</th><th>new</th><th>Δ</th>" * 2
               + "</tr></thead><tbody>"]
        for v, label in [("own", "The persona the game used"), ("swap", "Another modus mentis"),
                         ("loose", "A deliberately loosened persona")]:
            o, n = summarise(by[(a.old, "persona:" + v, "ALL")]), summarise(by[(a.new, "persona:" + v, "ALL")])
            if o["n"] and n["n"]:
                out.append(f"<tr><th>{label}</th><td>{o['n']}</td>{delta_cell(o['F'], n['F'])}{delta_cell(o['P'], n['P'])}</tr>")
        out.append("</tbody></table>")
        return "".join(out)

    def timing_block():
        if not a.timing:
            return ""
        t = load(a.timing)
        def agg(v):
            ts = [x for x in t if x["version"] == v]
            return dict(n=len(ts), tok=mean(x["user_tokens"] for x in ts), ptok=mean(x["prompt_tokens"] for x in ts),
                        pn=mean(x["prompt_n"] for x in ts), pms=mean(x["prompt_ms"] for x in ts),
                        out=mean(x["predicted_n"] for x in ts), wall=mean(x["wall"] for x in ts))
        o, n = agg(a.old), agg(a.new)
        return f"""<table class="crit"><thead><tr><th>Speed, measured alone on 4 CPU cores</th><th>old</th><th>new</th><th>change</th></tr></thead><tbody>
<tr><th>user-message tokens</th>{delta_cell(o['tok'], n['tok'], better_high=False, nd=0)}</tr>
<tr><th>whole request, tokens (persona + history + message)</th>{delta_cell(o['ptok'], n['ptok'], better_high=False, nd=0)}</tr>
<tr><th>tokens the server had to read (not cached)</th>{delta_cell(o['pn'], n['pn'], better_high=False, nd=0)}</tr>
<tr><th>prompt reading, ms</th>{delta_cell(o['pms'], n['pms'], better_high=False, nd=0)}</tr>
<tr><th>tokens written</th>{delta_cell(o['out'], n['out'], better_high=False, nd=1)}</tr>
<tr><th>wall time per request, s</th>{delta_cell(o['wall'], n['wall'], better_high=False, nd=2)}</tr>
</tbody></table><p class="small">{o['n']} requests per version, the same cases and seeds, nothing else running.</p>"""

    def judge_table():
        def parse(p):
            out = {}
            if not p or not os.path.exists(p):
                return out
            for line in open(p):
                parts = line.split()
                if len(parts) >= 3 and "/" in parts[-2] and parts[-1].endswith("%"):
                    out[" ".join(parts[:-2])] = (parts[-2], parts[-1])
            return out
        o, n = parse(a.judge_old), parse(a.judge_new)
        if not o:
            return ""
        rows_ = "".join(f"<tr><th>{esc(k)}</th><td>{o[k][0]}</td><td>{o[k][1]}</td>"
                        f"<td>{n.get(k, ('–', '–'))[0]}</td><td>{n.get(k, ('–', '–'))[1]}</td></tr>" for k in o)
        return f"""<table class="crit"><thead><tr><th>ChoiceJudge kind</th><th colspan=2>old</th><th colspan=2>new</th></tr></thead>
<tbody>{rows_}</tbody></table>"""

    def critic_table():
        def acc(paths):
            rs = [r for p in paths for r in load(p)]
            if not rs:
                return {}
            out = {}
            for split in ("dev", "held"):
                for tree in ("ItemAppropriateness", "ToolSubstitution", "ALL"):
                    sel = [r for r in rs if (r["case"].startswith("held") == (split == "held"))
                           and (tree == "ALL" or r["tree"] == tree)]
                    if sel:
                        out[(split, tree)] = (sum(r["ok"] for r in sel), len(sel))
            return out
        o, n = acc(a.critic_old), acc(a.critic_new)
        if not o:
            return ""
        body = ""
        for k in sorted(o):
            ov, nv = o[k], n.get(k)
            body += (f"<tr><th>{'held-out' if k[0] == 'held' else 'dev'} · {k[1]}</th>"
                     f"<td>{ov[0]}/{ov[1]}</td><td>{100 * ov[0] / ov[1]:.1f}%</td>"
                     + (f"<td>{nv[0]}/{nv[1]}</td><td>{100 * nv[0] / nv[1]:.1f}%</td>" if nv else "<td></td><td></td>")
                     + "</tr>")
        return f"""<table class="crit"><thead><tr><th>Item-use critic</th><th colspan=2>old</th><th colspan=2>new</th></tr></thead>
<tbody>{body}</tbody></table>"""

    # ── the cases ────────────────────────────────────────────────────────────────────────────────
    def sample_cell(r):
        bad = " f0" if r["F"] == 0 else ""
        facts = f"<div class='facts'>{esc(r['facts'])}</div>" if r.get("facts") else ""
        return (f"<div class='samp{bad}'><div class='txt'>{esc(r['text'])}</div>"
                f"<div class='sc'>F{r['F']} P{r['P']} C{'–' if r['C'] is None else r['C']} Q{r['Q']}"
                f" · seed {r['seed']}</div>{facts}</div>")

    def case_block(cid):
        c = cases[cid]
        rec = c["record"]
        o = sorted([r for r in rows if r["case"] == cid and r["version"] == a.old], key=lambda r: r["seed"])
        n = sorted([r for r in rows if r["case"] == cid and r["version"] == a.new], key=lambda r: r["seed"])
        if not o or not n:
            return ""
        so, sn = summarise(o), summarise(n)
        worse = (sn["F"] < so["F"]) or (sn["F"] == so["F"] and sn["P"] < so["P"])
        tags = []
        if c["split"] == "heldout":
            tags.append("<span class='tag held'>held out</span>")
        if c["variant"] == "loose":
            tags.append("<span class='tag loose'>loosened persona</span>")
        if c["variant"] == "swap":
            tags.append("<span class='tag swap'>persona swapped in</span>")
        if worse:
            tags.append("<span class='tag worse'>new is worse</span>")
        ctx = [("persona", c["persona"]["id"]), ("reminder", c["persona"].get("reminder2")),
               ("style", c["persona"].get("style")), ("place", rec.get("place")),
               ("speaker", rec.get("speakerName")), ("addressee", rec.get("addressee")),
               ("about", rec.get("dialogueContext")), ("looking at", rec.get("observedOther"))]
        dl = "".join(f"<dt>{k}</dt><dd>{esc(v)}</dd>" for k, v in ctx if v)
        return f"""<article class="case{' isworse' if worse else ''}" data-kind="{rec['kind']}" data-split="{c['split']}" data-worse="{int(worse)}">
<header><h3>{esc(cid)}</h3><div class="tags">{''.join(tags)}</div></header>
<dl>{dl}</dl>
<p class="neutral"><span>neutral line</span>{esc(rec['neutralText'])}</p>
<div class="cols"><div><h4>old <small>F {fmt(so['F'])} · P {fmt(so['P'])} · {fmt(so['tok'], 0)} tok</small></h4>{''.join(sample_cell(r) for r in o)}</div>
<div><h4>new <small>F {fmt(sn['F'])} · P {fmt(sn['P'])} · {fmt(sn['tok'], 0)} tok</small></h4>{''.join(sample_cell(r) for r in n)}</div></div>
</article>"""

    sections = []
    for kind in KINDS:
        ids = sorted({r["case"] for r in rows if r["kind"] == kind},
                     key=lambda i: (cases[i]["split"] != "dev", i))
        blocks = "".join(case_block(i) for i in ids)
        if blocks:
            sections.append(f"<section id='k-{kind}'><h2>{esc(KIND_LABEL.get(kind, kind))}</h2>{blocks}</section>")

    notes = open(a.notes).read() if a.notes else ""
    page = TEMPLATE.format(
        notes=notes, crit_all=criteria_table("ALL"), crit_dev=criteria_table("dev"),
        crit_held=criteria_table("heldout"), timing=timing_block(), kinds=kind_table(), personas=persona_table(),
        judge=judge_table(), critic=critic_table(), sections="".join(sections),
        nav="".join(f"<a href='#k-{k}'>{esc(KIND_LABEL.get(k, k))}</a>" for k in KINDS))
    os.makedirs(os.path.dirname(a.out), exist_ok=True)
    open(a.out, "w", encoding="utf-8").write(page)
    print("wrote", a.out, len(page) // 1024, "KB")


TEMPLATE = """<title>Persona Prompt Overhaul</title>
<meta name="claude:viewport-fit-sides" content="cover">
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;600&family=Source+Serif+4:opsz,wght@8..60,400;8..60,600&display=swap">
<style>
/* Layout: one reading column; cases as old/new pairs; the game's own yellow/purple/grey. */
:root {{
  --bg:#f6f5f1; --panel:#ffffff; --ink:#1d1b22; --dim:#6b6774; --rule:#dcd8e0;
  --accent:#6b3fa0; --gold:#a07800; --good:#2f7a3e; --bad:#b23a3a; --bad-bg:#fbeeee;
  --mono:"IBM Plex Mono",Consolas,ui-monospace,monospace; --serif:"Source Serif 4",Georgia,serif;
}}
@media (prefers-color-scheme: dark) {{ :root:not([data-theme="light"]) {{
  --bg:#16141a; --panel:#1f1c25; --ink:#ebe8f0; --dim:#a29dac; --rule:#36313f;
  --accent:#b993e8; --gold:#e3c14c; --good:#7fcf8c; --bad:#f08a8a; --bad-bg:#3a2226; color-scheme:dark }} }}
:root[data-theme="dark"] {{
  --bg:#16141a; --panel:#1f1c25; --ink:#ebe8f0; --dim:#a29dac; --rule:#36313f;
  --accent:#b993e8; --gold:#e3c14c; --good:#7fcf8c; --bad:#f08a8a; --bad-bg:#3a2226; color-scheme:dark }}
body {{ background:var(--bg); color:var(--ink); font:16px/1.55 var(--serif);
  padding-left:max(16px,env(safe-area-inset-left,0px)); padding-right:max(16px,env(safe-area-inset-right,0px)); padding-block:24px; }}
main {{ max-width:1100px; margin:0 auto; }}
h1 {{ font:600 2rem/1.15 var(--serif); margin:0 0 .25rem; text-wrap:balance; }}
h1 small {{ display:block; font:400 .8rem var(--mono); color:var(--gold); letter-spacing:.08em; text-transform:uppercase; margin-bottom:.4rem; }}
h2 {{ font:600 1.35rem var(--serif); margin:2.5rem 0 1rem; border-bottom:1px solid var(--rule); padding-bottom:.3rem; }}
h3 {{ font:600 .85rem var(--mono); margin:0; color:var(--accent); overflow-wrap:anywhere; }}
h4 {{ font:600 .8rem var(--mono); margin:.2rem 0 .4rem; text-transform:uppercase; letter-spacing:.06em; color:var(--dim); }}
h4 small {{ text-transform:none; letter-spacing:0; font-weight:400; }}
p {{ max-width:68ch; }}
.small {{ font:.8rem var(--mono); color:var(--dim); }}
nav {{ display:flex; flex-wrap:wrap; gap:.5rem 1rem; font:.8rem var(--mono); margin:1rem 0; }}
nav a {{ color:var(--accent); }}
.wrap {{ overflow-x:auto; }}
table {{ border-collapse:collapse; font:.82rem var(--mono); font-variant-numeric:tabular-nums; margin:.5rem 0; }}
th, td {{ padding:.3rem .55rem; border-bottom:1px solid var(--rule); text-align:right; }}
th {{ text-align:left; font-weight:600; }}
thead th {{ color:var(--dim); font-weight:400; }}
td.up {{ color:var(--good); font-weight:600; }} td.down {{ color:var(--bad); font-weight:600; }}
.grid {{ display:grid; grid-template-columns:repeat(auto-fit,minmax(320px,1fr)); gap:1rem 2rem; }}
.grid > div {{ min-width:0; }}
.filters {{ display:flex; flex-wrap:wrap; gap:.5rem 1rem; font:.8rem var(--mono); margin:.5rem 0 1rem; }}
.filters label {{ display:flex; gap:.3rem; align-items:center; }}
.case {{ background:var(--panel); border:1px solid var(--rule); border-radius:4px; padding:1rem; margin:0 0 1rem; }}
.case.isworse {{ border-color:var(--bad); }}
.case header {{ display:flex; flex-wrap:wrap; gap:.5rem; justify-content:space-between; align-items:baseline; }}
.tag {{ font:.7rem var(--mono); padding:.1rem .4rem; border-radius:3px; border:1px solid var(--rule); color:var(--dim); }}
.tag.held {{ color:var(--gold); border-color:var(--gold); }} .tag.worse {{ color:var(--bad); border-color:var(--bad); }}
.tag.loose {{ color:var(--accent); border-color:var(--accent); }}
dl {{ display:grid; grid-template-columns:max-content 1fr; gap:.1rem .8rem; font:.78rem var(--mono); margin:.6rem 0; }}
dt {{ color:var(--dim); }} dd {{ margin:0; overflow-wrap:anywhere; }}
.neutral {{ font-size:.95rem; border-left:3px solid var(--gold); padding-left:.7rem; max-width:none; }}
.neutral span {{ display:block; font:.7rem var(--mono); color:var(--dim); text-transform:uppercase; letter-spacing:.06em; }}
.cols {{ display:grid; grid-template-columns:1fr 1fr; gap:1rem; }}
.cols > div {{ min-width:0; }}
@media (max-width:700px) {{ .cols {{ grid-template-columns:1fr; }} }}
.samp {{ border-top:1px solid var(--rule); padding:.45rem 0; }}
.samp.f0 .txt {{ background:var(--bad-bg); }}
.txt {{ font-size:.95rem; }}
.sc {{ font:.72rem var(--mono); color:var(--dim); margin-top:.15rem; }}
.facts {{ font:.72rem var(--mono); color:var(--bad); }}
</style>
<main>
<h1><small>Proscribed Palimpsest · modi mentis</small>Persona prompt overhaul: before and after</h1>
{notes}
<nav>{nav}<a href="#judges">Judges</a></nav>
<h2>Summary</h2>
<div class="grid"><div><h4>all cases</h4><div class="wrap">{crit_all}</div></div>
<div><h4>held-out playthroughs only</h4><div class="wrap">{crit_held}</div></div></div>
<div class="wrap">{timing}</div>
<h2>Per request kind</h2><div class="wrap">{kinds}</div>
<h2>Per kind of persona</h2><div class="wrap">{personas}</div>
<h2 id="judges">Judges: decision accuracy</h2>
<div class="grid"><div class="wrap">{judge}</div><div class="wrap">{critic}</div></div>
<h2>The cases</h2>
<div class="filters"><label><input type="checkbox" id="onlyworse"> only cases where the new wording is worse</label>
<label><input type="checkbox" id="onlyheld"> only held-out cases</label></div>
{sections}
</main>
<script>
const w=document.getElementById('onlyworse'), h=document.getElementById('onlyheld');
function f(){{document.querySelectorAll('.case').forEach(c=>{{c.hidden=(w.checked&&c.dataset.worse!=='1')||(h.checked&&c.dataset.split!=='heldout');}});}}
w.addEventListener('change',f); h.addEventListener('change',f);
</script>
"""

if __name__ == "__main__":
    main()
