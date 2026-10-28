"""The personas the harness speaks through: every modus mentis's persona fields, read from the C#
source, and a few deliberately loosened ones written here as test inputs.

The loosened ones are not proposals for the game. They stand in for a persona that is vaguely or
briefly written, which the global instructions must still carry: a bare name, one vague line, a
name with a reminder but no style. Nothing here is ever ported to a modus mentis.
"""
import glob, os, re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))


def _prop(src, name):
    m = re.search(r'\b' + name + r'\s*=>\s*@"((?:[^"]|"")*)"', src)
    if m:
        return m.group(1).replace('""', '"').replace("\r\n", "\n")
    m = re.search(r'\b' + name + r'\s*=>\s*\n?\s*((?:"(?:[^"\\]|\\.)*"\s*\+?\s*)+);', src)
    if not m:
        return None
    parts = re.findall(r'"((?:[^"\\]|\\.)*)"', m.group(1))
    return "".join(re.sub(r'\\(.)', lambda x: {"n": "\n"}.get(x.group(1), x.group(1)), p) for p in parts)


def load_modi_mentis():
    """id -> {id, name, prompt, reminder2, style, tone}, for every modus mentis with a persona."""
    out = {}
    for f in sorted(glob.glob(os.path.join(ROOT, "src/game/narrative/modimentis/*ModusMentis.cs"))):
        src = open(f, encoding="utf-8").read()
        mid = _prop(src, "ModusMentisId")
        prompt = _prop(src, "PersonaPrompt")
        if not mid or not prompt:
            continue
        funcs = re.search(r"Functions\s*=>\s*new\[\]\s*\{([^}]*)\}", src)
        out[mid] = dict(id=mid, name=_prop(src, "DisplayName"), prompt=prompt,
                        reminder2=_prop(src, "PersonaReminder2"), style=_prop(src, "StyleInstruction"),
                        tone=_prop(src, "PersonaTone"),
                        functions=re.findall(r"ModusMentisFunction\.(\w+)", funcs.group(1)) if funcs else [])
    return out


# Test inputs only: how the global wording copes with a persona written loosely or barely at all.
LOOSENED = {
    "loose:greed_bare": dict(id="loose:greed_bare", name="Greed", prompt="You are the inner voice of GREED.",
                             reminder2=None, style=None, tone=None),
    "loose:melancholy_line": dict(id="loose:melancholy_line", name="Melancholy",
                                  prompt="You are the inner voice of MELANCHOLY. Everything reminds you of something lost.",
                                  reminder2=None, style=None, tone=None),
    "loose:pedant_reminder": dict(id="loose:pedant_reminder", name="Pedantry",
                                  prompt="You are the inner voice of PEDANTRY.",
                                  reminder2="a pedant", style=None, tone=None),
    "loose:hunger_vague": dict(id="loose:hunger_vague", name="Hunger",
                               prompt="You are hunger. You speak like hunger would.",
                               reminder2="hunger", style="Be hungry.", tone=None),
}


def identify(system_prompt, mms):
    """Which modus mentis (or NPC) a recorded slot's system prompt belongs to."""
    for mid, m in mms.items():
        if system_prompt.startswith(m["prompt"].strip()[:120]):
            return mid
    first = system_prompt.strip().split("\n")[0]
    m = re.match(r"You are the inner voice of (?:the )?([A-Z][A-Z' -]+)", first)
    if m:   # a modus mentis whose persona this loader could not read out of its source
        return "mm:" + m.group(1).strip().lower().replace(" ", "_")
    return "npc:" + re.sub(r"\W+", "_", first[:40]).strip("_").lower()
