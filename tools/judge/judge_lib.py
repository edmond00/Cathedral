"""Shared plumbing for measuring the choice judge (src/game/narrative/ChoiceJudge.cs) offline.

Everything the judge reads is taken from the game's own source at import time — the six system
prompts, the questions, the willingness scale, the setting rule appended to every slot, the
decision thresholds, and every modus mentis's DisplayName / MenuDescription / PersonaTone /
SkillMeans — so a measurement here is a measurement of what the game sends, and cannot drift from it.

Needs a llama-server on --port (default 8091) loaded with the shipped model, e.g. on CPU:
    models/llama/llama-server.exe -m models/model.gguf -c 8192 -ngl 0 --port 8091
"""
import glob, json, math, os, re, urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PORT = int(os.environ.get("JUDGE_PORT", "8091"))
L = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"

def _read(rel):
    return open(os.path.join(ROOT, rel), encoding="utf-8").read()

def _verbatim(s):  # C# @"..." body -> text
    return s.replace('""', '"').replace("\r\n", "\n")

# ── the judge, as the game defines it ────────────────────────────────────────────────────────────
_judge = _read("src/game/narrative/ChoiceJudge.cs")
SYSTEM = {m.group(1): _verbatim(m.group(2))
          for m in re.finditer(r'\[ChoiceKind\.(\w+)\]\s*=\s*@"((?:[^"]|"")*)"', _judge)}
QUESTION = {m.group(1): m.group(2)
            for m in re.finditer(r'\[ChoiceKind\.(\w+)\]\s*=\s*"((?:[^"\\]|\\.)*)"', _judge)}
STANCE = {m.group(1): m.group(2)
          for m in re.finditer(r'"(eager|willing|reluctant|unwilling)"\s*=>\s*"([^"]*)"', _judge)}
CONST = {m.group(1): float(m.group(2))
         for m in re.finditer(r'public const double (\w+)\s*=\s*(-?[\d.]+);', _judge)}
_setting = _read("src/game/narrative/SceneSetting.cs")
SETTING = "".join(re.findall(r'"((?:[^"\\]|\\.)*)"',
                             re.search(r'public const string Rule\s*=(.*?);', _setting, re.S).group(1)))
KINDS = ["Focus", "KeepFocus", "Goal", "Means", "Willingness", "Reply"]
assert set(KINDS) <= set(SYSTEM) and set(KINDS) <= set(QUESTION), "ChoiceJudge.cs no longer parses"
SCALE = [STANCE[s] for s in ("eager", "willing", "reluctant", "unwilling")]

# ── the modi mentis ──────────────────────────────────────────────────────────────────────────────
def _prop(src, name):
    m = re.search(r'\b' + name + r'\s*=>\s*@"((?:[^"]|"")*)"', src)
    if m: return _verbatim(m.group(1))
    m = re.search(r'\b' + name + r'\s*=>\s*\n?\s*"((?:[^"\\]|\\.)*)"', src)
    return m.group(1).encode("latin-1", "backslashreplace").decode("unicode_escape") if m else None

def load_modi_mentis():
    """id -> {id, file, name, menu, tone, means, functions, moral}; only those with a description."""
    out = {}
    for f in sorted(glob.glob(os.path.join(ROOT, "src/game/narrative/modimentis/*ModusMentis.cs"))):
        src = open(f, encoding="utf-8").read()
        mid = _prop(src, "ModusMentisId")
        if not mid: continue
        funcs = re.search(r"Functions\s*=>\s*new\[\]\s*\{([^}]*)\}", src)
        moral = re.search(r"MoralLevel\s*=>\s*MoralLevel\.(\w+)", src)
        mm = dict(id=mid, file=os.path.relpath(f, ROOT), name=_prop(src, "DisplayName"),
                  menu=_prop(src, "MenuDescription"), tone=_prop(src, "PersonaTone"),
                  means=_prop(src, "SkillMeans"),
                  functions=re.findall(r"ModusMentisFunction\.(\w+)", funcs.group(1)) if funcs else [],
                  moral=moral.group(1) if moral else "Medium")
        if mm["menu"] and mm["means"]: out[mid] = mm
    return out

# ── the prompt, as ChoiceJudge.BuildPrompt writes it ─────────────────────────────────────────────
def describe(mm):
    s = f"{mm['name'].upper()} - {mm['menu'].strip()}"
    if mm.get("tone") and mm["tone"].strip(): s += f" Its manner: {mm['tone'].strip()}."
    return s

def third_person(t):
    parts = t.split('"')
    for i in range(0, len(parts), 2):
        s = parts[i]
        for a, b in [(r"\bYourself\b", "Themselves"), (r"\byourself\b", "themselves"), (r"\bYour\b", "Their"),
                     (r"\byour\b", "their"), (r"\bYou\b", "They"), (r"\byou\b", "they")]:
            s = re.sub(a, b, s)
        parts[i] = s
    return '"'.join(parts)

def build_prompt(kind, mm, situation, options, question=None):
    s = ("The thinking faculty: " if kind == "Means" else "The faculty: ") + describe(mm) + "\n\n"
    if situation and situation.strip():
        s += "The situation: " + third_person(re.sub(r"\s+", " ", situation.strip())) + "\n\n"
    s += (question or QUESTION[kind]) + "\n"
    s += "".join(f"{L[i]} - {third_person(o.strip())}\n" for i, o in enumerate(options))
    return s + "\nAnswer with the letter and nothing else."

def means_option(mm):
    """A means option as ThinkingExecutor labels it: "{DisplayName}: {SkillMeans}"."""
    return f"{mm['name']}: {mm['means']}"

# ── one forward pass, every letter's score ───────────────────────────────────────────────────────
def letter_logprobs(kind, prompt, n, system=None):
    """log P(letter) for the first n letters, as the reply's first token (absent = -40)."""
    body = dict(messages=[dict(role="system", content=(system or SYSTEM[kind]).rstrip() + "\n\n" + SETTING),
                          dict(role="user", content=prompt)],
                max_tokens=1, temperature=0, logprobs=True, top_logprobs=40, cache_prompt=True)
    req = urllib.request.Request(f"http://127.0.0.1:{PORT}/v1/chat/completions", json.dumps(body).encode(),
                                 {"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=600) as r:
        d = json.load(r)
    lp = [float("-inf")] * n
    for t in d["choices"][0]["logprobs"]["content"][0]["top_logprobs"]:
        tok = t["token"].strip()
        if len(tok) == 1 and tok in L[:n]:
            lp[L.index(tok)] = max(lp[L.index(tok)], t["logprob"])
    if all(x == float("-inf") for x in lp):
        raise RuntimeError("no offered letter among the top candidates")
    return [x if x != float("-inf") else -40.0 for x in lp]

def lse(v):
    m = max(v); return m + math.log(sum(math.exp(x - m) for x in v))

# ── the decision rules, as ChoiceJudge applies them ──────────────────────────────────────────────
def decide(kind, lp, decline_index=-1):
    """The option index the game would take from these scores."""
    if kind == "Goal" and decline_index >= 0:
        odds = lp[decline_index] - lse([x for i, x in enumerate(lp) if i != decline_index])
        if odds > CONST["GoalDeclineLogOdds"]: return decline_index
        return max((i for i in range(len(lp)) if i != decline_index), key=lp.__getitem__)
    return max(range(len(lp)), key=lp.__getitem__)

def willingness(lp):
    """'eager' | 'willing' | 'reluctant' | 'unwilling' from the four scale scores, as JudgeWillingnessAsync."""
    assent = lse(lp[:2]) - lse(lp[2:])
    if assent > CONST["AssentLogOdds"]:
        return "eager" if lp[0] - lp[1] >= CONST["EagerLogOdds"] else "willing"
    return "unwilling" if lp[3] - lp[2] >= CONST["UnwillingLogOdds"] else "reluctant"
