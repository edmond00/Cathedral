namespace Cathedral.Game;

/// <summary>
/// Centralized layout constants for Narrative UI to prevent duplication and ensure consistency.
/// </summary>
public static class NarrativeLayout
{
    // Terminal dimensions
    public const int TERMINAL_WIDTH = 100;
    public const int TERMINAL_HEIGHT = 30;
    
    // Header section
    public const int HEADER_HEIGHT = 2;
    
    // Status bar section
    public const int STATUS_BAR_HEIGHT = 1;
    public const int STATUS_BAR_Y = TERMINAL_HEIGHT - STATUS_BAR_HEIGHT;
    
    // Separator section
    public const int SEPARATOR_HEIGHT = 1;
    public const int SEPARATOR_Y = STATUS_BAR_Y - SEPARATOR_HEIGHT;
    
    // Content area (scrollable narrative)
    public const int CONTENT_START_Y = HEADER_HEIGHT;
    public const int CONTENT_END_Y = SEPARATOR_Y - 1;
    public const int NARRATIVE_HEIGHT = CONTENT_END_Y - CONTENT_START_Y + 1; // 26 lines
    
    // Margins
    public const int LEFT_MARGIN = 4;
    public const int RIGHT_MARGIN = 4;
    public const int CONTENT_WIDTH = TERMINAL_WIDTH - LEFT_MARGIN - RIGHT_MARGIN;
    
    // Scrollbar
    public const int SCROLLBAR_TRACK_HEIGHT = NARRATIVE_HEIGHT; // 26 cells for track (Y=2 to Y=27)
    public const int SCROLLBAR_THUMB_MIN_HEIGHT = 3;
    
    // Scroll behavior
    public const int SCROLL_BOTTOM_MARGIN = 5; // Extra lines for comfortable scrolling at bottom
    
    /// <summary>
    /// Calculate the maximum scroll offset for a given total line count.
    /// </summary>
    public static int CalculateMaxScrollOffset(int totalLines)
    {
        return Math.Max(0, totalLines - NARRATIVE_HEIGHT + SCROLL_BOTTOM_MARGIN);
    }
}
