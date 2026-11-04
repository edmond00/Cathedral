"""The substitution tree's degree asked as two yes/no questions instead of read off its options.

Asked as a five-option menu, the strict substitution question put almost all its weight on
serves_poorly whatever the item — a dagger offered as a knife, a war axe as an axe, fish-hooks to fish —
and no rewording of the options moved it (critic_bench variants sub_w1, sub_w2). This asks, of an item
that has already passed, two plain questions on the same preamble and reads yes against no from the
first token:

    low     "Is X a kind of Y, or near enough the same tool?"
    medium  "Would X do this work properly, the way Y does, if a little less well?"

degree = low if the first says yes, else medium if the second does, else high. A pass the strict
question refused (carried by the lenient one) stays high without asking.

    python tools/prompts/critic_degree_yesno.py pair_run.jsonl [--variant current]
"""
import argparse, json, math, os, sys
sys.path.insert(0, os.path.dirname(__file__))
import critic_bench as B
import prompt_lib as P

LOW_Q = "Is {itemName} a kind of {referenceToolPhrase}, or near enough the same tool?"
MEDIUM_Q = "Would {itemName} do this work properly, the way {referenceToolPhrase} does, if a little less well?"
YES = ("yes", "Yes")
NO = ("no", "No")


def lse(xs):
    m = max(xs)
    return m + math.log(sum(math.exp(x - m) for x in xs))


def yes_logodds(T, c, ask):
    """Log-odds of yes against no as the reply's first token, to `ask` on the strict question's head."""
    st = T["strict"]
    q, _ = st["trees"]["ToolSubstitution"]
    head = q[:q.index("\n\n", q.index("They are holding")) + 2]
    text = P._fill(head.replace("{context.BuildPreamble()}", "{pre}") + ask + "\n\nAnswer yes or no.",
                   pre=st["preamble"](st, c), goalText=B.goal_of(st, c), itemName=c["item"],
                   itemContext=c.get("item_context") or c["item"], referenceToolPhrase=c["tool"])
    msgs = [{"role": "system", "content": P.system_prompt(st["templates"], st["system"])},
            {"role": "user", "content": text}]
    d = B._post("/v1/chat/completions", dict(model="local", messages=msgs, max_tokens=1, stream=False,
                                             cache_prompt=True, temperature=0, logprobs=True, top_logprobs=40))
    top = {t["token"].strip(): t["logprob"] for t in d["choices"][0]["logprobs"]["content"][0]["top_logprobs"]}
    y = [top[k] for k in YES if k in top] or [-30.0]
    n = [top[k] for k in NO if k in top] or [-30.0]
    return lse(y) - lse(n)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("run")
    ap.add_argument("--variant", default="current")
    ap.add_argument("--out")
    a = ap.parse_args()
    T = B.VARIANTS[a.variant]()
    byid = {c["id"]: c for c in B.cases("all")}
    out = open(a.out, "w") if a.out else None
    for r in map(json.loads, open(a.run)):
        if r["tree"] != "ToolSubstitution" or not r["help"]:
            continue
        c = byid[r["case"]]
        rec = dict(case=r["case"], label=r["label"], band=r["band"], item=r["item"], goal=r["goal"],
                   strict=r["strict"], answer=r["answer"])
        if r["strict"] > 0:
            rec["low"] = yes_logodds(T, c, LOW_Q)
            rec["medium"] = yes_logodds(T, c, MEDIUM_Q)
        print(json.dumps(rec), flush=True)
        if out:
            out.write(json.dumps(rec) + "\n")


if __name__ == "__main__":
    main()
