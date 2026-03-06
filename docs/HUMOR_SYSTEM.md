# Humor Queue System

Cathedral models the four classical humors as FIFO slot queues, one queue per humoral organ. Humor flows through the body continuously: organs secrete new humors into the front of their queue; consumption removes from the back.

---

## Humor Types

| Name          | Symbol | Color       | Vital Heat | Transmuting Virtue          | Source          |
|---------------|--------|-------------|------------|-----------------------------|-----------------|
| Blood         | ♉      | Deep red    | +1         | Convert face **5 → 6**      | High organ score |
| Phlegm        | ♓      | Pale blue   | 0          | Subtract **1** from result  | Always ~13%     |
| Yellow Bile   | ꤁      | Yellow      | −1         | Convert **any face → 1**    | Low organ score  |
| Black Bile    | ☩      | Dark red    | 0          | *(none)*                    | Medium-low score |
| Melancholia   | ☽      | Purple      | −2         | Subtract **2** from result  | Event-only       |

**Vital Heat** is an integer modifier applied to dice rolls when a humor is consumed. Positive values raise the result; negative values lower it.

**Transmuting Virtue** is the dice-face modification applied when the humor is consumed:
- `NumericModVirtue(n)` — adds `n` to the rolled value.
- `DigitConversionVirtue(src, tgt)` — converts face `src` to `tgt`; `src == -1` means "any face".

---

## Organs and Queues

Each party member has four `HumorQueue` instances grouped in a `HumorQueueSet`:

| Queue     | Organ   | Secretion bias                          |
|-----------|---------|-----------------------------------------|
| `Hepar`   | Liver   | Blood-heavy at high scores              |
| `Paunch`  | Stomach | Yellow-bile-heavy at low scores         |
| `Pulmones`| Lungs   | Phlegm baseline                         |
| `Spleen`  | Spleen  | Black-bile-heavy at moderate-low scores |

### Capacity

Each queue holds exactly **49 positions**, index 0 (newest) through 48 (oldest/consumed next).

### On-disk position-map encoding

The art overlay files (`assets/art/humors/*.txt`) mark each position with a single character:

| Character range | Queue index |
|-----------------|-------------|
| `'0'`–`'9'`     | 0–9         |
| `'a'`–`'z'`     | 10–35       |
| `'A'`–`'M'`     | 36–48       |

---

## Secretion Percentages (per organ)

Given an organ score `s` (1–10):

| Humor       | Formula                           | Notes                    |
|-------------|-----------------------------------|--------------------------|
| Blood       | `max(0, s × 8 − 3)`              | Dominant at s ≥ 5        |
| Yellow Bile | `max(0, 40 − s × 3)`             | Dominant at s ≤ 5        |
| Black Bile  | `max(0, 50 − s × 5)`             | Dominant at s ≤ 5        |
| Phlegm      | `100 − Blood% − YellowBile% − BlackBile%` | Always **13%**  |

Phlegm is always 13% because the other three formulas always sum to 87.

These exact percentages are exposed as derived stats (see §Derived Stats).

---

## Black Bile Stacking Rule

Black bile units are **pinned** at the back of the queue (high indices). They cannot be consumed by normal `Consume()` calls.

**Mechanism in `HumorQueue`:**
1. `FindRemoveIndex()` scans from the back forward, returning the index of the first *non-black-bile* humor.
2. That index is removed; all entries before it shift right by one; a new humor is secreted at index 0.
3. Any black biles at the tail stay in place indefinitely.

`BlackBileStackDepth` reports how many consecutive black-bile humors occupy the tail.  
`IsCritical` is true when `BlackBileStackDepth >= 20`.

Black bile can only be removed via event-driven purgation (`HumorQueue.ForceRemoveBlackBile()`).

---

## Queue Initialization

When a `PartyMember` is constructed, `HumorQueueSet.Initialize(member, rng)` fills all four queues to capacity using `FillWithSecretion(organScore, rng)`. This calls `Secrete()` 49 times, building the queue from position 48 down to 0, so the "oldest" slot reflects the organ's baseline composition at character creation.

---

## Derived Stats (16 total)

Each organ exposes four secretion-percentage stats:

- `hepar_blood_pct` / `hepar_phlegm_pct` / `hepar_yellowbile_pct` / `hepar_blackbile_pct`
- `paunch_blood_pct` / `paunch_phlegm_pct` / `paunch_yellowbile_pct` / `paunch_blackbile_pct`
- `pulmones_blood_pct` / `pulmones_phlegm_pct` / `pulmones_yellowbile_pct` / `pulmones_blackbile_pct`
- `spleen_blood_pct` / `spleen_phlegm_pct` / `spleen_yellowbile_pct` / `spleen_blackbile_pct`

These are re-computed every time the organ score changes, reflecting the current secretion bias.

---

## Core Public API (`HumorQueue`)

```csharp
// Consume the oldest consumable humor and secrete a new one at the front
BodyHumor Consume(int organScore, Random rng)

// Inject a specific humor at the front (used by events and FeelGoodOutcome)
void Produce(BodyHumor humor)

// Secrete one new humor at the front based on organ score (no consumption)
void Secrete(int organScore, Random rng)

// Fill all 49 slots (character creation)
void FillWithSecretion(int organScore, Random rng)

// Forcibly remove one black-bile unit from the tail (purgation ritual)
bool ForceRemoveBlackBile()

// Query
int  BlackBileStackDepth { get; }
bool IsCritical           { get; }
BodyHumor? PeekConsumable() // oldest non-black-bile, or null
bool HasHumorType<T>()       where T : BodyHumor
```

---

## Adding a New Humor Type

1. Create `src/game/narrative/humors/YourHumor.cs` extending `BodyHumor` in `namespace Cathedral.Game.Narrative`.
2. Implement `Symbol`, `Name`, `Color`, `VitalHeat`, `TransmutingVirtue` (return `null` if none), and optionally override `IsBlackBile`.
3. Add a weighted case to `HumorQueue.CreateWeightedHumor(organScore, rng)` to control when it appears.
4. If it should appear in `FeelGoodOutcome.DetermineHumorChangeAsync`, add `new YourHumor()` to the `producibleHumors` array in [FeelGoodOutcome.cs](../src/game/narrative/FeelGoodOutcome.cs).

---

## Triggering Melancholia Production

Melancholia is event-only — it is never selected by the normal secretion formula. To inject it:

```csharp
protagonist.HumorQueues.Hepar.Produce(new MelancholiaHumor());
```

Or use the future `HumorQueueSet.ProduceHumor(organId, humor)` API once the routing logic is complete.

---

## Management UI

The **Humors** tab in the management menu renders the four interconnected spirals from `assets/art/humors/ascii_art.txt`, overlaid with humor symbols (`♉ ♓ ꤁ ☩ ☽`) at the positions defined by the position-map files.

Hovering over any symbol shows details in the bottom info panel (rows 77–99):
- Organ name and queue position
- Humor name and vital-heat value (green = positive, red = negative)
- Transmuting virtue description and plain-language explanation
- Black bile corruption warning (if applicable)
