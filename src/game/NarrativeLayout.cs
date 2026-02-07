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
    
    /// <summary>
    /// Creates a dynamic layout calculator based on terminal dimensions.
    /// </summary>
    public NarrativeLayout(int terminalWidth, int terminalHeight)
    {
        TERMINAL_WIDTH = terminalWidth;
        TERMINAL_HEIGHT = terminalHeight;
        
        // Calculate proportional layout values
        HEADER_HEIGHT = 2;
        STATUS_BAR_HEIGHT = 1;
        SEPARATOR_HEIGHT = 1;
        
        STATUS_BAR_Y = TERMINAL_HEIGHT - STATUS_BAR_HEIGHT;
        SEPARATOR_Y = STATUS_BAR_Y - SEPARATOR_HEIGHT;
        
        CONTENT_START_Y = HEADER_HEIGHT;
        CONTENT_END_Y = SEPARATOR_Y - 1;
        NARRATIVE_HEIGHT = CONTENT_END_Y - CONTENT_START_Y + 1;
        
        LEFT_MARGIN = 4;
        RIGHT_MARGIN = 4;
        CONTENT_WIDTH = TERMINAL_WIDTH - LEFT_MARGIN - RIGHT_MARGIN;
        
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
