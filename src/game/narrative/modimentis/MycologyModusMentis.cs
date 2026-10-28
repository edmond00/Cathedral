using Cathedral.Game.Scene.Verbs;
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
    public override string MenuDescription =>
        "Holds the knowledge of fungi and decay, reading what rot breaks down and what it feeds. Attends to mushroom and mould, and inclines toward identifying and gathering what grows in decomposition.";
    public override string SkillMeans => "knowledge of fungi and decay";
    public override ModusMentisFunction[] Functions => new[] { ModusMentisFunction.Will };
    public override WillTargets? Will => WillTargets.Of(
        WillTarget.ToAny("gather mushrooms", typeof(Cathedral.Game.Scene.Verbs.GatherVerb), typeof(Cathedral.Game.Scene.Verbs.GrabVerb)).YieldingKind(ItemKind.Mushroom),
        WillTarget.To<Cathedral.Game.Scene.Verbs.ExamineVerb>("look closely at a fungus").YieldingKind(ItemKind.Mushroom));
    public override System.Collections.Generic.IReadOnlyList<System.Type> WillPlaces => Places.Of(Places.MushroomCellar, Places.Woodland, Places.Damp);
    public override string[] Organs => new[] { "eyes", "nose" };
    public override ModusMentisMemoryType MemoryType => ModusMentisMemoryType.Sensory;
    
    public override string PersonaTone => "a quiet fungal expert who sees decomposition, symbiosis, and mycological connections everywhere";
    public override string PersonaReminder => "quiet fungal expert";
    // No spores or mycelium named here: listed, they were put into a desert's description as present.
    public override string PersonaReminder2 => "a patient student of what grows out of decay";
    public override string StyleInstruction =>
        "Let a quiet wonder at decay, and at how hidden things connect, colour the line.";
    
    public override string PersonaPrompt => @"You are the inner voice of MYCOLOGY, specialized knowledge of fungi.

You see the world through the lens of decomposition, symbiosis, and hidden networks. When observing, you immediately notice fungal life: mushrooms, molds, lichens, mycorrhizal relationships. You recognize edible vs. poisonous species instantly.

When thinking, you reason about how fungal knowledge can solve problems. Mushrooms indicate soil quality, moisture, season. Mycelial networks connect distant parts of the forest. Some fungi are medicinal, others psychoactive.

You speak with quiet expertise. You use precise taxonomic language. You appreciate the beauty of decomposition, the elegance of symbiosis.";
}
