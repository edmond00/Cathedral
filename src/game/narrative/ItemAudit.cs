using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Fight;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Headless report on the item catalogue — the counterpart to <c>--npc-audit</c> and
/// <c>--dialogue-audit</c>, run with <c>--item-audit</c>. It reads
/// <see cref="ItemRegistry"/>, so coverage is automatic: any item with a public parameterless
/// constructor is audited the moment it is written, with no list to keep in sync.
///
/// It checks the things that stay invisible until a player trips over them:
///
/// <list type="bullet">
///   <item><b>Identity.</b> Duplicate item ids or display names — two items the player cannot tell
///     apart, and that trade matches by type rather than by name.</item>
///   <item><b>Reachability.</b> A liquid no vessel accepts can never be picked up; an item with no
///     trade tag can never appear in a catalogue.</item>
///   <item><b>Authoring gaps.</b> Items left on an inherited weight, categories short of items,
///     trade tags too thin to fill a catalogue.</item>
///   <item><b>Leakage.</b> Debug items reaching the live registry, where they surface in shops.</item>
/// </list>
///
/// Failures are reported, never thrown — the report is a worklist, not a gate.
/// </summary>
public static class ItemAudit
{
    /// <summary>Below this, <c>NpcTradeCatalog.Build</c> offers every tagged item and stops varying.</summary>
    private const int MinItemsPerTag = 3;

    /// <summary>Below this, a subcategory offers the player no real choice.</summary>
    private const int MinItemsPerCategory = 3;

    /// <summary>Ceiling on a single garment's contribution, per the WearableItem contract.</summary>
    private const int MaxDefenseDicePerItem = 3;

    /// <summary>Ceiling on how many social standings one garment may flatter.</summary>
    private const int MaxAppealsPerItem = 3;

    /// <summary>
    /// Armour is uncapped in code, so this is the tripwire. Natural defence runs 0–5 and attack
    /// pools 3–8; once a section's worst case climbs past this, a hit becomes hard to land at all.
    /// </summary>
    private const int MaxReasonableArmor = 3;

    public static string BuildReport()
    {
        var sb       = new StringBuilder();
        var warnings = new List<string>();
        var items    = ItemRegistry.Instance.All.OrderBy(i => i.ItemId).ToList();

        sb.AppendLine("── Item catalogue audit ──────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine($"  {items.Count} item types discovered.");
        sb.AppendLine();

        warnings.AddRange(CheckIdentity(items, sb));
        warnings.AddRange(CheckCategories(items, sb));
        warnings.AddRange(CheckWearables(items, sb));
        warnings.AddRange(CheckInfoPanel(items, sb));
        warnings.AddRange(CheckLiquids(items, sb));
        warnings.AddRange(CheckTrade(items, sb));
        warnings.AddRange(CheckWeapons(items, sb));
        warnings.AddRange(CheckWeights(items, sb));
        warnings.AddRange(CheckHitLocationDistribution(sb));

        sb.AppendLine();
        if (warnings.Count == 0)
            sb.AppendLine("No warnings — every item is identifiable, reachable and priced.");
        else
        {
            sb.AppendLine($"Warnings ({warnings.Count}):");
            foreach (var w in warnings) sb.AppendLine($"  {w}");
        }

        return sb.ToString();
    }

    // ── Hit locations ─────────────────────────────────────────────────────

    /// <summary>
    /// Proves the armour change did not move the needle on where blows land.
    ///
    /// Armour needs the hit location <em>before</em> the dice, so an unaimed attack now pre-rolls
    /// one. That is only safe if pre-rolling a location and then drawing a wound inside it produces
    /// the same distribution as drawing from the flat pool did — which holds exactly when each body
    /// part is chosen in proportion to its share of that pool. This samples the real
    /// <c>PreRollHitLocation</c> and compares it against the pool shares, so the claim is measured
    /// rather than argued. A drift beyond sampling noise means the buckets and the wound filter
    /// have fallen out of step.
    /// </summary>
    private static List<string> CheckHitLocationDistribution(StringBuilder sb)
    {
        const int Samples = 60_000;
        const double TolerancePct = 1.5;   // comfortably above sampling noise at this N

        var warnings = new List<string>();

        var member  = new Protagonist();
        var fighter = new Fight.Fighter(member, 0, 0, false, Fight.FighterFaction.Enemy);
        var pool    = Fight.FightResolver.BuildAnatomyWoundPool(fighter);

        sb.AppendLine("── Hit locations ─────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine($"  {pool.Count} wounds reachable on a human body; {Samples:N0} samples");
        sb.AppendLine();

        var rng    = new Random(20260729);
        var counts = new Dictionary<string, int>();
        for (int i = 0; i < Samples; i++)
        {
            string key = Fight.FightResolver.PreRollHitLocation(fighter, rng) ?? "(wildcard)";
            counts[key] = counts.GetValueOrDefault(key) + 1;
        }

        sb.AppendLine("  location         sampled   expected   drift");
        foreach (var (key, n) in counts.OrderByDescending(kv => kv.Value))
        {
            double sampled  = 100.0 * n / Samples;
            double expected = 100.0 * ExpectedShare(fighter, pool, key) / pool.Count;
            double drift    = sampled - expected;
            sb.AppendLine($"    {key,-15} {sampled,6:F2}%   {expected,6:F2}%   {drift,+6:F2}");

            if (Math.Abs(drift) > TolerancePct)
                warnings.Add($"hit location '{key}' is drawn {drift:+0.00;-0.00}% away from its share of " +
                             "the wound pool — pre-rolling has changed where blows land");
        }
        sb.AppendLine();

        return warnings;
    }

    /// <summary>How many wounds a location owns: its bucket size, or the wildcard remainder.</summary>
    private static int ExpectedShare(Fight.Fighter fighter, IReadOnlyList<Wound> pool, string key)
    {
        if (key != "(wildcard)")
            return pool.Count(w => WoundBelongsTo(fighter, w, key));

        return pool.Count(w => !fighter.Member.BodyParts.Any(bp => WoundBelongsTo(fighter, w, bp.Id)));
    }

    /// <summary>Whether a wound lives inside a body part — directly, via an organ, or via a part.</summary>
    private static bool WoundBelongsTo(Fight.Fighter fighter, Wound wound, string bodyPartId)
    {
        var bodyPart = fighter.Member.GetBodyPartById(bodyPartId);
        if (bodyPart == null) return false;
        if (wound.AffectsBodyPart(bodyPartId)) return true;

        foreach (var organ in bodyPart.Organs)
        {
            if (wound.AffectsOrgan(organ.Id, bodyPartId)) return true;
            foreach (var part in organ.Parts)
                if (wound.AffectsOrganPart(part.Id, organ.Id, bodyPartId)) return true;
        }
        return false;
    }

    /// <summary>Packs short ids into fixed-width columns so long lists stay readable.</summary>
    private static IEnumerable<string> Columnise(IEnumerable<string> values, int perLine = 4, int width = 24)
    {
        var list = values.OrderBy(v => v).ToList();
        for (int i = 0; i < list.Count; i += perLine)
            yield return string.Concat(list.Skip(i).Take(perLine).Select(v => v.PadRight(width))).TrimEnd();
    }

    // ── Identity ──────────────────────────────────────────────────────────

    /// <summary>
    /// Ids and display names must both be unique. Ids key the synthetic modus mentis and the
    /// liquid-identity test; display names are the only thing the player and the trade UI see,
    /// and trade already matches stock by <c>GetType()</c> rather than by name.
    /// </summary>
    private static List<string> CheckIdentity(IReadOnlyList<Item> items, StringBuilder sb)
    {
        var warnings = new List<string>();

        sb.AppendLine("── Identity ──────────────────────────────────────────────────────────");
        sb.AppendLine();

        var dupIds = items.GroupBy(i => i.ItemId).Where(g => g.Count() > 1).ToList();
        var dupNames = items.GroupBy(i => i.DisplayName).Where(g => g.Count() > 1).ToList();

        sb.AppendLine($"  duplicate ids   : {dupIds.Count}");
        sb.AppendLine($"  duplicate names : {dupNames.Count}");
        sb.AppendLine();

        foreach (var g in dupIds)
            warnings.Add($"duplicate ItemId '{g.Key}' on {string.Join(", ", g.Select(i => i.GetType().Name))}");
        foreach (var g in dupNames)
            warnings.Add($"duplicate DisplayName '{g.Key}' on {string.Join(", ", g.Select(i => i.GetType().Name))}");

        return warnings;
    }

    // ── Taxonomy ──────────────────────────────────────────────────────────

    /// <summary>
    /// Every category and subcategory should have enough items that the player meets variety
    /// rather than the same three objects. Weapons are exempt: two per fighting medium is
    /// deliberate, since each additional one also needs skill wiring to be worth anything.
    /// </summary>
    private static List<string> CheckCategories(IReadOnlyList<Item> items, StringBuilder sb)
    {
        var warnings = new List<string>();

        sb.AppendLine("── Categories ────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine("  category       sub                items");

        foreach (ItemCategory cat in Enum.GetValues<ItemCategory>())
        {
            var inCat = items.Where(i => i.Category == cat).ToList();
            sb.AppendLine($"    {cat,-13}{"",-19}{inCat.Count,4}");

            var subs = inCat.GroupBy(i => i.SubcategoryKey)
                            .Where(g => g.Key.Length > 0)
                            .OrderBy(g => g.Key);
            foreach (var sub in subs)
            {
                bool thin = sub.Count() < MinItemsPerCategory && cat != ItemCategory.Weapon;
                sb.AppendLine($"    {"",-13}{sub.Key,-19}{sub.Count(),4}{(thin ? "   ← thin" : "")}");
                if (thin)
                    warnings.Add($"{cat}/{sub.Key} has only {sub.Count()} item(s) — the player sees little variety");
            }

            // A wearable slot with no items at all never shows up in the grouping above.
            if (cat == ItemCategory.Wearing)
                foreach (var slot in WearSlotExtensions.All)
                    if (!inCat.Any(i => i.SubcategoryKey == slot.ToString()))
                        warnings.Add($"Wearing/{slot} has no items at all — that anchor can never be filled");
        }
        sb.AppendLine();

        // "Other" is the honest answer for a keepsake, but it is also where an uncategorised item
        // silently lands. Name them so the difference stays a decision rather than an oversight.
        var uncategorised = items.Where(i => i.Category == ItemCategory.Other).ToList();
        if (uncategorised.Count > 0)
        {
            sb.AppendLine($"  Uncategorised ({uncategorised.Count}) — confirm each is genuinely nothing but a keepsake:");
            foreach (var line in Columnise(uncategorised.Select(i => i.ItemId)))
                sb.AppendLine($"    {line}");
            sb.AppendLine();
        }

        return warnings;
    }

    /// <summary>
    /// A garment that neither protects, nor flatters, nor holds anything is dead weight: it
    /// occupies an anchor and gives nothing back. Also reports the maximum armour a single body
    /// section could reach, which is the only guard against runaway defence — there is
    /// deliberately no cap in code, so this number is what keeps the content honest.
    /// </summary>
    private static List<string> CheckWearables(IReadOnlyList<Item> items, StringBuilder sb)
    {
        var warnings  = new List<string>();
        var wearables = items.OfType<WearableItem>().ToList();

        sb.AppendLine("── Wearables ─────────────────────────────────────────────────────────");
        sb.AppendLine();

        // Worst case per section: every slot feeding it contributes its best-armoured garment.
        sb.AppendLine("  body section    max armour dice");
        foreach (var section in ArmorSections.AllSections)
        {
            int max = ArmorSections.SlotsFor(section)
                .Sum(slot => wearables.Where(w => w.Slot == slot)
                                      .Select(w => w.DefenseDice)
                                      .DefaultIfEmpty(0)
                                      .Max());
            sb.AppendLine($"    {section,-15} {max,3}{(max > MaxReasonableArmor ? "   ← heavy" : "")}");
            if (max > MaxReasonableArmor)
                warnings.Add($"body section '{section}' can reach {max} armour dice — " +
                             $"above {MaxReasonableArmor} an attacker struggles to land anything");
        }
        sb.AppendLine();

        sb.AppendLine("  social standing  garments");
        foreach (SocialCategory social in Enum.GetValues<SocialCategory>())
            sb.AppendLine($"    {social,-16} {wearables.Count(w => w.DialogueAppeal.Contains(social)),3}");
        sb.AppendLine();

        foreach (var w in wearables.Where(w => !w.HasFunction))
            warnings.Add($"wearable '{w.ItemId}' does nothing — no armour, no social appeal, holds nothing");

        // Every garment should read as *something* to someone; SocialCategory.Pauper exists so the
        // humblest still do. A silent one shows an Impresses list of nothing but empty boxes.
        foreach (var w in wearables.Where(w => w.DialogueAppeal.Count == 0))
            warnings.Add($"wearable '{w.ItemId}' impresses nobody — give it a standing " +
                         "(Pauper covers plain or makeshift dress)");

        foreach (var w in wearables.Where(w => w.DefenseDice is < 0 or > MaxDefenseDicePerItem))
            warnings.Add($"wearable '{w.ItemId}' has DefenseDice {w.DefenseDice}, outside 0–{MaxDefenseDicePerItem}");

        foreach (var w in wearables.Where(w => w.DialogueAppeal.Count > MaxAppealsPerItem))
            warnings.Add($"wearable '{w.ItemId}' appeals to {w.DialogueAppeal.Count} standings, above {MaxAppealsPerItem}");

        foreach (var w in wearables.Where(w => w.DialogueAppeal.Distinct().Count() != w.DialogueAppeal.Count))
            warnings.Add($"wearable '{w.ItemId}' lists the same social standing twice — the duplicate grants nothing");

        return warnings;
    }

    /// <summary>
    /// The inventory panel prints <see cref="Item.Description"/> itself and then every line of
    /// <see cref="Item.Info"/>. An <c>Info</c> line equal to the description is therefore printed
    /// twice — which is exactly what the old <c>Info =&gt; new[] { Description }</c> default did.
    /// </summary>
    private static List<string> CheckInfoPanel(IReadOnlyList<Item> items, StringBuilder sb)
    {
        var warnings = new List<string>();

        var echoes = items
            .Where(i => i.Info.Any(l => string.Equals(l?.Trim(), i.Description?.Trim(),
                                                      StringComparison.OrdinalIgnoreCase)))
            .ToList();

        sb.AppendLine("── Info panel ────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine($"  items overriding Info      : {items.Count(i => i.Info.Length > 0)}");
        sb.AppendLine($"  Info repeating Description : {echoes.Count}");
        sb.AppendLine();

        foreach (var i in echoes)
            warnings.Add($"{i.GetType().Name} ('{i.ItemId}') repeats its description in Info — it will print twice");

        return warnings;
    }

    // ── Reachability ──────────────────────────────────────────────────────

    /// <summary>
    /// Every liquid must be accepted by at least one registered container, or it can never be
    /// picked up at all. Reported both ways round, because a vessel that accepts nothing is just
    /// as broken as a liquid nothing accepts.
    /// </summary>
    private static List<string> CheckLiquids(IReadOnlyList<Item> items, StringBuilder sb)
    {
        var warnings   = new List<string>();
        var liquids    = items.Where(IsLiquid).ToList();
        var containers = items.OfType<IContainer>().ToList();
        var vessels    = containers.Where(c => c.Kind == ContainerKind.Vessel).ToList();
        var storage    = containers.Where(c => c.Kind == ContainerKind.Storage).ToList();

        sb.AppendLine("── Liquids and vessels ───────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine($"  liquids : {liquids.Count}");
        sb.AppendLine($"  vessels : {vessels.Count}");
        sb.AppendLine($"  storage : {storage.Count}");
        sb.AppendLine();

        // Strict acquisition means a liquid no vessel accepts can never be picked up at all.
        foreach (var liquid in liquids.Where(l => !vessels.Any(v => v.CanContain(l))))
            warnings.Add($"liquid '{liquid.ItemId}' fits in no vessel — it can never be picked up");

        if (liquids.Count > 0 && vessels.Count == 0)
            warnings.Add("no vessels exist at all — every drink in the game is unobtainable");

        foreach (var c in containers.Where(c => !items.Any(c.CanContain)))
            warnings.Add($"container '{((Item)c).ItemId}' accepts no known item");

        return warnings;
    }

    /// <summary>Whether an item is stored as a liquid — inferred from its category, never declared.</summary>
    private static bool IsLiquid(Item item) => item.IsLiquid;

    // ── Trade ─────────────────────────────────────────────────────────────

    /// <summary>
    /// An untagged item is invisible to every catalogue. A tag with fewer than three items makes
    /// <c>NpcTradeCatalog.Build</c> fall into its "take all when scarce" branch, so every NPC with
    /// that tag offers an identical, unvarying shelf.
    /// </summary>
    private static List<string> CheckTrade(IReadOnlyList<Item> items, StringBuilder sb)
    {
        var warnings = new List<string>();

        sb.AppendLine("── Trade ─────────────────────────────────────────────────────────────");
        sb.AppendLine();

        var untagged = items.Where(i => i.Tags.Count == 0).ToList();
        sb.AppendLine($"  untagged (never traded) : {untagged.Count}");
        sb.AppendLine();

        // Goods are priced in copper, all of them. Denominations never convert, so a single item
        // quoted in silver would sit in a shop the copper purse cannot reach — and silver and gold
        // are reserved for wages and larger dealings, not for things on a shelf.
        sb.AppendLine("  coin      items   price range");
        foreach (CoinType coin in Enum.GetValues<CoinType>())
        {
            var priced = items.Where(i => i.PriceCoin == coin && i.Tags.Count > 0).ToList();
            string range = priced.Count == 0
                ? "—"
                : $"{priced.Min(i => i.PriceReference)}–{priced.Max(i => i.PriceReference)}";
            string note = coin == CoinType.Copper ? "" : "   (reserved for wages)";
            sb.AppendLine($"    {coin,-8} {priced.Count,4}   {range}{note}");

            if (coin != CoinType.Copper && priced.Count > 0)
                warnings.Add($"{priced.Count} tradeable item(s) are priced in {coin} — goods are " +
                             $"copper-only, and no denomination converts, so these are unbuyable " +
                             $"with a copper purse (e.g. {string.Join(", ", priced.Take(3).Select(i => i.ItemId))})");
        }
        sb.AppendLine();

        // A bundle sums the liquid's price and its vessel's, but charges the whole thing in the
        // liquid's coin. If the two disagree the total is meaningless — 5 copper of ale in a
        // 2 silver bottle would be billed as 7 copper.
        var vessels = items.Where(i => i is IContainer { Kind: ContainerKind.Vessel }).ToList();
        foreach (var liquid in items.Where(i => i.IsLiquid))
            foreach (var vessel in vessels.Where(v => ((IContainer)v).CanContain(liquid)))
                if (vessel.PriceCoin != liquid.PriceCoin)
                    warnings.Add($"bundle '{vessel.ItemId}' + '{liquid.ItemId}' mixes " +
                                 $"{vessel.PriceCoin} and {liquid.PriceCoin} — the summed price is nonsense");
        sb.AppendLine("  tag             items");
        foreach (ItemTag tag in Enum.GetValues<ItemTag>())
        {
            int n = items.Count(i => i.Tags.Contains(tag));
            sb.AppendLine($"    {tag,-13} {n,4}{(n < MinItemsPerTag ? "   ← thin" : "")}");
            if (n < MinItemsPerTag)
                warnings.Add($"tag '{tag}' has only {n} item(s) — every catalogue using it is identical");
        }
        sb.AppendLine();

        if (untagged.Count > 0)
            warnings.Add($"{untagged.Count} item(s) carry no trade tag and can never be bought or sold " +
                         $"(e.g. {string.Join(", ", untagged.Take(5).Select(i => i.ItemId))})");

        // Debug items are real registry entries: they reach shops and duplicate real items' names.
        var debug = items.Where(i => i.ItemId.StartsWith("debug_") || i.ItemId.StartsWith("fight_")).ToList();
        if (debug.Count > 0)
            warnings.Add($"{debug.Count} debug item(s) are in the live registry and can reach trade catalogues " +
                         $"({string.Join(", ", debug.Take(4).Select(i => i.ItemId))}…)");

        return warnings;
    }

    // ── Weapons ───────────────────────────────────────────────────────────

    /// <summary>
    /// A weapon whose category matches no <see cref="WeaponMediumRegistry"/> entry grants no
    /// fighting skills — it is a stick with a name. Also reports per-category counts, since a
    /// category with one weapon gives the player no choice within that medium.
    /// </summary>
    private static List<string> CheckWeapons(IReadOnlyList<Item> items, StringBuilder sb)
    {
        var warnings = new List<string>();
        var weapons  = items.OfType<IWeaponItem>().ToList();
        var known    = WeaponMediumRegistry.GetAll().Select(c => c.CategoryId).ToHashSet();

        sb.AppendLine("── Weapons ───────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine($"  weapons : {weapons.Count}");
        sb.AppendLine();
        sb.AppendLine("  category        items");
        foreach (var cat in WeaponMediumRegistry.GetAll())
            sb.AppendLine($"    {cat.CategoryId,-13} {weapons.Count(w => w.WeaponCategory == cat.CategoryId),4}");
        sb.AppendLine();

        foreach (var w in weapons.Where(w => !known.Contains(w.WeaponCategory)))
            warnings.Add($"weapon '{((Item)w).ItemId}' has category '{w.WeaponCategory}', " +
                         "which matches no weapon medium — it unlocks no skills");

        return warnings;
    }

    // ── Weight ────────────────────────────────────────────────────────────

    /// <summary>
    /// Weight only matters once it gates acquisition, so this reports the distribution and, more
    /// usefully, what the distribution <em>means</em>: how much of a starting character's capacity
    /// a typical item costs. A catalogue where everything is Heavy is as broken as one where
    /// everything is Insignificant, and neither shows up as an error anywhere else.
    /// </summary>
    private static List<string> CheckWeights(IReadOnlyList<Item> items, StringBuilder sb)
    {
        var warnings = new List<string>();

        sb.AppendLine("── Weight ────────────────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine("  class            items");
        foreach (WeightClass w in Enum.GetValues<WeightClass>())
            sb.AppendLine($"    {w,-15}{items.Count(i => i.Weight == w),4}   ({w.Points()} pt)");
        sb.AppendLine();

        // What the tiers mean in practice. Weight never blocks a pickup — it blocks *travel*, once
        // the total exceeds what the backbone can bear — so the meaningful question is how much a
        // character can carry and still leave town. This walks the real stat rather than assuming
        // a number: if the common case cannot carry a working kit, the tiers are wrong however
        // sensible they look in isolation.
        // Sampled across the backbone's real 0–4 range, not an assumed one — reading the maximum
        // off the anatomy is how the mis-scaled ladder that capped everyone at 50 wt was caught.
        var stat        = new MaxWeightStat();
        var probe       = new Protagonist();
        int backboneMax = probe.GetOrganById("backbone")?.MaxScore ?? 4;

        sb.AppendLine($"  backbone  capacity   can travel carrying     (max score {backboneMax})");
        for (int score = 0; score <= backboneMax; score++)
        {
            int cap = stat.PreviewValue(score);
            sb.AppendLine($"    {score,5}   {cap,6} wt   " +
                          $"{cap / Math.Max(1, WeightClass.Light.Points()),3} light  ·  " +
                          $"{cap / Math.Max(1, WeightClass.Medium.Points()),2} medium  ·  " +
                          $"{cap / Math.Max(1, WeightClass.Heavy.Points()),2} heavy");
        }
        sb.AppendLine();

        // A single item heavier than the weakest capacity grounds that character on its own: they
        // can pick it up, but cannot then go anywhere until they put it back down.
        int weakest = stat.PreviewValue(1);
        var groundingItems = items.Where(i => i.WeightPoints > weakest).ToList();
        if (groundingItems.Count > 0)
            warnings.Add($"{groundingItems.Count} item(s) weigh more than the weakest back can travel with " +
                         $"({weakest} wt), so carrying one alone grounds that character " +
                         $"(e.g. {string.Join(", ", groundingItems.Take(4).Select(i => i.ItemId))})");

        return warnings;
    }
}
