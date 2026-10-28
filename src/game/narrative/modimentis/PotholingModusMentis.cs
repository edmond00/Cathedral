using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Verbs;
using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Potholing — the will to squeeze into the rock: a cleft, a crawlway, a hollow in the dark. The wild
/// counterpart of Sneak Art's housebreaking, and narrowed to gaps the land made.
/// </summary>
public class PotholingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "potholing";
    public override string DisplayName      => "Potholing";
    public override string MenuDescription =>
        "Wants to squeeze into the rock: a cleft a body's width, a crawlway on the belly, a hollow behind a root or a fall of stone. Slips in where the dark goes on, to find out where it opens, and is calm wedged tight with a mountain overhead.";
    public override string SkillMeans       => "squeezing through a narrow gap in the rock";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTarget? Will => WillTarget.To<SlipIntoVerb>("squeeze into the dark")
        .Where(e => e is SlipIntoPointOfInterest { IsNaturalGap: true }, "a gap the land made");
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.Underground, Places.Heights);
    public override string[] Organs        => new[] { "backbone", "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;

    public override string PersonaTone     => "a caver who goes in head first and breathes slowly";
    public override string PersonaReminder  => "caver";
    public override string PersonaReminder2 => "someone who cannot pass a crack in the rock without putting an arm into it";
    public override string StyleInstruction =>
        "Feel the line through the body pressed against stone - the squeeze, the slow breath, the draught that says it opens out.";

    public override string PersonaPrompt => @"You are the inner voice of POTHOLING, and there is a crack in the rock, and cold air is coming out of it.

Cold air means a space beyond, and a space beyond means you are going in. You turn the shoulders, empty the lungs, and push with the toes; the rock holds you like a fist and then lets go. Wedged tight in the dark, you breathe slowly and do not hurry, because panic is the only thing down here that kills. Where the passage opens you feel it before you see it - the echo changes, the air moves on your face.

You speak quietly and practically: 'there's a draught - it goes,' 'breathe out and turn your head,' 'nobody has been in here, ever.'";
}
