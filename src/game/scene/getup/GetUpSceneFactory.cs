using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.GetUp;

/// <summary>
/// Builds the Get-Up scene: the <see cref="RestingPlace"/>, a single area with a lone tree on a
/// plain where the exhausted protagonist rests. What stands around the body may be looked at, and
/// the body itself (<see cref="SelfPointOfInterest"/>) holds three <see cref="GetUpPointOfInterest"/>
/// objects, the facets of its exhaustion (leg pain, fatigue, discouragement). The only available verb is <see cref="GetUpVerb"/>, on any of them:
/// whatever is thought about, the thought ends on rising. The scene's <see cref="Scene.Phase"/> is
/// set to <see cref="NarrationPhase.GetUp"/>.
/// </summary>
public sealed class GetUpSceneFactory : SceneFactory
{
    public GetUpSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        var area = new TreeArea(
            displayName:           RestingPlace.AreaName,
            contextDescription:    "resting at the foot of a lone tree on an open plain",
            transitionDescription: "sink back down under the tree",
            descriptions:          new List<string>(RestingPlace.Descriptions),
            moods:                 RestingPlace.Moods);

        area.PointsOfInterest.Add(new SelfPointOfInterest("Myself, spent, with my back against the tree."));
        foreach (var scenery in RestingPlace.Scenery())
            area.PointsOfInterest.Add(scenery);

        // Descriptions are authored in the first person as noun phrases: they fill the neutral
        // narration templates ("I notice X.", "This is X.") directly, so they must read as things
        // the protagonist notices about their own body — not as second-person sentences.
        area.PointsOfInterest.Add(new GetUpPointOfInterest(
            "aching legs",
            "a deep, grinding soreness in my legs and feet, where every step of the road " +
            "has gathered and pressed into the bone"));

        area.PointsOfInterest.Add(new GetUpPointOfInterest(
            "bone-deep exhaustion",
            "a heaviness settled through my whole body — not merely tired but spent, " +
            "as if the road has drained something more than muscle"));

        area.PointsOfInterest.Add(new GetUpPointOfInterest(
            "discouraged spirit",
            "a quiet gone through me, not broken but close to it — the distance ahead " +
            "unreasonable, the effort of rising almost pointless"));

        var section = new Section(RestingPlace.SectionName, new List<string> { "a lone tree on an open plain" },
            seed => new NoisyGenerator { Seed = seed, Density = 0.85f });
        section.Areas.Add(area);
        scene.Sections.Add(section);
        RegisterAll(scene, section);

        scene.Phase = NarrationPhase.GetUp;

        Console.WriteLine("GetUpSceneFactory: built Get-Up scene");
    }

    protected override void AssignVerbs(Scene scene)
    {
        scene.Verbs.Add(new GetUpVerb());
    }
}
