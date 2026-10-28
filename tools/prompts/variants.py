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


VARIANTS = {f.__name__: f for f in [base, reminder_is_voice, voice_and_style_one_clause, no_reminder]}
