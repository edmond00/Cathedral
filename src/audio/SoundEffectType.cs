namespace Cathedral.Audio;

/// <summary>
/// Sound effects fired on player interactions.
/// All routed through the same MIDI engine for coherence.
/// </summary>
public enum SoundEffectType
{
    /// <summary>Short click — button press, menu navigation.</summary>
    ButtonClick,

    /// <summary>Softer confirm — selecting a narrative option.</summary>
    MenuSelect,

    /// <summary>Rising tone — text/narrative is being revealed.</summary>
    NarrativeReveal,

    /// <summary>Ascending figure — transitioning up (e.g. entering a mode).</summary>
    TransitionUp,

    /// <summary>Descending figure — transitioning down (e.g. exiting a mode).</summary>
    TransitionDown,

    /// <summary>Low resonant note — a childhood memory surfaces.</summary>
    MemoryFragment,
}
