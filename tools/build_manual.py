#!/usr/bin/env python3
"""
Typeset docs/manual/*.md into a single PDF.

    python tools/build_manual.py                 -> docs/manual/ProscribedPalimpsest-Manual.pdf
    python tools/build_manual.py --html-only     -> leaves the intermediate HTML for inspection
    python tools/build_manual.py -o some.pdf     -> writes somewhere else

HOW IT WORKS
    Markdown is converted to HTML by the small parser below (the manual uses a narrow, regular
    subset, so this costs nothing and keeps the script dependency-free), styled for print, and
    rendered by headless Chrome — which is on every Windows machine and needs no install.

    Chrome's paged-media support stops at page size, margins and breaks: it has no CSS page
    counters and no margin boxes, so it cannot draw folios or running heads. Those are stamped on
    afterwards. That is also why each chapter is rendered SEPARATELY — it is what makes the page
    count of each chapter known, which is what a folio sequence, a recto-opening rule and a
    page-numbered contents are all derived from.

REQUIREMENTS
    Chrome or Edge          required. Found automatically.
    pypdf + reportlab       optional. Without them you get the same typesetting with no folios,
                            no running heads and no contents page numbers, and a warning saying so.
                            pip install pypdf reportlab
"""

from __future__ import annotations

import argparse
import base64
import html
import io
import os
import re
import shutil
import subprocess
import sys
import tempfile
from pathlib import Path

# The console here is not necessarily UTF-8 (a cp932 console will refuse an em dash outright and
# take the build down with it, after the PDF has already been written). Never let reporting fail.
for _stream in (sys.stdout, sys.stderr):
    try:
        _stream.reconfigure(encoding="utf-8", errors="replace")
    except Exception:
        pass

ROOT = Path(__file__).resolve().parent.parent
MANUAL_DIR = ROOT / "docs" / "manual"
DEFAULT_OUT = MANUAL_DIR / "ProscribedPalimpsest-Manual.pdf"

BOOK_TITLE = "Proscribed Palimpsest"
BOOK_SUBTITLE = "A Manual of its Systems"

# 6 x 9 inches, in points — the trim size, needed by the stamping pass.
PAGE_W, PAGE_H = 432.0, 648.0


# ─────────────────────────────────────────────────────────────────────────────
# Markdown → HTML
# ─────────────────────────────────────────────────────────────────────────────
# Deliberately not a general Markdown implementation. It covers exactly what the manual uses:
# ATX headings, paragraphs, pipe tables, unordered and ordered lists, horizontal rules, and the
# four inline forms. Anything else passes through as text, which is a visible failure rather than
# a silent one.

_INLINE_CODE = re.compile(r"`([^`]+)`")
_STRONG = re.compile(r"\*\*([^*]+)\*\*")
_EM = re.compile(r"(?<!\*)\*([^*]+)\*(?!\*)")
_LINK = re.compile(r"\[([^\]]+)\]\(([^)]+)\)")


def inline(text: str) -> str:
    """Inline formatting. Escaped first, so the source can contain < and & safely."""
    out = html.escape(text, quote=False)
    # Code before emphasis: a code span may legitimately contain asterisks.
    codes: list[str] = []

    def stash(m: re.Match[str]) -> str:
        codes.append(m.group(1))
        return f"\x00{len(codes) - 1}\x00"

    out = _INLINE_CODE.sub(stash, out)
    out = _LINK.sub(r'<a href="\2">\1</a>', out)
    out = _STRONG.sub(r"<strong>\1</strong>", out)
    out = _EM.sub(r"<em>\1</em>", out)
    for i, c in enumerate(codes):
        out = out.replace(f"\x00{i}\x00", f"<code>{c}</code>")
    return out


def _split_row(line: str) -> list[str]:
    return [c.strip() for c in line.strip().strip("|").split("|")]


def _alignments(sep: str) -> list[str]:
    out = []
    for c in _split_row(sep):
        left, right = c.startswith(":"), c.endswith(":")
        out.append("center" if left and right else "right" if right else "left")
    return out


def markdown_to_html(md: str) -> tuple[str, str]:
    """Returns (chapter title, body HTML). The title is the first level-1 heading."""
    lines = md.replace("\r\n", "\n").split("\n")
    out: list[str] = []
    title = ""
    i = 0
    first_para_done = False

    while i < len(lines):
        line = lines[i]
        stripped = line.strip()

        if not stripped:
            i += 1
            continue

        # Horizontal rule — used in the manual only as the footer separator.
        if re.fullmatch(r"-{3,}", stripped):
            out.append('<hr class="rule"/>')
            i += 1
            continue

        # Headings
        m = re.match(r"^(#{1,4})\s+(.*)$", stripped)
        if m:
            level = len(m.group(1))
            text = m.group(2).strip()
            if level == 1 and not title:
                title = re.sub(r"^[IVXLC]+\.\s*", "", text)
                out.append(f"<h1>{inline(text)}</h1>")
            else:
                out.append(f"<h{level}>{inline(text)}</h{level}>")
            i += 1
            continue

        # Pipe table: a header row followed by an alignment row.
        if stripped.startswith("|") and i + 1 < len(lines) and re.match(
            r"^\s*\|[\s:|-]+\|\s*$", lines[i + 1]
        ):
            header = _split_row(stripped)
            aligns = _alignments(lines[i + 1])
            i += 2
            body: list[list[str]] = []
            while i < len(lines) and lines[i].strip().startswith("|"):
                body.append(_split_row(lines[i].strip()))
                i += 1
            out.append('<table>')
            out.append("<thead><tr>" + "".join(
                f'<th style="text-align:{aligns[j] if j < len(aligns) else "left"}">{inline(c)}</th>'
                for j, c in enumerate(header)
            ) + "</tr></thead><tbody>")
            for row in body:
                out.append("<tr>" + "".join(
                    f'<td style="text-align:{aligns[j] if j < len(aligns) else "left"}">{inline(c)}</td>'
                    for j, c in enumerate(row)
                ) + "</tr>")
            out.append("</tbody></table>")
            continue

        # Lists. No nesting: the manual has none, and supporting it silently would hide the day
        # somebody writes one and it comes out flat.
        bullet = re.match(r"^[-*]\s+(.*)$", stripped)
        number = re.match(r"^\d+[.)]\s+(.*)$", stripped)
        if bullet or number:
            tag = "ul" if bullet else "ol"
            pattern = r"^[-*]\s+(.*)$" if bullet else r"^\d+[.)]\s+(.*)$"
            items: list[str] = []
            while i < len(lines):
                m2 = re.match(pattern, lines[i].strip())
                if m2:
                    items.append(m2.group(1))
                    i += 1
                elif lines[i].strip() and lines[i].startswith(("  ", "\t")) and items:
                    items[-1] += " " + lines[i].strip()   # continuation line
                    i += 1
                else:
                    break
            out.append(f"<{tag}>" + "".join(f"<li>{inline(t)}</li>" for t in items) + f"</{tag}>")
            continue

        # Paragraph: consume until a blank line or a construct.
        para = [stripped]
        i += 1
        while i < len(lines):
            nxt = lines[i].strip()
            if not nxt or nxt.startswith(("#", "|", "- ", "* ")) or re.match(r"^\d+[.)]\s", nxt) \
                    or re.fullmatch(r"-{3,}", nxt):
                break
            para.append(nxt)
            i += 1
        cls = ""
        if not first_para_done and title:
            cls = ' class="opening"'      # carries the drop capital
            first_para_done = True
        out.append(f"<p{cls}>{inline(' '.join(para))}</p>")

    return title or "Untitled", "\n".join(out)


# ─────────────────────────────────────────────────────────────────────────────
# The style sheet
# ─────────────────────────────────────────────────────────────────────────────
# Monochrome by instruction: black, white and grey only. Hierarchy therefore has to come entirely
# from size, letterspacing, small capitals, rules and grey value — which is how a nineteenth-century
# press did it anyway, since a second ink cost a second pass.

def stylesheet(mono_data_uri: str, sink: bool) -> str:
    return f"""
@font-face {{
  font-family: 'ManualMono';
  src: url({mono_data_uri}) format('truetype');
  font-weight: normal; font-style: normal;
}}

@page {{
  size: 6in 9in;
  /* Room at head and foot for the running head and folio stamped on afterwards. */
  /* The head margin has to clear the running head AND its rule, which are stamped at a fixed
     offset below the trim edge; too little and the rule reads as the top rule of whatever
     follows it. */
  margin: 0.92in 0.78in 0.85in 0.78in;
}}

html {{ -webkit-print-color-adjust: exact; print-color-adjust: exact; }}

body {{
  /* Constantia is a print text serif with oldstyle figures as its default — the single most
     period-correct face installed on a stock Windows machine. Sitka and Cambria are the next
     best; Georgia is the floor. */
  font-family: Constantia, 'Sitka Text', Sitka, Cambria, Georgia, 'Times New Roman', serif;
  font-size: 10.5pt;
  line-height: 15.5pt;                 /* the baseline; every vertical space is a multiple */
  color: #1a1a1a;
  background: #ffffff;
  margin: 0;
  text-align: justify;
  hyphens: auto; -webkit-hyphens: auto;
  font-variant-numeric: oldstyle-nums proportional-nums;
  font-kerning: normal;
  font-variant-ligatures: common-ligatures;
  orphans: 2; widows: 2;
}}

/* ── Chapter opening ─────────────────────────────────────────────────────── */
h1 {{
  {"padding-top: 2.05in;" if sink else ""}   /* the sink: text begins a third down the page */
  font-size: 15pt;
  font-weight: normal;
  font-variant-caps: small-caps;
  letter-spacing: 0.14em;
  text-align: center;
  text-indent: 0;
  margin: 0 0 46.5pt 0;
  padding-bottom: 15.5pt;
  border-bottom: 0.5pt solid #1a1a1a;
}}

h2 {{
  font-size: 10.5pt;
  font-weight: normal;
  font-variant-caps: small-caps;
  letter-spacing: 0.1em;
  text-align: left;
  margin: 31pt 0 7.75pt 0;
  break-after: avoid; page-break-after: avoid;
}}

h3 {{
  font-size: 10.5pt;
  font-weight: normal;
  font-style: italic;
  letter-spacing: 0.01em;
  color: #3d3d3d;
  margin: 15.5pt 0 0 0;
  break-after: avoid; page-break-after: avoid;
}}

/* ── Prose ───────────────────────────────────────────────────────────────── */
p {{ margin: 0; text-indent: 1.2em; }}

/* First paragraph of a section is never indented — indentation marks a *continuation*,
   and there is nothing yet to continue from. */
h1 + p, h2 + p, h3 + p, hr + p, table + p, ul + p, ol + p {{ text-indent: 0; }}

p.opening {{ text-indent: 0; }}
p.opening::first-letter {{
  float: left;
  font-size: 40pt;
  line-height: 31pt;
  padding: 2pt 4pt 0 0;
  font-variant-caps: normal;
}}
p.opening::first-line {{ font-variant-caps: small-caps; letter-spacing: 0.04em; }}

em {{ font-style: italic; }}
strong {{ font-weight: 600; }}

code {{
  font-family: 'ManualMono', Consolas, monospace;
  font-size: 8.6pt;
  font-variant-numeric: lining-nums tabular-nums;
}}

a {{ color: inherit; text-decoration: none; }}

/* ── Tables: the booktabs convention ─────────────────────────────────────── */
/* No fills, no zebra, no vertical rules, no boxes. Three horizontal rules only. This is the
   single change that most separates a typeset table from a generated one. */
table {{
  width: 100%;
  border-collapse: collapse;
  margin: 15.5pt 0;
  font-size: 9.4pt;
  line-height: 13pt;
  text-align: left;
  hyphens: none; -webkit-hyphens: none;
  /* Lining, tabular figures so columns align — the deliberate counterpart to the oldstyle
     figures used throughout the prose. */
  font-variant-numeric: lining-nums tabular-nums;
  break-inside: avoid; page-break-inside: avoid;
}}
thead th {{
  font-weight: normal;
  font-variant-caps: small-caps;
  letter-spacing: 0.06em;
  padding: 3pt 8pt 3.5pt 0;
  border-top: 1pt solid #1a1a1a;
  border-bottom: 0.4pt solid #8c8c8c;
}}
tbody td {{ padding: 2.6pt 8pt 2.6pt 0; vertical-align: top; }}
tbody tr:last-child td {{ border-bottom: 1pt solid #1a1a1a; }}
th:last-child, td:last-child {{ padding-right: 0; }}

/* ── Lists ───────────────────────────────────────────────────────────────── */
ul, ol {{ margin: 11pt 0 11pt 0; padding-left: 1.5em; }}
li {{ margin-bottom: 4pt; text-align: justify; }}
li::marker {{ color: #6b6b6b; }}

/* ── The rule that ends a chapter ────────────────────────────────────────── */
hr.rule {{
  border: none;
  border-top: 0.4pt solid #b0b0b0;
  width: 22%;
  margin: 26pt auto 15.5pt auto;
}}
/* The navigation line the Markdown carries for web reading is meaningless on paper. */
hr.rule + p {{ display: none; }}

/* ── Front matter ────────────────────────────────────────────────────────── */
.titlepage {{ text-align: center; padding-top: 2.6in; }}
.titlepage .title {{
  font-size: 25pt; font-variant-caps: small-caps; letter-spacing: 0.17em;
  margin: 0 0 22pt 0; text-indent: 0;
}}
.titlepage .subtitle {{
  font-size: 12pt; font-style: italic; color: #3d3d3d; margin: 0; text-indent: 0;
}}
.titlepage .divider {{
  border: none; border-top: 0.5pt solid #1a1a1a; width: 34%; margin: 31pt auto;
}}
.titlepage .imprint {{
  font-size: 9pt; font-variant-caps: small-caps; letter-spacing: 0.11em;
  color: #5c5c5c; margin: 0; text-indent: 0;
}}

.colophon {{ padding-top: 1.6in; text-align: center; font-size: 9.4pt; color: #3d3d3d; }}
.colophon p {{ text-indent: 0; margin-bottom: 11pt; }}

h1.plain {{ padding-top: 0; }}

.toc {{ margin-top: 15.5pt; }}
.toc-entry {{
  display: flex; align-items: baseline;
  margin: 0 0 9pt 0; text-indent: 0; text-align: left;
}}
.toc-entry .num {{
  /* Wide enough for VIII., which is the longest numeral the manual reaches. */
  flex: 0 0 3.1em; font-variant-caps: small-caps; letter-spacing: 0.06em; color: #5c5c5c;
}}
.toc-entry .name {{ flex: 0 0 auto; }}
.toc-entry .dots {{
  flex: 1 1 auto; margin: 0 0.5em;
  border-bottom: 0.4pt dotted #a8a8a8; transform: translateY(-0.25em);
}}
.toc-entry .folio {{ flex: 0 0 auto; font-variant-numeric: lining-nums; color: #3d3d3d; }}
.toc-part {{
  font-variant-caps: small-caps; letter-spacing: 0.11em; color: #1a1a1a;
  margin: 20pt 0 9pt 0; text-indent: 0; text-align: left; font-size: 9.4pt;
}}
.toc-part:first-of-type {{ margin-top: 0; }}
"""


def page_html(body: str, mono_uri: str, sink: bool) -> str:
    return f"""<!doctype html>
<html><head><meta charset="utf-8"><style>{stylesheet(mono_uri, sink)}</style></head>
<body>{body}</body></html>"""


# ─────────────────────────────────────────────────────────────────────────────
# Chrome
# ─────────────────────────────────────────────────────────────────────────────

CHROME_CANDIDATES = [
    r"C:\Program Files\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe",
    r"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
    r"C:\Program Files\Microsoft\Edge\Application\msedge.exe",
    "/usr/bin/google-chrome", "/usr/bin/chromium", "/usr/bin/chromium-browser",
    "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome",
]


def find_chrome() -> str:
    for name in ("chrome", "google-chrome", "chromium", "msedge"):
        found = shutil.which(name)
        if found:
            return found
    for path in CHROME_CANDIDATES:
        if os.path.exists(path):
            return path
    sys.exit("No Chrome or Edge found. Install one, or put it on PATH.")


def render_pdf(chrome: str, html_path: Path, pdf_path: Path) -> None:
    proc = subprocess.run(
        [chrome, "--headless", "--disable-gpu", "--no-pdf-header-footer",
         "--virtual-time-budget=10000",
         f"--print-to-pdf={pdf_path}", html_path.as_uri()],
        capture_output=True, text=True, timeout=180,
    )
    if not pdf_path.exists():
        sys.exit(f"Chrome produced no PDF for {html_path.name}:\n{proc.stderr.strip()}")


# ─────────────────────────────────────────────────────────────────────────────
# Folios and running heads
# ─────────────────────────────────────────────────────────────────────────────

def load_stamping():
    """The optional half. Returns None when pypdf/reportlab are absent."""
    try:
        from pypdf import PdfReader, PdfWriter          # noqa: F401
        from reportlab.pdfgen import canvas             # noqa: F401
        from reportlab.pdfbase import pdfmetrics        # noqa: F401
        from reportlab.pdfbase.ttfonts import TTFont    # noqa: F401
        return True
    except ImportError:
        return None


FONT_CANDIDATES = [
    (r"C:\Windows\Fonts\constan.ttf", "Constantia"),
    (r"C:\Windows\Fonts\cambria.ttc", "Cambria"),
    (r"C:\Windows\Fonts\georgia.ttf", "Georgia"),
]


def register_stamp_font() -> str:
    """The face used for folios and running heads. Falls back to a built-in."""
    from reportlab.pdfbase import pdfmetrics
    from reportlab.pdfbase.ttfonts import TTFont
    for path, name in FONT_CANDIDATES:
        if os.path.exists(path) and path.lower().endswith(".ttf"):
            try:
                pdfmetrics.registerFont(TTFont(name, path))
                return name
            except Exception:
                continue
    return "Times-Roman"


def stamp_overlay(font: str, folio: int, running_head: str | None):
    """One transparent page carrying the furniture, to be merged onto a rendered page."""
    from reportlab.pdfgen import canvas
    buf = io.BytesIO()
    c = canvas.Canvas(buf, pagesize=(PAGE_W, PAGE_H))

    if running_head:
        # Letterspaced small capitals, faked: reportlab's TTF support reaches no OpenType
        # features, so the effect is built out of capitals, size and tracking instead.
        # Tracking lives on a text object — the canvas has no setter for it.
        text = running_head.upper()
        tracking = 1.5
        w = c.stringWidth(text, font, 7.6) + tracking * (len(text) - 1)
        t = c.beginText((PAGE_W - w) / 2.0, PAGE_H - 38)
        t.setFont(font, 7.6)
        t.setFillGray(0.36)
        t.setCharSpace(tracking)
        t.textOut(text)
        c.drawText(t)
        c.setStrokeGray(0.72)
        c.setLineWidth(0.4)
        c.line(56, PAGE_H - 47, PAGE_W - 56, PAGE_H - 47)

    c.setFont(font, 9)
    c.setFillGray(0.22)
    s = str(folio)
    c.drawString((PAGE_W - c.stringWidth(s, font, 9)) / 2.0, 42, s)

    c.showPage()
    c.save()
    buf.seek(0)
    return buf


# ─────────────────────────────────────────────────────────────────────────────
# Build
# ─────────────────────────────────────────────────────────────────────────────

PARTS = {
    1: "The Frame",
    4: "What is Carried",
    5: "The World",
    6: "The Conduct of a Visit",
}

ROMAN = {1: "I", 2: "II", 3: "III", 4: "IV", 5: "V", 6: "VI", 7: "VII", 8: "VIII", 9: "IX"}


def chapter_files() -> list[Path]:
    files = sorted(p for p in MANUAL_DIR.glob("*.md") if re.match(r"^\d\d-", p.name))
    if not files:
        sys.exit(f"No numbered chapters found in {MANUAL_DIR}")
    return files


def check_links(files: list[Path]) -> int:
    """
    Warn about cross-references that no longer resolve. Renumbering a section silently breaks every
    link into it, and nothing else in the pipeline would notice — the PDF renders a dead link as
    ordinary text, so the reader is simply sent nowhere.
    """
    def slug(heading: str) -> str:
        s = re.sub(r"[^\w\s-]", "", heading.strip().lower())
        return re.sub(r"\s+", "-", s).strip("-")

    anchors: dict[str, set[str]] = {}
    for p in files:
        text = p.read_text(encoding="utf-8")
        anchors[p.name] = {slug(m.group(1)) for m in re.finditer(r"^#{1,4}\s+(.*)$", text, re.M)}

    broken = 0
    for p in files:
        joined = " ".join(p.read_text(encoding="utf-8").split())   # links may wrap across lines
        for label, target in re.findall(r"\[([^\]]+)\]\(([^)]+)\)", joined):
            if target.startswith(("http://", "https://")):
                continue
            file, _, frag = target.partition("#")
            name = file or p.name
            if name not in anchors:
                print(f"  ! {p.name}: link to missing file '{target}'  ({label})"); broken += 1
            elif frag and frag not in anchors[name]:
                print(f"  ! {p.name}: dead anchor '{target}'  ({label})"); broken += 1
    return broken


def git_sha() -> str:
    synced = MANUAL_DIR / ".synced"
    if synced.exists():
        return synced.read_text(encoding="utf-8").strip()[:12]
    try:
        return subprocess.run(["git", "rev-parse", "HEAD"], cwd=ROOT,
                              capture_output=True, text=True).stdout.strip()[:12]
    except Exception:
        return "unknown"


def main() -> None:
    ap = argparse.ArgumentParser(description="Typeset docs/manual into a PDF.")
    ap.add_argument("-o", "--output", type=Path, default=DEFAULT_OUT)
    ap.add_argument("--html-only", action="store_true",
                    help="write the intermediate HTML next to the output and stop")
    args = ap.parse_args()

    chrome = find_chrome()
    can_stamp = load_stamping()

    mono_path = ROOT / "assets" / "fonts" / "DejaVuSansMono.ttf"
    mono_uri = ("data:font/ttf;base64,"
                + base64.b64encode(mono_path.read_bytes()).decode()) if mono_path.exists() else ""

    files = chapter_files()
    print(f"Manual: {len(files)} chapters, rendering with {Path(chrome).name}")

    broken = check_links(files + [MANUAL_DIR / "README.md"])
    if broken:
        print(f"  {broken} broken cross-reference(s) — the PDF will render them as plain text\n")

    with tempfile.TemporaryDirectory(prefix="manual-") as tmp:
        tmp = Path(tmp)
        chapters = []

        # ── Pass 1: render each chapter alone, so its page count is known ──────
        for idx, path in enumerate(files, start=1):
            title, body = markdown_to_html(path.read_text(encoding="utf-8"))
            hp = tmp / f"{path.stem}.html"
            hp.write_text(page_html(body, mono_uri, sink=True), encoding="utf-8")
            if args.html_only:
                debug_dir = args.output.parent / "_html"
                debug_dir.mkdir(parents=True, exist_ok=True)
                shutil.copy(hp, debug_dir / f"{path.stem}.html")
                continue
            pp = tmp / f"{path.stem}.pdf"
            render_pdf(chrome, hp, pp)
            chapters.append({"n": idx, "title": title, "pdf": pp})
            print(f"  {ROMAN.get(idx, idx):>4}. {title}")

        if args.html_only:
            print(f"HTML written to {args.output.parent / '_html'}")
            return

        if not can_stamp:
            print("\n! pypdf and reportlab are not installed, so this PDF will have no folios,\n"
                  "  no running heads and no contents page numbers.\n"
                  "  pip install pypdf reportlab\n")
            from_simple_merge(chapters, args.output)
            return

        from pypdf import PdfReader, PdfWriter

        # ── Pagination: every chapter opens on a recto ─────────────────────────
        folio = 1
        for ch in chapters:
            ch["pages"] = len(PdfReader(str(ch["pdf"])).pages)
            if folio % 2 == 0:              # a recto is an odd folio
                ch["blank_before"] = True
                folio += 1
            else:
                ch["blank_before"] = False
            ch["start"] = folio
            folio += ch["pages"]

        # ── Front matter, now that the folios are known ────────────────────────
        title_html = tmp / "title.html"
        title_html.write_text(page_html(f"""
<div class="titlepage">
  <p class="title">{html.escape(BOOK_TITLE)}</p>
  <p class="subtitle">{html.escape(BOOK_SUBTITLE)}</p>
  <hr class="divider"/>
  <p class="imprint">Being an account of the machinery<br/>upon which the world is conducted</p>
</div>""", mono_uri, sink=False), encoding="utf-8")
        title_pdf = tmp / "title.pdf"
        render_pdf(chrome, title_html, title_pdf)

        entries = []
        for ch in chapters:
            if ch["n"] in PARTS:
                entries.append(f'<p class="toc-part">{html.escape(PARTS[ch["n"]])}</p>')
            entries.append(
                '<p class="toc-entry">'
                f'<span class="num">{ROMAN.get(ch["n"], ch["n"])}.</span>'
                f'<span class="name">{html.escape(ch["title"])}</span>'
                '<span class="dots"></span>'
                f'<span class="folio">{ch["start"]}</span></p>'
            )
        toc_html = tmp / "toc.html"
        toc_html.write_text(page_html(
            '<h1 class="plain">Contents</h1><div class="toc">' + "".join(entries) + "</div>",
            mono_uri, sink=False), encoding="utf-8")
        toc_pdf = tmp / "toc.pdf"
        render_pdf(chrome, toc_html, toc_pdf)

        colophon_html = tmp / "colophon.html"
        colophon_html.write_text(page_html(f"""
<div class="colophon">
  <hr class="divider" style="width:22%;margin:0 auto 31pt auto;border-top:0.4pt solid #b0b0b0"/>
  <p>Set from the source of the game itself,<br/>at revision <code>{git_sha()}</code>.</p>
  <p>The text is Constantia; the monospace, DejaVu Sans Mono,<br/>
     which is the face the game itself is lettered in.</p>
</div>""", mono_uri, sink=False), encoding="utf-8")
        colophon_pdf = tmp / "colophon.pdf"
        render_pdf(chrome, colophon_html, colophon_pdf)

        # ── Assemble ──────────────────────────────────────────────────────────
        font = register_stamp_font()
        writer = PdfWriter()

        for p in PdfReader(str(title_pdf)).pages:      # front matter carries no folio
            writer.add_page(p)
        writer.add_blank_page(PAGE_W, PAGE_H)
        for p in PdfReader(str(toc_pdf)).pages:
            writer.add_page(p)

        while len(writer.pages) % 2 != 0:               # chapter I must land on a recto
            writer.add_blank_page(PAGE_W, PAGE_H)

        for ch in chapters:
            if ch["blank_before"]:
                writer.add_blank_page(PAGE_W, PAGE_H)
            for i, page in enumerate(PdfReader(str(ch["pdf"])).pages):
                n = ch["start"] + i
                # A chapter's opening page carries no running head — the title is right there.
                head = None if i == 0 else (BOOK_TITLE if n % 2 == 0 else ch["title"])
                overlay = PdfReader(stamp_overlay(font, n, head)).pages[0]
                page.merge_page(overlay)
                writer.add_page(page)

        for p in PdfReader(str(colophon_pdf)).pages:
            writer.add_page(p)

        writer.add_metadata({
            "/Title": f"{BOOK_TITLE} — {BOOK_SUBTITLE}",
            "/Subject": "The systems of the game, explained.",
            "/Creator": "tools/build_manual.py",
        })

        args.output.parent.mkdir(parents=True, exist_ok=True)
        with open(args.output, "wb") as fh:
            writer.write(fh)

    size_kb = args.output.stat().st_size / 1024
    print(f"\n{args.output.relative_to(ROOT)} — {folio - 1} numbered pages, {size_kb:.0f} KB")


def from_simple_merge(chapters, out: Path) -> None:
    """Degraded path: concatenate the rendered chapters with no furniture."""
    data = b""
    for ch in chapters:
        data = ch["pdf"].read_bytes()   # only meaningful for a single chapter
    if len(chapters) == 1:
        out.write_bytes(data)
        print(f"{out} written (single chapter, no furniture).")
        return
    sys.exit("pypdf is required to join the chapters. Run: pip install pypdf reportlab")


if __name__ == "__main__":
    main()
