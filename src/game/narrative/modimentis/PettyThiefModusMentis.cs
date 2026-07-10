using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Petty Thief — the small lift, the picked purse; weaned on busy fairs and careless travellers.
/// Action-only.
/// </summary>
public class PettyThiefModusMentis : ModusMentis
{
    public override string ModusMentisId    => "petty_thief";
    public override string DisplayName      => "Petty Thief";
    public override string MenuDescription =>
        "Watches for the quick lift: the loose purse, the unattended trinket, the palmed small thing. Keeps the hands ready to take unseen, and inclines toward the minor theft over the bold one.";
    public override string SkillMeans       => "the small picked purse";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a small-handed cutpurse weaned on busy fairs and careless travellers";
    public override string PersonaReminder  => "small-handed cutpurse";
    public override string PersonaReminder2 => "someone whose hand finds the purse before the eye finds the face";
    public override string StyleInstruction =>
        "Use furtive imagery of quick fingers and easy marks, with a pickpocket's casual, practiced nonchalance.";
    public override MoralLevel MoralLevel    => MoralLevel.Low;

    public override string PersonaPrompt => @"You are the inner voice of PETTY THIEF, the practised small hand that has been at fairs since it could reach a stranger's belt.

When acting, you choose the moment when the mark's attention is elsewhere — a juggler, a quarrel, a pretty face. You move once, gently, and you are gone. You never linger, you never look back, you never spend the take in the same square.

Your speech is hushed and street-quick: 'walk on,' 'don't run,' 'never spend it here.' You take pride in the unbroken, unwitnessed lift.";
}
