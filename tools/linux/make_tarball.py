"""Archive a staged Linux package as .tar.gz with explicit file modes.

    python tools/linux/make_tarball.py <staged folder> <out.tar.gz> <executable name> [<executable name> ...]

package.ps1 calls this for -Runtime linux-x64. It exists because the package is built on Windows,
where the file system has no executable bit: a zip, or a tar written from what NTFS reports, would
unpack on Linux with the game and llama-server not executable. So the modes are set here by name,
not read from the disk:

    0755  every file whose name is passed as an executable (the game), llama-server, llama-bench,
          and every shared library (*.so, *.so.N...), which the loader maps executable
    0644  everything else
    0755  directories

Symbolic links are stored as links when the staging folder has them (a Linux machine), and as the
files they point at otherwise (a Windows checkout, where they arrive as copies). The loader is
content with either.

The top-level folder in the archive is the staged folder's own name, as in the Windows zip.
"""
import os
import re
import sys
import tarfile

SHARED_LIBRARY = re.compile(r"\.so(\.\d+)*$")
ALWAYS_EXECUTABLE = {"llama-server", "llama-bench"}


def mode_for(name: str, executables: set) -> int:
    if name in executables or name in ALWAYS_EXECUTABLE or SHARED_LIBRARY.search(name):
        return 0o755
    return 0o644


def main() -> int:
    if len(sys.argv) < 4:
        print(__doc__, file=sys.stderr)
        return 2
    stage, out, executables = sys.argv[1], sys.argv[2], set(sys.argv[3:])
    stage = os.path.abspath(stage)
    top = os.path.basename(stage.rstrip("/\\"))

    def clean(info: tarfile.TarInfo) -> tarfile.TarInfo:
        info.uid = info.gid = 0
        info.uname = info.gname = ""
        if info.isdir():
            info.mode = 0o755
        elif info.isfile():
            info.mode = mode_for(os.path.basename(info.name), executables)
        return info

    count = 0
    with tarfile.open(out, "w:gz", compresslevel=6) as tar:
        for dirpath, dirnames, filenames in os.walk(stage):
            dirnames.sort()
            rel = os.path.relpath(dirpath, stage)
            arcdir = top if rel == "." else f"{top}/{rel.replace(os.sep, '/')}"
            tar.add(dirpath, arcname=arcdir, recursive=False, filter=clean)
            for f in sorted(filenames):
                tar.add(os.path.join(dirpath, f), arcname=f"{arcdir}/{f}", recursive=False, filter=clean)
                count += 1

    missing = [e for e in executables if not any(
        os.path.basename(p) == e for _, _, fs in os.walk(stage) for p in fs)]
    if missing:
        print(f"make_tarball: executable(s) not in the package: {', '.join(missing)}", file=sys.stderr)
        return 1
    print(f"make_tarball: {count} files -> {out}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
