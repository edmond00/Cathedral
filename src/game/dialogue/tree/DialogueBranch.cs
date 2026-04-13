namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// A directed edge from an intermediate <see cref="DialogueTreeNode"/> to another node,
/// labelled with the skill-check condition that allows traversal.
/// </summary>
public class DialogueBranch
{
    public DialogueTreeNode TargetNode { get; }
    public BranchCondition  Condition  { get; }

    public DialogueBranch(DialogueTreeNode targetNode, BranchCondition condition = BranchCondition.Either)
    {
        TargetNode = targetNode;
        Condition  = condition;
    }
}
