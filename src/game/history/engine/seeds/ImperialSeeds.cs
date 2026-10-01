using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.History.Generation;

namespace Cathedral.Game.History.Engine.Seeds;

// The empire's dealings with a world, from the first ship down the vortex to the breaking of the
// province after the Hatching, and the long afterwards. Sown by the world's profile according to its
// imperial status; the lore-world profiles sow the same seeds at the lore's own dates.

/// <summary>The faith the empire carried at a given round.</summary>
public static class ImperialFaith
{
    public static Religion At(HistorySimulation sim, int round)
    {
        var e = sim.Empire;
        if (round < 262) return e.Principism;
        if (round < 1004) return e.EarlyQothism;
        if (round < HistoryCalendar.InquisitionRound)
            return sim.Pick(new[] { e.GenesiveQothism, e.FigurativeQothism, e.PrincipativeQothism });
        // The Inquisition's own church most of the time; the other five permitted faiths now and then.
        return sim.Chance(0.6) ? e.GenesiveQothism : sim.Pick(e.PermittedFaiths);
    }
}

/// <summary>The first imperial ship comes down the vortex. On a world never held, trade and missions follow.</summary>
public sealed class ImperialContactSeed : HistorySeed
{
    public ImperialContactSeed(int due, HistoricInfo? agent = null, string? detail = null) : base(due)
    {
        Agent = agent;
        Detail = detail;
    }

    public HistoricInfo? Agent { get; }
    public string? Detail { get; }

    public override void Sprout(HistorySimulation sim)
    {
        var world = sim.History.World;
        var captain = Agent ?? sim.NewAdult(sim.Empire.IISTG, imperial: true,
            why: $"Captain of the first imperial submarine to come down the vortex of {sim.History.World.Name}.");
        sim.Record(new ImperialEvent(sim.Today, HistoryScope.World, ImperialStage.Contact, world, captain,
            Detail ?? $"An imperial submarine under {captain.Name} comes down the vortex of {world.Name}."));

        // Every ship that came down the vortex could have carried something forbidden: a few of them did.
        int window = HistoryCalendar.HatchingRound - 1 - sim.Now;
        if (window > 5)
        {
            for (int i = sim.Rng.Next(1, 4); i > 0; i--)
                sim.Sow(new ClandestineArrivalSeed(sim.Now + sim.Rng.Next(5, window + 1)));
            for (int i = sim.Rng.Next(1, 3); i > 0; i--)
                sim.Sow(new ClandestineCellSeed(sim.Now + sim.Rng.Next(5, window + 1)));
        }

        if (world.Status == ImperialStatus.Visited)
        {
            sim.Sow(new TradingPostSeed(sim.Later(1, 25)));
            sim.Sow(new ImperialBranchSeed(sim.Later(1, 30), e => e.IISTG, needsCoast: true));
            sim.Sow(new MissionarySeed(sim.Later(3, 40)));
            if (sim.Chance(0.5)) sim.Sow(new MissionarySeed(sim.Later(40, 200)));
        }
    }
}

/// <summary>The IISTG plants a trading post in a coastal region.</summary>
public sealed class TradingPostSeed : HistorySeed
{
    public TradingPostSeed(int due) : base(due) { }

    public override bool CanSprout(HistorySimulation sim)
        => sim.Now < HistoryCalendar.HatchingRound && sim.Regions.Any(r => r.Coastal);

    public override void Sprout(HistorySimulation sim)
    {
        int region = sim.Pick(sim.Regions.Where(r => r.Coastal).Select(r => r.Id).ToList());
        sim.NewPlace(PlaceKind.Port, region, sim.Empire.IISTG, $"the {sim.History.RegionNames[region]} trading post");
    }
}

/// <summary>Missionaries of the empire's faith win over a realm.</summary>
public sealed class MissionarySeed : HistorySeed
{
    public MissionarySeed(int due) : base(due) { }

    public override bool CanSprout(HistorySimulation sim)
        => sim.Now < HistoryCalendar.HatchingRound && sim.History.LivingRealms.Any();

    public override void Sprout(HistorySimulation sim)
    {
        var realm = sim.Pick(sim.History.LivingRealms.ToList());
        var faith = sim.Present(ImperialFaith.At(sim, sim.Now));
        sim.Adopt(realm, faith, "won over by imperial missionaries");
    }
}

/// <summary>
/// The conquest begins: a province is proclaimed, a governor named, and every free realm is given a
/// date to fall by. <paramref name="provinceName"/> and <paramref name="provinceRuler"/> let a lore
/// world make its province a kingdom (Prunil under Rosena) or a dominion (Oox's worlds).
/// </summary>
public sealed class ImperialConquestSeed : HistorySeed
{
    public ImperialConquestSeed(int due, int duration, HistoricInfo? conqueror = null, string? provinceName = null,
                                HistoricFigure? provinceRuler = null, Government government = Government.ImperialProvince,
                                Religion? faith = null, bool imperialRule = true) : base(due)
    {
        Duration = Math.Max(1, duration);
        Conqueror = conqueror;
        ProvinceName = provinceName;
        ProvinceRuler = provinceRuler;
        Government = government;
        Faith = faith;
        ImperialRule = imperialRule;
    }

    /// <summary>
    /// False for a conquest that is not the empire's (Oox's dominions): the world is taken, but no
    /// Inquisition, governors or imperial works follow until someone sows them.
    /// </summary>
    public bool ImperialRule { get; }

    public int Duration { get; }
    public HistoricInfo? Conqueror { get; }
    public string? ProvinceName { get; }
    public HistoricFigure? ProvinceRuler { get; }
    public Government Government { get; }
    public Religion? Faith { get; }

    public override void Sprout(HistorySimulation sim)
    {
        var world = sim.History.World;
        var province = new Realm(ProvinceName ?? $"the Province of {world.Name}", Government, HistoryScope.World)
        {
            Founded = sim.Today,
        };
        sim.History.Realms.Add(province);
        sim.History.Province = province;
        province.Founder = ProvinceRuler ?? sim.NewAdult(province, imperial: true,
            why: $"First governor of {province.Name}, who led the conquest of {world.Name}.");
        province.StateReligion = sim.Present(Faith ?? ImperialFaith.At(sim, sim.Now));

        string by = Conqueror == null ? "The empire" : Conqueror.Name;
        sim.Record(new ImperialEvent(sim.Today, HistoryScope.World, ImperialStage.ConquestBegun, world, Conqueror,
            $"{by} begins the conquest of {world.Name} and proclaims {province.Name}."));
        sim.Record(new FactionFoundedEvent(sim.Today, HistoryScope.World, province));
        sim.Crown(province, province.Founder);

        foreach (var realm in sim.History.LivingRealms.Where(r => r != province).ToList())
            sim.Sow(new ConquestCampaignSeed(sim.Later(0, Duration), realm, province, Conqueror));
        sim.Sow(new ConquestCompleteSeed(sim.Now + Duration + 1, province, Conqueror, ImperialRule));
    }
}

/// <summary>One free realm falls to the province: by battle, or by submission.</summary>
public sealed class ConquestCampaignSeed : HistorySeed
{
    public ConquestCampaignSeed(int due, Realm target, Realm province, HistoricInfo? conqueror) : base(due)
    {
        Target = target;
        Province = province;
        Conqueror = conqueror;
    }

    public Realm Target { get; }
    public Realm Province { get; }
    public HistoricInfo? Conqueror { get; }

    public override bool CanSprout(HistorySimulation sim)
        => !Target.Dissolved.IsKnown && Target.Regions.Count > 0 && !Province.Dissolved.IsKnown;

    public override void Sprout(HistorySimulation sim)
    {
        if (sim.Chance(0.3))
        {
            sim.Chronicle("A submission", $"{Target.Name} submits to {Province.Name} without a fight.", Target, Province);
            sim.Transfer(Target.Regions.ToList(), Province, "submitted");
            return;
        }

        var war = new War($"the conquest of {RealmGenerator.ShortName(Target)}", HistoryScope.World)
        {
            Started = sim.Today, Ended = sim.Today, Outcome = WarOutcome.AttackerWon,
        };
        war.Attackers.Add(Province);
        war.Defenders.Add(Target);
        sim.History.Wars.Add(war);
        sim.Record(new WarStartedEvent(sim.Today, HistoryScope.World, war, "the imperial conquest"));
        string site = sim.History.RegionNames[Target.CapitalRegion >= 0 ? Target.CapitalRegion : Target.Regions.Min];
        sim.Record(new BattleEvent(sim.Today, HistoryScope.World, war, $"The fall of {site}", Province, Target));
        var ruler = Target.Ruler;
        sim.Transfer(Target.Regions.ToList(), Province, "by conquest");
        sim.Record(new WarEndedEvent(sim.Today, HistoryScope.World, war));
        if (ruler != null && sim.Chance(0.5)) sim.Kill(ruler, sim.Chance(0.5) ? DeathCause.Battle : DeathCause.Execution);
        if (sim.Chance(0.25)) sim.Sow(new RevoltSeed(sim.Later(10, 80)));
    }
}

/// <summary>The last free land is annexed and the world is a province. Imperial rule settles in.</summary>
public sealed class ConquestCompleteSeed : HistorySeed
{
    public ConquestCompleteSeed(int due, Realm province, HistoricInfo? conqueror, bool imperialRule = true) : base(due)
    {
        Province = province;
        Conqueror = conqueror;
        ImperialRule = imperialRule;
    }

    public Realm Province { get; }
    public HistoricInfo? Conqueror { get; }
    public bool ImperialRule { get; }

    public override int Priority => 60;

    public override bool CanSprout(HistorySimulation sim) => !Province.Dissolved.IsKnown;

    public override void Sprout(HistorySimulation sim)
    {
        foreach (var realm in sim.History.LivingRealms.Where(r => r != Province).ToList())
            sim.Transfer(realm.Regions.ToList(), Province, "annexed");
        var open = sim.UnclaimedRegions.ToList();
        if (open.Count > 0) sim.Transfer(open, Province, "annexed");

        var world = sim.History.World;
        sim.Record(new ImperialEvent(sim.Today, HistoryScope.World, ImperialStage.ConquestCompleted, world, Conqueror,
            $"The last free land of {world.Name} is annexed; the world is {Province.Name}."));

        if (ImperialRule) SowImperialRule(sim, Province);
    }

    /// <summary>What an imperial province gets once it is whole: the Inquisition, governors, risings, works.</summary>
    public static void SowImperialRule(HistorySimulation sim, Realm province)
    {
        sim.Sow(new InquisitionSeed(Math.Max(HistoryCalendar.InquisitionRound, sim.Later(1, 10)), province));
        sim.Sow(new GovernorRotationSeed(sim.Later(8, 35), province));
        sim.Sow(new RevoltPulseSeed(sim.Later(30, 120)));
        sim.Sow(new ImperialWorksSeed(Math.Max(HistoryCalendar.InquisitionRound + 1, sim.Later(2, 20)), PlaceKind.Commandery));
        sim.Sow(new ImperialWorksSeed(Math.Max(HistoryCalendar.InquisitionRound + 1, sim.Later(5, 40)), PlaceKind.ImperialSchool));
        sim.Sow(new ImperialBranchSeed(Math.Max(HistoryCalendar.InquisitionRound + 1, sim.Later(2, 30)), e => e.PlebeianTribunal));
        sim.Sow(new ImperialBranchSeed(Math.Max(HistoryCalendar.InquisitionRound + 2, sim.Later(2, 25)), e => e.Knights, needsCoast: true));
        sim.Sow(new ImperialBranchSeed(sim.Later(0, 20), e => e.IISTG, needsCoast: true));
    }
}

/// <summary>
/// The Principian Inquisition arrives: native faiths proscribed, their temples torn down and a
/// Fogunian temple raised on the rubble, the unconverted buried alive.
/// </summary>
public sealed class InquisitionSeed : HistorySeed
{
    public InquisitionSeed(int due, Realm province) : base(due) => Province = province;

    public Realm Province { get; }

    public override bool CanSprout(HistorySimulation sim)
        => sim.History.Province != null && sim.Now < HistoryCalendar.HatchingRound;

    public override void Sprout(HistorySimulation sim)
    {
        var province = sim.History.Province!;
        var world = sim.History.World;
        sim.Record(new ImperialEvent(sim.Today, HistoryScope.World, ImperialStage.InquisitionArrives, world,
            sim.Empire.Inquisition, $"The Principian Inquisition comes to {world.Name}."));

        // Everything but the six permitted faiths: the natives' gods, and any empire faith the Cosmic
        // Empire had come to forbid (Medusosianism, the Fallen God, Beatildism).
        var permitted = sim.Empire.PermittedFaiths;
        foreach (var faith in sim.History.LivingFaiths.Where(r => !permitted.Contains(r)).ToList())
            sim.Proscribe(province, faith, "by the Inquisition", extinguishChance: 0.25);
        new ImperialBranchSeed(sim.Now, e => e.Inquisition).Sprout(sim);
        Crackdown.Apply(sim, province);

        if (province.StateReligion == null || !sim.Empire.PermittedFaiths.Contains(province.StateReligion))
            sim.Adopt(province, sim.Present(ImperialFaith.At(sim, sim.Now)), "imposed by the Inquisition");

        var shrines = sim.History.Places.Where(p => p.Kind is PlaceKind.Temple or PlaceKind.Sanctuary && !p.Ruined.IsKnown).ToList();
        foreach (var shrine in shrines) sim.Ruin(shrine, "torn down by the Inquisition");

        var regions = province.Regions.ToList();
        if (regions.Count == 0) return;
        int site = shrines.Count > 0 ? shrines[0].Region : province.CapitalRegion >= 0 ? province.CapitalRegion : regions[0];
        sim.NewPlace(PlaceKind.ImperialTemple, site, sim.Empire.Inquisition, $"the Fogunian temple of {sim.History.RegionNames[site]}");
        if (sim.Chance(0.6))
        {
            int field = sim.Pick(regions);
            sim.NewPlace(PlaceKind.BurialField, field, sim.Empire.Inquisition, $"the burial field of {sim.History.RegionNames[field]}");
            sim.Chronicle("The sowing", $"The unconverted of {sim.History.RegionNames[field]} are buried alive by the Inquisition.", province);
        }
    }
}

/// <summary>A new governor arrives from Pyr. Rotates until the Hatching.</summary>
public sealed class GovernorRotationSeed : HistorySeed
{
    public GovernorRotationSeed(int due, Realm province) : base(due) => Province = province;

    public Realm Province { get; }

    public override bool CanSprout(HistorySimulation sim)
        => !Province.Dissolved.IsKnown && sim.Now < HistoryCalendar.HatchingRound
        && Province.Government == Government.ImperialProvince;

    public override void Sprout(HistorySimulation sim)
    {
        sim.Crown(Province, sim.NewAdult(Province, imperial: true, why: $"A governor sent from Pyr to rule {Province.Name}."));
        sim.Sow(new GovernorRotationSeed(sim.Later(8, 35), Province));
    }
}

/// <summary>An imperial work is built: a Knights' commandery by the sea, an imperial school.</summary>
public sealed class ImperialWorksSeed : HistorySeed
{
    public ImperialWorksSeed(int due, PlaceKind kind) : base(due) => Kind = kind;

    public PlaceKind Kind { get; }

    public override bool CanSprout(HistorySimulation sim)
        => sim.History.Province is { Regions.Count: > 0 } && sim.Now < HistoryCalendar.HatchingRound
        && (Kind != PlaceKind.Commandery || sim.History.Province.Regions.Any(r => sim.Regions[r].Coastal));

    public override void Sprout(HistorySimulation sim)
    {
        var province = sim.History.Province!;
        var regions = province.Regions.Where(r => Kind != PlaceKind.Commandery || sim.Regions[r].Coastal).ToList();
        int region = sim.Pick(regions);
        string name = Kind == PlaceKind.Commandery
            ? $"the commandery of {sim.History.RegionNames[region]}"
            : $"the imperial school of {sim.History.RegionNames[region]}";
        HistoricFaction by = Kind == PlaceKind.Commandery ? sim.Empire.Knights : sim.Empire.Empire;
        sim.NewPlace(Kind, region, by, name);
    }
}

/// <summary>From time to time the natives of a held world rise. Stops at the Hatching.</summary>
public sealed class RevoltPulseSeed : PulseSeed
{
    public RevoltPulseSeed(int due) : base(due) { }

    protected override void Act(HistorySimulation sim)
    {
        if (sim.Chance(0.5)) new RevoltSeed(sim.Now).TrySprout(sim);
    }

    protected override HistorySeed? Next(HistorySimulation sim)
        => sim.Now < HistoryCalendar.HatchingRound ? new RevoltPulseSeed(sim.Later(40, 160)) : null;
}

/// <summary>A native rising against the province: a breakaway, a war, and nearly always a reconquest.</summary>
public sealed class RevoltSeed : HistorySeed
{
    public RevoltSeed(int due) : base(due) { }

    public override bool CanSprout(HistorySimulation sim)
        => sim.History.Province is { Regions.Count: >= 2 } && sim.Now < HistoryCalendar.HatchingRound;

    internal void TrySprout(HistorySimulation sim)
    {
        if (CanSprout(sim)) Sprout(sim);
    }

    public override void Sprout(HistorySimulation sim)
    {
        var province = sim.History.Province!;
        var candidates = province.Regions.Where(r => r != province.CapitalRegion).ToList();
        if (candidates.Count == 0) return;
        var part = sim.GrowCluster(sim.Pick(candidates), sim.Rng.Next(1, 4),
                                   r => province.Regions.Contains(r) && r != province.CapitalRegion);
        var leader = sim.NewAdult(null, minYearsLeft: 2, maxYearsLeft: 30,
            why: $"Led a native rising against {province.Name}.");
        var rebels = sim.Split(province, part, "a native rising", leader, Government.Kingdom);
        var natives = sim.History.Religions.Where(r => r.Scope == HistoryScope.World).ToList();
        if (natives.Count > 0) sim.Adopt(rebels, sim.Pick(natives), "the old faith, restored by the rising");
        sim.Sow(new RevoltEndSeed(sim.Later(1, 8), rebels, province));
    }
}

/// <summary>The rising is crushed (or, rarely, holds until the empire has other worries).</summary>
public sealed class RevoltEndSeed : HistorySeed
{
    public RevoltEndSeed(int due, Realm rebels, Realm province) : base(due)
    {
        Rebels = rebels;
        Province = province;
    }

    public Realm Rebels { get; }
    public Realm Province { get; }

    public override bool CanSprout(HistorySimulation sim)
        => !Rebels.Dissolved.IsKnown && !Province.Dissolved.IsKnown && Rebels.Regions.Count > 0;

    public override void Sprout(HistorySimulation sim)
    {
        if (sim.Chance(0.1) && sim.Now > HistoryCalendar.HatchingRound - 60)
        {
            sim.Chronicle("An unbroken rising", $"{Rebels.Name} holds out against {Province.Name}.", Rebels, Province);
            return;
        }
        var leader = Rebels.Ruler;
        sim.Transfer(Rebels.Regions.ToList(), Province, "reconquered");
        if (leader != null)
            sim.Kill(leader, sim.Now >= HistoryCalendar.InquisitionRound ? DeathCause.BuriedAlive : DeathCause.Execution);
    }
}

/// <summary>
/// Pyr hatches. On a held world the ships stop coming and the province is stranded; on a visited
/// world the traders stop coming; on an unvisited one a light goes out of the sky.
/// </summary>
public sealed class HatchingSeed : HistorySeed
{
    public HatchingSeed() : base(HistoryCalendar.HatchingRound) { }

    public override int Priority => 0;

    public override void Sprout(HistorySimulation sim)
    {
        var world = sim.History.World;
        switch (world.Status)
        {
            case ImperialStatus.Held:
                sim.Record(new ImperialEvent(sim.Today, HistoryScope.World, ImperialStage.Stranding, world, sim.Empire.Pyr,
                    $"Pyr's light goes out of the sky of {world.Name}. No ship comes again."));
                sim.History.Profile.SowStranding(sim);
                foreach (var place in sim.History.Places.Where(p => p.IsImperialWork && !p.Ruined.IsKnown))
                    if (sim.Chance(place.Kind == PlaceKind.Port ? 0.5 : 0.8))
                        sim.Sow(new RuinSeed(sim.Later(15, 350), place, "abandoned after the empire"));
                sim.Sow(new NativeRevivalSeed(sim.Later(10, 120)));
                if (sim.Chance(0.5)) sim.Sow(new NativeRevivalSeed(sim.Later(100, 400)));
                break;
            case ImperialStatus.Visited:
                sim.Chronicle("The ships stop", $"A light goes out of the sky of {world.Name}, and the imperial ships never come again.", world);
                foreach (var post in sim.History.Places.Where(p => p.IsImperialWork && !p.Ruined.IsKnown))
                    sim.Sow(new RuinSeed(sim.Later(5, 150), post, "abandoned when the ships stopped"));
                break;
            default:
                sim.Chronicle("A light goes out", $"A bright star vanishes from the sky of {world.Name}.", world);
                break;
        }
        foreach (var branch in sim.History.LivingOrganisations.Where(o => o.Kind == OrganisationKind.ImperialBranch).ToList())
            sim.Sow(new BranchAfterHatchingSeed(sim.Later(0, 40), branch));
        foreach (var cell in sim.History.LivingOrganisations.Where(o => o.Kind != OrganisationKind.ImperialBranch && o.ImperialCounterpart != null).ToList())
            sim.Sow(new CellAfterHatchingSeed(sim.Later(0, 60), cell));
        sim.Sow(new GoblinSeed(sim.History.Profile.GoblinRound(sim)));
    }
}

/// <summary>
/// The stranded province becomes a kingdom under its last governor, and then comes apart over the
/// following decades; a Knights' commandery becomes a realm of its own.
/// </summary>
public sealed class StrandingSeed : HistorySeed
{
    public StrandingSeed(int due, string? kingdomName = null) : base(due) => KingdomName = kingdomName;

    public string? KingdomName { get; }

    public override bool CanSprout(HistorySimulation sim) => sim.History.Province is { Regions.Count: > 0 };

    public override void Sprout(HistorySimulation sim)
    {
        var province = sim.History.Province!;
        var governor = province.Ruler;
        var kingdom = sim.FoundRealm(province.Regions.ToList(), Government.Kingdom,
                                     governor is { Scope: HistoryScope.World } g && g.IsAliveAt(sim.Now) ? g : null,
                                     name: KingdomName ?? $"the Kingdom of {sim.History.World.Name}", predecessor: province);
        sim.Chronicle("The Stranding", $"Cut off from Pyr, {province.Name} makes itself {kingdom.Name}.", province, kingdom);
        sim.History.Province = null;

        int pieces = Math.Max(1, kingdom.Regions.Count / 4);
        for (int i = 0; i < pieces; i++)
            sim.Sow(new RealmSplitSeed(sim.Later(3, 90), kingdom, sim.Chance(0.5) ? "a governor's revolt" : "the old kingdoms restored",
                                       sim.Chance(0.4) ? Government.Duchy : null, forced: true));

        foreach (var commandery in sim.History.Places.Where(p => p.Kind == PlaceKind.Commandery && !p.Ruined.IsKnown))
            sim.Sow(new CommanderySecessionSeed(sim.Later(0, 25), commandery));
    }
}

/// <summary>The Knights of a stranded commandery make their fort a realm.</summary>
public sealed class CommanderySecessionSeed : HistorySeed
{
    public CommanderySecessionSeed(int due, Place commandery) : base(due) => Commandery = commandery;

    public Place Commandery { get; }

    public override bool CanSprout(HistorySimulation sim)
        => sim.History.OwnerOf(Commandery.Region) is { Government: not Government.Commandery } owner
        && owner.Regions.Count >= 2 && owner.CapitalRegion != Commandery.Region;

    public override void Sprout(HistorySimulation sim)
    {
        var owner = sim.History.OwnerOf(Commandery.Region)!;
        var commander = sim.NewAdult(null, imperial: true,
            why: $"Commander of the Knights at {Commandery.Name}, who made the stranded fort a realm.");
        sim.Split(owner, new[] { Commandery.Region }, "the Knights of the commandery", commander, Government.Commandery);
    }
}

public sealed class RuinSeed : HistorySeed
{
    public RuinSeed(int due, Place place, string how) : base(due)
    {
        Place = place;
        How = how;
    }

    public Place Place { get; }
    public string How { get; }

    public override bool CanSprout(HistorySimulation sim) => !Place.Ruined.IsKnown;

    public override void Sprout(HistorySimulation sim) => sim.Ruin(Place, How);
}

/// <summary>After the empire, a proscribed native faith comes out of hiding and a realm takes it back.</summary>
public sealed class NativeRevivalSeed : HistorySeed
{
    public NativeRevivalSeed(int due) : base(due) { }

    public override bool CanSprout(HistorySimulation sim)
        => Candidates(sim).Count > 0 && sim.History.LivingRealms.Any();

    // Ordered by position in the world's faith list: the set's own order is not something a seeded
    // history may depend on. A faith this world may never hold openly stays hidden.
    private static List<Religion> Candidates(HistorySimulation sim)
        => sim.History.Religions.Where(r => sim.History.PresenceOf(r) == FaithPresence.Clandestine
                                         && sim.History.Profile.MayHoldOpenly(r)).ToList();

    public override void Sprout(HistorySimulation sim)
    {
        var faith = sim.Pick(Candidates(sim));
        sim.Surface(faith, sim.Pick(sim.History.LivingRealms.ToList()), "revived after the empire");
    }
}

/// <summary>Goblins are first seen: in the deep places, and then spreading.</summary>
public sealed class GoblinSeed : HistorySeed
{
    public GoblinSeed(int due, bool first = true) : base(due) => First = first;

    public bool First { get; }

    public override bool CanSprout(HistorySimulation sim) => sim.Regions.Count > 0;

    public override void Sprout(HistorySimulation sim)
    {
        var deep = sim.Regions.Where(r => r.MountainCells > 0).Select(r => r.Id).ToList();
        int region = deep.Count > 0 && sim.Chance(0.7) ? sim.Pick(deep) : sim.Pick(sim.Regions.Select(r => r.Id).ToList());
        var owner = sim.History.OwnerOf(region);
        string where = sim.History.RegionNames[region];
        if (First)
            sim.Chronicle("The first goblins", $"Small, green, pig-nosed creatures are seen in the deep places of {where}. No one knows what they are.",
                          owner == null ? Array.Empty<HistoricInfo>() : new HistoricInfo[] { owner });
        else
            sim.Chronicle("", $"Goblins raid the farms of {where}.", owner == null ? Array.Empty<HistoricInfo>() : new HistoricInfo[] { owner });
        sim.Sow(new GoblinSeed(sim.Later(20, 90), first: false));
    }
}
