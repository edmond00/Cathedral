using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.History.Generation;

namespace Cathedral.Game.History.Engine;

/// <summary>
/// A thing that must happen on a world's timeline, sown before anyone knows exactly how.
///
/// <para>Sowing is a promise ("this world's realms will be founded", "the empire will come for it");
/// sprouting is where it is decided against the world as it stands on the day. A seed that no longer
/// makes sense on its day (a war with only one realm left) answers false to <see cref="CanSprout"/> and
/// is skipped. A seed that sprouts changes the world, records events at the current round, and may sow
/// further seeds in the future, never in the past.</para>
/// </summary>
public abstract class HistorySeed
{
    protected HistorySeed(int due) => Due = due;

    /// <summary>The round it sprouts at.</summary>
    public int Due { get; }

    /// <summary>Order among seeds due on the same round: lower first. Endings before beginnings.</summary>
    public virtual int Priority => 50;

    public virtual bool CanSprout(HistorySimulation sim) => true;

    public abstract void Sprout(HistorySimulation sim);
}

/// <summary>
/// Runs a world's history forward from <see cref="HistoryCalendar.LocalHistoryStart"/> to the present:
/// takes the next due seed, advances the cursor to its round, sprouts it or skips it, until none is
/// left before the present.
///
/// <para>Also the one place the world is changed from: every seed goes through the helpers below
/// (found a realm, move a region, crown a ruler, kill someone), so the region ownership table, each
/// realm's own region set and the chronology can never disagree.</para>
/// </summary>
public sealed class HistorySimulation
{
    private readonly PriorityQueue<HistorySeed, (int Due, int Priority, long Seq)> _queue = new();
    private long _seq;

    public HistorySimulation(WorldHistory history, Random rng)
    {
        History = history;
        Rng = rng;
    }

    public WorldHistory History { get; }
    public Random Rng { get; }
    public WorldLanguage Names => History.Language;
    public Lore.EmpireLore Empire => History.Empire;

    /// <summary>The round the simulation stands at.</summary>
    public int Now => History.Chronology.Cursor == int.MinValue ? HistoryCalendar.LocalHistoryStart : History.Chronology.Cursor;

    public HistoricDate Today => HistoricDate.At(Now);

    // ── Seeds ───────────────────────────────────────────────────────────────────

    /// <summary>Sows a seed. One due after the present is dropped: it would never sprout.</summary>
    public void Sow(HistorySeed seed)
    {
        if (seed.Due < Now)
            throw new InvalidOperationException($"{seed.GetType().Name} sown at round {seed.Due}, before the cursor at {Now}");
        Stat(seed, sown: 1);
        if (seed.Due > HistoryCalendar.PresentRound) return;
        _queue.Enqueue(seed, (seed.Due, seed.Priority, _seq++));
    }

    public void Run()
    {
        History.Chronology.Advance(HistoryCalendar.LocalHistoryStart);
        while (_queue.TryDequeue(out var seed, out var key))
        {
            if (key.Due > HistoryCalendar.PresentRound) break;
            History.Chronology.Advance(key.Due);
            if (seed.CanSprout(this))
            {
                seed.Sprout(this);
                Stat(seed, sprouted: 1);
            }
            else
            {
                Stat(seed, skipped: 1);
            }
        }
        History.Chronology.Advance(HistoryCalendar.PresentRound);
    }

    private void Stat(HistorySeed seed, int sown = 0, int sprouted = 0, int skipped = 0)
    {
        string key = seed.GetType().Name;
        var s = History.SeedStats.TryGetValue(key, out var v) ? v : (0, 0, 0);
        History.SeedStats[key] = (s.Item1 + sown, s.Item2 + sprouted, s.Item3 + skipped);
    }

    public void Record(HistoricEvent e) => History.Chronology.Record(e);

    public void Chronicle(string title, string text, params HistoricInfo[] involved)
        => Record(new ChronicleEvent(Today, HistoryScope.World, title, text, involved));

    /// <summary>A round <paramref name="min"/> to <paramref name="max"/> rounds from now.</summary>
    public int Later(int min, int max) => Now + Rng.Next(min, max + 1);

    public T Pick<T>(IReadOnlyList<T> from) => from[Rng.Next(from.Count)];

    public bool Chance(double p) => Rng.NextDouble() < p;

    // ── Geography ───────────────────────────────────────────────────────────────

    public IReadOnlyList<RegionProfile> Regions => History.Geography.Regions;

    public IEnumerable<int> UnclaimedRegions => Regions.Where(r => History.OwnerOf(r.Id) == null).Select(r => r.Id);

    /// <summary>
    /// Standing realms per region. Above <see cref="CrowdedAbove"/> the world is splintered, and the
    /// seeds that would splinter it further (a new realm on free land, a routine breakaway) hold back,
    /// while conquest and union carry on. Without this pressure the map drifts, over six thousand
    /// rounds, toward one realm per region.
    /// </summary>
    public double Crowdedness => Regions.Count == 0 ? 1 : History.LivingRealms.Count() / (double)Regions.Count;

    public const double CrowdedAbove = 0.25;

    public bool IsCrowded => Crowdedness > CrowdedAbove;

    /// <summary>
    /// A realm this large (a sixth of the world, and never fewer than four regions) is a great realm:
    /// unions that would build one do not happen, and one that exists is under pressure to break up.
    /// </summary>
    public int GreatRealmSize => Math.Max(4, Regions.Count / 6);

    /// <summary>
    /// Two realms can fight when they share a land border, or when both reach the sea.
    /// </summary>
    public bool CanReach(Realm a, Realm b)
    {
        foreach (int r in a.Regions)
            foreach (int n in Regions[r].Neighbours)
                if (b.Regions.Contains(n)) return true;
        return a.Regions.Any(r => Regions[r].Coastal) && b.Regions.Any(r => Regions[r].Coastal);
    }

    /// <summary>Regions of <paramref name="of"/> that border <paramref name="facing"/> by land.</summary>
    public List<int> BorderRegions(Realm of, Realm facing)
        => of.Regions.Where(r => Regions[r].Neighbours.Any(n => facing.Regions.Contains(n))).ToList();

    /// <summary>
    /// Grows a contiguous cluster of up to <paramref name="size"/> regions from <paramref name="start"/>
    /// through regions <paramref name="allowed"/> accepts.
    /// </summary>
    public List<int> GrowCluster(int start, int size, Func<int, bool> allowed)
    {
        var cluster = new List<int> { start };
        var frontier = new List<int>(Regions[start].Neighbours.Where(allowed));
        while (cluster.Count < size && frontier.Count > 0)
        {
            int next = frontier[Rng.Next(frontier.Count)];
            frontier.RemoveAll(x => x == next);
            if (cluster.Contains(next)) continue;
            cluster.Add(next);
            foreach (int n in Regions[next].Neighbours)
                if (allowed(n) && !cluster.Contains(n) && !frontier.Contains(n)) frontier.Add(n);
        }
        return cluster;
    }

    // ── People ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// A person made up on the spot. Their birth lies before the cursor and so stays unknown; their
    /// death is sown in the future. Native names unless <paramref name="imperial"/>.
    /// <paramref name="why"/> is why history remembers them at all ("led a rising against..."),
    /// kept as their description; a caller that only learns it afterwards sets it then.
    /// </summary>
    public HistoricFigure NewAdult(HistoricFaction? affiliation, bool imperial = false, Sex? sex = null,
                                   int minYearsLeft = 18, int maxYearsLeft = 60, string why = "")
    {
        var s = sex ?? (Chance(0.62) ? Sex.Male : Sex.Female);
        string name = imperial ? ImperialNames.Person(s, Rng) : Names.PersonName(s);
        var f = new HistoricFigure(name, s, HistoryScope.World)
        {
            Epithet = imperial ? "" : RealmGenerator.MaybeEpithet(Rng),
            Affiliation = affiliation,
            Description = why,
        };
        History.Figures.Add(f);
        Sow(new Seeds.FigureDeathSeed(Later(minYearsLeft, maxYearsLeft), f));
        return f;
    }

    /// <summary>A child born today, with a recorded birth and a death sown at the end of a drawn lifespan.</summary>
    public HistoricFigure Birth(HistoricFigure parent, HistoricFaction? affiliation)
    {
        var s = Chance(0.5) ? Sex.Male : Sex.Female;
        string parentTitle = affiliation is Realm r && r.Ruler == parent ? $"{r.RulerTitle(parent.Sex)} {parent.FullName} of {r.Name}" : parent.FullName;
        var child = new HistoricFigure(Names.PersonName(s), s, HistoryScope.World)
        {
            Born = Today,
            Affiliation = affiliation,
            Epithet = RealmGenerator.MaybeEpithet(Rng),
            Description = $"Born to {parentTitle}, and so in the line of succession.",
        };
        parent.AddChild(child);
        History.Figures.Add(child);
        Record(new BirthEvent(Today, HistoryScope.World, child));
        Sow(new Seeds.FigureDeathSeed(Later(Rng.Next(3) == 0 ? 1 : 20, 85), child));
        return child;
    }

    /// <summary>Records a death now. Returns false when the person was already dead.</summary>
    public bool Kill(HistoricFigure f, DeathCause cause, HistoricFigure? killer = null)
    {
        // An empire figure's life is the lore's, shared by every world; a world may name them but
        // never end them. Their own death is already in the empire chronology.
        if (f.Scope != HistoryScope.World) return false;
        if (!f.IsAliveAt(Now)) return false;
        f.Died = Today;
        Record(new DeathEvent(Today, HistoryScope.World, f, cause, killer));
        foreach (var realm in History.LivingRealms.Where(r => r.Ruler == f).ToList())
            Succeed(realm);
        return true;
    }

    // ── Realms ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Founds a realm over <paramref name="regions"/> (taken from whoever held them), names it after its
    /// capital, crowns <paramref name="founder"/> (or someone made up), and gives it a faith.
    /// </summary>
    public Realm FoundRealm(IReadOnlyList<int> regions, Government? government = null, HistoricFigure? founder = null,
                            string? name = null, Realm? predecessor = null, bool recordFoundation = true)
    {
        int capital = regions.OrderByDescending(r => Regions[r].Habitability).ThenBy(r => r).First();
        var gov = government
               ?? History.Profile.NativeGovernment(this, Now)
               ?? RealmGenerator.DrawGovernment(Rng, Now, Regions[capital], regions.Count);
        var realm = new Realm(name ?? RealmGenerator.RealmName(gov, History.RegionNames[capital], Rng), gov, HistoryScope.World)
        {
            Founded = Today,
            CapitalRegion = capital,
            Predecessor = predecessor,
        };
        History.Realms.Add(realm);

        var previous = regions.Select(r => History.OwnerOf(r)).Where(o => o != null).Distinct().ToList();
        foreach (int r in regions) SetRegion(r, realm);

        realm.Founder = founder ?? NewAdult(realm, imperial: gov is Government.ImperialProvince or Government.Commandery,
            why: predecessor == null ? $"Founded {realm.Name}." : $"Led {realm.Name} out of {predecessor.Name}.");
        if (realm.Founder.Scope == HistoryScope.World) realm.Founder.Affiliation ??= realm;
        realm.StateReligion = FaithFor(realm);

        if (recordFoundation) Record(new FactionFoundedEvent(Today, HistoryScope.World, realm));
        Crown(realm, realm.Founder);
        if (realm.StateReligion != null)
            Record(new ReligiousChangeEvent(Today, HistoryScope.World, realm, realm.StateReligion, false, "at its founding"));

        foreach (var old in previous) CheckEmptied(old!, realm, "absorbed");
        return realm;
    }

    /// <summary>Moves one region to <paramref name="to"/> (null: unclaimed), keeping both sides' sets in step.</summary>
    public void SetRegion(int region, Realm? to)
    {
        var from = History.OwnerOf(region);
        if (from == to) return;
        from?.Regions.Remove(region);
        to?.Regions.Add(region);
        History.SetOwner(region, to);
        // A realm proclaimed before it holds anything (a province at the start of its conquest) takes
        // its seat in the first region it gains.
        if (to != null && to.CapitalRegion < 0) to.CapitalRegion = region;
        if (from != null && from.CapitalRegion == region)
            from.CapitalRegion = from.Regions.Count > 0 ? from.Regions.OrderByDescending(r => Regions[r].Habitability).ThenBy(r => r).First() : -1;
    }

    /// <summary>Hands regions over and records it; dissolves the loser if it has nothing left.</summary>
    public void Transfer(IReadOnlyList<int> regions, Realm to, string how)
    {
        if (regions.Count == 0) return;
        var losers = regions.Select(r => History.OwnerOf(r)).Distinct().ToList();
        foreach (var loser in losers)
        {
            var lost = regions.Where(r => History.OwnerOf(r) == loser).ToList();
            foreach (int r in lost) SetRegion(r, to);
            Record(new TerritoryEvent(Today, HistoryScope.World, to, loser, lost, how));
        }
        foreach (var loser in losers)
            if (loser != null) CheckEmptied(loser, to, "conquered");
    }

    private void CheckEmptied(Realm realm, Realm? by, string how)
    {
        if (realm.Regions.Count == 0 && !realm.Dissolved.IsKnown) Dissolve(realm, by, how);
    }

    public void Dissolve(Realm realm, Realm? by, string how)
    {
        if (realm.Dissolved.IsKnown) return;
        foreach (int r in realm.Regions.ToList()) SetRegion(r, by);
        realm.Dissolved = Today;
        Record(new FactionDissolvedEvent(Today, HistoryScope.World, realm, by, how));
        if (History.Province == realm) History.Province = null;
    }

    /// <summary>Breaks <paramref name="regions"/> off <paramref name="from"/> into a new realm.</summary>
    public Realm Split(Realm from, IReadOnlyList<int> regions, string why, HistoricFigure? leader = null, Government? government = null)
    {
        var breakaway = FoundRealm(regions, government, leader, predecessor: from, recordFoundation: false);
        Record(new RealmSplitEvent(Today, HistoryScope.World, from, breakaway, why));
        return breakaway;
    }

    public void Crown(Realm realm, HistoricFigure ruler)
    {
        realm.Ruler = ruler;
        realm.Rulers.Add(ruler);
        Record(new AccessionEvent(Today, HistoryScope.World, ruler, realm, realm.RulerTitle(ruler.Sex)));
        // A ruler of a native realm may found a line: children come later, with recorded births.
        if (ruler.Scope == HistoryScope.World
            && realm.Government is not (Government.ImperialProvince or Government.Commandery) && Chance(0.35))
            Sow(new Seeds.HeirBirthSeed(Later(1, 15), realm, ruler));
    }

    /// <summary>
    /// Picks and crowns the next ruler of <paramref name="realm"/>: a living child of the dead ruler,
    /// else a relative or usurper made up on the spot. A contested succession may split the realm.
    /// </summary>
    public void Succeed(Realm realm)
    {
        if (realm.Dissolved.IsKnown) return;
        var dead = realm.Ruler;
        if (realm.Government is Government.ImperialProvince)
        {
            Crown(realm, NewAdult(realm, imperial: true, why: $"A governor sent from Pyr to rule {realm.Name}."));
            return;
        }
        var heir = dead?.Children.Where(c => c.Scope == HistoryScope.World && c.IsAliveAt(Now)
                                             && c.Born.IsKnown && Now - c.Born.Round >= 12)
                                 .OrderBy(c => c.Born.Round).FirstOrDefault();
        var next = heir ?? NewAdult(realm, why: dead == null
            ? $"Took the rule of {realm.Name}."
            : $"Took the rule of {realm.Name} when {dead.FullName} died with no heir of age.");
        Crown(realm, next);
        if (heir == null && realm.Regions.Count >= 2 && Chance(0.2))
            Sow(new Seeds.RealmSplitSeed(Later(0, 3), realm, "a contested succession"));
    }

    // ── Faith and places ────────────────────────────────────────────────────────

    /// <summary>
    /// The faith a new realm takes: its predecessor's or a neighbour's most of the time, a new one now
    /// and then, and one of the world's old faiths otherwise.
    /// </summary>
    public Religion? FaithFor(Realm realm)
    {
        var tolerated = History.LivingFaiths.Where(CanAdoptOpenly).ToList();
        if (realm.Predecessor?.StateReligion is { } inherited && CanAdoptOpenly(inherited) && Chance(0.7)) return inherited;

        var neighbourFaiths = History.LivingRealms
            .Where(o => o != realm && o.StateReligion is { } f && CanAdoptOpenly(f) && CanReach(realm, o))
            .Select(o => o.StateReligion!)
            .ToList();
        if (neighbourFaiths.Count > 0 && Chance(0.6)) return Pick(neighbourFaiths);
        if (tolerated.Count == 0 || Chance(0.03)) return NewReligion(null);
        return Pick(tolerated);
    }

    public Religion NewReligion(HistoricFigure? founder, Religion? parent = null, ReligionKind? kind = null)
    {
        var r = ReligionGenerator.Create(Names, Rng, Today, founder, parent, kind);
        History.Religions.Add(r);
        History.Presence[r] = FaithPresence.Open;
        Record(new ReligionFoundedEvent(Today, HistoryScope.World, r));
        return r;
    }

    /// <summary>Adds a faith the world already has when its history starts. No event: it predates the record.</summary>
    public void AddAncientFaith(Religion r)
    {
        History.Religions.Add(r);
        History.Presence[r] = FaithPresence.Open;
    }

    public bool IsProscribed(Religion r) => History.Proscribed.Contains(r);

    /// <summary>
    /// Whether a realm could take <paramref name="r"/> as its own here and now: still kept on the world,
    /// not under a ban, and a faith this world may hold openly at all (see
    /// <c>WorldProfile.MayHoldOpenly</c>).
    /// </summary>
    public bool CanAdoptOpenly(Religion r)
        => History.PresenceOf(r) != FaithPresence.Extinct && !IsProscribed(r) && History.Profile.MayHoldOpenly(r);

    /// <summary>
    /// Forbids a faith. Its open followers go underground (it becomes a clandestine sect) unless the
    /// persecution is the end of it, which <paramref name="extinguishChance"/> decides.
    /// </summary>
    public void Proscribe(HistoricFaction by, Religion r, string how, double extinguishChance = 0.15)
    {
        if (!History.Proscribed.Add(r)) return;
        Record(new ReligiousChangeEvent(Today, HistoryScope.World, by, r, true, how));
        if (History.PresenceOf(r) != FaithPresence.Open) return;
        if (Chance(extinguishChance))
            DieOut(r, $"{r.Name} does not survive its proscription by {by.Name}: its last keepers recant or die.", by);
        else
            SetPresence(r, FaithPresence.Clandestine, $"{r.Name} goes underground: forbidden by {by.Name}, its faithful keep the rites in secret.", by);
    }

    /// <summary>An empire faith reaches this world openly: it joins the world's list of faiths.</summary>
    public Religion Present(Religion faith)
    {
        if (!History.Religions.Contains(faith)) History.Religions.Add(faith);
        if (History.PresenceOf(faith) == FaithPresence.Extinct) History.Presence[faith] = FaithPresence.Open;
        return faith;
    }

    /// <summary>
    /// A faith arrives in hiding: a forbidden book in a sailor's kit, an exile preaching at night.
    /// Nothing happens when it is already kept openly here.
    /// </summary>
    public void Smuggle(Religion faith, string text, params HistoricInfo[] others)
    {
        if (History.PresenceOf(faith) == FaithPresence.Open && History.Religions.Contains(faith)) return;
        if (!History.Religions.Contains(faith)) History.Religions.Add(faith);
        History.Presence[faith] = FaithPresence.Clandestine;
        Record(new FaithPresenceEvent(Today, HistoryScope.World, faith, FaithPresence.Clandestine, text, others));
    }

    private void SetPresence(Religion faith, FaithPresence to, string text, params HistoricInfo[] others)
    {
        History.Presence[faith] = to;
        Record(new FaithPresenceEvent(Today, HistoryScope.World, faith, to, text, others));
    }

    /// <summary>The last keepers of a faith die or recant. Any realm still holding it as its own lets it go.</summary>
    public void DieOut(Religion faith, string text, params HistoricInfo[] others)
    {
        if (History.PresenceOf(faith) == FaithPresence.Extinct) return;
        SetPresence(faith, FaithPresence.Extinct, text, others);
        foreach (var realm in History.LivingRealms.Where(r => r.StateReligion == faith)) realm.StateReligion = null;
    }

    /// <summary>
    /// A hidden faith comes out: lifted from any ban and taken up by <paramref name="realm"/>. Refused
    /// (it stays hidden) where the world may not hold it openly.
    /// </summary>
    public bool Surface(Religion faith, Realm realm, string how)
    {
        if (!History.Profile.MayHoldOpenly(faith)) return false;
        History.Proscribed.Remove(faith);
        SetPresence(faith, FaithPresence.Open, $"{faith.Name} comes out of hiding: {how}.", realm);
        Adopt(realm, faith, how);
        return true;
    }

    /// <summary>
    /// A realm takes a faith as its own. A faith this world may not hold openly cannot be adopted:
    /// it arrives (or stays) as a hidden sect instead, which is the whole of the clandestine rule.
    /// </summary>
    public void Adopt(Realm realm, Religion religion, string how)
    {
        if (!History.Profile.MayHoldOpenly(religion))
        {
            Smuggle(religion, $"{religion.Name} cannot be kept openly on {History.World.Name}; in {realm.Name} it lives as a hidden sect ({how}).", realm);
            return;
        }
        Present(religion);
        History.Presence[religion] = FaithPresence.Open;
        realm.StateReligion = religion;
        Record(new ReligiousChangeEvent(Today, HistoryScope.World, realm, religion, false, how));
    }

    // ── Organisations ───────────────────────────────────────────────────────────

    /// <summary>
    /// Founds an order, guild, company or society seated in <paramref name="region"/>. Named by the
    /// generator unless <paramref name="name"/> is given; founded by someone made up on the spot unless
    /// <paramref name="founder"/> is. A clandestine one is founded in hiding.
    /// </summary>
    public Organisation FoundOrganisation(OrganisationKind kind, int region, Realm? patron, string? name = null,
                                          HistoricFigure? founder = null, HistoricFaction? counterpart = null,
                                          Religion? faith = null, bool clandestine = false, string? description = null)
    {
        string place = region >= 0 ? History.RegionNames[region] : History.World.Name;
        var org = new Organisation(name ?? OrganisationGenerator.Name(kind, Names, Rng, place, faith), kind)
        {
            Founded = Today,
            HomeRegion = region,
            Patron = patron,
            Faith = faith,
            ImperialCounterpart = counterpart,
            Clandestine = clandestine,
            Description = description ?? OrganisationGenerator.Purpose(kind),
        };
        org.Founder = founder ?? NewAdult(org, imperial: kind == OrganisationKind.ImperialBranch,
            why: kind == OrganisationKind.ImperialBranch ? $"Sent from Pyr to head {org.Name}." : $"Founded {org.Name}.");
        History.Organisations.Add(org);
        Record(new FactionFoundedEvent(Today, HistoryScope.World, org));
        return org;
    }

    public void ChangeOrganisation(Organisation org, OrganisationChange change, string text, params HistoricInfo[] others)
    {
        switch (change)
        {
            case OrganisationChange.Outlawed:
            case OrganisationChange.WentUnderground: org.Clandestine = true; break;
            case OrganisationChange.Surfaced:        org.Clandestine = false; break;
        }
        Record(new OrganisationEvent(Today, HistoryScope.World, org, change, text, others));
    }

    public void DissolveOrganisation(Organisation org, HistoricFaction? by, string how)
    {
        if (org.Dissolved.IsKnown) return;
        org.Dissolved = Today;
        org.Clandestine = false;
        Record(new FactionDissolvedEvent(Today, HistoryScope.World, org, by, how));
    }

    public Place NewPlace(PlaceKind kind, int region, HistoricFaction? by, string? name = null)
    {
        var p = new Place(name ?? Names.PlaceName(), kind, HistoryScope.World)
        {
            Region = region,
            Founded = Today,
            Builder = by,
        };
        History.Places.Add(p);
        Record(new PlaceFoundedEvent(Today, HistoryScope.World, p, by));
        return p;
    }

    public void Ruin(Place place, string how)
    {
        if (place.Ruined.IsKnown) return;
        place.Ruined = Today;
        Record(new PlaceRuinedEvent(Today, HistoryScope.World, place, how));
    }
}
