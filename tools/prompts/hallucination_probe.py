"""State hallucinations: how often a persona rewrite reports a world state the game did not supply.

Started from a player report: a LOCKED door narrated as standing open, and a lockpicking refusal
("I would need to find a hairpin or a bone needle") narrated as having FOUND the needle. Both are
the same failure — the rewrite asserts a state the neutral line only names as wanted, needed or
seemed — so this probe measures that failure across every place the game hands the model such a
line, and A/B-tests rewordings on the same seeds.

Every prompt is built by `prompt_lib` (the byte-identical mirror of the C#), in the persona fields
read out of the modus mentis sources, with the game's sampling. A *context* is one neutral line the
game really produces (each was read off a --playground run or out of NeutralNarration.cs); a
*variant* rewords neutral lines (and nothing else), so a change measured here ports to
NeutralNarration / the verb's Verbatim with no other moving part.

    python tools/prompts/hallucination_probe.py run  --out results/halluc_base.jsonl [--variant base] [--n 6]
    python tools/prompts/hallucination_probe.py score results/halluc_*.jsonl

Needs serve.sh's llama-server on PROMPT_PORT (8092). Scoring is a regex screen biased towards
recall; every flagged sample is printed so it can be read, and the counts in NOTES are from reading.
"""
import argparse, json, os, re, sys, time

sys.path.insert(0, os.path.dirname(__file__))
import prompt_lib as P
import personas as PS

MMS = PS.load_modi_mentis()
# The wording as the source has it NOW. prompt_lib.T0 is frozen at the overhaul's base commit and is
# only right for comparing against that overhaul; a hallucination a player meets is today's wording.
T_LIVE = P.load_templates()

# ── The neutral sentences, as the game builds them ─────────────────────────────────────────────────
# Keep these literal copies of NeutralNarration so a variant can be compared against them; the base
# variant is the code as it stands (checked against a --playground screen, see NOTES).

def tool_required(act, tool):          # NeutralNarration.ToolRequired
    return (f"I wanted to {act}, but I cannot do that with my bare hands. "
            f"I would need to find {tool} for that, and take it in hand to do this work.")

def failure(act, effects):             # NeutralNarration.OutcomeFailure
    return f"Alas, I failed to {act}." + (f" Due to this failure I {', '.join(effects)}." if effects else "")

def impossible(act, reason):           # NeutralNarration.ActionImpossible
    return f"I cannot {act}: {reason}"

def refusal(act):                      # NeutralNarration.ActionRefusal
    return f"I don't want to {act}."

def observation(name, desc, hint):     # NeutralNarration.Observation + DoorPointOfInterest.DescribeFrom
    return f"My attention is drawn to {name}. This is {desc} — {hint}."

def reasoning(desc, hint, goal, means):  # NeutralNarration.ReasoningChain
    return f"I notice {desc} — {hint}. I want to {goal}. I will rely on {means}."


DOOR = "the door"
UNLOCK = "unlock and open the door"           # UnlockDoorVerb.Verbatim
PICKS = "a hairpin or a bone needle"           # RequiredToolRule.ToolPhrase(hairpin, bone_needle)
DOOR_DESCS = [                                 # BuildingDescriptions: build + fabric + detail
    "a heavy plank door, iron-hinged and rough-fitted",
    "a warped board door, its planks shrunk apart enough to see light through",
    "a narrow oak door, pale where an older lock was prised away",
    "a battered elm door, hung crooked, catching on the sill",
]
DOOR_ACTORS = ["stealth", "lockpicking", "brute_force", "finesse", "cutpurse", "jailcraft"]
OBSERVERS = ["keywise", "poetry"]
THINKERS = ["keywise", "ruination", "avarice"]

# The other Required verbs (ToolGatedVerbs, SlayVerb, CutVerb), each with the crafts that would act on it.
TOOL_VERBS = [
    ("mine",     "work the rock face for ore", "a pick",                  ["metalcraft", "hard_labor", "stealth"]),
    ("dig",      "dig into the clay bank",     "a shovel",                ["spadework", "hard_labor", "stealth"]),
    ("fish",     "fish the creek",             "a fishing rod or a net",  ["anglery", "hard_labor", "stealth"]),
    ("cut_wood", "cut wood from the fallen log", "an axe",                ["woodcraft", "hard_labor", "stealth"]),
    ("break",    "break the crate apart",      "a hammer or an axe",      ["brute_force", "hard_labor", "stealth"]),
    ("cut",      "cut the pelt",               "a knife",                 ["butchery", "hard_labor", "stealth"]),
]
TOOL_WORDS = {
    "unlock_door": r"needle|hairpin|pin|pick(?:lock)?|wire|key",
    "mine": r"pick(?:axe)?", "dig": r"shovel|spade", "fish": r"rod|net|line|hook",
    "cut_wood": r"axe|hatchet", "break": r"hammer|axe|mallet", "cut": r"knife|blade",
}


def contexts(V):
    """(context id, verb, kind, neutral, persona ids, place). V rewords neutral lines (see VARIANTS)."""
    tr, fl, unl = V.get("tool_required", tool_required), V.get("failure", failure), V.get("unlock", UNLOCK)
    imp, ref = V.get("impossible", impossible), V.get("refusal", refusal)
    hint_l = V.get("hint_locked", "the door seems locked")
    obs, rsn = V.get("observation", observation), V.get("reasoning", reasoning)
    out = [
        ("door_tool_refusal", "unlock_door", "Outcome", tr(unl, PICKS), DOOR_ACTORS, "village"),
        ("door_failure_focus", "unlock_door", "Outcome", fl(unl, ["lost the thread of my thoughts"]), DOOR_ACTORS, "village"),
        ("door_failure_time", "unlock_door", "Outcome", fl(unl, ["lost the time over it until noon"]), DOOR_ACTORS, "village"),
        ("door_witness", "unlock_door", "Outcome",
         imp(unl, "the weaver is right here — I would be caught red-handed."), DOOR_ACTORS, "village"),
        ("door_unwilling", "unlock_door", "Outcome", ref(unl), DOOR_ACTORS, "village"),
    ]
    for i, d in enumerate(DOOR_DESCS):
        out.append((f"door_obs_{i}", "unlock_door", "Observation", obs("a door", d, hint_l), OBSERVERS, "village"))
    for i, d in enumerate(DOOR_DESCS[:2]):
        out.append((f"door_reason_{i}", "unlock_door", "Reasoning",
                    rsn(d, hint_l, unl, "the delicate picking of locks and latches"), THINKERS, "village"))
    for vid, act, tool, actors in TOOL_VERBS:
        out.append((f"{vid}_tool_refusal", vid, "Outcome", tr(act, tool), actors, "village"))
    # Other verbatims that bundle a second, unearned result after "and" — the same shape as
    # "unlock and open", asked to be narrated as a failure.
    lose = ["lost the thread of my thoughts"]
    out += [
        ("open_door_failure", "open_door", "Outcome", fl("open the door and step through", lose),
         ["stealth", "brute_force", "hard_labor"], "village"),
        ("tame_failure", "tame", "Outcome", fl("tame him and keep him at my side", lose),
         ["husbandry", "stealth", "brute_force"], "farm"),
        ("stalk_failure", "stalk", "Outcome", fl("follow him at a distance and see where he goes", lose),
         ["stealth", "cutpurse", "hard_labor"], "village"),
        ("hide_failure", "hide_and_wait", "Outcome", fl("hide in the hay loft and watch", lose),
         ["stealth", "cutpurse", "hard_labor"], "farm"),
    ]
    return out


# ── Variants: rewordings of the neutral lines only ─────────────────────────────────────────────────

def tool_required_v1(act, tool):
    # No "find", no "take it in hand": the tool is said to be ABSENT, in the past tense of a thing
    # that did not happen, and the act is said not to have been done.
    return (f"I wanted to {act}, but my bare hands could not do it, and I have no {strip_article(tool)} with me. "
            f"Without one I left it undone.")

def tool_required_v2(act, tool):
    return (f"I wanted to {act}, but I have nothing to do it with: it takes {tool}, and I carry none. "
            f"So I did not {act}.")

def tool_v3(act, tool):
    # True whether or not the tool is carried: RequiredToolRule refuses when nothing is COMBINED with
    # the act, so "I have none" would be false for a player with a hairpin in the pack.
    return (f"I wanted to {act}, but my bare hands could not do it: it takes {tool}, "
            f"and my hands held none for it. So I left it undone.")

def impossible_undone(act, reason):
    return f"I cannot {act}: {reason} So I leave it undone."

def refusal_undone(act):
    return f"I don't want to {act}, so I leave it undone."

def failure_v1(act, effects):
    return f"Alas, I failed to {act}; it did not happen." + (f" Due to this failure I {', '.join(effects)}." if effects else "")

def strip_article(np):
    return re.sub(r"\b(a|an) ", "", np)

def observation_sentence(name, desc, hint):
    # The lock state as a sentence of its own, not a clause trailing an em dash.
    return f"My attention is drawn to {name}. This is {desc}. The door is locked."

def reasoning_sentence(desc, hint, goal, means):
    return f"I notice {desc}. The door is locked. I want to {goal}. I will rely on {means}."

def observation_named(name, desc, hint):
    # The state carried by the name the attention line gives the object, which every rewrite keeps.
    return f"My attention is drawn to a locked door. This is {desc}."

def refusal_left(act):
    return f"I don't want to {act}, so I leave things as they are."

VARIANTS = {
    "base": {},
    # the verb's Verbatim without "and open": the act named is the unlocking alone
    "unlock_only": {"unlock": "unlock the door"},
    "tool_v1": {"tool_required": tool_required_v1},
    "tool_v2": {"tool_required": tool_required_v2},
    "tool_v3": {"tool_required": tool_v3},
    "refusal_undone": {"impossible": impossible_undone, "refusal": refusal_undone},
    "combo": {"unlock": "pick the lock of the door", "tool_required": tool_required_v2, "failure": failure_v1,
              "hint_locked": "the door is locked"},
    "hint_is": {"hint_locked": "the door is locked"},
    "name_locked": {"observation": observation_named},
    "refusal_left": {"refusal": refusal_left, "impossible": impossible_undone},
    "hint_sentence": {"observation": observation_sentence, "reasoning": reasoning_sentence},
}


# ── Running ────────────────────────────────────────────────────────────────────────────────────────

def record(kind, neutral, mm, place):
    return dict(kind=kind, neutralText=neutral, personaReminder2=mm["reminder2"], styleInstruction=mm["style"],
                addressee=None, dialogueContext=None, speakerName=None, observedOther=None,
                forcedPrefix="I ", keepHistory=True, place=place,
                history=[{"role": "system", "content": P.system_prompt(T_LIVE, mm["prompt"])}])


def run(args):
    V = VARIANTS[args.variant]
    T = T_LIVE
    only = set(args.only.split(",")) if args.only else None
    done = set()
    if os.path.exists(args.out):
        for l in open(args.out):
            d = json.loads(l); done.add((d["context"], d["persona"], d["seed"]))
    with open(args.out, "a") as fo:
        for cid, verb, kind, neutral, pids, place in contexts(V):
            if only and not any(cid.startswith(o) for o in only):
                continue
            n = args.n if kind == "Outcome" else max(2, args.n // 2)
            for pid in pids:
                mm = MMS[pid]
                r = record(kind, neutral, mm, place)
                # Built here, not by P.messages_for, which strips the rule with the frozen T0 wording
                # and would append the live rule a second time.
                msgs = r["history"] + [{"role": "user", "content": P.build_prompt(T, r)}]
                for s in range(n):
                    seed = 1000 + s
                    if (cid, pid, seed) in done:
                        continue
                    g = P.generate(msgs, P.grammar(r), seed)
                    fo.write(json.dumps(dict(variant=args.variant, context=cid, verb=verb, kind=kind, persona=pid,
                                             seed=seed, neutral=neutral, output=g["text"], wall=g["wall"])) + "\n")
                    fo.flush()
                    print(f"{cid:22} {pid:12} {g['wall']:5.1f}s  {g['text'][:110]}", flush=True)


# ── Scoring ────────────────────────────────────────────────────────────────────────────────────────

NEG = r"\b(not|n't|no|never|without|nothing|none|cannot|can't|unable|refus\w*|fail\w*|would|could|need\w*|lack\w*|if|until|seems?|seemed|wish\w*|want\w*|hope\w*|try|tried|trying|attempt\w*|must|should|ought|before|yet|sought|seek\w*|meant|mean|aim\w*|intend\w*|wanted|longed|hoped|bid|set out|reach\w* to)\b"

OPEN_RE = re.compile(
    r"\b(swing|swings|swung|swinging|creak\w*\s+open|open(s|ed|ing)?|ajar|unlock(s|ed)|unbolt\w*|unlatch\w*|"
    r"gave way|gives way|give way|yield(s|ed)|click(s|ed)? (open|free|back)|sprang|springs? open|"
    r"step(ped|s)? (inside|through|in)|slip(ped|s)? (inside|through|in)|enter(ed|s)?|cross(ed)? the threshold|"
    r"pushed (it )?open|lock (gives|gave|turns|turned|releases|released))\b", re.I)

def clauses(text):
    return [c for c in re.split(r"[.;!?]|,\s*(?:but|and|yet)\b|\bbut\b", text) if c.strip()]

LOCK_KEPT = re.compile(r"\b(locked|sealed|shut|barred|held fast|locks in place|lock(ed)? tight)\b", re.I)

def door_open(text):
    """Clauses asserting the door opened / was unlocked / was passed, with no hedge in the clause."""
    hits = []
    for c in clauses(text):
        if OPEN_RE.search(c) and not re.search(NEG, c, re.I):
            hits.append(c.strip())
    return hits

HAVE_RE = r"\b(found|find|finds|finding|have|has|had|hold|holds|holding|held|grasp\w*|clutch\w*|grip\w*|pull\w*|drew|draw\w*|slid\w*|insert\w*|took|take|takes|taking|pluck\w*|retriev\w*|produc\w*|fish\w* out|work\w*|twist\w*|turn\w*|wield\w*|use|used|using|with (my|the|a|this))\b"

def tool_found(text, verb):
    words = TOOL_WORDS.get(verb, "")
    hits = []
    for c in clauses(text):
        if re.search(rf"\b({words})s?\b", c, re.I) and re.search(HAVE_RE, c, re.I) and not re.search(NEG, c, re.I):
            hits.append(c.strip())
    return hits

def tool_sought(text, verb):
    """A search the neutral line never made: "I searched for a hairpin", "I rummaged for a pick"."""
    words = TOOL_WORDS.get(verb, "")
    return [c.strip() for c in clauses(text)
            if re.search(rf"\b({words})s?\b", c, re.I)
            and re.search(r"\b(searched|rummaged|hunted|looked (about|around)|sought out|went (to find|looking)|scoured|groped)\b", c, re.I)]

def act_done(text, verb):
    """For a refusal or failure of a non-door verb: a clause saying the act was carried out."""
    pat = {"mine": r"\b(ore|seam|rock)\b.*\b(broke|split|cracked|came|freed|chipped|struck)\b",
           "dig": r"\b(dug|digging|earth|clay)\b.*\b(gave|turned|broke|opened|parted|came)\b",
           "fish": r"\b(caught|catch|hooked|landed|fish (bit|took))\b",
           "cut_wood": r"\b(split|chopped|hewed?|cleaved?|felled|wood came)\b",
           "break": r"\b(splinter\w*|shatter\w*|broke|smashed|cracked|gave way)\b",
           "cut": r"\b(sliced|carved|cut (away|free|through)|peeled|parted the)\b",
           "open_door": r"\b(swung|swings|opened|opens|gave way|stepped (through|inside)|step (through|inside)|i am (inside|through)|threshold)\b",
           "tame": r"\b(follows?|followed|at my side|by my side|trots|heels|calm(ed)? (now|beside)|tamed|my companion|nuzzl\w*|obey\w*)\b",
           "stalk": r"\b(he (went|goes|led|leads|turned|entered|stopped)|i (saw|see|watched|followed) (him|where)|led me)\b",
           "hide_and_wait": r"\b(i (hid|hide|watched|watch|lay|crouched|nestled|tucked)|hidden|concealed|unseen in)\b"}.get(verb)
    if not pat:
        return []
    return [c.strip() for c in clauses(text) if re.search(pat, c, re.I) and not re.search(NEG, c, re.I)]

def score(paths, show):
    rows = [json.loads(l) for p in paths for l in open(p)]
    agg = {}
    for d in rows:
        t = d["output"]
        flags = {}
        if d["verb"] == "unlock_door":
            o = door_open(t)
            if o: flags["door_open"] = o
        ts = tool_sought(t, d["verb"])
        if ts and d["kind"] == "Outcome": flags["tool_sought"] = ts
        tf = tool_found(t, d["verb"])
        if tf and d["kind"] == "Outcome": flags["tool_found"] = tf
        ad = act_done(t, d["verb"]) if d["kind"] == "Outcome" else []
        if ad: flags["act_done"] = ad
        if d["kind"] in ("Observation", "Reasoning") and d["verb"] == "unlock_door" and not LOCK_KEPT.search(t):
            flags["lock_dropped"] = ["(the locked state is not said)"]
        k = (d["variant"], d["context"])
        a = agg.setdefault(k, dict(n=0, door_open=0, tool_found=0, tool_sought=0, act_done=0, lock_dropped=0, any=0))
        a["n"] += 1
        for f in flags: a[f] += 1
        a["any"] += bool(set(flags) - {"tool_sought", "lock_dropped"})   # reported, not counted as invented
        d["flags"] = flags
        if flags and show:
            print(f"[{d['variant']}/{d['context']}/{d['persona']}/{d['seed']}] {list(flags)}\n   {t}\n")
    print(f"{'variant':12} {'context':22} {'n':>4} {'open':>5} {'tool':>5} {'srch':>5} {'done':>5} {'nolock':>6} {'bad%':>6}")
    for (v, c), a in sorted(agg.items(), key=lambda x: (x[0][1], x[0][0])):
        print(f"{v:12} {c:22} {a['n']:4} {a['door_open']:5} {a['tool_found']:5} {a['tool_sought']:5} {a['act_done']:5} {a['lock_dropped']:6} {100*a['any']/a['n']:5.0f}%")
    return rows


if __name__ == "__main__":
    ap = argparse.ArgumentParser()
    sub = ap.add_subparsers(dest="cmd", required=True)
    r = sub.add_parser("run"); r.add_argument("--out", required=True); r.add_argument("--variant", default="base")
    r.add_argument("--n", type=int, default=6); r.add_argument("--only", default="")
    s = sub.add_parser("score"); s.add_argument("paths", nargs="+"); s.add_argument("--show", action="store_true")
    a = ap.parse_args()
    if a.cmd == "run":
        run(a)
    else:
        score(a.paths, a.show)
