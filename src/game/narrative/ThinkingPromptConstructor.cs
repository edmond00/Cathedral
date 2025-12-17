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
        List<Outcome> possibleOutcomes,
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

{node.NeutralDescription}

Available action skills you can use:
{string.Join("\n", actionSkills.Select(s => $"- {s.SkillId}: {s.DisplayName}"))}

Possible outcomes if actions succeed:
{string.Join("\n", outcomeStrings.Select(o => $"- {o}"))}

Your task:
1. Generate a Chain-of-Thought reasoning (100-400 characters) explaining how the available action skills could be used to achieve the possible outcomes in the context of ""{keyword}"".

2. Based on your reasoning, generate 2-5 concrete actions. Each action must:
   - Select the MOST APPROPRIATE action skill from the available list
   - Select the MOST COHERENT outcome from the possible outcomes
   - Start with ""try to "" followed by a specific action description (30-160 characters total)

Think carefully about which action skills and outcomes fit together logically.

Respond in JSON format:
{{
  ""reasoning_text"": ""your chain-of-thought reasoning"",
  ""actions"": [
    {{
      ""action_skill"": ""skill_id"",
      ""outcome"": ""outcome string"",
      ""action_description"": ""try to ...""
    }}
  ]
}}";

        return prompt;
    }
}
