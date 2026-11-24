"""Builds report/index.html: the experiment's numbers and every sample side by side.

    python report.py
"""
import collections, html, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import person_lib as L      # noqa: E402

res = json.load(open(os.path.join(HERE, "results.json")))
res2 = json.load(open(os.path.join(HERE, "results_r2.json")))
sc2 = {(s["case"], s["version"], s["seed"]): s for s in (json.loads(l) for l in open(os.path.join(HERE, "scored_r2.jsonl")))}
met = json.load(open(os.path.join(HERE, "metrics.json")))
sc = {(s["case"], s["version"], s["seed"]): s for s in (json.loads(l) for l in open(os.path.join(HERE, "scored.jsonl")))}
timing = [json.loads(l) for l in open(os.path.join(HERE, "samples", "timing.jsonl"))]
cases = {c["id"]: c for c in (json.loads(l) for l in open(os.path.join(HERE, "..", "cases.jsonl")))}

CRIT = [("F", "No hallucination", 2), ("I", "Info transmitted", 2), ("K", "Kind's task done", 2),
        ("P", "Persona expressed", 2), ("C", "Optional context", 1), ("R", "No repetition / echo", 2),
        ("Q", "Writing", 2)]
VLABEL = {"first": "First person", "second": "Second person", "second_tn": "Second, line transposed",
          "relation": "Spoken to the character (relation)"}
V1 = ("first", "second", "second_tn")
V2 = ("first", "second_tn", "relation")
e = html.escape


def cell_diff(group, v, c, R=None):
    R = R or res
    d = R[group].get("diff_" + v, {}).get(c)
    if not d:
        return "<td>–</td>"
    sig = d["lo"] > 0 or d["hi"] < 0
    cls = "sig up" if sig and d["d"] > 0 else "sig down" if sig else "flat"
    if c == "F0":
        cls = cls.replace("up", "tmp").replace("down", "up").replace("tmp", "down")
    return (f'<td class="{cls}"><b>{d["d"]:+.2f}</b><small>[{d["lo"]:+.2f}, {d["hi"]:+.2f}]</small></td>')


def crit_table(group, R=None, vs=V1):
    R = R or res
    g = R[group]
    rows = []
    for c, name, mx in CRIT + [("F0", "Meaning broken (F = 0)", None)]:
        vals = "".join(
            f"<td>{(100 * g[v][c]):.0f}%</td>" if c == "F0" else f"<td>{g[v][c]:.2f}<small>/{mx}</small></td>"
            for v in vs)
        rows.append(f"<tr><th>{name}</th>{vals}" + "".join(cell_diff(group, v, c, R) for v in vs[1:]) + "</tr>")
    head = ("<tr><th></th>" + "".join(f"<th>{VLABEL[v]}</th>" for v in vs)
            + "".join(f"<th>{ {'second': 'Second', 'second_tn': 'Transposed', 'relation': 'Relation'}[v]} − first</th>" for v in vs[1:]) + "</tr>")
    return f'<div class="scroll"><table class="num"><thead>{head}</thead><tbody>{"".join(rows)}</tbody></table></div>'


def kind_table(R=None, vs=V1, short=("1st", "2nd", "2nd T")):
    R = R or res
    rows = []
    for k in L.KINDS:
        g = R[k]
        n = g["first"]["n"] // 2
        cells = []
        for c in ["P", "I", "R", "F0"]:
            for v in vs:
                x = g[v][c]
                cells.append(f"<td>{100 * x:.0f}%</td>" if c == "F0" else f"<td>{x:.2f}</td>")
        rows.append(f"<tr><th>{k} <small>{n} cases</small></th>{''.join(cells)}</tr>")
    sub = "".join(f"<th>{s}</th>" for s in list(short) * 4)
    head = ('<tr><th rowspan="2"></th><th colspan="3">Persona</th><th colspan="3">Info kept</th>'
            '<th colspan="3">No echo</th><th colspan="3">Meaning broken</th></tr><tr>' + sub + "</tr>")
    return f'<div class="scroll"><table class="num kinds"><thead>{head}</thead><tbody>{"".join(rows)}</tbody></table></div>'


def mech_table():
    s = met["summary"]
    keys = [("voice_ok", "Voice as asked", True), ("voice_slip", "Fell back to “I”", True), ("recall", "Content words kept", True),
            ("parrot", "4-grams copied from the line", True), ("counsel", "Counsel or imperative", True),
            ("copy_style", "Style phrase copied", True), ("lesson_lost", "Lesson lost (outcomes, emotions)", True),
            ("words", "Words per answer", False), ("tok", "Prompt tokens (user message)", False)]
    rows = []
    for k, name, pct in keys:
        tds = []
        for v in L.VERSIONS:
            x = s[f"all|{v}"].get(k)
            tds.append("<td>–</td>" if x is None else f"<td>{100 * x:.0f}%</td>" if pct else f"<td>{x:.0f}</td>")
        rows.append(f"<tr><th>{name}</th>{''.join(tds)}</tr>")
    # sequential timing
    tds = []
    for v in L.VERSIONS:
        ts = [t for t in timing if t["version"] == v]
        tds.append(f"<td>{sum(t['wall'] for t in ts) / len(ts):.1f} s</td>")
    rows.append(f"<tr><th>Wall time per request <small>sequential, {len(timing) // 3} cases</small></th>{''.join(tds)}</tr>")
    tds = []
    for v in L.VERSIONS:
        ts = [t for t in timing if t["version"] == v]
        tds.append(f"<td>{sum(t['predicted_n'] for t in ts) / len(ts):.0f}</td>")
    rows.append(f"<tr><th>Tokens generated</th>{''.join(tds)}</tr>")
    head = "<tr><th></th>" + "".join(f"<th>{VLABEL[v]}</th>" for v in L.VERSIONS) + "</tr>"
    return f'<div class="scroll"><table class="num"><thead>{head}</thead><tbody>{"".join(rows)}</tbody></table></div>'


def samples_json():
    by = collections.defaultdict(lambda: collections.defaultdict(list))
    meta = {}
    for s in met["samples"]:
        k = (s["case"], s["seed"])
        q = (sc2 if s["version"] == "relation" else sc).get((s["case"], s["version"], s["seed"]), {})
        by[s["case"]][s["version"]].append(dict(seed=s["seed"], text=s["text"],
                                                sc={c: q.get(c) for c in "FIKPCRQ"}, facts=q.get("facts", "")))
        if s["version"] == "first":
            c = cases[s["case"]]
            p = L.current_persona(c)
            meta[s["case"]] = dict(kind=s["kind"], set=s["set"], persona=p["id"], reminder=p.get("reminder2"),
                                   style=p.get("style"), place=c["record"].get("place"), neutral=s["neutral"],
                                   loose=":loose:" in s["case"])
    out = []
    for cid, vs in by.items():
        out.append(dict(id=cid, **meta[cid], v={v: sorted(x, key=lambda y: y["seed"]) for v, x in vs.items()}))
    out.sort(key=lambda x: (L.KINDS.index(x["kind"]), x["set"], x["id"]))
    return json.dumps(out)


page = open(os.path.join(HERE, "report_template.html"), encoding="utf-8").read()
page = (page.replace("{{CRIT_ALL}}", crit_table("all"))
            .replace("{{CRIT_HELD}}", crit_table("heldout"))
            .replace("{{KINDS}}", kind_table())
            .replace("{{CRIT_R2}}", crit_table("all", res2, V2))
            .replace("{{CRIT_R2_HELD}}", crit_table("heldout", res2, V2))
            .replace("{{KINDS_R2}}", kind_table(res2, V2, ("1st", "2nd T", "Rel")))
            .replace("{{MECH}}", mech_table())
            .replace("{{DATA}}", samples_json().replace("</", "<\\/")))
os.makedirs(os.path.join(HERE, "report"), exist_ok=True)
open(os.path.join(HERE, "report", "index.html"), "w", encoding="utf-8").write(page)
print("report/index.html", len(page) // 1024, "KB")
