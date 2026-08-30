using System;

namespace Cathedral.Game.Narrative;

/// <summary>
/// What a body is <b>able</b> to do, beyond what its organs mechanically are.
///
/// <para>Two different things stop a party member acting. The first is anatomy itself: a modus
/// mentis related to <c>fangs</c> or <c>hands</c> is out of reach for a body that has neither, and
/// that needs no authoring — the organ either resolves on the body or it does not. The second is
/// this: a wolf has a tongue and a cerebrum, so nothing structural stops it learning rhetoric, and
/// nothing structural stops it picking a lock. Someone has to say so.</para>
///
/// <para>Capabilities are declared <b>per anatomy</b> (on <see cref="IAnatomyFactory"/>) and
/// <b>required</b> per piece of content (<see cref="ModusMentis.RequiredCapabilities"/>,
/// <c>Verb.RequiredCapabilities</c>). Content that requires nothing — the default — is open to every
/// anatomy. That direction matters: adding an anatomy is one capability line and no content edits,
/// where a list of allowed anatomies on each item would mean revisiting every one of them.</para>
/// </summary>
[Flags]
public enum AnatomyCapability
{
    /// <summary>Requires nothing of the body beyond the organs already named. The default.</summary>
    None = 0,

    /// <summary>
    /// Can hold a conversation with a person: language, not merely voice. Gates every dialogue verb
    /// and every modus mentis carrying the Speaking function. A beast may still howl and snarl — those
    /// modi mentis claim no Speech — but it can never open a dialogue tree.
    /// </summary>
    Speech = 1 << 0,

    /// <summary>
    /// Fine manipulation: tools, locks, knots, carrying, picking, cutting. What separates a hand from
    /// a paw, and so what a beast cannot be asked for.
    /// </summary>
    Handcraft = 1 << 1,

    /// <summary>
    /// Letters, number, law, coin, doctrine — thought that stands on human institutions rather than on
    /// the senses. A beast can track, remember and scheme; it cannot do algebra or read a charter.
    /// </summary>
    Abstraction = 1 << 2,
}
