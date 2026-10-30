#!/usr/bin/env bash
# Starts a llama-server for the harness the way the game starts its own (LlamaServerManager.
# BuildServerArguments on a CPU-only install): same model, context, cache types and the sampler
# penalties that live on the server's command line. Port 8092, so it never collides with a game run.
cd "$(dirname "$0")/../.."
BIN=models/llama-linux/llama-server; [ -x "$BIN" ] || BIN=models/llama/llama-server.exe
exec "$BIN" -m models/model.gguf -c 2048 --port "${PROMPT_PORT:-8092}" \
    --cache-type-k f16 --cache-type-v f16 \
    --repeat-penalty 1.0 --frequency-penalty 0.2 --dry-multiplier 0.8 -ngl 0
