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
    /// The first screen of a new run: the world sphere is not drawn (and not yet generated), the
    /// star sphere stands alone, and the player picks the world to play by clicking one of the moons
    /// in it. Confirming seeds the run and generates the world; cancelling returns to
    /// <see cref="MainMenu"/>.
    ///
    /// <para>Skipped when <c>--seed</c> pinned the world on the command line: the question has
    /// already been answered.</para>
    /// </summary>
    WorldSelection,

    /// <summary>
    /// Player is creating/configuring their protagonist before starting the game.
    /// Terminal shows body art with interactive organ-part score adjustment.
    /// </summary>
    ProtagonistCreation,
    
    /// <summary>
    /// Player is managing their protagonist/companions from the main menu.
    /// Terminal shows tabbed interface: anatomy/organs viewer, inventory, journal.
    /// </summary>
    ProtagonistManagement,

    /// <summary>
    /// Standalone dialogue system demo (--dialogue CLI flag).
    /// Runs a scripted NPC conversation for testing the dialogue subsystem.
    /// </summary>
    DialogueDemo,

    /// <summary>
    /// Played once per run, immediately after <see cref="ProtagonistCreation"/>:
    /// the protagonist sits exhausted at the foot of a tree and recovers their childhood
    /// in a chain of reminescences. Each REMEMBER fragments grants the run's first skills
    /// and items; when the chain ends the run drops into <see cref="WorldView"/>.
    /// </summary>
    ChildhoodReminescence,

    /// <summary>
    /// Short intermediate scene between childhood reminiscence and world exploration.
    /// The protagonist rests exhausted under a lone tree; the only action is GET UP.
    /// No noetic cost, no failure penalty (loop back), difficulty forced to 1.
    /// </summary>
    GetUp,

    /// <summary>
    /// Protagonist is engaged in turn-based combat within the narrative.
    /// Fight system runs on the main terminal; narrative resumes when fight ends.
    /// </summary>
    Fighting,

    /// <summary>
    /// Protagonist is in dialogue with an NPC within the narrative.
    /// Dialogue system runs on the main terminal; narrative resumes when dialogue ends.
    /// </summary>
    Dialogue,

    /// <summary>
    /// Protagonist is in a buy/sell menu with an NPC merchant, reached by succeeding a
    /// propose-to-buy/sell dialogue. Trade UI runs on the main terminal; narrative resumes
    /// when the menu is closed.
    /// </summary>
    Trading,

    /// <summary>
    /// Protagonist is in a work menu with a master or reeve, reached by succeeding a request-job
    /// dialogue. Work UI runs on the main terminal; narrative resumes when the menu is closed.
    /// </summary>
    Working,

    /// <summary>
    /// The protagonist has died. Shows a purple death screen with cause of death
    /// and an "End Run" button that returns to the main menu.
    /// </summary>
    Death,

    /// <summary>
    /// A travel encounter has been rolled. Shows a purple message box describing
    /// the threat with an "Engage" button; clicking it transitions to Fighting.
    /// </summary>
    EncounterPrompt,

    /// <summary>
    /// Settings screen reached from the main menu. Shows music and sound-effects
    /// volume controls; a Back button returns to <see cref="MainMenu"/>.
    /// </summary>
    Settings
}
