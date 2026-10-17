namespace Cathedral.Game.Dialogue.Affinity;

/// <summary>
/// What kind of crime a witness saw. Derived from the verb at the moment of the catch and used to
/// word the confrontation — what the witness accuses you of, at every level of the tree.
///
/// <para>Not stored. A per-NPC criminal record existed here once and was written, escalated and
/// cleared without a single reader; the consequence of being caught is the confrontation and what it
/// leads to, which is a fight and a lasting enmity, not a label filed against your name.</para>
/// </summary>
public enum CriminalAffinityType
{
    /// <summary>No witnessed crime.</summary>
    None,

    /// <summary>Witnessed stealing an item from a private area or owned object.</summary>
    Thief,

    /// <summary>Witnessed trespassing in a private area without consent.</summary>
    Intruder,

    /// <summary>Witnessed attacking or slaying an innocent.</summary>
    Murderer,

    /// <summary>
    /// Witnessed wrecking someone's property. Distinct from <see cref="Thief"/> because nothing was
    /// taken and the damage stays visible: a witness to a broken loom is looking at the broken loom
    /// for as long as it stands there.
    /// </summary>
    Vandal,
}

/// <summary>
/// Which <see cref="CriminalAffinityType"/> a witness accuses somebody of, from the act they caught
/// or heard. Only reached once the act is already established as a crime, so this names the kind
/// rather than re-deciding it — see <see cref="Cathedral.Game.Scene.Verbs.Verb.IsIllegal"/>.
///
/// <para>The violent verbs all read as <c>Murderer</c>, including a bare <c>attack</c>: what the
/// witness reacts to is somebody setting about a living thing, and the accusation is worded from
/// that. Anything unlisted done inside a private area is trespass, which is what makes an otherwise
/// innocuous verb a crime there in the first place. Gated on the verb's type, never its id: a renamed
/// id would otherwise fall through to the default without a word.</para>
/// </summary>
public static class CrimeKinds
{
    public static CriminalAffinityType Of(Cathedral.Game.Scene.Verbs.Verb verb, bool areaIsPrivate) => verb switch
    {
        Cathedral.Game.Scene.Verbs.StealVerb or Cathedral.Game.Scene.Verbs.PickpocketVerb
            => CriminalAffinityType.Thief,
        Cathedral.Game.Scene.Verbs.GrabVerb
            => areaIsPrivate ? CriminalAffinityType.Thief : CriminalAffinityType.None,
        Cathedral.Game.Scene.Verbs.SlayVerb or Cathedral.Game.Scene.Verbs.MurderVerb or Cathedral.Game.Scene.Verbs.AttackVerb
            => CriminalAffinityType.Murderer,
        Cathedral.Game.Scene.Verbs.UnlockDoorVerb or Cathedral.Game.Scene.Verbs.SlipIntoVerb or Cathedral.Game.Scene.Verbs.StalkVerb
            => CriminalAffinityType.Intruder,
        Cathedral.Game.Scene.Verbs.BreakVerb
            => CriminalAffinityType.Vandal,
        _   => areaIsPrivate ? CriminalAffinityType.Intruder : CriminalAffinityType.None,
    };
}
