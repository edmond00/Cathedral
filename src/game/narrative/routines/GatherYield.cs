using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>One item slot of a gathering source, and the day it is (or will be) there to take.</summary>
public sealed record GatherSlot(ItemElement Element, double AvailableAt, double RegenDays);

/// <summary>What a stay of some days will come to.</summary>
public sealed record GatherForecast(int Days, int Attempts)
{
    /// <summary>What the stay yields: every attempt it offers takes its item.</summary>
    public int Expected => Attempts;
}

/// <summary>What a stay actually yielded.</summary>
public sealed class GatherResult
{
    public int Days { get; init; }
    public int Gathered { get; set; }
    /// <summary>Items never taken because nothing more could be carried.</summary>
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
/// <item><b>A stay is a number of days, and the yield grows with it.</b> Every slot gives its item as
///   soon as it is full, and again each time it has regrown within the stay. There is no roll: the
///   gathering was learned by doing it once, and a routine is the knowing how — what a stay costs is
///   the days, and what it is worth is how long they were.</item>
/// <item><b>Nothing is taken that cannot be carried.</b> Once the pack refuses an item the stay goes
///   on but nothing more is taken.</item>
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

    public static GatherForecast Forecast(IReadOnlyList<GatherSlot> slots, double now, int days)
        => new(days, Attempts(slots, now, days).Count);

    /// <summary>
    /// Walks the stay: takes each item in turn into the protagonist's pack, and stamps every slot taken
    /// as picked on the day it was taken. The clock is the caller's to advance.
    /// </summary>
    public static GatherResult Run(IReadOnlyList<GatherSlot> slots, Scene.Scene scene, Protagonist protagonist,
        double now, int days)
    {
        var result = new GatherResult { Days = days };
        bool full  = false;

        foreach (var (at, slot) in Attempts(slots, now, days))
        {
            if (full) { result.NoRoom++; continue; }

            var item = ItemRegistry.GetById(slot.Element.Item.ItemId) ?? slot.Element.Item;
            if (!protagonist.CanAcquireItem(item) || !protagonist.AcquireItem(item)) { full = true; result.NoRoom++; continue; }

            result.Gathered++;
            result.ByItem[item.DisplayName] = result.ByItem.GetValueOrDefault(item.DisplayName) + 1;
            if (slot.Element.DepletionKey.Length > 0)
                scene.ItemDepletions[slot.Element.DepletionKey] = at;
        }

        Console.WriteLine($"GatherYield: {days} day(s) — {result.Gathered} gathered, {result.NoRoom} left for want of room");
        return result;
    }
}
