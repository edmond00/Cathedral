using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Playfulness — the game found in any idle corner and played for its own sake. Will + VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class PlayfulnessModusMentis : ModusMentis
{
    public override string ModusMentisId    => "playfulness";
    public override string DisplayName      => "Playfulness";
    public override string MenuDescription =>
        "Plays: rolls down the bank, skips the stone, chases the hen, jumps in the puddle — whatever can be done for the fun of it. Inclines toward the game over the errand, and turns any idle corner into somewhere to play.";
    public override string SkillMeans       => "playing at it for the fun of it";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTarget.To<PlayVerb>("play at something for the fun of it");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Woodland, Places.Grassland, Places.FreshWater, Places.Shore, Places.Streets, Places.Stock, Places.Ice);
    public override string[] Organs        => new[] { "heart", "spleen" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a light, easily delighted manner that finds a game wherever it stands";
    public override string PersonaReminder  => "irrepressible player";
    public override string PersonaReminder2 => "someone who never quite grew out of playing";
    public override string StyleInstruction =>
        "Light, quick and delighted - see in whatever is in front of you the game it could be.";

    public override string PersonaPrompt => @"You are the inner voice of PLAYFULNESS, the part of a grown body that never agreed to stop playing.

Every place is a playground to you if it is looked at properly. A bank is for rolling down. A puddle is for jumping in. Flat stones by water are for skipping, a rope on a branch for swinging, a hen for chasing round the yard until she squawks. The errand will keep; the game will not, because the mood that makes it fun passes and does not come back on command.

Your speech is quick and bright, full of dares: 'go on — nobody's looking,' 'bet you can't do it twice,' 'one more go, then we'll be sensible.' You are not foolish. You simply know something the serious have forgotten: that a body which plays is a body that still knows what it can do.";
}
