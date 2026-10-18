"""Audits modus mentis descriptions through the choice judge, with no hand labels.

    python tools/judge/mm_audit.py [--ids spite,fording] [--out audit.jsonl]

Three probes, each read with the game's own prompts (judge_lib):

  SELF   every modus mentis: shown its own description and five ways of working (its own SkillMeans
         and four drawn from others), does the judge pick its own? A miss means the description
         points somewhere other than what the modus mentis does.
  OWN    action modi mentis: asked how far an act calling for its own means belongs to it, does the
         willingness judge assent? A dissent means the manner contradicts the use - typically a
         violent or criminal craft whose tone apologises for itself, or a description using the
         scale's own words ("refusal"). Confirm a flag with a concrete act before editing: "an act
         that calls for <means>" reads awkwardly, and two of three flags on the first run were that.
  GOAL   thinking modi mentis: one everyday scene (a stranger by a well) and eight everyday goals.
         There is no right answer, so nothing is flagged: the picks are listed by goal, to be read.
         A mind sitting where its nature would not put it - Spite choosing "sit and wait" because its
         description said it "waits" - is the word-pull this probe exists to show.

A pass is necessary, not sufficient: these probes catch a description that misleads in general. A
description that misleads in one decision only shows in a labelled case (cases.py, judge_bench.py).
About 2 passes per modus mentis; the whole set takes over an hour on CPU, so pass --ids after an edit.
"""
import argparse, collections as co, json, os, random, sys
sys.path.insert(0, os.path.dirname(__file__))
import judge_lib as J

SELF_SYS = """You are the FACULTY READER. You are neutral and analytical, with no personality of your own.

A character's mind is made of faculties, each with its own nature, concerns and skills. You are given the description of ONE faculty and a lettered list of ways of working. Decide which way of working is this faculty's own.

Guidelines:
- Judge by what the description says the faculty does, knows and cares about.
- Answer with the letter of your choice."""
GOALS = ["greet the stranger", "provoke the stranger", "pickpocket the stranger", "examine the stranger closely",
         "sit and wait", "walk away to the road", "listen to the stranger", "attack the stranger"]
SCENE = "You are in a village, in the market square. Your attention is on a stranger by the well."

ap = argparse.ArgumentParser()
ap.add_argument("--ids")
ap.add_argument("--out")
args = ap.parse_args()

MM = J.load_modi_mentis()
ids = sorted(MM) if not args.ids else args.ids.split(",")
rng = random.Random(5)
out = open(args.out, "w", encoding="utf-8") if args.out else None
flags, goals = [], co.defaultdict(list)
for k in ids:
    m = MM[k]
    rec = dict(id=k)
    others = rng.sample([x for x in sorted(MM) if x != k and MM[x]["means"] != m["means"]], 4)
    opts = [k] + others; rng.shuffle(opts)
    prompt = (f"The faculty: {J.describe(m)}\n\nWhich way of working is this faculty's own?\n"
              + "".join(f"{J.L[i]} - {MM[o]['means']}\n" for i, o in enumerate(opts)) + "\nAnswer with the letter and nothing else.")
    lp = J.letter_logprobs(None, prompt, 5, system=SELF_SYS)
    own = opts.index(k)
    rec["self"] = lp[own] - J.lse([x for i, x in enumerate(lp) if i != own])
    if rec["self"] <= 0:
        flags.append(f"SELF  {k:24} took '{MM[opts[max(range(5), key=lp.__getitem__)]]['means']}' for its own")
    if "Action" in m["functions"]:
        lp = J.letter_logprobs("Willingness", J.build_prompt("Willingness", m,
                               f"You are considering an act that calls for {m['means']}.", J.SCALE), 4)
        rec["own"] = J.willingness(lp)
        if rec["own"] in ("reluctant", "unwilling"):
            flags.append(f"OWN   {k:24} is {rec['own']} to do its own work ({m['means']})")
    if "Thinking" in m["functions"]:
        g = GOALS[:]; rng.shuffle(g); g.append("do something else entirely")
        lp = J.letter_logprobs("Goal", J.build_prompt("Goal", m, SCENE, g), len(g))
        rec["goal"] = g[J.decide("Goal", lp, len(g) - 1)]
        goals[rec["goal"]].append(k)
    if out: out.write(json.dumps(rec) + "\n"); out.flush()
    print(".", end="", flush=True)

print(f"\n\n{len(flags)} flagged of {len(ids)}")
for f in flags: print("  " + f)
if goals:
    print("\nGOAL probe (read for minds placed where their nature would not put them):")
    for g, who in sorted(goals.items(), key=lambda kv: -len(kv[1])):
        print(f"  {g:28} {len(who):3}  {', '.join(who)}")
