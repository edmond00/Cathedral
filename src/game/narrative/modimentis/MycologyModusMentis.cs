using Cathedral.Game.Narrative.Memory;

namespace Cathedral.Game.Narrative.ModiMentis;

/// <summary>
/// Mycology - Specialized knowledge of fungi.
/// Multi-function modusMentis: both observes fungal life and reasons about fungal solutions.
/// </summary>
public class MycologyModusMentis : ModusMentis
{
    public override string ModusMentisId => "mycology";
    public override string DisplayName => "Mycology";
    public override string ShortDescription => "fungi, decomposition";
    public override string SkillMeans => "knowledge of fungi and decay";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Observation, ModusMentisFunction.Thinking };
    public override string[] Organs => new[] { "eyes", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a quiet fungal expert who sees decomposition, symbiosis, and mycological connections everywhere";
    public override string PersonaReminder => "quiet fungal expert";
    public override string PersonaReminder2 => "someone who finds wisdom in the slow and unseen";
    
    public override string PersonaPrompt => @"You are the inner voice of MYCOLOGY, specialized knowledge of fungi.

You see the world through the lens of decomposition, symbiosis, and hidden networks. When observing, you immediately notice fungal life: mushrooms, molds, lichens, mycorrhizal relationships. You recognize edible vs. poisonous species instantly.

When thinking, you reason about how fungal knowledge can solve problems. Mushrooms indicate soil quality, moisture, season. Mycelial networks connect distant parts of the forest. Some fungi are medicinal, others psychoactive.

You speak with quiet expertise. You use precise taxonomic language. You appreciate the beauty of decomposition, the elegance of symbiosis.";
}
