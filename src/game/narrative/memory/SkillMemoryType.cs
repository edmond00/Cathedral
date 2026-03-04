namespace Cathedral.Game.Narrative.Memory;

/// <summary>
/// Defines which long-term memory module a skill belongs to.
/// Working and Residual modules accept any skill regardless of this value.
/// Procedural, Semantic, and Sensory modules only accept skills whose
/// MemoryType matches the module type.
/// </summary>
public enum SkillMemoryType
{
    /// <summary>Motor / learned physical patterns. Governed by the cerebellum.</summary>
    Procedural,

    /// <summary>Conceptual and factual knowledge. Governed by the cerebrum.</summary>
    Semantic,

    /// <summary>Perceptual and experiential memory. Governed by the hippocampus.</summary>
    Sensory
}
