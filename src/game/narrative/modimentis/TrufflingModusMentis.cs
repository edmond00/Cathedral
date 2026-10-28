using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Truffling — the will to find and take desert truffles from under their cracked mounds. Learned on
/// truffle ground.
/// </summary>
public class TrufflingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "truffling";
    public override string DisplayName      => "Truffling";
    public override string MenuDescription =>
        "Wants truffles: walks the sand after rain looking for the little cracked mounds, kneels, and works the pale knobs out of the ground with the fingers. A season of truffles feeds a family, and it means to have every one before the jackals do.";
    public override string SkillMeans       => "working truffles out of the sand by hand";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTarget? Will => WillTarget.ToAny("take truffles from the sand", typeof(GatherVerb), typeof(GrabVerb))
        .Yielding<World.Items.DesertTruffle>();
    public override string[] Organs        => new[] { "hands", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a forager on the knees in the sand, fingers working round a pale knob";
    public override string PersonaReminder  => "truffle-hunter";
    public override string PersonaReminder2 => "someone who reads a crack in the sand as a meal underneath";
    public override string StyleInstruction =>
        "Keep the line close to the ground - cracks in the sand, the pale thing under them, the earthy smell when it comes out.";

    public override string PersonaPrompt => @"You are the inner voice of TRUFFLING, and it rained a week ago, and the sand is cracking.

Each little mound with a split in its top has something swelling under it - pale, knobbly, smelling of earth and nuts when it comes out of the ground. You walk bent, eyes down, and kneel at every crack, and work the fingers in round it gently so as not to break it. The jackals know the season too; the trick is to be out before them.

You talk in pleased mutters: 'there - see the crack?', 'gently, gently, don't break it,' 'that's supper, and tomorrow's.'";
}
