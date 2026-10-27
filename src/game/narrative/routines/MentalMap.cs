using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// What the protagonist remembers of one location: its map — the areas stood in, the ways gone by,
/// what an observation took in (<see cref="Scene.AreaVisits"/>) — and the routines learned there.
///
/// <para><b>Remembering is the protagonist's, not the location's.</b> The four visit stores used to
/// live on <c>LocationInstanceState</c>, which never forgets; held here they go with the map when it
/// is forgotten, and a place visited long ago is unknown again. A visit hands these sets to the scene
/// by reference (<see cref="AttachTo"/>), so what the visit records lands here with no save step, as
/// the location's own stores do.</para>
///
/// <para>Held in a queue on the protagonist (<see cref="Protagonist.MentalMaps"/>), as many as the
/// anamnesis allows (<see cref="MentalMapsStat"/>). Visiting a location makes its map the newest;
/// visiting one not held forgets the oldest unlocked map to make room. A <see cref="Locked"/> map is
/// never forgotten to make room.</para>
/// </summary>
public sealed class MentalMap
{
    /// <summary>World vertex (location id) this map is of.</summary>
    public int LocationId { get; set; }

    /// <summary>The location's name as the player knew it, for the menus.</summary>
    public string LocationName { get; set; } = "";

    /// <summary>When true, never forgotten to make room for another.</summary>
    public bool Locked { get; set; }

    /// <summary>The game day of the last visit — what makes a map the newest.</summary>
    public double LastVisitedDay { get; set; }

    /// <summary>Areas stood in, by <c>Area.StableKey</c>. See <see cref="Scene.Scene.VisitedAreas"/>.</summary>
    public HashSet<string> VisitedAreas { get; set; } = new();

    /// <summary>Ways gone by. See <see cref="Scene.Scene.TakenLinks"/>.</summary>
    public HashSet<string> TakenLinks { get; set; } = new();

    /// <summary>Areas an observation took in. See <see cref="Scene.Scene.SeenAreas"/>.</summary>
    public HashSet<string> SeenAreas { get; set; } = new();

    /// <summary>Ways an observation took in. See <see cref="Scene.Scene.SeenLinks"/>.</summary>
    public HashSet<string> SeenLinks { get; set; } = new();

    /// <summary>The routines learned here, oldest first. No cap: the map is what is limited.</summary>
    public List<Routine> Routines { get; set; } = new();

    /// <summary>
    /// Hands this map's stores to a scene built for its location, by reference: what the visit
    /// records is remembered here.
    /// </summary>
    public void AttachTo(Scene.Scene scene)
    {
        scene.VisitedAreas = VisitedAreas;
        scene.TakenLinks   = TakenLinks;
        scene.SeenAreas    = SeenAreas;
        scene.SeenLinks    = SeenLinks;
    }

    /// <summary>
    /// Learns a routine, unless an identical one is already held (same kind, place, hour and target).
    /// Learned again, the one held keeps its place but takes the new name and, for a trade, the
    /// catalogue — so one recorded wrong by an older build mends itself. Returns whether it was new.
    /// </summary>
    public bool Learn(Routine routine)
    {
        if (Routines.FirstOrDefault(r => r.Signature == routine.Signature) is { } known)
        {
            known.Name = routine.Name;
            if (known is TradeRoutine knownTrade && routine is TradeRoutine trade)
                knownTrade.Catalogue = trade.Catalogue;
            return false;
        }
        Routines.Add(routine);
        return true;
    }

    /// <summary>One assertable line for <c>inspect mental-maps</c>.</summary>
    [JsonIgnore]
    public string CliLine
        => $"mental-map location={LocationId} name=\"{LocationName}\" locked={(Locked ? "yes" : "no")} "
         + $"routines={Routines.Count} visited={VisitedAreas.Count} seen={SeenAreas.Count}";
}
