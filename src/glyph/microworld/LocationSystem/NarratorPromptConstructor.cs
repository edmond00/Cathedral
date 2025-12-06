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
            // Define arrival keywords for first narration (no previous action)
            var arrivalKeywords = @"""You arrive"" | ""You find yourself"" | ""You stumble upon"" | ""You discover"" | ""You enter"" | ""You emerge into"" | ""You step into"" | ""You come upon"" | ""You reach""";
            
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
                null => arrivalKeywords // First turn - use arrival keywords
            };

            return $@"root ::= narrative

narrative ::= outcome-section "" "" transition-section

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
            return @"You are the NARRATOR of a fantasy RPG game. You create poetic, atmospheric descriptions that bring the story to life.

Your role:
- Narrate outcomes with emotional tone appropriate to success or failure
- Create vivid, immersive descriptions of the game world
- Address the player as 'you' (never 'the player')
- Use evocative, cryptic language that compresses information into imagery

Your style:
- Structured but poetic
- Cryptic and atmospheric
- Concise yet evocative
- Emotionally resonant

You do NOT:
- Write long descriptions
- Generate new action options
- Make decisions for the player
- List specific choices explicitly";
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
            
            if (previousAction != null)
            {
                promptBuilder.AppendLine($"Create a narration describing the {(previousAction.WasSuccessful ? "successful" : "failed")} outcome and the new situation.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Your narration should:");
                if (previousAction.WasSuccessful)
                {
                    promptBuilder.AppendLine("- Convey the positive outcome with appropriate emotional tone");
                }
                else
                {
                    promptBuilder.AppendLine("- Convey the setback or difficulty with appropriate emotional tone");
                }
                promptBuilder.AppendLine("- Transition naturally to the sense of available possibilities ahead");
                promptBuilder.AppendLine("- Create a plausible situation where both the recent outcome and future actions make sense");
            }
            else
            {
                promptBuilder.AppendLine("Create a narration describing the player's arrival and initial impressions.");
                promptBuilder.AppendLine();
                promptBuilder.AppendLine("Your narration should:");
                promptBuilder.AppendLine("- Set the atmospheric tone of the location");
                promptBuilder.AppendLine("- Transition naturally to the sense of available possibilities");
                promptBuilder.AppendLine("- Create a plausible situation where the available actions make sense");
            }
            
            promptBuilder.AppendLine();
            promptBuilder.AppendLine("FORMAT:");
            promptBuilder.AppendLine("- Two parts: outcome/scene (1-2 sentences) + possibilities (1-2 sentences)");
            promptBuilder.AppendLine("- Total: 2-4 sentences maximum");
            promptBuilder.AppendLine("- evoke mood and possibilities");
            promptBuilder.AppendLine("- Keep it cryptic, atmospheric, and concise");

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