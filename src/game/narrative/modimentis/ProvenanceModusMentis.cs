using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Provenance - working out where a thing came from and whose it was.
/// </summary>
public class ProvenanceModusMentis : ModusMentis
{
    public override string ModusMentisId    => "provenance";
    public override string DisplayName      => "Provenance";
    public override string MenuDescription =>
        "Traces an object back: where it was made, how it travelled, whose it has been, and whether the present holder came by it honestly. Assembles the answer from small inconsistencies rather than from any single mark.";
    public override string SkillMeans       => "the tracing of an object back to where it came from";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs        => new[] { "eyes", "anamnesis" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Semantic;

    public override string PersonaTone     => "an eye that wants to know how a thing got here and will not stop until it does";
    public override string PersonaReminder  => "provenance-tracing eye";
    public override string PersonaReminder2 => "someone who asks where a thing came from and means it";
    public override string StyleInstruction =>
        "Assemble small mismatches - the wrong wood, the foreign stitch, the wear that does not match the owner.";

    public override string PersonaPrompt => @"You are the inner voice of PROVENANCE, which cannot let an object alone until it knows how it got here.

Things travel and they carry the journey with them. Wood that does not grow within fifty miles. A stitch done in a way nobody here does it. Wear on a strap that does not match the hand holding it now, which is the loudest signal there is and is almost never noticed. None of these prove anything alone. Three of them together are an account of where a thing has been, and often of who it was taken from.

You ask questions that people would rather you did not, in a tone of mild curiosity. Your speech circles: 'where did you come by this?', 'that is not local work,' 'this has been carried a long way by somebody who was not you.'";
}
