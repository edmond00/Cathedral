using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cathedral.Game.History.Generation;
using Cathedral.Game.History.Lore;
using Cathedral.Game.History.Worlds;

namespace Cathedral.Game.History;

/// <summary>
/// The history of one generated world, complete to <see cref="HistoryCalendar.PresentRound"/>: its
/// chronology, everything that chronology is about, and which realm holds each region today.
///
/// <para><b>A pure function of the world seed</b>, like the terrain it is laid over. It is never saved;
/// Continue regenerates it and compares <see cref="Hash"/> with the one the save recorded.</para>
///
/// <para>The empire's own history is not copied in. It is <see cref="Empire"/>, shared by every world;
/// this world's chronology names empire infos (Oox, the Inquisition) where they touched it, but those
/// infos never list this world's events (see <see cref="Chronology"/>), because they are shared.</para>
/// </summary>
public sealed class WorldHistory
{
    public WorldHistory(WorldInfo world, WorldProfile profile, HistoryGeography geography, WorldLanguage language)
    {
        World = world;
        Profile = profile;
        Geography = geography;
        Language = language;
        Chronology = new Chronology(HistoryScope.World);
        _owner = new Realm?[geography.Regions.Count];
        RegionNames = new string[geography.Regions.Count];
    }

    public WorldInfo World { get; }
    public WorldProfile Profile { get; }
    public HistoryGeography Geography { get; }
    public WorldLanguage Language { get; }
    public Chronology Chronology { get; }
    public EmpireLore Empire => EmpireLore.Instance;

    /// <summary>The native name of every region, indexed by region id.</summary>
    public string[] RegionNames { get; }

    public List<Realm> Realms { get; } = new();
    public List<HistoricFigure> Figures { get; } = new();
    public List<Religion> Religions { get; } = new();
    public List<Place> Places { get; } = new();
    public List<War> Wars { get; } = new();

    /// <summary>
    /// The faiths proscribed on this world as of now. Held here and not on <see cref="Religion"/>,
    /// because a faith can be an empire faith shared by every world (Medusosianism, the Fallen God):
    /// the Inquisition proscribing it on one world must not proscribe it on the next one generated.
    /// </summary>
    public HashSet<Religion> Proscribed { get; } = new();

    /// <summary>
    /// How each faith on <see cref="Religions"/> exists here now: open, clandestine or extinct. Kept
    /// per world for the same reason as <see cref="Proscribed"/>: an empire faith is shared.
    /// <see cref="Religions"/> is every faith that ever reached the world; this says which still live.
    /// </summary>
    public Dictionary<Religion, FaithPresence> Presence { get; } = new();

    public FaithPresence PresenceOf(Religion r) => Presence.TryGetValue(r, out var p) ? p : FaithPresence.Extinct;

    /// <summary>Faiths kept on this world now, openly or in hiding, in the order they arrived.</summary>
    public IEnumerable<Religion> LivingFaiths => Religions.Where(r => PresenceOf(r) != FaithPresence.Extinct);

    /// <summary>Every order, guild, company and society the world has had, in the order founded.</summary>
    public List<Organisation> Organisations { get; } = new();

    public IEnumerable<Organisation> LivingOrganisations => Organisations.Where(o => !o.Dissolved.IsKnown);

    /// <summary>The imperial province, while there is one (held worlds only).</summary>
    public Realm? Province { get; internal set; }

    private readonly Realm?[] _owner;

    /// <summary>Who holds <paramref name="regionId"/> now (at the present, once generation is done), or null for unclaimed land.</summary>
    public Realm? OwnerOf(int regionId)
        => regionId >= 0 && regionId < _owner.Length ? _owner[regionId] : null;

    internal void SetOwner(int regionId, Realm? realm) => _owner[regionId] = realm;

    public IEnumerable<Realm> LivingRealms => Realms.Where(r => r.Regions.Count > 0 && !r.Dissolved.IsKnown);

    /// <summary>
    /// A fingerprint of the whole chronology. Stored in the save, so that Continue can refuse a save
    /// whose history this build would not regenerate identically.
    /// </summary>
    public int Hash { get; internal set; }

    /// <summary>How long generation took. Reported by the audit and the log, never part of the hash.</summary>
    public long GenerationMilliseconds { get; internal set; }

    /// <summary>Counts of sown, sprouted and skipped seeds, by seed type, for the audit.</summary>
    public Dictionary<string, (int Sown, int Sprouted, int Skipped)> SeedStats { get; } = new();

    internal static int ComputeHash(Chronology chronology, WorldHistory history)
    {
        unchecked
        {
            uint h = 2166136261u;
            void Mix(string s)
            {
                foreach (char c in s) { h ^= c; h *= 16777619u; }
            }
            foreach (var e in chronology.Events)
            {
                Mix(e.Date.Round.ToString());
                Mix(e.Describe());
            }
            for (int r = 0; r < history._owner.Length; r++) Mix(history._owner[r]?.Name ?? "-");
            return (int)h;
        }
    }

    /// <summary>
    /// The world's relation to the empire in a few words, for the moon box: "held from 2588 FC",
    /// "visited from 2612 FC, never held", "never reached". Read off the chronology, so a lore world
    /// says its own dates (Belune: held from 1321 FC).
    /// </summary>
    public string EmpireRelation()
    {
        var stages = Chronology.Events.OfType<ImperialEvent>().ToList();
        HistoricDate? When(ImperialStage stage) => stages.FirstOrDefault(e => e.Stage == stage)?.Date;
        return World.Status switch
        {
            ImperialStatus.Held when When(ImperialStage.ConquestCompleted) is { } held => $"held from {held}",
            ImperialStatus.Held    => "held",
            ImperialStatus.Visited when When(ImperialStage.Contact) is { } met => $"visited from {met}",
            ImperialStatus.Visited => "visited, never held",
            _                      => "never reached",
        };
    }

    /// <summary>A plain-text summary, for the CLI and the audit.</summary>
    public string Summary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"{World.Name}: {World.Status}, profile {Profile.GetType().Name}, "
                    + $"{Geography.Regions.Count} regions on {Geography.LandmassCount} landmass(es).");
        sb.AppendLine($"  {Chronology.Count} events, {Figures.Count} figures, {Realms.Count} realms "
                    + $"({LivingRealms.Count()} standing), {Religions.Count} faiths, {Places.Count} places, "
                    + $"{Wars.Count} wars. Hash {Hash:X8}.");
        foreach (var realm in LivingRealms.OrderByDescending(r => r.Regions.Count))
            sb.AppendLine($"  {realm.Name} ({realm.Government}): {realm.Regions.Count} region(s), "
                        + $"ruler {realm.Ruler?.FullName ?? "none"}, faith {realm.StateReligion?.Name ?? "none"}");
        int unclaimed = Geography.Regions.Count(r => OwnerOf(r.Id) == null);
        if (unclaimed > 0) sb.AppendLine($"  {unclaimed} region(s) unclaimed.");
        return sb.ToString();
    }
}
