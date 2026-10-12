using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.History;

/// <summary>
/// Something that happened, on a date, to some infos. The base class of every chronology entry.
///
/// <para>Subclasses carry their own typed fields and name what they involve through
/// <see cref="Involved"/>; recording an event in a <see cref="Chronology"/> links it into each involved
/// info's <see cref="HistoricInfo.Events"/>. <see cref="Describe"/> is one neutral sentence of what
/// happened: the true version, which books will later distort.</para>
/// </summary>
public abstract class HistoricEvent
{
    protected HistoricEvent(HistoricDate date, HistoryScope scope)
    {
        Date = date;
        Scope = scope;
    }

    public HistoricDate Date { get; }
    public HistoryScope Scope { get; }

    /// <summary>Every info this event is about, most important first.</summary>
    public abstract IEnumerable<HistoricInfo> Involved { get; }

    /// <summary>The neutral, true account of what happened, in one sentence.</summary>
    public abstract string Describe();

    public override string ToString() => $"{Date}: {Describe()}";
}

// ── People ──────────────────────────────────────────────────────────────────────

public sealed class BirthEvent : HistoricEvent
{
    public BirthEvent(HistoricDate date, HistoryScope scope, HistoricFigure figure) : base(date, scope)
        => Figure = figure;

    public HistoricFigure Figure { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { Figure }.Concat(Figure.Parents);

    public override string Describe()
    {
        if (Figure.Parents.Count == 0) return $"{Figure.FullName} is born.";
        return $"{Figure.FullName} is born to {string.Join(" and ", Figure.Parents.Select(p => p.Name))}.";
    }
}

public enum DeathCause
{
    Natural,
    Illness,
    Battle,
    Murder,
    Execution,
    Accident,
    Plague,
    BuriedAlive,
    Fire,
    Hatching,
    Unknown,
}

public sealed class DeathEvent : HistoricEvent
{
    public DeathEvent(HistoricDate date, HistoryScope scope, HistoricFigure figure, DeathCause cause,
                      HistoricFigure? killer = null) : base(date, scope)
    {
        Figure = figure;
        Cause = cause;
        Killer = killer;
    }

    public HistoricFigure Figure { get; }
    public DeathCause Cause { get; }
    public HistoricFigure? Killer { get; }

    public override IEnumerable<HistoricInfo> Involved
        => Killer == null ? new HistoricInfo[] { Figure } : new HistoricInfo[] { Figure, Killer };

    public override string Describe() => Cause switch
    {
        DeathCause.Natural     => $"{Figure.FullName} dies of age.",
        DeathCause.Illness     => $"{Figure.FullName} dies of illness.",
        DeathCause.Battle      => Killer == null ? $"{Figure.FullName} is killed in battle." : $"{Figure.FullName} is killed in battle by {Killer.Name}.",
        DeathCause.Murder      => Killer == null ? $"{Figure.FullName} is murdered." : $"{Figure.FullName} is murdered by {Killer.Name}.",
        DeathCause.Execution   => $"{Figure.FullName} is executed.",
        DeathCause.Accident    => $"{Figure.FullName} dies in an accident.",
        DeathCause.Plague      => $"{Figure.FullName} dies of plague.",
        DeathCause.BuriedAlive => $"{Figure.FullName} is buried alive.",
        DeathCause.Fire        => $"{Figure.FullName} dies in a fire.",
        DeathCause.Hatching    => $"{Figure.FullName} dies when the world hatches.",
        _                      => $"{Figure.FullName} dies.",
    };
}

/// <summary>A person takes a title or office: a throne, a patriarchate, a command.</summary>
public sealed class AccessionEvent : HistoricEvent
{
    public AccessionEvent(HistoricDate date, HistoryScope scope, HistoricFigure figure, HistoricFaction office,
                          string title) : base(date, scope)
    {
        Figure = figure;
        Office = office;
        Title = title;
    }

    public HistoricFigure Figure { get; }
    public HistoricFaction Office { get; }
    public string Title { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { Figure, Office };

    public override string Describe() => $"{Figure.FullName} becomes {Title} of {Office.Name}.";
}

public sealed class MarriageEvent : HistoricEvent
{
    public MarriageEvent(HistoricDate date, HistoryScope scope, HistoricFigure a, HistoricFigure b) : base(date, scope)
    {
        A = a;
        B = b;
    }

    public HistoricFigure A { get; }
    public HistoricFigure B { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { A, B };

    public override string Describe() => $"{A.Name} marries {B.Name}.";
}

// ── Factions and realms ─────────────────────────────────────────────────────────

public sealed class FactionFoundedEvent : HistoricEvent
{
    public FactionFoundedEvent(HistoricDate date, HistoryScope scope, HistoricFaction faction) : base(date, scope)
        => Faction = faction;

    public HistoricFaction Faction { get; }

    public override IEnumerable<HistoricInfo> Involved
        => Faction.Founder == null ? new HistoricInfo[] { Faction } : new HistoricInfo[] { Faction, Faction.Founder };

    public override string Describe()
    {
        string by = Faction.Founder == null ? "" : $" by {Faction.Founder.Name}";
        if (Faction is Realm realm)
            return $"{Faction.Name} is founded{by}, holding {realm.Regions.Count} region(s).";
        return $"{Faction.Name} is founded{by}.";
    }
}

public sealed class FactionDissolvedEvent : HistoricEvent
{
    public FactionDissolvedEvent(HistoricDate date, HistoryScope scope, HistoricFaction faction,
                                 HistoricFaction? absorbedBy, string how) : base(date, scope)
    {
        Faction = faction;
        AbsorbedBy = absorbedBy;
        How = how;
    }

    public HistoricFaction Faction { get; }
    public HistoricFaction? AbsorbedBy { get; }

    /// <summary>A short clause: "conquered", "broken apart", "extinguished".</summary>
    public string How { get; }

    public override IEnumerable<HistoricInfo> Involved
        => AbsorbedBy == null ? new HistoricInfo[] { Faction } : new HistoricInfo[] { Faction, AbsorbedBy };

    public override string Describe()
        => AbsorbedBy == null ? $"{Faction.Name} comes to an end ({How})." : $"{Faction.Name} comes to an end, {How} by {AbsorbedBy.Name}.";
}

/// <summary>Part of a realm breaks away and becomes a realm of its own.</summary>
public sealed class RealmSplitEvent : HistoricEvent
{
    public RealmSplitEvent(HistoricDate date, HistoryScope scope, Realm from, Realm breakaway, string why)
        : base(date, scope)
    {
        From = from;
        Breakaway = breakaway;
        Why = why;
    }

    public Realm From { get; }
    public Realm Breakaway { get; }
    public string Why { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { Breakaway, From };

    public override string Describe()
        => $"{Breakaway.Name} breaks away from {From.Name} ({Why}), taking {Breakaway.Regions.Count} region(s).";
}

/// <summary>Regions change hands: by conquest, treaty, settlement or inheritance.</summary>
public sealed class TerritoryEvent : HistoricEvent
{
    public TerritoryEvent(HistoricDate date, HistoryScope scope, Realm gainer, Realm? loser,
                          IReadOnlyList<int> regions, string how) : base(date, scope)
    {
        Gainer = gainer;
        Loser = loser;
        Regions = regions;
        How = how;
    }

    public Realm Gainer { get; }
    public Realm? Loser { get; }
    public IReadOnlyList<int> Regions { get; }
    public string How { get; }

    public override IEnumerable<HistoricInfo> Involved
        => Loser == null ? new HistoricInfo[] { Gainer } : new HistoricInfo[] { Gainer, Loser };

    public override string Describe()
    {
        string what = Regions.Count == 1 ? $"region {Regions[0]}" : $"{Regions.Count} regions";
        return Loser == null
            ? $"{Gainer.Name} takes {what} ({How})."
            : $"{Gainer.Name} takes {what} from {Loser.Name} ({How}).";
    }
}

// ── War ─────────────────────────────────────────────────────────────────────────

public sealed class WarStartedEvent : HistoricEvent
{
    public WarStartedEvent(HistoricDate date, HistoryScope scope, War war, string why) : base(date, scope)
    {
        War = war;
        Why = why;
    }

    public War War { get; }
    public string Why { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { War }.Concat(War.Belligerents);

    public override string Describe()
        => $"{string.Join(" and ", War.Attackers.Select(a => a.Name))} go to war against "
         + $"{string.Join(" and ", War.Defenders.Select(d => d.Name))} ({Why}): {War.Name}.";
}

public sealed class BattleEvent : HistoricEvent
{
    public BattleEvent(HistoricDate date, HistoryScope scope, War? war, string name, HistoricFaction? victor,
                       HistoricFaction? loser, Place? place = null) : base(date, scope)
    {
        War = war;
        Name = name;
        Victor = victor;
        Loser = loser;
        Place = place;
    }

    public War? War { get; }
    public string Name { get; }
    public HistoricFaction? Victor { get; }
    public HistoricFaction? Loser { get; }
    public Place? Place { get; }

    public override IEnumerable<HistoricInfo> Involved
    {
        get
        {
            if (War != null) yield return War;
            if (Victor != null) yield return Victor;
            if (Loser != null) yield return Loser;
            if (Place != null) yield return Place;
        }
    }

    public override string Describe()
        => Victor == null ? $"{Name}." : $"{Name}: {Victor.Name} defeats {Loser?.Name ?? "its enemies"}.";
}

public sealed class WarEndedEvent : HistoricEvent
{
    public WarEndedEvent(HistoricDate date, HistoryScope scope, War war) : base(date, scope) => War = war;

    public War War { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { War }.Concat(War.Belligerents);

    public override string Describe() => War.Outcome switch
    {
        WarOutcome.AttackerWon => $"{War.Name} ends in victory for {string.Join(" and ", War.Attackers.Select(a => a.Name))}.",
        WarOutcome.DefenderWon => $"{War.Name} ends in victory for {string.Join(" and ", War.Defenders.Select(d => d.Name))}.",
        WarOutcome.WhitePeace  => $"{War.Name} ends with nothing won.",
        _                      => $"{War.Name} ends.",
    };
}

// ── Faith ───────────────────────────────────────────────────────────────────────

public sealed class ReligionFoundedEvent : HistoricEvent
{
    public ReligionFoundedEvent(HistoricDate date, HistoryScope scope, Religion religion) : base(date, scope)
        => Religion = religion;

    public Religion Religion { get; }

    public override IEnumerable<HistoricInfo> Involved
    {
        get
        {
            yield return Religion;
            if (Religion.Founder != null) yield return Religion.Founder;
            if (Religion.Parent != null) yield return Religion.Parent;
        }
    }

    public override string Describe()
    {
        string from = Religion.Parent == null ? "" : $", breaking from {Religion.Parent.Name}";
        string by = Religion.Founder == null ? "" : $" by {Religion.Founder.Name}";
        return $"{Religion.Name} is founded{by}{from}.";
    }
}

/// <summary>A realm takes a faith as its own, or a faith is imposed on it, or proscribed in it.</summary>
public sealed class ReligiousChangeEvent : HistoricEvent
{
    public ReligiousChangeEvent(HistoricDate date, HistoryScope scope, HistoricFaction faction, Religion religion,
                                bool proscribed, string how) : base(date, scope)
    {
        Faction = faction;
        Religion = religion;
        Proscribed = proscribed;
        How = how;
    }

    public HistoricFaction Faction { get; }
    public Religion Religion { get; }
    public bool Proscribed { get; }
    public string How { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { Faction, Religion };

    public override string Describe()
        => Proscribed
            ? $"{Faction.Name} proscribes {Religion.Name} ({How})."
            : $"{Faction.Name} adopts {Religion.Name} ({How}).";
}

/// <summary>
/// A faith changes how it exists on a world: smuggled in as a hidden sect, driven underground,
/// resurfacing, dying out. Carries its own sentence, because the flavour (a book in a sailor's kit,
/// martyrs in a market square) is the whole interest of it.
/// </summary>
public sealed class FaithPresenceEvent : HistoricEvent
{
    public FaithPresenceEvent(HistoricDate date, HistoryScope scope, Religion religion, FaithPresence becomes,
                              string text, params HistoricInfo[] others) : base(date, scope)
    {
        Religion = religion;
        Becomes = becomes;
        Text = text;
        _others = others;
    }

    private readonly HistoricInfo[] _others;

    public Religion Religion { get; }
    public FaithPresence Becomes { get; }
    public string Text { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { Religion }.Concat(_others);

    public override string Describe() => Text;
}

public enum OrganisationChange
{
    /// <summary>A branch or a cell of something from beyond the world sets up here.</summary>
    Imported,
    Outlawed,
    WentUnderground,
    Surfaced,
    Chartered,
    Schism,
    Absorbed,
    BecameIndependent,
    Spread,
}

/// <summary>Something happens to an order, guild, company or society other than its founding or its end.</summary>
public sealed class OrganisationEvent : HistoricEvent
{
    public OrganisationEvent(HistoricDate date, HistoryScope scope, Organisation organisation, OrganisationChange change,
                             string text, params HistoricInfo[] others) : base(date, scope)
    {
        Organisation = organisation;
        Change = change;
        Text = text;
        _others = others;
    }

    private readonly HistoricInfo[] _others;

    public Organisation Organisation { get; }
    public OrganisationChange Change { get; }
    public string Text { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { Organisation }.Concat(_others);

    public override string Describe() => Text;
}

// ── Places ──────────────────────────────────────────────────────────────────────

public sealed class PlaceFoundedEvent : HistoricEvent
{
    public PlaceFoundedEvent(HistoricDate date, HistoryScope scope, Place place, HistoricFaction? by) : base(date, scope)
    {
        Place = place;
        By = by;
    }

    public Place Place { get; }
    public HistoricFaction? By { get; }

    public override IEnumerable<HistoricInfo> Involved
        => By == null ? new HistoricInfo[] { Place } : new HistoricInfo[] { Place, By };

    public override string Describe()
    {
        string where = Place.Region >= 0 ? $" in region {Place.Region}" : "";
        string by = By == null ? "" : $" by {By.Name}";
        return $"{Place.Name} ({Place.Kind.ToString().ToLowerInvariant()}) is founded{where}{by}.";
    }
}

public sealed class PlaceRuinedEvent : HistoricEvent
{
    public PlaceRuinedEvent(HistoricDate date, HistoryScope scope, Place place, string how) : base(date, scope)
    {
        Place = place;
        How = how;
    }

    public Place Place { get; }
    public string How { get; }

    public override IEnumerable<HistoricInfo> Involved => new HistoricInfo[] { Place };

    public override string Describe() => $"{Place.Name} falls into ruin ({How}).";
}

// ── The empire and the cosmos ───────────────────────────────────────────────────

public enum ImperialStage
{
    /// <summary>The first imperial ship comes down the vortex.</summary>
    Contact,
    /// <summary>The conquest begins.</summary>
    ConquestBegun,
    /// <summary>The last free realm falls and the world is a province.</summary>
    ConquestCompleted,
    /// <summary>The Principian Inquisition arrives.</summary>
    InquisitionArrives,
    /// <summary>Pyr's light goes out; the ships stop coming.</summary>
    Stranding,
}

/// <summary>A step in the empire's dealings with a world.</summary>
public sealed class ImperialEvent : HistoricEvent
{
    public ImperialEvent(HistoricDate date, HistoryScope scope, ImperialStage stage, WorldInfo world,
                         HistoricInfo? agent, string detail) : base(date, scope)
    {
        Stage = stage;
        World = world;
        Agent = agent;
        Detail = detail;
    }

    public ImperialStage Stage { get; }
    public WorldInfo World { get; }

    /// <summary>Who did it: a conqueror, an expedition captain, an institution.</summary>
    public HistoricInfo? Agent { get; }

    public string Detail { get; }

    public override IEnumerable<HistoricInfo> Involved
        => Agent == null ? new HistoricInfo[] { World } : new HistoricInfo[] { World, Agent };

    public override string Describe() => Detail;
}

/// <summary>
/// Anything that fits no narrower type: a lore episode, a plague, an omen. Carries its own title and
/// sentence, and names what it involves.
/// </summary>
public sealed class ChronicleEvent : HistoricEvent
{
    public ChronicleEvent(HistoricDate date, HistoryScope scope, string title, string text,
                          params HistoricInfo[] involved) : base(date, scope)
    {
        Title = title;
        Text = text;
        _involved = involved;
    }

    private readonly HistoricInfo[] _involved;

    public string Title { get; }
    public string Text { get; }

    public override IEnumerable<HistoricInfo> Involved => _involved;

    public override string Describe() => Title.Length == 0 ? Text : $"{Title}. {Text}";
}
