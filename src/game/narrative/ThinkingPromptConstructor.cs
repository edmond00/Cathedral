using System.Collections.Generic;
using System.Linq;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Constructs prompts for thinking skills to generate Chain-of-Thought reasoning and actions.
/// Builds the user message that gets sent to the LLM with the cached persona prompt.
/// </summary>
public class ThinkingPromptConstructor
{
    /// <summary>
    /// Builds a thinking request for the LLM.
    /// Includes keyword context, possible outcomes, available action skills, and avatar state.
    /// Separates straightforward outcomes from circuitous outcomes in the prompt.
    /// </summary>
    public string BuildThinkingPrompt(
        string keyword,
        NarrationNode node,
        List<OutcomeWithMetadata> outcomesWithMetadata,
        List<Skill> actionSkills,
        Avatar avatar,
        Skill thinkingSkill)
    {
        // Separate straightforward and circuitous outcomes
        var straightforwardOutcomes = outcomesWithMetadata
            .Where(o => !o.IsCircuitous)
            .Select(o => o.Outcome.ToNaturalLanguageString())
            .ToList();
            
        var circuitousOutcomes = outcomesWithMetadata
            .Where(o => o.IsCircuitous)
            .Select(o => o.Outcome.ToNaturalLanguageString())
            .ToList();
        
        // Get action skill IDs
        var actionSkillIds = actionSkills.Select(s => s.SkillId).ToList();
        
        // Build outcomes section with separation
        var outcomesSection = new System.Text.StringBuilder();
        outcomesSection.AppendLine("What could happen:");
        foreach (var outcome in straightforwardOutcomes)
        {
            outcomesSection.AppendLine($"- {outcome}");
        }
        
        if (circuitousOutcomes.Count > 0)
        {
            outcomesSection.AppendLine();
            outcomesSection.AppendLine("Other possibilities (harder to achieve):");
            foreach (var outcome in circuitousOutcomes)
            {
                outcomesSection.AppendLine($"- {outcome}");
            }
        }
        
        var prompt = $@"Your attention is drawn to: ""{keyword}""

Current situation:
{node.GenerateNeutralDescription(avatar.CurrentLocationId)}

Skills you can apply:
{string.Join("\n", actionSkills.Select(s => $"- {s.SkillId}: {s.DisplayName}"))}

{outcomesSection.ToString().TrimEnd()}

Think about what ""{keyword}"" suggests to you. What possibilities does it open?

First, express your internal thoughts about this element—why it catches your interest,
what connections you see, how it relates to your capabilities.

Then propose 2–5 specific things you could try.

Guidelines:
- Think and speak as {thinkingSkill.PersonaTone}.
- Your perspective and instincts drive everything.
- Connect ""{keyword}"" naturally to what you can do.
- Each action begins with 'try to'.
- Let your reasoning flow into your proposed actions.

Output fields:
- reasoning_text
- actions[] with: action_skill, outcome, action_description
";

        return prompt;
    }
    
    /// <summary>
    /// Legacy overload that accepts plain outcomes (treats all as straightforward).
    /// </summary>
    public string BuildThinkingPrompt(
        string keyword,
        NarrationNode node,
        List<OutcomeBase> possibleOutcomes,
        List<Skill> actionSkills,
        Avatar avatar,
        Skill thinkingSkill)
    {
        var outcomesWithMetadata = possibleOutcomes
            .Select(o => OutcomeWithMetadata.Straightforward(o))
            .ToList();
            
        return BuildThinkingPrompt(keyword, node, outcomesWithMetadata, actionSkills, avatar, thinkingSkill);
    }
}
