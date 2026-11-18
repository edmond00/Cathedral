#!/usr/bin/env bash
#
# The Docker image's entry point: run_tests.sh under a virtual display. Arguments are run_tests.sh's
# (`all` by default, `audits`, `cli`, `cli verb`, ...).
#
# models/ is expected to be mounted at /src/models. The CLI suite (--playground) needs no model; the
# GloVe vectors are read by --mm-audit's persona lint alone, which says so when they are absent.

set -uo pipefail
cd /src

# llvmpipe is slower than a GPU; give each script more room unless told otherwise.
export CLI_TIMEOUT="${CLI_TIMEOUT:-400}"

exec xvfb-run -a -s "-screen 0 1280x1024x24" ./run_tests.sh "$@"
