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

    /// <summary>
    /// A second question about the same thing, asked alongside this one and weighed with it: the
    /// verdict's side is the SUM of the two questions' log-odds of help against refusal, and the id on
    /// that side — degree or refusal — is this node's. Null for an ordinary node. See
    /// <c>ItemUseCritic.WeighAsync</c> and <c>CriticTrees.BuildItemAppropriatenessTree</c> for why one
    /// question alone cannot do it. Its choice ids must be this node's.
    /// </summary>
    public CriticNode? Counterweight { get; set; }

    /// <summary>
    /// Yes/no questions that settle the degree of a pass this node itself accepted, asked in order: the
    /// first answered yes gives its choice id; none answered yes gives the last help choice (the clumsy
    /// degree). Empty for a node whose own answer gives the degree. See
    /// <c>CriticTrees.BuildToolSubstitutionTree</c> for why the substitution tree needs them.
    /// </summary>
    public List<CriticDegreeQuestion> DegreeQuestions { get; set; } = new();

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

/// <summary>
/// One of a node's <see cref="CriticNode.DegreeQuestions"/>: a full question (the node's shared head
/// and the question itself), answered yes or no, and the choice id a yes stands for.
/// </summary>
public record CriticDegreeQuestion(string Question, string ChoiceId);
