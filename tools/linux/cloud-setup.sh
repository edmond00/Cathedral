#!/usr/bin/env bash
#
# Prepares a Linux machine (a Claude Code cloud session, a WSL2 distro, a CI runner) to build and
# test Cathedral headless. Idempotent: safe to run again, skips whatever is already in place.
#
#   bash tools/linux/cloud-setup.sh
#
# What it installs:
#   - the .NET 8 SDK                      (apt if the distro carries it, dotnet-install.sh otherwise)
#   - Xvfb + Mesa                         a virtual display with software OpenGL 3.3 (llvmpipe)
#   - the X11 libraries GLFW loads        OpenTK ships libglfw for linux-x64 but not its X11 deps
#   - OpenAL Soft + ALSA                  for the cross-platform audio path the port introduces
#   - libgomp, ffmpeg, jq, unzip, python3
#   - llama.cpp's Ubuntu x64 CPU build, at the version LlamaInstallCheck.ExpectedBuild names,
#     into models/llama-linux/
#
# What it does NOT fetch: the 2 GB model and the 347 MB GloVe vectors. Those are on demand, see
# tools/linux/fetch-assets.sh. --playground runs (the whole CLI suite) need neither.
#
# Run as root (cloud sessions are) or with sudo available.

set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT="$(pwd)"

SUDO=""
if [ "$(id -u)" != "0" ]; then SUDO="sudo"; fi

say() { echo "[setup] $*"; }

# ── system packages ──────────────────────────────────────────────────────────

export DEBIAN_FRONTEND=noninteractive
say "apt packages"
$SUDO apt-get update -qq

# Ubuntu 24.04 renamed libasound2 to libasound2t64; take whichever exists.
ASOUND=libasound2t64
apt-cache show libasound2t64 >/dev/null 2>&1 || ASOUND=libasound2

$SUDO apt-get install -y -qq --no-install-recommends \
    ca-certificates curl unzip jq python3 file procps \
    xvfb xauth x11-utils \
    libgl1 libegl1 libgl1-mesa-dri mesa-utils \
    libx11-6 libxrandr2 libxi6 libxcursor1 libxinerama1 libxxf86vm1 libxext6 \
    libopenal1 "$ASOUND" \
    libgomp1 libicu-dev \
    ffmpeg >/dev/null

# ── .NET 8 SDK ───────────────────────────────────────────────────────────────

if command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks | grep -q '^8\.'; then
    say ".NET 8 SDK already present: $(dotnet --list-sdks | grep '^8\.' | head -1)"
else
    if $SUDO apt-get install -y -qq dotnet-sdk-8.0 >/dev/null 2>&1; then
        say ".NET 8 SDK from apt"
    else
        say ".NET 8 SDK via dotnet-install.sh"
        curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
        $SUDO bash /tmp/dotnet-install.sh --channel 8.0 --install-dir /usr/share/dotnet
        $SUDO ln -sf /usr/share/dotnet/dotnet /usr/local/bin/dotnet
    fi
fi
export DOTNET_CLI_TELEMETRY_OPTOUT=1
dotnet --info | head -5

# ── llama.cpp, Linux x64 CPU build ───────────────────────────────────────────
#
# The version is read from the code, not written here, so this script cannot drift from the
# game's install check.

BUILD=$(sed -nE 's/.*ExpectedBuild[[:space:]]*=[[:space:]]*([0-9]+).*/\1/p' src/LLM/LlamaInstallCheck.cs)
COMMIT=$(sed -nE 's/.*ExpectedCommit[[:space:]]*=[[:space:]]*"([0-9a-f]+)".*/\1/p' src/LLM/LlamaInstallCheck.cs)
[ -n "$BUILD" ] || { say "ERROR: could not read ExpectedBuild from LlamaInstallCheck.cs"; exit 1; }
DEST="$ROOT/models/llama-linux"

if [ -x "$DEST/llama-server" ] && head -1 "$DEST/BUILD.txt" 2>/dev/null | grep -q "b$BUILD"; then
    say "llama.cpp b$BUILD already in models/llama-linux"
else
    say "llama.cpp b$BUILD (Ubuntu x64 CPU) -> models/llama-linux"
    # Upstream has changed its asset naming more than once (zip vs tar.gz), so ask the API
    # rather than guess a URL.
    URL=$(curl -sSL "https://api.github.com/repos/ggml-org/llama.cpp/releases/tags/b$BUILD" \
          | jq -r '.assets[]?.browser_download_url' 2>/dev/null \
          | grep -E 'bin-ubuntu-x64\.(zip|tar\.gz)$' | head -1 || true)
    # The API can be unreachable where the download host is not (a cloud session gates the REST
    # API per repository). Fall back to the names upstream has used, newest first.
    if [ -z "$URL" ]; then
        for ext in tar.gz zip; do
            CAND="https://github.com/ggml-org/llama.cpp/releases/download/b$BUILD/llama-b$BUILD-bin-ubuntu-x64.$ext"
            if curl -sSfIL -o /dev/null "$CAND" 2>/dev/null; then URL="$CAND"; break; fi
        done
    fi
    if [ -z "$URL" ]; then
        say "ERROR: no ubuntu-x64 asset in release b$BUILD. Assets are:"
        curl -sSL "https://api.github.com/repos/ggml-org/llama.cpp/releases/tags/b$BUILD" \
            | jq -r '.assets[].name' >&2
        exit 1
    fi
    TMP=$(mktemp -d)
    curl -sSL "$URL" -o "$TMP/$(basename "$URL")"
    mkdir -p "$TMP/x"
    case "$URL" in
        *.zip)    unzip -q "$TMP/$(basename "$URL")" -d "$TMP/x" ;;
        *.tar.gz) tar -xzf "$TMP/$(basename "$URL")" -C "$TMP/x" ;;
    esac
    # The archive nests its files one or two folders down (build/bin/ in older releases).
    SRV=$(find "$TMP/x" -type f -name llama-server | head -1)
    [ -n "$SRV" ] || { say "ERROR: llama-server not found in $(basename "$URL")"; exit 1; }
    rm -rf "$DEST"; mkdir -p "$DEST"
    cp -a "$(dirname "$SRV")"/. "$DEST"/
    chmod +x "$DEST"/llama-* 2>/dev/null || true
    cat > "$DEST/BUILD.txt" <<EOF
llama.cpp build: b$BUILD
commit:          $COMMIT
toolchain:       upstream release, Ubuntu x86_64
upstream asset:  $(basename "$URL")
license:         MIT (https://github.com/ggml-org/llama.cpp/blob/master/LICENSE)

This file is READ BY THE GAME, so keep the "llama.cpp build: bNNNN" format on the first line.
Unpruned copy of the upstream release, fetched by tools/linux/cloud-setup.sh. Prune it to the
import closure of llama-server and llama-bench (ldd) before it is ever shipped.
EOF
    rm -rf "$TMP"
    say "llama-server reports: $(LD_LIBRARY_PATH="$DEST" "$DEST/llama-server" --version 2>&1 | grep -i version | head -1)"
fi

# ── smoke check: can Xvfb give us OpenGL 3.3? ────────────────────────────────

say "OpenGL under Xvfb:"
xvfb-run -a -s "-screen 0 1280x1024x24" glxinfo -B 2>/dev/null \
    | grep -E 'OpenGL (core profile )?version|renderer' || say "WARNING: glxinfo failed under Xvfb"

say "done. Assets on demand: bash tools/linux/fetch-assets.sh --glove [--model]"
