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
        // Font configuration
        public const string FontPath = "assets/fonts/FreeMono.ttf";
        public const string FallbackFontPath = "assets/fonts/DejaVuSansMono.ttf";
        
        /// <summary>
        /// Characters that should use the fallback font instead of the main font.
        /// Add any special characters here that don't render properly in FreeMono.
        /// </summary>
        public static readonly HashSet<char> FallbackGlyphs = new HashSet<char>
        {
            // Add characters here that need fallback font
            // Example: '█', '▓', '▒', '░', etc.
            '♞', '⚓'
        };
        
        // Main terminal dimensions
        public const int MainWidth = 100;
        public const int MainHeight = 100;
        public const int MainCellSize = 35;
        public const int MainFontSize = 35;
        
        // Popup terminal dimensions
        public const int PopupWidth = 40;
        public const int PopupHeight = 40;
        public const int PopupCellSize = MainCellSize;

        // Glyph scale relative to cell (1.0 = exact fit, >1.0 = slight overflow for natural look)
        public const float GlyphScale = 1.2f;
    }
    
    #endregion
    
    #region GlyphSphere Configuration
    
    public static class GlyphSphere
    {
        // Sphere geometry
        public const float QuadSize = 0.3f; // Size of each glyph quad on the sphere
        public const float VertexShaderSizeMultiplier = 2.0f; // Multiplier used in vertex shader
        public const float SphereRadius = 45.0f; // Main sphere radius
        public const int SphereSubdivisions = 6; // Icosphere subdivision level (affects vertex density)
        
        // Camera settings
        public const float CameraDefaultDistance = 80.0f; // Default camera distance
        public const float CameraMinDistance = 30.0f; // Minimum camera distance
        public const float CameraMaxDistance = 200f; // Maximum camera distance
        
        // Camera zoom distances for different game phases
        public const float CameraZoomWorldView = 100.0f; // Destination selection phase (starting value)
        public const float CameraZoomTraveling = 85.0f; // Travel animation phase
        public const float CameraZoomNarration = 65.0f; // Location interaction/narration phase
        
        // Default glyph settings
        public const char DefaultGlyph = '.';
        public const int GlyphFontSize = 65; // Raster size
        public const int GlyphCellSize = 60; // Cell in atlas
        
        // Protagonist and pathfinding characters
        public const char ProtagonistChar = '☻'; // Smiling face for protagonist
        public const char PathWaypointChar = '.'; // Dot for waypoints
        public const char PathDestinationChar = '+'; // Plus for destination
        
        // Protagonist and pathfinding colors (RGB 0-255)
        public static readonly System.Numerics.Vector3 ProtagonistColor = new(255, 255, 255); // Yellow
        public static readonly System.Numerics.Vector3 PathWaypointPreviewColor = new(255, 255, 255); // Light blue
        public static readonly System.Numerics.Vector3 PathDestinationPreviewColor = new(255, 255, 255); // Light red
        public static readonly System.Numerics.Vector3 PathWaypointActiveColor = new(255, 255, 255); // Gold
        public static readonly System.Numerics.Vector3 PathDestinationActiveColor = new(255, 255, 255); // Bright yellow
        
        // Update timing for interface animations
        public const float UpdateInterval = 0.1f; // Update every 100ms (10 Hz)
        
        // Pathfinding noise
        public const int PathfindingNoiseSeed = 42; // Fixed seed for consistent paths
        public const float PathfindingNoiseStrength = 0.25f; // 0-1, adds up to 15% variation to edge costs
        
        // Rendering
        public const float NarrationWorldDarkeningFactor = 0.3f; // 0-1, multiplier for world brightness during narration (0.3 = 70% darker)
        
        // Clip planes
        public const float NearClipPlane = 0.01f;
        public const float FarClipPlane = 800.0f; // Must be > SkyCloud.SkySphereRadius + CameraMaxDistance
    }
    
    #endregion
    
    #region SkyCloud Configuration
    
    /// <summary>
    /// Configuration for decorative cloud and star sky spheres.
    /// Purely visual - no gameplay interaction.
    /// </summary>
    public static class SkyCloud
    {
        // Cloud sphere (slightly larger than world)
        public const float CloudSphereRadius = 46.0f;
        public const int CloudSubdivisions = 6;           // Icosphere detail level (6 = ~40k verts, use CloudQuadSize for coverage)
        public const float CloudRotationSpeed = 0.3f;     // Degrees per second
        public const float CloudGlyphMinSize = 0.4f;
        public const float CloudGlyphMaxSize = 0.4f;
        public const float CloudQuadSize = 1.2f;          // Base quad size for cloud glyphs (world uses 0.3). Larger = bigger cloud coverage per glyph
        public const float CloudNoiseScale = 2.5f;        // Perlin noise frequency (higher = more varied patches)
        public const float CloudNoiseThreshold = 0.55f;   // Only show clouds where noise > threshold (higher = more gaps/blue sky)
        public const float CloudBaseOpacity = 0.75f;      // Max alpha for densest cloud glyphs (0.5 = half transparent)
        public const string CloudChars = "⁙";          // Characters used for cloud glyphs (ordered by density)
        
        // Sky sphere (much larger, camera is inside it)
        public const string SkyChars = ".*oO";              // Characters for stars/planets/moons (. dim star, * bright star, o planet, O moon)
        public const float SkySphereRadius = 400.0f;
        public const int SkySubdivisions = 6;             // More vertices = more stars
        public const float SkyStarMinSize = 1.2f;
        public const float SkyStarMaxSize = 6.0f;
        public const float StarDensity = 0.25f;           // 25% of vertices become stars
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
        public static readonly Vector4 Orange = new(1.0f, 0.5f, 0.0f, 1.0f);
        public static readonly Vector4 DarkOrange = new(0.6f, 0.25f, 0.0f, 1.0f);
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
        // ModusMentis level indicators
        public const char ModusMentisLevelIndicator = '⟐';
        
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

        /// <summary>
        /// Color for a difficulty level on the 1-10 scale:
        /// two-segment gradient: white (1) → yellow (5-6) → red (10).
        /// </summary>
        public static Vector4 DifficultyLevelColor(int level)
        {
            float t = (Math.Clamp(level, 1, 10) - 1) / 9.0f; // 0.0 = easy, 1.0 = hard
            if (t <= 0.5f)
            {
                // white → yellow
                float s = t / 0.5f;
                return new Vector4(1.0f, 1.0f, 1.0f - s, 1.0f);
            }
            else
            {
                // yellow → red
                float s = (t - 0.5f) / 0.5f;
                return new Vector4(1.0f, 1.0f - s, 0.0f, 1.0f);
            }
        }
        
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
        // Layout padding - transparent lines/columns at edges for menu size control
        public const int TopPadding = 10;    // Number of transparent lines above header
        public const int BottomPadding = 10; // Number of transparent lines below status bar
        public const int LeftPadding = 1;    // Number of transparent columns on left side
        public const int RightPadding = 1;   // Number of transparent columns on right side
        
        // Top padding appearance
        public const char TopPaddingChar = ' ';
        public static readonly Vector4 TopPaddingTextColor = Colors.DarkGray20;
        public static readonly Vector4 TopPaddingBackgroundColor = Colors.Transparent;
        public const char TopPaddingEdgeChar = '▪';  // Last line of top padding (frame)
        public static readonly Vector4 TopPaddingEdgeTextColor = Colors.DarkGray20;
        public static readonly Vector4 TopPaddingEdgeBackgroundColor = Colors.Black;
        
        // Bottom padding appearance
        public const char BottomPaddingChar = ' ';
        public static readonly Vector4 BottomPaddingTextColor = Colors.DarkGray20;
        public static readonly Vector4 BottomPaddingBackgroundColor = Colors.Transparent;
        public const char BottomPaddingEdgeChar = '▪';  // First line of bottom padding (frame)
        public static readonly Vector4 BottomPaddingEdgeTextColor = Colors.DarkGray20;
        public static readonly Vector4 BottomPaddingEdgeBackgroundColor = Colors.Black;
        
        // Left padding appearance
        public const char LeftPaddingChar = ' ';
        public static readonly Vector4 LeftPaddingTextColor = Colors.DarkGray20;
        public static readonly Vector4 LeftPaddingBackgroundColor = Colors.Transparent;
        public const char LeftPaddingEdgeChar = '▪';  // Last column of left padding (frame)
        public static readonly Vector4 LeftPaddingEdgeTextColor = Colors.DarkGray20;
        public static readonly Vector4 LeftPaddingEdgeBackgroundColor = Colors.Black;
        
        // Right padding appearance
        public const char RightPaddingChar = ' ';
        public static readonly Vector4 RightPaddingTextColor = Colors.DarkGray20;
        public static readonly Vector4 RightPaddingBackgroundColor = Colors.Transparent;
        public const char RightPaddingEdgeChar = '▪';  // First column of right padding (frame)
        public static readonly Vector4 RightPaddingEdgeTextColor = Colors.DarkGray20;
        public static readonly Vector4 RightPaddingEdgeBackgroundColor = Colors.Black;
        
        // Colors following black/white/yellow theme
        public static readonly Vector4 HeaderColor = Colors.DarkYellowGrey; // Dark yellow-grey for location title
        public static readonly Vector4 ModusMentisHeaderColor = Colors.DarkYellowGrey; // Dark yellow-grey for modusMentis headers
        public static readonly Vector4 NarrativeColor = Colors.MediumGray60; // Medium grey for base text (darker for better contrast)
        public static readonly Vector4 KeywordNormalColor = Colors.White; // White for interactive elements
        public static readonly Vector4 KeywordHoverColor = Colors.BrightYellow; // Yellow text on hover
        public static readonly Vector4 KeywordHoverBackgroundColor = Colors.DarkYellow; // Dark yellow background on hover
        public static readonly Vector4 ActionNormalColor = Colors.White; // White for interactive elements
        public static readonly Vector4 ActionHoverColor = Colors.BrightYellow; // Yellow text on hover
        public static readonly Vector4 ActionHoverBackgroundColor = Colors.DarkYellow; // Dark yellow background on hover
        public static readonly Vector4 ActionModusMentisColor = Colors.BrightYellow; // Yellow for modusMentis brackets
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
        public static readonly Vector4 DimmedContentColor = Colors.DarkGray35; // Dark grey for content when continue button is shown
    }
    
    #endregion
    
    #region Location UI
    
    public static class LocationUI
    {
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
    
    #region Thinking ModusMentis Popup
    
    public static class ThinkingModusMentisPopup
    {
        public static readonly Vector4 ModusMentisNormalColor = Colors.White; // White for interactive modiMentis
        public static readonly Vector4 ModusMentisHoverColor = Colors.BrightYellow; // Yellow text on hover
        public static readonly Vector4 ModusMentisHoverBackgroundColor = Colors.DarkYellow; // Dark yellow background on hover
        public static readonly Vector4 BackgroundColor = new(0.0f, 0.0f, 0.0f, 0.9f); // Semi-transparent black
        public static readonly Vector4 TransparentColor = new(0.0f, 0.0f, 0.0f, 0.0f);
    }
    
    #endregion
    
    #region Exploration Popup
    
    public static class ExplorationPopup
    {
        public static readonly Vector4 LocationNameTextColor = Colors.Black; // White text for location names
        public static readonly Vector4 LocationNameBackgroundColor = Colors.BrightYellow; // Black background
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
    
    #region Narrative Configuration
    
    public static class Narrative
    {
        /// <summary>
        /// The name used to refer to the player in prompts and UI text.
        /// Default: "player" (can be changed to "protagonist", "protagonist", etc.)
        /// </summary>
        public const string PlayerName = "character";
        
        /// <summary>
        /// Target number of keywords to include in observations.
        /// For overall observations: if more outcomes than this, sample this many outcomes.
        /// If fewer outcomes, sample multiple keywords per outcome until reaching this target.
        /// For focus observations: if more keywords than this, sample this many keywords.
        /// </summary>
        public const int TargetKeywordCount = 10;

        /// <summary>
        /// Base instruction appended to every LLM prompt to enforce concise, grounded JSON responses.
        /// Does not include the character reminder — use <see cref="AnswerInstructionFor"/> to get the full instruction.
        /// </summary>
        public const string AnswerInstruction = "Respond in JSON format. Answer in one short sentence and stop. Use only the given information; no invention.";

        /// <summary>
        /// Returns the full answer instruction, appending a character reminder from PersonaReminder2 when available.
        /// Falls back to "Stay in character." when no reminder is provided.
        /// </summary>
        public static string AnswerInstructionFor(string? personaReminder2) =>
            personaReminder2 != null
                ? $"{AnswerInstruction} Stay in the character of {personaReminder2}."
                : $"{AnswerInstruction} Stay in character.";
    }

    /// <summary>
    /// Configuration for the plausibility check questions asked to the critic LLM.
    /// Each entry defines one independent question, its enum choices, and which choice ids
    /// count as a failure (action rejected). Questions are evaluated in order; all are always
    /// asked (continueOnFailure mode) so every check is visible in the trace.
    /// Add, remove, or reorder entries here to change the plausibility check battery.
    /// </summary>
    public static class PlausibilityQuestions
    {
        public record Choice(string Id, string Description, bool IsFailure, string? ErrorMessage = null);
        public record Question(string Name, string Text, List<Choice> Choices);

        public static readonly List<Question> Questions =
        [
            new Question(
                Name: "PhysicalFeasibility",
                Text: "Can a human body physically perform this action at all?",
                Choices:
                [
                    new("clearly_possible",    "clearly within human physical capability",            IsFailure: false),
                    new("possible_with_effort","possible but requires significant physical effort",   IsFailure: false),
                    new("borderline",          "borderline — requires exceptional ability",           IsFailure: false),
                    new("barely_possible",     "barely conceivable for an exceptional individual",   IsFailure: false),
                    new("physically_impossible","physically impossible for any human",                IsFailure: true,
                        ErrorMessage: "This action is physically impossible"),
                ]
            ),

            new Question(
                Name: "RequiredElements",
                Text: "Is anything missing for this action to be attempted?",
                Choices:
                [
                    new("nothing_missing",  "nothing is missing, all required elements are present", IsFailure: false),
                    new("minor_missing",    "a minor, easily improvised element is absent",          IsFailure: false),
                    new("tool_missing",     "a specific tool or object is absent",                   IsFailure: true,
                        ErrorMessage: "A required object is not available"),
                    new("person_missing",   "a required person or creature is absent",               IsFailure: true,
                        ErrorMessage: "A required person is not present"),
                    new("location_wrong",   "the wrong location — a specific place is required",    IsFailure: true,
                        ErrorMessage: "This action requires a different location"),
                ]
            ),

            new Question(
                Name: "Duration",
                Text: "How long would this action realistically take to complete?",
                Choices:
                [
                    new("seconds", "a few seconds",   IsFailure: false),
                    new("minutes", "several minutes", IsFailure: false),
                    new("hours",   "a few hours",     IsFailure: false),
                    new("days",    "multiple days",   IsFailure: true,
                        ErrorMessage: "This action would take too many days to complete"),
                    new("weeks",   "weeks or longer", IsFailure: true,
                        ErrorMessage: "This action would take weeks — far too long"),
                ]
            ),

            new Question(
                Name: "SituationalConsistency",
                Text: "How well does this action fit the current situation and recent events?",
                Choices:
                [
                    new("fully_consistent",      "fully consistent with the current situation",          IsFailure: false),
                    new("mostly_consistent",     "mostly consistent, minor tension with the situation",  IsFailure: false),
                    new("somewhat_inconsistent", "somewhat inconsistent with recent events",             IsFailure: false),
                    new("contradicts_events",    "directly contradicts what just happened",              IsFailure: true,
                        ErrorMessage: "This contradicts what just occurred"),
                    new("nonsensical",           "makes no sense given the current context",             IsFailure: true,
                        ErrorMessage: "This action makes no sense in the current situation"),
                ]
            ),
        ];
    }

    #endregion
    
    #region Image-to-Text Conversion
    
    /// <summary>
    /// Configuration for layered image-to-text conversion with engraving-style rendering.
    /// Each layer represents a brightness range with its own glyph gradient and color.
    /// </summary>
    public static class ImageToText
    {
        /// <summary>
        /// Defines a brightness layer for image conversion
        /// </summary>
        public class BrightnessLayer
        {
            public string Name { get; set; } = "";
            public float MinBrightness { get; set; } // 0.0 to 1.0
            public float MaxBrightness { get; set; } // 0.0 to 1.0
            public string GlyphGradient { get; set; } = ""; // Characters from thinnest to boldest
            public Vector4 Color { get; set; } // Color for this layer
        }
        
        /// <summary>
        /// Layered brightness configuration for engraving-style conversion.
        /// Layers are processed from darkest to lightest.
        /// Each layer uses a distinct glyph set for varied texture.
        /// </summary>
        public static readonly List<BrightnessLayer> Layers = new()
        {
            // Layer 0: Shadows (0-33% brightness) - Shade blocks for background
            new BrightnessLayer
            {
                Name = "Shadows",
                MinBrightness = 0.0f,
                MaxBrightness = 0.25f,
                GlyphGradient = "░░▒▒▓",
                Color = new Vector4(0.1f, 0.1f, 0.1f, 1.0f) // Dark gray
            },
            
            // Layer 1: Mid-tones - Normal symbols
            new BrightnessLayer
            {
                Name = "Mid-tones",
                MinBrightness = 0.25f,
                MaxBrightness = 0.5f,
                GlyphGradient = ".:",
                Color = new Vector4(0.3f, 0.3f, 0.3f, 1.0f)
            },
            
            // Layer 2: Highlights - Bold patterns
            new BrightnessLayer
            {
                Name = "Highlights",
                MinBrightness = 0.5f,
                MaxBrightness = 0.75f,
                GlyphGradient = "~=*#",
                Color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f)
            },
            
            // Layer 3: Bright - Brightest highlights
            new BrightnessLayer
            {
                Name = "Bright",
                MinBrightness = 0.75f,
                MaxBrightness = 1.0f,
                GlyphGradient = "-+@",
                Color = new Vector4(0.7f, 0.7f, 0.7f, 1.0f)
            }
        };
        
        /// <summary>
        /// Folder name prefix for output files
        /// </summary>
        public static readonly string OutputFolderPrefix = "ascii_art_layers_";
        
        /// <summary>
        /// Base output directory (relative to executable)
        /// </summary>
        public static readonly string OutputBaseDirectory = "logs";
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
            { '∅', 1.5f },
            { '⎆', 1.7f },

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
    
    #region LLM Configuration
    
    /// <summary>
    /// Parameters sent to the local llama.cpp server for all LLM requests.
    /// </summary>
    public static class LLM
    {
        // Sampling parameters (narrative generation and constrained single-token requests)
        public const int GenerationMaxTokens = 2048;
        public const double Temperature = 0.5;
        public const int TopK = 6;
        public const double TopP = 0.9;

        // Temperature for utility requests (health-check, prompt pre-caching)
        public const double UtilityTemperature = 0.1;
    }
    
    #endregion
}
