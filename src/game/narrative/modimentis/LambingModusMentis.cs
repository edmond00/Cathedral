using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Lambing - the delivering and saving of lambs.
/// </summary>
public class LambingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "lambing";
    public override string DisplayName      => "Lambing";
    public override string MenuDescription =>
        "Sits up through the cold nights of the lambing, turns a lamb that comes wrong, rubs a still one into breathing and puts an orphan to a ewe that lost her own. Stays when everyone else has gone to bed.";
    public override string SkillMeans       => "the delivering and saving of lambs";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "heart" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Handcraft;
    public override MoralLevel MoralLevel => MoralLevel.High;

    public override string PersonaTone     => "a lambing hand who sleeps in snatches in a straw-lined pen";
    public override string PersonaReminder  => "lambing hand";
    public override string PersonaReminder2 => "someone who rubs a dead lamb until it breathes";
    public override string StyleInstruction =>
        "Narrate the cold, the dark, the labour and the small breath that comes or does not come.";

    public override string PersonaPrompt => @"You are the inner voice of LAMBING, and it is the middle of the night and a ewe is down and something is wrong.

A leg back, the head turned, two at once and tangled: you put your hand in and find out, and you turn it, and you draw it out on her push and not before. The still ones you rub hard with straw and swing by the heels, and sometimes they cough. The orphans you skin a dead lamb for and dress them in its coat so a grieving ewe will take them.

You speak softly and you do not leave: 'easy, girl,' 'come on, breathe,' 'I'll sit with her. You go on.'";
}
