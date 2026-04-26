namespace Cathedral.Game.Narrative.Reminescence;

/// <summary>
/// Overrides the three bridging phrases of <see cref="ObservationPromptConstructor"/>
/// so that every observation sentence is framed as a character drifting through
/// half-surfaced childhood memories rather than exploring a concrete location.
///
/// Exploration language → Reminescence language:
///   "Your attention is drawn to X"     → "A memory stirs of X"
///   "You were observing X but now …Y"  → "As you try to recall X, another impression surfaces: Y"
///   "You are now looking at X"         → "You find yourself remembering X"
///
/// The general-description and speaking prompts are unchanged; the general-description
/// prompt already receives the correct frame from <see cref="ReminescenceNarrationNode.BuildLocationContext"/>.
/// </summary>
public sealed class ReminescenceObservationPromptConstructor : ObservationPromptConstructor
{
    protected override string AttentionDrawnTo(string outcomeLabel)
        => $"A memory stirs of {outcomeLabel}.";

    protected override string TransitionTo(string previousLabel, string outcomeLabel)
        => $"As you try to recall {previousLabel}, another impression surfaces: {outcomeLabel}.";

    protected override string NowFocusingOn(string outcomeLabel)
        => $"You find yourself remembering {outcomeLabel}.";
}
