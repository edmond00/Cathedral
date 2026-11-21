#!/usr/bin/env python3
"""Which characters the game can draw have no glyph in either of its fonts.

The terminal and the world sphere rasterise each character from one of two fonts, the main one
(Config.Terminal.FontPath) and a fallback (FallbackFontPath). The font that has the glyph draws
it (src/terminal/GlyphFonts.cs); Config.Terminal.FallbackGlyphs only says which to prefer when
both do. A character neither font has is drawn as the font's "missing" box.

This reads every character and string literal under src/ (escapes decoded, comments and plain
console/log lines left out) and every text file under assets/, and checks each non-ASCII
character against both fonts.

    python3 tools/glyph_audit.py            # report; exits 1 if a character has no glyph, or the list is stale
    python3 tools/glyph_audit.py --all      # also list characters only ever written to logs

It over-reports on purpose: a literal is not proof the character reaches the screen. A character
that is only logged can be acknowledged in LOG_ONLY below, with where it is. It cannot see text
a model writes at run time; the game reports those itself, once each, on stderr ("[glyphs] ...").

Needs fontTools (pip install fonttools).
"""
import collections, os, re, sys, unicodedata
from fontTools.ttLib import TTFont

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))

# Characters that appear in literals and are never drawn: where they go instead.
LOG_ONLY = {
    "⚠": "LLMLogger and OutcomeAudit: log files and audit output",
    "❌": "LLMLogger: log files",
    "💥": "ActionExecutionController: the failure-penalty line of the log",
    "⏱": "LlmMonitorWindow: the developer's monitor, drawn by the system font",
    "﻿": "a byte-order mark at the head of an art file, stripped on read",
}

LITERAL = re.compile(r'@"(?:[^"]|"")*"|\$?"(?:\\.|[^"\\\n])*"|\'(?:\\.|[^\'\\\n])+\'')
ESCAPE = re.compile(r'\\u([0-9A-Fa-f]{4})|\\U([0-9A-Fa-f]{8})|\\x([0-9A-Fa-f]{1,4})')


def config(name):
    text = open(os.path.join(ROOT, "src", "Config.cs"), encoding="utf-8").read()
    return re.search(name + r'\s*=\s*"([^"]+)"', text).group(1), text


def strip_comment(line):
    in_string = False
    for i in range(len(line) - 1):
        c = line[i]
        if c == '"' and (i == 0 or line[i - 1] != "\\"):
            in_string = not in_string
        if not in_string and line[i:i + 2] == "//":
            return line[:i]
    return line


def decode(literal):
    def sub(m):
        return chr(int(m.group(1) or m.group(2) or m.group(3), 16))
    return ESCAPE.sub(sub, literal)


def main():
    show_all = "--all" in sys.argv
    font_path, cfg = config("FontPath")
    fallback_path, _ = config("FallbackFontPath")
    main_font = TTFont(os.path.join(ROOT, font_path)).getBestCmap()
    fallback_font = TTFont(os.path.join(ROOT, fallback_path)).getBestCmap()
    block = re.search(r"FallbackGlyphs = new HashSet<char>\s*\{(.*?)\};", cfg, re.S).group(1)
    routed = set(re.findall(r"'(.)'", re.sub(r"//.*", "", block)))

    used = collections.defaultdict(set)
    for base, _, files in os.walk(os.path.join(ROOT, "src")):
        for name in files:
            if not name.endswith(".cs"):
                continue
            path = os.path.join(base, name)
            text = re.sub(r"/\*.*?\*/", "", open(path, encoding="utf-8", errors="replace").read(), flags=re.S)
            for line in text.split("\n"):
                line = strip_comment(line)
                if "Console." in line or "GameLog" in line or "throw new" in line:
                    continue
                for literal in LITERAL.findall(line):
                    for ch in decode(literal):
                        if ord(ch) > 126:
                            used[ch].add(os.path.relpath(path, ROOT))
    for base, _, files in os.walk(os.path.join(ROOT, "assets")):
        for name in files:
            if not name.endswith((".txt", ".json", ".csv")):
                continue
            path = os.path.join(base, name)
            try:
                text = open(path, encoding="utf-8").read()
            except (UnicodeDecodeError, OSError):
                continue
            for ch in text:
                if ord(ch) > 126 and ch not in "\r\n\t":
                    used[ch].add(os.path.relpath(path, ROOT))

    missing, by_fallback, logged = [], [], []
    for ch in sorted(used):
        cp = ord(ch)
        in_main, in_fallback = cp in main_font, cp in fallback_font
        row = (ch, cp, in_main, in_fallback, sorted(used[ch]))
        if in_main or in_fallback:
            # Drawn by the fallback font with nobody having asked for it: worth knowing, not a fault.
            if not in_main and ch not in routed and ch not in LOG_ONLY:
                by_fallback.append(row)
            continue
        (logged if ch in LOG_ONLY else missing).append(row)
    # An entry the fallback font lacks does nothing: the main font draws it. The list should say what is true.
    stale = [ch for ch in sorted(routed) if ord(ch) not in fallback_font]

    def show(rows, note):
        for ch, cp, in_main, in_fallback, files in rows:
            where = ", ".join(files[:3]) + (f" (+{len(files) - 3})" if len(files) > 3 else "")
            has = "main font has it" if in_main else "fallback font has it" if in_fallback else "NEITHER font has it"
            print(f"  {ch}  U+{cp:04X} {unicodedata.name(ch, '?')[:36]:36} {has:22} {note(ch)}{where}")

    print(f"{len(used)} non-ASCII characters in literals and assets; "
          f"main font {os.path.basename(font_path)}, fallback {os.path.basename(fallback_path)} for {len(routed)} listed.")
    if by_fallback:
        print("\nDrawn by the fallback font because the main font lacks them (not listed, and need not be):")
        show(by_fallback, lambda ch: "")
    if missing:
        print("\nIn neither font (a box on screen):")
        show(missing, lambda ch: "")
    if stale:
        print("\nListed in FallbackGlyphs, but the fallback font lacks them (remove the entries):", " ".join(stale))
    if show_all and logged:
        print("\nNever drawn (acknowledged in LOG_ONLY):")
        show(logged, lambda ch: f"[{LOG_ONLY[ch]}] ")
    if not (missing or stale):
        print("\nEvery character that is drawn has a glyph in one of the two fonts.")
        return 0
    return 1


if __name__ == "__main__":
    sys.exit(main())
