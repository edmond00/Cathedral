using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.History.Generation;

namespace Cathedral.Game.History.Engine.Seeds;

// The seeds every world's own history is made of, in every era: people are born and die, realms are
// founded, fight, split and unite, faiths come and go, places are built and fall. None of them knows
// about the empire; the imperial seeds (ImperialSeeds.cs) do.

/// <summary>
/// A seed that comes back: it acts (or finds nothing to act on) and sows itself again. Used for the
/// background rhythm of a world: wars, new faiths, new towns. It never reports itself skipped, because
/// a quiet round is not a failed promise.
/// </summary>
public abstract class PulseSeed : HistorySeed
{
    protected PulseSeed(int due) : base(due) { }

    /// <summary>The next pulse, or null to stop.</summary>
    protected abstract HistorySeed? Next(HistorySimulation sim);

    protected abstract void Act(HistorySimulation sim);

    public sealed override void Sprout(HistorySimulation sim)
    {
        Act(sim);
        if (Next(sim) is { } next) sim.Sow(next);
    }
}

/// <summary>Someone dies: of age, illness, accident, violence. A ruler's death opens the succession.</summary>
public sealed class FigureDeathSeed : HistorySeed
{
    public FigureDeathSeed(int due, HistoricFigure figure) : base(due) => Figure = figure;

    public HistoricFigure Figure { get; }

    public override int Priority => 10;

    public override bool CanSprout(HistorySimulation sim) => Figure.IsAliveAt(sim.Now);

    public override void Sprout(HistorySimulation sim)
    {
        bool atWar = sim.History.Wars.Any(w => !w.Ended.IsKnown && w.Belligerents.Contains(Figure.Affiliation!));
        var cause = ReligionGenerator.Weighted(sim.Rng, new (DeathCause, int)[]
        {
            (DeathCause.Natural, 55), (DeathCause.Illness, 25), (DeathCause.Accident, 6),
            (DeathCause.Murder, 6), (DeathCause.Battle, atWar ? 30 : 0), (DeathCause.Plague, 4),
        }.Where(t => t.Item2 > 0).ToList());
        sim.Kill(Figure, cause);
    }
}

/// <summary>A ruler has a child, whose birth is recorded and who may one day inherit.</summary>
public sealed class HeirBirthSeed : HistorySeed
{
    public HeirBirthSeed(int due, Realm realm, HistoricFigure parent) : base(due)
    {
        Realm = realm;
        Parent = parent;
    }

    public Realm Realm { get; }
    public HistoricFigure Parent { get; }

    public override bool CanSprout(HistorySimulation sim)
        => Parent.Scope == HistoryScope.World && Parent.IsAliveAt(sim.Now) && Parent.Children.Count < 3;

    public override void Sprout(HistorySimulation sim)
    {
        sim.Birth(Parent, Realm);
        if (sim.Chance(0.2)) sim.Sow(new HeirBirthSeed(sim.Later(1, 10), Realm, Parent));
    }
}

/// <summary>
/// A realm is founded on unclaimed land. Sown in numbers at the start of a world's history; later,
/// settlement brings more.
/// </summary>
public sealed class RealmFoundationSeed : HistorySeed
{
    public RealmFoundationSeed(int due, int preferredRegion = -1) : base(due) => PreferredRegion = preferredRegion;

    public int PreferredRegion { get; }

    public override bool CanSprout(HistorySimulation sim) => sim.UnclaimedRegions.Any();

    public override void Sprout(HistorySimulation sim)
    {
        int start = PreferredRegion >= 0 && sim.History.OwnerOf(PreferredRegion) == null
            ? PreferredRegion
            : PickHabitable(sim);
        var cluster = sim.GrowCluster(start, sim.Rng.Next(1, 4), r => sim.History.OwnerOf(r) == null);
        var realm = sim.FoundRealm(cluster);

        if (!sim.Chance(0.5)) return;
        var capital = sim.Regions[realm.CapitalRegion];
        var kind = capital.Coastal && sim.Chance(0.4) ? PlaceKind.Port : sim.Chance(0.5) ? PlaceKind.City : PlaceKind.Town;
        sim.NewPlace(kind, realm.CapitalRegion, realm, sim.History.RegionNames[realm.CapitalRegion]);
    }

    private static int PickHabitable(HistorySimulation sim)
    {
        var open = sim.UnclaimedRegions.ToList();
        var weighted = open.Select(r => (r, 1 + sim.Regions[r].Habitability)).ToList();
        return ReligionGenerator.Weighted(sim.Rng, weighted);
    }
}

/// <summary>Two named realms go to war. Sown by a split, a revolt, or a pulse that has found its pair.</summary>
public sealed class WarSeed : HistorySeed
{
    public WarSeed(int due, Realm attacker, Realm defender, string? cause = null) : base(due)
    {
        Attacker = attacker;
        Defender = defender;
        Cause = cause;
    }

    public Realm Attacker { get; }
    public Realm Defender { get; }
    public string? Cause { get; }

    public override bool CanSprout(HistorySimulation sim)
        => !Attacker.Dissolved.IsKnown && !Defender.Dissolved.IsKnown
        && Attacker.Regions.Count > 0 && Defender.Regions.Count > 0
        && !AtWar(sim, Attacker, Defender) && sim.CanReach(Attacker, Defender);

    internal static bool AtWar(HistorySimulation sim, Realm a, Realm b)
        => sim.History.Wars.Any(w => !w.Ended.IsKnown && w.Belligerents.Contains(a) && w.Belligerents.Contains(b));

    public override void Sprout(HistorySimulation sim)
    {
        var (name, cause) = RealmGenerator.War(Attacker, Defender, sim.Rng);
        var war = new War(name, HistoryScope.World) { Started = sim.Today };
        war.Attackers.Add(Attacker);
        war.Defenders.Add(Defender);
        sim.History.Wars.Add(war);
        sim.Record(new WarStartedEvent(sim.Today, HistoryScope.World, war, Cause ?? cause));
        sim.Sow(new WarEndSeed(sim.Later(1, 12), war));
    }
}

/// <summary>
/// A war is decided: the stronger side usually wins, takes border land, sometimes a ruler falls or a
/// town is sacked. A side that has vanished in the meantime loses by default.
/// </summary>
public sealed class WarEndSeed : HistorySeed
{
    public WarEndSeed(int due, War war) : base(due) => War = war;

    public War War { get; }

    public override int Priority => 20;

    public override bool CanSprout(HistorySimulation sim) => !War.Ended.IsKnown;

    public override void Sprout(HistorySimulation sim)
    {
        var a = (Realm)War.Attackers[0];
        var d = (Realm)War.Defenders[0];
        bool aStands = !a.Dissolved.IsKnown && a.Regions.Count > 0;
        bool dStands = !d.Dissolved.IsKnown && d.Regions.Count > 0;

        if (!aStands || !dStands)
        {
            War.Outcome = aStands ? WarOutcome.AttackerWon : dStands ? WarOutcome.DefenderWon : WarOutcome.WhitePeace;
        }
        else
        {
            double aStrength = a.Regions.Count + sim.Rng.NextDouble() * 4;
            double dStrength = d.Regions.Count * 1.2 + sim.Rng.NextDouble() * 4;   // defending is easier
            War.Outcome = Math.Abs(aStrength - dStrength) < 0.6 ? WarOutcome.WhitePeace
                        : aStrength > dStrength ? WarOutcome.AttackerWon : WarOutcome.DefenderWon;

            if (War.Outcome != WarOutcome.WhitePeace)
            {
                var (winner, loser) = War.Outcome == WarOutcome.AttackerWon ? (a, d) : (d, a);
                string battleSite = sim.History.RegionNames[sim.Pick(loser.Regions.ToList())];
                sim.Record(new BattleEvent(sim.Today, HistoryScope.World, War, $"The battle of {battleSite}", winner, loser));

                var border = sim.BorderRegions(loser, winner);
                if (border.Count == 0) border = loser.Regions.Where(r => sim.Regions[r].Coastal).ToList();
                if (border.Count == 0) border = loser.Regions.ToList();
                var taken = border.OrderBy(_ => sim.Rng.Next()).Take(sim.Rng.Next(1, 3)).ToList();
                // A small realm beaten is often swallowed whole; more often still on a splintered map.
                if (loser.Regions.Count - taken.Count <= 0
                    || (loser.Regions.Count <= 2 && sim.Chance(sim.IsCrowded ? 0.7 : 0.4)))
                    taken = loser.Regions.ToList();

                var sacked = sim.History.Places.Where(p => taken.Contains(p.Region) && !p.Ruined.IsKnown).ToList();
                sim.Transfer(taken, winner, "by conquest");
                if (sacked.Count > 0 && sim.Chance(0.25)) sim.Ruin(sim.Pick(sacked), "sacked in " + War.Name);
                if (loser.Ruler != null && loser.Ruler.Scope == HistoryScope.World && sim.Chance(0.15))
                    sim.Kill(loser.Ruler, DeathCause.Battle);
            }
        }

        War.Ended = sim.Today;
        sim.Record(new WarEndedEvent(sim.Today, HistoryScope.World, War));
    }
}

/// <summary>Part of a realm breaks away: a province, a rebel lord, a pretender's party.</summary>
public sealed class RealmSplitSeed : HistorySeed
{
    public RealmSplitSeed(int due, Realm realm, string why, Government? government = null, bool forced = false) : base(due)
    {
        Realm = realm;
        Why = why;
        Government = government;
        Forced = forced;
    }

    public Realm Realm { get; }
    public string Why { get; }
    public Government? Government { get; }

    /// <summary>
    /// A split that happens however splintered the world already is: the breaking of a stranded
    /// province. A routine breakaway (a contested succession) holds back on a crowded map.
    /// </summary>
    public bool Forced { get; }

    public override bool CanSprout(HistorySimulation sim)
        => !Realm.Dissolved.IsKnown && Realm.Regions.Count >= 2 && (Forced || !sim.IsCrowded);

    public override void Sprout(HistorySimulation sim)
    {
        var candidates = Realm.Regions.Where(r => r != Realm.CapitalRegion).ToList();
        int start = sim.Pick(candidates);
        int size = Math.Max(1, sim.Rng.Next(1, Realm.Regions.Count / 2 + 1));
        var part = sim.GrowCluster(start, size, r => Realm.Regions.Contains(r) && r != Realm.CapitalRegion);
        var breakaway = sim.Split(Realm, part, Why, government: Government);
        if (sim.Chance(0.4)) sim.Sow(new WarSeed(sim.Later(0, 2), Realm, breakaway, "to take back what broke away"));
    }
}

/// <summary>The background of war: every few decades two realms that can reach each other fight.</summary>
public sealed class WarPulseSeed : PulseSeed
{
    public WarPulseSeed(int due) : base(due) { }

    protected override void Act(HistorySimulation sim)
    {
        var realms = sim.History.LivingRealms.ToList();
        var pairs = new List<(Realm, Realm)>();
        foreach (var a in realms)
            foreach (var b in realms)
                if (a != b && sim.CanReach(a, b) && !WarSeed.AtWar(sim, a, b)) pairs.Add((a, b));
        if (pairs.Count == 0) return;
        var (x, y) = sim.Pick(pairs);
        var start = new WarSeed(sim.Now, x, y);
        if (start.CanSprout(sim)) start.Sprout(sim);
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new WarPulseSeed(sim.Later(30, 90));
}

/// <summary>Unclaimed land is taken: by a neighbour pushing its border, or by a new realm.</summary>
public sealed class SettlementPulseSeed : PulseSeed
{
    public SettlementPulseSeed(int due) : base(due) { }

    protected override void Act(HistorySimulation sim)
    {
        var open = sim.UnclaimedRegions.ToList();
        if (open.Count == 0) return;
        int region = sim.Pick(open);
        var neighbours = sim.Regions[region].Neighbours.Select(n => sim.History.OwnerOf(n)).Where(o => o != null)
                           .Distinct().ToList();
        // Free land goes to whoever borders it; a new realm rises on it only where nobody does, or
        // while the world is still thinly held.
        if (neighbours.Count > 0 && (sim.IsCrowded || sim.Chance(0.8)))
            sim.Transfer(new[] { region }, sim.Pick(neighbours)!, "settled");
        else if (!sim.IsCrowded || neighbours.Count == 0)
            new RealmFoundationSeed(sim.Now, region).Sprout(sim);
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new SettlementPulseSeed(sim.Later(15, 70));
}

/// <summary>Two neighbouring crowns joined by marriage; the smaller realm ends in the larger.</summary>
public sealed class UnionPulseSeed : PulseSeed
{
    public UnionPulseSeed(int due) : base(due) { }

    protected override void Act(HistorySimulation sim)
    {
        var realms = sim.History.LivingRealms.Where(r => r.Ruler is { Scope: HistoryScope.World } && r.Government
                         is Government.Kingdom or Government.Principality or Government.Duchy or Government.Chiefdom).ToList();
        var pairs = new List<(Realm, Realm)>();
        foreach (var a in realms)
            foreach (var b in realms)
                if (a != b && a.Regions.Count >= b.Regions.Count && sim.BorderRegions(a, b).Count > 0 && !WarSeed.AtWar(sim, a, b)
                    && a.Regions.Count + b.Regions.Count <= sim.GreatRealmSize)
                    pairs.Add((a, b));
        if (pairs.Count == 0) return;
        var (big, small) = sim.Pick(pairs);
        sim.Record(new MarriageEvent(sim.Today, HistoryScope.World, big.Ruler!, small.Ruler!));
        big.Ruler!.Marry(small.Ruler!);
        sim.Transfer(small.Regions.ToList(), big, "united by marriage");
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new UnionPulseSeed(sim.Later(sim.IsCrowded ? 25 : 60, sim.IsCrowded ? 90 : 220));
}

/// <summary>
/// The counterweight to conquest and marriage: a realm grown past what one court can hold loses a
/// province to a rebel lord, a regent or a younger son. The larger the realm, the likelier. Imperial
/// provinces are left to <c>RevoltPulseSeed</c> while the empire stands.
/// </summary>
public sealed class BreakupPulseSeed : PulseSeed
{
    public BreakupPulseSeed(int due) : base(due) { }

    private static readonly string[] Causes =
    {
        "a rebellious province", "a regent's revolt", "a younger son's claim", "an overmighty lord",
        "the distant marches", "a governor turned king", "a quarrel over the crown",
    };

    protected override void Act(HistorySimulation sim)
    {
        var great = sim.History.LivingRealms
            .Where(r => r.Regions.Count >= sim.GreatRealmSize && r != sim.History.Province)
            .OrderByDescending(r => r.Regions.Count).ThenBy(r => r.Name, StringComparer.Ordinal)
            .FirstOrDefault();
        if (great == null) return;
        double pressure = Math.Min(0.9, great.Regions.Count / (double)(sim.GreatRealmSize * 2));
        if (!sim.Chance(pressure)) return;
        var split = new RealmSplitSeed(sim.Now, great, sim.Pick(Causes), forced: true);
        if (split.CanSprout(sim)) split.Sprout(sim);
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new BreakupPulseSeed(sim.Later(15, 50));
}

/// <summary>Faith moves: a prophet founds a new one or splits an old one, or a realm converts.</summary>
public sealed class ReligionPulseSeed : PulseSeed
{
    public ReligionPulseSeed(int due) : base(due) { }

    // A faith can be born, spread, be persecuted into hiding, or fade away. One of the four per beat.
    protected override void Act(HistorySimulation sim)
    {
        var realms = sim.History.LivingRealms.Where(r => r.Government is not (Government.ImperialProvince or Government.Commandery)).ToList();
        double roll = sim.Rng.NextDouble();
        if (roll < 0.35 || realms.Count < 2) Prophesy(sim, realms);
        else if (roll < 0.65) Convert(sim, realms);
        else if (roll < 0.82) Persecute(sim, realms);
        else Fade(sim);
    }

    private static void Prophesy(HistorySimulation sim, List<Realm> realms)
    {
        var prophet = sim.NewAdult(null, minYearsLeft: 3, maxYearsLeft: 40);
        var natives = sim.History.LivingFaiths.Where(r => r.Scope == HistoryScope.World && sim.CanAdoptOpenly(r)).ToList();
        var parent = natives.Count > 0 && sim.Chance(0.4) ? sim.Pick(natives) : null;
        var faith = sim.NewReligion(prophet, parent, parent == null ? null : parent.Kind);
        prophet.Description = parent == null ? $"The prophet who founded {faith.Name}." : $"The prophet who broke {faith.Name} away from {parent.Name}.";
        if (realms.Count > 0 && sim.Chance(0.4)) sim.Adopt(sim.Pick(realms), faith, "converted by its prophet");
    }

    private static void Convert(HistorySimulation sim, List<Realm> realms)
    {
        var convert = sim.Pick(realms);
        var neighbours = realms.Where(o => o != convert && o.StateReligion != null && o.StateReligion != convert.StateReligion
                                           && sim.CanAdoptOpenly(o.StateReligion) && sim.CanReach(convert, o)).ToList();
        if (neighbours.Count == 0) return;
        sim.Adopt(convert, sim.Pick(neighbours).StateReligion!, "converted by its neighbours");
    }

    /// <summary>The strongest realm bans a faith no realm keeps; its people take it underground, or lose it.</summary>
    private static void Persecute(HistorySimulation sim, List<Realm> realms)
    {
        var persecutor = realms.Where(r => r.StateReligion != null).OrderByDescending(r => r.Regions.Count).ThenBy(r => r.Name, StringComparer.Ordinal).FirstOrDefault();
        if (persecutor == null) return;
        var held = sim.History.LivingRealms.Select(r => r.StateReligion).ToHashSet();
        var minorities = sim.History.LivingFaiths.Where(f => sim.History.PresenceOf(f) == FaithPresence.Open && !held.Contains(f)
                                                            && !sim.IsProscribed(f)).ToList();
        if (minorities.Count == 0) return;
        var victim = sim.Pick(minorities);
        sim.Proscribe(persecutor, victim, $"{persecutor.Name} will suffer no rival to {persecutor.StateReligion!.Name}", extinguishChance: 0.3);
    }

    /// <summary>A faith no realm keeps and nobody hunts thins out and is forgotten.</summary>
    private static void Fade(HistorySimulation sim)
    {
        var held = sim.History.LivingRealms.Select(r => r.StateReligion).ToHashSet();
        var orphans = sim.History.LivingFaiths.Where(f => sim.History.PresenceOf(f) == FaithPresence.Open && !held.Contains(f)
                                                         && f.Scope == HistoryScope.World).ToList();
        if (orphans.Count == 0) return;
        var faith = sim.Pick(orphans);
        sim.DieOut(faith, $"{faith.Name} fades: its temples are left to the weather, and the last who kept it die unremembered.");
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new ReligionPulseSeed(sim.Later(200, 600));
}

/// <summary>A realm builds: a town, a port, a fortress, a temple, a mine, as its country allows.</summary>
public sealed class PlacePulseSeed : PulseSeed
{
    public PlacePulseSeed(int due) : base(due) { }

    protected override void Act(HistorySimulation sim)
    {
        var realms = sim.History.LivingRealms.ToList();
        if (realms.Count == 0) return;
        var realm = sim.Pick(realms);
        int region = sim.Pick(realm.Regions.ToList());
        var kind = RealmGenerator.DrawPlaceKind(sim.Rng, sim.Regions[region], sim.Now);
        sim.NewPlace(kind, region, realm);
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new PlacePulseSeed(sim.Later(25, 70));
}

/// <summary>Plague, famine, flood, earthquake, fire: the world's own misfortunes.</summary>
public sealed class CatastrophePulseSeed : PulseSeed
{
    public CatastrophePulseSeed(int due) : base(due) { }

    private static readonly (string Title, string Text, DeathCause Cause)[] Kinds =
    {
        ("A plague", "A plague sweeps through {0}.", DeathCause.Plague),
        ("A famine", "The harvests fail in {0}, and famine follows.", DeathCause.Illness),
        ("A great flood", "The rivers of {0} break their banks.", DeathCause.Accident),
        ("An earthquake", "The ground shakes in {0}, and towns fall.", DeathCause.Accident),
        ("A great fire", "Fire takes the chief town of {0}.", DeathCause.Fire),
        ("A hard cold", "The cold lasts twice its time in {0}.", DeathCause.Illness),
    };

    protected override void Act(HistorySimulation sim)
    {
        var realms = sim.History.LivingRealms.ToList();
        if (realms.Count == 0) return;
        var realm = sim.Pick(realms);
        var (title, text, cause) = Kinds[sim.Rng.Next(Kinds.Length)];
        // No title: the sentence says it all, and "A plague. A plague sweeps..." reads as a stammer.
        sim.Chronicle("", string.Format(text, realm.Name), realm);
        if (realm.Ruler is { Scope: HistoryScope.World } ruler && sim.Chance(0.25)) sim.Kill(ruler, cause);
        var places = sim.History.Places.Where(p => realm.Regions.Contains(p.Region) && !p.Ruined.IsKnown).ToList();
        if (places.Count > 0 && sim.Chance(0.2)) sim.Ruin(sim.Pick(places), title.ToLowerInvariant());
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new CatastrophePulseSeed(sim.Later(80, 250));
}

/// <summary>
/// A lore beat: something the lore says happened on this world on this date, with whatever it does to
/// the world. The lore-world profiles are mostly made of these.
/// </summary>
public sealed class ScriptedSeed : HistorySeed
{
    private readonly Action<HistorySimulation> _act;
    private readonly int _priority;

    public ScriptedSeed(int due, Action<HistorySimulation> act, int priority = 30) : base(due)
    {
        _act = act;
        _priority = priority;
    }

    public override int Priority => _priority;

    public override void Sprout(HistorySimulation sim) => _act(sim);
}
