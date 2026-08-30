# models/

Everything the game loads at runtime that is not code. Five things live here, and only
`model.gguf` is meant to be swapped by a player.

| path | what | required |
|---|---|---|
| `model.gguf` | the language model driving all narration | yes — the game exits without it |
| `llama/` | the llama.cpp runtime that serves it (x64) | yes |
| `llama-arm64/` | the same runtime built for Windows-on-ARM | yes on ARM64 hosts |
| `embeddings/glove.6B.100d.txt` | GloVe vectors for `WordEmbedding` | yes |
| `en_scowl_40.txt` | SCOWL common-word list for the narration sanitizer | no — layer-2 guard disables itself |

## What is tracked by git, and why the binaries are not

Only the documentation is committed: this file, `en_scowl_40.txt` (452 KB), and the two
`BUILD.txt` files plus `llama/backends/README.md`. The ~2.5 GB of binaries beside them are
deliberately gitignored, **including under Git LFS**, and the reasoning is worth keeping:

- **None of it is authored here.** A third-party quantization, upstream llama.cpp release
  binaries, and a Stanford corpus. Diff, blame, merge and history — the whole of what git is
  for — are worth nothing on any of them.
- **LFS does not delta-compress.** Every llama.cpp bump would add a full fresh copy: ~185 MB
  across the two toolchains, permanently, removable only by rewriting history.
- **`model.gguf` is 98% of GitHub's 2 GiB per-file LFS limit** (2,104,932,768 bytes, ~40 MB of
  headroom). A Q5 quant or a 7B model would be refused outright.
- **Bandwidth, not storage, is the binding cost.** Every fresh clone would pull 2.5 GB against a
  1 GB/month free allowance.

What replaces it is the table below: a checksum for each file, so any copy can be verified and
any download can be checked, for no bytes and no monthly bill. LFS would be the right tool if
these were artifacts produced *here* — a custom fine-tune or an in-house quantization with no
upstream to fetch from. None of them are.

## Provenance

Verify with `sha256sum <file>`, or `Get-FileHash <file> -Algorithm SHA256` in PowerShell.

| file | bytes | SHA-256 | source |
|---|---|---|---|
| `model.gguf` | 2,104,932,768 | `626b4a6678b86442240e33df819e00132d3ba7dddfe1cdc4fbb18e0a9615c62d` | [Qwen2.5-3B-Instruct-GGUF](https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF) — verified |
| `embeddings/glove.6B.100d.txt` | 347,116,733 | `95dde4dfd627ab26608d33e76d1195ec059734bd29089ea52cadb08d07c64544` | [glove.6B.zip](https://nlp.stanford.edu/data/glove.6B.zip) — hash is of the local extracted copy, not confirmed upstream |
| `en_scowl_40.txt` | 452,974 | `a7e0f9a111bd0606e0c6365ebe0a9bcd9d0731934e778aafa1735c5a5dfb7ca3` | derived from SCOWL; committed, so git is the source |

`tools/verify_models.ps1` checks this table, and the toolchain build numbers, in one command.

### Identifying `model.gguf`

**The name is not enough to re-download it, and this folder proves why.** Beside it sits
`qwen2.5-3b-instruct-q4_k_m.gguf`, which answers to the same description — Qwen2.5 3B Instruct,
`general.file_type` 15 (Q4_K_M) — and is **a different model**:

| | `model.gguf` (shipped) | `qwen2.5-3b-instruct-q4_k_m.gguf` |
|---|---|---|
| bytes | 2,104,932,768 | 1,929,903,008 |
| tensors | **435** | **434** |
| `general.size_label` | **3.4B** | **3B** |
| `general.name` | `qwen2.5-3b-instruct` | `Qwen2.5 3B Instruct` |
| `general.version` | `v0.1-v0.1` | *absent* |
| `general.license`, `base_model`, `tags` | *absent* | present |

One extra tensor and ~0.3B extra parameters, which is what the 175 MB is: the shipped file
appears to keep its **output embeddings untied**, where the other ties them to the input
embeddings. Same architecture, same quantization, different weights — so "download a Qwen2.5-3B
Instruct Q4_K_M" can land you on either, and only one is what the sampling constants were tuned
against.

So identify a candidate by these, in order:

1. **SHA-256** — the table above. Decisive.
2. **Size and parameter count** — 2,104,932,768 bytes, 435 tensors, **3,397,103,616 parameters**.
   That last one is the sharpest single number: stock Qwen2.5-3B is 3.09B, and the extra 0.31B is
   exactly the untied output embedding (151936 vocab x 2048 hidden).
3. **Header fingerprint** — `general.size_label` = `3.4B` and `general.version` = `v0.1-v0.1`.
   Both are unusual and neither appears in the neighbouring file.

### Where it came from

**Qwen's own GGUF repository** — <https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF>, file
`qwen2.5-3b-instruct-q4_k_m.gguf`:

    https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf

Confirmed against that repository's file listing: 2,104,932,768 bytes and SHA-256
`626b4a66…15c62d`, both exact. Hugging Face stores an LFS file's SHA-256 as its `oid`, so
`https://huggingface.co/api/models/Qwen/Qwen2.5-3B-Instruct-GGUF/tree/main` re-checks this in a
second without downloading anything — worth doing before a 2 GB fetch rather than after.

That it is a **vendor** conversion rather than a community one is what the sparse header was
telling us. Twenty-six metadata keys, with no `general.source.url`, no `repo_url` and no
`quantized_by` — that last tag is one most third-party quantizers add, and its absence is what
pointed here. The neighbouring `qwen2.5-3b-instruct-q4_k_m.gguf` carries thirty-five keys
including `general.base_model.0.repo_url = https://huggingface.co/Qwen/Qwen2.5-3B`, which is the
fingerprint of the community tooling that produced it.

To re-read any of this without the game, from either toolchain folder:

    ./models/llama/llama-bench.exe -m models/model.gguf -p 1 -n 1 -r 1 -o json

It loads the model, so allow a minute. The fields that matter are `model_type`
(`qwen2 3B Q4_K - Medium`), `model_size` (2,098,976,768 — tensor bytes, so a little under the
file size) and `model_n_params` (3,397,103,616).

`glove.6B.100d.txt` is the 100-dimension file from Stanford's `glove.6B.zip`
(<https://nlp.stanford.edu/data/glove.6B.zip>), **400,000 lines**.

> A copy with **400,001** lines is also in circulation, carrying an extra `<unk>` vector appended
> for out-of-vocabulary handling. It is inert here: nothing in the codebase looks up `<unk>`, and
> `WordEmbedding` answers an unknown word by `TryGetValue` returning false. Either file works;
> the canonical 400,000-line one is what the hash above covers.

`en_scowl_40.txt` is derived from SCOWL. The hash covers the **LF** form, which is what is
committed. A CRLF checkout is 502,767 bytes and equally correct — `CommonWordLexicon` calls
`line.Trim()`, and `\r` is whitespace in .NET, so the terminator never reaches the word list.

The two llama.cpp toolchains record their own upstream zip and build number in
`llama/BUILD.txt` (b8851, x64) and `llama-arm64/BUILD.txt` (b8746, ARM64). **The build numbers
are allowed to differ between the two folders**; what must never differ is a GPU backend under
`backends/` versus the `ggml-base.dll` beside it in the same folder.

## Changing the model

Replace `model.gguf` with any other GGUF **under that exact name**. There is no setting, no
alias table and no path anywhere in the code — the file name is the whole interface.

This works because a GGUF carries its own identity. Architecture, tokenizer and chat template
are all read out of the file, so llama.cpp does not care what it is called. The game reads
`general.name` out of the header and writes it to the startup log, so you can still tell which
model is loaded — the Settings screen deliberately does not show it.

Two things to know before swapping:

- **The compute settings are re-probed.** They are recorded against the model's size and
  timestamp, so a different file re-runs hardware detection on the next launch rather than
  reusing an offload figure chosen for a model of another size.
- **The sampling constants were tuned on a 3B model** (see `Config.LLM` — the repeat-penalty
  finding in particular was measured, not guessed). They are not model-independent. A much
  smaller model may need them revisited.

The file currently shipped is `qwen2.5-3b-instruct-q4_k_m` (~2.0 GB, Q4_K_M) — see Provenance
above for its checksum.

## models_old/

The previous unpruned folder, kept as an archive: six other GGUFs, a CUDA llama.cpp build and
the upstream CPU release zip. Nothing reads it and it is not tracked by git — delete it when
you are satisfied nothing is missing.
