namespace Cathedral.Audio;

/// <summary>
/// The four layered tracks of the procedural ambiance engine.
/// Tracks are activated progressively as the game progresses through phases.
/// </summary>
public enum TrackRole
{
    /// <summary>Slow, sustained low notes. Church Organ or Choir Aahs. Always the first track active.</summary>
    Drone = 0,

    /// <summary>Sparse melodic line. Violin. Added at Protagonist Creation phase.</summary>
    Melody = 1,

    /// <summary>Counter-melody / upper voice. Flute. Added at Childhood Reminiscence phase.</summary>
    Counter = 2,

    /// <summary>High texture / decoration. Harpsichord. Added at WorldView phase.</summary>
    Texture = 3,
}
