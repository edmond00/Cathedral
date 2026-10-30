"""Turns the corpus (runs/*/session/rewrite_inputs.jsonl) into the cases the harness samples.

A case is one recorded rewrite, asked of one persona:
  * its own persona, with the slot history it really had (`own`);
  * another modus mentis's persona, on a cleared history (`swap`), so the corpus reaches more minds
    than the playthroughs happened to hold — chosen to span the catalogue, terse and florid,
    thinking and bodily, moral and amoral, well and loosely written;
  * a deliberately loosened persona (`loose`, see personas.LOOSENED).

NPC lines are only ever asked in their own persona: an NPC's text profile is not a modus mentis.

The held-out split is whole playthroughs (HELDOUT), never iterated on: their places, hours and minds
are ones the wording was not tuned against.

    python tools/prompts/build_cases.py   # writes cases.jsonl and prints the coverage
"""
import collections, glob, hashlib, json, os, random, sys
sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P
import personas as Q

HERE = os.path.dirname(os.path.abspath(__file__))
HELDOUT = {"cave_morning", "village_evening", "plain_afternoon_party"}

# Minds the swaps speak through, spanning the catalogue. Each with the kinds it is asked to voice
# (a will or a thinking mind is never asked to narrate an emotion, and so on, as in the game).
SWAP = {
    "poetry": "Observation Reasoning Outcome",
    "arithmetic_logic": "Observation Reasoning Outcome",
    "brute_force": "Action Outcome",
    "cutpurse": "Action Outcome",
    "cruelty": "DialogueReplica Speaking",
    "courtesy": "DialogueReplica Speaking Reasoning Emotion",
    "plain_dealing": "DialogueReplica Speaking",
    "rhetoric": "DialogueReplica Speaking Reasoning",
    "avarice": "Reasoning Emotion",
    "mercy": "Reasoning Emotion",
    "dread": "Observation Reasoning Emotion",
    "keen_nose": "Observation",
    "woodcraft": "Observation Action Outcome",
    "stoneface": "Action Outcome",
    "sloth": "Emotion",
    "pride": "Reasoning Emotion DialogueReplica",
    "architecture": "Observation",
    "ruination": "Reasoning Emotion",
}
LOOSE_KINDS = "Observation Reasoning Action Outcome Emotion DialogueReplica Speaking"
PER_RECORD_SWAPS = 2      # other minds per recorded rewrite
LOOSE_EVERY = 3           # one loosened persona on every third recorded rewrite


def stable(s):
    return int(hashlib.sha1(s.encode()).hexdigest()[:8], 16)


def main():
    mms = Q.load_modi_mentis()
    cases, seen = [], set()
    for path in sorted(glob.glob(os.path.join(HERE, "runs", "*", "session", "rewrite_inputs.jsonl"))):
        run = path.split(os.sep)[-3]
        split = "heldout" if run in HELDOUT else "dev"
        for n, r in enumerate(P.load_corpus(path)):
            persona_prompt, prior = P.split_history(r)
            who = Q.identify(persona_prompt, mms)
            key = (r["kind"], r["neutralText"], who)
            if key in seen:
                continue
            seen.add(key)
            rid = f"{run}:{n:03d}"
            base = dict(record=r, split=split, run=run, source_persona=who)
            cases.append(dict(base, id=f"{rid}:own", variant="own", persona=dict(
                id=who, prompt=persona_prompt, reminder2=r.get("personaReminder2"), style=r.get("styleInstruction")),
                prior=prior))
            if who.startswith("npc:") or who.startswith("mm:"):
                continue
            rng = random.Random(stable(rid))
            pool = [m for m, kinds in SWAP.items() if r["kind"] in kinds.split() and m != who]
            for m in rng.sample(pool, min(PER_RECORD_SWAPS, len(pool))):
                mm = mms[m]
                cases.append(dict(base, id=f"{rid}:swap:{m}", variant="swap", persona=dict(
                    id=m, prompt=mm["prompt"], reminder2=mm["reminder2"], style=mm["style"]), prior=[]))
            if n % LOOSE_EVERY == 0 and r["kind"] in LOOSE_KINDS.split():
                lo = Q.LOOSENED[sorted(Q.LOOSENED)[stable(rid) % len(Q.LOOSENED)]]
                cases.append(dict(base, id=f"{rid}:loose:{lo['id'][6:]}", variant="loose", persona=dict(
                    id=lo["id"], prompt=lo["prompt"], reminder2=lo["reminder2"], style=lo["style"]), prior=[]))
    with open(os.path.join(HERE, "cases.jsonl"), "w", encoding="utf-8") as f:
        for c in cases:
            f.write(json.dumps(c) + "\n")
    cov = collections.Counter((c["split"], c["record"]["kind"]) for c in cases)
    minds = {c["persona"]["id"] for c in cases}
    print(len(cases), "cases;", len(minds), "personas")
    for k in sorted(cov):
        print(f"  {k[0]:8} {k[1]:16} {cov[k]}")


if __name__ == "__main__":
    main()
