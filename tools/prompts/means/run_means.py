"""How a thinking modus mentis names the means it hands an act to.

The reasoning's neutral line ends "I will rely on {SkillMeans}." (NeutralNarration.ReasoningChain).
In play the rewrite either parrots that description ("relying on long heavy bodily work") or bends it
into nonsense ("I shall introduce myself with fire-making"). This samples wordings of that last
sentence (and of the Reasoning instruction) on the corpus's recorded reasoning cases, everything else
exactly as the game sends it (prompt_lib).

    python tools/prompts/means/run_means.py --variants base,name,... --seeds 1,2 --out tools/prompts/means/samples.jsonl
"""
import argparse, json, os, re, sys
HERE = os.path.dirname(__file__)
sys.path.insert(0, os.path.join(HERE, ".."))
import prompt_lib as P
import run_samples as RS

MEANS = json.load(open(os.path.join(HERE, "means_map.json")))
RX = re.compile(r"^(?P<head>.*?)(?: I want to (?P<goal>.*?)\.)? I will rely on (?P<means>.*)\.$")


def parts(nt):
    m = RX.match(nt)
    return m.group("head"), m.group("goal"), m.group("means")


def name_of(means):
    return MEANS[means]["name"].strip().lower()


# Each variant: (head, goal, means, name) -> the neutral line.
SENTENCES = {
    "base":      lambda h, g, m, n: f"{h} I want to {g}. I will rely on {m}.",
    # the means named as the faculty it is: a part of the same mind the act is handed to
    "name":      lambda h, g, m, n: f"{h} I want to {g}. I will leave it to my {n}: {m}.",
    # the faculty's name alone, its description dropped
    "name_only": lambda h, g, m, n: f"{h} I want to {g}, and I will leave the doing of it to my {n}.",
    # the means bound to the act in one sentence
    "fused":     lambda h, g, m, n: f"{h} I want to {g}, by way of {m}.",
    # the means as a manner of doing this act
    "manner":    lambda h, g, m, n: f"{h} I want to {g}. I will do it with {m}.",
    # the faculty's name, shorter
    "name_ref":  lambda h, g, m, n: f"{h} I want to {g}, and I will leave it to my {n}.",
    # no means in the line at all: "aside" puts it outside the quoted sentence (see make_variant)
    "aside":     lambda h, g, m, n: f"{h} I want to {g}, and I will leave it to my {n}.",
    "none":      lambda h, g, m, n: f"{h} I want to {g}.",
    # the name as a part of the speaker, so "peasantry" or "petty thief" cannot read as people
    "inme":      lambda h, g, m, n: f"{h} I want to {g}, and I will leave it to the {n} in me.",
}

ASIDE = ("\n\nYour {n} is the part of you that will do it: {m}. Mention it in your own way if you like; "
         "it describes you, not anything in the scene.")

BASE_INSTR = P.T0["Instruction.Reasoning"]
INSTRUCTIONS = {
    "": BASE_INSTR,
    # tell the voice what to do with the means, rather than only what to keep
    "+how": BASE_INSTR + " Say how that way of doing it suits this act, in your own words, without "
            "repeating its description.",
    "+own": BASE_INSTR + " The way you will do it is yours to put in your own words: never repeat "
            "its description.",
}


_ORIG = [None]


def make_variant(spec):
    sent, _, instr = spec.partition("+")
    instr = "+" + instr if instr else ""
    build = SENTENCES[sent]

    def fix(r):
        h, g, m = parts(r["neutralText"])
        if g is None:
            return r
        return dict(r, neutralText=build(h, g, m, name_of(m)))
    T = P.variant(neutral_fix=[fix])
    T["Instruction.Reasoning"] = INSTRUCTIONS[instr]
    if sent == "aside":
        def build_aside(T2, r, _T0=dict(T)):
            _T0.pop("build", None)
            prompt = P.build_prompt(dict(_T0, neutral_fix=[]), r)
            h, g, m = _ORIG[0]
            head, sep, tail = prompt.partition("\n\n")
            return head + ASIDE.format(n=name_of(m), m=m) + sep + tail
        def fix2(r):
            _ORIG[0] = parts(r["neutralText"])
            return fix(r)
        T["neutral_fix"] = [fix2]
        T["build"] = build_aside
    return T


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--variants", required=True)
    ap.add_argument("--seeds", default="1,2")
    ap.add_argument("--out", required=True)
    ap.add_argument("--limit", type=int, default=0)
    a = ap.parse_args()
    cases = [json.loads(l) for l in open(os.path.join(HERE, "..", "cases.jsonl"), encoding="utf-8")]
    cases = [c for c in cases if c["record"]["kind"] == "Reasoning" and "I will rely on" in c["record"]["neutralText"]
             and parts(c["record"]["neutralText"])[1]]
    if a.limit:
        cases = cases[: a.limit]
    done = set()
    if os.path.exists(a.out):
        for l in open(a.out):
            s = json.loads(l)
            done.add((s["case"], s["version"], s["seed"]))
    out = open(a.out, "a")
    seeds = [int(s) for s in a.seeds.split(",")]
    # case-major so the persona system prompt stays cached across variants
    for i, c in enumerate(cases):
        for v in a.variants.split(","):
            T = make_variant(v)
            r, msgs = RS.request_for(T, c)
            for seed in seeds:
                if (c["id"], v, seed) in done:
                    continue
                g = P.generate(msgs, P.grammar(r), seed)
                text = g.pop("text")
                out.write(json.dumps(dict(case=c["id"], version=v, seed=seed, text=text,
                                          neutral=P.build_prompt(T, dict(c["record"]))[:0] or r["neutralText"],
                                          user_tokens=P.tokens(msgs[-1]["content"]), **g)) + "\n")
                out.flush()
        print(f"{i + 1}/{len(cases)} {c['id']}", flush=True)


if __name__ == "__main__":
    main()
