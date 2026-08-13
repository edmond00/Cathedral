using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Save;

/// <summary>
/// Checks that a live run survives being written down and read back, without touching the disk or the
/// running game.
///
/// <para>This exists because the suite runs one script per launch, so a real save-quit-relaunch-load
/// test cannot be expressed in it. Moving the assertion in-process catches the failure that actually
/// matters — a field added to a party type and forgotten in the DTO, which silently resets on every
/// load — and any existing script can append it after doing something interesting.</para>
/// </summary>
public static class SaveRoundTrip
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,   // indented so a divergence report shows one field per line
        Converters    = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Serialises <paramref name="live"/>, rebuilds it, serialises the result, and compares. Returns
    /// null when they match, or a description of the first divergence.
    /// </summary>
    public static string? Check(SaveGame live)
    {
        if (live == null) return "no run to check";

        string before;
        try { before = JsonSerializer.Serialize(live, Options); }
        catch (Exception ex) { return $"serialise failed: {ex.GetType().Name}: {ex.Message}"; }

        SaveGame? reread;
        try { reread = JsonSerializer.Deserialize<SaveGame>(before, Options); }
        catch (Exception ex) { return $"deserialise failed: {ex.GetType().Name}: {ex.Message}"; }
        if (reread == null) return "deserialise produced nothing";

        var rebuilt = reread.Party.Rebuild();
        if (rebuilt == null) return "rebuild failed (see stderr for the unresolved id)";

        // Reference topology, which the text comparison below CANNOT see: two modus mentis objects
        // where the live graph had one serialise identically, while every reference-equality check in
        // the memory system (remove, archive, consolidate) silently stops working.
        string? aliasing = CheckAliasing(rebuilt);
        if (aliasing != null) return aliasing;

        var after = new SaveGame
        {
            Seed         = reread.Seed,
            Days         = reread.Days,
            AvatarVertex = reread.AvatarVertex,
            Party        = PartyState.Capture(rebuilt),
            Locations    = reread.Locations,
        };

        string second;
        try { second = JsonSerializer.Serialize(after, Options); }
        catch (Exception ex) { return $"re-serialise failed: {ex.GetType().Name}: {ex.Message}"; }

        return before == second ? null : FirstDifference(before, second);
    }

    /// <summary>
    /// The two invariants a value comparison is blind to: the modus-mentis list is one object under
    /// two names, and a memory slot holds the very instance the list holds — not an equal copy.
    /// </summary>
    private static string? CheckAliasing(Protagonist protagonist)
    {
        foreach (var member in new[] { (PartyMember)protagonist }.Concat(protagonist.CompanionParty))
        {
            if (!ReferenceEquals(member.ModiMentis, member.LearnedModiMentis))
                return $"aliasing: {member.DisplayName}'s ModiMentis and LearnedModiMentis are two different lists";

            foreach (var module in member.MemoryModules)
                foreach (var slot in module.Slots)
                {
                    if (slot.ModusMentis == null) continue;
                    bool shared = member.ModiMentis.Any(mm => ReferenceEquals(mm, slot.ModusMentis));
                    if (!shared)
                        return $"aliasing: {member.DisplayName}'s {module.Type} slot holds a copy of "
                             + $"'{slot.ModusMentis.ModusMentisId}', not the instance in ModiMentis";
                }
        }
        return null;
    }

    /// <summary>The first line that differs, with its line number — enough to name the guilty field.</summary>
    private static string FirstDifference(string a, string b)
    {
        var linesA = a.Split('\n');
        var linesB = b.Split('\n');
        for (int i = 0; i < Math.Max(linesA.Length, linesB.Length); i++)
        {
            string la = i < linesA.Length ? linesA[i].Trim() : "<end>";
            string lb = i < linesB.Length ? linesB[i].Trim() : "<end>";
            if (la != lb) return $"line {i + 1}: saved '{la}' but rebuilt '{lb}'";
        }
        return "the two differ in length only";
    }

    /// <summary>
    /// A fixed one-line-per-fact summary of a save, for scripts to assert on with the existing
    /// <c>expect</c> verbs. Counts rather than contents, so it stays stable as content is authored.
    /// </summary>
    public static IReadOnlyList<string> Describe(SaveGame save)
    {
        var p = save.Party;
        int mm     = p.Protagonist.ModiMentis.Count;
        int slots  = p.Protagonist.Memory.Values.Sum(v => v.Count(id => id != null));
        int items  = p.Protagonist.Inventory.Count + p.Protagonist.Equipped.Values.Sum(v => v.Count);
        // Invariant culture throughout: this is an assertion surface, and on a machine whose locale
        // uses a comma for the decimal point "days=0.0" silently becomes "days=0,0" and every script
        // that asserts on it fails somewhere it has never been run.
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        return new List<string>
        {
            $"save version={save.Version}",
            $"save seed={save.Seed}",
            $"save days={save.Days.ToString("F1", inv)}",
            $"save vertex={save.AvatarVertex}",
            $"save locations={save.Locations.Count}",
            $"save companions={p.Companions.Count}",
            $"save name={p.CharacterName}",
            $"save coins={p.Gold}g/{p.Silver}s/{p.Copper}c",
            $"save modimentis={mm}",
            $"save memoryslots={slots}",
            $"save items={items}",
            $"save wounds={p.Protagonist.Wounds.Count}",
            $"save routines={p.Routines.Count}",
        };
    }
}
