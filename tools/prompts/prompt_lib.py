"""The persona-rewrite prompts, rebuilt offline exactly as the game builds them.

Every string the game sends is read out of the C# source at import time — the clauses in
Config.Narrative, the instructions and frames in PersonaRewriter, the setting rule and reminder in
SceneSetting — and assembled by functions that mirror the C# methods line for line
(BuildPrompt, BuildSpokenPrompt, FooterFor, AnswerInstructionFor, ...). `check_mirror` proves the
mirror against a corpus the game recorded: every recorded prompt must come out byte-identical.

A *variant* is a dict of overrides on top of the source (`T` below): a clause replaced, a template
swapped. That is how wording is iterated without a rebuild of the game; the winner is then ported to
the C#, after which the source alone reproduces it and the variant is no longer needed.

Needs a llama-server on --port (default 8092) loaded with the shipped model, started the way the
game starts it (see serve.sh).
"""
import copy, json, os, re, time, urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PORT = int(os.environ.get("PROMPT_PORT", "8092"))


def _read(rel):
    return open(os.path.join(ROOT, rel), encoding="utf-8").read()


def _unescape(s):  # a regular C# string literal body -> text
    return re.sub(r'\\(.)', lambda m: {"n": "\n", "t": "\t"}.get(m.group(1), m.group(1)), s)


def _cs_string(src, name):
    """`name = "..."` or `name => "..."`, possibly as `"a" + "b"` concatenations."""
    m = re.search(r'\b' + re.escape(name) + r'\s*(?:=>|=)\s*((?:"(?:[^"\\]|\\.)*"\s*\+?\s*)+)[;,]', src)
    if not m:
        raise KeyError(name)
    return "".join(_unescape(p) for p in re.findall(r'"((?:[^"\\]|\\.)*)"', m.group(1)))


def _switch_arm(src, method, key):
    """The string of one arm of a C# switch expression inside `method`."""
    body = src[src.index(method):]
    m = re.search(r'\b' + re.escape(key) + r'\s*=>\s*((?:"(?:[^"\\]|\\.)*"\s*\+?\s*)+)[,;]', body)
    return "".join(_unescape(p) for p in re.findall(r'"((?:[^"\\]|\\.)*)"', m.group(1)))


def load_templates():
    """Every piece of wording the rewrite prompts are made of, as the C# source has it now."""
    cfg = _read("src/Config.cs")
    cfg = cfg[cfg.index("public static class Narrative"):]
    pr = _read("src/game/narrative/PersonaRewriter.cs")
    ss = _read("src/game/narrative/SceneSetting.cs")
    t = {}
    for name in ["OneSentenceClause", "ShortTextClause", "GroundingClause", "DefaultStyleInstruction",
                 "StyleIsNotSceneClause", "DialogueStyleInstruction"]:
        t[name] = _cs_string(cfg, name)
    t["Rule"] = _cs_string(ss, "Rule")
    t["ReminderSetting"] = re.search(r'const string setting = "((?:[^"\\]|\\.)*)";', ss).group(1)
    for kind in ["Observation", "Reasoning", "Action", "Outcome", "Emotion"]:
        t["Instruction." + kind] = _switch_arm(pr, "InstructionFor(NarrationKind kind)", "NarrationKind." + kind)
    # The verbatim templates, with their C# interpolation holes kept as {name} for format().
    t["BuildPrompt"] = _verbatim_template(pr, "private static string BuildPrompt(")
    t["BuildSpokenPrompt"] = _verbatim_template(pr, "private static string BuildSpokenPrompt(")
    t["OtherPersonNote"] = _interp_concat(pr, "private static string OtherPersonNote(")
    t["DialogueSituation"] = re.search(r'\$"(You are \{speaker\}\.[^"]*)"', pr).group(1)
    t["DialogueContext"] = re.search(r': \$" (You are speaking of [^"]*)"', pr).group(1)
    t["SpeakingSituation"] = re.search(r'\$"(You are speaking directly to \{who\}, calling[^"]*)"', pr).group(1)
    t["ReminderPlace"] = re.search(r': \$"\{setting\} (You are in [^"]*)"', ss).group(1)
    return t


def _verbatim_template(src, method):
    body = src[src.index(method):]
    m = re.search(r'\$@"((?:[^"]|"")*)"', body)
    return m.group(1).replace('""', '"').replace("\r\n", "\n")


def _interp_concat(src, method):
    body = src[src.index(method):]
    body = body[body.index("return"):]
    body = body[:body.index('";') + 2]
    return "".join(_unescape(p) for p in re.findall(r'\$"((?:[^"\\]|\\.)*)"', body))


T0 = load_templates()


def _fill(template, **kw):
    """C# interpolation: {name} and {expr.Trim()} style holes, filled from kw by their leading name."""
    def sub(m):
        key = re.split(r"[^\w]", m.group(1))[0]
        return str(kw[key])
    return re.sub(r"\{([^{}]+)\}", sub, template)


# ── the mirror of the C# ──────────────────────────────────────────────────────────────────────────

def article(noun):
    return "an" if noun and noun[0].lower() in "aeiou" else "a"


def reminder(T, place, extra=None):
    """SceneSetting.Reminder()."""
    if "reminder" in T:
        return T["reminder"](T, place)
    if not place:
        return T["ReminderSetting"]
    return T["ReminderSetting"] + " " + _fill(T["ReminderPlace"], Article=article(place), Place=place)


def answer_instruction(T, reminder2, style, include_length=True, place=""):
    style_s = T["DefaultStyleInstruction"] if not (style and style.strip()) else \
        f"Your way of seeing: {style.strip()} {T['StyleIsNotSceneClause']}"
    character = f"Stay in the character of {reminder2}." if reminder2 is not None else "Stay in character."
    parts = [T["OneSentenceClause"] if include_length else None, T["GroundingClause"], style_s,
             None if include_length else T["ShortTextClause"], character, reminder(T, place)]
    return " ".join(p for p in parts if p)


def dialogue_answer_instruction(T, reminder2, style, place=""):
    style_s = T["DialogueStyleInstruction"] if not (style and style.strip()) else \
        f"{style.strip()} {T['DialogueStyleInstruction']}"
    character = f"Speak as {reminder2} would." if reminder2 is not None else "Speak in your own character."
    return f"{character} {style_s} {reminder(T, place)}"


def footer_for(T, kind, reminder2, style, place):
    if "footer_for" in T:                 # a variant may replace the footer
        return T["footer_for"](T, kind, reminder2, style, place)
    if kind in ("DialogueReplica", "Speaking"):
        return dialogue_answer_instruction(T, reminder2, style, place)
    if kind in ("Observation", "Reasoning", "Outcome"):
        return answer_instruction(T, reminder2, style, include_length=False, place=place)
    return answer_instruction(T, reminder2, style, place=place)


def other_person_note(T, other):
    label = other.strip()
    head = label[0].upper() + label[1:]
    return _fill(T["OtherPersonNote"], head=head, label=label)


def reply_frame(addressee):
    who = addressee.strip() if addressee and addressee.strip() else "them"
    return f"I say to {who} : "


def build_spoken(T, neutral, addressee, situation, footer):
    frame = reply_frame(addressee)
    framed = f'{frame}"{neutral.strip().strip(chr(34))}"'
    tpl = T["BuildSpokenPrompt"].replace("{frame.Trim()}", "{frameTrimmed}")
    return _fill(tpl, situation=situation, framed=framed, frame=frame, frameTrimmed=frame.strip(),
                 footer=footer)


def build_prompt(T, r):
    """The user message the game sends for one recorded rewrite `r` (corpus record)."""
    if "build" in T:                      # a variant may replace the whole assembly
        return T["build"](T, r)
    kind = r["kind"]
    place = r.get("place") or ""
    footer = footer_for(T, kind, r.get("personaReminder2"), r.get("styleInstruction"), place)
    if kind == "DialogueReplica":
        who = r["addressee"].strip() if (r.get("addressee") or "").strip() else "the person in front of you"
        speaker = r["speakerName"].strip() if (r.get("speakerName") or "").strip() else "the speaker"
        ctx = "" if not (r.get("dialogueContext") or "").strip() else \
            " " + _fill(T["DialogueContext"], dialogueContext=r["dialogueContext"].strip().rstrip("."))
        return build_spoken(T, r["neutralText"], r.get("addressee"),
                            _fill(T["DialogueSituation"], speaker=speaker, who=who, context=ctx), footer)
    if kind == "Speaking":
        who = r["addressee"].strip() if (r.get("addressee") or "").strip() else "your companion"
        return build_spoken(T, r["neutralText"], r.get("addressee"),
                            _fill(T["SpeakingSituation"], who=who), footer)
    other = r.get("observedOther") if kind == "Observation" else None
    other_s = "" if not (other and other.strip()) else "\n\n" + other_person_note(T, other)
    return _fill(T["BuildPrompt"], neutralText=r["neutralText"], other=other_s,
                 instruction=T["Instruction." + kind], footer=footer)


def system_prompt(T, persona):
    """LlamaServerManager.CreateInstanceAsync: the persona closed by the setting rule."""
    return f"{persona.rstrip()}\n\n{T['Rule']}"


def grammar(r):
    """GBNF exactly as PersonaRewriter asks for it (GenerateRawTextGrammar / GenerateDialogueReplyGrammar)."""
    if r["kind"] in ("DialogueReplica", "Speaking"):
        frame = reply_frame(r.get("addressee")).replace("\\", "\\\\").replace('"', '\\"')
        return 'root ::= "' + frame + '\\"" [a-zA-Z] [-a-zA-Z0-9 .,?!\'`]{14,799} "\\""\n'
    prefix = r.get("forcedPrefix") or "I "
    p = '"' + prefix.replace("\\", "\\\\").replace('"', '\\"') + '"'
    return f"root ::= {p} [-a-zA-Z0-9 .,?!'`]{{15,800}}\n"


# ── the request ───────────────────────────────────────────────────────────────────────────────────

def split_history(r):
    """(persona system prompt without the rule, prior turns) from a recorded slot history."""
    hist = r.get("history") or []
    system = hist[0]["content"] if hist and hist[0]["role"] == "system" else ""
    persona = system[: system.rfind(T0["Rule"])].rstrip() if T0["Rule"] in system else system
    return persona, hist[1:]


def messages_for(T, r, persona=None, prior=None):
    p, h = split_history(r)
    persona = p if persona is None else persona
    prior = h if prior is None else prior
    return ([{"role": "system", "content": system_prompt(T, persona)}] + list(prior)
            + [{"role": "user", "content": build_prompt(T, r)}])


def _post(path, body, timeout=900):
    req = urllib.request.Request(f"http://127.0.0.1:{PORT}{path}", json.dumps(body).encode(),
                                 {"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=timeout) as resp:
        return json.load(resp)


def tokens(text):
    return len(_post("/tokenize", {"content": text})["tokens"])


def generate(messages, gbnf, seed, max_tokens=280):
    """One sample with the game's sampling settings (Config.LLM), seeded so variants pair up."""
    body = dict(model="local", messages=messages, max_tokens=max_tokens, stream=False, cache_prompt=True,
                top_k=12, temperature=0.7, top_p=0.95, grammar=gbnf, seed=seed)
    t0 = time.time()
    d = _post("/v1/chat/completions", body)
    wall = time.time() - t0
    tm = d.get("timings", {})
    return dict(text=d["choices"][0]["message"]["content"], wall=wall,
                prompt_tokens=d.get("usage", {}).get("prompt_tokens"),
                prompt_n=tm.get("prompt_n"), prompt_ms=tm.get("prompt_ms"),
                predicted_n=tm.get("predicted_n"), predicted_ms=tm.get("predicted_ms"))


def strip_frame(text, addressee):
    s = text.lstrip()
    f = reply_frame(addressee)
    return s[len(f):].lstrip() if s.lower().startswith(f.lower()) else s


# ── checking the mirror ───────────────────────────────────────────────────────────────────────────

def load_corpus(path):
    return [json.loads(l) for l in open(path, encoding="utf-8") if l.strip()]


def check_mirror(records, T=None):
    """Every recorded prompt rebuilt from its inputs; returns the mismatches."""
    T = T or T0
    bad = []
    for i, r in enumerate(records):
        got = build_prompt(T, r)
        if got != r["prompt"]:
            bad.append((i, r["kind"], got, r["prompt"]))
    return bad


def variant(**overrides):
    """A copy of the source templates with some pieces replaced."""
    T = copy.deepcopy({k: v for k, v in T0.items()})
    T.update(overrides)
    return T
