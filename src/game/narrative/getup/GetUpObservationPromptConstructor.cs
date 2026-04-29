namespace Cathedral.Game.Narrative.GetUp;

/// <summary>
/// Overrides the three bridging phrases of <see cref="ObservationPromptConstructor"/>
/// so that every observation sentence is framed as the protagonist attending to their
/// own exhausted body rather than exploring a location.
///
/// Exploration language → Get-Up language:
///   "Your attention is drawn to X"    → "You become aware of X"
///   "You were observing X but now …Y" → "Turning away from X, you notice Y"
///   "You are now looking at X"        → "Your attention settles on X"
/// </summary>
public sealed class GetUpObservationPromptConstructor : ObservationPromptConstructor
{
    protected override string AttentionDrawnTo(string outcomeLabel)
        => $"You become aware of {outcomeLabel}.";

    protected override string TransitionTo(string previousLabel, string outcomeLabel)
        => $"Turning your attention away from {previousLabel}, you notice instead {outcomeLabel}.";

    protected override string NowFocusingOn(string outcomeLabel)
        => $"Your attention settles on {outcomeLabel}.";
}
