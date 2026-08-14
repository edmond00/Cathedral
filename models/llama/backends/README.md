# GPU backends

Empty in a stock checkout. The game runs on CPU with nothing here; drop a backend in and it
is picked up on the next launch with no configuration.

## How this works

ggml resolves backends at runtime, not at link time. On startup llama-server scans for
`ggml-*.dll`, loads each one that initialises, and registers the devices it reports. The
CPU variants beside `llama-server.exe` already work this way — that is what the
`load_backend: loaded CPU backend from ...` line on stderr is.

`LlamaRuntime` adds each folder here to the server process's DLL search path, so a backend
dropped in is found the same way. Nothing else needs to change: no rebuild, no setting. The
first-run probe measures whatever it finds and picks the fastest for this game's workload.

## What a backend is NOT: an architecture

`models/llama-arm64/` is not a backend and is not resolved by anything here. A backend is a
plugin DLL loaded *into* the server process; an architecture is the server executable itself,
and an ARM64 DLL cannot be loaded into an x64 process. So there is nothing for the probe to
choose between, and nothing to measure — native always beats the same build emulated.

`LlamaRuntime.LlamaDirectory` therefore picks the toolchain by **detection**
(`RuntimeInformation.OSArchitecture`) before any of this runs, and the backends folder consulted
is the one inside whichever toolchain won. See `models/llama-arm64/BUILD.txt`.

## What the probe actually measures

**Both halves of inference, weighted for this game.** Prompt processing and token generation are
different code paths, and a device can be good at one and hopeless at the other. `LlamaProbe`
times both and scores candidates on how long one representative request takes end to end —
400 prompt tokens against 80 generated, because Cathedral answers several-hundred-token scenes
with a phrase (a persona choice is 4 tokens, a critic 20–60). **Prompt reading is most of the
wait**, so it is most of the score.

This is not theoretical. A Qualcomm Adreno X1-45, measured on the shipped 3B model:

| | prompt read | generation |
|---|---|---|
| Adreno X1-45 (Vulkan) | **1.1 tok/s** | 11.9 tok/s |
| the same machine's CPU (x64, emulated) | 13.6 tok/s | 5.4 tok/s |
| the same machine's CPU (arm64, native) | 76.1 tok/s | 13.8 tok/s |

It generates more than twice as fast as the emulated CPU beside it and reads prompts **twelve
times slower**. An earlier version of the probe scored generation alone, chose Vulkan, and the
game then sat on its loading bar indefinitely: nothing errored and nothing timed out, the first
495-token batch simply never finished. A GPU must now beat the CPU by 1.25x on the combined
score to be chosen at all, because the GPU path has strictly more ways to fail.

Run `dotnet run -- --llm-probe-audit` to see the whole comparison on any machine, including the
per-device rates and the margin. It ignores the cached answer and writes nothing back.

## Adding Vulkan (recommended)

    backends/vulkan/ggml-vulkan.dll

From `llama-b8851-bin-win-vulkan-x64.zip` — the **same build number** as `../BUILD.txt`.
Take only `ggml-vulkan.dll`; the rest of that zip duplicates what is already here.

It is **59 MB** — one file, and the whole cost of GPU support. That covers NVIDIA, AMD and
Intel from a single binary, including the integrated GPUs that are most of the install base.

Measured on an RTX 2080 Ti with the shipped 3B model: **~120 tok/s generation through Vulkan
against ~20 on the CPU**, about 6x. Successive probes landed on 123.2/19.7 and 120.1/20.4 — a
single unwarmed run each, which is plenty to separate two devices this far apart and is why the
probe does not bother repeating itself.

⚠ Those two figures are **generation only** — they were taken with the older probe, before it
measured prompt processing. They understate a real discrete GPU rather than overstating it: a
card like that reads prompts in the hundreds-to-thousands of tok/s against a CPU's 13–76, and
prompt reading is the axis this game spends its time on. The Adreno above is pathological, not
representative; do not read it as an argument against GPU support. Worth re-running
`--llm-probe-audit` on that card to capture the read rate, which is now the number that decides.

## CUDA — deliberately not shipped

Vulkan already covers NVIDIA. CUDA is faster on it, but the four files needed
(`ggml-cuda.dll`, `cudart64_12.dll`, `cublas64_12.dll`, `cublasLt64_12.dll`, from
`llama-b8851-bin-win-cuda-*-x64.zip` plus the matching `cudart-llama-bin-win-cuda-*.zip`) come
to ~420 MB — a fifth of the download, for one vendor already handled, and not faster by enough
to matter for a 3B model writing prose.

If it is ever wanted, it belongs as an optional extra the game offers to fetch when the probe
reports an NVIDIA device — not in the installer. The NVIDIA EULA permits redistributing the CUDA
runtime libraries and llama.cpp itself is MIT; ship both licences if you ship the binaries.

Backends for AMD (ROCm/HIP) and Intel (SYCL) are the same trade and lose it worse: large, needing
extra runtimes, and covering hardware Vulkan already reaches.

## What not to do

**Never mix build numbers.** A backend from a different llama.cpp revision than `ggml-base.dll`
crashes inside the backend with no usable diagnostic — the DLLs carry no version resource, so
nothing can check this by reading them.

This applies **within** a toolchain folder, not across them: `models/llama` (b8851) and
`models/llama-arm64` (b8746) each carry a complete set of ggml libraries and are never loaded
into the same process, so their build numbers are free to differ.

What protects you otherwise is that a backend is always **loaded in a subprocess first**: the probe
runs `llama-server --list-devices` against it, and one that crashes or reports no device is simply
not used. A mismatched pack costs you the GPU, not the game.

**Do not flatten these into the parent folder.** Separate directories are what keeps two packs
from colliding on a shared file name — both the CUDA and Vulkan zips carry their own copies of
`ggml-base.dll` and friends, and whichever landed last would win silently.
