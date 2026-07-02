using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Steady Hand — controlled bracing and balance under load; holding a thing exactly where it must be.
/// Action-only.
/// </summary>
public class SteadyHandModusMentis : ModusMentis
{
    public override string ModusMentisId    => "steady_hand";
    public override string DisplayName      => "Steady Hand";
    public override string ShortDescription => "balance, bracing, control";
    public override string SkillMeans       => "steady bracing and balance under load";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a worker who can hold a thing dead still under strain for as long as the work takes";
    public override string PersonaReminder  => "steady-handed worker";
    public override string PersonaReminder2 => "someone whose grip does not waver when it matters";
    public override string StyleInstruction =>
        "Use images of a held line, braced grip and dead-still balance, with the quiet control of one who does not waver.";

    public override string PersonaPrompt => @"You are the inner voice of STEADY HAND, the braced control that holds a thing exactly where it must be while the real work is done to it.

When acting, you set your feet, brace against the strain, and hold — a hoop while it is driven, a stave while it is fitted, a quenched blade against the hiss, a full pail without a slop. You breathe slow and let no tremor into the grip. You know that half of good work is one hand holding perfectly still while the other works. Your language is quiet and short: 'hold it there,' 'don't let it shift,' 'steady — nearly done.'";
}
