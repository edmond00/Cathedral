namespace Cathedral.Game.Dialogue;

/// <summary>
/// Layout constants for the Dialogue UI.
/// Mirrors the structure of NarrativeLayout but tuned for the two-pane dialogue design:
///   • Top header row (NPC name + affinity bar + subject label)
///   • Scrollable content area
///   • Bottom status bar
/// </summary>
public class DialogueLayout
{
    public int TerminalWidth  { get; }
    public int TerminalHeight { get; }

    // Header (row 0-1)
    public int HeaderHeight  { get; } = 2;
    public int HeaderY       { get; } = 0;

    // Status bar (last row)
    public int StatusBarY    { get; }
    public int StatusBarHeight { get; } = 1;

    // Separator above status bar
    public int SeparatorY    { get; }

    // Scrollable content area
    public int ContentStartY { get; }
    public int ContentEndY   { get; }
    public int ContentHeight { get; }

    // Horizontal margins
    public int LeftMargin    { get; } = 2;
    public int RightMargin   { get; } = 3; // +1 for scrollbar
    public int ContentStartX { get; }
    public int ContentEndX   { get; }
    public int ContentWidth  { get; }

    // Scrollbar column
    public int ScrollbarX    { get; }

    // Dice roll area (centered)
    public int DiceAreaY     { get; }
    public int DiceAreaHeight { get; } = 6;

    public DialogueLayout(int terminalWidth, int terminalHeight)
    {
        TerminalWidth   = terminalWidth;
        TerminalHeight  = terminalHeight;

        StatusBarY      = terminalHeight - 1;
        SeparatorY      = StatusBarY - 1;

        ContentStartY   = HeaderHeight;
        ContentEndY     = SeparatorY - 1;
        ContentHeight   = ContentEndY - ContentStartY + 1;

        ContentStartX   = LeftMargin;
        ScrollbarX      = terminalWidth - RightMargin;
        ContentEndX     = ScrollbarX - 1;
        ContentWidth    = ContentEndX - ContentStartX + 1;

        DiceAreaY       = ContentStartY + ContentHeight / 2 - DiceAreaHeight / 2;
    }
}
