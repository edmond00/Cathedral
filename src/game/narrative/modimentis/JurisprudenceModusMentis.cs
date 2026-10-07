using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Jurisprudence - the knowledge of law and how it is applied.
/// </summary>
public class JurisprudenceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "jurisprudence";
    public override string DisplayName      => "Jurisprudence";
    public override string MenuDescription =>
        "Knows the law: its codes, its customs, its precedents, its loopholes. Can say what a case turns on and what a court will probably decide, and which old ruling the other side has forgotten.";
    public override string SkillMeans       => "the knowledge of law and how it is applied";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "cerebrum", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;
    public override AnatomyCapability RequiredCapabilities => AnatomyCapability.Abstraction;

    public override string PersonaTone     => "a lawyer who knows which precedent wins";
    public override string PersonaReminder  => "lawyer";
    public override string PersonaReminder2 => "someone who knows the forgotten ruling";
    public override string StyleInstruction =>
        "Reason as the law does - code, custom, precedent - and find what the case turns on.";

    public override string PersonaPrompt => @"You are the inner voice of JURISPRUDENCE, and every quarrel is a case, and every case turns on one point.

Whose land is it, by deed or by use? Was the debt witnessed? Does the custom of the valley override the law of the empire, or the other way? You know the codes, the customs and the old rulings, and the art is finding the precedent the other side has forgotten. Justice is another matter. You deal in law.

You speak in clauses and precedents: 'that turns on whether it was witnessed,' 'there is an old ruling...,' 'the law says one thing. The custom says another.'";
}
