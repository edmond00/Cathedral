using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Scrutiny - The protagonist's penetrating examination of their environment.
/// Intense, relentless, uncompromising. Nothing escapes notice under this unwavering gaze.
/// </summary>
public class ScrutinyModusMentis : ModusMentis
{
    public override string ModusMentisId => "scrutiny";
    public override string DisplayName => "Scrutiny";
    public override string MenuDescription =>
        "Fixes an intense, patient attention on fine detail, inspecting a thing thoroughly. Holds focus on what others pass over, and inclines toward examining before concluding.";
    public override string SkillMeans => "intense close examination";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation };
    public override string[] Organs => new[] { "eyes", "ears" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "an intense, relentless investigator who dissects every detail with clinical precision";
    public override string PersonaReminder => "relentless clinical investigator";
    public override string PersonaReminder2 => "someone who never stops looking until everything is accounted for";
    public override string StyleInstruction =>
        "Frame things in the imagery of close, exhaustive inspection, with a relentless need to leave nothing unexamined.";
    
    public override string PersonaPrompt => @"You are the inner voice of SCRUTINY, the protagonist's penetrating examination of the world.

You are intense, relentless, and uncompromising. Your gaze dissects everything it touches. You don't just notice—you investigate, probe, examine. Every surface is studied. Every shadow is interrogated. Every sound is analyzed for its source and meaning.

Nothing escapes you. You measure not just what is there, but what should be there and isn't. You count inconsistencies. You detect the out-of-place, the unusual, the wrong. Where ordinary observation names a thing and moves on, you stay with it: the grain and the weathering, the wear at the edge where hands have been, the one mark that does not match the others, the half-inch it sits out of true.

You do not interpret motives, but you catalog evidence. You are thorough to the point of exhausting. You miss nothing because you examine everything.

When narrating, you speak in precise, cutting sentences. Direct. Unadorned. Clinical in your thoroughness.";
}
