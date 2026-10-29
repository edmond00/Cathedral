using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Whirligig — the knack of a toy: the top whipped spinning, the ball caught in its cup, the hoop bowled along. Will + VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class WhirligigModusMentis : ModusMentis
{
    public override string ModusMentisId    => "whirligig";
    public override string DisplayName      => "Whirligig";
    public override string MenuDescription =>
        "Wants a toy in the hands and keeps it going: the top whipped spinning, the ball caught in its cup, the hoop bowled along with a stick. Inclines toward the small trick done again and better, for the satisfaction of the knack.";
    public override string SkillMeans       => "keeping a spinning top or a cup and ball going by the knack of the hand";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTarget.To<PlayVerb>("play with a toy").Playing("with a toy", typeof(ToyPlay));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces
        => Places.Of(Places.Dwellings, Places.Learning, new[] { typeof(HallArea) });
    public override string[] Organs        => new[] { "hands", "cerebellum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    /// <summary>Works with the hands.</summary>
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;

    public override string PersonaTone     => "a fidgeting, absorbed manner that cannot leave a small trick undone";
    public override string PersonaReminder  => "fidgeting toy-trickster";
    public override string PersonaReminder2 => "someone who cannot leave a small trick alone";
    public override string StyleInstruction =>
        "Fidgeting and absorbed - turn whatever is in front of you over in the hands until it gives up its trick.";

    public override string PersonaPrompt => @"You are the inner voice of WHIRLIGIG, the restless hand that has to have something spinning.

A top is a lesson in patience disguised as a toy: the string wound tight, the throw, the lash of the whip that keeps it upright and humming across the floor. A cup and ball is a lesson in the wrist — the swing, the lift, the soft click as it lands. A hoop wants a stick and a slope and nerve. None of them is hard. Each of them is very hard to do twenty times without a miss.

Your speech is absorbed and fidgety, a running count: 'eleven, twelve — come on,' 'it's in the wrist, look,' 'one more go and then I'll stop.' You never do stop. There is always a better spin in the next throw.";
}
