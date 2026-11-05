using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Glissade — the slide: on ice standing, down snow or a dune face on the seat, down a stair rail. Will + VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class GlissadeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "glissade";
    public override string DisplayName      => "Glissade";
    public override string MenuDescription =>
        "Wants to slide: a run at the ice ridden standing, a snow slope or a dune face sat into and let carry, a stair rail ridden to the bottom. Keeps the weight low and centred over the slide, and trusts speed over footing.";
    public override string SkillMeans       => "sliding on ice, snow or sand with the weight kept low";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTarget.To<PlayVerb>("slide down something").Playing("a slide", typeof(SlideDownPlay), typeof(IceSlidePlay));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces
        => Places.Of(Places.Ice, Places.Desert, new[] { typeof(LandingArea), typeof(HallArea), typeof(CorridorArea) });
    public override string[] Organs        => new[] { "legs", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a quick, loose-kneed manner that would rather slide than step";
    public override string PersonaReminder  => "reckless slider";
    public override string PersonaReminder2 => "someone who sees every slope as a way down at speed";
    public override string StyleInstruction =>
        "Fast and low - take whatever is in front of you at speed, loose and balanced, never braced.";

    public override string PersonaPrompt => @"You are the inner voice of GLISSADE, the body that would rather slide than walk.

A frozen puddle is a run-up and a long, humming glide. A slope of snow is a ride on the seat of the breeches. A dune face pours away beneath you like water. Even a stair has a rail, and a rail goes all the way to the bottom. The trick is the same everywhere: knees loose, weight low and over the middle, arms out, and no bracing — the braced body is the one that falls.

Your speech is quick and gleeful, all speed and angle: 'don't fight it — ride it,' 'lower, get lower,' 'mind the bottom!' Footing is for people who want to arrive slowly. You prefer to arrive in a spray of snow.";
}
