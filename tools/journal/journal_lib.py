"""Shared plumbing for measuring the chronicler (src/game/narrative/Chronicler.cs) offline.

Nothing the chronicler sends is copied here. The prompts — system and user, every variant — are
rendered by the game itself (`--journal-prompts <journals-dir> <out.jsonl>`, ChroniclerBench.cs) from
journals dumped by real runs (`journal dump <file>` in the CLI). The sampling settings are read out of
src/Config.cs. This module only sends what it is handed, and scores what comes back.
"""
import json, os, re, subprocess, time, urllib.request

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
PORT = int(os.environ.get("JOURNAL_PORT", "8092"))

_config = open(os.path.join(ROOT, "src/Config.cs"), encoding="utf-8").read()
def _const(name):
    return float(re.search(r"public const (?:double|int) " + name + r"\s*=\s*([\d.]+);", _config).group(1))
SAMPLING = dict(temperature=_const("Temperature"), top_k=int(_const("TopK")), top_p=_const("TopP"))
SERVER_ARGS = ["--repeat-penalty", str(_const("RepeatPenalty")), "--frequency-penalty", str(_const("FrequencyPenalty")),
               "--dry-multiplier", str(_const("DryMultiplier")), "--cache-type-k", "f16", "--cache-type-v", "f16"]

def start_server(threads=4, ctx=4096):
    exe = os.path.join(ROOT, "models", "llama-linux" if os.name != "nt" else "llama",
                       "llama-server" + (".exe" if os.name == "nt" else ""))
    args = [exe, "-m", os.path.join(ROOT, "models", "model.gguf"), "-c", str(ctx), "-np", "1", "-ngl", "0",
            "-t", str(threads), "--port", str(PORT)] + SERVER_ARGS
    p = subprocess.Popen(args, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    for _ in range(600):
        try:
            urllib.request.urlopen(f"http://127.0.0.1:{PORT}/health", timeout=2)
            return p
        except Exception:
            time.sleep(1)
    p.kill(); raise RuntimeError("llama-server did not come up")

def chat(system, user, max_tokens, seed=7):
    """One completion; returns (text, timings dict, wall seconds)."""
    body = dict(messages=[dict(role="system", content=system), dict(role="user", content=user)],
                max_tokens=max_tokens, cache_prompt=True, seed=seed, **SAMPLING)
    req = urllib.request.Request(f"http://127.0.0.1:{PORT}/v1/chat/completions", json.dumps(body).encode(),
                                 {"Content-Type": "application/json"})
    t0 = time.time()
    with urllib.request.urlopen(req, timeout=900) as r:
        d = json.load(r)
    return d["choices"][0]["message"]["content"], d.get("timings", {}), time.time() - t0

def clean(reply):
    """ChroniclerPrompt.Clean: the first paragraph, headings and label lines dropped."""
    text = reply.replace("\r", "").strip().strip('"')
    para = next((p for p in text.split("\n\n") if p.strip()), "")
    lines = [l for l in para.split("\n") if not l.lstrip().startswith("#") and not l.rstrip().endswith(":")]
    joined = " ".join(lines).strip()
    end = max(joined.rfind(c) for c in ".!?")
    return joined[:end + 1] if end > len(joined) // 2 else joined

# ── automated checks ─────────────────────────────────────────────────────────────────────────────
_STOP = set("""I The A An And But So Then When As At In On Of To For With My Our It Its He She They We You His Her
Their This That These Those There Here After Before Day Today Tonight Yet Still Now Later Once While Though
Although Even If Not No Nothing Everything Everyone Someone Somebody Each Every Some""".split())

def proper_names(text):
    """Capitalised words that are not sentence starts or common function words."""
    names = set()
    for m in re.finditer(r"(?<![.!?]\s)(?<!^)\b([A-Z][a-z]{2,})\b", text):
        if m.group(1) not in _STOP: names.add(m.group(1))
    return names

def source_text(row):
    return " ".join(e["text"] + " " + e["actor"] + " " + e["place"] + " " + e["area"] for e in row["entries"])

def invented_names(output, row):
    """Proper names in the output found nowhere in the input (case-insensitive) — a fidelity alarm."""
    src = source_text(row).lower()
    return sorted(n for n in proper_names(output) if n.lower() not in src)

def milestone_coverage(output, row):
    """Share of milestones whose key word (the last noun-ish word) appears in the output."""
    ms = [e["text"] for e in row["entries"] if e["kind"] == "Milestone"]
    if not ms: return None
    hit = 0
    for m in ms:
        words = [w for w in re.findall(r"[a-zA-Z]{4,}", m.lower()) if w not in ("learned", "first", "came", "wounded", "healed")]
        if any(w in output.lower() for w in words): hit += 1
    return hit / len(ms)

def ngram_overlap(a, b, n=4):
    """Share of b's word 4-grams already in a — repetition between consecutive paragraphs."""
    def grams(t):
        w = re.findall(r"[a-z']+", t.lower())
        return {tuple(w[i:i + n]) for i in range(len(w) - n + 1)}
    gb = grams(b)
    return len(gb & grams(a)) / len(gb) if gb else 0.0

def copied_share(output, row, n=6):
    """Share of the output's word 6-grams lifted verbatim from the notes."""
    def grams(t):
        w = re.findall(r"[a-z']+", t.lower())
        return {tuple(w[i:i + n]) for i in range(len(w) - n + 1)}
    go = grams(output)
    return len(go & grams(source_text(row))) / len(go) if go else 0.0

def person(text):
    """'first' / 'third' by pronoun counts — consistency of voice."""
    w = re.findall(r"\b(i|me|my|we|our|he|she|they|his|her|their)\b", text.lower())
    first = sum(x in ("i", "me", "my", "we", "our") for x in w)
    return "first" if first >= len(w) - first else "third"
