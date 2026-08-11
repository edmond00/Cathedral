# models/

Everything the game loads at runtime that is not code. Four things live here, and only
`model.gguf` is meant to be swapped by a player.

| path | what | required |
|---|---|---|
| `model.gguf` | the language model driving all narration | yes — the game exits without it |
| `llama/` | the llama.cpp runtime that serves it | yes |
| `embeddings/glove.6B.100d.txt` | GloVe vectors for `WordEmbedding` | yes |
| `en_scowl_40.txt` | SCOWL common-word list for the narration sanitizer | no — layer-2 guard disables itself |

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

The file currently shipped is `qwen2.5-3b-instruct-q4_k_m` (~2.0 GB, Q4_K_M).

## models_old/

The previous unpruned folder, kept as an archive: six other GGUFs, a CUDA llama.cpp build and
the upstream CPU release zip. Nothing reads it and it is not tracked by git — delete it when
you are satisfied nothing is missing.
