---
description: Cut, review and publish a new Volume of the game to itch.io
---

Publish a new version of Proscribed Palimpsest.

Invoke the `release` skill and follow its ten steps in order. It carries the track record of the
last published commit, the changelog rules, the Volume naming convention, the review checklist, the
hand-off to the `manual` skill and the exact git shape of the develop → main merge.

Two things it will stop and ask you about, by design: the new Volume subtitle (step 4), and the
upload itself (step 9, which is irreversible).

$ARGUMENTS may restrict the run:

| | |
|---|---|
| *(none)* | the whole release |
| `status` | report the pending range against the record and stop |
| `changelog` | write and share the note, touching no branch and no constant |
| `review` | the verification pass alone (steps 5 and 6) |
| `record <sha>` | move the track record to `<sha>`, keeping the Volume label — what to run after a hotfix uploaded by hand, so the next changelog does not re-announce it |
