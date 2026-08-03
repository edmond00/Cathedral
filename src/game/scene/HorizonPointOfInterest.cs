using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// The view from a high place: what can be seen of the rest of this location from up here.
///
/// <para>Placed automatically in any area that is the top of a climb — a roof, a canopy, a crag, a
/// cliff top, a summit. It is the payoff for climbing, and it is the reason climbing is worth the
/// difficulty: from the ground you can only reach what borders you, and from up here you can see
/// where things <i>are</i>.</para>
///
/// <para>Observing it (<see cref="Verbs.ObserveHorizonVerb"/>) reveals the location's landmark areas,
/// which <see cref="Verbs.GoTowardVerb"/> then walks to directly, bypassing the area graph. That is
/// the whole loop: climb, look, know where to go, go.</para>
///
/// <para>Contextual, so the prose names the actual landmarks and changes with the light — the same
/// seam doors use to read differently from either side.</para>
/// </summary>
public class HorizonPointOfInterest : PointOfInterest, IContextualDescription
{
    private readonly IReadOnlyList<Area> _landmarks;

    public HorizonPointOfInterest(IReadOnlyList<Area> landmarks, string displayName, List<string> descriptions)
        : base(displayName, "horizon", descriptions,
               items: null,
               moods: new[] { "wide", "hazed", "far-off", "open" },
               isNatural: true)
    {
        _landmarks = landmarks;

        // Worth examining (what is out there, and how far) and contemplating (it is a view). Not
        // worth smelling, and the wind is the only thing to hear.
        Senses = SensoryProfile.Beautiful;
        VerbModiMentis = new Dictionary<string, string>
        {
            ["examine"]         = "topographia",
            ["contemplate"]     = "aesthetic",
            ["observe_horizon"] = "topographia",
        };
    }

    /// <summary>The landmark areas this view takes in. What an observation reveals and a walk targets.</summary>
    public IReadOnlyList<Area> Landmarks => _landmarks;

    public string DescribeFrom(Area viewingArea, TimePeriod when)
    {
        // Name what can actually be seen. A view that lists the place you are standing in reads as a
        // mistake, so the current area drops out.
        var named = _landmarks
            .Where(a => a.Id != viewingArea.Id)
            .Select(a => a.DisplayName.ToLowerInvariant())
            .ToList();

        string light = when switch
        {
            TimePeriod.Dawn      => "grey with the early light",
            TimePeriod.Morning   => "clear in the morning air",
            TimePeriod.Noon      => "flattened by the high sun",
            TimePeriod.Afternoon => "long-shadowed",
            TimePeriod.Evening   => "going gold and then going dim",
            _                    => "black but for what still shows a light",
        };

        if (named.Count == 0)
            return $"a wide view over the country below, {light}";

        string list = named.Count == 1
            ? named[0]
            : string.Join(", ", named.Take(named.Count - 1)) + " and " + named[^1];

        return $"a wide view over the country below, {light}, with {list} laid out in it";
    }
}
