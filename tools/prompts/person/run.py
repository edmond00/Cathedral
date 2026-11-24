"""Select the experiment's cases and sample every version on them.

    python run.py select                     # writes sets/dev.txt and sets/heldout.txt (case ids)
    python run.py sample --set dev --seeds 1,2 [--versions first,second,second_tn]

Cases come from ../cases.jsonl (real requests recorded in eleven playthroughs, each in its own
persona, in a swapped one or in a deliberately loosened one). Selection is stratified by kind and
spreads the personas: at most two cases per persona in a set. The held-out set is drawn from the
corpus's own held-out playthroughs and is not looked at until the wording is settled.

Every version is asked the same case with the same seeds, so samples pair up. No slot history is
sent (one corpus case had any): a first-person history would lean on the second-person versions.
Resumable.
"""
import argparse, collections, json, os, random, sys, time

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import person_lib as L          # noqa: E402
P = L.P

QUOTA = {"dev": dict(Observation=16, Reasoning=12, Action=8, Outcome=8, Emotion=4),
         "heldout": dict(Observation=10, Reasoning=8, Action=5, Outcome=5, Emotion=3)}


def cases():
    return [json.loads(l) for l in open(os.path.join(HERE, "..", "cases.jsonl"), encoding="utf-8") if l.strip()]


def select():
    rng = random.Random(20261124)
    allc = [c for c in cases() if c["record"]["kind"] in L.KINDS]
    os.makedirs(os.path.join(HERE, "sets"), exist_ok=True)
    for split, quota in QUOTA.items():
        pool = [c for c in allc if c["split"] == split]
        rng.shuffle(pool)
        per_persona = collections.Counter()
        chosen = []
        for kind, n in quota.items():
            got = 0
            # loosened personas first, so a few are always in
            ordered = sorted([c for c in pool if c["record"]["kind"] == kind], key=lambda c: c["variant"] != "loose")
            for c in ordered:
                if got == n:
                    break
                pid = c["persona"]["id"]
                if per_persona[pid] >= 2 or (c["variant"] == "loose" and got >= max(1, n // 4)):
                    continue
                per_persona[pid] += 1
                chosen.append(c["id"]); got += 1
        open(os.path.join(HERE, "sets", f"{split}.txt"), "w").write("\n".join(chosen) + "\n")
        print(split, len(chosen), "cases,", len(per_persona), "personas")


def sample(a):
    ids = open(os.path.join(HERE, "sets", f"{a.set}.txt")).read().split()
    k, n = (int(x) for x in a.shard.split("/"))
    ids = ids[k::n]
    by_id = {c["id"]: c for c in cases()}
    out_path = os.path.join(HERE, "samples", f"{a.set}.jsonl" if a.shard == "0/1" else f"{a.set}.{k}.jsonl")
    os.makedirs(os.path.dirname(out_path), exist_ok=True)
    done = set()
    if os.path.exists(out_path):
        for l in open(out_path):
            s = json.loads(l)
            done.add((s["case"], s["version"], s["seed"]))
    seeds = [int(s) for s in a.seeds.split(",")]
    versions = a.versions.split(",")
    out = open(out_path, "a", encoding="utf-8")
    for i, cid in enumerate(ids):
        c = dict(by_id[cid], prior=[])
        for v in versions:
            r, msgs, gbnf, persona = L.request(v, c)
            utoks = None
            for seed in seeds:
                if (cid, v, seed) in done:
                    continue
                if utoks is None:
                    utoks = P.tokens(msgs[-1]["content"])
                for attempt in range(4):
                    try:
                        g = P.generate(msgs, gbnf, seed)
                        break
                    except Exception as e:          # a busy server: wait and ask again
                        print("retry", cid, v, seed, e, flush=True)
                        time.sleep(10 * (attempt + 1))
                else:
                    raise RuntimeError(f"no answer for {cid} {v} {seed}")
                out.write(json.dumps(dict(case=cid, version=v, seed=seed, text=g.pop("text"), user_tokens=utoks,
                                          neutral=r["neutralText"], persona=persona["id"], prompt=msgs[-1]["content"],
                                          **g)) + "\n")
                out.flush()
        print(f"{i + 1}/{len(ids)} {cid}", flush=True)


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    ap.add_argument("cmd", choices=["select", "sample"])
    ap.add_argument("--set", default="dev")
    ap.add_argument("--seeds", default="1,2")
    ap.add_argument("--shard", default="0/1", help="k/n: every n-th case from the k-th, to its own file")
    ap.add_argument("--versions", default=",".join(L.VERSIONS))
    a = ap.parse_args()
    select() if a.cmd == "select" else sample(a)
