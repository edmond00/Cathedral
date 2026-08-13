using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Save;

/// <summary>
/// Checks that nothing on a party type has been left out of the save by accident.
///
/// <para><b>The failure this exists for is silent.</b> Somebody adds a field to
/// <see cref="PartyMember"/>, forgets <see cref="PartyState"/>, and it resets on every load — showing
/// up months later as "my character felt different after I quit". No test fails, because no test knew
/// to look at that field. The round-trip script cannot help either: it compares a capture against
/// itself, so a field that is captured nowhere matches perfectly.</para>
///
/// <para>So this reflects over the party types and demands that every public instance member is
/// classified: either <b>persisted</b> (it has a home in the DTO) or <b>derived</b> (it is recomputed,
/// and here is why). A member in neither list fails the audit by name. Adding a field therefore breaks
/// the build's audit until somebody has decided which it is — which is the whole point.</para>
/// </summary>
public static class SaveAudit
{
    /// <summary>Members carried in <see cref="PartyState"/>, keyed by declaring type.</summary>
    private static readonly Dictionary<Type, string[]> Persisted = new()
    {
        [typeof(PartyMember)] = new[]
        {
            "Species",            // as a species key
            "BiologicalSexMale",
            "BirthTimeDays",
            "DisplayName",
            "BodyParts",          // only the leaf OrganPart.Score values; the tree is rebuilt
            "HumorQueues",
            "MemoryModules",      // slot placement by index
            "ModiMentis",
            "LearnedModiMentis",  // the same list object, re-aliased on load
            "Inventory",
            "EquippedItems",
            "Wounds",
        },
        [typeof(Protagonist)] = new[]
        {
            "CharacterName",
            "CurrentLocationId",
            "JournalEntries",
            "Party",              // the three coin counts
            "ChildhoodHistory",
            "RecordedRoutines",
            "CompanionParty",
        },
        [typeof(EnemyCombatant)] = new[] { "ArchetypeId", "PersistentId", "DisplayName" },
        [typeof(Party)]          = new[] { "Gold", "Silver", "Copper" },
        [typeof(ChildhoodHistory)] = new[] { "Location", "VisitedReminescences", "RememberedFragments" },
        [typeof(WoundInstance)]    = new[] { "Template", "InflictedOnDay", "ArtX", "ArtY", "WildcardZoneHint" },
        [typeof(ModusMentis)]      = new[] { "ModusMentisId", "Level", "CurrentXp" },
    };

    /// <summary>
    /// Members deliberately NOT stored, with the reason. Every one of these is recomputed from
    /// something that IS stored, so persisting it would let a save contradict the body it belongs to.
    /// </summary>
    private static readonly Dictionary<Type, Dictionary<string, string>> Derived = new()
    {
        [typeof(PartyMember)] = new()
        {
            ["DerivedStats"]   = "formula objects, rediscovered by reflection from the anatomy",
            ["AnatomyType"]    = "the species says which",
            ["Capabilities"]   = "declared by the anatomy",
            ["AffinityKey"]    = "the display name, or a constant for the protagonist",
            ["MaxHp"]          = "from the trunk score",
            ["CurrentHp"]      = "MaxHp minus the wound count",
            ["MaxNoeticPoints"]= "from the encephalon score",
            ["MaxCarryWeight"] = "from the backbone score",
            ["CurrentWeight"]  = "summed from what is carried",
            ["RemainingWeight"]= "capacity less current",
            ["ExcessWeight"]   = "current less capacity",
            ["IsOverloaded"]   = "current against capacity",
            ["WoundHealDurationDays"] = "from the viscera score",
            ["BeautyDice"]     = "from the visage score",
            ["MaxBeautyDice"]  = "from the visage ceiling",
            ["PartyDescription"] = "display text",
        },
        [typeof(Protagonist)] = new()
        {
            ["DisplayName"]         = "the stored character name",
            ["AffinityKey"]         = "a fixed constant, so a rename never re-keys NPC affinity",
            ["EveryMember"]         = "the protagonist plus the companions",
            ["OverloadedMembers"]   = "computed per member",
            ["TravelWeightBlocker"] = "computed per member",
        },
        [typeof(EnemyCombatant)] = new() { },
        [typeof(Party)] = new() { },
        [typeof(ChildhoodHistory)] = new()
        {
            ["IsEmpty"] = "whether anything was recorded",
        },
        [typeof(WoundInstance)] = new()
        {
            ["CanHeal"]  = "from the handicap and the infliction day",
            ["DaysOld"]  = "the clock less the infliction day",
            // Passthrough to the shared, immutable catalogue entry the stored template key names.
            ["Description"] = "from the template",
            ["Handicap"]    = "from the template",
            ["TargetId"]    = "from the template",
            ["TargetKind"]  = "from the template",
            ["WoundId"]     = "from the template",
            ["WoundName"]   = "from the template",
        },
        [typeof(ModusMentis)] = new()
        {
            ["DisplayName"]     = "content, keyed by id",
            ["SkillMeans"]      = "content",
            ["MenuDescription"] = "content",
            ["Functions"]       = "content",
            ["Organs"]          = "content",
            ["RequiredCapabilities"] = "content",
            ["MemoryType"]      = "content",
            ["MoralLevel"]      = "content",
            ["ActsDiscretely"]  = "content",
            ["PersonaPrompt"]   = "content",
            ["PersonaTone"]     = "content",
            ["PersonaReminder"] = "content",
            ["PersonaReminder2"]= "content",
            ["StyleInstruction"]= "content",
        },
    };

    public static void Run()
    {
        Console.WriteLine("=== SAVE COVERAGE AUDIT ===\n");
        int problems = 0;

        foreach (var type in Persisted.Keys)
            problems += AuditType(type);

        problems += AuditItemState();

        Console.WriteLine(problems == 0
            ? "\nOK — every party member is either persisted or explained."
            : $"\n{problems} problem(s). Classify each in SaveAudit, and give it a home in PartyState if it is state.");
        Environment.ExitCode = problems == 0 ? 0 : 1;
    }

    private static int AuditType(Type type)
    {
        var persisted = Persisted[type].ToHashSet(StringComparer.Ordinal);
        var derived   = Derived.TryGetValue(type, out var d) ? d : new Dictionary<string, string>();

        // Declared-only: an inherited member is audited against the type that declares it, so nothing
        // is judged twice and a subclass's list stays about that subclass.
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var members = type.GetProperties(Flags).Where(p => p.GetIndexParameters().Length == 0)
                          .Select(p => p.Name)
                          .Concat(type.GetFields(Flags).Select(f => f.Name))
                          .Distinct()
                          .OrderBy(n => n, StringComparer.Ordinal)
                          .ToList();

        var unclassified = members.Where(m => !persisted.Contains(m) && !derived.ContainsKey(m)).ToList();
        var stale        = persisted.Concat(derived.Keys).Where(m => !members.Contains(m)).ToList();

        Console.WriteLine($"── {type.Name}: {members.Count} public member(s), "
                        + $"{persisted.Count} persisted, {derived.Count} derived");

        foreach (var m in unclassified)
            Console.WriteLine($"   ! UNCLASSIFIED  {type.Name}.{m} — persist it, or record why it is derived");
        foreach (var m in stale)
            Console.WriteLine($"   ! STALE ENTRY   {type.Name}.{m} is listed here but no longer exists");

        return unclassified.Count + stale.Count;
    }

    /// <summary>
    /// Items are the one family too large to list. They are audited by the property that makes them
    /// cheap to save instead: an item is its id and nothing else, bar two known carriers of
    /// per-instance state. A new mutable field on any item subclass breaks that assumption silently,
    /// so this looks for one.
    /// </summary>
    private static int AuditItemState()
    {
        const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic
                                 | BindingFlags.Instance | BindingFlags.DeclaredOnly;

        var known = new HashSet<string>(StringComparer.Ordinal)
        {
            "ConsumableItem._composition",  // captured; drawn once from a run-long stream
            "ContainerItem.<Contents>k__BackingField",
            "WearableContainerItem.<Contents>k__BackingField",
        };

        int problems = 0;
        int scanned  = 0;
        foreach (var type in Assembly.GetExecutingAssembly().GetTypes()
                     .Where(t => typeof(Item).IsAssignableFrom(t) && !t.IsInterface)
                     .OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            scanned++;
            foreach (var f in type.GetFields(Flags).Where(f => !f.IsInitOnly && !f.IsLiteral))
            {
                string key = $"{type.Name}.{f.Name}";
                if (known.Contains(key)) continue;
                Console.WriteLine($"   ! ITEM STATE    {key} is mutable per-instance state — "
                                + "an item is saved as its id alone, so this would be lost on load");
                problems++;
            }
        }

        Console.WriteLine($"── Items: {scanned} type(s) scanned, {known.Count} known state carrier(s)");
        return problems;
    }
}
