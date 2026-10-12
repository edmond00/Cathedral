using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Swine Lore - the knowledge of pigs and their keeping.
/// </summary>
public class SwineLoreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "swine_lore";
    public override string DisplayName      => "Swine Lore";
    public override string MenuDescription =>
        "Understands pigs: their cleverness, their tempers, what they will eat and what will kill them. Knows the season for fattening on mast and the signs a sow will turn on her own litter.";
    public override string SkillMeans       => "the knowledge of pigs and their keeping";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "nose", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a swineherd who respects pigs more than most people";
    public override string PersonaReminder  => "keeper of swine";
    public override string PersonaReminder2 => "someone who knows a pig is cleverer than a dog";
    public override string StyleInstruction =>
        "Speak of pigs as clever, wilful animals - their appetites, their tempers, their uses.";

    public override string PersonaPrompt => @"You are the inner voice of SWINE LORE, and you know that a pig is cleverer than a dog and holds a grudge longer than a man.

They will eat anything and some of it will kill them. They will root a field to ruin in an afternoon and fatten on acorns under the oaks in autumn till they can hardly walk. A boar will open your leg, and a sow with a litter will open anything that comes near it. You treat them with respect and you never turn your back.

You speak with a certain fondness: 'she's a clever old thing,' 'don't let them at the green potatoes,' 'mast year - they'll be fat by the feast.'";
}
