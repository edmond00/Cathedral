"""Compare foreground LLM latency with and without the chronicler.

    python tools/journal/latency.py <log-with.txt> <log-without.txt>

Both logs come from the same script (tools/journal/latency.cli) at the same --seed and --llm-seed,
once as it is and once with --no-chronicler. Reads the [llm-timing] lines LlamaServerManager writes to
the log for every request, keeps the foreground ones, pairs them by order (the same script asks the same
questions in the same order), and reports time to first token and total time, per kind and per pair.
"""
import re, statistics as st, sys

def read(path):
    rows = []
    for line in open(path, encoding="utf-8", errors="replace"):
        m = re.search(r"\[llm-timing\] (.*)", line)
        if not m: continue
        f = dict(kv.split("=", 1) for kv in m.group(1).split() if "=" in kv)
        tok = m.group(1).split()
        f["fg"] = "fg" in tok; f["stream"] = "stream" in tok
        rows.append(f)
    return rows

def num(r, k): return float(r.get(k, "-1"))

def queued(r):
    """Client time not accounted for by the server's own prompt and generation time: waiting behind
    something else (a background batch finishing, another slot), plus transport. The direct measure of
    what a preempted job still cost."""
    return num(r, "total") - max(0, num(r, "prompt_ms")) - max(0, num(r, "gen_ms"))

def summary(name, rows):
    fg = [r for r in rows if r["fg"]]
    bg = [r for r in rows if not r["fg"]]
    st_ = [num(r, "ttft") for r in fg if r["stream"]]
    tot = [num(r, "total") for r in fg]
    print(f"{name}: {len(fg)} foreground ({sum(r['stream'] for r in fg)} streamed), {len(bg)} background "
          f"({sum(r.get('outcome') == 'ok' for r in bg)} ok, {sum(r.get('outcome') == 'preempted' for r in bg)} preempted)")
    if st_: print(f"   stream ttft ms: median {st.median(st_):.0f}  mean {st.mean(st_):.0f}  max {max(st_):.0f}")
    if tot: print(f"   total ms:       median {st.median(tot):.0f}  mean {st.mean(tot):.0f}  sum {sum(tot)/1000:.0f}s")
    q = [queued(r) for r in fg]
    if q: print(f"   queued ms (total − server prompt − server gen): median {st.median(q):.0f}  mean {st.mean(q):.0f}  max {max(q):.0f}")
    delayed = [num(r, "bg_delay") for r in fg if num(r, "bg_delay") > 0]
    print(f"   foreground requests that preempted a background job: {len(delayed)}"
          + (f", its release took {st.mean(delayed):.0f} ms mean, {max(delayed):.0f} max" if delayed else ""))
    return fg

w = summary("WITH chronicler", read(sys.argv[1]))
o = summary("WITHOUT chronicler", read(sys.argv[2]))
n = min(len(w), len(o))
print(f"\nPaired by order ({n} pairs; a pair whose prompt sizes differ is marked *):")
print(" #  kind    prompt_n w/o   ttft w/o (ms)     total w/o (ms)     cache_n w/o   bg_delay  queued w/o")
dt, dtt = [], []
for i in range(n):
    a, b = w[i], o[i]
    mark = "*" if a.get("prompt_n") != b.get("prompt_n") and abs(num(a, "prompt_n") + num(a, "cache_n") - num(b, "prompt_n") - num(b, "cache_n")) > 8 else " "
    kind = "stream" if a["stream"] else "oneshot"
    print(f"{i+1:2}{mark} {kind:7} {a.get('prompt_n'):>5} {b.get('prompt_n'):>5}  {num(a,'ttft'):7.0f} {num(b,'ttft'):7.0f}  "
          f"{num(a,'total'):8.0f} {num(b,'total'):8.0f}  {a.get('cache_n'):>5} {b.get('cache_n'):>5}  {num(a,'bg_delay'):6.0f}  "
          f"{queued(a):6.0f} {queued(b):6.0f}")
    if mark == " ":
        dtt.append(num(a, "total") - num(b, "total"))
        if a["stream"]: dt.append(num(a, "ttft") - num(b, "ttft"))
if dtt:
    print(f"\nMatched pairs: total time with − without: median {st.median(dtt):+.0f} ms, mean {st.mean(dtt):+.0f} ms")
if dt:
    print(f"Matched streamed pairs: ttft with − without: median {st.median(dt):+.0f} ms, mean {st.mean(dt):+.0f} ms")
