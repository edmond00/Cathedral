using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.History;

/// <summary>
/// Where a piece of history belongs: to the empire's hardcoded history (the lore, identical on every
/// world) or to one generated world.
/// </summary>
public enum HistoryScope
{
    Empire,
    World,
}

/// <summary>
/// Something history can be about: a person, a faith, a realm, a place, a war, a world. Events point
/// at infos, and every info keeps the events that name it (<see cref="Events"/>), so either side can
/// be reached from the other.
///
/// <para><b>Identity is the object.</b> A history is regenerated from the seed, never saved, so there
/// is no id to keep stable and nothing refers to an info by name. Code that needs to know what KIND of
/// thing an info is asks its type, as everywhere else in this codebase.</para>
/// </summary>
public abstract class HistoricInfo
{
    protected HistoricInfo(string name, HistoryScope scope)
    {
        Name = name;
        Scope = scope;
    }

    /// <summary>What the thing is called, as the true history knows it.</summary>
    public string Name { get; set; }

    public HistoryScope Scope { get; }

    /// <summary>One neutral sentence of what this is, for dumps and, later, for book-writing prompts.</summary>
    public string Description { get; set; } = "";

    /// <summary>Every event that involves this info, in the order they were recorded.</summary>
    public List<HistoricEvent> Events { get; } = new();

    public override string ToString() => Name;
}

public enum Sex
{
    Male,
    Female,
}

/// <summary>A person of history.</summary>
public sealed class HistoricFigure : HistoricInfo
{
    public HistoricFigure(string name, Sex sex, HistoryScope scope) : base(name, scope) => Sex = sex;

    public Sex Sex { get; }

    /// <summary>An epithet, "the Lame", or empty.</summary>
    public string Epithet { get; set; } = "";

    public HistoricDate Born { get; set; } = HistoricDate.Unknown;
    public HistoricDate Died { get; set; } = HistoricDate.Unknown;

    /// <summary>The house, order or realm the person belongs to, when there is one worth naming.</summary>
    public HistoricFaction? Affiliation { get; set; }

    public List<HistoricFigure> Parents { get; } = new();
    public List<HistoricFigure> Children { get; } = new();
    public List<HistoricFigure> Spouses { get; } = new();

    public string FullName => Epithet.Length == 0 ? Name : $"{Name} {Epithet}";

    /// <summary>Alive at <paramref name="round"/>, as far as the record can tell.</summary>
    public bool IsAliveAt(int round)
        => (!Born.IsKnown || Born.Round <= round) && (!Died.IsKnown || Died.Round > round);

    public void AddChild(HistoricFigure child)
    {
        if (!Children.Contains(child)) Children.Add(child);
        if (!child.Parents.Contains(this)) child.Parents.Add(this);
    }

    public void Marry(HistoricFigure other)
    {
        if (!Spouses.Contains(other)) Spouses.Add(other);
        if (!other.Spouses.Contains(this)) other.Spouses.Add(this);
    }
}

/// <summary>
/// Any organised body: a house, a guild, an order, a church institution, a realm. The base class
/// for <see cref="Realm"/>.
/// </summary>
public class HistoricFaction : HistoricInfo
{
    public HistoricFaction(string name, HistoryScope scope) : base(name, scope) { }

    public HistoricDate Founded { get; set; } = HistoricDate.Unknown;
    public HistoricDate Dissolved { get; set; } = HistoricDate.Unknown;
    public HistoricFigure? Founder { get; set; }

    public bool ExistsAt(int round)
        => (!Founded.IsKnown || Founded.Round <= round) && (!Dissolved.IsKnown || Dissolved.Round > round);
}

/// <summary>How a realm is ruled. Drawn by the realm generator; each reads differently in a chronicle.</summary>
public enum Government
{
    Kingdom,
    Principality,
    Chiefdom,
    Tribe,
    Republic,
    Theocracy,
    CityLeague,
    ClanConfederacy,
    Duchy,
    ImperialProvince,
    Commandery,
}

/// <summary>
/// A political territory: a set of world regions under one rule. Only generated worlds have realms
/// over regions; the empire's own realms (the four republics, Vu) are ordinary factions.
///
/// <para><see cref="Regions"/> is the realm's CURRENT territory while the simulation runs and its
/// territory at the present once it has finished. What it held in between is in its events.</para>
/// </summary>
public sealed class Realm : HistoricFaction
{
    public Realm(string name, Government government, HistoryScope scope) : base(name, scope)
        => Government = government;

    public Government Government { get; set; }

    /// <summary>Region ids (<c>WorldRegion.Id</c>) the realm holds.</summary>
    public SortedSet<int> Regions { get; } = new();

    /// <summary>The seat of government: a region id, or -1 when the realm holds none.</summary>
    public int CapitalRegion { get; set; } = -1;

    public HistoricFigure? Ruler { get; set; }

    /// <summary>Every ruler in order of accession, the living one last.</summary>
    public List<HistoricFigure> Rulers { get; } = new();

    public Religion? StateReligion { get; set; }

    /// <summary>The realm this one broke away from or succeeded, if any.</summary>
    public Realm? Predecessor { get; set; }

    /// <summary>The title a ruler of this realm carries.</summary>
    public string RulerTitle(Sex sex) => Government switch
    {
        Government.Kingdom          => sex == Sex.Male ? "King" : "Queen",
        Government.Principality     => sex == Sex.Male ? "Prince" : "Princess",
        Government.Duchy            => sex == Sex.Male ? "Duke" : "Duchess",
        Government.Chiefdom         => "Chief",
        Government.Tribe            => "Elder",
        Government.Republic         => "First Magistrate",
        Government.Theocracy        => "High Priest" + (sex == Sex.Female ? "ess" : ""),
        Government.CityLeague       => "Syndic",
        Government.ClanConfederacy  => "Clan-Speaker",
        Government.ImperialProvince => "Governor",
        Government.Commandery       => "Commander",
        _                           => "Ruler",
    };
}

public enum ReligionKind
{
    /// <summary>Gods, each with a domain and a nature.</summary>
    Polytheism,
    /// <summary>Spirits of places, beasts and weather rather than gods.</summary>
    Animism,
    /// <summary>The dead of the family, worshipped at the hearth.</summary>
    AncestorCult,
    /// <summary>A cult of caves and deep places, the embryo half-guessed. Kin to Medusosianism.</summary>
    HollowCult,
    /// <summary>A faith without gods: a discipline, like the Stillness of Vu.</summary>
    Discipline,
    /// <summary>Any branch of Principism.</summary>
    Principist,
    /// <summary>The faith of the jellyfish, or one of its heresies.</summary>
    Medusosian,
    /// <summary>A cult of a living or dead person made divine (the Fallen God, the Last Empress).</summary>
    PersonCult,
}

/// <summary>A faith. Principist branches and Medusosianism are empire-scoped; generated faiths are world-scoped.</summary>
public sealed class Religion : HistoricInfo
{
    public Religion(string name, ReligionKind kind, HistoryScope scope) : base(name, scope) => Kind = kind;

    public ReligionKind Kind { get; }
    public HistoricDate Founded { get; set; } = HistoricDate.Unknown;
    public HistoricFigure? Founder { get; set; }

    /// <summary>The faith this one split from or reformed, if any.</summary>
    public Religion? Parent { get; set; }

    public List<Deity> Deities { get; } = new();

    /// <summary>Proscribed as of the end of the simulation (or, for the empire, at its height).</summary>
    public bool Proscribed { get; set; }

    /// <summary>
    /// An empire faith that may exist <b>openly</b> only on the worlds its lore ties it to
    /// (Medusosianism on Belune, the Fallen God on Oox's worlds...); anywhere else it can only arrive
    /// and live as a clandestine sect. Asked through <c>WorldProfile.MayHoldOpenly</c>, which is where
    /// a lore world lets its own faith out of hiding.
    /// </summary>
    public bool ClandestineAbroad { get; set; }

    /// <summary>
    /// From when the empire persecuted this faith, if ever. A faith forbidden at a given date is what
    /// an expedition smuggles in a hidden book rather than preaches from a pulpit.
    /// </summary>
    public HistoricDate ForbiddenSince { get; set; } = HistoricDate.Unknown;

    /// <summary>When it stopped existing as a faith of its own (Early Qothism, split in three in 1004).</summary>
    public HistoricDate Superseded { get; set; } = HistoricDate.Unknown;

    public bool IsForbiddenAt(int round) => ForbiddenSince.IsKnown && ForbiddenSince.Round <= round;
}

/// <summary>How a faith exists on one world now.</summary>
public enum FaithPresence
{
    /// <summary>Practised in the open, and possibly a realm's own.</summary>
    Open,
    /// <summary>A hidden sect: books under floorboards, rites at night, martyrs when found out.</summary>
    Clandestine,
    /// <summary>Gone from this world: no one keeps it any more.</summary>
    Extinct,
}

/// <summary>What a generated organisation is. The many kinds are the point: worlds should differ in who organises them.</summary>
public enum OrganisationKind
{
    KnightlyOrder,
    ArtisanGuild,
    MerchantGuild,
    MinersGuild,
    MonasticOrder,
    ScholarsCollege,
    HealersGuild,
    BardsCompany,
    HuntersLodge,
    MercenaryCompany,
    PirateBrotherhood,
    ThievesGuild,
    AssassinsGuild,
    SecretSociety,
    /// <summary>A local branch of an empire institution: an Inquisition tribunal, a Knights' chapter, an IISTG factor-house.</summary>
    ImperialBranch,
}

/// <summary>
/// A generated faction that is not a realm: an order, a guild, a company, a society. It has a home
/// region, often a patron realm, sometimes a faith, and, if it is a branch or an imitation of an
/// empire institution, that institution as <see cref="ImperialCounterpart"/>. It can be founded,
/// imported, outlawed, go underground, resurface, split, be absorbed and die out.
/// </summary>
public sealed class Organisation : HistoricFaction
{
    public Organisation(string name, OrganisationKind kind) : base(name, HistoryScope.World) => Kind = kind;

    public OrganisationKind Kind { get; }

    /// <summary>The region it is seated in (<c>WorldRegion.Id</c>).</summary>
    public int HomeRegion { get; set; } = -1;

    /// <summary>The realm that charters, employs or protects it, if any.</summary>
    public Realm? Patron { get; set; }

    /// <summary>The faith it serves, for an order or a sect-like society.</summary>
    public Religion? Faith { get; set; }

    /// <summary>The empire institution it is a branch, a cell or an imitation of.</summary>
    public HistoricFaction? ImperialCounterpart { get; set; }

    /// <summary>Living in hiding: outlawed, or never allowed to be anything else.</summary>
    public bool Clandestine { get; set; }
}

public enum DeityDomain
{
    Sea, Love, Hunt, Harvest, War, Death, Sky, Fire, Craft, Healing, Trickery, Fate, Night,
    Forest, Beasts, Wine, Hearth, Travel, Wisdom, Storms, TheDeep, Rivers, Mountains, Salt,
    Birth, Madness, Memory, Rain, Stone, Wind, Dreams, Justice, Plague, Trade, Silence,
}

public enum DeityNature
{
    Cheerful, Wrathful, Jealous, Serene, Capricious, Stern, Mournful, Cunning, Generous, Hungry,
    Sleeping, Cruel, Patient, Proud, Shy, Mad, Lustful, Weary, Watchful, Indifferent,
}

public enum DeityForm
{
    Human, Animal, BeastHeaded, Faceless, Twin, Giant, Child, Crone, Bird, Serpent, Tree,
    Stone, Fire, Many, Unseen,
}

/// <summary>One god of a generated polytheism, drawn from a domain, a nature and a form.</summary>
public sealed class Deity : HistoricInfo
{
    public Deity(string name, DeityDomain domain, DeityNature nature, DeityForm form)
        : base(name, HistoryScope.World)
    {
        Domain = domain;
        Nature = nature;
        Form = form;
    }

    public DeityDomain Domain { get; }
    public DeityNature Nature { get; }
    public DeityForm Form { get; }
}

public enum PlaceKind
{
    City,
    Town,
    Port,
    Fortress,
    Temple,
    Sanctuary,
    Mine,
    Monastery,
    /// <summary>A Knights of the Cosmic Sea commandery: a star fort by the sea.</summary>
    Commandery,
    /// <summary>A Fogunian temple, built on the rubble of a native one.</summary>
    ImperialTemple,
    ImperialSchool,
    Pyramid,
    BurialField,
    Palace,
    Wreck,
}

/// <summary>
/// A named place, recorded as data: in which region it stands, when it was founded, and when (if
/// ever) it fell into ruin. History does not yet put places on the map.
/// </summary>
public sealed class Place : HistoricInfo
{
    public Place(string name, PlaceKind kind, HistoryScope scope) : base(name, scope) => Kind = kind;

    public PlaceKind Kind { get; }

    /// <summary>The region it stands in (<c>WorldRegion.Id</c>), or -1 for an empire place.</summary>
    public int Region { get; set; } = -1;

    public HistoricDate Founded { get; set; } = HistoricDate.Unknown;
    public HistoricDate Ruined { get; set; } = HistoricDate.Unknown;
    public HistoricFigure? Founder { get; set; }

    /// <summary>
    /// Who built it: a realm, or an empire institution (the IISTG, the Knights, the Inquisition). An
    /// imperial work is a place whose builder is empire-scoped, and that is how the post-imperial
    /// seeds find what to abandon.
    /// </summary>
    public HistoricFaction? Builder { get; set; }

    public bool IsImperialWork => Builder is { Scope: HistoryScope.Empire };

    /// <summary>The world an empire place is on, when it is on one.</summary>
    public WorldInfo? World { get; set; }

    public bool IsRuinedAt(int round) => Ruined.IsKnown && Ruined.Round <= round;
}

public enum WarOutcome
{
    Undecided,
    AttackerWon,
    DefenderWon,
    WhitePeace,
}

/// <summary>A war, with its sides and how it ended.</summary>
public sealed class War : HistoricInfo
{
    public War(string name, HistoryScope scope) : base(name, scope) { }

    public List<HistoricFaction> Attackers { get; } = new();
    public List<HistoricFaction> Defenders { get; } = new();
    public HistoricDate Started { get; set; } = HistoricDate.Unknown;
    public HistoricDate Ended { get; set; } = HistoricDate.Unknown;
    public WarOutcome Outcome { get; set; } = WarOutcome.Undecided;

    public IEnumerable<HistoricFaction> Belligerents => Attackers.Concat(Defenders);
}

/// <summary>What the empire made of a world.</summary>
public enum ImperialStatus
{
    /// <summary>Never visited. The empire is a story the sky told: one light that went out.</summary>
    Unvisited,
    /// <summary>Visited by expeditions and traders, never held.</summary>
    Visited,
    /// <summary>A province of the empire, conquered and ruled until the Hatching.</summary>
    Held,
    /// <summary>A world that no longer exists: Pyr, Varam.</summary>
    Hatched,
}

/// <summary>A world: one of the lore's, or the generated world itself.</summary>
public sealed class WorldInfo : HistoricInfo
{
    public WorldInfo(string name, ImperialStatus status, HistoryScope scope) : base(name, scope) => Status = status;

    public ImperialStatus Status { get; set; }

    /// <summary>What the world's own people called it before the empire, when that is known.</summary>
    public string NativeName { get; set; } = "";

    /// <summary>The sky ordinal of the moon it is, or -1 for a world not in the sky (hatched, or not a moon).</summary>
    public int MoonOrdinal { get; set; } = -1;
}
