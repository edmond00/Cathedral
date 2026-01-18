using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Manages the state for Chain-of-Thought narration system.
/// Tracks narration blocks, scroll position, keyword regions, and loading state.
/// </summary>
public class NarrativeState
{
    /// <summary>
    /// All narration blocks in order (Observation, Thinking, Action, Outcome).
    /// </summary>
    public List<NarrationBlock> Blocks { get; private set; } = new();

    /// <summary>
    /// Current scroll offset (0 = top, increases scrolling down).
    /// </summary>
    public int ScrollOffset { get; set; } = 0;

    /// <summary>
    /// Clickable keyword regions for current view.
    /// </summary>
    public List<KeywordRegion> KeywordRegions { get; set; } = new();

    /// <summary>
    /// Is the system currently generating observations via LLM?
    /// </summary>
    public bool IsLoadingObservations { get; set; } = false;

    /// <summary>
    /// Is the system currently generating thinking/actions via LLM?
    /// </summary>
    public bool IsLoadingThinking { get; set; } = false;

    /// <summary>
    /// Is the system currently executing an action (skill check + outcome)?
    /// </summary>
    public bool IsLoadingAction { get; set; } = false;

    /// <summary>
    /// Current loading message to display.
    /// </summary>
    public string LoadingMessage { get; set; } = "Loading...";

    /// <summary>
    /// Currently hovered keyword region (null if none).
    /// </summary>
    public KeywordRegion? HoveredKeyword { get; set; } = null;

    /// <summary>
    /// Currently hovered action region (null if none).
    /// </summary>
    public ActionRegion? HoveredAction { get; set; } = null;

    /// <summary>
    /// Clickable action regions for current view.
    /// </summary>
    public List<ActionRegion> ActionRegions { get; set; } = new();

    /// <summary>
    /// Is the scrollbar currently being dragged?
    /// </summary>
    public bool IsScrollbarDragging { get; set; } = false;

    /// <summary>
    /// Mouse Y position when scrollbar drag started.
    /// </summary>
    public int ScrollbarDragStartY { get; set; } = 0;

    /// <summary>
    /// Scroll offset when scrollbar drag started.
    /// </summary>
    public int ScrollbarDragStartOffset { get; set; } = 0;

    /// <summary>
    /// Current scrollbar thumb position and size (Y, Height).
    /// </summary>
    public (int StartY, int Height) ScrollbarThumb { get; set; } = (0, 0);

    /// <summary>
    /// Is mouse hovering over scrollbar thumb?
    /// </summary>
    public bool IsScrollbarThumbHovered { get; set; } = false;

    /// <summary>
    /// Thinking attempts remaining (starts at 3, decrements on keyword click).
    /// </summary>
    public int ThinkingAttemptsRemaining { get; set; } = 3;

    /// <summary>
    /// Should the "Continue" button be shown?
    /// </summary>
    public bool ShowContinueButton { get; set; } = false;

    /// <summary>
    /// Is the continue button currently hovered?
    /// </summary>
    public bool IsContinueButtonHovered { get; set; } = false;
    
    // --- Dice Roll State ---
    
    /// <summary>
    /// Is the dice roll loading screen currently active?
    /// </summary>
    public bool IsDiceRollActive { get; set; } = false;
    
    /// <summary>
    /// Number of dice being rolled.
    /// </summary>
    public int DiceRollNumberOfDice { get; set; } = 0;
    
    /// <summary>
    /// Difficulty for the dice roll (number of 6s needed to succeed).
    /// </summary>
    public int DiceRollDifficulty { get; set; } = 1;
    
    /// <summary>
    /// Is the dice roll still in progress (rolling animation)?
    /// </summary>
    public bool IsDiceRolling { get; set; } = true;
    
    /// <summary>
    /// Final dice values after roll completes (null while rolling).
    /// </summary>
    public int[]? DiceRollFinalValues { get; set; } = null;
    
    /// <summary>
    /// Whether the dice roll succeeded (enough 6s rolled).
    /// </summary>
    public bool DiceRollSucceeded { get; set; } = false;
    
    /// <summary>
    /// Is the dice roll continue button currently hovered?
    /// </summary>
    public bool IsDiceRollButtonHovered { get; set; } = false;
    
    /// <summary>
    /// Is the skill popup being shown for focus observation (right-click) rather than thinking (left-click)?
    /// </summary>
    public bool IsSelectingObservationSkill { get; set; } = false;
    
    /// <summary>
    /// Is the system currently generating a focus observation via LLM?
    /// </summary>
    public bool IsLoadingFocusObservation { get; set; } = false;
    
    /// <summary>
    /// Pending narration node to transition to after continue button is clicked.
    /// Null means no transition (stay in current node or exit).
    /// </summary>
    public NarrationNode? PendingTransitionNode { get; set; } = null;

    /// <summary>
    /// Has the player requested to exit Phase 6 (via continue button)?
    /// </summary>
    public bool RequestedExit { get; set; } = false;

    /// <summary>
    /// Error message if something went wrong (null if no error).
    /// </summary>
    public string? ErrorMessage { get; set; } = null;

    /// <summary>
    /// Add a new narration block to the history.
    /// </summary>
    public void AddBlock(NarrationBlock block)
    {
        Blocks.Add(block);
    }

    /// <summary>
    /// Clear all blocks and reset state.
    /// </summary>
    public void Clear()
    {
        Blocks.Clear();
        ScrollOffset = 0;
        KeywordRegions.Clear();
        IsLoadingObservations = false;
        IsLoadingThinking = false;
        IsLoadingAction = false;
        IsLoadingFocusObservation = false;
        LoadingMessage = Config.LoadingMessages.Default;
        HoveredKeyword = null;
        ThinkingAttemptsRemaining = 3;
        ShowContinueButton = false;
        IsContinueButtonHovered = false;
        IsSelectingObservationSkill = false;
        PendingTransitionNode = null;
        ErrorMessage = null;
        ClearDiceRoll();
    }
    
    /// <summary>
    /// Reset state for a new narration node without clearing blocks.
    /// Used when transitioning to preserve history.
    /// </summary>
    public void ResetForNewNode()
    {
        Blocks.Clear();  // Clear current node's blocks (history is in scroll buffer)
        KeywordRegions.Clear();
        IsLoadingObservations = false;
        IsLoadingThinking = false;
        IsLoadingAction = false;
        IsLoadingFocusObservation = false;
        LoadingMessage = Config.LoadingMessages.Default;
        HoveredKeyword = null;
        HoveredAction = null;
        ActionRegions.Clear();
        ThinkingAttemptsRemaining = 3;
        ShowContinueButton = false;
        IsContinueButtonHovered = false;
        IsSelectingObservationSkill = false;
        PendingTransitionNode = null;
        ErrorMessage = null;
        ClearDiceRoll();
        // Note: ScrollOffset is NOT reset - it's managed by the scroll buffer
    }
    
    /// <summary>
    /// Start a dice roll animation.
    /// </summary>
    /// <param name="numberOfDice">Number of dice to roll</param>
    /// <param name="difficulty">Number of 6s needed to succeed (1-10)</param>
    public void StartDiceRoll(int numberOfDice, int difficulty)
    {
        IsDiceRollActive = true;
        DiceRollNumberOfDice = numberOfDice;
        DiceRollDifficulty = Math.Clamp(difficulty, 1, 10);
        IsDiceRolling = true;
        DiceRollFinalValues = null;
        DiceRollSucceeded = false;
        IsDiceRollButtonHovered = false;
    }
    
    /// <summary>
    /// Complete a dice roll with final values.
    /// </summary>
    /// <param name="finalValues">Array of final dice values (1-6)</param>
    public void CompleteDiceRoll(int[] finalValues)
    {
        IsDiceRolling = false;
        DiceRollFinalValues = finalValues;
        int numberOfSixes = finalValues.Count(v => v == 6);
        DiceRollSucceeded = numberOfSixes >= DiceRollDifficulty;
    }
    
    /// <summary>
    /// Clear dice roll state.
    /// </summary>
    public void ClearDiceRoll()
    {
        IsDiceRollActive = false;
        DiceRollNumberOfDice = 0;
        DiceRollDifficulty = 1;
        IsDiceRolling = true;
        DiceRollFinalValues = null;
        DiceRollSucceeded = false;
        IsDiceRollButtonHovered = false;
    }

    /// <summary>
    /// Get all keywords from all observation blocks.
    /// </summary>
    public List<string> GetAllKeywords()
    {
        var keywords = new List<string>();
        foreach (var block in Blocks)
        {
            if (block.Type == NarrationBlockType.Observation && block.Keywords != null)
            {
                keywords.AddRange(block.Keywords);
            }
        }
        return keywords;
    }
}

/// <summary>
/// Interface for all clickable regions in the terminal.
/// </summary>
public interface IClickableRegion
{
    /// <summary>
    /// Starting Y coordinate of the clickable region.
    /// </summary>
    int StartY { get; }
    
    /// <summary>
    /// Ending Y coordinate of the clickable region (inclusive).
    /// </summary>
    int EndY { get; }
    
    /// <summary>
    /// Starting X coordinate of the clickable region.
    /// </summary>
    int StartX { get; }
    
    /// <summary>
    /// Ending X coordinate of the clickable region (inclusive).
    /// </summary>
    int EndX { get; }
}

/// <summary>
/// Extension methods for IClickableRegion interface.
/// </summary>
public static class ClickableRegionExtensions
{
    /// <summary>
    /// Check if the given coordinates are within this clickable region.
    /// </summary>
    /// <param name="region">The clickable region</param>
    /// <param name="x">X coordinate to check</param>
    /// <param name="y">Y coordinate to check</param>
    /// <returns>True if the coordinates are within the region</returns>
    public static bool Contains(this IClickableRegion region, int x, int y)
    {
        return x >= region.StartX && x <= region.EndX && y >= region.StartY && y <= region.EndY;
    }
}

/// <summary>
/// Represents a clickable keyword region in the terminal.
/// </summary>
public record KeywordRegion(string Keyword, int Y, int StartX, int EndX) : IClickableRegion
{
    /// <summary>
    /// Starting Y coordinate (same as Y for single-line regions).
    /// </summary>
    public int StartY => Y;
    
    /// <summary>
    /// Ending Y coordinate (same as Y for single-line regions).
    /// </summary>
    public int EndY => Y;
}

/// <summary>
/// Represents a clickable action region in the terminal.
/// </summary>
public record ActionRegion(int ActionIndex, int StartY, int EndY, int StartX, int EndX) : IClickableRegion;
