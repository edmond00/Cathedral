using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Crafting;

/// <summary>
/// The roll behind every thing made at a station. Setting to work (USE) is rolled once, by one modus
/// mentis and one implement, like any act; each thing made afterwards is rolled on its own, and that
/// roll is wider: <b>every</b> trade the body holds for the station's kind and <b>every</b> implement of
/// that station's trades it carries put their dice in. A smith who knows both the hammer and the tongs,
/// and carries both, works the anvil with all four.
///
/// <list type="bullet">
///   <item><b>Dice</b> (<see cref="PoolFor"/>): the effective level of each held trade for the station
///     (a wounded or broken one gives what the wound leaves it), plus the usage level of each carried
///     implement of any trade for the station — once per kind of implement, the best one carried. One
///     die at the least.</item>
///   <item><b>Difficulty</b> (<see cref="Difficulty"/>): one to four sixes, from the made thing's price,
///     the measure of how much skill a thing holds beside its matter.</item>
///   <item><b>Success</b>: sixes at least the difficulty, as every roll in the game. A failure spoils the
///     work: its material is gone and nothing is made.</item>
/// </list>
///
/// <para>Drawn from <c>GameRng.Stream("crafting")</c>, so a seed replays it. Under <c>--debug</c> the
/// strategy forces it as it forces a conversation's roll: <c>succeed</c> lands the sixes needed (capped at
/// the dice), <c>fail-dice</c> lands none.</para>
/// </summary>
public static class CraftRoll
{
    /// <summary>Price bounds of the four difficulties: up to 7 is one six, up to 13 two, up to 22 three, above that four.</summary>
    public static readonly int[] PriceBounds = { 7, 13, 22 };

    /// <summary>Sixes needed to make <paramref name="output"/>, from 1 to 4.</summary>
    public static int Difficulty(Item output)
    {
        int price = output.PriceReference;
        int d = 1;
        foreach (var bound in PriceBounds) if (price > bound) d++;
        return d;
    }

    /// <summary>One source of dice: a trade by its level, or an implement by its usage.</summary>
    public sealed record Contribution(string Label, int Dice, bool IsTool);

    /// <summary>Every trade and implement that adds dice for <paramref name="member"/> at a station of <paramref name="station"/>'s kind.</summary>
    public static IReadOnlyList<Contribution> ContributionsFor(PartyMember member, Element station)
    {
        var list = new List<Contribution>();
        foreach (var trade in CraftingModusMentis.HeldFor(member, station))
            list.Add(new Contribution(trade.DisplayName, Math.Max(0, member.GetEffectiveModusMentisLevel(trade)), false));

        var toolTypes = CraftingModusMentis.All.Where(t => t.Station.IsInstanceOfType(station))
                                               .Select(t => t.Tool).Distinct().ToList();
        var carried = member.GetAllItems();
        foreach (var type in toolTypes)
        {
            var best = carried.Where(i => i.GetType() == type).OrderByDescending(i => i.UsageLevel).FirstOrDefault();
            if (best != null) list.Add(new Contribution(best.DisplayName, best.UsageLevel, true));
        }
        return list;
    }

    /// <summary>The dice a body rolls to make anything at <paramref name="station"/>: the contributions summed, one at the least.</summary>
    public static int PoolFor(PartyMember member, Element station)
        => Math.Max(1, ContributionsFor(member, station).Sum(c => c.Dice));

    /// <summary>What one roll came to.</summary>
    public sealed record Result(int Dice, int Difficulty, int[] Values)
    {
        public int Sixes => Values.Count(v => v == 6);
        public bool Succeeded => Sixes >= Difficulty;
    }

    /// <summary>Rolls <paramref name="dice"/> against <paramref name="difficulty"/> sixes.</summary>
    public static Result Roll(int dice, int difficulty)
    {
        var rng    = GameRng.Stream("crafting");
        var values = Enumerable.Range(0, dice).Select(_ => rng.Next(1, 7)).ToArray();
        if (DebugMode.IsActive && !DebugMode.IsAutoStrategy)
        {
            switch (DebugMode.CurrentStrategy)
            {
                case DebugStrategy.Succeed:
                    int needed = Math.Min(difficulty, dice);
                    for (int i = 0; i < values.Length; i++) values[i] = i < needed ? 6 : Math.Min(values[i], 5);
                    break;
                case DebugStrategy.FailDiceRoll:
                    for (int i = 0; i < values.Length; i++) if (values[i] == 6) values[i] = 5;
                    break;
            }
        }
        return new Result(dice, difficulty, values);
    }
}
