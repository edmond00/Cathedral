using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Reminescence;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Scene.Reminescence;

/// <summary>
/// Builds a single-area Scene representing one childhood reminescence.
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
        // The reminescence "place" is not a real location — the area display name is shown
        // to the player as "Reminescence" and POIs are read as fragments.
        var area = new Area(
            displayName:           "Reminescence",
            contextDescription:    "remembering fragments of your childhood",
            transitionDescription: "drift back into the memory",
            descriptions:          new List<string>(_data.ContentLines),
            moods:                 new[] { "fuzzy", "fading", "half-remembered", "shimmering", "veiled", "hazy" });

        // Wire the fragments as PoIs.
        foreach (var fragment in _data.Fragments)
            area.PointsOfInterest.Add(new FragmentPointOfInterest(fragment));

        var section = new Section("Reminescence", new List<string> { "the threshold of memory" });
        section.Areas.Add(area);
        scene.Sections.Add(section);
        RegisterAll(scene, section);

        scene.Phase                 = NarrationPhase.ChildhoodReminescence;
        scene.CurrentReminescenceId = _data.Id;

        Console.WriteLine($"ReminescenceSceneFactory: built scene for reminescence '{_data.Id}' with {_data.Fragments.Count} fragment(s)");
    }

    /// <summary>
    /// Only the REMEMBER verb is enabled in a reminescence scene.
    /// </summary>
    protected override void AssignVerbs(Scene scene)
    {
        scene.Verbs.Add(new RememberVerb());
    }
}
