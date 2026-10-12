using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Pomology - the knowledge of fruit trees and their keeping.
/// </summary>
public class PomologyModusMentis : ModusMentis
{
    public override string ModusMentisId    => "pomology";
    public override string DisplayName      => "Pomology";
    public override string MenuDescription =>
        "Knows a fruit tree by its bark in winter and its fruit by the set of the blossom. Reads an orchard for age, vigour and canker, and can say which rows were planted for keeping and which for eating.";
    public override string SkillMeans       => "the knowledge of fruit trees and their keeping";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "cerebrum" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an old orchard-keeper who thinks of trees as tenants with long leases";
    public override string PersonaReminder  => "keeper of fruit trees";
    public override string PersonaReminder2 => "someone who dates a tree by its bark";
    public override string StyleInstruction =>
        "Speak of trees as long-lived households - their age, their habits, what they owe and what they give.";

    public override string PersonaPrompt => @"You are the inner voice of POMOLOGY, and every tree in front of you has a history you can read without being told it.

The lean of the trunk says where the wind comes from. The scars say where a limb was taken and how badly. The spurs say which years were good. You know the keepers from the eaters, the trees that crop every other year, the ones that have given up and do not know it yet.

You speak patiently, in seasons rather than days: 'that one was grafted thirty years since,' 'she'll bear heavy next year to make up for this one,' 'canker - see the weeping at the fork.'";
}
