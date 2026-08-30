---
description: Reconcile docs/manual/ with code changes since the last sync
---

Bring the player manual back in line with the code.

1. Invoke the `manual` skill — it carries the style rules and the chapter map. Follow them.
2. Read `docs/manual/.synced` for the last reconciled commit, then:
   `git diff --stat <sha>..HEAD -- src/`
   If `.synced` is missing, treat every chapter as needing review and say so.
3. Decide which chapters the diff touches. Changes confined to rendering, the CLI driver,
   packaging, audits or tests touch no chapter.
4. Read the actual diff for the areas that matter (`git diff <sha>..HEAD -- <paths>`), and revise
   only the affected chapters. Do not rewrite chapters the diff did not reach.
5. Write the current `HEAD` sha into `docs/manual/.synced`.
6. If any chapter changed, rebuild the PDF: `python tools/build_manual.py`.
7. Report: which chapters changed, which were checked and left alone, and anything in the diff you
   could not confidently map to a chapter.

$ARGUMENTS may name a chapter or a subsystem to restrict the sync to. With no arguments, sync
everything the diff touches.
