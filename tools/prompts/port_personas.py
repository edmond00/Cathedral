"""Writes rewritten persona fields into the modus mentis classes.

    python tools/prompts/port_personas.py rules/catalogue.json [--dry-run]

The JSON maps a modus mentis id to the fields to replace: {"chandlery": {"style": "...",
"reminder2": "..."}}. "style" is StyleInstruction (its string on the line after the arrow, or after
the comment lines that follow it, which are kept), and "reminder2" is PersonaReminder2 (on the
arrow's line); the rest of each file is left as it is. A field whose source is not in that shape is
reported and left alone, never guessed at.
"""
import argparse, glob, json, os, re

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DIR = os.path.join(ROOT, "src", "game", "narrative", "modimentis")
PATTERNS = {
    "style": re.compile(r'(public override string StyleInstruction =>\s*\n(?:\s*//[^\n]*\n)*\s*)"((?:[^"\\]|\\.)*)";'),
    "reminder2": re.compile(r'(public override string PersonaReminder2 => )"((?:[^"\\]|\\.)*)";'),
}


def cs_literal(text):
    return text.replace("\\", "\\\\").replace('"', '\\"')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("rewrites")
    ap.add_argument("--dry-run", action="store_true")
    a = ap.parse_args()
    rewrites = json.load(open(a.rewrites, encoding="utf-8"))
    files = {}
    for f in glob.glob(os.path.join(DIR, "*ModusMentis.cs")):
        m = re.search(r'ModusMentisId\s*=>\s*"([^"]+)"', open(f, encoding="utf-8").read())
        if m:
            files[m.group(1)] = f
    changed = 0
    for mid, fields in sorted(rewrites.items()):
        path = files.get(mid)
        if not path:
            print(f"{mid}: no class found")
            continue
        src = open(path, encoding="utf-8").read()
        new = src
        for field, text in fields.items():
            pat = PATTERNS[field]
            if len(pat.findall(new)) != 1:
                print(f"{mid}: {field} not in the expected shape, left alone")
                continue
            new = pat.sub(lambda m: m.group(1) + '"' + cs_literal(text) + '";', new)
        if new != src:
            changed += 1
            if not a.dry_run:
                open(path, "w", encoding="utf-8", newline="").write(new)
    print(f"{changed} classes {'would change' if a.dry_run else 'changed'}")


if __name__ == "__main__":
    main()
