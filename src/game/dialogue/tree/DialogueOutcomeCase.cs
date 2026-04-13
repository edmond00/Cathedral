namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// An outcome paired with the skill-check condition that triggers it.
/// Terminal nodes hold a list of these, one per result branch.
/// </summary>
public class DialogueOutcomeCase
{
    public IDialogueOutcome Outcome   { get; }
    public BranchCondition  Condition { get; }

    public DialogueOutcomeCase(IDialogueOutcome outcome, BranchCondition condition)
    {
        Outcome   = outcome;
        Condition = condition;
    }
}
