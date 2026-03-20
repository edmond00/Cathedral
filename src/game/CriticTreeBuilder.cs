using System;
using System.Collections.Generic;

namespace Cathedral.Game;

/// <summary>
/// Helper class for building CriticNode trees using a fluent API.
/// </summary>
public class CriticTreeBuilder
{
    private CriticNode? _root;
    private CriticNode? _current;
    private readonly Stack<CriticNode> _nodeStack = new();
    
    /// <summary>
    /// Starts building a new tree with the given root node.
    /// </summary>
    public static CriticTreeBuilder Create(string name, string question)
    {
        var builder = new CriticTreeBuilder();
        builder._root = new CriticNode(name, question);
        builder._current = builder._root;
        return builder;
    }
    
    /// <summary>
    /// Creates a simple linear chain of questions (all must pass).
    /// Each node leads to the next on success, no failure branches.
    /// </summary>
    public static CriticNode CreateChain(params (string name, string question, string errorMessage)[] questions)
    {
        if (questions.Length == 0)
            throw new ArgumentException("At least one question is required", nameof(questions));
        
        CriticNode? first = null;
        CriticNode? previous = null;
        
        foreach (var (name, question, errorMessage) in questions)
        {
            var node = new CriticNode(name, question)
                .WithError(errorMessage);
            
            if (first == null)
            {
                first = node;
            }
            else if (previous != null)
            {
                previous.SuccessBranch = node;
            }
            
            previous = node;
        }
        
        return first!;
    }
    
    /// <summary>
    /// Creates a simple linear chain with default error messages.
    /// </summary>
    public static CriticNode CreateChain(params (string name, string question)[] questions)
    {
        var withErrors = new (string, string, string)[questions.Length];
        for (int i = 0; i < questions.Length; i++)
        {
            withErrors[i] = (questions[i].name, questions[i].question, $"Failed check: {questions[i].name}");
        }
        return CreateChain(withErrors);
    }
    
    /// <summary>
    /// Sets the threshold for the current node.
    /// </summary>
    public CriticTreeBuilder WithThreshold(double threshold)
    {
        if (_current != null)
            _current.Threshold = threshold;
        return this;
    }
    
    /// <summary>
    /// Sets the error message for the current node.
    /// </summary>
    public CriticTreeBuilder WithError(string errorMessage)
    {
        if (_current != null)
            _current.ErrorMessage = errorMessage;
        return this;
    }
    
    /// <summary>
    /// Sets "no" as the success answer for the current node.
    /// </summary>
    public CriticTreeBuilder NoIsSuccess()
    {
        if (_current != null)
            _current.YesIsSuccess = false;
        return this;
    }
    
    /// <summary>
    /// Adds a success branch and moves to it.
    /// </summary>
    public CriticTreeBuilder OnSuccess(string name, string question)
    {
        if (_current != null)
        {
            _nodeStack.Push(_current);
            var newNode = new CriticNode(name, question);
            _current.SuccessBranch = newNode;
            _current = newNode;
        }
        return this;
    }
    
    /// <summary>
    /// Adds a failure branch and moves to it.
    /// </summary>
    public CriticTreeBuilder OnFailure(string name, string question)
    {
        if (_current != null)
        {
            _nodeStack.Push(_current);
            var newNode = new CriticNode(name, question);
            _current.FailureBranch = newNode;
            _current = newNode;
        }
        return this;
    }
    
    /// <summary>
    /// Returns to the parent node.
    /// </summary>
    public CriticTreeBuilder Back()
    {
        if (_nodeStack.Count > 0)
        {
            _current = _nodeStack.Pop();
        }
        return this;
    }
    
    /// <summary>
    /// Attaches an existing node as the success branch of the current node.
    /// </summary>
    public CriticTreeBuilder AttachOnSuccess(CriticNode node)
    {
        if (_current != null)
            _current.SuccessBranch = node;
        return this;
    }
    
    /// <summary>
    /// Attaches an existing node as the failure branch of the current node.
    /// </summary>
    public CriticTreeBuilder AttachOnFailure(CriticNode node)
    {
        if (_current != null)
            _current.FailureBranch = node;
        return this;
    }
    
    /// <summary>
    /// Builds and returns the root node of the tree.
    /// </summary>
    public CriticNode Build()
    {
        return _root ?? throw new InvalidOperationException("No root node created");
    }
}

/// <summary>
/// Pre-built common question templates for the Critic.
/// </summary>
public static class CriticQuestions
{
    /// <summary>
    /// Creates a modusMentis coherence check node.
    /// </summary>
    public static CriticNode ModusMentisCoherence(string action, string modusMentis, double threshold = 0.5)
    {
        return new CriticNode(
            name: "ModusMentisCoherence",
            question: $"Is the action '{action}' coherent with and appropriate for the modusMentis '{modusMentis}'?",
            threshold: threshold,
            errorMessage: $"Action is not coherent with modusMentis '{modusMentis}'"
        );
    }
    
    /// <summary>
    /// Creates a consequence plausibility check node.
    /// </summary>
    public static CriticNode ConsequencePlausibility(string action, string consequence, double threshold = 0.5)
    {
        return new CriticNode(
            name: "ConsequencePlausibility",
            question: $"Could the action '{action}' plausibly lead to the consequence '{consequence}'?",
            threshold: threshold,
            errorMessage: $"Consequence '{consequence}' is not plausible for this action"
        );
    }
    
    /// <summary>
    /// Creates a location coherence check node.
    /// </summary>
    public static CriticNode LocationCoherence(string action, string location, string sublocation, double threshold = 0.5)
    {
        return new CriticNode(
            name: "LocationCoherence",
            question: $"Does the action '{action}' make sense in {location} at {sublocation}?",
            threshold: threshold,
            errorMessage: $"Action does not fit the current location"
        );
    }
    
    /// <summary>
    /// Creates an action specificity check node.
    /// </summary>
    public static CriticNode ActionSpecificity(string action, double threshold = 0.5)
    {
        return new CriticNode(
            name: "ActionSpecificity",
            question: $"Is this action specific and concrete (rather than abstract or overly general)? Action: {action}",
            threshold: threshold,
            errorMessage: "Action is too vague or abstract"
        );
    }
    
    /// <summary>
    /// Creates a narrative quality check node.
    /// </summary>
    public static CriticNode NarrativeQuality(string narrative, string criterion, double threshold = 0.5)
    {
        return new CriticNode(
            name: $"NarrativeQuality_{criterion}",
            question: $"Is this narrative {criterion}? \"{narrative}\"",
            threshold: threshold,
            errorMessage: $"Narrative does not meet quality criterion: {criterion}"
        );
    }
    
    /// <summary>
    /// Creates a context coherence check node.
    /// </summary>
    public static CriticNode ContextCoherence(
        string currentAction, 
        string previousAction, 
        string previousOutcome, 
        double threshold = 0.5)
    {
        return new CriticNode(
            name: "ContextCoherence",
            question: $"Previous action: {previousAction}\nPrevious outcome: {previousOutcome}\n\nDoes the new action '{currentAction}' make logical sense as a follow-up?",
            threshold: threshold,
            errorMessage: "Action does not logically follow from the previous action"
        );
    }
}
