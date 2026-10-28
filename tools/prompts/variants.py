"""Candidate wordings, each a function returning a template set (prompt_lib.variant(...)).

Each round changes one thing against the current best and is measured on the dev split. What won is
kept in the chain below; what lost stays here, with the reason in NOTES.md, so nothing is tried twice.
"""
import prompt_lib as P

T0 = P.T0


def base():
    return P.variant()


# ── the character reminder ────────────────────────────────────────────────────────────────────────
# Seen in the first corpus run: a reminder written as a picture ("someone who knows how a sung breath
# calms a beating heart", "someone who already knows where the floorboards betray a step") comes back
# as a thing in the scene — "a world where gentle songs soothe a beating heart", "the creak of the
# floorboards". The style already carries a clause saying what it names is not in the scene; the
# reminder carries none.

def _footer(T, kind, reminder2, style, place, character, style_fmt=None, order=None):
    """AnswerInstructionFor / DialogueAnswerInstructionFor with the character clause and the style
    clause swappable, the rest as the game has it."""
    if kind in ("DialogueReplica", "Speaking"):
        style_s = T["DialogueStyleInstruction"] if not (style and style.strip()) else \
            f"{style.strip()} {T['DialogueStyleInstruction']}"
        return " ".join(p for p in [character(reminder2, spoken=True), style_s, P.reminder(T, place)] if p)
    include_length = kind not in ("Observation", "Reasoning", "Outcome")
    style_s = (style_fmt or (lambda s: T["DefaultStyleInstruction"] if not (s and s.strip())
                             else f"Your way of seeing: {s.strip()} {T['StyleIsNotSceneClause']}"))(style)
    parts = [T["OneSentenceClause"] if include_length else None, T["GroundingClause"], style_s,
             None if include_length else T["ShortTextClause"], character(reminder2, spoken=False),
             P.reminder(T, place)]
    return " ".join(p for p in parts if p)


def reminder_is_voice():
    """The reminder said as a voice, and said not to be in the scene, beside the style."""
    def character(r2, spoken):
        if r2 is None:
            return "Speak in your own character." if spoken else "Stay in character."
        return (f"Speak as {r2} would." if spoken
                else f"Speak as {r2} — that is who is speaking, not something in the scene.")
    return P.variant(footer_for=lambda T, k, r2, st, pl: _footer(T, k, r2, st, pl, character))


def voice_and_style_one_clause():
    """Reminder and style stated together, followed by ONE not-in-the-scene clause covering both."""
    def footer(T, kind, r2, style, place):
        if kind in ("DialogueReplica", "Speaking"):
            return P.dialogue_answer_instruction(T, r2, style, place)
        include_length = kind not in ("Observation", "Reasoning", "Outcome")
        who = f"You speak as {r2}." if r2 else ""
        how = f"Your way of seeing: {style.strip()}" if style and style.strip() else T["DefaultStyleInstruction"]
        notscene = ("This only colours how the given facts are told — nothing it names is in the scene "
                    "unless the given information puts it there.")
        parts = [T["OneSentenceClause"] if include_length else None, T["GroundingClause"], who, how, notscene,
                 None if include_length else T["ShortTextClause"], P.reminder(T, place)]
        return " ".join(p for p in parts if p)
    return P.variant(footer_for=footer)


def no_reminder():
    """The character clause dropped: the persona is the system prompt already."""
    return P.variant(footer_for=lambda T, k, r2, st, pl: _footer(
        T, k, r2, st, pl, lambda r, spoken: ("Speak in your own character." if spoken else None)))


# ── modifiers, stacked on whatever has won so far ────────────────────────────────────────────────

import re as _re

# The hour each playthrough was at, read off its first area introduction ("I find myself …, at
# dawn.") rather than its --period flag: an unknown period (dusk) is ignored and the place's own hour
# is used, so the flag can lie.
RUN_PERIOD = {}
import glob as _glob, json as _json, os as _os
for _f in _glob.glob(_os.path.join(_os.path.dirname(__file__), "runs", "*", "session", "rewrite_inputs.jsonl")):
    for _l in open(_f):
        _m = _re.match(r"I find myself .*?, ((?:at|in) [a-z ]+)\.", _json.loads(_l)["neutralText"])
        if _m:
            RUN_PERIOD[_f.split(_os.sep)[-3]] = _m.group(1)
            break


# TimePeriod's own adverbial phrase, as the area introduction says it.
_tp = open(__file__.replace("tools/prompts/variants.py", "src/game/narrative/TimePeriod.cs")).read()
PHRASE = {m.group(1).lower(): m.group(2) for m in _re.finditer(r'TimePeriod\.(\w+)\s*=>\s*"((?:at|in) [^"]*)"', _tp)}


def with_hour(T):
    """The reminder names the hour beside the place: "You are in a village, at dusk". The model
    otherwise furnishes any hour with its favourite light (a morning action told "as the night draws
    in"). The hour is the run's --period; the game would set it with the place."""
    def reminder(T2, place, r=None):
        hour = RUN_PERIOD.get((r or {}).get("_run"))
        if not place:
            return T2["ReminderSetting"]
        at = f", {hour}" if hour else ""
        return f"{T2['ReminderSetting']} You are in {P.article(place)} {place}{at} — write only what belongs there."
    T = dict(T, reminder_r=reminder)
    return T


def short_setting(T):
    """The per-request setting sentence without "a medieval world": that phrase, and the system
    rule's "iron, myth and hand labour", came back as scene in the text ("every medieval world act",
    "this medieval village where iron and myth held sway"). The system prompt keeps the full rule."""
    def reminder(T2, place, r=None):
        return f"You are in {P.article(place)} {place} — write only what belongs there, and nothing modern." if place \
            else "Write nothing modern."
    return dict(T, reminder_r=reminder)


def chain(*mods):
    def make():
        T = base()
        for m in mods:
            T = m(T)
        return T
    make.__name__ = "+".join(m.__name__ for m in mods) or "base"
    return make


def sentence_last(T):
    """The sentence moved from the top of the prompt to the bottom. Round 1 showed the model copying
    whatever text sits last — the style, the not-in-the-scene clause, the setting rule — into its
    answer. With the sentence last, the facts are the last thing it reads. Same words, same tokens."""
    return dict(T, BuildPrompt=(
        'Re-express a sentence in your own voice. It is written in the first person, and that "I" is you '
        "— it describes your own perception, thought or action, not anyone else's.{other}\n\n"
        "{instruction}\n{footer}\n\n"
        'The sentence: "{neutralText}"'))


def closed_world(T):
    """The grounding clause made concrete, naming what round 1 caught the model inventing: memories,
    weather, sounds and smells, people, things done."""
    return dict(T, GroundingClause="Name only what the sentence names: add no object, person, place, sound, "
                                   "smell, weather, memory or event of your own.")


# ── neutral-line fixes (NeutralNarration, ThinkingExecutor), applied to a record's inputs ───────────

# A person's label as the observation of them gave it ("the blacksmith of the village (a woman)"),
# keyed by the start of their appearance clause, so a reasoning that pasted the clause can name them.
_LABELS = {}
for _f in _glob.glob(_os.path.join(_os.path.dirname(__file__), "runs", "*", "session", "rewrite_inputs.jsonl")):
    for _l in open(_f):
        _r = _json.loads(_l)
        _m = _re.match(r"My attention (?:is drawn|shifts) to (.+?)\. (.+)", _r["neutralText"])
        if _m and _r.get("observedOther"):
            _LABELS.setdefault(_m.group(2)[:40].lower(), _m.group(1))


def _person_named(r):
    """ThinkingExecutor names a person the way the observation did, never by their appearance clause:
    "I notice someone hammers a length of iron…; a worry sits behind the eyes…" handed every mind the
    blacksmith's worry as its own (8 of 8 rewrites in round 1), and dropped that she is a woman."""
    if r["kind"] != "Reasoning":
        return r
    m = _re.match(r"I (notice|remember) (.+?)((?:\. |, but ).*)$", r["neutralText"])
    if not m:
        return r
    label = _LABELS.get(m.group(2)[:40].lower())
    return dict(r, neutralText=f"I {m.group(1)} {label}{m.group(3)}") if label else r


def _managed(r):
    """"I succeeded to X" is not English; "I managed to X" says the same."""
    return dict(r, neutralText=r["neutralText"].replace("I succeeded to ", "I managed to "))


def _discreetly(r):
    """ActionIntent wrote "discretely" (separately) for "discreetly" (unseen)."""
    return dict(r, neutralText=r["neutralText"].replace(" discretely ", " discreetly "))


_LESSON = _re.compile(r"(, )?learned ([a-z' -]+?)([.,])")


def lesson_sentence(r):
    """The lesson out of the consequence list into a sentence of its own, last: "… I made my way to the
    coop. And I learned trailcraft." Inside the list it was the clause the rewrite dropped (half the
    outcomes in rounds 1-3)."""
    n = r["neutralText"]
    m = _LESSON.search(n)
    if not m or r["kind"] not in ("Outcome", "Emotion"):
        return r
    if m.group(1):   # "…, learned X." → "…. And I learned X."
        n = n[:m.start()] + "." + f" And I learned {m.group(2)}{m.group(3)}" + n[m.end():]
    return dict(r, neutralText=n)


def lesson_craft(r):
    """The lesson named as a craft: "learned the craft of water voice". A modus mentis's name read as a
    common phrase ("water voice", "iron fist") was rewritten into what it sounds like — "earth's song",
    "my water voice"."""
    if r["kind"] not in ("Outcome", "Emotion"):
        return r
    return dict(r, neutralText=_LESSON.sub(lambda m: f"{m.group(1) or ''}learned the craft of {m.group(2)}{m.group(3)}",
                                           r["neutralText"]))


def lesson_new(T):
    return dict(T, neutral_fix=T.get("neutral_fix", []) + [lesson_sentence])


def lesson_named(T):
    return dict(T, neutral_fix=T.get("neutral_fix", []) + [lesson_craft])


def no_style(T):
    """The persona's style left out of the request (its persona prompt is still the system prompt).
    The style is the text the answers copy most (14% of base narration samples carried a run of its
    words — "strings in hand", "the offer declined, the tone noted")."""
    return dict(T, footer_for=lambda T2, k, r2, st, pl: P.footer_for({x: y for x, y in T2.items() if x != "footer_for"},
                                                                     k, r2, None, pl)
                if k not in ("DialogueReplica", "Speaking")
                else P.footer_for({x: y for x, y in T2.items() if x != "footer_for"}, k, r2, st, pl),
                DefaultStyleInstruction="")


def person_only(T):
    return dict(T, neutral_fix=T.get("neutral_fix", []) + [_person_named])


def managed_only(T):
    return dict(T, neutral_fix=T.get("neutral_fix", []) + [_managed])


def neutral_fixes(T):
    return dict(T, neutral_fix=[_person_named, _managed, _discreetly])


def no_notscene(T):
    """StyleIsNotSceneClause dropped. Rounds 1-2 caught it copied word for word into the answer
    ("it only colours how the given facts are told") by Pride, Dread, Arithmetic: a clause the model
    repeats is a clause it reads as material."""
    return dict(T, StyleIsNotSceneClause="")


_KIND_SHORT = {
    "Observation": "Say what you perceive.",
    "Reasoning": "Say it as your own thought: what draws you, what you want, and how you will do it.",
    "Action": "Say what you will do. The act and its target stay exactly as they are.",
    "Outcome": "Say what happened. Whether it succeeded or failed stays as it is, and so does everything it brought.",
    "Emotion": "Say it as the feeling it left in you. The feeling stays as it is: do not soften or replace it.",
}


def minimal(T):
    """Every narration prompt cut to what it must carry: the sentence, whose "I" it is, the kind's
    task in one sentence, keep-and-invent-nothing, the persona's style and reminder, the length, the
    place. 40% fewer instruction words."""
    def build(T2, r):
        kind = r["kind"]
        if kind in ("DialogueReplica", "Speaking"):
            return P.build_prompt({k: v for k, v in T2.items() if k != "build"}, r)
        style = r.get("styleInstruction")
        r2 = r.get("personaReminder2")
        other = r.get("observedOther") if kind == "Observation" else None
        parts = [f'Re-express this in your own voice: "{r["neutralText"]}"', "",
                 'The "I" is you.' + (f" {other.strip()[0].upper() + other.strip()[1:]} is someone else, not you: "
                                       "what the sentence says they are or do is theirs." if other and other.strip() else ""),
                 "",
                 " ".join(p for p in [
                     _KIND_SHORT[kind], "Keep every fact; invent none.",
                     f"Your way of seeing: {style.strip()}" if style and style.strip() else None,
                     f"Speak as {r2}." if r2 else None,
                     T2["OneSentenceClause"] if kind in ("Action", "Emotion") else "One to three sentences.",
                     f"You are in {P.article(r['place'])} {r['place']}; nothing modern." if r.get("place") else "Nothing modern.",
                 ] if p)]
        return "\n".join(parts)
    return dict(T, build=build)


def spoken_compact(T):
    """BuildSpokenPrompt with the line stated once, at the end, and the task, the operations and the
    output shape in one paragraph each. The statement-to-question flips of round 1 ("My name is Dennis"
    asked back as a question) get their own words: a statement stays a statement."""
    return dict(T, BuildSpokenPrompt=(
        "{situation}\n\n"
        "Say the line below again in your own words. Change the word order, use other words, add an "
        "interjection or a figure of speech, so that it sounds like you; never change what it means. Add "
        "nothing and drop nothing: a question stays a question and a statement stays a statement.\n\n"
        "{footer}\n\n"
        'Answer with the opening "{frame.Trim()}" and your words in quotes, nothing else.\n\n'
        "The line: {framed}"))


VARIANTS = {f.__name__: f for f in [base, reminder_is_voice, voice_and_style_one_clause, no_reminder]}
for _c in [chain(sentence_last), chain(closed_world), chain(sentence_last, closed_world), chain(with_hour),
           chain(short_setting), chain(neutral_fixes), chain(spoken_compact), chain(no_notscene), chain(minimal),
           chain(managed_only), chain(person_only), chain(no_style), chain(lesson_new), chain(lesson_named), chain(managed_only, lesson_new)]:
    VARIANTS[_c.__name__] = _c


def final():
    """The wording the game now sends: the working tree's templates (StyleIsNotSceneClause gone) and
    the neutral-line changes the game makes upstream of the rewrite (a person named by their label,
    "managed to", "discreetly", the lesson in its own sentence), applied to the recorded inputs."""
    return dict(P.T_NOW, neutral_fix=[_person_named, _managed, _discreetly, lesson_sentence])


VARIANTS["final"] = final
