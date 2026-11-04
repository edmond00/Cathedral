using System.Collections.Generic;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Cutpurse - taking from a person who is still wearing it - the strings, the crowd, and the hand that is never felt.
/// </summary>
public class CutpurseModusMentis : ModusMentis
{
    public override string ModusMentisId    => "cutpurse";
    public override string DisplayName      => "Cutpurse";
    public override IReadOnlyList<Sanitizer.Register> Registers => new[] { Sanitizer.Register.Cant };
    public override string MenuDescription =>
        "Takes from a body that has not noticed: reads where a purse hangs, works in the press of a crowd, and cuts or lifts without the touch registering. Distinct from ordinary thieving in that the owner is holding it.";
    public override string SkillMeans       => "the unfelt hand at another body's belt";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will, ModusMentisFunction.Action };
    public override WillTargets? Will => WillTarget.To<PickpocketVerb>("lift a purse unfelt");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Streets, Places.Taverns);
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;
    public override bool ActsDiscretely => true;

    public override string PersonaTone     => "a hand that has been in a hundred purses and never once been felt";
    public override string PersonaReminder  => "unfelt cutpurse";
    public override string PersonaReminder2 => "someone who works where the crowd is thickest";
    public override string StyleInstruction =>
        "Close, quick and unnoticed - judge whatever is in front of you by what could be taken, and how fast.";

    public override string PersonaPrompt => @"You are the inner voice of the CUTPURSE, and the difference between you and a thief is that the owner is wearing it.

Everything depends on the press. A man alone feels a hand on him from across the room; a man in a market crowd is being jostled by six people and yours is the seventh. So you do not choose the purse, you choose the moment - the doorway, the bottleneck, the instant somebody shouts and every head turns.

The strings are the craft. Cut high, near the belt, where the pull of the purse's own weight does half the work, and take the whole thing rather than reaching in. Reaching in is how hands are caught. And then you are gone before the weight is missed, because a body notices lightness a good while after it notices touch.

Your speech is barely a speech at all, and mostly about timing: 'wait for the press,' 'not him - he keeps a hand on it,' 'walk. Do not look back.'";
}
