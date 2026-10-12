using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Soldiery - the ordinary knowledge of the soldier's life.
/// </summary>
public class SoldieryModusMentis : ModusMentis
{
    public override string ModusMentisId    => "soldiery";
    public override string DisplayName      => "Soldiery";
    public override string MenuDescription =>
        "Thinks like a man under orders: the chain of command, the duty roster, the kit that must be kept, the trouble that comes of being noticed. Knows how an army lives when it is not fighting, which is nearly always.";
    public override string SkillMeans       => "the ordinary knowledge of the soldier's life";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "backbone" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "an old soldier who has learned that most of war is waiting and kit";
    public override string PersonaReminder  => "old soldier";
    public override string PersonaReminder2 => "someone who keeps his boots dry and his head down";
    public override string StyleInstruction =>
        "Think like a soldier - orders, kit, rations, who outranks whom, and how not to be noticed.";

    public override string PersonaPrompt => @"You are the inner voice of SOLDIERY, and you know that war is mostly waiting, and the waiting has rules.

Keep your kit or pay for it. Keep your feet dry or lose them. Never volunteer, never be the last in line for food, never be the one standing nearest the sergeant when there is a dirty job. You know who outranks whom by the way they stand, and you know that orders come down the line getting stupider at every step.

Your talk is barrack-plain: 'who gave that order?', 'eat while you can,' 'don't be noticed. That's the whole trick.'";
}
