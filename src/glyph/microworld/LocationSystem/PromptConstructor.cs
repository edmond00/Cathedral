using Cathedral.Glyph.Microworld.LocationSystem;
using Cathedral.LLM.JsonConstraints;
using Cathedral.Game;

namespace Cathedral.Glyph.Microworld.LocationSystem
{
    /// <summary>
    /// Represents an action taken by the player and its outcome
    /// </summary>
    public class PlayerAction
    {
        public string ActionText { get; set; } = "";
        public string Outcome { get; set; } = "";
        public bool WasSuccessful { get; set; }
        public Dictionary<string, string> StateChanges { get; set; } = new();
        public string? NewSublocation { get; set; }
        public string? ItemGained { get; set; }
        public string? CompanionGained { get; set; }
        public string? QuestGained { get; set; }
    }

    /// <summary>
    /// Base class for constructing prompts for different LLM roles in the location system
    /// </summary>
    public abstract class PromptConstructor
    {
        protected LocationBlueprint Blueprint { get; }
        protected string CurrentSublocation { get; set; }
        protected Dictionary<string, string> CurrentStates { get; set; }

        protected PromptConstructor(LocationBlueprint blueprint, string currentSublocation, Dictionary<string, string> currentStates)
        {
            Blueprint = blueprint;
            CurrentSublocation = currentSublocation;
            CurrentStates = new Dictionary<string, string>(currentStates);
        }

        /// <summary>
        /// Updates the current game state
        /// </summary>
        public virtual void UpdateGameState(string newSublocation, Dictionary<string, string> newStates)
        {
            CurrentSublocation = newSublocation;
            CurrentStates = new Dictionary<string, string>(newStates);
        }

        /// <summary>
        /// Constructs the system prompt for this LLM role
        /// </summary>
        public abstract string GetSystemPrompt();

        /// <summary>
        /// Constructs the user prompt for the current situation
        /// </summary>
        public abstract string ConstructPrompt(PlayerAction? previousAction = null, List<ActionInfo>? availableActions = null);
    }
}