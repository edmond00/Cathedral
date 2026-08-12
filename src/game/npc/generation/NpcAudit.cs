using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc.Archetypes;
using Cathedral.Game.Npc.Traits;

namespace Cathedral.Game.Npc.Generation;

/// <summary>
/// Headless report on NPC generation — the counterpart to <c>--dialogue-audit</c>, run with
/// <c>--npc-audit</c>. It spawns a sample of every speaking archetype and checks the things that are
/// invisible until a player meets someone broken:
///
/// <list type="bullet">
///   <item><b>Determinism.</b> The same NPC id must produce the same person. Every NPC is generated
///     twice and the two are compared organ by organ, skill by skill, item by item.</item>
///   <item><b>Trait content.</b> Every trait's modus-mentis ids and organ-part ids must actually
///     exist — a typo silently grants nothing, and nothing is exactly what a working trait looks
///     like from the outside.</item>
///   <item><b>Pool sizes.</b> 60 global traits and 6 per archetype, with no duplicate ids.</item>
///   <item><b>Shape.</b> Skills placed in memory rather than floating, a sex that agrees with the
///     genitories score, and organ totals inside the band the roll can actually produce.</item>
/// </list>
/// </summary>
public static class NpcAudit
{
    /// <summary>How many distinct individuals of each archetype the sample walks.</summary>
    private const int SampleSize = 12;

    private const int ExpectedGlobalTraits    = 60;
    private const int ExpectedArchetypeTraits = 6;

    public static string BuildReport()
    {
        var sb       = new StringBuilder();
        var warnings = new List<string>();

        sb.AppendLine("── NPC generation audit ──────────────────────────────────────────────");
        sb.AppendLine();

        warnings.AddRange(CheckTraitCatalogue(sb));
        sb.AppendLine();
        warnings.AddRange(CheckSocialCategories(sb));
        sb.AppendLine();
        warnings.AddRange(CheckArchetypes(sb));

        sb.AppendLine();
        if (warnings.Count == 0)
            sb.AppendLine("No warnings — generation is deterministic and every trait resolves.");
        else
        {
            sb.AppendLine($"Warnings ({warnings.Count}):");
            foreach (var w in warnings) sb.AppendLine($"  {w}");
        }

        sb.AppendLine();
        AppendWorkedExample(sb);

        return sb.ToString();
    }

    /// <summary>
    /// One generated individual, printed in full. The table above proves the numbers are in range;
    /// this proves the <i>person</i> hangs together — that the trait that broke his hand is the same
    /// trait the description mentions and the same one the LLM will be told about.
    /// </summary>
    private static void AppendWorkedExample(StringBuilder sb)
    {
        var npc  = new BlacksmithArchetype().Spawn(new Random(3));
        var body = npc.Combatant;

        sb.AppendLine("── A worked example ──────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine($"  {npc.DisplayName} ({npc.Archetype.ArchetypeId}), " +
                      $"{(body.BiologicalSexMale == true ? "male" : "female")}, " +
                      $"{body.GetAgeDays() / LifetimeStat.DaysPerYear:0} years old");
        sb.AppendLine($"  traits    : {string.Join(", ", npc.Text.TraitIds)}");
        sb.AppendLine($"  observed  : {npc.ObservationHint}");
        sb.AppendLine($"  introduces: {npc.SelfIntroduction}");
        sb.AppendLine($"  on work   : {npc.OpinionOn(Dialogue.Tree.DialogueTopic.Work)}");
        sb.AppendLine($"  wounds    : {Describe(body.Wounds.Select(w =>
            $"{w.WoundName} [{(w.CanHeal ? "heals" : w.Handicap == WoundHandicap.High ? "permanent" : "old")}]"))}");
        sb.AppendLine($"  heals in  : {body.WoundHealDurationDays} d (viscera-derived)");
        sb.AppendLine($"  carries   : {Describe(body.GetAllItems().Select(i => i.DisplayName))}");
        sb.AppendLine($"  skills    : {Describe(body.ModiMentis.Select(m => $"{m.ModusMentisId} {m.Level}"))}");

        foreach (var module in body.MemoryModules)
        {
            int used = module.Slots.Count(s => s.ModusMentis != null);
            sb.AppendLine($"  {module.Type,-11}: {used}/{module.ActiveCapacity} slots used");
        }

        // The last paragraph of the persona prompt is what the traits added — the archetype brief
        // above it is the same for every smith.
        string persona = npc.WayToSpeakDescription ?? "";
        int split = persona.LastIndexOf("\n\n", StringComparison.Ordinal);
        sb.AppendLine($"  told LLM  : …{(split >= 0 ? persona[(split + 2)..] : persona)}");
    }

    private static string Describe(IEnumerable<string> values)
    {
        var list = values.ToList();
        return list.Count == 0 ? "(none)" : string.Join(", ", list);
    }

    // ── Trait catalogue ────────────────────────────────────────────────────────

    private static IEnumerable<string> CheckTraitCatalogue(StringBuilder sb)
    {
        var registry = PersonalityTraitRegistry.Instance;
        var warnings = new List<string>();

        var archetypeIds = registry.ArchetypeIds.OrderBy(id => id).ToList();
        int archetypeTotal = archetypeIds.Sum(id => registry.ForArchetype(id).Count);

        sb.AppendLine($"Traits: {registry.Global.Count} global + {archetypeTotal} archetype " +
                      $"across {archetypeIds.Count} trades = {registry.Global.Count + archetypeTotal} total");
        sb.AppendLine($"Each NPC is dealt {PersonalityTraitRegistry.ArchetypeDraw} archetype trait " +
                      $"and {PersonalityTraitRegistry.GlobalDraw} global.");

        if (registry.Global.Count != ExpectedGlobalTraits)
            warnings.Add($"global pool has {registry.Global.Count} traits, expected {ExpectedGlobalTraits}");

        foreach (var id in archetypeIds)
        {
            int count = registry.ForArchetype(id).Count;
            if (count != ExpectedArchetypeTraits)
                warnings.Add($"archetype '{id}' has {count} traits, expected {ExpectedArchetypeTraits}");
        }

        // Every id a trait names must resolve. A bad modus-mentis id grants nothing and a bad organ
        // id adjusts nothing — both fail silently at runtime, which is the worst way to fail.
        var all = registry.Global
            .Concat(archetypeIds.SelectMany(registry.ForArchetype))
            .ToList();

        var anatomy = new Protagonist();   // any human body will do as a name check
        foreach (var trait in all)
        {
            foreach (var mmId in trait.ModiMentis)
                if (ModusMentisRegistry.Instance.GetModusMentis(mmId) == null)
                    warnings.Add($"trait '{trait.TraitId}' grants unknown modus mentis '{mmId}'");

            foreach (var (organPartId, _) in trait.Organs)
                if (anatomy.GetOrganPartById(organPartId) == null)
                    warnings.Add($"trait '{trait.TraitId}' adjusts unknown organ part '{organPartId}'");
        }

        // An archetype's own traits must name skills that archetype's body can hold. A shepherd trait
        // granting a beast's wind-reading, or a wolf trait granting a lettered one, teaches nothing at
        // all — the grant is refused at generation and the character is quietly one skill short.
        // Global traits are exempt: they are dealt to every anatomy, so some of what they offer is
        // always going to miss.
        foreach (var archetypeId in archetypeIds)
        {
            var species = ArchetypeAnatomy(archetypeId);
            if (species == null) continue;

            foreach (var trait in registry.ForArchetype(archetypeId))
                foreach (var mmId in trait.ModiMentis)
                {
                    var mm = ModusMentisRegistry.Instance.GetModusMentis(mmId);
                    if (mm != null && !ModusMentisAnatomy.IsLearnableBy(mm, species.Value))
                        warnings.Add($"trait '{trait.TraitId}' ({archetypeId}, {species}) grants "
                                     + $"'{mmId}', which that anatomy cannot learn");
                }
        }

        return warnings;
    }

    // ── Per-archetype sample ───────────────────────────────────────────────────

    private static IEnumerable<string> CheckArchetypes(StringBuilder sb)
    {
        var warnings = new List<string>();

        sb.AppendLine($"{"archetype",-18}{"organs",9}{"skills",8}{"lvl",6}{"items",7}{"wounds",8}{"repeatable",12}");
        sb.AppendLine(new string('─', 68));

        foreach (var archetype in SpeakingArchetypes())
        {
            var organTotals = new List<int>();
            var skillCounts = new List<int>();
            var levels      = new List<int>();
            var itemCounts  = new List<int>();
            var woundCounts = new List<int>();
            bool repeatable = true;

            for (int i = 0; i < SampleSize; i++)
            {
                // The same scene seed twice: the name RNG, and therefore the stable id, and therefore
                // every content roll, must land in exactly the same place both times.
                var first  = archetype.Spawn(new Random(i));
                var second = archetype.Spawn(new Random(i));

                string difference = FirstDifference(first, second);
                if (difference != null)
                {
                    repeatable = false;
                    warnings.Add($"{archetype.ArchetypeId}: same id produced different NPCs — {difference}");
                }

                var body = first.Combatant;
                organTotals.Add(OrganTotal(body));
                skillCounts.Add(body.ModiMentis.Count);
                levels.AddRange(body.ModiMentis.Select(m => m.Level));
                itemCounts.Add(body.GetAllItems().Count);
                woundCounts.Add(body.Wounds.Count);

                // Two separately generated bodies must never share a wound OBJECT. WoundRegistry
                // hands out one shared template per wound type, and appending those directly is
                // how per-injury state (art position, healing date) used to leak between every
                // character in the process. Reference equality is the only way to catch a
                // regression here — the wounds are supposed to look identical.
                foreach (var w in first.Combatant.Wounds)
                    if (second.Combatant.Wounds.Any(o => ReferenceEquals(o, w)))
                        warnings.Add($"{archetype.ArchetypeId}: two NPCs share one wound instance " +
                                     $"({w.WoundName}) — per-injury state will leak between them");

                // An NPC must be born alive. CurrentHp is MaxHp (the trunk score) minus the wound
                // count, and traits both add wounds and move organ scores, so a low-trunk body dealt
                // two or three wound-bearing traits used to reach zero and arrive dead: still in
                // Scene.Npcs, still scheduled, but filtered out of GetNpcsAt, so every verb refused
                // them and the player met a person they could not touch. NpcContentGenerator now
                // trims historical wounds to prevent it; this is what stops it coming back.
                if (!first.IsAlive)
                    warnings.Add($"{archetype.ArchetypeId}: generated DEAD — trunk {body.MaxHp} vs " +
                                 $"{body.Wounds.Count} wound(s). Nothing in the world can interact with them");

                // Backstory wounds must be historical, or one long work stint heals every scar off
                // every NPC in the world.
                foreach (var w in first.Combatant.Wounds.Where(w => w.InflictedOnDay.HasValue))
                    warnings.Add($"{archetype.ArchetypeId}: trait wound '{w.WoundName}' is dated " +
                                 $"(day {w.InflictedOnDay}) and will heal away — it should be historical");

                warnings.AddRange(CheckIndividual(first));
            }

            sb.AppendLine(
                $"{archetype.ArchetypeId,-18}{Range(organTotals),9}{Range(skillCounts),8}" +
                $"{Range(levels),6}{Range(itemCounts),7}{Range(woundCounts),8}{(repeatable ? "yes" : "NO"),12}");
        }

        return warnings.Distinct();
    }

    /// <summary>Checks one generated individual against the invariants the generator promises.</summary>
    private static IEnumerable<string> CheckIndividual(NpcEntity npc)
    {
        var body = npc.Combatant;
        string who = npc.Archetype.ArchetypeId;

        // Sex and the genitories score must agree — that coupling is the whole point of the design.
        var genitories = body.GetOrganPartById("genitories");
        if (genitories != null && body.BiologicalSexMale is bool male)
        {
            if (male && genitories.Score < 1)
                yield return $"{who}: male with genitories {genitories.Score}";
            if (!male && genitories.Score != 0)
                yield return $"{who}: female with genitories {genitories.Score}";
        }

        // Every skill must be filed somewhere. One floating in ModiMentis but in no module is held by
        // nobody and would never be usable.
        var filed = body.MemoryModules
            .SelectMany(m => m.Slots)
            .Where(s => s.ModusMentis != null)
            .Select(s => s.ModusMentis!)
            .ToHashSet();

        int unfiled = body.ModiMentis.Count(m => !filed.Contains(m));
        if (unfiled > 0)
            yield return $"{who}: {unfiled} skill(s) held but not in any memory module";

        foreach (var mm in body.ModiMentis)
        {
            int cap = body.GetMaxLevelForModusMentis(mm);
            if (mm.Level > cap)
                yield return $"{who}: '{mm.ModusMentisId}' at level {mm.Level} over its cap of {cap}";
            if (mm.Level < 1)
                yield return $"{who}: '{mm.ModusMentisId}' at level {mm.Level}";

            // A skill this body cannot hold — a wolf with rhetoric, someone with a fang skill. It
            // would sit at level 1 for ever (an absent organ contributes +0), so it reads as merely a
            // weak skill rather than as the generation fault it is.
            if (!ModusMentisAnatomy.IsLearnableBy(mm, body))
                yield return $"{who} ({body.AnatomyType}): holds '{mm.ModusMentisId}', which this "
                             + $"anatomy cannot learn (organs [{string.Join(", ", mm.Organs)}]"
                             + (mm.RequiredCapabilities != AnatomyCapability.None
                                 ? $", needs {mm.RequiredCapabilities}" : "") + ")";
        }

        // The observation hint is what the player actually reads; an empty one is a hole in the scene.
        if (string.IsNullOrWhiteSpace(npc.ObservationHint))
            yield return $"{who}: empty observation hint";

        if (npc.CanSpeak && string.IsNullOrWhiteSpace(npc.WayToSpeakDescription))
            yield return $"{who}: speaks but has no persona prompt";

        if (npc.Text.TraitIds.Count != PersonalityTraitRegistry.GlobalDraw + PersonalityTraitRegistry.ArchetypeDraw)
            yield return $"{who}: dealt {npc.Text.TraitIds.Count} traits";
    }

    /// <summary>
    /// The first way two supposedly-identical NPCs differ, or null when they match. Compares what a
    /// player could actually notice: who they are, what their body is, what they know and carry.
    /// </summary>
    private static string? FirstDifference(NpcEntity a, NpcEntity b)
    {
        if (a.DisplayName != b.DisplayName) return $"names '{a.DisplayName}' vs '{b.DisplayName}'";

        if (!a.Text.TraitIds.SequenceEqual(b.Text.TraitIds))
            return $"traits [{string.Join(",", a.Text.TraitIds)}] vs [{string.Join(",", b.Text.TraitIds)}]";

        if (OrganTotal(a.Combatant) != OrganTotal(b.Combatant))
            return $"organ totals {OrganTotal(a.Combatant)} vs {OrganTotal(b.Combatant)}";

        var skillsA = a.Combatant.ModiMentis.Select(m => $"{m.ModusMentisId}:{m.Level}").OrderBy(s => s);
        var skillsB = b.Combatant.ModiMentis.Select(m => $"{m.ModusMentisId}:{m.Level}").OrderBy(s => s);
        if (!skillsA.SequenceEqual(skillsB)) return "skill sets or levels";

        var itemsA = a.Combatant.GetAllItems().Select(i => i.ItemId).OrderBy(s => s);
        var itemsB = b.Combatant.GetAllItems().Select(i => i.ItemId).OrderBy(s => s);
        if (!itemsA.SequenceEqual(itemsB)) return "inventories";

        if (a.ObservationHint != b.ObservationHint) return "observation hints";

        return null;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private static int OrganTotal(PartyMember body) => body.BodyParts
        .SelectMany(bp => bp.Organs)
        .SelectMany(o => o.Parts)
        .Sum(p => p.Score);

    private static string Range(IReadOnlyList<int> values)
        => values.Count == 0 ? "-" : values.Min() == values.Max() ? $"{values.Min()}" : $"{values.Min()}-{values.Max()}";

    /// <summary>
    /// <summary>
    /// Every speaking archetype must declare a <see cref="NamedNpcArchetype.Social"/>, or the
    /// dialogue bonus from worn garments silently does nothing when talking to them — which looks
    /// exactly like the feature not existing.
    ///
    /// Empty standings are reported but are <b>not</b> warnings: Aristocrat, Military and Urban
    /// have garments authored for them and are waiting on archetype families that do not exist yet.
    /// </summary>
    private static List<string> CheckSocialCategories(StringBuilder sb)
    {
        var warnings   = new List<string>();
        var archetypes = SpeakingArchetypes().ToList();

        sb.AppendLine("── Social standing ───────────────────────────────────────────────────");
        sb.AppendLine();
        sb.AppendLine("  standing         archetypes");

        foreach (SocialCategory social in Enum.GetValues<SocialCategory>())
        {
            var members = archetypes.Where(a => a.Social == social).Select(a => a.ArchetypeId).ToList();
            string list = members.Count == 0 ? "(none yet)" : string.Join(", ", members);
            sb.AppendLine($"    {social,-16} {members.Count,2}  {list}");
        }
        sb.AppendLine();

        foreach (var a in archetypes.Where(a => a.Social == null))
            warnings.Add($"archetype '{a.ArchetypeId}' can speak but declares no social standing — " +
                         "worn garments will grant it no dialogue dice");

        return warnings;
    }

    /// <summary>
    /// One instance of every archetype whose NPCs go through the full generation path. Beasts are
    /// deliberately absent: they have no traits, no loadout and no dialogue (see the design note in
    /// <see cref="NpcContentGenerator"/>).
    /// </summary>
    /// <summary>
    /// The anatomy behind an archetype id, or null when no archetype claims it. Discovered by
    /// reflection rather than listed, so beast archetypes (which never appear in
    /// <see cref="SpeakingArchetypes"/>) are covered too — they are exactly the ones whose traits are
    /// most likely to name a skill of the wrong anatomy.
    /// </summary>
    private static AnatomyType? ArchetypeAnatomy(string archetypeId)
    {
        _anatomyByArchetype ??= System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(NamedNpcArchetype).IsAssignableFrom(t)
                        && t.GetConstructor(System.Type.EmptyTypes) != null)
            .Select(t => (NamedNpcArchetype)System.Activator.CreateInstance(t)!)
            .GroupBy(a => a.ArchetypeId)
            .ToDictionary(g => g.Key, g => g.First().Species.AnatomyType);

        return _anatomyByArchetype.TryGetValue(archetypeId, out var anatomy) ? anatomy : null;
    }

    private static Dictionary<string, AnatomyType>? _anatomyByArchetype;

    private static IEnumerable<NamedNpcArchetype> SpeakingArchetypes() => new NamedNpcArchetype[]
    {
        new BlacksmithArchetype(), new BakerArchetype(),   new BrewerArchetype(),
        new CarpenterArchetype(),  new CooperArchetype(),  new MillerArchetype(),
        new WeaverArchetype(),     new ApprenticeArchetype(),
        new ReeveArchetype(),      new HaywardArchetype(), new PlowmanArchetype(),
        new ReaperArchetype(),     new BondmanArchetype(), new ShepherdArchetype(),
        new SwineherdArchetype(),  new DairymaidArchetype(), new PoultryKeeperArchetype(),
        new FarmerArchetype(),     new FarmhandArchetype(),
        new WoodcutterArchetype(), new CharcoalBurnerArchetype(),
        new FishermanArchetype(),  new MinerArchetype(),
        new DruidArchetype(),      new HermitArchetype(), new SavageArchetype(),
    };
}
