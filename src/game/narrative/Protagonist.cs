using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative.Routines;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The player-controlled protagonist.  Extends <see cref="PartyMember"/> with features
/// that are exclusive to the protagonist: journal, companion party list, and location tracking.
///
/// Shared state (body, organs, modiMentis, inventory, …) lives in <see cref="PartyMember"/>.
/// </summary>
public class Protagonist : PartyMember
{
    // ── Protagonist-only data ────────────────────────────────────

    /// <summary>Journal entries written throughout the journey.</summary>
    public List<string> JournalEntries { get; set; } = new();

    /// <summary>Named companions travelling with the protagonist.</summary>
    /// <summary>
    /// Named companions travelling with the protagonist.
    ///
    /// <para>Typed <see cref="PartyMember"/> rather than <c>Companion</c> so a recruited NPC joins as
    /// the body it already is. An <c>NpcEntity</c> wraps an <c>EnemyCombatant</c>, which is a
    /// <see cref="PartyMember"/> too, so recruiting is a list insertion rather than a copy — and a
    /// copy is what would go stale: organs, skills, wounds and inventory all live on that body, and a
    /// duplicate of it would silently stop being the person you recruited.</para>
    /// </summary>
    public List<PartyMember> CompanionParty { get; set; } = new();

    /// <summary>Everyone who travels together: the protagonist first, then the companions.</summary>
    public IEnumerable<PartyMember> EveryMember =>
        new PartyMember[] { this }.Concat(CompanionParty);

    /// <summary>
    /// Members carrying more than they can bear. The party travels together or not at all, so a
    /// single overloaded companion grounds everyone until the player puts something down —
    /// acquisition itself is never blocked (see <see cref="PartyMember.IsOverloaded"/>).
    /// </summary>
    public IReadOnlyList<PartyMember> OverloadedMembers =>
        EveryMember.Where(m => m.IsOverloaded).ToList();

    /// <summary>
    /// The reason travel is blocked, or null when the party can set out.
    ///
    /// Kept under ~36 characters because it has to fit inside the travel panel, which is 40 cells
    /// wide — a longer sentence gets truncated exactly where the actionable half would be. Hence
    /// the given name only, and "drop N wt" rather than a fuller phrasing.
    /// </summary>
    public string? TravelWeightBlocker
    {
        get
        {
            var overloaded = OverloadedMembers;
            if (overloaded.Count == 0) return null;

            int excess = overloaded.Sum(m => m.ExcessWeight);

            if (overloaded.Count == 1)
            {
                string name = GivenName(overloaded[0].DisplayName);
                return $"{name} is overloaded — drop {excess} wt";
            }

            return $"{overloaded.Count} members overloaded — drop {excess} wt";
        }
    }

    /// <summary>First word of a display name ("Filyppo the Stout" → "Filyppo"), to save panel width.</summary>
    private static string GivenName(string displayName)
    {
        int space = displayName.IndexOf(' ');
        return space > 0 ? displayName[..space] : displayName;
    }

    /// <summary>
    /// Global party-wide state not tied to a single member — currently the shared coin wallet
    /// used by the buy/sell economy. Coins are global (not per-member) and weightless.
    /// </summary>
    public Party Party { get; } = new();

    /// <summary>Current location on the world sphere (used as RNG seed).</summary>
    public int CurrentLocationId { get; set; }

    /// <summary>
    /// The protagonist's childhood biography, populated during the childhood reminescence
    /// phase as fragments are remembered. Empty at run start.
    /// </summary>
    public ChildhoodHistory ChildhoodHistory { get; } = new();

    /// <summary>
    /// The locations remembered, oldest first: each one's map and the routines learned there. As many
    /// as <see cref="GetMentalMapSlots"/>; see <see cref="EnterMentalMap"/> for how one comes and goes.
    /// </summary>
    public List<MentalMap> MentalMaps { get; set; } = new();

    /// <summary>
    /// The map of the visit in progress when there was no room for it in <see cref="MentalMaps"/> —
    /// every map locked, or no anamnesis to hold one. It serves the visit like any other and is
    /// forgotten when the visit ends (<see cref="LeaveMentalMap"/>). Never saved: a save is written
    /// on the world map, between visits.
    /// </summary>
    private MentalMap? _unheldMap;

    private DerivedStat MentalMapsStat => DerivedStats.First(s => s is MentalMapsStat);

    /// <summary>How many locations can be remembered at once, from the anamnesis.</summary>
    public int GetMentalMapSlots() => MentalMapsStat.GetValue(this);

    /// <summary>The most this body could ever remember — the anamnesis at its full score.</summary>
    public int GetMentalMapSlotsAtBest() => MentalMapsStat.GetValueAtMaxScore(this);

    /// <summary>Every routine remembered, map by map, oldest map first.</summary>
    public IEnumerable<Routine> RecordedRoutines => MentalMaps.SelectMany(m => m.Routines);

    /// <summary>The routines of one kind, across every map remembered.</summary>
    public List<Routine> RoutinesOf(RoutineCategory category)
        => RecordedRoutines.Where(r => r.Category == category).ToList();

    /// <summary>
    /// The map of <paramref name="locationId"/>: a remembered one, or the one the visit in progress
    /// holds without room for it. Null when the place is not remembered.
    /// </summary>
    public MentalMap? MentalMapOf(int locationId)
        => MentalMaps.FirstOrDefault(m => m.LocationId == locationId)
           ?? (_unheldMap?.LocationId == locationId ? _unheldMap : null);

    /// <summary>
    /// A visit to <paramref name="locationId"/> begins: its map becomes the newest. A place not
    /// remembered gets a fresh map, and when every slot is taken the oldest UNLOCKED map is forgotten
    /// to make room — its areas unknown again, its routines gone. With no room at all (every map
    /// locked, or no slot), the visit keeps a map of its own until it ends. Also lets go of maps over
    /// the count, oldest unlocked first, should the anamnesis have shrunk since.
    /// </summary>
    public MentalMap EnterMentalMap(int locationId, string locationName)
    {
        _unheldMap = null;
        var map = MentalMaps.FirstOrDefault(m => m.LocationId == locationId);
        if (map != null) MentalMaps.Remove(map);
        else map = new MentalMap { LocationId = locationId, LocationName = locationName };
        map.LastVisitedDay = GameClock.Days;
        if (locationName.Length > 0) map.LocationName = locationName;

        int slots = GetMentalMapSlots();
        while (MentalMaps.Count >= slots && MentalMaps.FirstOrDefault(m => !m.Locked) is { } oldest)
        {
            MentalMaps.Remove(oldest);
            Console.WriteLine($"Protagonist: forgot the map of {oldest.LocationName} ({oldest.Routines.Count} routines)");
        }

        if (MentalMaps.Count < slots) MentalMaps.Add(map);
        else
        {
            _unheldMap = map;
            Console.WriteLine($"Protagonist: no room to remember {locationName} — held for this visit only");
        }
        return map;
    }

    /// <summary>The visit has ended: a map held without room for it is forgotten.</summary>
    public void LeaveMentalMap() => _unheldMap = null;

    /// <summary>
    /// Learns a routine into the map of its location, if that place is remembered (or being visited).
    /// Returns whether it was new.
    /// </summary>
    public bool RecordRoutine(Routine routine)
        => MentalMapOf(routine.LocationId)?.Learn(routine) ?? false;

    // ── PartyMember abstract ─────────────────────────────────────

    /// <summary>
    /// The protagonist's generated proper name, rolled at run start and re-rollable in the creation
    /// screen (see <see cref="RegenerateName"/>). Null only until the constructor's first roll.
    /// </summary>
    public string? CharacterName { get; private set; }

    /// <summary>
    /// Puts a saved name back. The name is drawn from a run-long RNG stream whose position depends on
    /// everything drawn before it, so it cannot be reproduced from the seed alone — it has to travel
    /// in the save.
    /// </summary>
    public void RestoreCharacterName(string? name) => CharacterName = name;

    /// <summary>The generated name once set, falling back to "Protagonist" before the first roll.</summary>
    public override string DisplayName => CharacterName ?? "Protagonist";

    /// <summary>
    /// Affinity key stays the fixed "Protagonist" constant regardless of the generated
    /// <see cref="CharacterName"/>, so changing the display name never re-keys NPC affinity.
    /// </summary>
    /// <summary>
    /// The protagonist's affinity key, as a constant. Needed by name outside an instance — the
    /// debug affinity seeding runs when a location's store is created, before any protagonist is in
    /// scope — and a second literal "Protagonist" elsewhere would be a silent mismatch.
    /// </summary>
    public const string AffinityKeyConstant = "Protagonist";

    public override string AffinityKey => AffinityKeyConstant;

    /// <summary>
    /// Rolls a fresh gendered name from the procedural generator, matching the current gender
    /// (from the "gender" derived stat, driven by the genitories organ score). Uses a fresh
    /// <see cref="System.Random"/> so each reroll differs — the protagonist name is player-chosen,
    /// so it need not be reproducible under <c>--seed</c>.
    /// </summary>
    public void RegenerateName()
    {
        bool male = DerivedStats.First(s => s.Name == "gender").GetValue(this) > 0;
        // A stream, not a per-call generator: this rerolls whenever gender changes, and a fresh
        // generator seeded the same way would hand back the same name every time.
        CharacterName = Cathedral.Game.Npc.Naming.NameGenerator.GenerateHuman(male, GameRng.Stream("protagonist-name"));
    }

    // ── Starting age ─────────────────────────────────────────────

    /// <summary>Nominal age the protagonist begins a run at: 14 years.</summary>
    public const int StartAgeDays = 14 * LifetimeStat.DaysPerYear;   // 5040

    /// <summary>
    /// How far either side of <see cref="StartAgeDays"/> the protagonist's true starting age may
    /// fall: ±1 year. The heart only yields five distinct lifetimes, so without this every
    /// protagonist sharing a heart score would die on exactly the same day. Jittering the birth
    /// time rather than the lifetime keeps the Lifetime stat honest — the number shown on the body
    /// panel is the literal truth — while still making no two death dates alike.
    /// </summary>
    public const int StartAgeJitterDays = 1 * LifetimeStat.DaysPerYear;   // 360

    // ── Constructor ──────────────────────────────────────────────
    public Protagonist() : base(SpeciesRegistry.Human)
    {
        // No test equipment, no starter modus mentis: the protagonist starts the run with
        // only the ChildhoodReminescence MM (granted explicitly when entering the reminescence
        // phase) and an empty inventory. Items and skills are acquired via REMEMBER actions.

        // Born roughly fourteen years before day zero, give or take a year.
        var rng = GameRng.For("protagonist_birth");
        SetAgeAtCreation(StartAgeDays + rng.Next(-StartAgeJitterDays, StartAgeJitterDays + 1));

        // Roll an initial name. Organ scores were just randomised to 1 (genitories = 1 → male), so
        // this starts as a male name matching the default body; it rerolls when gender changes.
        RegenerateName();
    }

}
