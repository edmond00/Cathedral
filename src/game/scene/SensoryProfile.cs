namespace Cathedral.Game.Scene;

/// <summary>
/// Which senses an object rewards being turned on it. A mushroom is worth examining and smelling; a
/// statue is worth examining and contemplating; birdsong is worth listening to and nothing else.
///
/// <para>This exists because the great majority of the game's authored objects — the anvil, the
/// loom, the trestle table, the ice formation — carried no verb at all. They read well and then did
/// nothing, which made every room a list of things to walk past. A sensory profile is the cheapest
/// possible way to give one an action: no items, no state, no connector, just a reason to stop.</para>
///
/// <para>Which modus mentis each sense teaches on this particular object is <b>not</b> declared here.
/// It goes in <see cref="PointOfInterest.VerbModiMentis"/>, keyed by verb id, so there is one
/// override mechanism rather than two — examining a mushroom teaches mycology through the same
/// channel that grabbing a lockpick would teach lockpicking.</para>
/// </summary>
/// <param name="Examine">Reward for looking closely and reasoning about it. The scientific sense.</param>
/// <param name="Contemplate">Reward for looking at it as a thing of beauty rather than of use.</param>
/// <param name="Listen">Reward for standing still and hearing it.</param>
/// <param name="Smell">Reward for its smell.</param>
public sealed record SensoryProfile(
    bool Examine = false,
    bool Contemplate = false,
    bool Listen = false,
    bool Smell = false)
{
    /// <summary>Worth a close look and nothing else — the commonest profile for tools and machinery.</summary>
    public static readonly SensoryProfile Examinable = new(Examine: true);

    /// <summary>Made, or grown, to be looked at: statues, carvings, flowers, a good view.</summary>
    public static readonly SensoryProfile Beautiful = new(Examine: true, Contemplate: true);

    /// <summary>Something that makes a noise: a stream, a beehive, a wind-shaken tree, a forge.</summary>
    public static readonly SensoryProfile Audible = new(Examine: true, Listen: true);

    /// <summary>Something that smells, pleasantly or otherwise: a midden, a bakery, hay, a pigsty.</summary>
    public static readonly SensoryProfile Odorous = new(Examine: true, Smell: true);

    /// <summary>Flowers and herbs — the archetypal object that rewards every sense but hearing.</summary>
    public static readonly SensoryProfile Fragrant = new(Examine: true, Contemplate: true, Smell: true);

    /// <summary>Living, growing, noisy and scented all at once: a beehive, a byre, a flowering hedge.</summary>
    public static readonly SensoryProfile FullyAlive = new(Examine: true, Contemplate: true, Listen: true, Smell: true);

    /// <summary>Whether <paramref name="verbId"/> is one of the four senses and this object rewards it.</summary>
    public bool Rewards(string verbId) => verbId switch
    {
        "examine"     => Examine,
        "contemplate" => Contemplate,
        "listen"      => Listen,
        "smell"       => Smell,
        _             => false,
    };
}
