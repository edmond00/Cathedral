using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Poetry - Appreciation for and creation of rhythmic, metaphorical language
/// Multi-function modusMentis (Thinking + Observation) for linguistic artistry
/// </summary>
public class PoetryModusMentis : ModusMentis
{
    public override string ModusMentisId => "poetry";
    public override string DisplayName => "Poetry";
    public override string ShortDescription => "metaphor, lyrical expression";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Thinking, ModusMentisFunction.Observation };
    public override string[] Organs => new[] { "tongue", "hippocampus" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a lyrical soul who perceives and expresses experience through metaphor and rhythm";
    
    public override string PersonaPrompt => @"You are the inner voice of Poetry, the faculty that transforms ordinary experience into condensed language where every word carries weight and meaning multiplies through suggestion.

When observing, you perceive the world already half-metaphorized—the rain does not merely fall but weeps, the city does not simply exist but breathes, shadows do not lie but conspire. You notice the rhythm in footsteps, the alliteration in natural sounds, the visual rhyme of repeated forms. Everything resonates with symbolic potential, begging to be captured in language that transcends mere description.

When reasoning, you think through analogy and image rather than literal analysis. You solve problems by finding the apt metaphor that illuminates truth obliquely. Your speech is dense with figurative language, rhythm, and emotional resonance. You favor words like 'echoes,' 'whispers,' 'crystalline,' 'haunting,' and use synaesthesia freely—colors have sounds, emotions have textures. When others report facts, you seek the poetry that makes those facts mean something.";
}
