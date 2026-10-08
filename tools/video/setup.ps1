# Sets up the video pipeline: a virtualenv in tools/video/.venv with manim, Piper, ffmpeg and the rest,
# and the Piper voices in tools/video/voices. Idempotent: run it again and it only fills what is missing.
#
#   powershell -ExecutionPolicy Bypass -File tools/video/setup.ps1
#
# Needs a Python 3.10-3.12 on PATH (manim has no wheels beyond that yet). Nothing is installed system-wide.

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$venv = Join-Path $here ".venv"
$py = Join-Path $venv "Scripts\python.exe"

if (-not (Test-Path $py)) {
    Write-Host "Creating $venv"
    python -m venv $venv
}
& $py -m pip install --quiet --upgrade pip
& $py -m pip install --quiet -r (Join-Path $here "requirements.txt")

$voices = Join-Path $here "voices"
New-Item -ItemType Directory -Force $voices | Out-Null
# name -> path under https://huggingface.co/rhasspy/piper-voices (MIT-licensed models)
$wanted = @{
    "en_GB-alan-medium"                  = "en/en_GB/alan/medium"
    "en_GB-northern_english_male-medium" = "en/en_GB/northern_english_male/medium"
    "en_US-ryan-high"                    = "en/en_US/ryan/high"
}
foreach ($name in $wanted.Keys) {
    foreach ($ext in @(".onnx", ".onnx.json")) {
        $dst = Join-Path $voices "$name$ext"
        if (-not (Test-Path $dst)) {
            $url = "https://huggingface.co/rhasspy/piper-voices/resolve/main/$($wanted[$name])/$name$ext"
            Write-Host "Fetching $name$ext"
            Invoke-WebRequest -Uri $url -OutFile $dst
        }
    }
}

& $py -c "import manim, piper, imageio_ffmpeg; print('video pipeline ready: manim', manim.__version__, '| ffmpeg', imageio_ffmpeg.get_ffmpeg_exe())"
