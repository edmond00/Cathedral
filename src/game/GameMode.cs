namespace Cathedral.Game;

/// <summary>
/// Defines the different game modes in the Location Travel Mode system.
/// </summary>
public enum GameMode
{
    /// <summary>
    /// Player is viewing the 3D glyph sphere world, can click locations to travel.
    /// GlyphSphere is interactive, Terminal is hidden or minimal.
    /// </summary>
    WorldView,
    
    /// <summary>
    /// Avatar is actively moving from one location to another.
    /// GlyphSphere shows path animation, Terminal may show travel info.
    /// Input is limited during this state.
    /// </summary>
    Traveling,
    
    /// <summary>
    /// Avatar has arrived at a location and is interacting via Terminal HUD.
    /// Terminal is prominent with action choices, GlyphSphere is visible but not interactive.
    /// Player makes choices via mouse clicks on terminal text.
    /// </summary>
    LocationInteraction
}
