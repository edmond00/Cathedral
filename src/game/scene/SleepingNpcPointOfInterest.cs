using System.Collections.Generic;
using Cathedral.Game.Narrative;

namespace Cathedral.Game.Scene;

/// <summary>
/// A person asleep in their own bed, as one thing rather than two.
///
/// <para>At night in somebody's own room there were previously two separate observations — the
/// person, and the pallet — and neither said what was actually true, which is that the one is in the
/// other. Worse, the person's observation carried the ordinary daytime conversations and the bed's
/// carried the ordinary "take what is in it", so the scene read as though nobody were asleep at
/// all.</para>
///
/// <para>Placement swaps both out for this while the sleep lasts (see <c>SceneNpcPlacement</c>), and
/// swaps them back the moment it does not — a different hour, a death, or somebody being woken. The
/// verbs that live here are the ones that only make sense against a sleeper: murder, waking, and
/// going through their pockets. The bed's own verbs go with the bed: you cannot search a bed
/// somebody is lying in.</para>
///
/// <para>It is also worth a sense or two. Every other object in the game rewards looking closely,
/// and a person asleep in front of you is the last thing that should be inert.</para>
/// </summary>
public class SleepingNpcPointOfInterest : PointOfInterest
{
    /// <summary>The sleeper. What the sleep-specific verbs act on.</summary>
    public SceneNpc Sleeper { get; }

    /// <summary>The bed they are in, kept so it can be put back when they get out of it.</summary>
    public PointOfInterest Bed { get; }

    public SleepingNpcPointOfInterest(SceneNpc sleeper, PointOfInterest bed)
        : base($"{sleeper.Entity.DisplayName}, asleep",
               "sleeper",
               new List<string> { Describe(sleeper, bed) },
               items: null,
               moods: new[] { "still", "breathing slow", "unguarded", "turned to the wall" })
    {
        Sleeper = sleeper;
        Bed     = bed;

        // Keyed off the sleeper rather than assigned by the section walk, because this object is
        // created and destroyed as the hours turn and never sits in the list long enough to be
        // walked. Without it the description would re-roll every time they lay down.
        StableKey = $"sleeper|{sleeper.Entity.NpcId}";

        Senses = new SensoryProfile(Examine: true, Listen: true, Smell: true);
        VerbModiMentis = new Dictionary<string, string>
        {
            ["examine"] = "scrutiny",
            ["listen"]  = "keen_ear",
            ["smell"]   = "scenting",
        };
    }

    private static string Describe(SceneNpc sleeper, PointOfInterest bed)
        => $"{sleeper.Entity.DisplayName} is asleep on the {bed.DisplayName.ToLowerInvariant()}, "
         + "breathing slowly, turned away from the door";
}
