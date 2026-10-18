#!/usr/bin/env bash
#
# The Docker image's entry point: run_tests.sh under a virtual display. Arguments are run_tests.sh's
# (`all` by default, `audits`, `cli`, `cli verb`, ...).
#
# models/ is expected to be mounted at /src/models. The CLI suite (--playground) needs no model, but
# the game exits without the GloVe vectors; when the mount does not carry them, every script gets
# --no-embeddings and this says so.

set -uo pipefail
cd /src

if [ ! -f models/embeddings/glove.6B.100d.txt ]; then
    echo "[docker] models/embeddings/glove.6B.100d.txt not mounted: running with --no-embeddings."
    export CLI_EXTRA_FLAGS="--no-embeddings ${CLI_EXTRA_FLAGS:-}"
fi

# llvmpipe is slower than a GPU; give each script more room unless told otherwise.
export CLI_TIMEOUT="${CLI_TIMEOUT:-400}"

exec xvfb-run -a -s "-screen 0 1280x1024x24" ./run_tests.sh "$@"
