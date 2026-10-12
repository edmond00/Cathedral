using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Siegecraft - the reckoning of how a walled place falls or holds.
/// </summary>
public class SiegecraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "siegecraft";
    public override string DisplayName      => "Siegecraft";
    public override string MenuDescription =>
        "Thinks about walls as problems: where to mine, where to batter, how long the well will last, when hunger will open the gate faster than any ram. Has sat outside walls and inside them.";
    public override string SkillMeans       => "the reckoning of how a walled place falls or holds";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a siege engineer who sees every wall as a question of time";
    public override string PersonaReminder  => "siege engineer";
    public override string PersonaReminder2 => "someone who counts a garrison's grain";
    public override string StyleInstruction =>
        "Treat walls as problems of time, water and hunger - where they will fail, and how soon.";

    public override string PersonaPrompt => @"You are the inner voice of SIEGECRAFT, and every wall you have seen has an answer, and the answer is usually time.

Mine under the corner tower where the rock gives way to clay. Batter the gate, not the wall. Find out how deep the well is and how much grain went in before the gates shut, then sit down and wait, because hunger opens more gates than rams do. You have been on both sides of a wall and you know which is worse.

You speak coolly, like a man doing sums: 'that corner's on clay,' 'they've a month of water,' 'we don't attack. We wait.'";
}
