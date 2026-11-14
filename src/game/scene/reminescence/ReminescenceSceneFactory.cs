using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Reminescence;
using Cathedral.Game.Scene.GetUp;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.Reminescence;

/// <summary>
/// Builds a single-area Scene representing one childhood reminescence, on the ground it is
/// remembered from: the <see cref="RestingPlace"/> the protagonist will get up from, with the same
/// things standing around the body and the protagonist among them (<see cref="SelfPointOfInterest"/>).
/// Each fragment in the reminescence becomes a <see cref="FragmentPointOfInterest"/>
/// in that area; the only verb available is <see cref="RememberVerb"/>.
/// The scene's <see cref="Scene.Phase"/> is set to
/// <see cref="NarrationPhase.ChildhoodReminescence"/> so the rest of the pipeline opts
/// into the reminescence-specific behaviour (no critic, no noetic cost, ○ glyph,
/// auto-success).
/// </summary>
public sealed class ReminescenceSceneFactory : SceneFactory
{
    private readonly ReminescenceData _data;

    public ReminescenceSceneFactory(ReminescenceData data, string? sessionPath = null)
        : base(sessionPath)
    {
        _data = data ?? throw new ArgumentNullException(nameof(data));
    }

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        // Where the remembering is done, which is where the get-up will be: the same name on the
        // card and the same things around it. The area is a memory all the same — what is said of
        // it to the narrator is the memory's own lines.
        var area = new MemoryArea(
            displayName:           RestingPlace.AreaName,
            contextDescription:    "remembering fragments of your childhood",
            transitionDescription: "drift back into the memory",
            descriptions:          new List<string>(_data.ContentLines),
            moods:                 new[] { "fuzzy", "fading", "half-remembered", "shimmering", "veiled", "hazy" });

        // Wire the fragments as PoIs, in an order drawn from the world's seed: the catalogue order
        // put the same fragment first in every run. The `rng` handed in is seeded from the location
        // id alone, so it would shuffle every world the same way.
        area.PointsOfInterest.Add(new SelfPointOfInterest("Myself, and what I was, coming back to me in pieces."));
        foreach (var scenery in RestingPlace.Scenery())
            area.PointsOfInterest.Add(scenery);

        var order = GameRng.For($"reminescence-order:{_data.Id}");
        foreach (var fragment in _data.Fragments.OrderBy(_ => order.Next()))
            area.PointsOfInterest.Add(new FragmentPointOfInterest(fragment));

        var section = new Section(RestingPlace.SectionName, new List<string> { "the threshold of memory" },
            seed => new ArenaGenerator { Seed = seed });
        section.Areas.Add(area);
        scene.Sections.Add(section);
        RegisterAll(scene, section);

        scene.Phase                 = NarrationPhase.ChildhoodReminescence;
        scene.CurrentReminescenceId = _data.Id;

        Console.WriteLine($"ReminescenceSceneFactory: built scene for reminescence '{_data.Id}' with {_data.Fragments.Count} fragment(s): "
            + string.Join(", ", area.PointsOfInterest.OfType<FragmentPointOfInterest>().Select(p => $"'{p.DisplayName}'")));
    }

    /// <summary>
    /// Only the REMEMBER verb is enabled in a reminescence scene.
    /// </summary>
    protected override void AssignVerbs(Scene scene)
    {
        scene.Verbs.Add(new RememberVerb());
    }
}
