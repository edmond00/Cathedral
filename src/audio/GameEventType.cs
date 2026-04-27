namespace Cathedral.Audio;

/// <summary>
/// Game-world events that trigger both a predetermined sound effect and a
/// short-term temporary effect on the background music.
/// <para>
/// Use <see cref="AmbianceEngine.TriggerGameEvent"/> to fire an event.
/// The SFX plays immediately; the musical effect is applied at the start of the
/// next Melody phrase so it never interrupts a note mid-playback.
/// </para>
/// </summary>
public enum GameEventType
{
    /// <summary>
    /// Subtle interaction: hovering a button, tooltip appearing, minor UI response.
    /// SFX: soft airy tap (PadNewAge, high register).
    /// Music: next melody phrase plays with a velocity accent (+20).
    /// </summary>
    SmallInteraction,

    /// <summary>
    /// Strong interaction: clicking a button, confirming a selection, opening a menu.
    /// SFX: sharp harpsichord stab (two-note fifth: A3+E4).
    /// Music: next melody phrase replays the previous phrase's contour (motif echo).
    /// </summary>
    StrongInteraction,

    /// <summary>
    /// Positive outcome: gaining an item, passing a skill check, unlocking something.
    /// SFX: ascending flute flourish (C4→E4→G4→C5).
    /// Music: mood briefly brightens (sadness -0.18, fear -0.12 for 5 s); accented phrase.
    /// </summary>
    PositiveOutcome,

    /// <summary>
    /// Negative outcome: taking damage, failing a skill check, losing something.
    /// SFX: dissonant tremolo-string thud (tritone A3+Eb4 simultaneously).
    /// Music: mood briefly darkens (sadness +0.15, fear +0.28 for 5 s); phrase breaks short.
    /// </summary>
    NegativeOutcome,

    /// <summary>
    /// Neutral outcome: entering a new location, discovering information, transitioning.
    /// SFX: resonant halo-pad bell tone (D4, sustained).
    /// Music: extra-long inter-phrase pause (contemplative breath ×3).
    /// </summary>
    NeutralOutcome,
}
