namespace Cathedral.Game.Scene;

/// <summary>
/// The "discreteness downgrades proximity by one step" model shared by the coded action rules and
/// the failure handler. A discrete modus mentis is one step harder to notice, so an observer's
/// effective proximity drops one level: Visual → Audio, Audio → None. A non-discrete modus mentis
/// keeps the raw level.
///
/// The effective level then decides behavior uniformly for witnesses (illegal actions) and threats
/// (enemies):
///   • effective Visual → the action is blocked outright by a coded rule (impossible);
///   • effective Audio  → the action proceeds, but a failure triggers the consequence
///                        (caught-red-handed confrontation for a witness, a fight for a threat);
///   • effective None   → no effect.
/// </summary>
public static class ProximityModel
{
    /// <summary>Effective witness proximity after applying the modus mentis's discreteness.</summary>
    public static WitnessType Effective(WitnessType raw, bool discrete)
    {
        if (!discrete) return raw;
        return raw switch
        {
            WitnessType.Visual => WitnessType.Audio,
            WitnessType.Audio  => WitnessType.None,
            _                  => WitnessType.None,
        };
    }

    /// <summary>Effective threat proximity after applying the modus mentis's discreteness.</summary>
    public static ThreatLevel Effective(ThreatLevel raw, bool discrete)
    {
        if (!discrete) return raw;
        return raw switch
        {
            ThreatLevel.Visual => ThreatLevel.Audio,
            ThreatLevel.Audio  => ThreatLevel.None,
            _                  => ThreatLevel.None,
        };
    }
}
