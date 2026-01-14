using System.Collections.Generic;
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
        LoadingMessage = Config.LoadingMessages.Default;
        HoveredKeyword = null;
        ThinkingAttemptsRemaining = 3;
        ShowContinueButton = false;
        IsContinueButtonHovered = false;
        PendingTransitionNode = null;
        ErrorMessage = null;
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
        LoadingMessage = Config.LoadingMessages.Default;
        HoveredKeyword = null;
        HoveredAction = null;
        ActionRegions.Clear();
        ThinkingAttemptsRemaining = 3;
        ShowContinueButton = false;
        IsContinueButtonHovered = false;
        PendingTransitionNode = null;
        ErrorMessage = null;
        // Note: ScrollOffset is NOT reset - it's managed by the scroll buffer
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
/// Represents a clickable keyword region in the terminal.
/// </summary>
public record KeywordRegion(string Keyword, int Y, int StartX, int EndX);

/// <summary>
/// Represents a clickable action region in the terminal.
/// </summary>
public record ActionRegion(int ActionIndex, int StartY, int EndY, int StartX, int EndX);
