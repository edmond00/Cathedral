"""Scores the item-use critic (src/game/ItemUseCritic.cs, src/game/CriticTrees.cs) on labelled cases.

The critic is sampled at the game's settings with a grammar that admits only the choice ids, so its
answer to one case is a distribution, not a letter: each case is asked once per seed, and a case's
score is the share of its samples on the labelled side (help / no help). The share that is also the
labelled degree is reported beside it.

Everything is read from the C# at import time: the system prompt, the preamble, both trees'
questions and choices. A variant replaces any of them (see VARIANTS at the bottom).

    python tools/prompts/critic_bench.py [--variant NAME] [--seeds 1,2,3,4,5] [--out file.jsonl]
"""
import argparse, json, os, re, sys
sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P

_crit = P._read("src/game/ItemUseCritic.cs")
_trees = P._read("src/game/CriticTrees.cs")

SYSTEM = re.search(r'GetCriticSystemPrompt\(\) => @"((?:[^"]|"")*)"', _crit).group(1).replace('""', '"')
SETTING_LINE = re.search(r'"(Setting: [^"]*)"', _trees).group(1)


def _tree(name):
    body = _trees[_trees.index(f'name: "{name}"'):]
    q = re.search(r'question: \$"((?:[^"\\]|\\.)*)"', body).group(1)
    choices = re.findall(r'new\("(\w+)",\s*"([^"]*)"(,\s*\n?\s*isFailure: true)?', body[:body.index("});")])
    return P._unescape(q), [(cid, desc, bool(fail)) for cid, desc, fail in choices]


TREES = {"ItemAppropriateness": _tree("ItemAppropriateness"), "ToolSubstitution": _tree("ToolSubstitution")}


def preamble(T, c):
    goal_line = f"The character's goal is to {c['goal']}." if c["goal"] else ""
    item_line = f"The character is using: {c['item_context']}." if c.get("item_context") else ""
    lines = [T["setting_line"], f"The scene: a {c['world']}. The character is {c['node']}.", goal_line, item_line]
    return "\n".join(l for l in lines if l)


def question(T, c):
    tree = "ToolSubstitution" if c.get("tool") else "ItemAppropriateness"
    q, choices = T["trees"][tree]
    text = P._fill(q.replace("{context.BuildPreamble()}", "{pre}"), pre=T["preamble"](T, c), goalText=c["goal"],
                   itemName=c["item"], referenceToolPhrase=c.get("tool") or "")
    return tree, text, choices


def choice_prompt(question_text, choices):
    lines = [question_text, "", "Choose one of the following options. Respond with ONLY the option id, nothing else:"]
    lines += [f"- {cid}: {desc}" if desc else f"- {cid}" for cid, desc, _ in choices]
    return "\n".join(lines).rstrip()


def base():
    return dict(system=SYSTEM, setting_line=SETTING_LINE, trees=TREES, preamble=preamble, prompt=choice_prompt)


# (world, node, goal, item, item context, reference tool or None, label: "help"/"no", degree)
CASES = [
    ("windswept plain", "at the foot of a rocky slope", "climb up the rocky slope", "the rope", "Rope (a coil of hemp rope)", None, "help", None),
    ("forest", "under an old oak", "climb the old oak", "the loaf of bread", "Loaf of Bread (a round brown loaf)", None, "no", None),
    ("village", "at the smithy door", "break open the smithy door", "the hammer", "Hammer (a smith's hammer)", None, "help", "clearly_helps"),
    ("village", "at the smithy door", "break open the smithy door", "the feather", "Feather (a long grey feather)", None, "no", None),
    ("forest", "on the bank of a stream", "cross the stream", "the walking staff", "Walking Staff (a stout ash staff)", None, "help", None),
    ("forest", "on the bank of a stream", "cross the stream", "the prayer book", "Prayer Book (a small leather-bound book)", None, "no", None),
    ("village", "in a cottage", "unlock the chest", "the hairpin", "Hairpin (a bent iron hairpin)", None, "help", None),
    ("village", "in a cottage", "unlock the chest", "the apple", "Apple (a red apple)", None, "no", None),
    ("farm", "in the stable", "calm the frightened horse", "the apple", "Apple (a red apple)", None, "help", None),
    ("farm", "in the stable", "calm the frightened horse", "the hammer", "Hammer (a smith's hammer)", None, "no", None),
    ("forest", "on the bank of a wide river", "swim across the river", "the iron pot", "Iron Pot (a heavy cooking pot)", None, "no", None),
    ("village", "in the square", "gather the spilled grain", "the sack", "Sack (an empty hemp sack)", None, "help", None),
    ("village", "in the square", "sing a song for the crowd", "the shovel", "Shovel (a wooden shovel)", None, "no", None),
    ("farm", "in the barn", "listen at the barn wall", "the axe", "Axe (a woodcutter's axe)", None, "no", None),
    ("cave", "in a dark passage", "examine the cave paintings", "the lantern", "Lantern (a horn lantern)", None, "help", None),
    ("farm", "in the vegetable garden", "dig the garden bed", "the hoe", "Hoe (a long-handled hoe)", "a shovel", "help", None),
    ("farm", "in the vegetable garden", "dig the garden bed", "the spade", "Spade (an iron-shod spade)", "a shovel", "help", "is_the_tool"),
    ("farm", "in the vegetable garden", "dig the garden bed", "the feather", "Feather (a long grey feather)", "a shovel", "no", None),
    ("mountain", "at an ore seam", "mine the ore seam", "the sledgehammer", "Sledgehammer (a heavy two-handed hammer)", "a pickaxe", "help", None),
    ("mountain", "at an ore seam", "mine the ore seam", "the loaf of bread", "Loaf of Bread (a round brown loaf)", "a pickaxe", "no", None),
    ("forest", "beside a fallen log", "cut wood from the fallen log", "the hatchet", "Hatchet (a small hand axe)", "an axe", "help", None),
    ("forest", "beside a fallen log", "cut wood from the fallen log", "the rope", "Rope (a coil of hemp rope)", "an axe", "no", None),
    ("coast", "on the shore of a pond", "fish in the pond", "the spear", "Spear (an ash-shafted spear)", "a fishing rod or a net", "help", None),
    ("coast", "on the shore of a pond", "fish in the pond", "the lantern", "Lantern (a horn lantern)", "a fishing rod or a net", "no", None),
    ("forest", "beside a slain deer", "cut the meat from the deer", "the sword", "Sword (an arming sword)", "a knife", "help", None),
    ("forest", "beside a slain deer", "cut the meat from the deer", "the wooden spoon", "Wooden Spoon (a carved spoon)", "a knife", "no", None),
    ("forest", "beside a slain wolf", "cut the pelt from the wolf", "the sickle", "Sickle (a curved reaping blade)", "a knife", "help", None),
    ("village", "in the square", "cut wood from the stack", "the prayer book", "Prayer Book (a small leather-bound book)", "an axe", "no", None),
]


def cases():
    for i, (world, node, goal, item, ctx, tool, label, degree) in enumerate(CASES):
        # NarrativeController: $"{item.WithArticle()} ({item.Description})" — "a spade (an iron-shod spade)".
        noun = item[len("the "):]
        ctx = f"{P.article(noun)} {noun} ({ctx[ctx.index('(') + 1:-1]})"
        yield dict(id=f"critic{i:02d}", world=world, node=node, goal=goal, item=item, item_context=ctx,
                   tool=tool, label=label, degree=degree)


def ask(T, c, seed):
    tree, q, choices = question(T, c)
    gbnf = "root ::= response\nresponse ::= " + " | ".join(f'"{cid}"' for cid, _, _ in choices)
    msgs = [{"role": "system", "content": P.system_prompt(P.T0, T["system"])},
            {"role": "user", "content": T["prompt"](q, choices)}]
    g = P.generate(msgs, gbnf, seed, max_tokens=20)
    fail = {cid for cid, _, f in choices if f}
    return dict(tree=tree, answer=g["text"].strip(), help=g["text"].strip() not in fail,
                user_tokens=P.tokens(msgs[-1]["content"]), **{k: g[k] for k in ("wall", "prompt_n", "prompt_ms")})


VARIANTS = {"base": base}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--variant", default="base")
    ap.add_argument("--seeds", default="1,2,3,4,5")
    ap.add_argument("--out")
    a = ap.parse_args()
    T = VARIANTS[a.variant]()
    seeds = [int(s) for s in a.seeds.split(",")]
    out = open(a.out, "a") if a.out else None
    side = deg = n = 0
    per = {}
    for c in cases():
        for s in seeds:
            r = ask(T, c, s)
            ok = r["help"] == (c["label"] == "help")
            side += ok; n += 1
            deg += ok and (c["degree"] is None or r["answer"] == c["degree"])
            per.setdefault(r["tree"], [0, 0]); per[r["tree"]][0] += ok; per[r["tree"]][1] += 1
            if out:
                out.write(json.dumps(dict(variant=a.variant, case=c["id"], seed=s, ok=ok, **r)) + "\n")
        print(c["id"], c["goal"], "/", c["item"], c["label"], flush=True)
    print(f"\n{a.variant}: right side {side}/{n} = {100 * side / n:.1f}%   right side and degree {deg}/{n}")
    for k, (o, m) in per.items():
        print(f"  {k:20} {o}/{m} = {100 * o / m:.1f}%")


if __name__ == "__main__":
    main()
