namespace Cathedral.Game;

/// <summary>
/// Defines the different game modes in the Location Travel Mode system.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Displayed while the LLM model is still loading on startup.
    /// A full-screen loading screen with progress bar is shown until the server is ready.
    /// </summary>
    LLMLoading,

    /// <summary>
    /// Main menu shown at startup or when ESC is pressed in WorldView.
    /// GlyphSphere is darkened, Terminal shows menu buttons.
    /// </summary>
    MainMenu,

    /// <summary>
    /// Player is viewing the 3D glyph sphere world, can click locations to travel.
    /// GlyphSphere is interactive, Terminal is hidden or minimal.
    /// </summary>
    WorldView,
    
    /// <summary>
    /// Protagonist is actively moving from one location to another.
    /// GlyphSphere shows path animation, Terminal may show travel info.
    /// Input is limited during this state.
    /// </summary>
    Traveling,
    
    /// <summary>
    /// Protagonist has arrived at a location and is interacting via Terminal HUD.
    /// Terminal is prominent with action choices, GlyphSphere is visible but not interactive.
    /// Player makes choices via mouse clicks on terminal text.
    /// </summary>
    LocationInteraction,
    
    /// <summary>
    /// Player is creating/configuring their protagonist before starting the game.
    /// Terminal shows body art with interactive organ-part score adjustment.
    /// </summary>
    ProtagonistCreation,
    
    /// <summary>
    /// Player is managing their protagonist/companions from the main menu.
    /// Terminal shows tabbed interface: body/organs viewer, inventory, journal.
    /// </summary>
    ProtagonistManagement,

    /// <summary>
    /// Standalone dialogue system demo (--dialogue CLI flag).
    /// Runs a scripted NPC conversation for testing the dialogue subsystem.
    /// </summary>
    DialogueDemo,

    /// <summary>
    /// Protagonist is engaged in turn-based combat within the narrative.
    /// Fight system runs on the main terminal; narrative resumes when fight ends.
    /// </summary>
    Fighting,

    /// <summary>
    /// Protagonist is in dialogue with an NPC within the narrative.
    /// Dialogue system runs on the main terminal; narrative resumes when dialogue ends.
    /// </summary>
    Dialogue
}
