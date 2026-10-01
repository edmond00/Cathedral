using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.History.Generation;

namespace Cathedral.Game.History.Engine.Seeds;

// Orders, guilds, companies and societies: what a world builds besides realms and faiths. Native ones
// are founded where the world can carry them (guilds where there are towns, pirates where there is a
// coast, knights where there are realms worth serving); imperial ones arrive as branches or as hidden
// cells; all of them can be chartered, outlawed, go underground, resurface, split, be absorbed or die.

/// <summary>A new organisation, of a kind the world can carry at this moment.</summary>
public sealed class OrganisationPulseSeed : PulseSeed
{
    public OrganisationPulseSeed(int due) : base(due) { }

    /// <summary>How many organisations a world keeps at once, at most: a few, and more on a larger world.</summary>
    internal static int Cap(HistorySimulation sim) => Math.Max(4, sim.Regions.Count / 5);

    protected override void Act(HistorySimulation sim)
    {
        if (sim.History.LivingOrganisations.Count() >= Cap(sim)) return;
        var realms = sim.History.LivingRealms.ToList();
        if (realms.Count == 0) return;

        var towns = sim.History.Places.Where(p => !p.Ruined.IsKnown && p.Kind is PlaceKind.City or PlaceKind.Town or PlaceKind.Port).ToList();
        var hidden = sim.History.LivingFaiths.Where(f => sim.History.PresenceOf(f) == FaithPresence.Clandestine).ToList();
        var heldFaiths = realms.Select(r => r.StateReligion).Where(f => f != null).Distinct().ToList();
        bool atWar = sim.History.Wars.Any(w => !w.Ended.IsKnown);
        int now = sim.Now;

        var table = new List<(OrganisationKind, int)>
        {
            (OrganisationKind.KnightlyOrder,     now > -600 && realms.Any(r => r.Regions.Count >= 2) ? 8 : 0),
            (OrganisationKind.ArtisanGuild,      towns.Count > 0 ? 12 : 0),
            (OrganisationKind.MerchantGuild,     towns.Any(t => t.Kind is PlaceKind.Port or PlaceKind.City) ? 9 : 0),
            (OrganisationKind.MinersGuild,       sim.Regions.Any(r => r.MountainCells > 0) ? 5 : 0),
            (OrganisationKind.MonasticOrder,     heldFaiths.Count > 0 ? 8 : 0),
            (OrganisationKind.ScholarsCollege,   now > -300 && towns.Any(t => t.Kind == PlaceKind.City) ? 5 : 0),
            (OrganisationKind.HealersGuild,      towns.Count > 0 ? 4 : 0),
            (OrganisationKind.BardsCompany,      4),
            (OrganisationKind.HuntersLodge,      sim.Regions.Any(r => r.IsForested) ? 5 : 0),
            (OrganisationKind.MercenaryCompany,  atWar ? 7 : 1),
            (OrganisationKind.PirateBrotherhood, sim.Regions.Any(r => r.Coastal) ? 4 : 0),
            (OrganisationKind.ThievesGuild,      towns.Count > 0 ? 4 : 0),
            (OrganisationKind.AssassinsGuild,    towns.Any(t => t.Kind == PlaceKind.City) ? 2 : 0),
            (OrganisationKind.SecretSociety,     hidden.Count > 0 ? 7 : 3),
        };
        var kind = ReligionGenerator.Weighted(sim.Rng, table.Where(t => t.Item2 > 0).ToList());

        // Where it sits: in a town for the town trades, in the right country for the rest.
        int region = kind switch
        {
            OrganisationKind.MinersGuild       => sim.Pick(sim.Regions.Where(r => r.MountainCells > 0).Select(r => r.Id).ToList()),
            OrganisationKind.HuntersLodge      => sim.Pick(sim.Regions.Where(r => r.IsForested).Select(r => r.Id).ToList()),
            OrganisationKind.PirateBrotherhood => sim.Pick(sim.Regions.Where(r => r.Coastal).Select(r => r.Id).ToList()),
            _ when towns.Count > 0 && kind is not (OrganisationKind.KnightlyOrder or OrganisationKind.BardsCompany or OrganisationKind.SecretSociety)
                                               => sim.Pick(towns).Region,
            _                                  => sim.Pick(sim.Pick(realms).Regions.ToList()),
        };
        var owner = sim.History.OwnerOf(region);

        // Outlaws are founded outside the law; the rest under the realm they sit in.
        bool outlaw = kind is OrganisationKind.ThievesGuild or OrganisationKind.AssassinsGuild or OrganisationKind.PirateBrotherhood;
        Religion? faith = null;
        bool clandestine = outlaw && sim.Chance(0.6);
        if (kind == OrganisationKind.MonasticOrder)
            faith = owner?.StateReligion ?? sim.Pick(heldFaiths)!;
        if (kind == OrganisationKind.SecretSociety && hidden.Count > 0 && sim.Chance(0.6))
        {
            faith = sim.Pick(hidden);
            clandestine = true;
        }
        else if (kind == OrganisationKind.SecretSociety) clandestine = sim.Chance(0.5);

        sim.FoundOrganisation(kind, region, outlaw || clandestine ? null : owner, faith: faith, clandestine: clandestine);
    }

    protected override HistorySeed? Next(HistorySimulation sim) => new OrganisationPulseSeed(sim.Later(60, 160));
}

/// <summary>
/// What happens to the organisations a world has: a new master of its home charters it or bans it, a
/// faith it serves is forbidden or dies, it spreads, splits, is absorbed by a rival, comes out of
/// hiding, or simply ends.
/// </summary>
public sealed class OrganisationLifePulseSeed : PulseSeed
{
    public OrganisationLifePulseSeed(int due) : base(due) { }

    protected override void Act(HistorySimulation sim)
    {
        // An imperial branch's fate is the empire's while it stands: no local master charters it, splits
        // it or lets it dwindle. After the Hatching it is decided once, by BranchAfterHatchingSeed.
        var living = sim.History.LivingOrganisations
            .Where(o => o.Kind != OrganisationKind.ImperialBranch || sim.Now >= HistoryCalendar.HatchingRound).ToList();
        if (living.Count == 0) return;
        var org = sim.Pick(living);
        var owner = sim.History.OwnerOf(org.HomeRegion);
        string home = org.HomeRegion >= 0 ? sim.History.RegionNames[org.HomeRegion] : sim.History.World.Name;

        // Its faith first: an order cannot outlive what it serves, and must hide what is hunted.
        if (org.Faith != null)
        {
            if (sim.History.PresenceOf(org.Faith) == FaithPresence.Extinct)
            {
                sim.DissolveOrganisation(org, null, $"its faith, {org.Faith.Name}, is gone");
                return;
            }
            if (!org.Clandestine && sim.IsProscribed(org.Faith))
            {
                sim.ChangeOrganisation(org, OrganisationChange.WentUnderground,
                    $"{org.Name} goes underground with {org.Faith.Name}, now forbidden.", org.Faith);
                return;
            }
        }

        // Its patron gone, or its home taken: the new master decides.
        if (org.Patron != null && (org.Patron.Dissolved.IsKnown || owner != org.Patron))
        {
            if (owner == null || sim.Chance(0.2))
            {
                sim.DissolveOrganisation(org, owner, owner == null ? "its patron is gone and no one takes it up" : "broken up");
                return;
            }
            if (sim.Chance(0.6))
            {
                org.Patron = owner;
                sim.ChangeOrganisation(org, OrganisationChange.Chartered, $"{owner.Name} grants {org.Name} a new charter.", owner);
            }
            else
            {
                org.Patron = null;
                sim.ChangeOrganisation(org, OrganisationChange.Outlawed, $"{owner.Name} outlaws {org.Name}, which goes underground in {home}.", owner);
            }
            return;
        }

        double roll = sim.Rng.NextDouble();
        int age = org.Founded.IsKnown ? sim.Now - org.Founded.Round : 100;
        if (roll < 0.25)
        {
            var other = sim.Pick(sim.Regions.Select(r => r.Id).ToList());
            sim.ChangeOrganisation(org, OrganisationChange.Spread, $"{org.Name} opens a house in {sim.History.RegionNames[other]}.");
        }
        else if (roll < 0.37 && sim.History.LivingOrganisations.Count() < OrganisationPulseSeed.Cap(sim))
        {
            var rival = sim.FoundOrganisation(org.Kind, org.HomeRegion, org.Clandestine ? null : owner, faith: org.Faith, clandestine: org.Clandestine);
            sim.ChangeOrganisation(org, OrganisationChange.Schism, $"{rival.Name} breaks away from {org.Name} over {Quarrel(sim, org)}.", rival);
        }
        else if (roll < 0.47)
        {
            var absorber = living.Where(o => o != org && o.Kind == org.Kind && !o.Clandestine).ToList();
            if (absorber.Count == 0) return;
            var into = sim.Pick(absorber);
            sim.ChangeOrganisation(into, OrganisationChange.Absorbed, $"{into.Name} absorbs {org.Name}.", org);
            sim.DissolveOrganisation(org, into, "absorbed");
        }
        else if (roll < 0.6 && org.Clandestine && owner != null && (org.Faith == null || !sim.IsProscribed(org.Faith))
                 && sim.History.Province is not { Government: Government.ImperialProvince })
        {
            org.Patron = org.Kind is OrganisationKind.ThievesGuild or OrganisationKind.AssassinsGuild or OrganisationKind.PirateBrotherhood ? null : owner;
            sim.ChangeOrganisation(org, OrganisationChange.Surfaced, $"{org.Name} comes out of hiding in {home}.", owner);
        }
        else if (roll < 0.7 && !org.Clandestine && owner != null
                 && org.Kind is OrganisationKind.ThievesGuild or OrganisationKind.SecretSociety or OrganisationKind.AssassinsGuild or OrganisationKind.MercenaryCompany)
        {
            sim.ChangeOrganisation(org, OrganisationChange.Outlawed, $"{owner.Name} outlaws {org.Name}; it goes underground.", owner);
        }
        else if (roll < 0.82 && age > 80 && sim.Chance(Math.Min(0.8, age / 400.0)))
        {
            sim.DissolveOrganisation(org, null, sim.Pick(Ends));
        }
    }

    private static readonly string[] Ends =
    {
        "its last masters die without apprentices", "ruined by debts", "scattered by plague", "its hall burnt and never rebuilt",
        "it dwindles to a name on a guild-roll", "its members drift to other trades",
    };

    // A quarrel that fits the body: doctrine splits the devout, prices split the trades.
    private static string Quarrel(HistorySimulation sim, Organisation org) => sim.Pick(org.Kind switch
    {
        OrganisationKind.MonasticOrder or OrganisationKind.SecretSociety => new[]
            { "a question of doctrine", "a vision one of them had", "the reading of a disputed text", "who is worthy to lead the rites" },
        OrganisationKind.ArtisanGuild or OrganisationKind.MerchantGuild or OrganisationKind.MinersGuild => new[]
            { "the price of its work", "the admission of foreigners", "the treatment of apprentices", "the election of a master" },
        OrganisationKind.KnightlyOrder or OrganisationKind.MercenaryCompany => new[]
            { "the share of the spoils", "a broken oath", "whose banner leads", "a lord they would not serve" },
        _ => new[] { "who sits at the head of the table", "a stolen book", "an inheritance", "a betrayal no one will name" },
    });

    protected override HistorySeed? Next(HistorySimulation sim) => new OrganisationLifePulseSeed(sim.Later(20, 60));
}

/// <summary>
/// A branch of an empire institution opens on the world: an IISTG factor-house, an Inquisition
/// tribunal, a Knights' chapter, a Plebeian court. Lives until the Hatching takes its masters away.
/// </summary>
public sealed class ImperialBranchSeed : HistorySeed
{
    public ImperialBranchSeed(int due, Func<Lore.EmpireLore, HistoricFaction> counterpart, bool needsCoast = false) : base(due)
    {
        Counterpart = counterpart;
        NeedsCoast = needsCoast;
    }

    public Func<Lore.EmpireLore, HistoricFaction> Counterpart { get; }
    public bool NeedsCoast { get; }

    public override bool CanSprout(HistorySimulation sim)
        => sim.Now < HistoryCalendar.HatchingRound && (!NeedsCoast || sim.Regions.Any(r => r.Coastal));

    public override void Sprout(HistorySimulation sim)
    {
        var e = sim.Empire;
        var counterpart = Counterpart(e);
        var province = sim.History.Province;
        var regions = (province?.Regions.ToList() ?? sim.Regions.Select(r => r.Id).ToList())
                      .Where(r => !NeedsCoast || sim.Regions[r].Coastal).ToList();
        if (regions.Count == 0) regions = sim.Regions.Where(r => !NeedsCoast || r.Coastal).Select(r => r.Id).ToList();
        int region = province?.CapitalRegion is int c && c >= 0 && regions.Contains(c) ? c : sim.Pick(regions);
        string where = sim.History.RegionNames[region];

        string name = counterpart == e.Inquisition      ? $"the Tribunal of the Inquisition at {where}"
                    : counterpart == e.Knights          ? $"the Chapter of the Knights of the Cosmic Sea at {where}"
                    : counterpart == e.PlebeianTribunal ? $"the Plebeian Court of {sim.History.World.Name}"
                    : counterpart == e.IISTG            ? $"the IISTG factor-house of {where}"
                    : $"the house of {counterpart.Name} at {where}";
        var org = sim.FoundOrganisation(OrganisationKind.ImperialBranch, region, province, name, counterpart: counterpart,
                                        description: $"A branch of {counterpart.Name} on {sim.History.World.Name}. {counterpart.Description}");
        sim.ChangeOrganisation(org, OrganisationChange.Imported, $"{counterpart.Name} opens {org.Name}.", counterpart);
    }
}

/// <summary>
/// A cell of a forbidden empire institution reaches the world in hiding: a Tickler who stayed, a
/// Beatildist well, a Far Lodge, the remnant of a Red Tribunal. Sown, like smuggled faiths, by contact.
/// </summary>
public sealed class ClandestineCellSeed : HistorySeed
{
    public ClandestineCellSeed(int due) : base(due) { }

    public override bool CanSprout(HistorySimulation sim) => sim.Now < HistoryCalendar.HatchingRound;

    public override void Sprout(HistorySimulation sim)
    {
        var e = sim.Empire;
        int now = sim.Now;
        var options = new List<(HistoricFaction Counterpart, OrganisationKind Kind, Religion? Faith, string[] Texts)>();
        // {0} the new cell. Several ways in per institution, as for smuggled faiths: every one of them
        // somebody with a reason to come down the vortex and stay hidden.
        if (now >= 900)
            options.Add((e.TicklersGuild, OrganisationKind.AssassinsGuild, null, new[]
            {
                "A Tickler sent down the vortex to kill a governor does it, stays, and takes apprentices: {0}.",
                "Two Tickler apprentices fleeing a purge on Pyr sign on as deck-hands and jump ship here; they found {0}.",
                "A Tickler who failed a contract cannot go home. He sells his feather-craft instead: {0}.",
            }));
        if (now >= 1703)
            options.Add((e.BeatildistWells, OrganisationKind.SecretSociety, e.Beatildism, new[]
            {
                "Refugees of the Red Night open a well of the Veiled Empress: {0}.",
                "A sailor's widow, Beatildist since childhood, ties a red thread to her door; those who knock are {0}.",
                "A copy of the Hours of the Veiled Empress, hidden under a homily, finds readers here: {0}.",
            }));
        if (now >= 2820)
            options.Add((e.FarLodges, OrganisationKind.SecretSociety, e.FarLore, new[]
            {
                "An envoy of the Far Lodges gathers the curious among the great: {0}.",
                "A governor's secretary, initiate of a Far Lodge on Pyr, keeps its rites in the residence cellar: {0}.",
            }));
        if (now >= HistoryCalendar.InquisitionRound)
            options.Add((e.RedTribunals, OrganisationKind.SecretSociety, null, new[]
            {
                "Masked judges of the outlawed Red Tribunals, fled from Pyr, sit again in a cellar: {0}.",
                "An old retainer of the Red Brick House, exiled after the Assassination, judges in secret here: {0}.",
            }));
        if (now >= -40)
            options.Add((e.TheWakeful, OrganisationKind.SecretSociety, null, new[]
            {
                "Descendants of the Avolor poor keep a candle for the Awakened Prince: {0}.",
                "A ropemaker from the Ropewalk of Avolor, transported for debt, teaches the Prince's dream of the Garden: {0}.",
            }));
        if (options.Count == 0) return;

        var (counterpart, kind, faith, texts) = sim.Pick(options);
        string text = sim.Pick(texts);
        if (faith != null) sim.Smuggle(faith, $"{faith.Name} comes with the founders of a hidden cell.");
        int region = sim.Pick(sim.Regions.Select(r => r.Id).ToList());
        var org = sim.FoundOrganisation(kind, region, null, counterpart: counterpart, faith: faith, clandestine: true,
                                        description: $"A hidden cell of {counterpart.Name}. {counterpart.Description}");
        sim.ChangeOrganisation(org, OrganisationChange.Imported, string.Format(text, org.Name), counterpart);
    }
}

/// <summary>What the Inquisition does to a world's organisations when it arrives.</summary>
internal static class Crackdown
{
    public static void Apply(HistorySimulation sim, Realm province)
    {
        var permitted = sim.Empire.PermittedFaiths;
        foreach (var org in sim.History.LivingOrganisations.Where(o => o.Kind != OrganisationKind.ImperialBranch).ToList())
        {
            bool heretic = org.Faith != null && !permitted.Contains(org.Faith);
            switch (org.Kind)
            {
                case OrganisationKind.KnightlyOrder:
                    if (sim.Chance(0.5)) sim.DissolveOrganisation(org, sim.Empire.Knights, "absorbed into the Knights of the Cosmic Sea");
                    else if (!org.Clandestine) sim.ChangeOrganisation(org, OrganisationChange.Outlawed, $"{org.Name} refuses the Knights' rule and is outlawed.", province);
                    break;
                case OrganisationKind.MonasticOrder or OrganisationKind.SecretSociety when heretic || org.Kind == OrganisationKind.SecretSociety:
                    if (sim.Chance(0.3)) sim.DissolveOrganisation(org, province, "its members buried alive by the Inquisition");
                    else if (!org.Clandestine) sim.ChangeOrganisation(org, OrganisationChange.WentUnderground, $"{org.Name} goes underground before the grey priests.", province);
                    break;
                case OrganisationKind.ThievesGuild or OrganisationKind.AssassinsGuild or OrganisationKind.PirateBrotherhood:
                    if (!org.Clandestine) sim.ChangeOrganisation(org, OrganisationChange.Outlawed, $"{province.Name} hunts {org.Name} into hiding.", province);
                    break;
                default:
                    if (!org.Clandestine && org.Patron != province)
                    {
                        org.Patron = province;
                        sim.ChangeOrganisation(org, OrganisationChange.Chartered, $"{org.Name} swears to {province.Name} and keeps its charter.", province);
                    }
                    break;
            }
        }
    }
}

/// <summary>
/// After the Hatching, a hidden cell of an empire institution has no masters left to answer to. It
/// goes its own way under the same name (keeping the institution as its lineage), or, without
/// orders and without hope, dwindles away.
/// </summary>
public sealed class CellAfterHatchingSeed : HistorySeed
{
    public CellAfterHatchingSeed(int due, Organisation cell) : base(due) => Cell = cell;

    public Organisation Cell { get; }

    public override bool CanSprout(HistorySimulation sim) => !Cell.Dissolved.IsKnown;

    public override void Sprout(HistorySimulation sim)
    {
        var e = sim.Empire;
        var c = Cell.ImperialCounterpart;
        (double Survive, string Text) fate =
              c == e.TicklersGuild   ? (0.7, $"No contracts come from the Guild on Pyr any more; {Cell.Name} takes its own, and answers to no one.")
            : c == e.BeatildistWells ? (0.8, $"{Cell.Name} reads the end of Pyr as the Veiled Empress's vengeance, and keeps her wells with no word from anywhere.")
            : c == e.FarLodges       ? (0.6, $"Its masters on Pyr gone, {Cell.Name} becomes a society of its own, keeping the Far Lore as it pleases.")
            : c == e.RedTribunals    ? (0.5, $"With no house left to serve, the masked judges of {Cell.Name} sit in judgement for whoever pays.")
            : c == e.TheWakeful      ? (0.8, $"{Cell.Name} keeps its candle for the Awakened Prince, though the city he died in is gone.")
            : (0.5, $"Cut off from Pyr, {Cell.Name} goes its own way.");
        if (!sim.Chance(fate.Survive))
        {
            sim.DissolveOrganisation(Cell, null, "without word from Pyr, it dwindles away");
            return;
        }
        sim.ChangeOrganisation(Cell, OrganisationChange.BecameIndependent, fate.Text,
                               c != null ? new HistoricInfo[] { c } : Array.Empty<HistoricInfo>());
    }
}

/// <summary>
/// After the Hatching, an imperial branch's masters are gone. A Knights' chapter becomes an order of
/// its own, a tribunal a brotherhood or a lynching, a factor-house a guild of salvagers or nothing.
/// </summary>
public sealed class BranchAfterHatchingSeed : HistorySeed
{
    public BranchAfterHatchingSeed(int due, Organisation branch) : base(due) => Branch = branch;

    public Organisation Branch { get; }

    public override bool CanSprout(HistorySimulation sim) => !Branch.Dissolved.IsKnown;

    public override void Sprout(HistorySimulation sim)
    {
        var e = sim.Empire;
        var c = Branch.ImperialCounterpart;
        string where = Branch.HomeRegion >= 0 ? sim.History.RegionNames[Branch.HomeRegion] : sim.History.World.Name;
        var owner = sim.History.OwnerOf(Branch.HomeRegion);

        (OrganisationKind Kind, string[] Names, Religion? Faith, double Survive) next =
              c == e.Knights          ? (OrganisationKind.KnightlyOrder, new[] { $"the Stranded Knights of {where}", "the Order of the Last Ship", $"the Sea-Knights of {where}", "the Knights of the Cold Commandery" }, null, 0.8)
            : c == e.Inquisition      ? (OrganisationKind.MonasticOrder, new[] { $"the Grey Brotherhood of {where}", $"the Sowers of {where}", "the Brothers of the Buried Truth" }, e.GenesiveQothism, 0.4)
            : c == e.IISTG            ? (OrganisationKind.ArtisanGuild, new[] { $"the Hulk-Wrights of {where}", "the Guild of the Rusting Hulls", $"the Vortex-Watchers of {where}" }, null, 0.55)
            : c == e.PlebeianTribunal ? (OrganisationKind.ScholarsCollege, new[] { $"the Plebeian Judges of {where}", "the Keepers of the Written Law", $"the Court of the Old Law at {where}" }, null, 0.5)
            : c == e.PhysiciansOfTheSprings ? (OrganisationKind.HealersGuild, new[] { $"the Bath-Masters of {where}", "the Physicians of the Old Sanatorium" }, null, 0.7)
            : (OrganisationKind.SecretSociety, new[] { $"the last servants of {c?.Name ?? "the empire"} at {where}" }, null, 0.3);

        if (!sim.Chance(next.Survive))
        {
            sim.DissolveOrganisation(Branch, owner, c == e.Inquisition
                ? "its grey priests are hanged by the people they buried"
                : "its masters are gone and no one keeps it");
            return;
        }
        // The heir keeps the institution as its counterpart: its lineage, which is what lets a book (or
        // the viewer) say that the Stranded Knights were once a chapter of the Knights of the Cosmic Sea.
        var heir = sim.FoundOrganisation(next.Kind, Branch.HomeRegion, owner, sim.Pick(next.Names), faith: next.Faith, counterpart: c,
                                         description: $"What {Branch.Name} became when the ships stopped coming. {OrganisationGenerator.Purpose(next.Kind)}");
        sim.ChangeOrganisation(heir, OrganisationChange.BecameIndependent, $"Cut off from Pyr, {Branch.Name} becomes {heir.Name}.", Branch);
        sim.DissolveOrganisation(Branch, heir, "became independent");
    }
}
