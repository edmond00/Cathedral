using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>One item slot of a gathering source, and the day it is (or will be) there to take.</summary>
public sealed record GatherSlot(ItemElement Element, double AvailableAt, double RegenDays);

/// <summary>What a stay of some days would come to, before any die is rolled.</summary>
public sealed record GatherForecast(int Days, int Attempts, int Dice, int Difficulty, double Chance)
{
    /// <summary>The yield the dice make likely — attempts × chance, rounded to the nearest item.</summary>
    public int Expected => (int)Math.Round(Attempts * Chance, MidpointRounding.AwayFromZero);
}

/// <summary>What a stay actually yielded.</summary>
public sealed class GatherResult
{
    public int Days { get; init; }
    public int Gathered { get; set; }
    public int Spoiled { get; set; }
    /// <summary>Attempts never made because nothing more could be carried.</summary>
    public int NoRoom { get; set; }
    public Dictionary<string, int> ByItem { get; } = new();
}

/// <summary>
/// The rules of the gathering phase, which only a <see cref="GatherRoutine"/> opens.
///
/// <list type="bullet">
/// <item><b>A source has slots, and each one regrows.</b> They are the same item slots narration
///   picks from, with the same depletion stamps (<see cref="Scene.Scene.ItemDepletions"/>) and the same
///   regrowth (<see cref="PointOfInterest.RegenDays"/>) — so a bush stripped by hand is just as bare
///   to a routine, and the other way round.</item>
/// <item><b>A stay is a number of days.</b> Every slot offers one attempt as soon as it is full, and
///   another each time it has regrown within the stay. A short stay takes what is there now; a long
///   one waits for the season.</item>
/// <item><b>Every attempt is rolled.</b> The dice are those of the chain the act was learned with —
///   its modi mentis at their present levels — plus the implement's usage level; the difficulty is
///   the verb's. Success takes the item. <b>Failure spoils it</b>: the slot is emptied all the same and
///   has to regrow, so a clumsy hand costs time rather than nothing.</item>
/// <item><b>Nothing is taken that cannot be carried.</b> Once the pack refuses an item the stay goes
///   on but nothing more is attempted.</item>
/// </list>
/// </summary>
public static class GatherYield
{
    /// <summary>The holding point of interest in <paramref name="area"/>, or null. A spawned one never counts.</summary>
    public static PointOfInterest? FindSource(GatherRoutine routine, Area area)
        => area.PointsOfInterest.FirstOrDefault(p => !p.SpawnedDuringVisit
            && string.Equals(p.DisplayName, routine.SourceName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Every slot of <paramref name="source"/> the verb takes from, with the day each is available.
    /// Read off a scene built <b>without</b> depletion applied (see <see cref="RoutineCheckContext"/>),
    /// so the empty slots are in it too.
    /// </summary>
    public static IReadOnlyList<GatherSlot> Slots(GatherRoutine routine, Verb verb, PointOfInterest source,
        Scene.Scene scene, double now)
    {
        IEnumerable<ItemElement> items = verb switch
        {
            // The extraction verbs take whatever the source holds next — a seam is all ore.
            ExtractionVerb extraction when extraction.Works(source) => source.Items,
            // Gathering is of one thing: the patch may hold others, which other routines are for.
            GatherVerb => source.Items.Where(i => i.Item.ItemId == routine.ItemId),
            _ => Enumerable.Empty<ItemElement>(),
        };

        var slots = new List<GatherSlot>();
        foreach (var item in items)
        {
            double at = now;
            if (item.DepletionKey.Length > 0
                && scene.ItemDepletions.TryGetValue(item.DepletionKey, out var pickedAt)
                && now - pickedAt < source.RegenDays)
                at = pickedAt + source.RegenDays;
            slots.Add(new GatherSlot(item, at, source.RegenDays));
        }
        return slots;
    }

    /// <summary>The carried implement the routine needs, or null.</summary>
    public static Item? CarriedTool(GatherRoutine routine, PartyMember member)
        => routine.NeedsTool ? member.GetAllItems().FirstOrDefault(i => i.ItemId == routine.ToolItemId) : null;

    /// <summary>
    /// The dice each attempt is rolled with: the present effective levels of the chain the act was
    /// learned with (a modus mentis no longer held adds nothing), plus the implement's usage level —
    /// the same sum narration rolls. Never fewer than one die.
    /// </summary>
    public static int Dice(GatherRoutine routine, Protagonist protagonist)
    {
        int total = 0;
        foreach (var id in routine.ChainModusMentisIds)
        {
            var held = protagonist.LearnedModiMentis.FirstOrDefault(m => m.ModusMentisId == id);
            if (held != null) total += Math.Max(0, protagonist.GetEffectiveModusMentisLevel(held));
        }
        total += CarriedTool(routine, protagonist)?.UsageLevel ?? 0;
        return Math.Max(1, total);
    }

    /// <summary>Sixes needed per attempt: the verb's own difficulty against the source.</summary>
    public static int Difficulty(Verb verb, PointOfInterest source, IReadOnlyList<GatherSlot> slots)
        => Math.Max(1, verb is GatherVerb && slots.Count > 0
            ? verb.DifficultyFor(slots[0].Element)
            : verb.DifficultyFor(source));

    /// <summary>The chance that <paramref name="dice"/> d6 show at least <paramref name="sixes"/> sixes.</summary>
    public static double Chance(int dice, int sixes)
    {
        if (sixes <= 0) return 1.0;
        if (sixes > dice) return 0.0;
        double p = 0.0;
        for (int k = sixes; k <= dice; k++)
            p += Binomial(dice, k) * Math.Pow(1.0 / 6.0, k) * Math.Pow(5.0 / 6.0, dice - k);
        return Math.Min(1.0, p);
    }

    private static double Binomial(int n, int k)
    {
        double r = 1.0;
        for (int i = 1; i <= k; i++) r = r * (n - k + i) / i;
        return r;
    }

    /// <summary>Every attempt a stay of <paramref name="days"/> offers, in the order they come.</summary>
    public static List<(double At, GatherSlot Slot)> Attempts(IReadOnlyList<GatherSlot> slots, double now, int days)
    {
        double end = now + days;
        var attempts = new List<(double, GatherSlot)>();
        foreach (var slot in slots)
            for (double t = Math.Max(now, slot.AvailableAt); t <= end; t += Math.Max(1.0, slot.RegenDays))
                attempts.Add((t, slot));
        // Stable on ties, so the order is the slots' build order and a seeded run repeats itself.
        return attempts.OrderBy(a => a.Item1).ToList();
    }

    public static GatherForecast Forecast(IReadOnlyList<GatherSlot> slots, double now, int days, int dice, int difficulty)
        => new(days, Attempts(slots, now, days).Count, dice, difficulty, Chance(dice, difficulty));

    /// <summary>
    /// Walks the stay: rolls each attempt in turn, puts what succeeds in the protagonist's pack, and
    /// stamps every attempted slot as picked on the day it was attempted. The clock is the caller's
    /// to advance.
    /// </summary>
    public static GatherResult Run(IReadOnlyList<GatherSlot> slots, Scene.Scene scene, Protagonist protagonist,
        double now, int days, int dice, int difficulty)
    {
        var result = new GatherResult { Days = days };
        var rng    = GameRng.Stream("routine_gather");
        bool full  = false;

        foreach (var (at, slot) in Attempts(slots, now, days))
        {
            if (full) { result.NoRoom++; continue; }

            var item = ItemRegistry.GetById(slot.Element.Item.ItemId) ?? slot.Element.Item;
            if (!protagonist.CanAcquireItem(item)) { full = true; result.NoRoom++; continue; }

            if (Roll(rng, dice, difficulty) && protagonist.AcquireItem(item))
            {
                result.Gathered++;
                result.ByItem[item.DisplayName] = result.ByItem.GetValueOrDefault(item.DisplayName) + 1;
            }
            else result.Spoiled++;

            if (slot.Element.DepletionKey.Length > 0)
                scene.ItemDepletions[slot.Element.DepletionKey] = at;
        }

        Console.WriteLine($"GatherYield: {days} day(s), {dice} dice vs {difficulty} — " +
                          $"{result.Gathered} gathered, {result.Spoiled} spoiled, {result.NoRoom} left for want of room");
        return result;
    }

    /// <summary>
    /// One attempt. <c>--debug</c>'s <c>strategy</c> forces it either way, as it forces narration's
    /// rolls — which is what lets a script assert a yield.
    /// </summary>
    private static bool Roll(Random rng, int dice, int difficulty)
    {
        if (DebugMode.IsActive && DebugMode.CurrentStrategy == DebugStrategy.Succeed)     return true;
        if (DebugMode.IsActive && DebugMode.CurrentStrategy == DebugStrategy.FailDiceRoll) return false;

        int sixes = 0;
        for (int i = 0; i < dice; i++)
            if (rng.Next(1, 7) == 6) sixes++;
        return sixes >= difficulty;
    }
}
