"""First person or second person: which voice should a modus mentis narrate in?

The game asks every modus mentis to re-express a neutral first-person line ("I see a well") as its
own first-person line ("I mark the well, its lip worn smooth by ropes"): the faculty *is* the "I".
The alternative measured here is the faculty as an inner voice addressing the person whose mind it
belongs to — "Look: a well, its lip worn smooth by ropes", "You should climb down while the light
holds". Same corpus, same personas, same sampling; only the voice the request asks for differs.

Three versions:

  first      the game's wording now (variants.final(): the working tree's templates and the neutral
             line as the game builds it today). Forced opening "I " / "I will ", as the game forces.
  second     the same neutral first-person line, the request asking it to be told TO the person as
             "you". No forced opening, so an imperative ("Look", "Go on") is possible.
  second_tn  the neutral line itself transposed to the second person ("You find yourself…"), so the
             model is not also asked to convert the grammar; the request as in `second`.

Only the narration kinds take part (Observation, Reasoning, Action, Outcome, Emotion). Dialogue
replies and calls to a companion are speech to another person and keep their own frame.
Nothing here is in the game: this is an experiment, see NOTES.md beside this file.
"""
import copy, os, re, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.dirname(HERE))
import prompt_lib as P          # noqa: E402
import variants as V            # noqa: E402
import personas as PS           # noqa: E402

KINDS = ("Observation", "Reasoning", "Action", "Outcome", "Emotion")

# ── the second-person wording ────────────────────────────────────────────────────────────────────
# Each instruction is the game's own (PersonaRewriter.InstructionFor) with the voice turned, and
# nothing else changed: the action's literal-facts sentence, the outcome's success-or-failure, the
# emotion's "do not soften it" all stay, so a difference in fidelity is the voice's and not a lost
# safeguard. No example openings ("Look!", "You should") are given: whatever the request names, the
# shipped model copies, and a tic would be measured as the voice's.

INTRO_SECOND = (
    'This sentence is written in the first person, and that "I" is the person whose mind you are part of — '
    "it describes their own perception, thought or action, not yours and not anyone else's. You are the "
    'voice inside them: tell it to them, addressing them as "you".')

INTRO_SECOND_TN = (
    'This sentence is written in the second person, and that "you" is the person whose mind you are part of — '
    "it describes their own perception, thought or action, not yours and not anyone else's. You are the "
    'voice inside them: tell it to them, addressing them as "you".')

INSTR_SECOND = {
    "Observation":
        "Tell them this perception in your own voice, keeping the same meaning.",
    "Reasoning":
        "Tell them this as your own counsel — what draws them, what they should do, and how they should go about it — while keeping the same meaning.",
    "Action":
        "Tell them this intended action in your own voice, concretely and naturally. The action they intend and its target are literal facts that must be preserved exactly: say plainly what they will do, never drop, blur, or replace the action itself — restyle only how it is told, not what is done.",
    "Outcome":
        "Tell them this result in your own voice — what happened to them and how it feels — while keeping the same meaning and whether it succeeded or failed.",
    "Emotion":
        "Tell them, in your own voice, the feeling it left in them. The stated feeling is a fact and must survive: do not soften it, argue with it, or replace it with a more flattering one. What happened is only the occasion — dwell on the feeling itself.",
}


def other_note_second(other):
    """PersonaRewriter.OtherPersonNote with its pronouns turned: the game's says "the one you are
    looking at, not you … never as "I"", which in the second person names the wrong people."""
    label = other.strip()
    head = label[0].upper() + label[1:]
    return (f"{head} is someone else: the one they are looking at, not them and not you. Everything the sentence "
            f"says {label} is, does or carries is theirs — write of them as he, she, it or they, never as \"you\" "
            f"or \"I\".")


def _build_second(intro):
    def build(T, r):
        kind = r["kind"]
        place = r.get("place") or ""
        footer = P.footer_for({k: v for k, v in T.items() if k != "build"}, kind,
                              r.get("personaReminder2"), r.get("styleInstruction"), place)
        other = r.get("observedOther") if kind == "Observation" else None
        other_s = "" if not (other and other.strip()) else "\n\n" + other_note_second(other)
        return (f'Re-express the following sentence in your own voice: "{r["neutralText"]}"\n\n'
                f"{intro}{other_s}\n\n{INSTR_SECOND[kind]}\n{footer}")
    return build


# ── transposing a neutral line to the second person ──────────────────────────────────────────────
_SUBST = [
    (r"\bI am\b", "you are"), (r"\bI'm\b", "you're"), (r"\bI was\b", "you were"),
    (r"\bam I\b", "are you"), (r"\bwas I\b", "were you"),
    (r"\bmyself\b", "yourself"), (r"\bmine\b", "yours"), (r"\bmy\b", "your"), (r"\bMy\b", "Your"),
    (r"\bme\b", "you"), (r"\bI\b", "you"),
]


def to_second(text):
    for pat, rep in _SUBST:
        text = re.sub(pat, rep, text)
    # sentence-initial capitals
    return re.sub(r"(^|[.!?]\s+)(you|your)\b", lambda m: m.group(1) + m.group(2).capitalize(), text)


def _transpose(r):
    r = dict(r, neutralText=to_second(r["neutralText"]))
    if r.get("observedOther"):           # the note names the other person; its "you" is right as is
        pass
    return r


# ── the three versions ───────────────────────────────────────────────────────────────────────────

def version(name):
    T = V.final()
    if name == "first":
        return T
    if name == "second":
        return dict(T, build=_build_second(INTRO_SECOND))
    if name == "second_tn":
        T = dict(T, build=_build_second(INTRO_SECOND_TN))
        T["neutral_fix"] = list(T.get("neutral_fix", [])) + [_transpose]
        return T
    raise KeyError(name)


VERSIONS = ("first", "second", "second_tn")


def grammar(name, r):
    """first: the game's own grammar (forced "I " / "I will "). Second person: no forced opening, the
    same charset and length as the game's raw-text grammar."""
    if name == "first":
        return P.grammar(r)
    return "root ::= [A-Za-z] [-a-zA-Z0-9 .,?!'`]{15,800}\n"


# ── personas: the current catalogue's fields where the case names a modus mentis ────────────────
_MMS = PS.load_modi_mentis()


def current_persona(case):
    p = case["persona"]
    m = _MMS.get(p["id"])
    if not m:
        return p                          # a loosened test persona, or an NPC: as recorded
    return dict(id=p["id"], prompt=m["prompt"], reminder2=m["reminder2"], style=m["style"])


def request(name, case):
    T = version(name)
    persona = current_persona(case)
    r = dict(case["record"], _run=case["run"])
    r["personaReminder2"] = persona.get("reminder2")
    r["styleInstruction"] = persona.get("style")
    msgs = P.messages_for(T, r, persona=persona["prompt"], prior=case.get("prior", []))
    # the record after the version's neutral fixes, for the metrics
    rr = dict(r)
    for fix in T.get("neutral_fix", []):
        rr = fix(rr)
    return rr, msgs, grammar(name, r), persona
