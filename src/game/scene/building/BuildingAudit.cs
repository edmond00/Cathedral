using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene.Farm;
using Cathedral.Game.Scene.Field;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Scene.Village;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// Headless audit of the inhabited locations — village, farm, field — run over a sample of location
/// ids. Reports the shape of what generation produced, then warns about the failures that are
/// invisible in play because nothing throws when they happen:
///
/// <list type="bullet">
/// <item>an area in two sections or in none (the second crashes the fight path);</item>
/// <item>two areas sharing a node-id slug, which costs one of them its narration node entirely;</item>
/// <item>two PoIs in one area sharing a display name, so only one is ever observable;</item>
/// <item>an area-graph edge duplicating a door, which lets movement walk straight past the lock;</item>
/// <item>an NPC with nowhere to sleep, or a bed shared by two people;</item>
/// <item>a public hall standing empty during a day period — a shop nobody minds;</item>
/// <item>stable keys that are missing, colliding, or different across two builds of one location.</item>
/// </list>
///
/// Run after touching <see cref="BuildingFactory"/>, any scene factory, or the schedule system.
/// </summary>
public static class BuildingAudit
{
    private const int SampleSize = 60;

    public static string BuildReport()
    {
        var sb       = new StringBuilder();
        var warnings = new List<string>();

        sb.AppendLine("=== BUILDING AUDIT ===");
        sb.AppendLine($"Sampling {SampleSize} location ids per factory.");
        sb.AppendLine();

        AuditFactory(sb, warnings, "VILLAGE", id => new VillageSceneFactory().Build(id));
        AuditFactory(sb, warnings, "FARM",    id => new FarmSceneFactory().Build(id),  allowedEmptyPeriods: 1);
        AuditFactory(sb, warnings, "FIELD",   id => new FieldSceneFactory().Build(id), allowedEmptyPeriods: 1);

        // The wilderness has no buildings, but the checks that are not about buildings — section
        // partition, node-id slugs, duplicate observation names, doors bypassed by graph edges — apply
        // to every scene, and these factories share the same path and terrain builders.
        AuditFactory(sb, warnings, "PLAIN",    id => new Plain.PlainSceneFactory().Build(id),       inhabited: false);
        AuditFactory(sb, warnings, "FOREST",   id => new Forest.ForestSceneFactory().Build(id),     inhabited: false);
        AuditFactory(sb, warnings, "CAVE",     id => new Cave.CaveSceneFactory().Build(id),         inhabited: false);
        AuditFactory(sb, warnings, "COAST",    id => new Coast.CoastSceneFactory().Build(id),       inhabited: false);
        AuditFactory(sb, warnings, "MOUNTAIN", id => new Mountain.MountainSceneFactory().Build(id), inhabited: false);
        AuditFactory(sb, warnings, "PEAK",     id => new Peak.PeakSceneFactory().Build(id),         inhabited: false);

        AuditVerbDiscovery(warnings);
        AuditDoorProse(sb, warnings);

        sb.AppendLine();
        if (warnings.Count == 0)
        {
            sb.AppendLine("No warnings — every building partitions cleanly, every worker has a bed,");
            sb.AppendLine("and every public hall is manned through the day.");
            return sb.ToString();
        }

        // Grouped by kind rather than listed flat: one systemic fault across 60 sampled locations
        // produces hundreds of lines and buries every other kind of warning under itself.
        sb.AppendLine($"--- {warnings.Count} WARNING(S), BY KIND ---");
        foreach (var group in warnings.GroupBy(Kind).OrderByDescending(g => g.Count()))
        {
            sb.AppendLine($"  [{group.Count(),4}] {group.Key}");
            foreach (var example in group.Distinct().Take(3))
                sb.AppendLine($"         e.g. {example}");
        }

        return sb.ToString();
    }

    /// <summary>Strips ids and quoted names so the same fault at 60 locations counts as one kind.</summary>
    private static string Kind(string warning)
    {
        var body = System.Text.RegularExpressions.Regex.Replace(warning, @"^(VILLAGE|FARM|FIELD) \d+: ", "");
        body     = System.Text.RegularExpressions.Regex.Replace(body, @"'[^']*'", "'…'");
        return System.Text.RegularExpressions.Regex.Replace(body, @"\b\d+\b", "N");
    }

    // ── Per-factory sweep ─────────────────────────────────────────────────────

    /// <param name="inhabited">
    /// True for the location types whose people live in buildings. Wilderness NPCs — a wolf, a hermit,
    /// a woodcutter at his clearing — are deliberately allowed to sleep rough, so the bed and
    /// hall-staffing rules are not applied to them.
    /// </param>
    /// <param name="allowedEmptyPeriods">
    /// How many daylight periods a public hall may stand empty. Zero for a village, whose halls are
    /// shop counters; one for a farm or field, where the hall is a household and its master is
    /// entitled to be out for a period without anyone covering.
    /// </param>
    private static void AuditFactory(
        StringBuilder sb, List<string> warnings, string label, Func<int, Scene> build,
        bool inhabited = true, int allowedEmptyPeriods = 0)
    {
        int minAreas = int.MaxValue, maxAreas = 0;
        int minNpcs  = int.MaxValue, maxNpcs  = 0;
        int minDoors = int.MaxValue, maxDoors = 0;
        int minLocked = int.MaxValue, maxLocked = 0;
        var kinds = new Dictionary<string, int>();

        for (int id = 1; id <= SampleSize; id++)
        {
            Scene scene;
            try
            {
                scene = build(id);
            }
            catch (Exception ex)
            {
                warnings.Add($"{label} location {id}: generation threw — {ex.GetType().Name}: {ex.Message}");
                continue;
            }

            var areas = scene.AllAreas;
            var doors = AllDoors(scene);

            minAreas = Math.Min(minAreas, areas.Count); maxAreas = Math.Max(maxAreas, areas.Count);
            minNpcs  = Math.Min(minNpcs, scene.Npcs.Count); maxNpcs = Math.Max(maxNpcs, scene.Npcs.Count);
            minDoors = Math.Min(minDoors, doors.Count); maxDoors = Math.Max(maxDoors, doors.Count);

            int locked = doors.Count(d => d.EffectiveState(TimePeriod.Noon) == DoorState.Locked);
            minLocked = Math.Min(minLocked, locked); maxLocked = Math.Max(maxLocked, locked);

            foreach (var section in scene.Sections)
                kinds[section.DisplayName] = kinds.GetValueOrDefault(section.DisplayName) + 1;

            CheckPartition(warnings, label, id, scene, areas);
            CheckNodeIdUniqueness(warnings, label, id, areas);
            CheckObservationNames(warnings, label, id, areas);
            CheckConnectorGraphExclusivity(warnings, label, id, scene);
            CheckLockRules(warnings, label, id, doors);
            CheckStableKeys(warnings, label, id, scene);
            CheckNoStutter(warnings, label, id, scene);
            if (inhabited)
            {
                CheckBeds(warnings, label, id, scene);
                CheckHallStaffing(warnings, label, id, scene, allowedEmptyPeriods);
                CheckMastersLeaveTheirHall(warnings, label, id, scene);
            }
        }

        // Determinism: the same location id must rebuild identically, or a door the player memorised
        // last visit looks like a different door this visit.
        CheckRebuildStability(warnings, label, build);

        sb.AppendLine($"--- {label} ---");
        sb.AppendLine($"  areas          {minAreas}-{maxAreas}");
        sb.AppendLine($"  named+shallow  {minNpcs}-{maxNpcs} NPCs");
        sb.AppendLine($"  doors          {minDoors}-{maxDoors}  (locked at noon: {minLocked}-{maxLocked})");
        sb.AppendLine($"  sections seen  {string.Join(", ", kinds.OrderByDescending(k => k.Value).Take(12).Select(k => $"{k.Key}×{k.Value}"))}");
        sb.AppendLine();
    }

    private static List<DoorPointOfInterest> AllDoors(Scene scene)
        => scene.AllAreas
            .SelectMany(a => a.PointsOfInterest)
            .OfType<DoorPointOfInterest>()
            .Distinct()
            .ToList();

    // ── Checks ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Sections must partition areas. An area in none makes <c>StartFight</c> bail out with an error;
    /// an area in two resolves arbitrarily, so ownership and arena flavour become a coin toss.
    /// </summary>
    private static void CheckPartition(List<string> w, string label, int id, Scene scene, List<Area> areas)
    {
        foreach (var area in areas.Distinct())
        {
            int count = scene.Sections.Count(s => s.Areas.Contains(area));
            if (count != 1)
                w.Add($"{label} {id}: area '{area.DisplayName}' is in {count} sections (must be exactly 1)");
        }
    }

    /// <summary>
    /// The narration graph keys nodes by a display-name slug. A duplicate silently overwrites, and the
    /// losing room gets no node — unreachable, and invisible to NPC placement.
    /// </summary>
    private static void CheckNodeIdUniqueness(List<string> w, string label, int id, List<Area> areas)
    {
        var bySlug = areas
            .GroupBy(a => a.DisplayName.ToLowerInvariant().Replace(' ', '_'))
            .Where(g => g.Count() > 1);

        foreach (var g in bySlug)
            w.Add($"{label} {id}: {g.Count()} areas share the node slug '{g.Key}' — one loses its node");
    }

    /// <summary>
    /// Observation candidates are de-duplicated by name, so two same-named PoIs in one area leave one
    /// permanently unobservable. Doors are the case that matters: they are the landmarks.
    /// </summary>
    private static void CheckObservationNames(List<string> w, string label, int id, List<Area> areas)
    {
        foreach (var area in areas)
        {
            var dupes = area.PointsOfInterest
                .GroupBy(p => p.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1);

            foreach (var g in dupes)
                w.Add($"{label} {id}: area '{area.DisplayName}' has {g.Count()} PoIs named '{g.Key}' — only one is observable");
        }
    }

    /// <summary>
    /// A connector and an area-graph edge between the same pair means <c>MoveToAreaVerb</c> — base
    /// difficulty 1, never fails — offers a way round the connector, which silently makes it
    /// decorative. Harmless for a door while every door is unlocked; the whole lock system once one
    /// is not, and the whole point of a cliff, a crossing or a river from the moment it exists.
    ///
    /// <para>Checked against every <see cref="ConnectorPointOfInterest"/> in the scene rather than
    /// against a hard-coded list of types, so a connector written next year is covered the day it is
    /// written. Paths opt out via <c>AllowsGraphEdge</c>: they are named walks, not gates.</para>
    /// </summary>
    private static void CheckConnectorGraphExclusivity(List<string> w, string label, int id, Scene scene)
    {
        var connectors = scene.AllAreas
            .SelectMany(a => a.PointsOfInterest)
            .OfType<ConnectorPointOfInterest>()
            .Distinct();

        foreach (var connector in connectors)
        {
            if (connector.AllowsGraphEdge) continue;

            bool forward = scene.AreaGraph.TryGetValue(connector.AreaA.Id, out var f) && f.Contains(connector.AreaB.Id);
            bool back    = scene.AreaGraph.TryGetValue(connector.AreaB.Id, out var b) && b.Contains(connector.AreaA.Id);
            if (forward || back)
                w.Add($"{label} {id}: '{connector.DisplayName}' is duplicated by an area-graph edge — it can be walked around");

            // A connector joining an area to itself moves nobody. Cheap to write by accident when a
            // factory has not decided what the far side is yet, and invisible in play: the verb
            // appears, the roll happens, and nothing moves.
            if (connector.AreaA.Id == connector.AreaB.Id)
                w.Add($"{label} {id}: '{connector.DisplayName}' joins '{connector.AreaA.DisplayName}' to itself — traversing it is a no-op");
        }
    }

    /// <summary>Every entry door is shut at night. That is the one rule with no exceptions yet.</summary>
    private static void CheckLockRules(List<string> w, string label, int id, List<DoorPointOfInterest> doors)
    {
        foreach (var door in doors.Where(d => d.Role == DoorRole.Entry))
        {
            if (door.EffectiveState(TimePeriod.Night) != DoorState.Locked)
                w.Add($"{label} {id}: entry door '{door.DisplayName}' is not locked at night");
            if (door.BuildingDescription.Length == 0)
                w.Add($"{label} {id}: entry door '{door.DisplayName}' has no building description for the outside view");
        }

        foreach (var door in doors)
        {
            if (door.DoorDescription.Length == 0)
                w.Add($"{label} {id}: door '{door.DisplayName}' has no rolled description");
        }
    }

    /// <summary>
    /// Every named NPC sleeps in a room with a bed, and no two of them share one. This is the whole
    /// point of sizing buildings from the roster rather than the other way round.
    /// </summary>
    private static void CheckBeds(List<string> w, string label, int id, Scene scene)
    {
        var sleepersByArea = new Dictionary<Guid, int>();

        foreach (var npc in scene.Npcs)
        {
            if (npc.Entity is not NpcEntity) continue;   // shallow animals live in their pen
            if (!scene.NpcSchedules.TryGetValue(npc.Id, out var schedule)) continue;

            var bed = schedule.GetArea(TimePeriod.Night);
            if (bed == null)
            {
                w.Add($"{label} {id}: '{npc.DisplayName}' has nowhere to sleep (Night is null)");
                continue;
            }

            if (!BuildingRooms.BedsIn(bed).Any())
                w.Add($"{label} {id}: '{npc.DisplayName}' sleeps in '{bed.DisplayName}', which has no bed");

            sleepersByArea[bed.Id] = sleepersByArea.GetValueOrDefault(bed.Id) + 1;
        }

        foreach (var (areaId, sleepers) in sleepersByArea)
        {
            var area = scene.GetArea(areaId);
            if (area == null) continue;
            int beds = BuildingRooms.BedsIn(area).Count();
            if (sleepers > beds)
                w.Add($"{label} {id}: '{area.DisplayName}' sleeps {sleepers} in {beds} bed(s)");
        }
    }

    /// <summary>
    /// A village shop counter must be manned at every daylight hour — that requirement is the reason
    /// every workshop employs an apprentice, since its master is out for part of the day. A farm or
    /// field hall is a household rather than a shop, so it is allowed to stand empty for the one
    /// period its master is away (<paramref name="allowedEmptyPeriods"/>).
    /// </summary>
    private static void CheckHallStaffing(List<string> w, string label, int id, Scene scene, int allowedEmptyPeriods)
    {
        // A public hall is the non-private area of a building section (one whose areas are otherwise
        // all private). Outdoor sections have no private areas at all and are skipped.
        //
        // Roofs are excluded structurally — an area reached by climbing is never a hall. Without that
        // a private building, whose hall IS private, has exactly one non-private area (its roof), and
        // the roof gets mistaken for the counter nobody is standing behind.
        foreach (var section in scene.Sections)
        {
            var halls = section.Areas.Where(a => !a.IsPrivate && !IsClimbedTo(scene, a)).ToList();
            if (halls.Count != 1 || section.Areas.Count < 2) continue;
            if (!section.Areas.Any(a => a.IsPrivate)) continue;

            var hall  = halls[0];
            var empty = BuildingSchedule.DayPeriods
                .Where(p => !scene.GetNpcsAt(hall, p).Any(n => n.Entity is NpcEntity))
                .ToList();

            if (empty.Count > allowedEmptyPeriods)
                w.Add($"{label} {id}: public hall '{hall.DisplayName}' is empty at {string.Join(", ", empty)} "
                    + $"({empty.Count} periods, at most {allowedEmptyPeriods} allowed)");
        }
    }

    /// <summary>
    /// Whether <paramref name="area"/> is reached by climbing — the top of a scale point. A hall is
    /// never one, so this is what separates a shop counter from a roof without reading names.
    /// </summary>
    private static bool IsClimbedTo(Scene scene, Area area)
        => scene.AllAreas
            .SelectMany(a => a.PointsOfInterest)
            .OfType<ScalePointOfInterest>()
            .Any(sp => sp.TopArea.Id == area.Id);

    /// <summary>
    /// Every master must be out of their own public hall for at least one day period.
    ///
    /// <para>This is what makes employing someone structural rather than decorative: without it a
    /// master could mind their own counter all day and the apprentice would have no job to do. It
    /// regresses silently — the hall stays staffed either way — so it is checked rather than trusted.
    /// A master is identified as the NPC who owns the building's section.</para>
    /// </summary>
    private static void CheckMastersLeaveTheirHall(List<string> w, string label, int id, Scene scene)
    {
        foreach (var section in scene.Sections)
        {
            var halls = section.Areas.Where(a => !a.IsPrivate && !IsClimbedTo(scene, a)).ToList();
            if (halls.Count != 1 || !section.Areas.Any(a => a.IsPrivate)) continue;

            var hall      = halls[0];
            var sectionId = section.Id.ToString();

            foreach (var npc in scene.Npcs)
            {
                if (npc.Entity is not NpcEntity named) continue;
                if (!named.OwnedSectionIds.Contains(sectionId)) continue;
                if (!scene.NpcSchedules.TryGetValue(npc.Id, out var schedule)) continue;

                if (BuildingSchedule.AwayPeriodCount(schedule, hall) == 0)
                    w.Add($"{label} {id}: '{npc.DisplayName}' never leaves '{hall.DisplayName}' during the day");
            }
        }
    }

    /// <summary>
    /// Catches a word repeated back-to-back in anything the player reads — an area or PoI name, or the
    /// neutral description an observation is built from.
    ///
    /// <para>Two separate mechanics produce these and neither is obvious at the call site: a path
    /// named for its endpoints plus its own kind ("Square–Mill Lane <b>Lane</b>"), and a mood
    /// adjective prefixed onto a description that already used it ("a <b>stone-rimmed stone-rimmed</b>
    /// well"). Both read as a bug to a player and both survive a normal test run unnoticed, because
    /// nothing fails — the sentence is just wrong.</para>
    /// </summary>
    private static void CheckNoStutter(List<string> w, string label, int id, Scene scene)
    {
        foreach (var area in scene.AllAreas)
        {
            Check(area.DisplayName, $"area name '{area.DisplayName}'");

            foreach (var poi in area.PointsOfInterest)
            {
                Check(poi.DisplayName, $"PoI name '{poi.DisplayName}'");

                // The description as the observation phase would actually assemble it, mood and all.
                var rng  = SceneViewAdapter.DescriptionRng(poi, id);
                var desc = poi.Descriptions.Count > 0 ? poi.Descriptions[rng.Next(poi.Descriptions.Count)]
                                                      : poi.DisplayName.ToLowerInvariant();
                var mood = SceneViewAdapter.PickMood(poi.Moods, desc, rng);
                Check(mood == null ? desc : $"{mood} {desc}", $"description of '{poi.DisplayName}'");
            }
        }

        void Check(string text, string what)
        {
            var words = text.Split(' ', ',', '.', ';', ':', '—', '–', '(', ')')
                            .Where(t => t.Length > 0)
                            .ToList();

            for (int i = 1; i < words.Count; i++)
                if (string.Equals(words[i], words[i - 1], StringComparison.OrdinalIgnoreCase))
                {
                    w.Add($"{label} {id}: {what} repeats the word \"{words[i]}\"");
                    return;
                }
        }
    }

    private static void CheckStableKeys(List<string> w, string label, int id, Scene scene)
    {
        var keys = new HashSet<string>();
        foreach (var area in scene.AllAreas)
        {
            AddKey(area);
            foreach (var poi in area.PointsOfInterest) AddKey(poi);
        }

        void AddKey(Element e)
        {
            if (e.StableKey.Length == 0)
            {
                w.Add($"{label} {id}: '{e.DisplayName}' has no stable key");
                return;
            }
            // A connector lives in both its areas' PoI lists, so this walk reaches it twice. Keyed
            // off the base type rather than a list of the connector classes that existed when this
            // was written — that list went stale the moment crossings and scale points arrived, and
            // reported every one of them as a collision.
            if (!keys.Add(e.StableKey) && e is not ConnectorPointOfInterest)
                w.Add($"{label} {id}: stable key '{e.StableKey}' is used twice");
        }
    }

    /// <summary>
    /// Two builds of one location id must agree. Descriptions are seeded from stable keys, so a key
    /// that moves between builds silently re-rolls how every door in the place looks.
    /// </summary>
    private static void CheckRebuildStability(List<string> w, string label, Func<int, Scene> build)
    {
        const int probe = 7;
        var first  = build(probe);
        var second = build(probe);

        var a = first.AllAreas.Select(x => $"{x.StableKey}|{x.DisplayName}").ToList();
        var b = second.AllAreas.Select(x => $"{x.StableKey}|{x.DisplayName}").ToList();

        if (!a.SequenceEqual(b))
            w.Add($"{label}: location {probe} does not rebuild identically — {a.Count} vs {b.Count} areas, keys differ");

        var doorsA = AllDoors(first).Select(d => d.DoorDescription).ToList();
        var doorsB = AllDoors(second).Select(d => d.DoorDescription).ToList();
        if (!doorsA.SequenceEqual(doorsB))
            w.Add($"{label}: location {probe} re-rolls its door descriptions between builds");
    }

    /// <summary>
    /// Verbs are discovered by reflection over public, non-abstract, parameterless-constructible
    /// subclasses. Giving one a constructor parameter removes it from the game with no error at all.
    /// </summary>
    private static void AuditVerbDiscovery(List<string> w)
    {
        var required = new[] { "open_door", "unlock_door", "go_up_stairs", "go_down_stairs", "follow_path", "move" };
        var found    = VerbRegistry.Instance.GetAll().Select(v => v.VerbId).ToHashSet();

        foreach (var id in required)
            if (!found.Contains(id))
                w.Add($"verb '{id}' is not registered — check it is public, concrete and parameterless");
    }

    /// <summary>
    /// Door prose is what the player navigates by, so it must differ door to door and must survive
    /// the noun-phrase normalisation that embeds it in the observation sentence.
    /// </summary>
    private static void AuditDoorProse(StringBuilder sb, List<string> w)
    {
        var scene = new VillageSceneFactory().Build(3);
        var doors = AllDoors(scene);

        var bySection = doors
            .Where(d => d.Role == DoorRole.Interior)
            .GroupBy(d => d.StableKey.Split('|').Skip(1).FirstOrDefault() ?? "");

        foreach (var g in bySection)
        {
            var distinct = g.Select(d => d.DoorDescription).Distinct().Count();
            if (distinct != g.Count())
                w.Add($"building '{g.Key}' reuses a door description ({distinct} distinct of {g.Count()})");
        }

        // Exercise the real observation object, not just DescribeFrom: the side and the period reach
        // it through a constructor argument and a stamp respectively, and a door that lost either
        // would still describe itself perfectly well — just always from outside, always by day.
        // A public building's door (open by day, shut at night) and a private one (shut always) are
        // both checked: they exercise different branches of EffectiveState.
        var publicEntry  = doors.FirstOrDefault(d => d.Role == DoorRole.Entry && d.DoorState == DoorState.Unlocked);
        var privateEntry = doors.FirstOrDefault(d => d.Role == DoorRole.Entry && d.DoorState == DoorState.Locked);

        if (publicEntry != null)
        {
            string outside = Describe(publicEntry, publicEntry.FrontArea, TimePeriod.Noon);
            string inside  = Describe(publicEntry, publicEntry.BackArea,  TimePeriod.Noon);
            string night   = Describe(publicEntry, publicEntry.FrontArea, TimePeriod.Night);

            if (outside == inside)
                w.Add("a public entry door reads the same from outside and inside — the viewing area is not reaching the observation");
            if (!outside.Contains("seems open"))
                w.Add("a public entry door does not read as open by day");
            if (!night.Contains("seems locked"))
                w.Add("a public entry door does not read as locked at night — the period stamp is not reaching the observation");
        }
        else w.Add("no public entry door in the sample village — cannot check the observation plumbing");

        if (privateEntry != null)
        {
            if (!Describe(privateEntry, privateEntry.FrontArea, TimePeriod.Noon).Contains("seems locked"))
                w.Add("a private entry door does not read as locked by day");
        }
        else w.Add("no private entry door in the sample village — every workshop should have an apprentice with a house");

        // Builds the observation object exactly as the graph factory does, so this exercises the real
        // description path rather than calling DescribeFrom directly.
        static string Describe(DoorPointOfInterest door, Area from, TimePeriod when)
        {
            var obs = new SyntheticObservationObject(
                door, new SceneViewEntry(door, new List<VerbAction>()), new List<SceneViewEntry>(), from);
            obs.StampPeriod(when);
            return obs.GenerateNeutralDescription(3);
        }

        sb.AppendLine("--- DOOR PROSE SAMPLE (village, location 3) ---");
        foreach (var door in doors.Take(6))
        {
            var outside = door.DescribeFrom(door.FrontArea, TimePeriod.Noon);
            var inside  = door.DescribeFrom(door.BackArea,  TimePeriod.Noon);
            sb.AppendLine($"  {door.DisplayName} [{door.Role}]");
            sb.AppendLine($"    from {door.FrontArea.DisplayName}: This is {NeutralNarration.NounPhrase(outside)}.");
            sb.AppendLine($"    from {door.BackArea.DisplayName}: This is {NeutralNarration.NounPhrase(inside)}.");
            sb.AppendLine($"    at night: {door.DescribeFrom(door.FrontArea, TimePeriod.Night)}");
        }
    }
}
