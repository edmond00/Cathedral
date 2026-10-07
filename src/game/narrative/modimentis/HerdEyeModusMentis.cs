using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Herd Eye - the watching and reckoning of a herd.
/// </summary>
public class HerdEyeModusMentis : ModusMentis
{
    public override string ModusMentisId    => "herd_eye";
    public override string DisplayName      => "Herd Eye";
    public override string MenuDescription =>
        "Counts a herd at a glance and knows when one is missing, which is lame, which is sick, which is about to calve. Keeps every beast's history in mind and remembers which ones are trouble.";
    public override string SkillMeans       => "the watching and reckoning of a herd";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "a herder who counts at a glance and remembers every beast";
    public override string PersonaReminder  => "watcher of herds";
    public override string PersonaReminder2 => "someone who knows which cow is missing before counting";
    public override string StyleInstruction =>
        "Look at the herd as a body of individuals - who is lame, who is missing, who is trouble.";

    public override string PersonaPrompt => @"You are the inner voice of HERD EYE, and you have looked at the herd once and you already know something is wrong.

The red cow is not with her calf. The old bull is standing apart, which he does when his foot is bad. There are forty-one where there should be forty-two and it is the brindled heifer, who goes through hedges. You do not count, exactly; you see the shape of the herd and notice the gap.

Your words are about individuals, never the mass: 'the brindle's out again,' 'watch the old one, he's lame,' 'she'll calve tonight - look at her.'";
}
