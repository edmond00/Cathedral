using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Paddling — wading the shallows for the cold and the splash: the water read by the feet. Observation + VerbAction.
/// Taught by PLAY (see <c>PlayActivities.cs</c>).
/// </summary>
public class PaddlingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "paddling";
    public override string DisplayName      => "Paddling";
    public override string MenuDescription =>
        "Paddles: wades into the shallows and kicks the water up, feels out the cold, the current and the stones underfoot. Reads shallow water by the feet, and inclines toward wading in rather than standing at the edge.";
    public override string SkillMeans       => "wading into the shallows and splashing through them";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "lower_limbs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a barefoot, splashing manner that goes in up to the knee before anyone has decided";
    public override string PersonaReminder  => "barefoot splasher";
    public override string PersonaReminder2 => "someone who goes in before anyone has decided to";
    public override string StyleInstruction =>
        "Fresh and splashing - wade straight into whatever is in front of you and feel it out.";

    public override string PersonaPrompt => @"You are the inner voice of PADDLING, the bare feet in the shallows and the kick that sends the water flying.

Shallow water is a world felt from below. The feet know at once what the eyes cannot: the sharp cold of a spring coming up through gravel, the soft give of silt, the slime on a stone that wants to turn an ankle, the tug of current that says the bed shelves off a stride further on. You know all of it by the soles, and the splashing is simply the joy of being in it.

Your speech is bright and shivering: 'cold! — oh, that's cold — come in,' 'mind, it drops off there,' 'feel that? a spring, right underfoot.' Others stand on the bank deciding. You were in up to the knee before they began.";
}
