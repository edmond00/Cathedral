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
        public const int MainCellSize = 20;
        public const int MainFontSize = 22;
        
        // Popup terminal dimensions
        public const int PopupWidth = 40;
        public const int PopupHeight = 40;
        public const int PopupCellSize = MainCellSize;
    }
    
    #endregion
    
    #region GlyphSphere Configuration
    
    public static class GlyphSphere
    {
        // Sphere geometry
        public const float QuadSize = 0.3f; // Size of each glyph quad on the sphere
        public const float VertexShaderSizeMultiplier = 2.0f; // Multiplier used in vertex shader
        public const float SphereRadius = 50.0f; // Main sphere radius
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
        public const int GlyphPixelSize = 65; // Raster size
        public const int GlyphCellSize = 50; // Cell in atlas
        
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
        public const float CloudSphereRadius = 52.0f;
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
        public const string PlayerName = "player";
        
        /// <summary>
        /// Target number of keywords to include in observations.
        /// For overall observations: if more outcomes than this, sample this many outcomes.
        /// If fewer outcomes, sample multiple keywords per outcome until reaching this target.
        /// For focus observations: if more keywords than this, sample this many keywords.
        /// </summary>
        public const int TargetKeywordCount = 10;
        
        /// <summary>
        /// Whether to include circuitous outcomes in the thinking phase.
        /// Circuitous outcomes are "outcomes of outcomes" - they require going through an intermediate node.
        /// </summary>
        public const bool EnableCircuitousOutcomes = true;
        
        /// <summary>
        /// Maximum number of circuitous outcomes to include in the thinking phase.
        /// If more circuitous outcomes are available, a random sample of this size will be used.
        /// This ensures straightforward outcomes remain the most common options.
        /// </summary>
        public const int MaxCircuitousOutcomes = 3;
        
        /// <summary>
        /// Difficulty score penalty (0.0-1.0) added to circuitous outcomes.
        /// Each 0.1 roughly corresponds to +1 difficulty level.
        /// Default: 0.1 = +1 difficulty level for circuitous actions.
        /// </summary>
        public const double CircuitousDifficultyPenalty = 0.1;
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
            
            // Layer 1: Mid-tones (33-66% brightness) - Normal symbols
            new BrightnessLayer
            {
                Name = "Mid-tones",
                MinBrightness = 0.25f,
                MaxBrightness = 0.5f,
                GlyphGradient = ".:",
                Color = new Vector4(0.6f, 0.6f, 0.6f, 1.0f) // Medium gray
            },
            
            // Layer 2: Highlights (66-100% brightness) - Bold patterns
            new BrightnessLayer
            {
                Name = "Highlights",
                MinBrightness = 0.5f,
                MaxBrightness = 0.75f,
                GlyphGradient = "~=*#",
                Color = new Vector4(0.8f, 0.8f, 0.4f, 1.0f) // Light gray
            },
            
            // Layer 2: Highlights (66-100% brightness) - Bold patterns
            new BrightnessLayer
            {
                Name = "Yellow",
                MinBrightness = 0.75f,
                MaxBrightness = 1.0f,
                GlyphGradient = "-+@",
                Color = new Vector4(1f, 1f, 0f, 1.0f) // Light gray
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
