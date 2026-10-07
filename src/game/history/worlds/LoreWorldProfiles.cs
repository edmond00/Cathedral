using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.History.Engine;
using Cathedral.Game.History.Engine.Seeds;
using Cathedral.Game.History.Generation;
using Cathedral.Game.History.Lore;

namespace Cathedral.Game.History.Worlds;

// The worlds the lore names, and the two groups it counts (Oox's seventeen, Fogun's twenty-five).
// Each profile keeps the generic native past and replaces the imperial chapter with the lore's: its
// dates, its conquerors, its places, and the beats that make the world the one lore/04 describes.
// Dates are FC rounds, from lore/01_chronology.md.

/// <summary>Helpers shared by every lore profile.</summary>
public abstract class LoreWorldProfile : WorldProfile
{
    protected static EmpireLore L => EmpireLore.Instance;

    /// <summary>The lore's own info for this world, or null for an unnamed member of a group.</summary>
    protected abstract WorldInfo? LoreWorld { get; }

    public override ImperialStatus Status => ImperialStatus.Held;

    public override WorldInfo CreateWorld(int ordinal, WorldLanguage language)
    {
        if (LoreWorld is not { } lore) return base.CreateWorld(ordinal, language);
        return new WorldInfo(lore.Name, Status, HistoryScope.World)
        {
            MoonOrdinal = ordinal,
            NativeName = language.PlaceName(),
            Description = lore.Description,
        };
    }

    /// <summary>Sows a lore beat: an action on the world at a round.</summary>
    protected static void At(HistorySimulation sim, int round, Action<HistorySimulation> act, int priority = 30)
        => sim.Sow(new ScriptedSeed(round, act, priority));

    /// <summary>Sows a lore beat that only records what happened.</summary>
    protected static void Beat(HistorySimulation sim, int round, string title, string text, params HistoricInfo[] involved)
        => At(sim, round, s => s.Chronicle(title, text, involved));

    protected static int Region(HistorySimulation sim, Func<RegionProfile, double> score, Func<RegionProfile, bool>? filter = null)
    {
        var pool = sim.Regions.Where(filter ?? (_ => true)).ToList();
        if (pool.Count == 0) pool = sim.Regions.ToList();
        return pool.OrderByDescending(score).ThenBy(r => r.Id).First().Id;
    }

    protected static int MostHabitable(HistorySimulation sim) => Region(sim, r => r.Habitability);
    protected static int MostMountainous(HistorySimulation sim) => Region(sim, r => r.MountainCells);
    protected static int LeastHabitable(HistorySimulation sim) => Region(sim, r => -r.Habitability);
    protected static int Coastal(HistorySimulation sim) => Region(sim, r => r.Habitability, r => r.Coastal);

    /// <summary>A lore place, built on a round in the region <paramref name="where"/> picks.</summary>
    protected static void PlaceBeat(HistorySimulation sim, int round, PlaceKind kind, string name,
                                    Func<HistorySimulation, int> where, HistoricFaction? by = null)
        => At(sim, round, s => s.NewPlace(kind, where(s), by, name));

    /// <summary>
    /// Everything the current ruler of the world holds passes to a new realm: a kingdom made a province,
    /// a dominion retaken. The new realm becomes the world's province.
    /// </summary>
    protected static Realm ReplaceProvince(HistorySimulation sim, string name, Government government,
                                           HistoricFigure? ruler, Religion? faith = null)
    {
        var old = sim.History.Province;
        var regions = old != null && old.Regions.Count > 0
            ? old.Regions.ToList()
            : sim.Regions.Select(r => r.Id).ToList();
        var realm = sim.FoundRealm(regions, government, ruler ?? sim.NewAdult(null, imperial: true, why: $"First governor of {name}."), name, predecessor: old);
        sim.History.Province = realm;
        sim.Adopt(realm, faith ?? ImperialFaith.At(sim, sim.Now), "the faith of the empire");
        return realm;
    }

    /// <summary>A lore organisation, founded on a round in the region <paramref name="where"/> picks.</summary>
    protected static void OrganisationBeat(HistorySimulation sim, int round, OrganisationKind kind, string name,
                                           Func<HistorySimulation, int> where, string description,
                                           HistoricFaction? counterpart = null, Religion? faith = null, bool clandestine = false)
        => At(sim, round, s =>
        {
            int region = where(s);
            var patron = clandestine ? null : s.History.OwnerOf(region);
            // An order serving a faith brings it, hidden or open, if the world does not have it yet.
            if (faith != null && s.History.PresenceOf(faith) == FaithPresence.Extinct)
            {
                if (clandestine) s.Smuggle(faith, $"{faith.Name} comes to {s.History.World.Name} with the founders of {name}.");
                else s.Present(faith);
            }
            var org = s.FoundOrganisation(kind, region, patron, name, counterpart: counterpart, faith: faith,
                                          clandestine: clandestine, description: description);
            if (counterpart != null)
                s.ChangeOrganisation(org, OrganisationChange.Imported, $"{counterpart.Name} opens {org.Name}.", counterpart);
        });

    /// <summary>The standard lore conquest: contact, then the conquest over a span.</summary>
    protected static void Conquest(HistorySimulation sim, int contact, int start, int end, HistoricInfo? conqueror,
                                   string? contactText = null, string? provinceName = null, HistoricFigure? ruler = null,
                                   Government government = Government.ImperialProvince, Religion? faith = null,
                                   bool imperialRule = true)
    {
        sim.Sow(new ImperialContactSeed(contact, conqueror, contactText));
        sim.Sow(new ImperialConquestSeed(start, end - start - 1, conqueror, provinceName, ruler, government, faith, imperialRule));
    }
}

// ── Belune ──────────────────────────────────────────────────────────────────────

/// <summary>
/// The nearest moon: tribes and hot springs, the Medusosian faith brought up from the dwarfs, the
/// Exchange Plagues, Jelebanne's kingdom, the War of the Regency, the three Red Pyramids, the short
/// Empire of Belune.
/// </summary>
public sealed class BeluneProfile : LoreWorldProfile
{
    protected override WorldInfo? LoreWorld => L.Belune;

    protected override int RegionsPerInitialRealm => 2;

    public override Government? NativeGovernment(HistorySimulation sim, int round)
        => round < 1321 ? (sim.Chance(0.8) ? Government.Tribe : Government.Chiefdom) : null;

    // One native faith before the dwarfs' came up the Gullet.
    protected override void SeedAncientFaiths(HistorySimulation sim)
    {
        var r = ReligionGenerator.Create(sim.Names, sim.Rng, HistoricDate.Unknown, null);
        r.Description = "An ancient faith. " + r.Description;
        sim.AddAncientFaith(r);
    }

    public override bool MayHoldOpenly(Religion faith) => faith == L.Medusosianism || base.MayHoldOpenly(faith);

    protected override void SowImperial(HistorySimulation sim)
    {
        At(sim, -900, s =>
        {
            s.NewPlace(PlaceKind.Sanctuary, MostMountainous(s), null, "the Gullet");
            s.Chronicle("The Descended", "Nemeth Oraun goes down the Gullet to the dwarfs of Oggerund and comes back with their faith: the dead sink into the god being born below.", L.NemethOraun, L.Medusosianism);
            foreach (var realm in s.History.LivingRealms.ToList())
                if (s.Chance(0.7)) s.Adopt(realm, L.Medusosianism, "taught by Nemeth Oraun");
            s.Present(L.Medusosianism);
        });

        sim.Sow(new ImperialContactSeed(806, L.JosuExculato,
            "Josu Exculato's submarine comes down the vortex of Belune, and cannot climb out again."));
        Beat(sim, 833, "The round trip", "Barnoix Barnutum reaches Belune and leaves again, with Josu's journal from his cairn.", L.BarnoixBarnutum, L.JosuExculato);
        sim.Sow(new MissionarySeed(851));
        At(sim, 1065, s =>
        {
            foreach (int r in s.Regions.OrderByDescending(r => r.Coastal).ThenByDescending(r => r.Habitability).Take(3).Select(r => r.Id))
                s.NewPlace(PlaceKind.Citadel, r, L.Empire, $"the sanatorium of {s.History.RegionNames[r]}");
            s.Chronicle("The springs", "Aqilon V plants trading posts and sanatoria at the hot springs, whose waters draw out black bile.", L.AqilonV);
            s.Record(new ImperialEvent(s.Today, HistoryScope.World, ImperialStage.ConquestBegun, s.History.World, L.AqilonV,
                "Belune is taken without a war: the empire's posts and sanatoria become colonies, and the colonies a province in all but name."));
        });
        At(sim, 1090, s =>
        {
            s.Chronicle("The Exchange Plagues", "The sick of Pyr bring fevers the people of Belune have never met. Over fifty rounds nine in ten of them die.");
            foreach (var realm in s.History.LivingRealms.ToList())
            {
                if (!s.Chance(0.7)) continue;
                if (realm.Ruler != null) s.Kill(realm.Ruler, DeathCause.Plague);
                s.Dissolve(realm, null, "emptied by plague");
            }
        });
        At(sim, 1212, s =>
        {
            s.NewPlace(PlaceKind.Palace, MostHabitable(s), L.Empire, "the Imperial Thermal Sanatorium");
            s.Chronicle("The Queen in the Steam", "Jelebanne, daughter of Polop II, is carried to the Sanatorium with a black bile corruption the springs can hold but not cure.", L.Jelebanne, L.PolopII);
        });
        At(sim, 1321, s =>
        {
            var kingdom = s.FoundRealm(s.Regions.Select(r => r.Id).ToList(), Government.Kingdom, L.Jelebanne, "the Kingdom of Belune");
            s.Record(new ImperialEvent(s.Today, HistoryScope.World, ImperialStage.ConquestCompleted, s.History.World, L.Jelebanne,
                "Belune is made a kingdom of the empire, with Jelebanne as its queen."));
            s.History.Province = kingdom;
            s.Adopt(kingdom, L.FigurativeQothism, "the faith of the court");
        });
        PlaceBeat(sim, 1362, PlaceKind.Monastery, "the Academy of the Springs", MostHabitable, L.Empire);
        OrganisationBeat(sim, 1212, OrganisationKind.ImperialBranch, "the Physicians of the Springs", MostHabitable,
            "The medical college of the Sanatorium: humoral theorists and bath-masters.", L.PhysiciansOfTheSprings);
        OrganisationBeat(sim, 1362, OrganisationKind.ScholarsCollege, "the College of the Academy of the Springs", MostHabitable,
            "The scholars of the Belunese court, rivals of Avolor's academy.");
        foreach (var (round, king) in new[] { (1392, L.Corvel), (1450, L.Amaranthe), (1530, L.OssianeII), (1618, L.Philandre) })
        {
            var heir = king;
            At(sim, round, s => { if (s.History.Province is { } k) s.Crown(k, heir); });
        }
        At(sim, 1703, s => s.Chronicle("The Red Night", "King Philandre is killed with the Aloid of Pyr; Belune's crown is given to Bemakor Ban-Balnus, wed to Philandre's daughter Loquine.", L.Philandre, L.Bemakor, L.Loquine));
        At(sim, 1704, s => { if (s.History.Province is { } k) s.Crown(k, L.Bemakor); });
        At(sim, 2532, s =>
        {
            s.Record(new BattleEvent(s.Today, HistoryScope.World, null, "The Causeway of Oranse", L.Empire, L.KingdomOfBelune));
            s.Chronicle("The end of the regency", "Bemakor dies on the causeway; Belune is made a province of the empire.", L.Bemakor, L.EdilonIII);
            var province = ReplaceProvince(s, "the Province of Belune", Government.ImperialProvince, null, L.GenesiveQothism);
            ConquestCompleteSeed.SowImperialRule(s, province);
        });
        At(sim, 2800, s =>
        {
            foreach (var (r, n) in new[] { (MostHabitable(s), "the Red Pyramid of the Sanatorium"), (LeastHabitable(s), "the Red Pyramid of Oranse"), (Coastal(s), "the Red Pyramid of Kell Harbour") })
                s.NewPlace(PlaceKind.Pyramid, r, L.RedBrickHouse, n);
        });
    }

    public override void SowStranding(HistorySimulation sim)
        => sim.Sow(new StrandingSeed(HistoryCalendar.HatchingRound + 10, "the Empire of Belune"));
}

// ── Prunil ──────────────────────────────────────────────────────────────────────

/// <summary>Mountains, salt and gold, small free cities; Rosena's conquest and her long reign as queen.</summary>
public sealed class PrunilProfile : LoreWorldProfile
{
    protected override WorldInfo? LoreWorld => L.Prunil;

    public override Government? NativeGovernment(HistorySimulation sim, int round)
        => round < 1705 ? (sim.Chance(0.5) ? Government.CityLeague : Government.Republic) : null;

    protected override void SowImperial(HistorySimulation sim)
    {
        PlaceBeat(sim, -800, PlaceKind.Citadel, "Carrow-in-the-Salt", MostMountainous);
        OrganisationBeat(sim, -700, OrganisationKind.ThievesGuild, "the Salt Brothers", MostMountainous,
            "Smugglers of salt and gold between the mountain cities, and the hiders of fugitives from Pyr.", clandestine: true);
        Conquest(sim, 1694, 1705, 1722, L.Rosena, "An IISTG expedition sights Prunil; the first landing follows three rounds later.",
                 "the Kingdom of Prunil", L.Rosena, Government.Kingdom);
        At(sim, 1709, s => s.Record(new BattleEvent(s.Today, HistoryScope.World, null, "The siege of Carrow-in-the-Salt", s.History.Province, null)));
        Beat(sim, 1713, "The Gorges", "Rosena's troops drive the people of the resisting cities off the cliffs of the gorges.", L.Rosena);
        PlaceBeat(sim, 1722, PlaceKind.Mine, "Aurimont", MostMountainous, L.Empire);
        PlaceBeat(sim, 1722, PlaceKind.Palace, "Rosena's Stair", MostHabitable, L.RedBrickHouse);
        At(sim, 2390, s =>
        {
            s.Chronicle("The death of the queen", "Rosena Ban-Balnus dies after six centuries on the throne of Prunil. Governors come after her.", L.Rosena);
            ReplaceProvince(s, "the Province of Prunil", Government.ImperialProvince, null);
            if (s.History.Province is { } p) s.Sow(new GovernorRotationSeed(s.Later(8, 35), p));
        });
    }
}

// ── The three Avorias ───────────────────────────────────────────────────────────

/// <summary>Dosh Avory's three worlds, governed by his house until Edilon III deposed it.</summary>
public sealed class AvoriaProfile : LoreWorldProfile
{
    public enum Kind { Green, Blue, Golden }

    public AvoriaProfile(Kind kind) => Which = kind;

    // The pyramid's household, kept so the dispersal can end it. A profile is built afresh per world.
    private Organisation? _household;

    public Kind Which { get; }

    protected override WorldInfo? LoreWorld => Which switch
    {
        Kind.Green => L.GreenAvoria,
        Kind.Blue  => L.BlueAvoria,
        _          => L.GoldenAvoria,
    };

    // The pilgrims of the Last Empress keep their cult openly only where she lies.
    public override bool MayHoldOpenly(Religion faith)
        => (Which == Kind.Golden && faith == L.LastEmpressCult) || base.MayHoldOpenly(faith);

    public override Government? NativeGovernment(HistorySimulation sim, int round) => round < 1731 && Which == Kind.Green
        ? (sim.Chance(0.6) ? Government.Tribe : Government.Chiefdom)
        : null;

    protected override void SowImperial(HistorySimulation sim)
    {
        var (start, end) = Which switch { Kind.Green => (1731, 1740), Kind.Blue => (1744, 1756), _ => (1760, 1771) };
        At(sim, start, s =>
        {
            var governor = s.NewAdult(null, imperial: true, sex: Sex.Male,
                why: $"Of House Avory, Dosh Avory's line: first governor of {s.History.World.Name}.");
            governor.Name = governor.Name.Split(' ')[0] + " Avory";
            s.Sow(new ImperialConquestSeed(s.Now, end - start - 1, L.DoshAvory, null, governor));
        }, priority: 5);
        sim.Sow(new ImperialContactSeed(start - 3, L.DoshAvory, $"Dosh Avory's submarines come down the vortex of {LoreWorld!.Name}."));
        Beat(sim, 2533, "The fall of House Avory", "Edilon III deposes the last Avory governor and sends him to Perpetua.", L.JourdainAvory, L.HouseAvory, L.EdilonIII);

        switch (Which)
        {
            case Kind.Green:
                PlaceBeat(sim, -600, PlaceKind.Citadel, "the tree-city of the Canopy Folk", s => Region(s, r => r.ForestCells));
                break;
            case Kind.Blue:
                Beat(sim, 1850, "The tide-opera", "Singers on floating stages found the tide-opera, which the whole empire will come to love.");
                OrganisationBeat(sim, 1850, OrganisationKind.BardsCompany, "the Tide-Opera Company", Coastal,
                    "Singers on floating stages: the tide-opera's first and greatest company.");
                Beat(sim, 2530, "A singer of Blue Avoria", "Ysme Varrocq, a courtesan and singer of this world, kills Emperor Akilon XII with a hairpin.", L.YsmeVarrocq, L.AkilonXII);
                break;
            case Kind.Golden:
                At(sim, 3420, s =>
                {
                    s.NewPlace(PlaceKind.Pyramid, LeastHabitable(s), null, "the Great Red Pyramid");
                    _household = s.FoundOrganisation(OrganisationKind.SecretSociety, LeastHabitable(s), null, "the Household of the Pyramid",
                        description: "The hundreds who feed and tend the heiress inside the pyramid, and tell no one what they see.",
                        clandestine: true);
                    s.Chronicle("The exile of the heiress", "Vesifia III, twenty-five metres tall, is brought to Golden Avoria; the tallest of the Red Pyramids rises around her.", L.VesifiaIII);
                });
                At(sim, HistoryCalendar.HatchingRound + 30, s =>
                {
                    s.Chronicle("The dispersal", "The household of the Great Red Pyramid seals the heiress's chamber and scatters into the dunes.", L.VesifiaIII);
                    if (_household != null) s.DissolveOrganisation(_household, null, "its charge sealed in, it scatters into the dunes");
                    int r = LeastHabitable(s);
                    var owner = s.History.OwnerOf(r);
                    if (owner != null && owner.Regions.Count >= 2)
                        s.Split(owner, new[] { r }, "the pyramid's household", government: Government.Tribe);
                });
                At(sim, HistoryCalendar.HatchingRound + 150, s =>
                {
                    s.Chronicle("The breathing stones", "Pilgrims to the Great Red Pyramid report breathing in the stones: the Last Empress lives.", L.VesifiaIII, L.LastEmpressCult);
                    var realms = s.History.LivingRealms.ToList();
                    if (realms.Count > 0) s.Adopt(s.Pick(realms), L.LastEmpressCult, "pilgrims of the Last Empress");
                });
                break;
        }
    }
}

// ── New Pyr and Aqilonia ────────────────────────────────────────────────────────

/// <summary>Violann III's two worlds.</summary>
public sealed class ViolannProfile : LoreWorldProfile
{
    public enum Kind { NewPyr, Aqilonia }

    public ViolannProfile(Kind kind) => Which = kind;

    public Kind Which { get; }

    protected override WorldInfo? LoreWorld => Which == Kind.NewPyr ? L.NewPyr : L.Aqilonia;

    protected override void SowImperial(HistorySimulation sim)
    {
        if (Which == Kind.NewPyr)
        {
            Conquest(sim, 1776, 1780, 1788, L.ViolannIII);
            PlaceBeat(sim, 1790, PlaceKind.Citadel, "New Avolor", MostHabitable, L.Empire);
            Beat(sim, 1795, "The colonists", "Ships from Pyr bring colonists in thousands; New Avolor is laid out on seven hills, with a Clay Hill and a salt market.");
        }
        else
        {
            Conquest(sim, 1789, 1792, 1799, L.ViolannIII);
            Beat(sim, 1900, "The masques", "The Aqilonian masques, court plays in the masks of the emperors, are first performed.");
        }
    }

    public override void SowStranding(HistorySimulation sim)
        => sim.Sow(new StrandingSeed(sim.Later(0, 12), Which == Kind.NewPyr ? "the Empire of New Pyr" : null));
}

// ── New Varam ───────────────────────────────────────────────────────────────────

/// <summary>The most distant pearl; the refuge of the Beatildists.</summary>
public sealed class NewVaramProfile : LoreWorldProfile
{
    protected override WorldInfo? LoreWorld => L.NewVaram;

    // The refuge of the Beatildists: the one world where the Veiled Empress may be worshipped openly,
    // once the empire that hunted her is gone.
    public override bool MayHoldOpenly(Religion faith) => faith == L.Beatildism || base.MayHoldOpenly(faith);

    public override Government? NativeGovernment(HistorySimulation sim, int round) => round < 1815 ? Government.Tribe : null;

    protected override void SowImperial(HistorySimulation sim)
    {
        Conquest(sim, 1811, 1815, 1829, L.GerestonBolish, "Gereston Bolish comes down the vortex of a bright, icy world, and names it New Varam.");
        Beat(sim, 1840, "The ice-song", "The ice-cantors' throat-song is heard at the imperial court for the first time.");
        At(sim, 1850, s =>
        {
            s.Chronicle("The wells of New Varam", "Beatildists fleeing Pyr settle in numbers, believing the world's name a sign.", L.Beatildism);
            s.Present(L.Beatildism);
            s.Proscribe(s.History.Province ?? (HistoricFaction)L.Empire, L.Beatildism, "as it is everywhere in the empire", extinguishChance: 0);
            var well = s.FoundOrganisation(OrganisationKind.SecretSociety, MostMountainous(s), null, "the Wells of New Varam",
                counterpart: L.BeatildistWells, faith: L.Beatildism, clandestine: true,
                description: "The Beatildist cells of New Varam, recognised by a red thread.");
            s.ChangeOrganisation(well, OrganisationChange.Imported, "Refugees from Pyr open the first wells of the Veiled Empress on New Varam.", L.BeatildistWells);
        });
        sim.Sow(new NativeRevivalSeed(HistoryCalendar.HatchingRound + 20));
    }
}

// ── Oox's seventeen ─────────────────────────────────────────────────────────────

/// <summary>
/// The worlds of the Fallen God: taken by Oox as a living god between 2566 and 2641, retaken by the
/// Inquisition after his capture in 2644. Zuilkansia is where he came down.
/// </summary>
public sealed class OoxWorldProfile : LoreWorldProfile
{
    public enum Kind { Zuilkansia, Kametzor, Trulhex, Gorrow, Uhlmeth, Sabbaroth, Unnamed }

    public OoxWorldProfile(Kind kind) => Which = kind;

    public Kind Which { get; }

    // The Fallen God is worshipped openly only on the worlds he took.
    public override bool MayHoldOpenly(Religion faith) => faith == L.FallenGod || base.MayHoldOpenly(faith);

    protected override WorldInfo? LoreWorld => Which switch
    {
        Kind.Zuilkansia => L.Zuilkansia,
        Kind.Kametzor   => L.Kametzor,
        Kind.Trulhex    => L.Trulhex,
        Kind.Gorrow     => L.Gorrow,
        Kind.Uhlmeth    => L.Uhlmeth,
        Kind.Sabbaroth  => L.Sabbaroth,
        _               => null,
    };

    protected override void SowImperial(HistorySimulation sim)
    {
        int start;
        if (Which == Kind.Zuilkansia)
        {
            At(sim, 2561, s =>
            {
                s.NewPlace(PlaceKind.Sanctuary, Region(s, r => r.Coastal ? 1 : 0), null, "the Crater of the Fall");
                s.Chronicle("The god fallen from the sky", "A giant climbs down the wall of the vortex half-drowned: Oox, whom the tribes call Bigruw Trulhexiwn Kametzoriis.", L.Oox, L.FallenGod);
                foreach (var realm in s.History.LivingRealms.ToList()) s.Adopt(realm, L.FallenGod, "worshipping the giant who fell");
            });
            start = 2566;
        }
        else
        {
            start = sim.Rng.Next(2567, 2638);
        }

        Conquest(sim, Which == Kind.Zuilkansia ? 2561 : start, start, start + sim.Rng.Next(2, 9), L.Oox,
                 Which == Kind.Zuilkansia ? null : "Oox's stolen submarine comes down the vortex, carrying a giant and his fanatics.",
                 "the Dominion of the Fallen God", L.Oox, Government.Theocracy, L.FallenGod, imperialRule: false);

        At(sim, start + 1, s => s.FoundOrganisation(OrganisationKind.MonasticOrder, MostHabitable(s), s.History.Province,
            "the Priests of the Pit", faith: L.FallenGod,
            description: "The priesthood of the Fallen God: initiates buried for a night and risen, keepers of the pits."));

        if (Which == Kind.Trulhex)
            Beat(sim, 2644, "The taking of the god", "The Principian Inquisition captures Oox on Trulhex.", L.Oox, L.Inquisition);

        At(sim, 2644 + sim.Rng.Next(0, 6), s =>
        {
            s.Chronicle("The grey priests", "The Inquisition retakes the dominion of the Fallen God; his priests are buried alive.", L.Inquisition, L.FallenGod);
            var province = ReplaceProvince(s, $"the Province of {s.History.World.Name}", Government.ImperialProvince, null);
            ConquestCompleteSeed.SowImperialRule(s, province);
        });
    }
}

// ── Fogun's twenty-five ─────────────────────────────────────────────────────────

/// <summary>The inquisitor's conquests: mass burials, native temples torn down, Fogunian temples raised.</summary>
public sealed class FogunWorldProfile : LoreWorldProfile
{
    public enum Kind { Ossomire, Salsuge, Cendre, Vessary, Unnamed }

    public FogunWorldProfile(Kind kind) => Which = kind;

    public Kind Which { get; }

    protected override WorldInfo? LoreWorld => Which switch
    {
        Kind.Ossomire => L.Ossomire,
        Kind.Salsuge  => L.Salsuge,
        Kind.Cendre   => L.Cendre,
        Kind.Vessary  => L.Vessary,
        _             => null,
    };

    protected override void SowImperial(HistorySimulation sim)
    {
        int start = Which == Kind.Ossomire ? 2598 : sim.Rng.Next(2600, 2648);
        int end = start + sim.Rng.Next(3, 11);
        Conquest(sim, start - sim.Rng.Next(1, 4), start, end, L.CenzusFogun);
        Beat(sim, end, "The sowing of the unconverted", "Cenzus Fogun buries alive those who will not convert, by the thousand.", L.CenzusFogun);

        switch (Which)
        {
            case Kind.Ossomire:
                PlaceBeat(sim, 2600, PlaceKind.BurialField, "the Burial Fields of Ossomire", MostHabitable, L.Inquisition);
                Beat(sim, 2645, "Buried twice", "Oox is buried alive in the Burial Fields; he digs himself out, and is buried again under a stone.", L.Oox, L.Inquisition);
                break;
            case Kind.Salsuge:
                Beat(sim, end + 5, "The marsh of the prophecy", "Fogun declares the salt marshes the drained sea of the Eleventh Chapter; pilgrims come to die here with seeds in their hands.", L.CenzusFogun, L.GenesiveQothism);
                Beat(sim, 2661, "The death of Fogun", "Cenzus Fogun dies of fever among the pilgrims' seed-graves.", L.CenzusFogun);
                break;
        }
    }
}

// ── Other provinces the lore names ──────────────────────────────────────────────

/// <summary>Held worlds the lore names for one thing each.</summary>
public sealed class ProvinceProfile : LoreWorldProfile
{
    public enum Kind { Calvassa, Hethra, Mirelle, Ysthane, Nadirine, Emberlee, Perpetua, Gorgomanth }

    public ProvinceProfile(Kind kind) => Which = kind;

    public Kind Which { get; }

    protected override WorldInfo? LoreWorld => Which switch
    {
        Kind.Calvassa   => L.Calvassa,
        Kind.Hethra     => L.Hethra,
        Kind.Mirelle    => L.Mirelle,
        Kind.Ysthane    => L.Ysthane,
        Kind.Nadirine   => L.Nadirine,
        Kind.Emberlee   => L.Emberlee,
        Kind.Perpetua   => L.Perpetua,
        _               => L.Gorgomanth,
    };

    protected override void SowImperial(HistorySimulation sim)
    {
        int start = Which switch
        {
            Kind.Perpetua => 2534,
            Kind.Calvassa => 2545,
            Kind.Gorgomanth => 2570,
            Kind.Hethra or Kind.Emberlee => 2600,
            Kind.Mirelle => 2620,
            Kind.Nadirine => 2650,
            _ => 2700,
        };
        Conquest(sim, start - sim.Rng.Next(2, 10), start, start + sim.Rng.Next(3, 20), null);

        switch (Which)
        {
            case Kind.Calvassa:
                PlaceBeat(sim, 2555, PlaceKind.Mine, "the forges of Calvassa", MostMountainous, L.IISTG);
                OrganisationBeat(sim, 2555, OrganisationKind.ImperialBranch, "the Guild of the Iron Hulls", MostMountainous,
                    "The IISTG's forge-masters on Calvassa, who rivet the iron submarines.", L.IISTG);
                Beat(sim, 2560, "The iron hulls", "The forges of Calvassa begin to rivet the iron submarines of the second expansion.", L.MaelisTourbe, L.IISTG);
                break;
            case Kind.Hethra:
                Beat(sim, -950, "The mushroom forests", "In the dark of Hethra mushrooms grow as tall as trees, and the people live beneath them.");
                break;
            case Kind.Mirelle:
                Beat(sim, HistoryCalendar.HatchingRound + 70, "The Complaint of the Wardens", "The wardens of Mirelle write down what they have seen in the old mines: small, green, pig-nosed creatures. It is the first account of goblins anywhere.");
                break;
            case Kind.Ysthane:
                PlaceBeat(sim, -950, PlaceKind.Sanctuary, "the speaking stones", LeastHabitable);
                OrganisationBeat(sim, -900, OrganisationKind.SecretSociety, "the Listeners of the Stones", LeastHabitable,
                    "Those who sit by the speaking stones at night and write down what they hum.");
                OrganisationBeat(sim, 2835, OrganisationKind.SecretSociety, "the Far Lodge of Ysthane", LeastHabitable,
                    "A lodge of the Far Lore, founded where its lore was found.", L.FarLodges, L.FarLore, clandestine: true);
                Beat(sim, 2830, "The linguist", "Arneviz Geoant listens to the speaking stones and carries their lore to the imperial court.", L.ArnevizGeoant, L.FarLore);
                break;
            case Kind.Nadirine:
                PlaceBeat(sim, 3565, PlaceKind.Port, "the deep docks", Coastal, L.IISTG);
                Beat(sim, 3569, "The Deep Expeditions", "Four iron submarines leave the deep docks for the sea floor.", L.NaabusVII, L.IISTG);
                break;
            case Kind.Emberlee:
                PlaceBeat(sim, 2700, PlaceKind.Monastery, "the Observatory", MostHabitable, L.Empire);
                break;
            case Kind.Perpetua:
                Beat(sim, 2540, "The penal colony", "The Plebeian Tribunal begins sending its convicts to Perpetua; Jourdain Avory, last governor of the Avorias, among the first.", L.PlebeianTribunal, L.JourdainAvory);
                OrganisationBeat(sim, 2545, OrganisationKind.ThievesGuild, "the Brotherhood of the Chain", MostHabitable,
                    "The convicts' own law in the camps of Perpetua.", clandestine: true);
                break;
            case Kind.Gorgomanth:
                Beat(sim, 2600, "The hunters", "Hunters for the imperial menageries take the giant beasts of Gorgomanth alive.");
                break;
        }
    }

    public override int GoblinRound(HistorySimulation sim)
        => Which == Kind.Mirelle ? HistoryCalendar.HatchingRound + 70 : base.GoblinRound(sim);
}

// ── Geoant's Mark ───────────────────────────────────────────────────────────────

/// <summary>The dim world Arneviz Geoant sailed for and never reached. The empire never came.</summary>
public sealed class GeoantsMarkProfile : LoreWorldProfile
{
    protected override WorldInfo? LoreWorld => L.GeoantsMark;

    public override ImperialStatus Status => ImperialStatus.Unvisited;

    protected override void SowImperial(HistorySimulation sim) { }
}
