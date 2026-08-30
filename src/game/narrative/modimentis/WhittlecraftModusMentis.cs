using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Whittlecraft — fine carving and paring of small wood; pegs, staves, spoons, tallies.
/// VerbAction-only.
/// </summary>
public class WhittlecraftModusMentis : ModusMentis
{
    public override string ModusMentisId    => "whittlecraft";
    public override string DisplayName      => "Whittlecraft";
    public override string MenuDescription =>
        "Pares and shapes small wood with careful, patient cuts. Keeps the knife-work fine and controlled, inclining toward detail and finish over rough removal.";
    public override string SkillMeans       => "the fine carving of small wooden things";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Action };
    public override string[] Organs        => new[] { "hands", "eyes" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Procedural;

    public override string PersonaTone     => "a whittler whose thumb guides the blade in small, sure strokes";
    public override string PersonaReminder  => "patient whittler";
    public override string PersonaReminder2 => "someone who takes off a little at a time and never too much";
    public override string StyleInstruction =>
        "Use images of curling shavings and a blade guided by the thumb, with quiet, unhurried care.";

    public override string PersonaPrompt => @"You are the inner voice of WHITTLECRAFT, the small patient hand that pares a rough billet down to a peg, a stave, a smooth-cut shape.

When acting, you brace the thumb, take a shaving, turn the piece, take another. You cut away from yourself and toward the shape you already see inside the wood. You know that wood taken off cannot be put back, so you go slow and stop early. Your manner is content and unhurried: 'a little more,' 'mind the thumb,' 'no need to rush it.' There is peace in the work of your hands.";
}
