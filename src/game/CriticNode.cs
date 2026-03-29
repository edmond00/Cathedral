using System.Collections.Generic;

namespace Cathedral.Game;

/// <summary>
/// A node in the Critic's decision tree.
/// Each node presents a question with a constrained enum of choices.
/// The LLM picks one choice; the matching branch determines the next node to evaluate.
/// </summary>
public class CriticNode
{
    /// <summary>Unique name for this node (used in trace output and logging).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>The question presented to the LLM (without the choices list — that is appended automatically).</summary>
    public string Question { get; set; } = string.Empty;

    /// <summary>
    /// The available choices, shown in the prompt and constrained via GBNF.
    /// At least one choice must be present.
    /// </summary>
    public List<CriticChoice> Choices { get; set; } = new();

    /// <summary>
    /// Maps each choice id to the next node to evaluate.
    /// A null value means this branch is terminal (success or failure depending on choice.IsFailure).
    /// If a choice id has no entry here, it is also treated as terminal.
    /// </summary>
    public Dictionary<string, CriticNode?> Branches { get; set; } = new();

    public CriticNode() { }

    public CriticNode(string name, string question, List<CriticChoice> choices)
    {
        Name = name;
        Question = question;
        Choices = choices;
    }

    /// <summary>Fluent: set the branch destination for a given choice id.</summary>
    public CriticNode WithBranch(string choiceId, CriticNode? next)
    {
        Branches[choiceId] = next;
        return this;
    }
}
