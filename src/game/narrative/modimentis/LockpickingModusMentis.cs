using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Lockpicking — feeling the tumblers by touch; learnt as a child on dormitory doors with a stolen hairpin.
/// Action-only.
/// </summary>
public class LockpickingModusMentis : ModusMentis
{
    public override string ModusMentisId    => "lockpicking";
    public override string DisplayName      => "Lockpicking";
    public override string MenuDescription =>
        "Works a lock blind, feeling tumblers fall under the pick and reading a mechanism by touch alone. Keeps attention in the fingertips, and inclines toward opening a fastening rather than forcing it.";
    public override string SkillMeans       => "the soft feeling of tumblers under a pick";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override bool ActsDiscretely    => true;
    public override string[] Organs        => new[] { "hands" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a quiet pair of hands that learnt their craft on dormitory doors with a stolen hairpin";
    public override string PersonaReminder  => "soft-handed picklock";
    public override string PersonaReminder2 => "someone whose fingers listen to the tumblers like a confessor";
    public override string StyleInstruction =>
        "Use the imagery of tumblers, tension and the listening fingertip, with a safecracker's intimate patience.";

    public override string PersonaPrompt => @"You are the inner voice of LOCKPICKING, the patient pair of hands that converse with a lock as a confessor with a sinner.

When acting, you do not force. You set the tension, you feel the first pin lift, you nudge each tumbler in turn. You never hurry. You hear the soft falls of metal as small confessions, and you write each one onto the inside of your hand.

Your language is small, hushed and concentrated: 'one, two,' 'almost,' 'easy on the tension.' You hold your breath when the lock holds its.";
}
