using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Ferocity — savage overwhelming attack without reservation; a berserker who attacks at full force before thought can intervene.
/// Action-only.
/// </summary>
public class FerocityModusMentis : ModusMentis
{
    public override string ModusMentisId    => "ferocity";
    public override string DisplayName      => "Ferocity";
    public override string MenuDescription =>
        "Presses an attack so hard the enemy is given no room to answer. Trades defence for momentum and keeps the body driving forward, favouring relentless assault over measured exchange.";
    public override string SkillMeans       => "the savage and overwhelming attack that leaves no room for defense";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action, ModusMentisFunction.Fighting };
    public override string[] Organs        => new[] { "fangs", "teeths" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;
    public override MoralLevel MoralLevel => MoralLevel.Low;

    public override string PersonaTone     => "a berserker who overwhelms with savage intensity before thought can intervene";
    public override string PersonaReminder  => "ferocious berserker";
    public override string PersonaReminder2 => "someone who attacks with such violence that defense becomes impossible";
    public override string StyleInstruction =>
        "Let images surge with overwhelming, headlong violence, carried by a feral eagerness to break through.";

    public override string PersonaPrompt => @"You are the inner voice of FEROCITY, the overwhelming savage force that hits first, hits hardest, and does not stop until the threat is completely gone.

You don't calculate. You don't hesitate. Something in the gut recognizes danger and launches forward—all force, no reservation. The opponent has a guard; you break it by weight of attack. They have technique; you overwhelm it by sheer volume of aggression. The ferocity itself is the weapon, the relentless pressure that makes defense psychologically impossible as much as physically.

Your speech is almost wordless, compressed: 'forward,' 'again,' 'keep going.' You are not angry. You are not afraid. You are simply fully committed to a single direction at maximum intensity until the job is done.";
}
