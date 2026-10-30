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

def _source(rev):
    crit, trees = P._read("src/game/ItemUseCritic.cs", rev), P._read("src/game/CriticTrees.cs", rev)
    system = re.search(r'GetCriticSystemPrompt\(\) => @"((?:[^"]|"")*)"', crit).group(1).replace('""', '"')
    setting = re.search(r'"(Setting: [^"]*)"', trees).group(1)
    return system, setting, {n: _tree(trees, n) for n in ("ItemAppropriateness", "ToolSubstitution")}


def _tree(_trees, name):
    body = _trees[_trees.index(f'name: "{name}"'):]
    q = re.search(r'question: \$"((?:[^"\\]|\\.)*)"', body).group(1)
    choices = re.findall(r'new\("(\w+)",\s*"([^"]*)"(,\s*\n?\s*isFailure: true)?', body[:body.index("});")])
    return P._unescape(q), [(cid, desc, bool(fail)) for cid, desc, fail in choices]


SYSTEM, SETTING_LINE, TREES = _source(P.BASE_REV)   # the old wording, frozen (see prompt_lib.BASE_REV)


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


# Held out: written once, never iterated on, scored only for the final before/after.
HELDOUT = [
    ("village", "at the well", "draw water from the well", "the bucket", "Bucket (a wooden bucket)", None, "help", None),
    ("village", "at the well", "draw water from the well", "the dagger", "Dagger (a short blade)", None, "no", None),
    ("forest", "under a beehive in a hollow oak", "take honey from the hive", "the torch", "Torch (a pitch torch)", None, "help", None),
    ("forest", "under a beehive in a hollow oak", "take honey from the hive", "the lute", "Lute (a pear-shaped lute)", None, "no", None),
    ("farm", "in the pigsty", "calm the angry boar", "the turnip", "Turnip (a fat white turnip)", None, "help", None),
    ("farm", "in the pigsty", "calm the angry boar", "the quill", "Quill (a goose quill)", None, "no", None),
    ("mountain", "at the foot of a crag", "climb the crag", "the grappling hook", "Grappling Hook (an iron hook on a line)", None, "help", None),
    ("mountain", "at the foot of a crag", "climb the crag", "the bowl", "Bowl (a clay bowl)", None, "no", None),
    ("forest", "beside a fallen birch", "cut wood from the fallen birch", "the saw", "Saw (a bow saw)", "an axe", "help", None),
    ("forest", "beside a fallen birch", "cut wood from the fallen birch", "the bread knife", "Bread Knife (a thin kitchen knife)", "an axe", "help", None),
    ("forest", "beside a fallen birch", "cut wood from the fallen birch", "the blanket", "Blanket (a wool blanket)", "an axe", "no", None),
    ("farm", "in the turnip field", "dig the turnip field", "the pitchfork", "Pitchfork (a three-tined fork)", "a shovel", "help", None),
    ("farm", "in the turnip field", "dig the turnip field", "the candle", "Candle (a tallow candle)", "a shovel", "no", None),
    ("mountain", "at an ore seam", "mine the ore seam", "the iron bar", "Iron Bar (a heavy iron crowbar)", "a pickaxe", "help", None),
    ("mountain", "at an ore seam", "mine the ore seam", "the rope", "Rope (a coil of hemp rope)", "a pickaxe", "no", None),
    ("coast", "at the edge of a stream", "fish in the stream", "the basket", "Basket (a wicker basket)", "a fishing rod or a net", "help", None),
    ("coast", "at the edge of a stream", "fish in the stream", "the hammer", "Hammer (a smith's hammer)", "a fishing rod or a net", "no", None),
    ("forest", "beside a slain boar", "cut the meat from the boar", "the axe", "Axe (a woodcutter's axe)", "a knife", "help", None),
    ("forest", "beside a slain boar", "cut the meat from the boar", "the cup", "Cup (a pewter cup)", "a knife", "no", None),
    ("village", "in a cottage", "unlock the door", "the key ring", "Key Ring (a ring of assorted iron keys)", None, "help", None),
]


def cases(heldout=False):
    for i, (world, node, goal, item, ctx, tool, label, degree) in enumerate(HELDOUT if heldout else CASES):
        # NarrativeController: $"{item.WithArticle()} ({item.Description})" — "a spade (an iron-shod spade)".
        noun = item[len("the "):]
        ctx = f"{P.article(noun)} {noun} ({ctx[ctx.index('(') + 1:-1]})"
        yield dict(id=f"{'held' if heldout else 'critic'}{i:02d}", world=world, node=node, goal=goal, item=item, item_context=ctx,
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


def no_strict():
    """The system prompt without "Be strict but fair; value plausibility over generosity", which reads
    as an instruction to refuse: every real substitute that was not near enough the tool itself
    (a sword for a knife, a spear for a net, a sledgehammer for a pick) came back cannot_serve."""
    T = base()
    T["system"] = T["system"].replace("- Be strict but fair; value plausibility over generosity.\n", "")
    return T


def craftsman():
    """Strictness replaced by a practical yardstick, said the same way for both trees."""
    T = base()
    T["system"] = T["system"].replace(
        "- Be strict but fair; value plausibility over generosity.",
        "- Judge as a practised worker would: a thing that does the job, even clumsily, helps; a thing that cannot do it at all does not.")
    return T


def how_well():
    """The substitution question asked as a degree ("how well"), not as a yes/no that the first
    failing option then answers."""
    T = no_strict()
    q, ch = T["trees"]["ToolSubstitution"]
    T["trees"] = dict(T["trees"], ToolSubstitution=(q.replace("Can {itemName} do the work of {referenceToolPhrase} here?",
                                                              "How well could {itemName} do the work of {referenceToolPhrase} here?"), ch))
    return T


def in_its_place():
    """The substitution asked as standing in for the tool, "even if less well": the model refused a
    sword for a knife and a spear for a net as if only the tool itself would do."""
    T = no_strict()
    q, ch = T["trees"]["ToolSubstitution"]
    T["trees"] = dict(T["trees"], ToolSubstitution=(q.replace(
        "Can {itemName} do the work of {referenceToolPhrase} here?",
        "Could {itemName} be used in place of {referenceToolPhrase} for this, even if less well?"), ch))
    return T


def current():
    """Whatever the working tree's C# says now."""
    system, setting, trees = _source(None)
    return dict(system=system, setting_line=setting, trees=trees, preamble=preamble, prompt=choice_prompt)


VARIANTS = {"current": current, "in_its_place": in_its_place, "base": base, "no_strict": no_strict, "craftsman": craftsman, "how_well": how_well}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--variant", default="base")
    ap.add_argument("--seeds", default="1,2,3,4,5")
    ap.add_argument("--out")
    ap.add_argument("--heldout", action="store_true")
    a = ap.parse_args()
    T = VARIANTS[a.variant]()
    seeds = [int(s) for s in a.seeds.split(",")]
    out = open(a.out, "a") if a.out else None
    side = deg = n = 0
    per = {}
    for c in cases(a.heldout):
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
