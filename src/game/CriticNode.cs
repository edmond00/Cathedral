using System;

namespace Cathedral.Game;

/// <summary>
/// Represents a node in the Critic's binary decision tree.
/// Each node contains a yes/no question that the Critic evaluates.
/// </summary>
public class CriticNode
{
    /// <summary>
    /// Unique name for this node (used in trace info).
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// The yes/no question to ask the Critic.
    /// </summary>
    public string Question { get; set; } = string.Empty;
    
    /// <summary>
    /// If true, "yes" means success. If false, "no" means success.
    /// </summary>
    public bool YesIsSuccess { get; set; } = true;
    
    /// <summary>
    /// The probability threshold (0.0 to 1.0) required for the answer to be considered.
    /// For YesIsSuccess=true: p(yes) must be >= threshold for success.
    /// For YesIsSuccess=false: p(no) must be >= threshold for success.
    /// </summary>
    public double Threshold { get; set; } = 0.5;
    
    /// <summary>
    /// Error message to return if this node's check fails.
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional next node to evaluate if this node succeeds.
    /// If null, the tree evaluation ends on success.
    /// </summary>
    public CriticNode? SuccessBranch { get; set; }
    
    /// <summary>
    /// Optional next node to evaluate if this node fails.
    /// If null, the tree evaluation ends on failure.
    /// </summary>
    public CriticNode? FailureBranch { get; set; }
    
    /// <summary>
    /// Creates a simple node with default settings (yes = success, threshold = 0.5).
    /// </summary>
    public CriticNode() { }
    
    /// <summary>
    /// Creates a node with the specified name and question.
    /// </summary>
    public CriticNode(string name, string question)
    {
        Name = name;
        Question = question;
    }
    
    /// <summary>
    /// Creates a fully configured node.
    /// </summary>
    public CriticNode(
        string name,
        string question,
        bool yesIsSuccess = true,
        double threshold = 0.5,
        string errorMessage = "",
        CriticNode? successBranch = null,
        CriticNode? failureBranch = null)
    {
        Name = name;
        Question = question;
        YesIsSuccess = yesIsSuccess;
        Threshold = threshold;
        ErrorMessage = errorMessage;
        SuccessBranch = successBranch;
        FailureBranch = failureBranch;
    }
    
    /// <summary>
    /// Fluent API: Sets the error message for this node.
    /// </summary>
    public CriticNode WithError(string errorMessage)
    {
        ErrorMessage = errorMessage;
        return this;
    }
    
    /// <summary>
    /// Fluent API: Sets the threshold for this node.
    /// </summary>
    public CriticNode WithThreshold(double threshold)
    {
        Threshold = threshold;
        return this;
    }
    
    /// <summary>
    /// Fluent API: Sets "no" as the success answer.
    /// </summary>
    public CriticNode NoIsSuccess()
    {
        YesIsSuccess = false;
        return this;
    }
    
    /// <summary>
    /// Fluent API: Sets the success branch.
    /// </summary>
    public CriticNode OnSuccess(CriticNode node)
    {
        SuccessBranch = node;
        return this;
    }
    
    /// <summary>
    /// Fluent API: Sets the failure branch.
    /// </summary>
    public CriticNode OnFailure(CriticNode node)
    {
        FailureBranch = node;
        return this;
    }
}
