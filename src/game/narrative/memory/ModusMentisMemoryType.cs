namespace Cathedral.Game.Narrative.Memory;

/// <summary>
/// Defines which long-term memory module a modusMentis belongs to.
/// Working and Residual modules accept any modusMentis regardless of this value.
/// Procedural, Semantic, and Sensory modules only accept modiMentis whose
/// MemoryType matches the module type.
/// </summary>
public enum ModusMentisMemoryType
{
    /// <summary>Motor / learned physical patterns. Governed by the cerebellum.</summary>
    Procedural,

    /// <summary>Conceptual and factual knowledge. Governed by the cerebrum.</summary>
    Semantic,

    /// <summary>Perceptual and experiential memory. Governed by the hippocampus.</summary>
    Sensory
}
