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

/// <summary>
/// Postprocess audio filters that layer on top of the ambient music.
/// Designed for UI phases of indeterminate length (loading screens, dice rolls).
/// Activate with <see cref="AmbianceEngine.SetFilter"/>.
/// <para>
/// <see cref="Loading"/>, <see cref="DiceRoll"/> and <see cref="Traveling"/> generate no music
/// of their own: they leave the ambient loop that was already playing completely alone — same
/// tempo, same volume, same key — and add only untuned noise on top. Only <see cref="Fighting"/>
/// still ducks and re-tempos the music and contributes tonal layers.
/// </para>
/// </summary>
public enum MusicFilter
{
    /// <summary>No filter — normal music playback.</summary>
    None,

    /// <summary>
    /// Generic loading-screen filter.
    /// Adds a brown-noise wash and nothing else — the sound of something being computed.
    /// The ambient music continues underneath exactly as it was.
    /// </summary>
    Loading,

    /// <summary>
    /// Dice-roll filter.
    /// Adds a brown-noise wash and nothing else; the ambient music continues underneath unchanged.
    /// The tumbling of the dice is the per-tick PCM click fired by the roll animation
    /// (<c>DiceRollComponent.OnDiceTick</c> → <see cref="GameEventType.SmallInteraction"/>), the
    /// same sound used for hovers and preview-text updates — this filter adds no percussion of its own.
    /// </summary>
    DiceRoll,

    /// <summary>
    /// Travel filter.
    /// Open-road mood, made entirely of untuned sound: a brown-noise wash (wind on the road)
    /// and sparse quiet footstep-percussion (Bass Drum / Low Tom) marking the walking rhythm.
    /// The ambient music continues underneath unchanged, so the location's mood travels with you.
    /// </summary>
    Traveling,

    /// <summary>
    /// Fight filter.
    /// Tense-suspense overlay: tremolo strings carry a slow heartbeat pulse,
    /// a high dissonant PadBowed drone (minor-2nd clusters) hovers above the
    /// scale, and sparse low timpani thuds mark the rhythm of dread. The
    /// ambient music is ducked but not silenced, so the location mood still
    /// bleeds through underneath.
    /// </summary>
    Fighting,
}
