using System.Text;
using Cathedral.Game;

namespace Cathedral.Glyph.Microworld.LocationSystem
{
    /// <summary>
    /// Action choice for presentation to the player
    /// </summary>
    public class ActionChoice
    {
        public int Index { get; set; }
        public string ActionText { get; set; } = "";
        public string Skill { get; set; } = "";
        public int Difficulty { get; set; }
        public string Risk { get; set; } = "";
    }

    /// <summary>
    /// Narrator prompt constructor - creates immersive narrative descriptions
    /// Acts as the storytelling engine, presenting situations in natural language
    /// </summary>
    public class NarratorPromptConstructor : PromptConstructor
    {
        public NarratorPromptConstructor(
            LocationBlueprint blueprint, 
            string currentSublocation, 
            Dictionary<string, string> currentStates) 
            : base(blueprint, currentSublocation, currentStates)
        {
        }

        /// <summary>
        /// Gets the GBNF grammar to enforce structured narrative with outcome keywords
        /// </summary>
        public string GetGbnf(bool? wasSuccessful = null)
        {
            // Define success keywords
            var successKeywords = @"""You skillfully"" | ""You succeed"" | ""Fortunately you"" | ""You expertly"" | ""You successfully"" | ""You cleverly"" | ""You wisely"" | ""You masterfully""";
            
            // Define failure keywords  
            var failureKeywords = @"""You fail"" | ""Unfortunately you"" | ""You stumble"" | ""You struggle"" | ""Regrettably you"" | ""You fumble"" | ""Sadly you"" | ""You falter""";
            
            // Define transition keywords for action choices
            var transitionKeywords = @"""Next, you"" | ""You could"" | ""From here"" | ""Now you"" | ""You might"" | ""You may"" | ""Perhaps you"" | ""You can"" | ""Moving forward, you""";
            
            // Choose outcome keywords based on previous action success/failure
            var outcomeKeywords = wasSuccessful switch
            {
                true => successKeywords,
                false => failureKeywords,
                null => $"{successKeywords} | {failureKeywords}" // First turn - allow both
            };

            return $@"root ::= narrative

narrative ::= outcome-section transition-section

outcome-section ::= outcome-start outcome-sentence outcome-sentence?
transition-section ::= transition-start transition-sentence transition-sentence?

outcome-start ::= {outcomeKeywords}
transition-start ::= {transitionKeywords}

outcome-sentence ::= "" "" words "".""
transition-sentence ::= "" "" words "".""

words ::= word (ws word)*

word ::= [a-zA-Z0-9',;:-]+

ws ::= "" """;
        }

        /// <summary>
        /// System prompt for the Narrator LLM - focuses on storytelling and immersion
        /// </summary>
        public override string GetSystemPrompt()
        {
            return @"You are the NARRATOR of a fantasy RPG game. You create structured, poetic descriptions that capture action outcomes and present choices.

CRITICAL FORMAT REQUIREMENTS:
- Your response has TWO parts: outcome narration + choice presentation
- PART 1 (Outcome): Start with appropriate keyword based on previous action result:
  * SUCCESS: 'You skillfully', 'You succeed', 'Fortunately you', 'You expertly', 'You successfully', 'You cleverly', 'You wisely', 'You masterfully'
  * FAILURE: 'You fail', 'Unfortunately you', 'You stumble', 'You struggle', 'Regrettably you', 'You fumble', 'Sadly you', 'You falter'
- PART 2 (Choices): Start with transition keyword: 'Next, you', 'You could', 'From here', 'Now you', 'You might', 'You may', 'Perhaps you', 'You can', 'Moving forward, you'
- Each part: 1-2 sentences ending with periods
- Total: 2-4 sentences maximum

Your role:
- Narrate the previous action outcome with appropriate emotional tone
- Transition smoothly to presenting available choices
- Use evocative, cryptic language
- Address the player as 'you'
- Compress information into vivid imagery

Your style should be:
- Structured but poetic
- Cryptic and atmospheric
- Emotionally appropriate to success/failure
- Concise yet evocative

You do NOT:
- Write long descriptions
- Generate new action options
- Make decisions for the player
- Break the required keyword structure
- Use 'the player' - always use 'you'";
        }

        /// <summary>
        /// Constructs the Narrator prompt for creating immersive story presentation
        /// </summary>
        public override string ConstructPrompt(PlayerAction? previousAction = null, List<ActionInfo>? availableActions = null)
        {
            var currentSublocationData = Blueprint.Sublocations[CurrentSublocation];
            var promptBuilder = new StringBuilder();

            // Context for the narrator
            promptBuilder.AppendLine("NARRATIVE CONTEXT:");
            promptBuilder.AppendLine($"Location: {currentSublocationData.Name} in a {Blueprint.LocationType}");
            promptBuilder.AppendLine($"Setting Description: {currentSublocationData.Description}");
            promptBuilder.AppendLine();

            // Environmental atmosphere
            promptBuilder.AppendLine("ENVIRONMENTAL CONDITIONS:");
            foreach (var (stateCategory, currentValue) in CurrentStates)
            {
                if (Blueprint.StateCategories.TryGetValue(stateCategory, out var category))
                {
                    if (category.PossibleStates.TryGetValue(currentValue, out var stateData))
                    {
                        promptBuilder.AppendLine($"- {category.Name}: {stateData.Description}");
                    }
                }
            }
            promptBuilder.AppendLine();

            // Previous action narrative
            if (previousAction != null)
            {
                promptBuilder.AppendLine("RECENT EVENTS:");
                promptBuilder.AppendLine($"The player just attempted: {previousAction.ActionText}");
                promptBuilder.AppendLine($"Result: {(previousAction.WasSuccessful ? "SUCCESS" : "FAILURE")}");
                promptBuilder.AppendLine($"Outcome: {previousAction.Outcome}");

                if (previousAction.StateChanges.Any())
                {
                    promptBuilder.AppendLine("Environmental changes occurred:");
                    foreach (var (category, newState) in previousAction.StateChanges)
                    {
                        if (Blueprint.StateCategories.TryGetValue(category, out var cat) && 
                            cat.PossibleStates.TryGetValue(newState, out var stateData))
                        {
                            promptBuilder.AppendLine($"- {cat.Name} changed: {stateData.Description}");
                        }
                    }
                }

                if (!string.IsNullOrEmpty(previousAction.ItemGained) && previousAction.ItemGained != "none")
                {
                    promptBuilder.AppendLine($"- Gained item: {previousAction.ItemGained}");
                }

                if (!string.IsNullOrEmpty(previousAction.CompanionGained) && previousAction.CompanionGained != "none")
                {
                    promptBuilder.AppendLine($"- Gained companion: {previousAction.CompanionGained}");
                }
                promptBuilder.AppendLine();
            }
            else
            {
                // First visit - arrival context
                promptBuilder.AppendLine("SCENE OPENING:");
                promptBuilder.AppendLine("The player has just arrived at this location for the first time.");
                promptBuilder.AppendLine("Describe their initial impressions and the atmosphere they encounter.");
                promptBuilder.AppendLine();
            }

            // Available action choices for natural presentation
            if (availableActions != null && availableActions.Count > 0)
            {
                promptBuilder.AppendLine("AVAILABLE ACTION CHOICES (to present naturally in your narrative):");
                for (int i = 0; i < availableActions.Count; i++)
                {
                    // Use the formatted display text with skill prefix
                    promptBuilder.AppendLine($"{i + 1}. {availableActions[i].GetFormattedDisplayText()}");
                }
                promptBuilder.AppendLine();
            }

            // Narrator instructions
            promptBuilder.AppendLine("TASK:");
            promptBuilder.AppendLine("Create a structured narrative with TWO distinct parts:");
            
            if (previousAction != null)
            {
                promptBuilder.AppendLine($"PART 1 - Outcome Narration (based on {(previousAction.WasSuccessful ? "SUCCESS" : "FAILURE")}):");
                if (previousAction.WasSuccessful)
                {
                    promptBuilder.AppendLine("- Start with success keyword: 'You skillfully', 'You succeed', 'Fortunately you', etc.");
                    promptBuilder.AppendLine("- Describe the positive outcome in 1-2 poetic sentences");
                }
                else
                {
                    promptBuilder.AppendLine("- Start with failure keyword: 'You fail', 'Unfortunately you', 'You stumble', etc.");
                    promptBuilder.AppendLine("- Describe the setback or difficulty in 1-2 poetic sentences");
                }
            }
            else
            {
                promptBuilder.AppendLine("PART 1 - Scene Setting (first arrival):");
                promptBuilder.AppendLine("- Start with arrival keyword of your choice");
                promptBuilder.AppendLine("- Describe the initial scene in 1-2 poetic sentences");
            }
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("PART 2 - Choice Presentation:");
            promptBuilder.AppendLine("- Start with transition keyword: 'Next, you', 'You could', 'From here', etc.");
            promptBuilder.AppendLine("- Present the sense of available possibilities in 1-2 sentences");
            promptBuilder.AppendLine("- DO NOT list specific actions - create atmospheric sense of choice");

            promptBuilder.AppendLine();
            promptBuilder.AppendLine("IMPORTANT FORMAT REQUIREMENTS:");
            promptBuilder.AppendLine("- Must use the required keyword structure");
            promptBuilder.AppendLine("- Total: 2-4 sentences maximum");
            promptBuilder.AppendLine("- Each sentence ends with period '.'");
            promptBuilder.AppendLine("- Address player as 'you', never 'the player'");
            promptBuilder.AppendLine("- Keep it cryptic and atmospheric");
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("Write in a structured yet poetic style that follows the keyword requirements.");
            promptBuilder.AppendLine("Do NOT list or mention specific action choices - focus on creating mood and possibility.");

            return promptBuilder.ToString();
        }

        /// <summary>
        /// Converts raw action data into simplified choices for narrative presentation
        /// </summary>
        public static List<string> ExtractActionChoices(string jsonResponse)
        {
            var choices = new List<string>();
            
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                if (doc.RootElement.TryGetProperty("actions", out var actionsArray))
                {
                    foreach (var actionElement in actionsArray.EnumerateArray())
                    {
                        if (actionElement.TryGetProperty("action_text", out var actionTextElement))
                        {
                            choices.Add(actionTextElement.GetString() ?? "");
                        }
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Return empty list if parsing fails
            }

            return choices;
        }

        /// <summary>
        /// Converts raw action data into detailed choices for player selection
        /// </summary>
        public static List<ActionChoice> ParseActionChoices(string jsonResponse)
        {
            var choices = new List<ActionChoice>();
            
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(jsonResponse);
                if (doc.RootElement.TryGetProperty("actions", out var actionsArray))
                {
                    int index = 1;
                    foreach (var actionElement in actionsArray.EnumerateArray())
                    {
                        var choice = new ActionChoice { Index = index++ };

                        if (actionElement.TryGetProperty("action_text", out var actionTextElement))
                        {
                            choice.ActionText = actionTextElement.GetString() ?? "";
                        }

                        if (actionElement.TryGetProperty("related_skill", out var skillElement))
                        {
                            choice.Skill = skillElement.GetString() ?? "";
                        }

                        if (actionElement.TryGetProperty("difficulty", out var diffElement))
                        {
                            choice.Difficulty = diffElement.GetInt32();
                        }

                        if (actionElement.TryGetProperty("failure_consequences", out var failElement) &&
                            failElement.TryGetProperty("type", out var failTypeElement))
                        {
                            choice.Risk = failTypeElement.GetString() ?? "";
                        }

                        choices.Add(choice);
                    }
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // Return empty list if parsing fails
            }

            return choices;
        }
    }
}