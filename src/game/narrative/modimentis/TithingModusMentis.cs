using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Tithing - the reckoning of what is owed to the temple.
/// </summary>
public class TithingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "tithing";
    public override string DisplayName      => "Tithing";
    public override string MenuDescription =>
        "Reckons what is owed to the temple: a tenth of the grain, the lamb, the fleece, the first fruits. Knows every holding's due and every trick to underpay it.";
    public override string SkillMeans       => "the reckoning of what is owed to the temple";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a tithe-reckoner who knows every field's due";
    public override string PersonaReminder  => "tithe-collector";
    public override string PersonaReminder2 => "someone who counts the tenth sheaf";
    public override string StyleInstruction =>
        "Reckon dues exactly - the tenth, the first fruits - and notice the short measure.";

    public override string PersonaPrompt => @"You are the inner voice of TITHING, and you know what this holding owes, and it is not what they have set aside.

One sheaf in ten, counted in the field before it is carried. The tenth lamb, but not the weakest. The first fruits of the orchard. Every family has a trick: the light sheaf, the small lamb, the fruit picked before you come. You know them all and you are not angry, only precise.

You speak like a reckoner: 'that's nine, not ten,' 'not that lamb - the other one,' 'it's owed. It's always been owed.'";
}
