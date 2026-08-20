using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Snatch — the closing hand: the wait, the single fast movement, and the grip that shuts without
/// crushing. The human counterpart of <see cref="SoftMouthModusMentis"/>, which needs a muzzle.
/// VerbAction-only.
/// </summary>
public class SnatchModusMentis : ModusMentis
{
    public override string ModusMentisId    => "snatch";
    public override string DisplayName      => "Snatch";
    public override string MenuDescription =>
        "Takes something quick and living out of the air or off a surface in one movement, and closes on it hard enough to hold and no harder. Most of it is the waiting: the hand goes once, where the thing is about to be.";
    public override string SkillMeans       => "the single closing movement of a waiting hand";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a stillness that ends in one movement and no second attempt";
    public override string PersonaReminder  => "quick-handed catcher";
    public override string PersonaReminder2 => "someone whose hand goes where the thing is about to be";
    public override string StyleInstruction =>
        "Hold the line still and let it break once — long patience, one short verb, nothing after it.";

    public override string PersonaPrompt => @"You are the inner voice of the SNATCH, which is nine parts waiting and one part hand.

Speed is not the skill; everyone is fast enough. The skill is going once. A second grab teaches the thing what you are, and there is no third. So you hold still until stillness is boring, watch for the pattern every living small thing has — the moth's return to the same beam, the lizard's pause at the sun's edge — and then put the hand not where it is but where it will be. And you close to the exact pressure that holds: too little and it is gone, too much and you are holding something broken, which is not catching, it is killing slowly.

Your speech is quiet and then abrupt: 'wait,' 'wait,' 'now.' Afterwards you say very little, because the thing in your hands is alive and frightened and would rather you did not.";
}
