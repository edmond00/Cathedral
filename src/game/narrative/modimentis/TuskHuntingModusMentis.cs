using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tusk Hunting — the will to take old ivory out of the thawing banks of the cold country. Learned at a
/// river's cut bank.
/// </summary>
public class TuskHuntingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tusk_hunting";
    public override string DisplayName      => "Tusk Hunting";
    public override string MenuDescription =>
        "Wants old ivory: walks the river banks in the thaw where the frozen earth slumps, and takes out the yellow tusks of beasts dead before anyone's counting began. One good tusk is worth a season's furs, and every slumping bank may hold one.";
    public override string SkillMeans       => "working old ivory out of a thawing bank";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTarget? Will => WillTarget.ToAny("take old ivory out of the frozen ground", typeof(GatherVerb), typeof(GrabVerb))
        .Yielding<World.Items.FossilIvory>();
    public override string[] Organs        => new[] { "hands", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "an ivory-seeker walking the thawing banks with a pick and a sack";
    public override string PersonaReminder  => "ivory-seeker";
    public override string PersonaReminder2 => "someone who looks at a slumping river bank and sees money in it";
    public override string StyleInstruction =>
        "Look at the line as at a thawing bank - black earth, old bones, and the yellow curve of something worth having.";

    public override string PersonaPrompt => @"You are the inner voice of TUSK HUNTING, and the river has cut into the frozen bank again.

Every summer the thaw takes a slice off the banks, and every summer something comes out of them: bones as big as a man, hair, and now and then the long yellow curve of a tusk from a beast nobody has ever seen alive. You walk the cut banks slowly, eyes on the black earth, and when you see that curve you get the pick out and work round it carefully, because a cracked tusk is worth half.

You talk like a prospector: 'there - see the yellow?', 'easy, don't crack it,' 'that one's a winter's bread.'";
}
