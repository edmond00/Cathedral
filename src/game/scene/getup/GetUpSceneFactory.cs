using System;
using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Fight.Generators;

namespace Cathedral.Game.Scene.GetUp;

/// <summary>
/// Builds the Get-Up scene: a single area with a lone tree on a plain where the exhausted
/// protagonist rests. Three <see cref="GetUpPointOfInterest"/> objects represent different
/// facets of exhaustion (leg pain, fatigue, discouragement). The only available verb is
/// <see cref="GetUpVerb"/>. The scene's <see cref="Scene.Phase"/> is set to
/// <see cref="NarrationPhase.GetUp"/>.
/// </summary>
public sealed class GetUpSceneFactory : SceneFactory
{
    public GetUpSceneFactory(string? sessionPath = null) : base(sessionPath) { }

    protected override void BuildSections(Random rng, int locationId, Scene scene)
    {
        var area = new Area(
            displayName:           "Under the Tree",
            referenceLemma:        "tree",
            contextDescription:    "resting at the foot of a lone tree on an open plain",
            transitionDescription: "sink back down under the tree",
            descriptions: new List<string>
            {
                "A lone tree on a flat, grey plain. You sit with your back against the bark, " +
                "the road stretching ahead of you in both directions, your body unwilling to move."
            },
            moods: new[] { "still", "heavy", "silent", "desolate", "open", "vast", "grey" });

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

        var section = new Section("The Plain", new List<string> { "a lone tree on an open plain" },
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
