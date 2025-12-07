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
            return @"You are the DIRECTOR of a fantasy RPG game. You generate structured action options based on game state.

GENERATION PROCESS:
For each action, work in this order:
1. Consider the success consequence
2. Choose the most appropriate skill from candidates that could lead to this consequence
3. Write a specific, concrete action that uses this skill to achieve the consequence

Action Text Guidelines:
- Straightforward and direct (6-12 words)
- Purely mechanical - describe what the player DOES
- Be SPECIFIC and CONCRETE - avoid abstract or overly general actions
- NO atmospheric descriptions, flavor text, or narrative elements (that's the Narrator's job)
- Focus on the concrete action being attempted
- Ensure the action makes sense in the given location and environmental conditions

Generate diverse action types considering different approaches. Output only valid JSON in the specified format.";
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
            contextBuilder.AppendLine($"LOCATION: {currentSublocationData.Name} ({Blueprint.LocationType})");
            contextBuilder.AppendLine($"{currentSublocationData.Description}");
            contextBuilder.AppendLine();

            // Environmental states
            contextBuilder.AppendLine("CONDITIONS:");
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

            // Show all possible skills that can be used
            var allUniqueSkills = _currentSkills!
                .SelectMany(sc => sc)
                .Distinct()
                .OrderBy(s => s);
            contextBuilder.AppendLine($"AVAILABLE SKILLS: {string.Join(", ", allUniqueSkills)}");
            contextBuilder.AppendLine();
            
            // Core task
            contextBuilder.AppendLine("TASK: Generate diverse actions that are coherent with their assigned skills and consequences.");
            contextBuilder.AppendLine("Consider different approaches based on the situation.");
            contextBuilder.AppendLine();

            // Field reference
            contextBuilder.AppendLine(_currentHints);

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
            contextBuilder.AppendLine($"LOCATION: {currentSublocationData.Name} ({Blueprint.LocationType})");
            contextBuilder.AppendLine($"{currentSublocationData.Description}");
            contextBuilder.AppendLine();

            // Environmental states
            contextBuilder.AppendLine("CONDITIONS:");
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
                contextBuilder.AppendLine("PREVIOUS ACTION:");
                contextBuilder.AppendLine($"Action: {previousAction.ActionText}");
                contextBuilder.AppendLine($"Outcome: {(previousAction.WasSuccessful ? "SUCCESS" : "FAILURE")} - {previousAction.Outcome}");
                
                if (previousAction.StateChanges.Any())
                {
                    contextBuilder.AppendLine($"State Changes: {string.Join(", ", previousAction.StateChanges.Select(sc => $"{sc.Key} → {sc.Value}"))}");
                }

                if (!string.IsNullOrEmpty(previousAction.NewSublocation))
                {
                    contextBuilder.AppendLine($"Moved to: {previousAction.NewSublocation}");
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

            // Show all possible skills that can be used
            var allUniqueSkills = _currentSkills!
                .SelectMany(sc => sc)
                .Distinct()
                .OrderBy(s => s);
            contextBuilder.AppendLine($"AVAILABLE SKILLS: {string.Join(", ", allUniqueSkills)}");
            contextBuilder.AppendLine();
            
            // Core task
            contextBuilder.AppendLine("TASK: Generate actions that DIRECTLY BUILD UPON the previous action.");
            contextBuilder.AppendLine("Actions must be coherent with their assigned skills and consequences.");
            contextBuilder.AppendLine("Consider approaches that respond to what just happened (follow-up, react, capitalize, recover, investigate, pivot).");
            contextBuilder.AppendLine();

            // Field reference
            contextBuilder.AppendLine(_currentHints);

            return contextBuilder.ToString();
        }
    }
}