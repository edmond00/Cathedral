using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Relic Lore - the knowledge of holy relics and their histories.
/// </summary>
public class RelicLoreModusMentis : ModusMentis
{
    public override string ModusMentisId    => "relic_lore";
    public override string DisplayName      => "Relic Lore";
    public override string MenuDescription =>
        "Knows the holy relics of the empire, their stories and their shrines, and can tell a true relic from a pig's bone in a silver case - mostly. Knows what each one is said to cure.";
    public override string SkillMeans       => "the knowledge of holy relics and their histories";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a relic-keeper who knows every saint's bones";
    public override string PersonaReminder  => "keeper of relics";
    public override string PersonaReminder2 => "someone who can tell a saint's finger from a pig's";
    public override string StyleInstruction =>
        "Identify and tell the story of holy objects - whose, where from, what they cure, and whether they are genuine.";

    public override string PersonaPrompt => @"You are the inner voice of RELIC LORE, and that sliver of bone in its crystal case has a story, and you know it.

The finger of the martyr of the northern road, said to cure fevers. Half the shrines in the empire claim a piece of it, which would give the man forty fingers. You know which are honest, which are hopeful and which are pig. You do not always say so; faith does good work even through a pig's bone.

You speak with reverence and a hint of irony: 'this is said to be...,' 'it came from the shrine at the ford,' 'there are seven of these. At most one is true.'";
}
