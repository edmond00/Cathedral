# Linux port — notes

Running log of the work described in `PORT-BRIEF.md`. Newest information is kept in place per
step; open questions for the developer are collected at the end.

## Environment

- Cloud session, Ubuntu (Linux 6.18 kernel), no GPU, no sound device.
- .NET SDK 8.0.131 from apt (`cloud-setup.sh`).
- **Pushing was refused** at the start of the session: the git proxy has no write credential for
  `edmond00/cathedral` (the Claude GitHub App is not installed on it / GitHub not linked). Work is
  committed locally on `linux-port` and pushed once access works; any commit not yet on
  `origin/linux-port` when the session ends is listed in the final summary.
- `cloud-setup.sh` failed at the llama.cpp step on its first run: in a cloud session the GitHub
  REST API is gated per repository, so `api.github.com/repos/ggml-org/llama.cpp/releases/...`
  returned an access message instead of the release, and `jq` choked on it. The release asset
  itself downloads fine from `github.com/.../releases/download/`. Fixed in the script (falls back
  to the conventional asset name), see step 1.

## Step 1 — Baseline

- `dotnet build -p:EnableWindowsTargeting=true` on the unmodified `develop` tree: **succeeds**,
  0 errors, 40 warnings (all nullable / unused-field warnings, the same kind Windows shows).
- `.gitattributes` added with `*.sh text eol=lf`. `git add --renormalize .` changed nothing: the
  four `.sh` files were already LF in the index.
