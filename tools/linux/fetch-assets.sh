#!/usr/bin/env bash
#
# Fetches the gitignored runtime assets a non-playground run needs, verified against the SHA-256
# recorded in models/README.md. Both are large, so they are separate from cloud-setup.sh and
# fetched only when a run actually needs them.
#
#   bash tools/linux/fetch-assets.sh --glove           # models/embeddings/glove.6B.100d.txt (822 MB zip)
#   bash tools/linux/fetch-assets.sh --model           # models/model.gguf (2.1 GB)
#   bash tools/linux/fetch-assets.sh --glove --model
#
# Needs network access to nlp.stanford.edu and huggingface.co.

set -euo pipefail
cd "$(dirname "$0")/../.."

MODEL_URL="https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf"
MODEL_SHA="626b4a6678b86442240e33df819e00132d3ba7dddfe1cdc4fbb18e0a9615c62d"
GLOVE_URL="https://nlp.stanford.edu/data/glove.6B.zip"
GLOVE_SHA="95dde4dfd627ab26608d33e76d1195ec059734bd29089ea52cadb08d07c64544"

say() { echo "[fetch] $*"; }

check() {  # file sha
    local got; got=$(sha256sum "$1" | cut -d' ' -f1)
    if [ "$got" = "$2" ]; then say "$1 verified"; else say "WARNING: $1 sha256 $got, expected $2"; fi
}

want_glove=0; want_model=0
for a in "$@"; do
    case "$a" in
        --glove) want_glove=1 ;;
        --model) want_model=1 ;;
        *) echo "usage: $0 [--glove] [--model]" >&2; exit 2 ;;
    esac
done
[ $want_glove = 1 ] || [ $want_model = 1 ] || { echo "usage: $0 [--glove] [--model]" >&2; exit 2; }

if [ $want_glove = 1 ]; then
    F=models/embeddings/glove.6B.100d.txt
    if [ -f "$F" ]; then say "$F present"; else
        mkdir -p models/embeddings
        TMP=$(mktemp -d)
        say "downloading glove.6B.zip"
        curl -sSL "$GLOVE_URL" -o "$TMP/glove.zip"
        unzip -q -j "$TMP/glove.zip" glove.6B.100d.txt -d models/embeddings
        rm -rf "$TMP"
    fi
    # The README notes the hash is of the local extracted copy, not confirmed upstream, so a
    # mismatch here is a warning to investigate, not proof of a bad file.
    check "$F" "$GLOVE_SHA"
fi

if [ $want_model = 1 ]; then
    F=models/model.gguf
    if [ -f "$F" ]; then say "$F present"; else
        say "downloading model (2.1 GB)"
        curl -sSL "$MODEL_URL" -o "$F.part"
        mv "$F.part" "$F"
    fi
    check "$F" "$MODEL_SHA"
fi
