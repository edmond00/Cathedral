namespace Cathedral.Game.Scene;

/// <summary>
/// What a discrete modus mentis buys, and what it does not. Shared by the coded action rules and the
/// failure handler so witnesses (illegal actions) and threats (enemies) are judged identically.
///
/// <para><b>Discreteness silences you through a wall, not in the same room.</b> Someone one room away
/// is working from what they can hear, and quiet work gives them nothing: Audio drops to None. Someone
/// standing in the room with you is working from what they can see, and no amount of quiet changes
/// that: Visual stays Visual.</para>
///
/// <para>That is a change from the old one-step-downgrade rule, which turned a Visual watcher into an
/// Audio one and so let a discreet skill fail in front of somebody and pay an Audio price for it.
/// The two questions are now separate, and only the second one lives here:</para>
///
/// <list type="number">
/// <item><b>May I act at all?</b> Asked by the coded rules against the <i>raw</i> proximity. A Visual
///   watcher blocks a non-discrete modus mentis outright; a discrete one may always attempt. This is
///   what discreteness is for — permission to try under observation.</item>
/// <item><b>What does failing cost?</b> Asked here, against the <i>effective</i> proximity:
///   <list type="bullet">
///   <item>effective Visual → caught on the spot: the confrontation (witness) or the fight (threat);</item>
///   <item>effective Audio → they heard something and come to look: the NPC moves into the area,
///     becoming a Visual presence from the next observation phase;</item>
///   <item>effective None → nothing happened that anyone can act on.</item>
///   </list></item>
/// </list>
///
/// <para>So a discreet skill is the only one that may work in front of an audience, and it pays the
/// full price when it slips there; away from the room it is not heard at all.</para>
/// </summary>
public static class ProximityModel
{
    /// <summary>Effective witness proximity after applying the modus mentis's discreteness.</summary>
    public static WitnessType Effective(WitnessType raw, bool discrete)
    {
        if (!discrete) return raw;
        return raw switch
        {
            WitnessType.Audio => WitnessType.None,   // not heard through a wall
            _                 => raw,                // seen is seen
        };
    }

    /// <summary>Effective threat proximity after applying the modus mentis's discreteness.</summary>
    public static ThreatLevel Effective(ThreatLevel raw, bool discrete)
    {
        if (!discrete) return raw;
        return raw switch
        {
            ThreatLevel.Audio => ThreatLevel.None,
            _                 => raw,
        };
    }
}
