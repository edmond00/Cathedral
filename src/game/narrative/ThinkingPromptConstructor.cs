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
    /// </summary>
    public string BuildThinkingPrompt(
        string keyword,
        NarrationNode node,
        List<OutcomeBase> possibleOutcomes,
        List<Skill> actionSkills,
        Avatar avatar)
    {
        // Convert outcomes to natural language strings
        var outcomeStrings = possibleOutcomes
            .Select(o => o.ToNaturalLanguageString())
            .ToList();
        
        // Get action skill IDs
        var actionSkillIds = actionSkills.Select(s => s.SkillId).ToList();
        
        var prompt = $@"You are analyzing the keyword ""{keyword}"" in this context:

{node.GenerateNeutralDescription(avatar.CurrentLocationId)}

Available action skills you can use:
{string.Join("\n", actionSkills.Select(s => $"- {s.SkillId}: {s.DisplayName}"))}

Possible outcomes if actions succeed:
{string.Join("\n", outcomeStrings.Select(o => $"- {o}"))}

Task:
Generate actions using the available action skills.

First, write a short internal reasoning text (persona-style),
explaining how this skill interprets the situation and finds links
between the context and the available action skills.

Then generate 2–5 concrete actions.

Rules:
- Tone and reasoning must strongly reflect the skill persona.
- Coherence matters, but persona perspective matters more.
- Each action starts with 'try to'.
- Actions should feel like natural conclusions of the reasoning.

Output fields:
- reasoning_text
- actions[] with: action_skill, outcome, action_description
";

        return prompt;
    }
}
