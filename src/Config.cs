using System.Collections.Generic;
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
        
        // Black/White/Yellow theme colors
        public static readonly Vector4 DarkGray20 = new(0.2f, 0.2f, 0.2f, 1.0f);
        public static readonly Vector4 DarkGray35 = new(0.35f, 0.35f, 0.35f, 1.0f);
        public static readonly Vector4 DarkGray40 = new(0.4f, 0.4f, 0.4f, 1.0f);
        public static readonly Vector4 MediumGray50 = new(0.5f, 0.5f, 0.5f, 1.0f);
        public static readonly Vector4 MediumGray60 = new(0.6f, 0.6f, 0.6f, 1.0f);
        public static readonly Vector4 LightGray75 = new(0.75f, 0.75f, 0.75f, 1.0f);
        public static readonly Vector4 LightGray85 = new(0.85f, 0.85f, 0.85f, 1.0f);
        
        // Yellow variants for the theme
        public static readonly Vector4 DarkYellow = new(0.4f, 0.4f, 0.0f, 1.0f);       // Dark yellow background for hover
        public static readonly Vector4 MediumYellow = new(0.7f, 0.7f, 0.0f, 1.0f);     // Medium yellow
        public static readonly Vector4 BrightYellow = new(1.0f, 1.0f, 0.0f, 1.0f);     // Bright yellow for headers
        public static readonly Vector4 LightYellow = new(1.0f, 1.0f, 0.6f, 1.0f);      // Light yellow for gradients
        public static readonly Vector4 DarkYellowGrey = new(0.6f, 0.6f, 0.2f, 1.0f);   // Dark yellow-grey for important non-gameplay elements
        public static readonly Vector4 GoldYellow = new(1.0f, 0.85f, 0.2f, 1.0f);      // Gold yellow for special highlights (dice sixes)
        
        // Semi-transparent colors
        public static readonly Vector4 BlackTransparent = new(0.0f, 0.0f, 0.0f, 0.9f);
        public static readonly Vector4 DarkYellowTransparent = new(0.2f, 0.2f, 0.0f, 0.9f); // Dark yellow transparent for hover backgrounds
        
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
    
    #region Symbols
    
    /// <summary>
    /// Special character symbols used throughout the UI for consistent theming.
    /// </summary>
    public static class Symbols
    {
        // Skill level indicators
        public const char SkillLevelIndicator = '⟐';
        
        // Noetic points marker
        public const char NoeticPointMarker = '⬤';
        
        // Dice faces (6-sided dice)
        public static readonly char[] DiceFaces = new[] { '⚀', '⚁', '⚂', '⚃', '⚄', '⚅' };
        
        // Dice side views (rolling animation)
        public static readonly char[] DiceSideViews = new[] { '⬖', '⬗', '⬘', '⬙' };
        
        // Combined dice rolling frames
        public static readonly char[] DiceRollingFrames = new[] { '⚀', '⚁', '⚂', '⚃', '⚄', '⚅', '⬖', '⬗', '⬘', '⬙' };
        
        // Difficulty level glyphs (1-10)
        public static readonly char[] DifficultyGlyphs = new[] { '①', '②', '③', '④', '⑤', '⑥', '⑦', '⑧', '⑨', '⑩' };
        
        // Loading spinner frames
        public static readonly string[] LoadingSpinner = new[] { "⠋", "⠙", "⠹", "⠸", "⠼", "⠴", "⠦", "⠧", "⠇", "⠏" };
        
        // Line drawing characters
        public const char HorizontalLine = '─';
        public const char VerticalLine = '│';
    }
    
    #endregion
    
    #region Narrative UI
    
    public static class NarrativeUI
    {
        // Colors following black/white/yellow theme
        public static readonly Vector4 HeaderColor = Colors.DarkYellowGrey; // Dark yellow-grey for location title
        public static readonly Vector4 SkillHeaderColor = Colors.DarkYellowGrey; // Dark yellow-grey for skill headers
        public static readonly Vector4 NarrativeColor = Colors.MediumGray60; // Medium grey for base text (darker for better contrast)
        public static readonly Vector4 KeywordNormalColor = Colors.White; // White for interactive elements
        public static readonly Vector4 KeywordHoverColor = Colors.BrightYellow; // Yellow text on hover
        public static readonly Vector4 KeywordHoverBackgroundColor = Colors.DarkYellow; // Dark yellow background on hover
        public static readonly Vector4 ActionNormalColor = Colors.White; // White for interactive elements
        public static readonly Vector4 ActionHoverColor = Colors.BrightYellow; // Yellow text on hover
        public static readonly Vector4 ActionHoverBackgroundColor = Colors.DarkYellow; // Dark yellow background on hover
        public static readonly Vector4 ActionSkillColor = Colors.BrightYellow; // Yellow for skill brackets
        public static readonly Vector4 ReasoningColor = Colors.MediumGray50; // Medium grey for reasoning text
        public static readonly Vector4 ScrollbarTrackColor = Colors.DarkGray20; // Dark grey for scrollbar track
        public static readonly Vector4 ScrollbarThumbColor = Colors.MediumGray50; // Medium grey for scrollbar thumb
        public static readonly Vector4 ScrollbarThumbHoverColor = Colors.LightGray75; // Light grey for scrollbar thumb hover
        public static readonly Vector4 StatusBarColor = Colors.MediumGray50; // Medium grey for status (darker)
        public static readonly Vector4 BackgroundColor = Colors.Black; // Black background
        public static readonly Vector4 ErrorColor = Colors.MediumYellow; // Yellow variant for errors (fits theme)
        public static readonly Vector4 LoadingColor = Colors.BrightYellow; // Bright yellow for loading
        public static readonly Vector4 SuccessColor = Colors.White; // White for success (positive end of gradient)
        public static readonly Vector4 FailureColor = Colors.DarkYellow; // Dark yellow for failure (negative end of gradient)
        public static readonly Vector4 ContinueButtonColor = Colors.Black; // Black text for better visibility
        public static readonly Vector4 ContinueButtonBackgroundColor = Colors.LightGray85; // Light grey background for visibility over text
        public static readonly Vector4 ContinueButtonHoverColor = Colors.Black; // Black text on hover
        public static readonly Vector4 ContinueButtonHoverBackgroundColor = Colors.BrightYellow; // Yellow background on hover
        public static readonly Vector4 HistoryColor = Colors.DarkGray20; // Darker grey for history text (better contrast)
        public static readonly Vector4 SeparatorColor = Colors.DarkGray35; // Dark grey for separator lines
        public static readonly Vector4 DiceGoldColor = Colors.GoldYellow; // Gold yellow for dice sixes
        public static readonly Vector4 HintTextColor = Colors.MediumGray50; // Medium grey for hint text
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
        
        // Colors following black/white/yellow theme
        public static readonly Vector4 HeaderColor = Colors.DarkYellowGrey; // Dark yellow-grey for headers
        public static readonly Vector4 NarrativeColor = Colors.MediumGray60; // Medium grey for narrative text (darker for better contrast)
        public static readonly Vector4 ActionNormalColor = Colors.White; // White for interactive actions
        public static readonly Vector4 ActionHoverColor = Colors.BrightYellow; // Yellow on hover
        public static readonly Vector4 ActionHoverBackgroundColor = Colors.DarkYellow; // Dark yellow background on hover
        public static readonly Vector4 StatusBarColor = Colors.MediumGray60; // Medium grey for status
        public static readonly Vector4 BackgroundColor = Colors.Black; // Black background
        public static readonly Vector4 SuccessColor = Colors.White; // White for success messages
        public static readonly Vector4 FailureColor = Colors.DarkYellow; // Dark yellow for failure messages
    }
    
    #endregion
    
    #region Thinking Skill Popup
    
    public static class ThinkingSkillPopup
    {
        public static readonly Vector4 SkillNormalColor = Colors.White; // White for interactive skills
        public static readonly Vector4 SkillHoverColor = Colors.BrightYellow; // Yellow text on hover
        public static readonly Vector4 SkillHoverBackgroundColor = Colors.DarkYellow; // Dark yellow background on hover
        public static readonly Vector4 BackgroundColor = new(0.0f, 0.0f, 0.0f, 0.9f); // Semi-transparent black
        public static readonly Vector4 TransparentColor = new(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    #endregion
    
    #region Loading Messages
    
    public static class LoadingMessages
    {
        // General
        public const string Default = "Loading...";
        
        // Phase 6 - Observation/Narration
        public const string GeneratingObservations = "Observing surroundings...";
        public const string ThinkingDeeply = "Thinking about what to do...";
        public const string EvaluatingAction = "Taking action...";
        
        // Location Travel
        public const string Thinking = "Thinking...";
        public const string EvaluatingDifficulty = "Evaluating action difficulty...";
        public const string DeterminingOutcome = "Determining outcome...";
        public const string NarratingDemise = "Narrating your demise...";
        public const string GeneratingActions = "Generating actions...";
        public const string GeneratingNarrative = "Generating narrative...";
    }
    
    #endregion
    
    #region Glyph Size Factors
    
    /// <summary>
    /// Per-glyph font size multipliers for special characters that need different sizing.
    /// Glyphs not in this dictionary use 1.0 (normal size).
    /// </summary>
    public static class GlyphSizeFactors
    {
        public static readonly Dictionary<char, float> Factors = new()
        {
            // Dice faces - make them 30% larger
            { '⚀', 2f },
            { '⚁', 2f },
            { '⚂', 2f },
            { '⚃', 2f },
            { '⚄', 2f },
            { '⚅', 2f },
            
            // Dice rolling animation glyphs
            { '⬖', 1.7f },
            { '⬗', 1.7f },
            { '⬘', 1.7f },
            { '⬙', 1.7f },
            
            // Difficulty indicators - slightly larger
            { '①', 1.3f },
            { '②', 1.3f },
            { '③', 1.3f },
            { '④', 1.3f },
            { '⑤', 1.3f },
            { '⑥', 1.3f },
            { '⑦', 1.3f },
            { '⑧', 1.3f },
            { '⑨', 1.3f },
            { '⑩', 1.3f },
        };
        
        /// <summary>
        /// Gets the size factor for a glyph. Returns 1.0 for normal-sized glyphs.
        /// </summary>
        public static float GetFactor(char c)
        {
            return Factors.TryGetValue(c, out float factor) ? factor : 1.0f;
        }
    }
    
    #endregion
}
