using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Scene.Building;

/// <summary>
/// Procedural prose for buildings and their doors.
///
/// <para>Doors matter more here than the word count suggests. A player finds their way around an
/// interior by remembering which door was which, so the descriptions within one building are drawn
/// <b>without replacement</b> from a combinatorial pool — two doors in the same house never read
/// alike. All returned phrases carry their own article, because they are embedded mid-sentence by
/// <c>NeutralNarration.ObservationDetail</c>, whose <c>NounPhrase</c> would otherwise prepend one.</para>
/// </summary>
public static class BuildingDescriptions
{
    // ── Materials ─────────────────────────────────────────────────────────────

    public static string MaterialWord(BuildingMaterial m) => m switch
    {
        BuildingMaterial.Stone         => "stone",
        BuildingMaterial.WattleAndDaub => "wattle-and-daub",
        BuildingMaterial.Timber        => "timber-framed",
        _                              => "wooden",
    };

    /// <summary>
    /// Rolls a material appropriate to the building's function. Weighted, not uniform: a longhouse
    /// is timber country and a workshop that holds a fire wants stone, so callers who care force it
    /// via <see cref="BuildingSpec.Material"/> and everyone else gets something plausible.
    /// </summary>
    public static BuildingMaterial RollMaterial(BuildingKind kind, Random rng)
    {
        BuildingMaterial[] pool = kind switch
        {
            BuildingKind.Workshop  => new[] { BuildingMaterial.Stone, BuildingMaterial.Timber,
                                              BuildingMaterial.Timber, BuildingMaterial.Wood },
            BuildingKind.Longhouse => new[] { BuildingMaterial.Timber, BuildingMaterial.Timber,
                                              BuildingMaterial.WattleAndDaub, BuildingMaterial.Wood },
            BuildingKind.Barracks  => new[] { BuildingMaterial.Wood, BuildingMaterial.WattleAndDaub,
                                              BuildingMaterial.WattleAndDaub },
            _                      => new[] { BuildingMaterial.Wood, BuildingMaterial.WattleAndDaub,
                                              BuildingMaterial.Timber, BuildingMaterial.Stone },
        };
        return pool[rng.Next(pool.Length)];
    }

    // ── Building exterior ─────────────────────────────────────────────────────

    private static readonly string[] BuildingSize =
    {
        "a low", "a squat", "a long", "a narrow", "a broad", "a lean", "a hunched",
    };

    /// <summary>
    /// Weathering adjectives. Also doubles as the distinguishing trait of an anonymous building — see
    /// <see cref="AnonymousLabel"/> — so the pool must stay large enough that a village can give every
    /// one of its houses a different one.
    /// </summary>
    public static readonly string[] BuildingWear =
    {
        "weather-beaten", "moss-shouldered", "smoke-stained", "sagging", "lime-washed",
        "soot-darkened", "rain-streaked", "patched", "settled", "wind-scoured",
        "crooked", "sunken", "ivy-grown", "bleached", "tar-daubed", "lichen-crusted",
        "slump-walled", "swallow-nested",
    };

    /// <summary>
    /// The name a passer-by would use for a building they know nothing about: its most obvious
    /// weathering plus what it plainly is — "Crooked House", "Ivy-Grown House".
    ///
    /// <para>Used for buildings whose occupant should not be legible from the street. A workshop can
    /// hang a sign, so it keeps its trade name; a house cannot, and calling its door "Blacksmith
    /// Apprentice's House Door" told the player who lived there before they had met anyone. The
    /// label still has to differ between houses, or the observation phase — which offers the player a
    /// list of things to look at — would show several identical entries.</para>
    /// </summary>
    public static string AnonymousLabel(string wear, BuildingKind kind)
        => $"{Capitalise(wear)} {Capitalise(KindNoun(kind))}";

    private static string Capitalise(string s)
    {
        if (s.Length == 0) return s;
        // Hyphenated adjectives read as titles on both halves: "moss-shouldered" → "Moss-Shouldered".
        var parts = s.Split('-');
        for (int i = 0; i < parts.Length; i++)
            if (parts[i].Length > 0)
                parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
        return string.Join('-', parts);
    }

    private static readonly string[] BuildingRoof =
    {
        "under a thatch gone grey", "under a steep shingled roof", "under sagging thatch",
        "under turf grown thick with weeds", "roofed in split shingle", "under a mossed slate roof",
    };

    /// <summary>
    /// The whole building as seen from the street: size, wear, material, function and roof. Used only
    /// for the outside view of an entry door, and stored as the section's description.
    /// </summary>
    public static string RollBuildingDescription(
        BuildingKind kind, BuildingMaterial mat, int floors, string? functionNoun, Random rng,
        string? forcedWear = null)
    {
        var size = BuildingSize[rng.Next(BuildingSize.Length)];
        // The caller forces the wear word when the building is known by it ("the crooked house"), so
        // that the name over the door and the prose describing it agree.
        var wear = forcedWear ?? BuildingWear[rng.Next(BuildingWear.Length)];
        var roof = BuildingRoof[rng.Next(BuildingRoof.Length)];
        var noun = functionNoun ?? KindNoun(kind);
        var storey = floors >= 2 ? "two-storey " : "";

        return $"{size} {wear} {storey}{MaterialWord(mat)} {noun} {roof}";
    }

    public static string KindNoun(BuildingKind kind) => kind switch
    {
        BuildingKind.Workshop  => "workshop",
        BuildingKind.Longhouse => "longhouse",
        BuildingKind.Barracks  => "range of quarters",
        _                      => "house",
    };

    // ── Doors ─────────────────────────────────────────────────────────────────

    private static readonly string[] DoorBuild =
    {
        "a low", "a narrow", "a heavy", "a warped", "a squat", "a stout", "a slender", "a battered",
    };

    private static readonly string[] DoorFabric =
    {
        "plank", "board", "batten", "oak", "elm", "ledged", "split-log", "panelled",
    };

    private static readonly string[] DoorDetail =
    {
        "iron-hinged and rough-fitted",
        "hung on leather straps",
        "its latch worn bright by hands",
        "studded with square-headed nails",
        "scarred low down where boots have shoved it",
        "banded with rust-pitted iron",
        "its planks shrunk apart enough to see light through",
        "swollen in the frame and hard to shift",
        "pale where an older lock was prised away",
        "hung crooked, catching on the sill",
        "smoke-blackened along the top edge",
        "patched with a fresher board at the foot",
    };

    /// <summary>
    /// <paramref name="count"/> distinct door descriptions for one building. Drawn without
    /// replacement across the whole (build × fabric × detail) space, so a building with a dozen doors
    /// still gives every one of them its own look — which is the only thing making a door usable as a
    /// landmark, since every door in the game shares the reference lemma "door".
    /// </summary>
    public static string[] RollDoorDescriptions(BuildingMaterial mat, int count, Random rng)
    {
        if (count <= 0) return Array.Empty<string>();

        var seen   = new HashSet<string>();
        var result = new List<string>(count);

        // Bounded rather than while(true): the pool is ~768 combinations, so collisions are rare, but
        // a caller asking for more doors than exist must degrade rather than spin.
        for (int attempt = 0; result.Count < count && attempt < count * 40; attempt++)
        {
            var phrase = $"{DoorBuild[rng.Next(DoorBuild.Length)]} "
                       + $"{DoorFabric[rng.Next(DoorFabric.Length)]} door, "
                       + $"{DoorDetail[rng.Next(DoorDetail.Length)]}";
            if (seen.Add(phrase)) result.Add(phrase);
        }

        // Degenerate fallback — numbered so they stay distinguishable even if prose runs out.
        while (result.Count < count)
            result.Add($"a plain {MaterialWord(mat)} door, the {Ordinal(result.Count + 1)} of its kind");

        return result.ToArray();
    }

    private static string Ordinal(int n) => n switch
    {
        1 => "first", 2 => "second", 3 => "third", 4 => "fourth", 5 => "fifth",
        6 => "sixth", 7 => "seventh", 8 => "eighth", 9 => "ninth", _ => $"{n}th",
    };
}
