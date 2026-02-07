namespace Cathedral.Game;

/// <summary>
/// Dynamic layout calculator for Narrative UI that adapts to terminal dimensions.
/// Calculates all layout values based on the provided terminal size.
/// </summary>
public class NarrativeLayout
{
    // Terminal dimensions
    public int TERMINAL_WIDTH { get; }
    public int TERMINAL_HEIGHT { get; }
    
    // Header section
    public int HEADER_HEIGHT { get; }
    
    // Status bar section
    public int STATUS_BAR_HEIGHT { get; }
    public int STATUS_BAR_Y { get; }
    
    // Separator section
    public int SEPARATOR_HEIGHT { get; }
    public int SEPARATOR_Y { get; }
    
    // Content area (scrollable narrative)
    public int CONTENT_START_Y { get; }
    public int CONTENT_END_Y { get; }
    public int NARRATIVE_HEIGHT { get; }
    
    // Margins
    public int LEFT_MARGIN { get; }
    public int RIGHT_MARGIN { get; }
    public int CONTENT_WIDTH { get; }
    
    // Scrollbar
    public int SCROLLBAR_TRACK_HEIGHT { get; }
    public int SCROLLBAR_THUMB_MIN_HEIGHT { get; }
    
    // Scroll behavior
    public int SCROLL_BOTTOM_MARGIN { get; }
    
    // Padding
    public int TOP_PADDING { get; }
    public int BOTTOM_PADDING { get; }
    public int LEFT_PADDING { get; }
    public int RIGHT_PADDING { get; }
    
    // Computed content positions (accounting for padding)
    public int CONTENT_START_X => LEFT_PADDING + LEFT_MARGIN;
    public int CONTENT_END_X => TERMINAL_WIDTH - RIGHT_PADDING - RIGHT_MARGIN;
    
    /// <summary>
    /// Creates a dynamic layout calculator based on terminal dimensions.
    /// </summary>
    public NarrativeLayout(int terminalWidth, int terminalHeight, int topPadding = 0, int bottomPadding = 0, int leftPadding = 0, int rightPadding = 0)
    {
        TERMINAL_WIDTH = terminalWidth;
        TERMINAL_HEIGHT = terminalHeight;
        TOP_PADDING = topPadding;
        BOTTOM_PADDING = bottomPadding;
        LEFT_PADDING = leftPadding;
        RIGHT_PADDING = rightPadding;
        
        // Calculate proportional layout values with padding
        HEADER_HEIGHT = 2;
        STATUS_BAR_HEIGHT = 1;
        SEPARATOR_HEIGHT = 1;
        
        // Status bar pushed up by bottom padding
        STATUS_BAR_Y = TERMINAL_HEIGHT - BOTTOM_PADDING - STATUS_BAR_HEIGHT;
        SEPARATOR_Y = STATUS_BAR_Y - SEPARATOR_HEIGHT;
        
        // Content starts after top padding and header
        CONTENT_START_Y = TOP_PADDING + HEADER_HEIGHT;
        CONTENT_END_Y = SEPARATOR_Y - 1;
        NARRATIVE_HEIGHT = CONTENT_END_Y - CONTENT_START_Y + 1;
        
        // Margins are internal spacing (constant, not affected by padding)
        LEFT_MARGIN = 4;
        RIGHT_MARGIN = 4;
        // Content width is terminal width minus padding zones and internal margins
        CONTENT_WIDTH = TERMINAL_WIDTH - LEFT_PADDING - RIGHT_PADDING - LEFT_MARGIN - RIGHT_MARGIN;
        
        SCROLLBAR_TRACK_HEIGHT = NARRATIVE_HEIGHT;
        SCROLLBAR_THUMB_MIN_HEIGHT = 3;
        SCROLL_BOTTOM_MARGIN = 5;
    }
    
    /// <summary>
    /// Calculate the maximum scroll offset for a given total line count.
    /// </summary>
    public int CalculateMaxScrollOffset(int totalLines)
    {
        return Math.Max(0, totalLines - NARRATIVE_HEIGHT + SCROLL_BOTTOM_MARGIN);
    }
}
