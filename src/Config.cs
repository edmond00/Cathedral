using OpenTK.Mathematics;

namespace Cathedral;

/// <summary>
/// Centralized configuration for the entire application.
/// Contains all UI settings, colors, dimensions, and layout constants.
/// </summary>
public static class Config
{
    #region Terminal Configuration
    
    public static class Terminal
    {
        // Main terminal dimensions
        public const int MainWidth = 100;
        public const int MainHeight = 30;
        public const int MainCellSize = 20;
        public const int MainFontSize = 22;
        
        // Popup terminal dimensions
        public const int PopupWidth = 40;
        public const int PopupHeight = 40;
        public const int PopupCellSize = 16;
    }
    
    #endregion
    
    #region Base Colors
    
    public static class Colors
    {
        // Basic colors
        public static readonly Vector4 Black = new(0.0f, 0.0f, 0.0f, 1.0f);
        public static readonly Vector4 White = new(1.0f, 1.0f, 1.0f, 1.0f);
        public static readonly Vector4 Red = new(1.0f, 0.0f, 0.0f, 1.0f);
        public static readonly Vector4 Green = new(0.0f, 1.0f, 0.0f, 1.0f);
        public static readonly Vector4 Blue = new(0.0f, 0.0f, 1.0f, 1.0f);
        public static readonly Vector4 Yellow = new(1.0f, 1.0f, 0.0f, 1.0f);
        public static readonly Vector4 Magenta = new(1.0f, 0.0f, 1.0f, 1.0f);
        public static readonly Vector4 Cyan = new(0.0f, 1.0f, 1.0f, 1.0f);
        public static readonly Vector4 Gray = new(0.5f, 0.5f, 0.5f, 1.0f);
        public static readonly Vector4 DarkGray = new(0.3f, 0.3f, 0.3f, 1.0f);
        public static readonly Vector4 LightGray = new(0.7f, 0.7f, 0.7f, 1.0f);
        public static readonly Vector4 Transparent = new(0.0f, 0.0f, 0.0f, 0.0f);
        
        // Extended colors
        public static readonly Vector4 LightCyan = new(0.5f, 0.9f, 1.0f, 1.0f);
        public static readonly Vector4 LightGreen = new(0.5f, 1.0f, 0.5f, 1.0f);
        public static readonly Vector4 BrightGreen = new(0.4f, 1.0f, 0.4f, 1.0f);
        public static readonly Vector4 BrightRed = new(1.0f, 0.4f, 0.4f, 1.0f);
        public static readonly Vector4 OrangeYellow = new(1.0f, 0.8f, 0.0f, 1.0f);
        public static readonly Vector4 LightPurpleGray = new(0.8f, 0.8f, 0.9f, 1.0f);
        public static readonly Vector4 Gray70 = new(0.7f, 0.7f, 0.7f, 1.0f);
        public static readonly Vector4 Gray90 = new(0.9f, 0.9f, 0.9f, 1.0f);
        
        // Semi-transparent colors
        public static readonly Vector4 BlackTransparent = new(0.0f, 0.0f, 0.0f, 0.9f);
        
        // Terminal 16-color palette
        public static readonly Vector4[] Terminal = new Vector4[]
        {
            Black,      // 0: Black
            Red,        // 1: Red
            Green,      // 2: Green
            Yellow,     // 3: Yellow
            Blue,       // 4: Blue
            Magenta,    // 5: Magenta
            Cyan,       // 6: Cyan
            White,      // 7: White
            DarkGray,   // 8: Bright Black (Dark Gray)
            new Vector4(1.0f, 0.5f, 0.5f, 1.0f),  // 9: Bright Red
            new Vector4(0.5f, 1.0f, 0.5f, 1.0f),  // 10: Bright Green
            new Vector4(1.0f, 1.0f, 0.5f, 1.0f),  // 11: Bright Yellow
            new Vector4(0.5f, 0.5f, 1.0f, 1.0f),  // 12: Bright Blue
            new Vector4(1.0f, 0.5f, 1.0f, 1.0f),  // 13: Bright Magenta
            new Vector4(0.5f, 1.0f, 1.0f, 1.0f),  // 14: Bright Cyan
            new Vector4(0.9f, 0.9f, 0.9f, 1.0f),  // 15: Bright White
        };
    }
    
    #endregion
    
    #region Phase 6 Observation UI
    
    public static class Phase6UI
    {
        // Colors
        public static readonly Vector4 HeaderColor = new(0.0f, 0.8f, 1.0f, 1.0f); // Cyan
        public static readonly Vector4 SkillHeaderColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        public static readonly Vector4 NarrativeColor = new(0.7f, 0.7f, 0.7f, 1.0f); // Gray70
        public static readonly Vector4 KeywordNormalColor = new(0.5f, 0.9f, 1.0f, 1.0f); // Light cyan
        public static readonly Vector4 KeywordHoverColor = new(1.0f, 1.0f, 1.0f, 1.0f); // White
        public static readonly Vector4 ActionNormalColor = new(1.0f, 1.0f, 1.0f, 1.0f); // White
        public static readonly Vector4 ActionHoverColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        public static readonly Vector4 ActionSkillColor = new(0.5f, 1.0f, 0.5f, 1.0f); // Light green
        public static readonly Vector4 ReasoningColor = new(0.8f, 0.8f, 0.9f, 1.0f); // Light purple-gray
        public static readonly Vector4 ScrollbarTrackColor = new(0.3f, 0.3f, 0.3f, 1.0f); // Dark gray
        public static readonly Vector4 ScrollbarThumbColor = new(0.6f, 0.6f, 0.6f, 1.0f); // Medium gray
        public static readonly Vector4 ScrollbarThumbHoverColor = new(0.8f, 0.8f, 0.8f, 1.0f); // Light gray
        public static readonly Vector4 StatusBarColor = new(0.5f, 0.5f, 0.5f, 1.0f); // Gray
        public static readonly Vector4 BackgroundColor = new(0.0f, 0.0f, 0.0f, 1.0f); // Black
        public static readonly Vector4 ErrorColor = new(1.0f, 0.3f, 0.3f, 1.0f); // Red
        public static readonly Vector4 LoadingColor = new(0.8f, 0.8f, 0.0f, 1.0f); // Yellow
        public static readonly Vector4 SuccessColor = new(0.4f, 1.0f, 0.4f, 1.0f); // Bright green
        public static readonly Vector4 FailureColor = new(1.0f, 0.4f, 0.4f, 1.0f); // Bright red
        public static readonly Vector4 ContinueButtonColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        public static readonly Vector4 ContinueButtonHoverColor = new(1.0f, 0.8f, 0.0f, 1.0f); // Orange-yellow
    }
    
    #endregion
    
    #region Location UI
    
    public static class LocationUI
    {
        // Terminal dimensions
        public const int TerminalWidth = 100;
        public const int TerminalHeight = 30;
        
        // Layout constants
        public const int HeaderHeight = 3;
        public const int StatusBarHeight = 1;
        public const int ActionMenuStartY = 18;
        public const int NarrativeStartY = HeaderHeight + 1;
        public const int NarrativeHeight = ActionMenuStartY - NarrativeStartY - 1;
        
        // Colors
        public static readonly Vector4 HeaderColor = new(0.0f, 0.8f, 1.0f, 1.0f); // Cyan
        public static readonly Vector4 NarrativeColor = new(0.9f, 0.9f, 0.9f, 1.0f); // Light gray
        public static readonly Vector4 ActionNormalColor = new(1.0f, 1.0f, 1.0f, 1.0f); // White
        public static readonly Vector4 ActionHoverColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        public static readonly Vector4 StatusBarColor = new(0.5f, 0.5f, 0.5f, 1.0f); // Gray
        public static readonly Vector4 BackgroundColor = new(0.0f, 0.0f, 0.0f, 1.0f); // Black
    }
    
    #endregion
    
    #region Thinking Skill Popup
    
    public static class ThinkingSkillPopup
    {
        public static readonly Vector4 SkillNormalColor = new(0.9f, 0.9f, 0.9f, 1.0f); // Light gray
        public static readonly Vector4 SkillHoverColor = new(1.0f, 1.0f, 0.0f, 1.0f); // Yellow
        public static readonly Vector4 BackgroundColor = new(0.0f, 0.0f, 0.0f, 0.9f); // Semi-transparent black
        public static readonly Vector4 TransparentColor = new(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    #endregion
}
