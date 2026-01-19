using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Abstract base class for elements in a skill chain.
/// The skill chain represents the sequence of skills involved in an action:
/// Observation -> Thinking -> Action
/// Each element has an associated skill and optional link to its origin element.
/// </summary>
public abstract class SkillChainElement
{
    /// <summary>
    /// The skill associated with this chain element.
    /// </summary>
    public abstract Skill ChainSkill { get; }
    
    /// <summary>
    /// The previous element in the skill chain (if any).
    /// - Observations: always null (they are roots of the chain)
    /// - Thinking: points to the observation that triggered it
    /// - Action: points to the thinking block that generated it
    /// </summary>
    public SkillChainElement? ChainOrigin { get; set; }
    
    /// <summary>
    /// Calculates the total skill level sum by traversing the chain back to the root.
    /// This represents the number of dice that will be rolled for a skill check.
    /// </summary>
    public int GetTotalSkillLevel()
    {
        int total = ChainSkill?.Level ?? 0;
        var current = ChainOrigin;
        while (current != null)
        {
            total += current.ChainSkill?.Level ?? 0;
            current = current.ChainOrigin;
        }
        return total;
    }
    
    /// <summary>
    /// Gets all skills in the chain from root to this element.
    /// </summary>
    public List<Skill> GetSkillChain()
    {
        var skills = new List<Skill>();
        var current = this;
        while (current != null)
        {
            if (current.ChainSkill != null)
            {
                skills.Insert(0, current.ChainSkill); // Insert at beginning to maintain order
            }
            current = current.ChainOrigin;
        }
        return skills;
    }
    
    /// <summary>
    /// Checks if a specific SkillChainElement is an ancestor in this element's chain.
    /// This checks the actual element instances, not just matching skills.
    /// </summary>
    public bool IsElementInChain(SkillChainElement? element)
    {
        if (element == null) return false;
        
        var current = this;
        while (current != null)
        {
            if (ReferenceEquals(current, element))
            {
                return true;
            }
            current = current.ChainOrigin;
        }
        return false;
    }
}

/// <summary>
/// Tracks the current state of narration flow.
/// Manages history of narration blocks, current node, thinking attempts, etc.
/// </summary>
public class NarrationState
{
    public string CurrentNodeId { get; set; } = "";
    public int ThinkingAttemptsRemaining { get; set; } = 3;
    public string? SelectedKeyword { get; set; }
    public Skill? SelectedThinkingSkill { get; set; }
    public List<NarrationBlock> NarrationHistory { get; } = new();
    
    public void AddBlock(NarrationBlock block)
    {
        NarrationHistory.Add(block);
    }
    
    public void ClearHistory()
    {
        NarrationHistory.Clear();
    }
    
    public List<string> GetAllKeywords()
    {
        return NarrationHistory
            .Where(b => b.Keywords != null)
            .SelectMany(b => b.Keywords!)
            .Distinct()
            .ToList();
    }
}

/// <summary>
/// Represents a single block of narration text in the UI.
/// Can be observation, thinking (CoT), action result, or outcome.
/// Inherits from SkillChainElement to participate in skill chain calculations.
/// </summary>
public class NarrationBlock : SkillChainElement
{
    public NarrationBlockType Type { get; init; }              // Observation, Thinking, Action, Outcome
    public Skill Skill { get; init; } = null!;                 // Which skill generated this block
    public string Text { get; init; } = "";                    // The narration text
    public List<string>? Keywords { get; init; }               // Highlighted keywords (if observation)
    public List<ParsedNarrativeAction>? Actions { get; init; } // Clickable actions (if thinking)
    
    /// <summary>
    /// Implements SkillChainElement.ChainSkill - returns the skill of this block.
    /// </summary>
    public override Skill ChainSkill => Skill;
    
    /// <summary>
    /// Creates a new NarrationBlock with the specified parameters.
    /// </summary>
    public NarrationBlock(
        NarrationBlockType Type,
        Skill Skill,
        string Text,
        List<string>? Keywords,
        List<ParsedNarrativeAction>? Actions,
        SkillChainElement? ChainOrigin = null)
    {
        this.Type = Type;
        this.Skill = Skill;
        this.Text = Text;
        this.Keywords = Keywords;
        this.Actions = Actions;
        this.ChainOrigin = ChainOrigin;
    }
}

/// <summary>
/// Types of narration blocks that can appear in the UI.
/// </summary>
public enum NarrationBlockType
{
    Observation,   // Skill perceives environment
    Thinking,      // Skill reasons about keyword (CoT)
    Action,        // Player selected action (skill check result)
    Outcome        // Result of action (success/failure)
}

/// <summary>
/// Represents an action generated by a thinking skill.
/// Extended version of ParsedAction for narrative system.
/// Inherits from SkillChainElement to participate in skill chain calculations.
/// The ChainOrigin should point to the thinking block that generated this action.
/// </summary>
public class ParsedNarrativeAction : SkillChainElement
{
    public string ActionText { get; set; } = "";              // Full text including "try to " prefix
    public string DisplayText { get; set; } = "";             // Text without "try to " prefix (for UI)
    public string ActionSkillId { get; set; } = "";           // Which action skill to use for check
    public Skill? ActionSkill { get; set; }                   // Resolved skill reference
    public Skill ThinkingSkill { get; set; } = null!;         // Which thinking skill generated this
    public OutcomeBase PreselectedOutcome { get; set; } = null!;  // Success outcome chosen by thinking skill
    public string Keyword { get; set; } = "";                 // Keyword this action relates to
    
    /// <summary>
    /// Implements SkillChainElement.ChainSkill - returns the action skill.
    /// </summary>
    public override Skill ChainSkill => ActionSkill!;
}
