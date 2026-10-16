# Rewrites each dumped area introduction with the shipped model, as the game does: a real persona's
# system prompt and user template from the logs, the game's grammar and sampling settings.
import json, random, re, sys, time, urllib.request
scenes = [json.loads(l) for l in open(sys.argv[1], encoding="utf-8")]
P = json.load(open("personas.json", encoding="utf-8"))
out_path = sys.argv[2]; limit = int(sys.argv[3]) if len(sys.argv) > 3 else len(scenes)
done = set()
try:
    for l in open(out_path, encoding="utf-8"): done.add(json.loads(l)["n"])
except FileNotFoundError: pass
rng = random.Random(7)
order = list(range(len(scenes))); rng.shuffle(order)
with open(out_path, "a", encoding="utf-8") as f:
    for n in order[:limit]:
        p = P[rng.randrange(len(P))]
        if n in done: continue
        s = scenes[n]
        place = s["key"]; art = "an" if place[0] in "aeiou" else "a"
        user = p["user"].replace("<<NEUTRAL>>", s["neutral"])
        user = re.sub(r"You are in an? [^—]+ — write only", f"You are in {art} {place} — write only", user)
        body = dict(model="local", messages=[dict(role="system", content=p["system"]), dict(role="user", content=user)],
                    max_tokens=512, temperature=0.7, top_k=12, top_p=0.95, cache_prompt=True)
        if p["grammar"]: body["grammar"] = p["grammar"]
        t = time.time()
        req = urllib.request.Request("http://127.0.0.1:8091/v1/chat/completions", json.dumps(body).encode(), {"Content-Type": "application/json"})
        with urllib.request.urlopen(req, timeout=900) as r: text = json.load(r)["choices"][0]["message"]["content"].strip()
        s = dict(s); s["n"] = n; s["text"] = text; s["persona"] = p["system"][:60]; s["secs"] = round(time.time() - t, 1)
        f.write(json.dumps(s, ensure_ascii=False) + "\n"); f.flush()
        print(n, s["secs"], flush=True)
