using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene;

/// <summary>
/// How a potential witness can perceive an action being committed.
/// </summary>
public enum WitnessType
{
    /// <summary>No witness can observe the action.</summary>
    None,

    /// <summary>A witness is in an adjacent area — may hear the action but cannot see it.</summary>
    Audio,

    /// <summary>A witness is in the same area — can both see and hear the action.</summary>
    Visual,
}

/// <summary>
/// Captures the best available witness and how they can perceive an illegal action.
/// Computed once before the action pipeline begins and threaded through evaluation and execution.
/// </summary>
public record WitnessContext(WitnessType Type, NpcEntity? Witness)
{
    public static readonly WitnessContext None = new(WitnessType.None, null);

    /// <summary>
    /// Natural-language description of the witness situation, injected into LLM prompts.
    /// Returns empty string when there is no witness.
    /// </summary>
    public string ToPromptDescription() => Type switch
    {
        WitnessType.Visual =>
            $"There is a potential witness ({Witness?.DisplayName ?? "someone"}) right here" +
            " who could see and hear everything.",
        WitnessType.Audio =>
            $"There is a potential witness ({Witness?.DisplayName ?? "someone"}) a few steps away" +
            " who might hear any noise.",
        _ => string.Empty,
    };
}
