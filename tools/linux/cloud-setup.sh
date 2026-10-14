#!/usr/bin/env bash
#
# Prepares a Linux machine (a Claude Code cloud session, a WSL2 distro, a CI runner) to build and
# test Cathedral headless. Idempotent: safe to run again, skips whatever is already in place.
#
#   bash tools/linux/cloud-setup.sh            the CPU toolchain
#   bash tools/linux/cloud-setup.sh --vulkan   also the Vulkan backend pack and the Vulkan loader
#   bash tools/linux/cloud-setup.sh --no-llama packages and SDK only (the Docker image: models/ is
#                                              mounted at run time, never baked in)
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
# tools/linux/fetch-assets.sh. --playground runs need no model; without GloVe pass --no-embeddings
# (CLI_EXTRA_FLAGS=--no-embeddings for run_tests.sh), or the game exits at its first observation.
#
# Run as root (cloud sessions are) or with sudo available.

set -euo pipefail
cd "$(dirname "$0")/../.."
ROOT="$(pwd)"

SUDO=""
if [ "$(id -u)" != "0" ]; then SUDO="sudo"; fi

say() { echo "[setup] $*"; }

WANT_VULKAN=0; WANT_LLAMA=1
for a in "$@"; do
    case "$a" in
        --vulkan)   WANT_VULKAN=1 ;;
        --no-llama) WANT_LLAMA=0 ;;
        *) echo "usage: $0 [--vulkan] [--no-llama]" >&2; exit 2 ;;
    esac
done

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

if [ $WANT_LLAMA = 1 ]; then
    BUILD=$(sed -nE 's/.*ExpectedBuild[[:space:]]*=[[:space:]]*([0-9]+).*/\1/p' src/LLM/LlamaInstallCheck.cs)
    COMMIT=$(sed -nE 's/.*ExpectedCommit[[:space:]]*=[[:space:]]*"([0-9a-f]+)".*/\1/p' src/LLM/LlamaInstallCheck.cs)
    [ -n "$BUILD" ] || { say "ERROR: could not read ExpectedBuild from LlamaInstallCheck.cs"; exit 1; }
fi
DEST="$ROOT/models/llama-linux"

# The tracked record of this toolchain. Deterministic (sorted, no dates), so re-running setup
# rewrites models/llama-linux/BUILD.txt byte-for-byte and leaves git clean.
write_build_txt() {  # upstream asset url
    {
        echo "llama.cpp build: b$BUILD"
        echo "commit:          $COMMIT"
        echo "toolchain:       upstream release, Ubuntu x86_64 (GNU)"
        echo "upstream asset:  $(basename "$1")"
        echo "license:         MIT (https://github.com/ggml-org/llama.cpp/blob/master/LICENSE)"
        echo
        echo 'This file is READ BY THE GAME, so keep the "llama.cpp build: bNNNN" format on the first line.'
        echo "Fetched and pruned by tools/linux/cloud-setup.sh; the binaries are gitignored, this file is not."
        echo
        echo "── what was kept, and why ─────────────────────────────────────────────────────"
        echo
        echo "The ldd closure of llama-server and llama-bench that lives in this folder (each"
        echo "versioned library with its symlink), plus the CPU backends ggml loads by directory"
        echo "scan. llama-server carries RUNPATH=\$ORIGIN, so it finds them with no LD_LIBRARY_PATH."
        echo "Needed from the OS: libstdc++6, libgomp1, libssl3 (libcrypto)."
        echo
        echo "kept:"
        (cd "$DEST" && ls | grep -v '^BUILD.txt$' | LC_ALL=C sort | sed 's/^/  /')
        echo
        echo "sha256:"
        (cd "$DEST" && find . -maxdepth 1 -type f ! -name BUILD.txt -printf '%f\n' | LC_ALL=C sort \
            | while read -r f; do printf '  %s  %s\n' "$(sha256sum "$f" | cut -d' ' -f1)" "$f"; done)
    } > "$DEST/BUILD.txt"
}

if [ $WANT_LLAMA = 0 ]; then
    say "llama.cpp skipped (--no-llama)"
elif [ -x "$DEST/llama-server" ] && head -1 "$DEST/BUILD.txt" 2>/dev/null | grep -q "b$BUILD"; then
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
    rm -rf "$TMP"

    # Prune to what the game runs: llama-server and llama-bench, the libraries in this folder that
    # their ldd closure names (with the symlinks' targets), and the CPU variants ggml loads by
    # directory scan, which appear in no import table. Mirrors models/llama/BUILD.txt for Windows.
    # System libraries in the closure (libstdc++, libgomp, libssl, libcrypto) are left to the OS.
    KEEP=$(mktemp)
    printf '%s\n' llama-server llama-bench LICENSE BUILD.txt > "$KEEP"
    for lib in $( (ldd "$DEST/llama-server"; ldd "$DEST/llama-bench") | awk -v d="$DEST" 'index($3, d) == 1 {print $1}' | sort -u); do
        echo "$lib" >> "$KEEP"
        basename "$(readlink -f "$DEST/$lib")" >> "$KEEP"
    done
    (cd "$DEST" && ls libggml-cpu-*.so) >> "$KEEP"
    for f in "$DEST"/*; do
        grep -qxF "$(basename "$f")" "$KEEP" || rm -rf "$f"
    done
    rm -f "$KEEP"

    write_build_txt "$URL"
    say "llama-server reports: $(LD_LIBRARY_PATH="$DEST" "$DEST/llama-server" --version 2>&1 | grep -i version | head -1)"
fi

# ── optional: the Vulkan backend pack ────────────────────────────────────────
#
# One library, libggml-vulkan.so, from upstream's Ubuntu Vulkan build — the same pack shape as
# models/llama/backends/vulkan/ on Windows. It resolves libggml-base.so.0 from the server that loads
# it (the two builds' base libraries are byte-identical) and needs the system Vulkan loader. Mesa's
# lavapipe gives a machine with no GPU a Vulkan device, but llama.cpp hides CPU-type devices unless
# GGML_VK_VISIBLE_DEVICES names them (GGML_VK_VISIBLE_DEVICES=0) — which is how the probe's
# two-candidate path can be exercised without a GPU.

if [ $WANT_VULKAN = 1 ] && [ $WANT_LLAMA = 1 ]; then
    $SUDO apt-get install -y -qq --no-install-recommends libvulkan1 mesa-vulkan-drivers >/dev/null
    VK="$DEST/backends/vulkan"
    if [ -f "$VK/libggml-vulkan.so" ] && [ "$(sha256sum "$VK/libggml-vulkan.so" | cut -d' ' -f1)" = \
         "$(sed -nE 's/^libggml-vulkan\.so sha256: ([0-9a-f]{64}).*/\1/p' "$VK/BUILD.txt" 2>/dev/null)" ]; then
        say "Vulkan backend b$BUILD already in models/llama-linux/backends/vulkan"
    else
        say "Vulkan backend b$BUILD -> models/llama-linux/backends/vulkan"
        VURL="https://github.com/ggml-org/llama.cpp/releases/download/b$BUILD/llama-b$BUILD-bin-ubuntu-vulkan-x64.tar.gz"
        TMP=$(mktemp -d)
        curl -sSfL "$VURL" -o "$TMP/vk.tar.gz"
        tar -xzf "$TMP/vk.tar.gz" -C "$TMP"
        LIB=$(find "$TMP" -type f -name libggml-vulkan.so | head -1)
        [ -n "$LIB" ] || { say "ERROR: libggml-vulkan.so not in $(basename "$VURL")"; exit 1; }
        mkdir -p "$VK"
        cp "$LIB" "$VK/"
        rm -rf "$TMP"
        H=$(sha256sum "$VK/libggml-vulkan.so" | cut -d' ' -f1)
        {
            echo "llama.cpp build: b$BUILD"
            echo "libggml-vulkan.so sha256: $H"
            echo "from:            $(basename "$VURL") (libggml-vulkan.so only)"
            echo
            echo "Tracked in git and read by LlamaInstallCheck (src/LLM): the build must match"
            echo "LlamaInstallCheck.ExpectedBuild and ../../BUILD.txt, and the library beside this file must hash to"
            echo "the value above. Needs the system Vulkan loader (libvulkan1) and a driver. It resolves"
            echo "libggml-base.so.0 from the server, which has already loaded it. Fetched by"
            echo "tools/linux/cloud-setup.sh --vulkan."
        } > "$VK/BUILD.txt"
    fi
fi

# ── smoke check: can Xvfb give us OpenGL 3.3? ────────────────────────────────

say "OpenGL under Xvfb:"
xvfb-run -a -s "-screen 0 1280x1024x24" glxinfo -B 2>/dev/null \
    | grep -E 'OpenGL (core profile )?version|renderer' || say "WARNING: glxinfo failed under Xvfb"

say "done. Assets on demand: bash tools/linux/fetch-assets.sh --glove [--model]"
