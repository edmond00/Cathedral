using System.Collections.Generic;
using OpenTK.Mathematics;

namespace Cathedral;

/// <summary>
/// Centralized configuration for the entire application.
/// Contains all UI settings, colors, dimensions, and layout constants.
/// </summary>
public static class Config
{
    #region Randomness

    public static class Rng
    {
        /// <summary>
        /// Master seed for the entire playthrough — world layout, protagonist spawn,
        /// dice rolls, travel-path jitter, etc. all derive from it (see <see cref="GameRng"/>).
        ///
        /// <list type="bullet">
        /// <item><c>null</c> (default) → a fresh time-based seed each launch, so every run
        /// gets a different world.</item>
        /// <item>a fixed integer → the exact same run can be replayed by making the same
        /// decisions.</item>
        /// </list>
        ///
        /// Set it here, or pass <c>--seed &lt;n&gt;</c> on the command line (the CLI flag
        /// overrides this value). The resolved seed is printed at startup so a time-based
        /// run can be pinned afterwards.
        ///
        /// Note: this does not affect the LLM (its sampling is nondeterministic and carries
        /// no seed), nor RNG already seeded from world data such as NPC or location ids.
        /// </summary>
        public static int? Seed { get; set; } = null;
    }

    /// <summary>
    /// Switches that exist only to make a feature testable, set from command-line flags and never
    /// from gameplay. Anything here must be inert when left at its default, so a normal run behaves
    /// exactly as if the option did not exist.
    ///
    /// <para>Adding to this is expected: when a change is hard to reach from a <c>--cli</c> script —
    /// it needs a rare biome, a particular time of day, a specific roll — the fix is a new flag here
    /// plus a line in CLAUDE.md, not a hunt for a lucky seed.</para>
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Time of day to arrive at every location with, instead of a random draw. Set by
        /// <c>--period &lt;dawn|morning|noon|afternoon|evening|night&gt;</c>. Night is the one that
        /// matters most: it is when every building's entry door is shut, and a random arrival hits it
        /// one visit in six.
        /// </summary>
        public static Game.Narrative.TimePeriod? ForcedPeriod { get; set; } = null;

        /// <summary>
        /// Whether the developer keyboard shortcuts respond. <b>False in a shipped build</b>
        /// (<c>-p:Ship=true</c> defines SHIP); true everywhere else.
        ///
        /// <para>What it gates: the render-debug keys (D shader mode, M markers), the post-process
        /// tuning keys (F dither, G levels, H grain, J pulses), the debug camera (C, V), the
        /// window's diagnostic dumps (D, G), <b>and camera zoom (W, S)</b>.</para>
        ///
        /// <para>Zoom is in that list on purpose even though it is not a debug feature. The game
        /// frames its own camera — narration, travel and fights each set a distance — and a player
        /// who zooms out of that framing has no way back to it and reports the result as a bug.
        /// Rotation and re-centring (arrows, Space) stay: they cannot leave a state you cannot
        /// recover from.</para>
        ///
        /// <para><b>Escape is not gated</b> and never should be. It opens the pause menu and closes
        /// narration popups; without it a player has no way out of a scene.</para>
        ///
        /// <para>Settable so a development build can be tested as though it were shipped
        /// (<c>--no-developer-keys</c>). Setting it true in a SHIP build does nothing useful — the
        /// handlers are still compiled in, but nothing turns it on.</para>
        /// </summary>
        public static bool DeveloperKeys { get; set; } =
#if SHIP
            false;
#else
            true;
#endif

        /// <summary>
        /// Compute device for the language model, overriding both the player's setting and the
        /// first-run probe. Set by <c>--cpu</c> (and <c>--gpu</c>).
        ///
        /// <para>It lives here rather than being written into <see cref="UserSettings"/> because
        /// flags are parsed before <c>UserSettings.Load()</c> runs, so a flag that wrote to the
        /// settings object would be silently overwritten by the file a moment later. It also
        /// <i>should</i> not persist: <c>--cpu</c> is one run's instruction, not a change to what
        /// the player chose.</para>
        ///
        /// <para>Null means "no override" — the settings and the probe decide, exactly as if the
        /// flag did not exist.</para>
        ///
        /// <para>Qualified with <c>global::</c> because <see cref="Config.LLM"/> — the sampling
        /// settings nested in this same class — shadows the <c>Cathedral.LLM</c> namespace here.</para>
        /// </summary>
        public static global::Cathedral.LLM.LlamaComputeDevice? ForcedLlmDevice { get; set; } = null;

        /// <summary>
        /// Biome or location name to place the protagonist on at world generation, e.g. "village".
        /// Set by <c>--start-at &lt;name&gt;</c>. Matched case-insensitively as a substring; ignored
        /// when nothing in the world matches.
        /// </summary>
        public static string? StartAt { get; set; } = null;

        /// <summary>
        /// Forces every scene to be built as though it were this location id, whatever vertex the
        /// protagonist actually stands on. Set by <c>--location-id &lt;n&gt;</c>.
        ///
        /// <para><b>What it is for.</b> A scene is a pure function of its location id
        /// (<c>SceneFactory.CreateSeededRandom(locationId)</c>), so the id decides the layout, the
        /// room names, the objects and the people — a village rolls a Chain or a Hub with entirely
        /// different rooms depending on it. That makes every other targeting flag id-dependent:
        /// <c>--start-area "Alehouse Store"</c> and <c>--observe-only "Shelving Rack"</c> name things
        /// that exist in *some* villages.</para>
        ///
        /// <para><c>--verb-probe</c> reports the situations that reach each verb by sampling ids
        /// 0…N. Without this flag a test written from that report is aimed at a location the game
        /// will never build — <c>--start-at village</c> lands on whatever vertex the world generated,
        /// and its id is not one of the sampled ones. Pinning the id is what makes the probe's
        /// findings and the run agree, and so what makes a generated verb test hit its verb.</para>
        ///
        /// <para>Inert at its default of null: the vertex index is used, exactly as before.</para>
        /// </summary>
        public static int? LocationId { get; set; } = null;

        /// <summary>
        /// Forces which scene factory builds every location, ignoring the biome under the avatar.
        /// Set by <c>--location-type &lt;name&gt;</c> ("forest", "village", "cave"…).
        ///
        /// <para><b>Why <c>--start-at</c> is not enough.</b> That flag walks the generated world
        /// looking for a matching biome, and when the world does not contain one within reach it
        /// shrugs and uses the normal spawn. At seed 42 there is no forest near the start, so every
        /// test written for a forest ran in a plain — and, since <c>--location-id</c> pins the id but
        /// not the factory, went on to build the wrong factory at the right number.</para>
        ///
        /// <para>Together with <see cref="LocationId"/> this gives a test complete control: the
        /// factory and the id decide the whole scene, so the world need not contain the biome at all.
        /// That is what makes a verb test independent of world generation.</para>
        ///
        /// <para>Inert at its default of null.</para>
        /// </summary>
        public static string? LocationType { get; set; } = null;

        /// <summary>
        /// Area inside a location to open narration in, e.g. "pigsty". Set by
        /// <c>--start-area &lt;name&gt;</c>. Matched case-insensitively as a substring of the area's
        /// display name; ignored when the location has no such area, so it is harmless to leave on
        /// while moving between locations.
        ///
        /// <para><c>--start-at</c> gets a script to the right location; this gets it to the right room.
        /// Without it a script arrives in whichever area the factory built first — a farm's courtyard —
        /// and reaching anything else means walking there through observation, thinking and an action
        /// per step, with the persona choosing what is observable at each one. Anything that lives in a
        /// specific room (a pigsty's pigs, a smithy's anvil) is otherwise a long approach away.</para>
        /// </summary>
        public static string? StartArea { get; set; } = null;

        /// <summary>
        /// Pins every NPC to one area for the whole day, instead of following their schedule. Set by
        /// <c>--npc-static</c>.
        ///
        /// <para><b>What it is for.</b> A schedule sends somebody to their bed, their workplace and
        /// two or three other rooms across the six periods, drawn from the location seed — so "where
        /// is the brewer at dawn?" has an answer that depends on the location id and the hour, and
        /// nothing about it is guessable when writing a test. Every NPC verb (stalk, provoke,
        /// pickpocket, meet_stranger, attack …) has to find its person before it can do anything, and
        /// a test that names the wrong room finds an empty one.</para>
        ///
        /// <para>This removes the variable: the NPC is where the schedule puts them at their busiest
        /// hour, at every hour. Run <c>--verb-probe</c> with the same flag and its reported rooms are
        /// the rooms the run will use.</para>
        ///
        /// <para>Inert at its default of false — schedules are followed exactly as before, so nothing
        /// about a normal run changes.</para>
        /// </summary>
        public static bool NpcStatic { get; set; } = false;

        /// <summary>
        /// Opens no audio device at all: no music, no sound effects, no loading wash.
        ///
        /// <para>For test runs. The suite launches the game a hundred-odd times, and every one of
        /// them starts its ambience — so a full run is an hour of music from windows nobody is
        /// looking at, several of them overlapping. <c>run_tests.sh</c> passes this on every script.</para>
        ///
        /// <para>Implemented by not opening the MIDI device rather than by turning a volume down,
        /// because that is a state the engine already supports and handles everywhere: a machine with
        /// no MIDI device runs this path in production. Nothing else has to learn about silence.</para>
        ///
        /// <para>Inert at its default of false.</para>
        /// </summary>
        public static bool Silent { get; set; } = false;
        /// <summary>
        /// Settles every dialogue as an immediate success instead of holding it. Set by
        /// <c>--auto-dialogue</c>.
        ///
        /// <para>A dozen verbs do nothing themselves — they open a conversation, and the tree decides
        /// what follows. Without this, a test for the <i>verb</i> has to walk somebody else's tree to
        /// reach its own assertion, and is broken by any re-authoring of that tree. With it, the verb
        /// test asserts about the verb; the trees are covered separately by
        /// <c>cli/_systems/dialogue_*.cli</c>, which drive them properly.</para>
        ///
        /// <para>See <c>DialogueAutoResolve</c>: it performs exactly the writes a won conversation
        /// makes, so nothing reading the world afterwards can tell the difference.</para>
        ///
        /// <para>Inert at its default of false.</para>
        /// </summary>
        public static bool AutoDialogue { get; set; } = false;

        /// <summary>
        /// Starts every NPC at this affinity with the protagonist instead of Stranger. Set by
        /// <c>--npc-affinity &lt;level&gt;</c> (<c>distant_acquaintance</c>, <c>close_friend</c>…).
        ///
        /// <para>Six verbs are gated on already knowing somebody — <c>propose_to_buy</c>,
        /// <c>propose_to_sell</c>, <c>propose_to_join</c>, <c>request_job</c>,
        /// <c>strengthen_relationship</c> and the trade modes behind them all want
        /// DistantAcquaintance or better. Earning that in-script means holding a whole conversation
        /// first and winning its roll, which makes the test a test of <i>that</i> conversation.</para>
        ///
        /// <para>Seeded when a location's affinity store is first handed out, so it behaves exactly
        /// like a relationship built in play — including persisting, since it lands in the same
        /// backing store. Inert at its default of null.</para>
        /// </summary>
        public static Game.Dialogue.Affinity.AffinityLevel? NpcAffinity { get; set; } = null;

        /// <summary>
        /// Makes every NPC count the protagonist an enemy from the start. Set by <c>--npc-hostile</c>.
        ///
        /// <para>The counterpart to <see cref="NpcAffinity"/>, for the verbs that need somebody who
        /// already wants to fight: <c>reconcile</c> and <c>appease</c> apply only to an enemy (or an
        /// annoying acquaintance). <c>--spawn-beast</c> covers the beast case, but a wolf cannot be
        /// reconciled with — that tree needs somebody who can speak — and earning a human enemy in
        /// script means committing a crime, being caught, losing the confrontation and walking out of
        /// the resulting fight.</para>
        ///
        /// <para>Seeded into the same enemy store gameplay writes to, so it reads back exactly like a
        /// grudge earned in play. Inert at its default of false.</para>
        /// </summary>
        public static bool NpcHostile { get; set; } = false;

        /// <summary>
        /// Restricts what an observation phase may look at to objects whose name contains this, e.g.
        /// "pig". Set by <c>--observe-only &lt;name&gt;</c>. Null (the default) offers the whole scene,
        /// exactly as before.
        ///
        /// <para>Which object a phase observes is a persona choice, and a phase opens on ONE object out
        /// of a dozen — so a script that wants to act on a particular thing is at the mercy of that
        /// choice, and re-rolling seeds until the right one comes up first is not a test. This pins it.
        /// Ignored for a phase where nothing matches (the persona chooses freely again), so an area
        /// without the named object still narrates instead of falling silent.</para>
        /// </summary>
        public static string? ObserveOnly { get; set; } = null;

        /// <summary>
        /// Suppresses random travel encounters. Set by <c>--no-encounters</c>. Inert at its default
        /// of false: a run without the flag rolls for encounters exactly as it always did.
        ///
        /// <para>For scripted runs. A CLI script that travels somewhere and waits for
        /// <c>LocationInteraction</c> has no way to know an encounter has put the game in
        /// <c>EncounterPrompt</c> instead, so it sits there until its timeout and reports a failure
        /// that has nothing to do with what it was testing. The encounter is not the thing under
        /// test; being able to turn it off is what makes everything else testable.</para>
        /// </summary>
        public static bool NoEncounters { get; set; } = false;

        /// <summary>
        /// Days to push the world clock forward once the run reaches the world map, on top of
        /// whatever travel and work have accrued. Set by <c>--advance-days &lt;n&gt;</c>.
        /// Inert at its default of 0.
        ///
        /// <para>For scripted runs. <see cref="Game.Narrative.GameClock"/> only moves on travel
        /// arrival and work stints, and a wound takes 100–1000 days to close — so without this a
        /// script that wants to see healing happen has to simulate years of journeys. Applied once,
        /// at the first entry to the world view, and then cleared.</para>
        /// </summary>
        public static double AdvanceDays { get; set; } = 0;

        /// <summary>
        /// Modi mentis to grant the protagonist after character creation, and the level to set them
        /// to. Set by <c>--grant-mm &lt;id[,id…]&gt;[:level]</c>. Null means grant nothing.
        ///
        /// <para>For scripted runs. Fighting skills are gated behind their modi mentis, so without
        /// this a script cannot reach the buffs at all — and cannot exercise both ends of the
        /// level-derived vital-heat curve, which is the whole of a buff's cost model.</para>
        /// </summary>
        public static (string[] Ids, int Level)? GrantModiMentis { get; set; } = null;

        /// <summary>
        /// Creature to fight immediately on reaching the world map, e.g. "wolf". Set by
        /// <c>--start-fight &lt;creature&gt;</c>. Null means no debug fight.
        ///
        /// <para>For scripted runs, and the flag the rest of the fight work depends on: the only
        /// real ways into a fight are a random travel encounter (which every script disables with
        /// <c>--no-encounters</c>) and provoking a location NPC through a conversation and a check.
        /// Neither is a reasonable prerequisite for testing combat itself.</para>
        /// </summary>
        public static string? StartFight { get; set; } = null;

        /// <summary>
        /// Beast archetype to add to every scene as it is built, e.g. "wolf". Set by
        /// <c>--spawn-beast &lt;name&gt;</c>. Null means add nothing.
        ///
        /// <para>For scripted runs. Every beast a wilderness factory places is rolled (a wolf 10–20%
        /// of the time, a boar 25–40%) and then given a roaming schedule, so whether one is standing
        /// where the script opens is two coin flips deep — and the beast is the whole subject of
        /// appease/tame. This puts one in the opening area at every period, flagged an enemy by the
        /// same first-contact pass that flags a rolled one.</para>
        /// </summary>
        public static string? SpawnBeast { get; set; } = null;

        /// <summary>
        /// Verb id the playground's goal choice must land on, e.g. "tame". Set by
        /// <c>--goal-only &lt;verb-id&gt;</c> and by the CLI's <c>goal</c> command, which is what a
        /// script uses when the goal has to change between steps. Null (the default) leaves the
        /// choice to the RNG, exactly as before.
        ///
        /// <para><c>--playground</c> replaces the persona's "what do you want to do?" with a uniform
        /// draw over every goal the observed object offers, so a script that means to appease — and
        /// then to tame — a beast offering a dozen goals is not testing anything it can name. Ignored
        /// for a thinking phase where no goal matches, which then draws as usual.</para>
        /// </summary>
        public static string? GoalOnly { get; set; } = null;
    }

    #endregion

    #region Terminal Configuration
    public static class Name {
        /// <summary>Stylised lowercase, as the main menu draws it.</summary>
        public const string GameTitle = "proscribed palimpsest";

        /// <summary>
        /// Title case, for the OS window title bar — the one place the name appears outside the
        /// game's own typography, next to every other application's. Kept here rather than written
        /// at the window so the launchers cannot drift from the menu, which is how the title bar
        /// came to read "Cathedral - Location Travel Mode" in a shipped build.
        /// </summary>
        public const string WindowTitle = "Proscribed Palimpsest";

        public const string Chapter = "Volume 1";
        public const string ChapterSubtitle = "Turnips and Radishes";
    }
    
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
            '♞', '⚓',
            '⨯', // U+2A2F — vector cross used for the forbidden-travel flash on the world sphere
            '⎊', '◉', '⊚', // coin denomination glyphs (gold / silver / copper)
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
        public const float CameraZoomRoutineMinimap = 50.0f; // Routines-tab minimap porthole (closer = focused minimap)
        
        // Default glyph settings
        public const char DefaultGlyph = '.';
        public const int GlyphFontSize = 65; // Raster size
        public const int GlyphCellSize = 60; // Cell in atlas
        
        // Protagonist and pathfinding characters
        public const char ProtagonistChar = '☻'; // Smiling face for protagonist
        public const char PathWaypointChar = '.'; // Dot for waypoints
        public const char PathDestinationChar = '+'; // Plus for destination
        public const char ForbiddenDestinationChar = '⨯'; // Cross for impassable tiles (sea/ocean on foot)

        // Numbered glyphs used to mark waypoints in click-order on the world map.
        // Falls back to PathDestinationChar past the array length.
        public static readonly char[] WaypointNumberChars = new[] { '①', '②', '③', '④', '⑤', '⑥', '⑦', '⑧', '⑨' };

        // Maximum number of travel waypoints that can be queued at once.
        public const int MaxTravelWaypoints = 4;

        // Protagonist and pathfinding colors (RGB 0-255)
        public static readonly System.Numerics.Vector3 ProtagonistColor = new(255, 255, 255); // Yellow
        public static readonly System.Numerics.Vector3 PathWaypointPreviewColor = new(255, 255, 255); // Light blue
        public static readonly System.Numerics.Vector3 PathDestinationPreviewColor = new(255, 255, 255); // Light red
        public static readonly System.Numerics.Vector3 PathWaypointActiveColor = new(255, 255, 255); // Gold
        public static readonly System.Numerics.Vector3 PathDestinationActiveColor = new(255, 255, 255); // Bright yellow
        public static readonly System.Numerics.Vector3 PathForbiddenColor = new(200, 60, 60); // Red for impassable cells
        
        // Update timing for interface animations
        public const float UpdateInterval = 0.1f; // Update every 100ms (10 Hz)
        
        // Pathfinding noise. The seed now comes from the central Config.Rng.Seed /
        // GameRng master seed (see GameRng.DerivedSeed("pathfinding-noise")).
        public const float PathfindingNoiseStrength = 0.25f; // 0-1, adds up to 25% terrain-correlated variation to edge costs

        /// <summary>
        /// Per-edge deterministic jitter applied on top of the terrain-correlated noise.
        /// Because the jitter is independent across edges (hashed from the edge's two
        /// vertex ids), even subtle values push A* off the dead-straight great-circle
        /// and produce visually wandering travel paths. 0.10–0.25 is a good range.
        /// </summary>
        public const float PathfindingEdgeJitterStrength = 0.15f;
        
        // Rendering
        public const float NarrationWorldDarkeningFactor = 0.3f; // 0-1, multiplier for world brightness during narration (0.3 = 70% darker)
        
        // Clip planes
        public const float NearClipPlane = 0.01f;
        public const float FarClipPlane = 800.0f; // Must be > SkyCloud.SkySphereRadius + CameraMaxDistance
    }

    #endregion

    #region PostProcess Configuration

    /// <summary>
    /// Final full-screen shader layer applied to the whole render.
    /// Mutable rather than const: --dither sets it at startup and F/G/H retune it live.
    /// </summary>
    public static class PostProcess
    {
        // Resting state of the layer. 0 = off, 1 = Bayer 8x8, 2 = Bayer 4x4 two-tone, 3 = noise
        public static int DitherMode = 1;

        /// <summary>
        /// True once <c>--dither</c> has set the mode above. The saved
        /// <see cref="UserSettings.DitherEnabled"/> is applied at startup only when this is false,
        /// so a player who once turned dither on in the Settings screen does not silently break
        /// <c>--dither off</c> for every later run. The flag is one run's instruction and wins.
        /// </summary>
        public static bool DitherModeSetByFlag = false;
        public static int Levels = 6;       // Quantisation steps per channel (2 = 1 bit)
        public static int PixelScale = 1;   // Dither cell size in pixels (1 = fine, 4 = chunky)
        public static float Strength = 1.0f; // Blend between original and dithered

        // Event pulses: the layer briefly switches to a different dither on a game
        // event, the visual counterpart of the UI sound effects. See PostProcessRenderer.
        public static bool PulsesEnabled = true;
        public static float PulseDuration = 0.15f; // Seconds a pulse lasts before decaying back
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

        // Coin denomination colors (gold = yellow, silver = grey, copper = dark yellow)
        public static readonly Vector4 CoinGold   = new(1.0f, 0.85f, 0.2f, 1.0f);      // bright gold yellow
        public static readonly Vector4 CoinSilver = new(0.75f, 0.75f, 0.78f, 1.0f);    // cool grey
        public static readonly Vector4 CoinCopper = new(0.72f, 0.52f, 0.18f, 1.0f);    // dark yellow / bronze
        
        // Purple variants (for negative elements, wounds, danger)
        public static readonly Vector4 DarkPurple = new(0.3f, 0.0f, 0.45f, 1.0f);     // Dark purple for depleted HP, subtle negatives
        public static readonly Vector4 Purple = new(0.55f, 0.0f, 0.75f, 1.0f);         // Standard purple for wounds, enemy UI
        public static readonly Vector4 BrightPurple = new(0.72f, 0.0f, 1.0f, 1.0f);   // Bright purple for wound headers, defeat
        public static readonly Vector4 LightPurple = new(0.85f, 0.55f, 1.0f, 1.0f);   // Light purple for severity/gradients

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

        // Coin denomination glyphs (gold ≈ horse, silver ≈ sword, copper ≈ egg)
        public const char GoldCoinSymbol   = '⎊'; // yellow
        public const char SilverCoinSymbol = '◉'; // grey
        public const char CopperCoinSymbol = '⊚'; // dark yellow

        /// <summary>
        /// Color for a difficulty level on the 1-10 scale:
        /// two-segment gradient: white (1) → yellow (5-6) → purple (10).
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
                // yellow → purple: (1,1,0) → (0.72,0,1)
                float s = (t - 0.5f) / 0.5f;
                return new Vector4(1.0f - s * 0.28f, 1.0f - s, s, 1.0f);
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

        // Outcome report chip colors (colored background, black text)
        public static readonly Vector4 OutcomeReportTextColor        = Colors.Black;
        public static readonly Vector4 OutcomeReportPositiveBackground = Colors.DarkYellow;   // dark yellow — item / skill gain
        public static readonly Vector4 OutcomeReportNegativeBackground = Colors.DarkPurple;   // dark purple — wound / combat
        public static readonly Vector4 OutcomeReportNeutralBackground  = Colors.DarkGray40;   // mid grey — location / conversation
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
    
    #region Travel UI

    /// <summary>
    /// Compact UI box displayed in WorldView mode while travel waypoints are set.
    /// Sits a few rows above the bottom of the screen and is centered horizontally.
    /// </summary>
    public static class TravelUI
    {
        // Layout
        public const int BoxWidth = 40;
        public const int BoxHeight = 12;
        /// <summary>Cells of empty space between the box bottom edge and the screen bottom.</summary>
        public const int BoxBottomMargin = 8;

        // Colors
        public static readonly Vector4 BorderColor      = Colors.DarkYellowGrey;
        public static readonly Vector4 BackgroundColor  = Colors.BlackTransparent;
        public static readonly Vector4 TitleColor       = Colors.BrightYellow;
        public static readonly Vector4 LabelColor       = Colors.MediumGray50;
        public static readonly Vector4 ValueColor       = Colors.White;
        public static readonly Vector4 ValueAccentColor = Colors.BrightYellow;
        public static readonly Vector4 WarningColor     = Colors.OrangeYellow;
        public static readonly Vector4 DangerColor      = Colors.BrightPurple;

        // Primary action button (TRAVEL) — yellow chip.
        public static readonly Vector4 TravelButtonTextColor             = Colors.Black;
        public static readonly Vector4 TravelButtonBackgroundColor       = Colors.BrightYellow;
        public static readonly Vector4 TravelButtonHoverTextColor        = Colors.Black;
        public static readonly Vector4 TravelButtonHoverBackgroundColor  = Colors.White;

        // Secondary action button (CLEAR) — muted grey chip.
        public static readonly Vector4 ClearButtonTextColor              = Colors.LightGray85;
        public static readonly Vector4 ClearButtonBackgroundColor        = Colors.DarkGray35;
        public static readonly Vector4 ClearButtonHoverTextColor         = Colors.Black;
        public static readonly Vector4 ClearButtonHoverBackgroundColor   = Colors.LightGray85;

        public static readonly Vector4 HintColor        = Colors.DarkGray40;
    }

    #endregion

    #region Loading Messages
    
    /// <summary>
    /// The footer line under the narration panel while the game is busy. It is the player's only signal
    /// for what the wait is <i>for</i>, so each phase gets its own — but it is read from inside the
    /// fiction, so none of them names the machinery. "Narrating the outcome" tells the player there is
    /// an LLM writing a paragraph for them; "What follows" tells them the same thing about the world.
    /// <para>
    /// The register, for anything added here: three or four words, impersonal, present tense, no "you",
    /// no names, no verb the UI performs (generating, evaluating, loading). Say what is happening in the world,
    /// vaguely enough that it fits both a success and a failure — the message is up before the result
    /// is known. The trailing ellipsis is stripped and re-animated by
    /// <c>TerminalPanelUI.RenderWaitingStatus</c>; it is written here only so the strings read whole.
    /// </para>
    /// Dialogue keeps its own set, built per NPC in <c>DialogueTreeUI.BuildGeneratingText</c>.
    /// </summary>
    public static class LoadingMessages
    {
        // General — only ever seen between phases, on a state reset.
        public const string Default = "The world stirs...";

        // Observation phase — the senses reaching out, or a memory surfacing during the childhood
        // phase, where there are no surroundings to observe.
        public const string GeneratingObservations = "The senses wander...";
        public const string Remembering = "A memory surfaces...";

        // Thinking phase (keyword → modus mentis → actions).
        public const string ThinkingDeeply = "The mind turns...";

        // Speak About — the active member addresses a companion. Deliberately nameless: the player
        // picked the companion one click ago, so the name adds nothing here, and a footer that names
        // people is a footer that has to know which name is the real one.
        public const string Speaking = "Words take shape...";

        // Action: intent weighed (plausibility + difficulty) → dice → outcome. Three distinct waits,
        // and the last says nothing about which way it went — it is written before the player knows.
        public const string EvaluatingAction = "Intent gathers...";
        public const string RollingDice = "The bones fall...";
        public const string NarratingOutcome = "What follows...";
        public const string CombiningItem = "The hand weighs it...";
    }
    
    #endregion
    
    #region Dice

    public static class Dice
    {
        /// <summary>
        /// How long the dice-roll animation tumbles before the values lock in. Shared by every roll
        /// context — narrative checks, get-up, runaway, dialogue resolutions and fights — so the spin
        /// (and the dice-roll music that plays alongside it) has one consistent length.
        ///
        /// <para><b>Zero in playground mode.</b> Every one of these is time spent watching something
        /// there is nothing to decide about, and the test suite pays it once per roll across a
        /// hundred-odd scripts. Playground already replaces the thing that makes a run slow (the
        /// LLM); this replaces the rest. See <see cref="AnimationsAreInstant"/>.</para>
        /// </summary>
        public static float AnimationDurationSeconds => AnimationsAreInstant ? 0f : 5.0f;

        /// <summary>
        /// The animation duration in milliseconds, for <see cref="System.Threading.Tasks.Task.Delay(int)"/>.
        /// </summary>
        public static int AnimationDurationMs => (int)(AnimationDurationSeconds * 1000);
    }

    /// <summary>
    /// Whether purely decorative timing should be skipped: dice tumbling, a vital-heat bar draining,
    /// an AI pausing before it moves. True under <c>--playground</c>, which is the flag that means
    /// "this is a test run, not a play session".
    ///
    /// <para>Deliberately not its own switch. Every caller of this is animating a result that has
    /// already been decided, so there is nothing a script could want to observe mid-way — and a
    /// second flag would be one more thing a new test has to remember to pass.</para>
    /// </summary>
    public static bool AnimationsAreInstant => Cathedral.Game.PlaygroundMode.IsActive;

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
        /// Maximum character length of an LLM-generated narrative text — observation, thinking,
        /// action, outcome, speaking, dialogue, and sanitizer rewrites all share this cap. It is
        /// encoded straight into the GBNF grammar as the upper bound of the free-text body.
        /// </summary>
        public const int MaxNarrativeTextLength = 800;

        // ── Observation phase length thresholds (see ObservationPhaseController) ──────────────
        // All in characters of the final (sanitized) observation text. Kept here so the pacing of a
        // multi-object observation phase can be tuned without touching the controller.

        /// <summary>
        /// If the first object's observation text alone is longer than this, the phase stops there and
        /// no second (or third) observation is started — one rich paragraph stands on its own.
        /// </summary>
        public const int ObservationSkipSecondThreshold = 500;

        /// <summary>
        /// If the first + second observation texts together stay below this, and more than
        /// <see cref="ObservationThirdMinRemaining"/> other objects could still be observed, a third
        /// observation is added over the remaining objects.
        /// </summary>
        public const int ObservationTriggerThirdThreshold = 400;

        /// <summary>Minimum number of still-observable objects required before a third observation is added.</summary>
        public const int ObservationThirdMinRemaining = 5;

        /// <summary>
        /// If a single observation's text is longer than this, two distinct keywords (both linked to the
        /// same object, so either click does the same thing) are highlighted instead of one.
        /// </summary>
        public const int ObservationTwoKeywordsThreshold = 400;

        /// <summary>Length clause used for single-sentence rewrites (the default).</summary>
        private const string OneSentenceClause = "Answer in one short sentence and stop.";

        /// <summary>
        /// Brevity clause for the kinds that omit <see cref="OneSentenceClause"/> (observation,
        /// reasoning, outcome). Those are free to unfold over more than one sentence, but a small model
        /// left with no ceiling at all drifts into a paragraph that then gets cut off by the token
        /// limit — this caps the unfolding without pinning it to a single sentence.
        /// </summary>
        private const string ShortTextClause = "Write a short text: one to three sentences at most.";

        /// <summary>Grounding clause appended after the length clause; keeps the rewrite faithful.</summary>
        private const string GroundingClause = "Keep every literal fact from the given information and invent no new facts, names, objects or events.";

        /// <summary>
        /// Fallback styling clause used when no per-modusMentis <c>StyleInstruction</c> is supplied
        /// (e.g. NPC speaking, which carries an NPC persona rather than a modusMentis). Per-modusMentis
        /// callers pass their own <c>ModusMentis.StyleInstruction</c> instead.
        /// </summary>
        private const string DefaultStyleInstruction = "Where it fits, a figure of speech (metaphor, comparison, imagery) or an inner feeling that suits your character is welcome.";

        /// <summary>
        /// Style clause for dialogue replies: a spoken line carries personality in its wording, not in a
        /// described tone. The broad <see cref="DefaultStyleInstruction"/> ("an inner feeling that suits
        /// your character is welcome") invites a small model to narrate its own manner ("…, you say with
        /// curiosity"), which is exactly what a spoken line must not do — so dialogue uses this instead.
        /// This clause is always in force for dialogue: a caller-supplied style is kept but this is
        /// appended after it — see <see cref="DialogueAnswerInstructionFor"/>.
        /// </summary>
        private const string DialogueStyleInstruction = "Let your wording and personality colour the words themselves; do not describe your tone, expression, or manner of speaking.";

        /// <summary>
        /// The setting + place reminder that closes every answer instruction below. Its text and the
        /// scene's place live in <see cref="Cathedral.Game.Narrative.SceneSetting"/>; it is restated
        /// per request because the system prompt's copy of the rule is too far up the context to hold
        /// a small model on its own.
        /// </summary>
        private static string SettingReminder => Cathedral.Game.Narrative.SceneSetting.Reminder();

        /// <summary>
        /// Returns the full answer instruction, appending a character reminder from PersonaReminder2 when available.
        /// Falls back to "Stay in character." when no reminder is provided. The rewrite is emitted as raw
        /// text (constrained by GBNF), so there is no "respond in JSON" clause.
        /// </summary>
        public static string AnswerInstructionFor(string? personaReminder2, string? styleInstruction = null, bool includeLengthClause = true)
        {
            string style = string.IsNullOrWhiteSpace(styleInstruction) ? DefaultStyleInstruction : styleInstruction.Trim();
            string character = personaReminder2 != null ? $"Stay in the character of {personaReminder2}." : "Stay in character.";
            var parts = new[]
            {
                includeLengthClause ? OneSentenceClause : null,
                GroundingClause,
                style,
                // The kinds that drop the one-sentence clause still get a ceiling, right before the
                // closing character reminder.
                includeLengthClause ? null : ShortTextClause,
                character,
                SettingReminder,
            };
            return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        /// <summary>
        /// The footer for an address to a companion during narration. It is
        /// <see cref="DialogueAnswerInstructionFor"/>, because the two are the same act: words one
        /// character says to another, generated behind the same <c>I say to X : "…"</c> frame.
        /// <para>
        /// It used to carry the pronoun rules, the shape ("no narration, no third-person phrasing")
        /// and a one-sentence length clause on top of a full grounding clause. The first two the
        /// spoken prompt now states beside the grammar that enforces them, and a second wording of a
        /// rule reads to a small model as a second rule. The length clause had to go outright: the
        /// address is generated as one utterance of three merged sentences, so "answer in one short
        /// sentence and stop" contradicted the line it was attached to.
        /// </para>
        /// </summary>
        public static string SpeakingAnswerInstructionFor(string? personaReminder2, string? styleInstruction = null)
            => DialogueAnswerInstructionFor(personaReminder2, styleInstruction);

        /// <summary>
        /// The one sentence of guidance a dialogue reply gets: whose voice to choose the words in, and
        /// the setting they belong to. It is a clause of <c>PersonaRewriter.BuildDialoguePrompt</c>'s
        /// "Rewrite the description as the exact words X says to Y" line, not a paragraph of its own.
        /// <para>
        /// Deliberately shorter than every other kind's. It used to carry the reply's shape, the
        /// pronouns and the ban on narrating one's own speech — all of which the dialogue prompt already
        /// said in different words — and then a length clause and a grounding clause on top. Two
        /// wordings of one rule read to a small model as two rules, and the pile made the dialogue prompt
        /// the longest in the game while the replies got worse. The shape lives in the prompt alone, next
        /// to the grammar that enforces it; the length clause is gone (a spoken line is as long as it
        /// needs to be, and the grammar bounds it); and grounding is one short clause rather than the
        /// full <see cref="GroundingClause"/>, since the description is right there to be faithful to.
        /// </para>
        /// </summary>
        public static string DialogueAnswerInstructionFor(string? personaReminder2, string? styleInstruction = null)
        {
            // A dialogue line is spoken words, so the dialogue style clause is always in force — not just
            // as a default. A caller-supplied style (a modusMentis's, say) is written for narration, and a
            // small model reads it as licence to describe its own manner: MURMUR's "use hushed images of
            // whisper and undertone" produced «"I whisper it softly, 'that sounds like more…'"» — the real
            // line nested inside a narrated frame. Appending the dialogue clause after the caller's style
            // keeps the flavour and cancels the invitation to narrate.
            string style = string.IsNullOrWhiteSpace(styleInstruction)
                ? DialogueStyleInstruction
                : $"{styleInstruction.Trim()} {DialogueStyleInstruction}";
            string character = personaReminder2 != null
                ? $"Speak as {personaReminder2} would."
                : "Speak in your own character.";
            // No grounding clause here: the dialogue prompt's own "the meaning must not change" sentence
            // sits next to the line it applies to, and a second wording of the same rule reads to a small
            // model as a second requirement.
            return $"{character} {style} {SettingReminder}";
        }
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
        // Nothing here names the model or the hardware, deliberately.
        //
        //   which model  — always models/model.gguf (LlamaRuntime.ModelFileName). Changing models
        //                  means replacing that file; there is no setting and no path to edit.
        //   which device — measured once by LlamaProbe and stored in UserSettings, where the
        //                  player can override it from the Settings screen. A constant cannot
        //                  know whether this machine has a working GPU.
        //
        // What remains below is sampling: properties of the prompts and the prose we want out of
        // them, which is a content decision and belongs in code.

        // Sampling parameters (narrative generation and constrained single-token requests)
        public const int GenerationMaxTokens = 512;

        // Maximum context window size in tokens passed to the llama.cpp server (-c flag)
        public const int ContextSize = 2048;
        public const double Temperature = 0.7;
        public const int TopK = 12;
        public const double TopP = 0.95;

        // Anti-repetition. Passed to the llama.cpp server at launch (not per request), so these
        // apply to every call — narration, dialogue and constrained single-token alike. That reach is
        // the thing to keep in mind: the windows are the llama.cpp defaults (--repeat-last-n 64,
        // --dry-penalty-last-n -1 = whole context), and neither is set here, so all three look at the
        // PROMPT as well as at what has been generated. Our prompts are written to be echoed — the
        // option labels a choice must name back, the sentence PersonaRewriter must re-express — so any
        // penalty raised here is aimed partly at vocabulary the game itself supplied.
        //   RepeatPenalty   — llama.cpp default 1.0 (off), and left off deliberately. It is a FLAT
        //                     divisor on any token seen in the last 64, so ordinary function words in
        //                     the prompt tail take the same hit as a looping phrase. At the usual mild
        //                     1.1 that measurably cost grammar: ~3% of persona-choice answers (10/330,
        //                     qwen2.5-3b at the sampler above) dropped the "to" out of "I want to …"
        //                     and returned "I want focus on the muck heap". At 1.0 and 1.05 it was
        //                     0/420. Turning it off cost nothing it was there for — repeated 4-grams
        //                     in long narration stayed at 0.0%, the same as with it on, because DRY
        //                     already covers looping (all three off gives 0.5%).
        //   FrequencyPenalty— llama.cpp default 0.0; usual range 0.1-0.3. Above that a 3B model
        //                     starts avoiding words it ought to repeat (names, a recurring object).
        //                     Measured clean of the effect above (0/60 at 0.2 with repeat off).
        //   DryMultiplier   — default 0.0 (off); 0.8 is the recommended on-value. Penalises
        //                     repeated *sequences* rather than tokens, so it curbs verbatim
        //                     looping without flattening vocabulary. Set to 0 to disable. This is what
        //                     carries anti-repetition now, and it is also clean of the effect (0/60).
        public const double RepeatPenalty = 1.0;
        public const double FrequencyPenalty = 0.2;
        public const double DryMultiplier = 0.8;

        // Temperature for utility requests (health-check, prompt pre-caching)
        public const double UtilityTemperature = 0.1;

        // GPU offloading and thread count used to live here as constants (-ngl 99, -t 6). Both are
        // now in UserSettings, decided per machine by LlamaProbe and overridable in the Settings
        // screen. The -ngl 99 in particular was actively harmful once a GPU backend was present:
        // it means "put all layers in VRAM" and overrides llama.cpp's own --fit, which is on by
        // default and sizes the offload to the card. See LlamaServerManager.BuildServerArguments.
    }
    
    #endregion
}
