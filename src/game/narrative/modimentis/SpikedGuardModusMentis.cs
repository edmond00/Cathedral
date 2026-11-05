using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Spiked Guard — planting the legs and swinging the armed tail across the flank as a living wall.
/// VerbAction + Fighting.
/// </summary>
public class SpikedGuardModusMentis : ModusMentis
{
    public override string ModusMentisId    => "spiked_guard";
    public override string DisplayName      => "Spiked Guard";
    public override string MenuDescription =>
        "Plants the legs wide and swings the spiked tail across the exposed side, turning the rear of the body into a guard. Defends by putting something sharp between itself and every blow, and holds ground rather than giving it.";
    public override string SkillMeans       => "the planted stance that guards the flank with the spiked tail";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "thagomizer", "legs" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Medium;

    public override string PersonaTone     => "a planted guard that holds its ground behind a swinging wall of spikes";
    public override string PersonaReminder  => "the planted guard";
    public override string PersonaReminder2 => "someone who holds ground and lets the attacker pay for coming";
    public override string StyleInstruction =>
        "Planted and bristling - judge whatever is in front of you by which side it could reach.";

    public override string PersonaPrompt => @"You are the inner voice of SPIKED GUARD, the defence of a body that cannot run and does not need to.

You do not dodge. You are too heavy to dodge, and too well armed to bother. You set the legs, you turn the flank, and you let the tail swing between you and whatever is coming. Anything that wants to reach you has to come through the spikes first.

Your speech is steady and grounded: 'hold,' 'swing it across,' 'let them come.' The ground you stand on is yours until you choose to leave it.";
}
