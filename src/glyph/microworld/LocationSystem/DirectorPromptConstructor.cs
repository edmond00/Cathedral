using Cathedral.LLM.JsonConstraints;
using System.Text;

namespace Cathedral.Glyph.Microworld.LocationSystem
{
    /// <summary>
    /// Director prompt constructor - generates structured action choices for the player
    /// Acts as the game mechanics engine, providing JSON-formatted action options
    /// </summary>
    public class DirectorPromptConstructor : PromptConstructor
    {
        private readonly int _numberOfActions;
        private JsonField? _currentConstraints;
        private string? _currentTemplate;
        private string? _currentGbnf;

        public DirectorPromptConstructor(
            LocationBlueprint blueprint, 
            string currentSublocation, 
            Dictionary<string, string> currentStates,
            int numberOfActions = 7) 
            : base(blueprint, currentSublocation, currentStates)
        {
            _numberOfActions = numberOfActions;
            RegenerateConstraints();
        }

        /// <summary>
        /// Gets the JSON constraints for the current game state
        /// </summary>
        public JsonField GetConstraints()
        {
            if (_currentConstraints == null)
                RegenerateConstraints();
            return _currentConstraints!;
        }

        /// <summary>
        /// Gets the JSON template for the current constraints
        /// </summary>
        public string GetTemplate()
        {
            if (_currentTemplate == null)
                RegenerateConstraints();
            return _currentTemplate!;
        }

        /// <summary>
        /// Gets the GBNF grammar for the current constraints
        /// </summary>
        public string GetGbnf()
        {
            if (_currentGbnf == null)
                RegenerateConstraints();
            return _currentGbnf!;
        }

        /// <summary>
        /// Regenerates the JSON constraints, template, and GBNF grammar based on current game state
        /// </summary>
        public void RegenerateConstraints()
        {
            _currentConstraints = Blueprint2Constraint.GenerateActionConstraints(
                Blueprint, CurrentSublocation, CurrentStates, _numberOfActions);
            _currentTemplate = JsonConstraintGenerator.GenerateTemplate(_currentConstraints);
            _currentGbnf = JsonConstraintGenerator.GenerateGBNF(_currentConstraints);
        }

        /// <summary>
        /// Updates game state and regenerates constraints
        /// </summary>
        public override void UpdateGameState(string newSublocation, Dictionary<string, string> newStates)
        {
            base.UpdateGameState(newSublocation, newStates);
            RegenerateConstraints();
        }

        /// <summary>
        /// System prompt for the Director LLM - focuses on mechanical action generation
        /// </summary>
        public override string GetSystemPrompt()
        {
            return @"You are the DIRECTOR of a fantasy RPG game. Your role is purely mechanical - you generate structured action options for the player based on the current game state.

Your responsibilities:
- Analyze the current location, environment, and game state
- Generate diverse, contextually appropriate action options
- Provide variety in action types: exploration, interaction, combat preparation, skill use, environmental manipulation, social interaction, etc.
- Consider different approaches: direct action, careful observation, creative problem-solving, risk/reward balance
- Ensure actions match the current environment and available opportunities
- Output only valid JSON in the exact format specified

You do NOT:
- Provide narrative descriptions or storytelling
- Address the player directly
- Add flavor text or atmospheric descriptions
- Make decisions for the player

Focus on mechanical variety and strategic options that fit the current situation.";
        }

        /// <summary>
        /// Constructs the Director prompt for generating action choices
        /// </summary>
        public override string ConstructPrompt(PlayerAction? previousAction = null, List<string>? availableActions = null)
        {
            var currentSublocationData = Blueprint.Sublocations[CurrentSublocation];
            var contextBuilder = new StringBuilder();

            // Current location details
            contextBuilder.AppendLine($"CURRENT GAME STATE:");
            contextBuilder.AppendLine($"Location Type: {Blueprint.LocationType}");
            contextBuilder.AppendLine($"Current Sublocation: {currentSublocationData.Name}");
            contextBuilder.AppendLine($"Description: {currentSublocationData.Description}");
            contextBuilder.AppendLine();

            // Environmental states
            contextBuilder.AppendLine("ENVIRONMENTAL CONDITIONS:");
            foreach (var (stateCategory, currentValue) in CurrentStates)
            {
                if (Blueprint.StateCategories.TryGetValue(stateCategory, out var category))
                {
                    contextBuilder.AppendLine($"- {category.Name}: {currentValue}");
                }
            }
            contextBuilder.AppendLine();

            // Previous action context (if any)
            if (previousAction != null)
            {
                contextBuilder.AppendLine("PREVIOUS ACTION RESULT:");
                contextBuilder.AppendLine($"- Action Taken: {previousAction.ActionText}");
                contextBuilder.AppendLine($"- Outcome: {(previousAction.WasSuccessful ? "Success" : "Failure")} - {previousAction.Outcome}");
                
                if (previousAction.StateChanges.Any())
                {
                    contextBuilder.AppendLine("- State Changes:");
                    foreach (var (category, newState) in previousAction.StateChanges)
                    {
                        contextBuilder.AppendLine($"  • {category} → {newState}");
                    }
                }

                if (!string.IsNullOrEmpty(previousAction.NewSublocation))
                {
                    contextBuilder.AppendLine($"- Location: Moved to {previousAction.NewSublocation}");
                }
                contextBuilder.AppendLine();
            }

            // Connected locations for movement options
            if (Blueprint.SublocationConnections.TryGetValue(CurrentSublocation, out var connections))
            {
                contextBuilder.AppendLine("ACCESSIBLE AREAS:");
                foreach (var connectionId in connections.Take(5)) // Limit to avoid prompt bloat
                {
                    if (Blueprint.Sublocations.TryGetValue(connectionId, out var connectedLocation))
                    {
                        contextBuilder.AppendLine($"- {connectedLocation.Name}: {connectedLocation.Description}");
                    }
                }
                contextBuilder.AppendLine();
            }

            // Generation instructions
            contextBuilder.AppendLine($"TASK: Generate {_numberOfActions} diverse action options for the current situation.");
            contextBuilder.AppendLine("Consider different approaches:");
            contextBuilder.AppendLine("- Exploration and movement");
            contextBuilder.AppendLine("- Environmental interaction");
            contextBuilder.AppendLine("- Skill-based actions");
            contextBuilder.AppendLine("- Social interaction (if applicable)");
            contextBuilder.AppendLine("- Preparation and planning");
            contextBuilder.AppendLine("- Creative problem-solving");
            contextBuilder.AppendLine("- Risk vs. reward decisions");
            contextBuilder.AppendLine();

            // Template instruction
            contextBuilder.AppendLine("Generate a JSON response that exactly matches this template format:");
            contextBuilder.AppendLine(_currentTemplate);
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Respond with valid JSON only, no additional text or explanations.");

            return contextBuilder.ToString();
        }
    }
}