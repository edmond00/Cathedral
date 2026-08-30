---
name: models
description: Maintain the models/ folder — the LLM, the GloVe vectors and the two llama.cpp toolchains. Use when setting up a new machine, upgrading llama.cpp, swapping the model, reconciling two machines whose models/ folders differ, or diagnosing a "which copy is right?" question. Carries the diagnostic ladder for interpreting differences between copies.
---

# Maintaining models/

`models/` holds ~2.5 GB of third-party binaries that are **deliberately not in git**, so nothing
propagates them between machines and nothing notices when one falls behind.

**Read `models/README.md` first — it is the source of truth** for what each file is, its
SHA-256, and where it came from. This skill does not repeat any of that; duplicating a hash
table is how one copy goes stale. What this carries is the part a document cannot: how to decide
whether a difference *matters*.

## Always start here

    ./tools/verify_models.ps1          # checks hashes, architectures, and toolchain drift
    ./tools/verify_models.ps1 -Fix     # also prints the repair commands

Exit code is the number of problems, so it gates a release. It downloads and modifies nothing —
a 2 GB fetch is not a verifier's decision to make.

**The check that matters most is drift.** `BUILD.txt` is *tracked*; the binaries beside it are
*not*. A `git pull` moves the declaration and leaves the reality behind, so a stale machine reads
`b8851` out of a committed file while its own `llama-server` reports `8746` — and will build a
release that way without complaint.

## The diagnostic ladder

When two copies of a file differ, work down this list before assuming either is wrong. Every rung
was learned by getting it wrong first.

**1. Is the size delta exactly the line count?** Then it is line endings, and it is inert.
`core.autocrlf=true` gives one machine CRLF and another LF. `en_scowl_40.txt` is 452,974 bytes as
LF and 502,767 as CRLF — a difference of exactly its 49,793 lines. `CommonWordLexicon` calls
`line.Trim()` and `\r` is whitespace in .NET, so the terminator never reaches the word list.
Confirm with `cmp <(tr -d '\r' < a) <(tr -d '\r' < b)`.

**2. Is one file a byte-exact prefix of the other?** `cmp` reports `EOF on <file>` with no earlier
difference. Then diff the tail and ask whether the extra content is *referenced anywhere* before
assuming it matters. The 400,001-line GloVe carries an appended `<unk>` vector; nothing in the
codebase looks `<unk>` up, and `WordEmbedding` answers an unknown word by `TryGetValue` returning
false. Inert.

**3. Same model name, different tensor or parameter count?** Then it is **a different model**, not
a variant, and they are not interchangeable. `models/` has proved this: two files both answering
to "Qwen2.5 3B Instruct, Q4_K_M" differ by one tensor and 0.31B parameters, because one keeps its
output embeddings untied. A name and a quantization label do not identify a GGUF. Read the header
rather than trusting the name:

    ./models/llama/llama-bench.exe -m <file> -p 1 -n 1 -r 1 -o json

`model_type`, `model_size` and `model_n_params` are the fields that settle it.

**4. Does `BUILD.txt` disagree with the binary?** `llama-server --version` prints
`version: 8746 (0893f50f2)`. If it disagrees with the committed `BUILD.txt`, this machine is
stale — re-fetch the upstream zip that `BUILD.txt` names. Note it prints to **stderr**, so in
Windows PowerShell 5.1 read the streams via `ProcessStartInfo`; `2>&1` wraps them in a
`NativeCommandError` that throws under `ErrorActionPreference = "Stop"`.

**5. Never assume the shipped package is newer than the dev tree.** Verify. It has been checked
once and was wrong on every count: the model byte-identical, both text files differing only by
line endings, GloVe a prefix.

## Reconciling two machines

1. Run the verifier on both. It names what diverged and which side is stale.
2. Where they disagree, the **repository** is authoritative — `BUILD.txt` and the hashes in
   `models/README.md` are committed; a local folder is just what that machine happens to have.
3. Re-fetch from the sources in `models/README.md`. Never copy `models/` wholesale between
   machines: only `llama*/` is architecture-specific, and copying the rest drags stale
   line-ending and variant differences along with it.
4. Re-run the verifier.

## Upgrading llama.cpp

Both toolchains are separate downloads and **their build numbers may legitimately differ** —
they carry complete, independent sets of ggml libraries and are never loaded into one process.
What must never differ is a GPU backend under `backends/` versus the `ggml-base.dll` beside it in
the *same* folder; that crashes inside the backend with no usable diagnostic, and the DLLs carry
no version resource, so nothing can catch it by reading them.

1. Fetch the upstream zip for each architecture (named in each `BUILD.txt`).
2. Replace the folder contents, keeping `BUILD.txt` and `backends/`.
3. Update `BUILD.txt` — build number, commit, zip name.
4. Re-fetch any GPU backend at the **matching** build number.
5. `./tools/verify_models.ps1`, then `./package.ps1 -NoModel -NoZip` to confirm staging.

## Swapping the model

The file is always `models/model.gguf` — there is no setting and no path anywhere in the code.
After swapping, update the provenance table in `models/README.md` (size, SHA-256, source) and the
expectations at the top of `tools/verify_models.ps1`, which are duplicated there on purpose: a
verifier that reads its expectations from the prose it is checking can only agree with itself.

The compute probe re-runs automatically, because its cache is keyed on the model's size and
timestamp. Note the sampling constants in `Config.LLM` were tuned on a 3B model and are not
model-independent.

## What must never go in git

The binaries — **including under Git LFS**. `models/README.md` records the full reasoning; the
short version is that none of it is authored here, LFS does not delta-compress so every upgrade
is a permanent full copy, `model.gguf` sits at 98% of GitHub's 2 GiB per-file limit, and every
clone would pull 2.5 GB. Checksums give the same reproducibility for no bytes.

Tracked under `models/`: `README.md`, `en_scowl_40.txt`, both `BUILD.txt` files, and
`llama/backends/README.md`. `.gitignore` uses negation patterns to keep exactly those — check
with `git add --dry-run models/`, which must list only text files.
