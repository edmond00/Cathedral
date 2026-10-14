# tools/linux/

Building, running and testing the game on Linux. The development name is used throughout
(`Cathedral`); see the `release` skill for naming.

| file | what |
|---|---|
| `cloud-setup.sh` | prepares a Linux machine: .NET 8 SDK, Xvfb + Mesa, OpenAL, the X11 libraries GLFW loads, and llama.cpp's Ubuntu build pruned into `models/llama-linux/`. `--vulkan` adds the Vulkan backend pack; `--no-llama` installs packages only |
| `fetch-assets.sh` | `--glove` / `--model`: the two large runtime files, checked against `models/README.md` |
| `Dockerfile`, `docker-entrypoint.sh` | an image that runs `run_tests.sh` under Xvfb (below) |
| `PORT-BRIEF.md`, `PORT-NOTES.md` | the Linux port: what was asked, and what was done and found |

## On a Linux machine (or WSL2)

```bash
bash tools/linux/cloud-setup.sh
bash tools/linux/fetch-assets.sh --glove          # 822 MB download; or pass --no-embeddings
dotnet build Cathedral.csproj -r linux-x64        # a RID build must name the project
xvfb-run -a ./run_tests.sh audits
CLI_TIMEOUT=400 xvfb-run -a ./run_tests.sh cli    # llvmpipe is slower than a GPU
```

Without the GloVe file the game exits at its first observation, `--playground` included. For a
machine that cannot have it, `CLI_EXTRA_FLAGS=--no-embeddings ./run_tests.sh cli`.

With no sound device, `export ALSOFT_DRIVERS=null` so OpenAL opens a null device. The suite passes
`--silent` anyway.

## Docker

The image holds the SDK, the packages and the game built for Linux; **`models/` is mounted at run
time, never baked in**. The suite runs with `--playground`, so it needs no model, only (ideally) the
GloVe file under `models/embeddings/`. Without it the entry point adds `--no-embeddings` and says so.

### Linux / WSL2 shell

```bash
docker build -f tools/linux/Dockerfile -t cathedral-tests .
docker run --rm -v "$PWD/models:/src/models" cathedral-tests              # audits, then CLI
docker run --rm -v "$PWD/models:/src/models" cathedral-tests audits       # any run_tests.sh args
docker run --rm -v "$PWD/models:/src/models" cathedral-tests cli verb
```

Keep the per-script logs by mounting a folder for them:

```bash
mkdir -p test-logs
docker run --rm -e TMPDIR=/logs -v "$PWD/test-logs:/logs" -v "$PWD/models:/src/models" cathedral-tests cli
# logs in test-logs/cathedral-tests/
```

### Windows (Docker Desktop, WSL2 backend), PowerShell

Run from the repository root. `${PWD}` expands to a Windows path, which Docker Desktop accepts in a
volume mount.

```powershell
docker build -f tools/linux/Dockerfile -t cathedral-tests .
docker run --rm -v "${PWD}\models:/src/models" cathedral-tests
docker run --rm -v "${PWD}\models:/src/models" cathedral-tests audits

New-Item -ItemType Directory -Force test-logs | Out-Null
docker run --rm -e TMPDIR=/logs -v "${PWD}\test-logs:/logs" -v "${PWD}\models:/src/models" cathedral-tests cli
```

The mounted Windows `models/` has `llama/`, not `llama-linux/`. That does not matter here, since the
suite never starts the server. GloVe is read from it as is.

### Options

| | |
|---|---|
| `-e CLI_TIMEOUT=600` | seconds per script (entry point default 400; `run_tests.sh` alone defaults to 200) |
| `-e CLI_EXTRA_FLAGS=...` | appended to every script's command line |
| `--build-arg BASE_IMAGE=mirror.gcr.io/library/ubuntu:24.04` | when Docker Hub answers 429 Too Many Requests |
| `tools/linux/extra-ca/*.crt` | extra CA certificates installed into the image, for building behind a TLS-intercepting proxy (gitignored) |

Behind a proxy that only the host can reach, build with
`--network host --build-arg HTTPS_PROXY=... --build-arg HTTP_PROXY=...` and put the proxy's CA in
`tools/linux/extra-ca/`.

### One suite per machine, container or not

`run_tests.sh` refuses to start while a `Cathedral` process exists, and kills every `Cathedral`
process after each script. A Linux host's process table includes its containers' processes, so
**a suite run on a Linux host kills the game inside a container running at the same time**: SIGKILL
mid-script, reported as "no verdict". (Not the reverse, since a container sees only its own processes.
And not on Windows, where Docker Desktop's containers live in a VM that `Get-Process` does not see.)
On one Linux machine, run one suite at a time.
