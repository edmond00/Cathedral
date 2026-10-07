using System;
using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.History.Lore;

/// <summary>
/// The empire's history, hardcoded from <c>lore/</c>: every figure, house, institution, faith, world
/// and dated event the lore files establish, as objects. Identical on every world and built once per
/// process (<see cref="Instance"/>), with no randomness.
///
/// <para><b>This is the true version.</b> Books will tell it otherwise on purpose; the lore files say
/// how. Anything a generator needs to refer to (the Inquisition, Genesive Qothism, Oox) is a field
/// here, so a reference that no longer exists is a build error and not a silent miss.</para>
///
/// <para>Split in three: this file holds the factions, faiths, worlds and places; the figures are in
/// <c>EmpireLore.Figures.cs</c> and the dated events in <c>EmpireLore.Events.cs</c>. Construction order
/// matters and is fixed in the constructor: infos first, then people (who point at houses), then
/// events (which point at everything).</para>
/// </summary>
public sealed partial class EmpireLore
{
    private static readonly Lazy<EmpireLore> _instance = new(() => new EmpireLore());

    /// <summary>The one catalogue. Pure data, so sharing it between worlds and threads is safe once built.</summary>
    public static EmpireLore Instance => _instance.Value;

    public Chronology Chronology { get; } = new(HistoryScope.Empire);

    public List<HistoricFigure> Figures { get; } = new();
    public List<HistoricFaction> Factions { get; } = new();
    public List<Religion> Religions { get; } = new();
    public List<WorldInfo> Worlds { get; } = new();
    public List<Place> Places { get; } = new();
    public List<War> Wars { get; } = new();

    private EmpireLore()
    {
        BuildFactions();
        BuildReligions();
        BuildWorlds();
        BuildPlaces();
        BuildFigures();
        BuildEvents();
        BuildLifeEvents();
    }

    private const HistoryScope E = HistoryScope.Empire;

    private static HistoricDate At(int round) => HistoricDate.At(round);
    private static HistoricDate Circa(int round) => HistoricDate.Circa(round);

    // ── Factions ────────────────────────────────────────────────────────────────

    public HistoricFaction HouseOfBan = null!, OldHouse = null!, RedBrickHouse = null!, Counsel = null!,
        BanGoerim = null!, HouseMoll = null!, HouseHask = null!, HouseAvory = null!,
        Empire = null!, KingdomOfVu = null!, Benepelop = null!, Napel = null!, Penaros = null!, Pebelos = null!,
        KingdomOfBelune = null!, IISTG = null!, TicklersGuild = null!, SaltGuard = null!, RedTribunals = null!,
        PlebeianTribunal = null!, Inquisition = null!, Knights = null!, Senate = null!, CouncilOfPearls = null!,
        ChildrenOfRede = null!, TheWakeful = null!, AcademyOfTheHook = null!, BlankHouses = null!,
        PhysiciansOfTheSprings = null!, FarLodges = null!, NorthernCoalition = null!, BeatildistWells = null!;

    private HistoricFaction Faction(string name, HistoricDate founded, HistoricDate dissolved, string description)
    {
        var f = new HistoricFaction(name, E) { Founded = founded, Dissolved = dissolved, Description = description };
        Factions.Add(f);
        return f;
    }

    private void BuildFactions()
    {
        var none = HistoricDate.Unknown;
        HouseOfBan = Faction("the House of Ban", At(-1052), At(-512),
            "The barons of Ban and their descendants before the Parting of the Houses.");
        OldHouse = Faction("the Old House", At(-512), At(1004),
            "The Ban clergy of Principism, headed by a patriarch; extinct in the Burning Council.");
        RedBrickHouse = Faction("the Red Brick House", At(-512), At(3579),
            "Ban-Balnus: keepers of the elf and of its blood, headed by a matriarch, emperors from 1703.");
        Counsel = Faction("the Counsel", At(-512), At(1703),
            "Ban-Aloid: advisors to the noble houses, then the imperial dynasty until the Red Night.");
        BanGoerim = Faction("Ban-Goerim", Circa(-380), At(1540),
            "The cadet line of the Old House at Perorigak; founders of the Genesive and Figurative churches.");
        HouseMoll = Faction("House Moll", At(-1052), At(1703),
            "Descendants of the golden knight, hereditary silent wardens of the Red Brick House gate.");
        HouseHask = Faction("House Hask", none, At(121),
            "Clan-chiefs of the Sorvan hearths; Vhomanor's house.");
        HouseAvory = Faction("House Avory", At(1731), At(2533),
            "Dosh Avory's descendants, hereditary governors of the three Avorias.");

        Empire = Faction("the Empire of the Half-Elves", At(0), At(3579),
            "The Ban empire: Vahyr, then Pyr, then the Nine Pearls, then 229 worlds.");
        KingdomOfVu = Faction("the Kingdom of Vu", Circa(-1600), At(601),
            "The Tide-Kings of the Yellow Island, masters of the salt of Osk.");
        Benepelop = Faction("the Republic of Benepelop", Circa(-1410), At(0),
            "A merchant republic of the southern horn, governed by an Ekklesia of thirty-three.");
        Napel = Faction("the League of Napel", Circa(-1380), At(-11),
            "River-towns under the Syndics of Yr.");
        Penaros = Faction("the Commonwealth of Penaros", Circa(-1300), At(-7),
            "Perorigak and Perostro under the Council of Bridges.");
        Pebelos = Faction("the Hearths of Pebelos", Circa(-1260), At(0),
            "Mountain clans sworn to the Hearth-Moot of Dokur.");
        NorthernCoalition = Faction("the Northern Coalition", At(-10), At(-7),
            "Penaros and Pebelos, allied against Aqilon.");
        KingdomOfBelune = Faction("the Kingdom of Belune", At(1321), At(3579),
            "Belune as a kingdom of the empire, with its own court at the Sanatorium.");

        IISTG = Faction("the Imperial Inland Seas Transport Guild", Circa(60), HistoricDate.AH(40),
            "Shipwrights, navigators and explorers of the empire, from the galley to the iron submarine.");
        TicklersGuild = Faction("the Ticklers' Guild", Circa(900), none,
            "Assassins of the Perostro docks; outlawed in 2533 and never quite gone.");
        SaltGuard = Faction("the Salt Guard", At(601), At(1703),
            "The imperial household guard, loyal to the Aloid to the end.");
        RedTribunals = Faction("the Red Tribunals", Circa(-300), At(2533),
            "Secret courts of the Red Brick House.");
        PlebeianTribunal = Faction("the Plebeian Tribunal", At(2533), At(3579),
            "Edilon III's public courts of written law.");
        Inquisition = Faction("the Principian Inquisition", At(2533), At(3579),
            "The grey priests, enforcers of devotion among the six permitted faiths.");
        Knights = Faction("the Knights of the Cosmic Sea", At(2534), At(3579),
            "The military order that held the colonised worlds.");
        Senate = Faction("the Imperial Senate", At(0), At(1704),
            "The Ekklesia's successor: thirty-three seats in the emperor's gift.");
        CouncilOfPearls = Faction("the Council of the Pearls", At(1704), At(2533),
            "One seat per world of the Nine Pearls.");
        ChildrenOfRede = Faction("the Children of Rede", At(249), At(263),
            "Followers of Grizx who burnt people alive.");
        TheWakeful = Faction("the Wakeful", At(-40), none,
            "The Avolor poor's devotion to the Awakened Prince.");
        AcademyOfTheHook = Faction("the Academy of the Hook", Circa(300), At(3579),
            "The Adswarist school at Perorigak.");
        BlankHouses = Faction("the Blank Houses", Circa(270), none,
            "Silihist monasteries of perpetual silence.");
        PhysiciansOfTheSprings = Faction("the Physicians of the Springs", At(1212), At(3579),
            "The medical college of the Imperial Thermal Sanatorium.");
        FarLodges = Faction("the Far Lodges", Circa(2820), At(3579),
            "Esoteric circles of the imperial family and high nobility.");
        BeatildistWells = Faction("the Beatildist wells", At(1703), none,
            "The cells of the proscribed cult of the Veiled Empress.");
    }

    // ── Faiths ──────────────────────────────────────────────────────────────────

    public Religion Principism = null!, EarlyQothism = null!, Adswarism = null!, Silihism = null!,
        Principlurism = null!, GenesiveQothism = null!, FigurativeQothism = null!, PrincipativeQothism = null!,
        Medusosianism = null!, Beatildism = null!, Stillness = null!, FallenGod = null!, FarLore = null!,
        RedeCult = null!, LastEmpressCult = null!;

    private Religion Faith(string name, ReligionKind kind, HistoricDate founded, Religion? parent, string description)
    {
        var r = new Religion(name, kind, E) { Founded = founded, Parent = parent, Description = description };
        Religions.Add(r);
        return r;
    }

    /// <summary>The six faiths the Cosmic Empire permitted (2533 to 3579).</summary>
    public IReadOnlyList<Religion> PermittedFaiths => new[]
    {
        GenesiveQothism, FigurativeQothism, PrincipativeQothism, Adswarism, Silihism, Principlurism,
    };

    private void BuildReligions()
    {
        Stillness = Faith("the Stillness of Vu", ReligionKind.Discipline, HistoricDate.Unknown, null,
            "A godless discipline of sitting still and silent, facing the sea.");
        Medusosianism = Faith("Medusosianism", ReligionKind.Medusosian, Circa(-900), null,
            "The faith of the jellyfish: the embryo gathers the souls of the dead into a god.");
        Principism = Faith("Principism", ReligionKind.Principist, Circa(-880), null,
            "The faith of the Ten Chapters: the cosmos as a chain of necessary answers.");
        RedeCult = Faith("the cult of the Throw", ReligionKind.Principist, Circa(100), Principism,
            "Fraternities of gamblers, sailors and soldiers who worship Rede.");
        EarlyQothism = Faith("Early Qothism", ReligionKind.Principist, At(262), Principism,
            "Qoth is the main principium. The official faith from 262 to 1004.");
        Adswarism = Faith("Adswarism", ReligionKind.Principist, At(262), Principism,
            "Reason above all; Adswaru is the main principium.");
        Silihism = Faith("Silihism", ReligionKind.Principist, At(262), Principism,
            "Silance is the main principium, and reality does not exist.");
        Principlurism = Faith("Principlurism", ReligionKind.Principist, At(262), Principism,
            "No principium is main; all seven are one chain.");
        GenesiveQothism = Faith("Genesive Qothism", ReligionKind.Principist, At(1009), EarlyQothism,
            "Qoth alone remains and is omnipotent; the other principia ceased with the creation.");
        FigurativeQothism = Faith("Figurative Qothism", ReligionKind.Principist, At(1009), EarlyQothism,
            "Qoth is omnipotent; the other principia are Qoth's inner thoughts.");
        PrincipativeQothism = Faith("Principative Qothism", ReligionKind.Principist, At(1009), EarlyQothism,
            "All principia are eternal; Qoth is main but not omnipotent and does not intervene.");
        Beatildism = Faith("Beatildism", ReligionKind.Medusosian, At(1703), Medusosianism,
            "Beatilda, the rightful empress, lives on as the jellyfish of Varam.");
        Beatildism.Proscribed = true;
        FallenGod = Faith("the Faith of the Fallen God", ReligionKind.PersonCult, At(2566), null,
            "Oox, Bigruw Trulhexiwn Kametzoriis: the god who fell, was buried, and rose.");
        FallenGod.Proscribed = true;
        FarLore = Faith("the Far Lore", ReligionKind.PersonCult, Circa(2820), null,
            "Esoteric lore of the far worlds: elf-science, the speaking stones, the numerology of eleven.");
        LastEmpressCult = Faith("the cult of the Last Empress", ReligionKind.PersonCult, HistoricDate.AH(150), null,
            "Pilgrims who believe Vesifia III still breathes inside her pyramid.");

        // Which faiths the empire hunted, and from when (lore/07_religions.md, "The churches under each
        // era"). A faith forbidden at a date is what travels in a hidden book rather than a pulpit.
        RedeCult.ForbiddenSince = At(262);
        Stillness.ForbiddenSince = At(601);
        Beatildism.ForbiddenSince = At(1703);
        Medusosianism.ForbiddenSince = At(HistoryCalendar.InquisitionRound);
        FallenGod.ForbiddenSince = At(HistoryCalendar.InquisitionRound + 111);   // 2644, Oox taken
        FarLore.ForbiddenSince = At(2820);   // never public: kept inside the family and the Lodges

        // Superseded by the schisms: still in old books, no longer anyone's church.
        Principism.Superseded = At(262);
        EarlyQothism.Superseded = At(1004);

        // Faiths that may be open only where their lore puts them; anywhere else, only a hidden sect.
        foreach (var r in new[] { Medusosianism, Beatildism, FallenGod, FarLore, LastEmpressCult, RedeCult, Stillness })
            r.ClandestineAbroad = true;
    }

    // ── Worlds ──────────────────────────────────────────────────────────────────

    public WorldInfo Pyr = null!, Varam = null!, Belune = null!, Prunil = null!, GreenAvoria = null!,
        BlueAvoria = null!, GoldenAvoria = null!, NewPyr = null!, Aqilonia = null!, NewVaram = null!,
        Zuilkansia = null!, Kametzor = null!, Trulhex = null!, Gorrow = null!, Uhlmeth = null!, Sabbaroth = null!,
        Ossomire = null!, Salsuge = null!, Cendre = null!, Vessary = null!, Calvassa = null!, Hethra = null!,
        Mirelle = null!, Ysthane = null!, Nadirine = null!, Emberlee = null!, Perpetua = null!,
        Gorgomanth = null!, GeoantsMark = null!;

    private WorldInfo World(string name, ImperialStatus status, string description)
    {
        var w = new WorldInfo(name, status, E) { Description = description };
        Worlds.Add(w);
        return w;
    }

    private void BuildWorlds()
    {
        Pyr = World("Pyr", ImperialStatus.Hatched, "The Ban world: the crescent of Vahyr and the Yellow Island. Hatched 3579.");
        Varam = World("Varam", ImperialStatus.Hatched, "The brightest moon of Pyr, a world of ice; a prison. Hatched 1703.");
        Belune = World("Belune", ImperialStatus.Held, "The nearest moon of Pyr: hills and hot springs. A kingdom of the empire from 1321.");
        Prunil = World("Prunil", ImperialStatus.Held, "Mountains, gorges, salt and gold; conquered by Rosena, 1705 to 1722.");
        GreenAvoria = World("Green Avoria", ImperialStatus.Held, "Forests and jungles; conquered by Dosh Avory, 1731 to 1740.");
        BlueAvoria = World("Blue Avoria", ImperialStatus.Held, "One ocean and a thousand islands; conquered by Dosh Avory, 1744 to 1756.");
        GoldenAvoria = World("Golden Avoria", ImperialStatus.Held, "Dunes and salt-pans; conquered by Dosh Avory, 1760 to 1771. Vesifia III's exile.");
        NewPyr = World("New Pyr", ImperialStatus.Held, "A temperate world colonised from Pyr; conquered by Violann III, 1780 to 1788.");
        Aqilonia = World("Aqilonia", ImperialStatus.Held, "Plains and deltas; conquered by Violann III, 1792 to 1799.");
        NewVaram = World("New Varam", ImperialStatus.Held, "The most distant pearl, icy and bright; found by Gereston Bolish in 1811.");
        Zuilkansia = World("Zuilkansia", ImperialStatus.Held, "Basalt plateaus and warm shallows, where Oox came down in 2561.");
        Kametzor = World("Kametzor", ImperialStatus.Held, "One of the seventeen worlds of the Fallen God.");
        Trulhex = World("Trulhex", ImperialStatus.Held, "One of the seventeen worlds of the Fallen God; where Oox was taken in 2644.");
        Gorrow = World("Gorrow", ImperialStatus.Held, "One of the seventeen worlds of the Fallen God.");
        Uhlmeth = World("Uhlmeth", ImperialStatus.Held, "One of the seventeen worlds of the Fallen God.");
        Sabbaroth = World("Sabbaroth", ImperialStatus.Held, "One of the seventeen worlds of the Fallen God.");
        Ossomire = World("Ossomire", ImperialStatus.Held, "Cenzus Fogun's first conquest; the Burial Fields, where Oox was buried twice.");
        Salsuge = World("Salsuge", ImperialStatus.Held, "A world of salt marshes that Fogun called the marsh of the prophecy.");
        Cendre = World("Cendre", ImperialStatus.Held, "One of Cenzus Fogun's twenty-five worlds.");
        Vessary = World("Vessary", ImperialStatus.Held, "One of Cenzus Fogun's twenty-five worlds.");
        Calvassa = World("Calvassa", ImperialStatus.Held, "An iron world: the forges of the iron submarines.");
        Hethra = World("Hethra", ImperialStatus.Held, "A dark world of mushrooms as tall as trees.");
        Mirelle = World("Mirelle", ImperialStatus.Held, "A farming world; the first written account of goblins comes from here.");
        Ysthane = World("Ysthane", ImperialStatus.Held, "The world of the speaking stones, source of the Far Lore.");
        Nadirine = World("Nadirine", ImperialStatus.Held, "The Low Moon, near the sand-floor; base of the Deep Expeditions.");
        Emberlee = World("Emberlee", ImperialStatus.Held, "The High Moon, so bright its nights are twilit; astronomers' world.");
        Perpetua = World("Perpetua", ImperialStatus.Held, "A penal colony-world of the Plebeian Tribunal.");
        Gorgomanth = World("Gorgomanth", ImperialStatus.Held, "A world of giant beasts, hunted for the imperial menageries.");
        GeoantsMark = World("Geoant's Mark", ImperialStatus.Unvisited, "The dim world Arneviz Geoant sailed for in 2871 and never reached.");
    }

    // ── Places (on Pyr and the lost worlds; a playable world's places are its own) ──

    public Place FishingHut = null!, BanKeep = null!, OldHouseBuilding = null!, RedBrickHouseBuilding = null!,
        SaltPalace = null!, TowerOfSalts = null!, HouseOfThirtyThree = null!, Yr = null!, Kalom = null!,
        Avenas = null!, Sorvas = null!, Dokur = null!, Perorigak = null!, Perostro = null!, Hanavun = null!,
        MinesOfOsk = null!, Guildyards = null!, BlindCells = null!, FoundlingHouse = null!, LuskChapel = null!,
        Vaur = null!, RedPyramidsOfPyr = null!, Avolor = null!;

    private Place PyrPlace(string name, PlaceKind kind, HistoricDate founded, HistoricDate ruined, string description)
    {
        var p = new Place(name, kind, E) { Founded = founded, Ruined = ruined, World = Pyr, Description = description };
        Places.Add(p);
        return p;
    }

    private void BuildPlaces()
    {
        var none = HistoricDate.Unknown;
        var hatching = At(HistoryCalendar.HatchingRound);
        Avolor = PyrPlace("Avolor", PlaceKind.Citadel, none, hatching, "Capital of Benepelop, then of the empire, on seven hills.");
        FishingHut = PyrPlace("the fishing hut of Tavunesh", PlaceKind.Citadel, none, At(-1052), "Where the twins kept the elf chained.");
        BanKeep = PyrPlace("Ban Keep", PlaceKind.Fortress, none, hatching, "The barons' tower-house, and Hesperine's Tower.");
        OldHouseBuilding = PyrPlace("the Old House", PlaceKind.Monastery, Circa(-880), At(1004), "The Ban townhouse where Principism was first taught.");
        RedBrickHouseBuilding = PyrPlace("the Red Brick House", PlaceKind.Fortress, At(-505), hatching, "A windowless manor of dark red brick on the Clay Hill, and the Well-Room beneath it.");
        SaltPalace = PyrPlace("the Salt Palace", PlaceKind.Palace, none, hatching, "The Hundred Gardens Palace, renamed in 601 for the Salt Mound in its Garden of Lilies.");
        TowerOfSalts = PyrPlace("the Tower of Salts", PlaceKind.Fortress, none, hatching, "A salt magazine made a prison.");
        HouseOfThirtyThree = PyrPlace("the House of Thirty-Three", PlaceKind.Palace, Circa(-1410), hatching, "The Ekklesia's hall, then the Senate's, then the Plebeian Tribunal's.");
        Yr = PyrPlace("Yr", PlaceKind.Citadel, none, hatching, "Capital of Napel; burnt in -9 and rebuilt.");
        Kalom = PyrPlace("Kalom", PlaceKind.Citadel, none, hatching, "A walled trading town north of Avolor.");
        Avenas = PyrPlace("Avenas", PlaceKind.Citadel, none, hatching, "A lake town reached by a causeway.");
        Sorvas = PyrPlace("Sorvas", PlaceKind.Citadel, none, At(-1), "A mountain town buried by Aqilon's sappers.");
        Dokur = PyrPlace("Dokur", PlaceKind.Citadel, none, hatching, "Capital of Pebelos, where the empire was founded.");
        Perorigak = PyrPlace("Perorigak", PlaceKind.Citadel, none, hatching, "Capital of Penaros.");
        Perostro = PyrPlace("Perostro", PlaceKind.Port, none, hatching, "Port city of the IISTG.");
        Hanavun = PyrPlace("Hanavun", PlaceKind.Citadel, none, hatching, "Royal city of Vu and its Yellow Court.");
        MinesOfOsk = PyrPlace("the mines of Osk", PlaceKind.Mine, none, hatching, "The only salt on Pyr.");
        Guildyards = PyrPlace("the Guildyards of Perostro", PlaceKind.Port, Circa(60), hatching, "The greatest shipyards of Pyr.");
        BlindCells = PyrPlace("the Blind Cells", PlaceKind.Fortress, none, hatching, "Lightless dungeons under Perorigak's citadel.");
        FoundlingHouse = PyrPlace("the Foundling House of Perorigak", PlaceKind.Monastery, none, hatching, "Burnt by Grizx in 211 and rebuilt.");
        LuskChapel = PyrPlace("the Plain Chapel of Lusk", PlaceKind.Temple, At(1009), hatching, "The see of the Plain Church: no altar, no image, no bell.");
        RedPyramidsOfPyr = PyrPlace("the five Red Pyramids of Pyr", PlaceKind.Pyramid, Circa(2700), hatching, "Where the giant Balnus lived out their centuries.");
        Vaur = new Place("Vaur", PlaceKind.Fortress, E) { Founded = Circa(1400), Ruined = At(1703), World = Varam,
                                                           Description = "The ice dungeon of Varam." };
        Places.Add(Vaur);
    }
}
