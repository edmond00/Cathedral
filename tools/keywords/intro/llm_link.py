# The LLM method: one extra request per introduction, asking the shipped model which word of the
# text names each object. Greedy, grammar-constrained to one line per object. Cached to a jsonl.
import json, re, sys, time, urllib.request
L = "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
SYSTEM = ("You link the words of a short text to the things they name. You answer only with the "
          "requested lines, copying words exactly as they are written in the text.")
def prompt(text, pool):
    things = "\n".join(f"{L[i]} - {re.sub(r'\s+', ' ', p['name'].lower())}" for i, p in enumerate(pool))
    return (f"Text: \"{text}\"\n\nThings:\n{things}\n\n"
            "For each thing, write its letter, a colon, and the one word of the text that names that thing. "
            "Write - when the text does not mention it. A word that names the place itself, or something "
            "not in the list, belongs to no thing. Use each word at most once.")
def grammar(n, words):
    """One line per object; the answer is "-" or one of the text's candidate words, so nothing is invented."""
    lines = " ".join(f'"{L[i]}: " ans "\\n"' for i in range(n))
    alts = " | ".join(json.dumps(w) for w in words)
    return f'root ::= {lines}\nans ::= "-" | {alts}'
def ask(text, pool, words, port=8091):
    body = dict(model="local", messages=[dict(role="system", content=SYSTEM), dict(role="user", content=prompt(text, pool))],
                max_tokens=12 * len(pool) + 20, temperature=0, grammar=grammar(len(pool), words), cache_prompt=True)
    req = urllib.request.Request(f"http://127.0.0.1:{port}/v1/chat/completions", json.dumps(body).encode(), {"Content-Type": "application/json"})
    with urllib.request.urlopen(req, timeout=900) as r: out = json.load(r)["choices"][0]["message"]["content"]
    links = {}
    for line in out.splitlines():
        m = re.match(r"([A-Z]): (.+)$", line.strip())
        if m and m.group(2) != "-": links[L.index(m.group(1))] = m.group(2)
    return out, links
if __name__ == "__main__":
    # args: cases.jsonl labels.txt prefix cache.jsonl
    cases = [json.loads(l) for l in open(sys.argv[1], encoding="utf-8")]
    tags = {json.loads(l)["id"]: json.loads(l) for l in open(sys.argv[5], encoding="utf-8")}
    ids = [l.split("|")[0].strip() for l in open(sys.argv[2], encoding="utf-8") if l.strip()]
    cache = {}
    try:
        for l in open(sys.argv[4], encoding="utf-8"): o = json.loads(l); cache[o["cid"]] = o
    except FileNotFoundError: pass
    with open(sys.argv[4], "a", encoding="utf-8") as f:
        for cid in ids:
            if cid in cache: continue
            c = cases[int(cid[len(sys.argv[3]):])]
            words = sorted({x[0] for x in tags[int(cid[len(sys.argv[3]):])]["cands"]}, key=lambda w: -len(w))
            t = time.time(); out, links = ask(c["text"], c["pool"], words)
            f.write(json.dumps(dict(cid=cid, out=out, links=links, secs=round(time.time() - t, 1)), ensure_ascii=False) + "\n"); f.flush()
            print(cid, round(time.time() - t, 1), flush=True)
