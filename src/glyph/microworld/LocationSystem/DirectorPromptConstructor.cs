using Cathedral.LLM.JsonConstraints;
using Cathedral.Game;
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
        private readonly Random _skillRng;
        private JsonField? _currentConstraints;
        private string? _currentHints;
        private string? _currentGbnf;
        private string[][]? _currentSkills; // Each action has 5 skill candidates
        private bool _isFirstRequest = true;

        public DirectorPromptConstructor(
            LocationBlueprint blueprint, 
            string currentSublocation, 
            Dictionary<string, string> currentStates,
            int numberOfActions = 7) 
            : base(blueprint, currentSublocation, currentStates)
        {
            _numberOfActions = numberOfActions;
            _skillRng = new Random();
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
        /// Gets the field hints for the current constraints
        /// </summary>
        public string GetHints()
        {
            if (_currentHints == null)
                RegenerateConstraints();
            return _currentHints!;
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
        /// Gets the currently sampled skill candidates for debugging/logging
        /// Returns an array where each element contains 5 skill options
        /// </summary>
        public string[][] GetCurrentSkillCandidates()
        {
            if (_currentSkills == null)
                RegenerateConstraints();
            return _currentSkills!;
        }

        /// <summary>
        /// Regenerates the JSON constraints, hints, and GBNF grammar based on current game state
        /// Samples 5 skill candidates for each action to give LLM choice while maintaining variety
        /// </summary>
        public void RegenerateConstraints()
        {
            // Sample 5 skill candidates for each action
            _currentSkills = SampleSkillCandidates(_numberOfActions);
            
            _currentConstraints = Blueprint2Constraint.GenerateActionConstraints(
                Blueprint, CurrentSublocation, CurrentStates, _currentSkills, _numberOfActions);
            _currentHints = JsonConstraintGenerator.GenerateHints(_currentConstraints);
            _currentGbnf = JsonConstraintGenerator.GenerateGBNF(_currentConstraints);
        }

        /// <summary>
        /// Randomly samples 5 skill candidates for each action
        /// Returns an array where each element is a 5-skill array
        /// Samples without replacement to avoid skill repetition
        /// </summary>
        private string[][] SampleSkillCandidates(int actionCount)
        {
            var allSkills = Blueprint2Constraint.GetAvailableSkillsList();
            
            // Create a temporary list for sampling without replacement
            var availableSkills = new List<string>(allSkills);
            
            var skillCandidates = new string[actionCount][];
            for (int i = 0; i < actionCount; i++)
            {
                // Refill the pool if we've used all skills
                if (availableSkills.Count < 5)
                {
                    availableSkills = new List<string>(allSkills);
                }
                
                // Sample 5 different skills for this action
                var candidates = new string[5];
                for (int j = 0; j < 5; j++)
                {
                    int index = _skillRng.Next(availableSkills.Count);
                    candidates[j] = availableSkills[index];
                    availableSkills.RemoveAt(index);
                }
                
                skillCandidates[i] = candidates;
            }
            return skillCandidates;
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
        public override string ConstructPrompt(PlayerAction? previousAction = null, List<ActionInfo>? availableActions = null)
        {
            // Mark that we've moved past the first request if there's a previous action
            if (previousAction != null && _isFirstRequest)
            {
                _isFirstRequest = false;
            }

            // Use different prompt generation based on whether this is the first request
            if (_isFirstRequest)
            {
                return ConstructInitialPrompt();
            }
            else
            {
                return ConstructFollowUpPrompt(previousAction);
            }
        }

        /// <summary>
        /// Constructs the initial exploration prompt for the first action choices
        /// </summary>
        private string ConstructInitialPrompt()
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

            // Generation instructions for initial exploration
            contextBuilder.AppendLine($"TASK: Generate {_numberOfActions} diverse action options for the current situation.");
            contextBuilder.AppendLine();
            
            // Show all possible skills that can be used
            var allUniqueSkills = _currentSkills!
                .SelectMany(sc => sc)
                .Distinct()
                .OrderBy(s => s);
            contextBuilder.AppendLine($"ALL AVAILABLE SKILLS: {string.Join(", ", allUniqueSkills)}");
            contextBuilder.AppendLine();
            
            contextBuilder.AppendLine("IMPORTANT: Each action has been assigned:");
            contextBuilder.AppendLine("- A pre-determined SUCCESS consequence (already fixed in the constraints)");
            contextBuilder.AppendLine("- 5 skill candidates to choose from (LLM chooses 1)");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Skill candidates for each action:");
            for (int i = 0; i < _currentSkills!.Length; i++)
            {
                contextBuilder.AppendLine($"  Action {i + 1}: {string.Join(", ", _currentSkills[i])}");
            }
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Your task: Generate SHORT actions (3-8 words) that:");
            contextBuilder.AppendLine("1. Use one of the provided skill candidates appropriately");
            contextBuilder.AppendLine("2. Logically lead to the pre-determined success consequence");
            contextBuilder.AppendLine("3. Make sense if the failure consequence occurs instead");
            contextBuilder.AppendLine("4. Are contextually appropriate for the current location and situation");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Consider different approaches:");
            contextBuilder.AppendLine("- Exploration and movement");
            contextBuilder.AppendLine("- Environmental interaction");
            contextBuilder.AppendLine("- Skill-based actions");
            contextBuilder.AppendLine("- Social interaction (if applicable)");
            contextBuilder.AppendLine("- Preparation and planning");
            contextBuilder.AppendLine("- Creative problem-solving");
            contextBuilder.AppendLine("- Risk vs. reward decisions");
            contextBuilder.AppendLine();

            // Field reference
            contextBuilder.AppendLine(_currentHints);
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Generate a valid JSON response with the above fields. Structure your response as an object with an 'actions' array.");
            contextBuilder.AppendLine("Respond with valid JSON only, no additional text or explanations.");

            return contextBuilder.ToString();
        }

        /// <summary>
        /// Constructs a follow-up prompt that emphasizes relevance to the previous action
        /// </summary>
        private string ConstructFollowUpPrompt(PlayerAction? previousAction)
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

            // EMPHASIZED: Previous action context
            if (previousAction != null)
            {
                contextBuilder.AppendLine("═══════════════════════════════════════");
                contextBuilder.AppendLine("PREVIOUS ACTION AND ITS CONSEQUENCES:");
                contextBuilder.AppendLine("═══════════════════════════════════════");
                contextBuilder.AppendLine($"Action Taken: {previousAction.ActionText}");
                contextBuilder.AppendLine($"Outcome: {(previousAction.WasSuccessful ? "SUCCESS" : "FAILURE")} - {previousAction.Outcome}");
                
                if (previousAction.StateChanges.Any())
                {
                    contextBuilder.AppendLine();
                    contextBuilder.AppendLine("State Changes Caused:");
                    foreach (var (category, newState) in previousAction.StateChanges)
                    {
                        contextBuilder.AppendLine($"  • {category} → {newState}");
                    }
                }

                if (!string.IsNullOrEmpty(previousAction.NewSublocation))
                {
                    contextBuilder.AppendLine();
                    contextBuilder.AppendLine($"Location Changed: Now at {previousAction.NewSublocation}");
                }
                contextBuilder.AppendLine("═══════════════════════════════════════");
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

            // Generation instructions focused on follow-up actions
            contextBuilder.AppendLine($"TASK: Generate {_numberOfActions} action options that DIRECTLY BUILD UPON the previous action.");
            contextBuilder.AppendLine();
            
            // Show all possible skills that can be used
            var allUniqueSkills = _currentSkills!
                .SelectMany(sc => sc)
                .Distinct()
                .OrderBy(s => s);
            contextBuilder.AppendLine($"ALL AVAILABLE SKILLS: {string.Join(", ", allUniqueSkills)}");
            contextBuilder.AppendLine();
            
            contextBuilder.AppendLine("IMPORTANT: Each action has been assigned:");
            contextBuilder.AppendLine("- A pre-determined SUCCESS consequence (already fixed)");
            contextBuilder.AppendLine("- 5 skill candidates to choose from (LLM chooses 1)");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Skill candidates for each action:");
            for (int i = 0; i < _currentSkills!.Length; i++)
            {
                contextBuilder.AppendLine($"  Action {i + 1}: {string.Join(", ", _currentSkills[i])}");
            }
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Generate SHORT actions (3-8 words) that:");
            contextBuilder.AppendLine("1. Build upon what just happened");
            contextBuilder.AppendLine("2. Use one skill from the candidates appropriately");
            contextBuilder.AppendLine("3. Lead logically to the pre-determined consequences");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("CRITICAL REQUIREMENTS:");
            contextBuilder.AppendLine("- Actions should be logical next steps following what the player just did");
            contextBuilder.AppendLine("- Consider the immediate consequences and opportunities created by the previous action");
            contextBuilder.AppendLine("- If the previous action succeeded, suggest ways to capitalize on or extend that success");
            contextBuilder.AppendLine("- If the previous action failed, suggest alternative approaches or ways to recover");
            contextBuilder.AppendLine("- Include options that react to any state changes that occurred");
            contextBuilder.AppendLine("- Maintain narrative continuity - the player's story should flow naturally");
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Action types to consider:");
            contextBuilder.AppendLine("- Direct follow-up: Continue or complete what was started");
            contextBuilder.AppendLine("- Reactive response: Respond to what just happened");
            contextBuilder.AppendLine("- Alternative approach: Try a different method related to the same goal");
            contextBuilder.AppendLine("- Exploitation: Take advantage of new opportunities created");
            contextBuilder.AppendLine("- Investigation: Examine the results or consequences more closely");
            contextBuilder.AppendLine("- Mitigation: Deal with negative effects or complications");
            contextBuilder.AppendLine("- Pivot: Change strategy based on what was learned");
            contextBuilder.AppendLine();

            // Field reference
            contextBuilder.AppendLine(_currentHints);
            contextBuilder.AppendLine();
            contextBuilder.AppendLine("Generate a valid JSON response with the above fields. Structure your response as an object with an 'actions' array.");
            contextBuilder.AppendLine("Respond with valid JSON only, no additional text or explanations.");

            return contextBuilder.ToString();
        }
    }
}