using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Anatomy factory for human characters.
/// Produces the five canonical human body parts, the 20 shared derived stats,
/// and the human wound class map.
/// </summary>
public sealed class HumanAnatomyFactory : IAnatomyFactory
{
    public static readonly HumanAnatomyFactory Instance = new();

    public AnatomyType AnatomyType => AnatomyType.Human;

    /// <summary>Speech, hands and letters — the three a beast has none of.</summary>
    public AnatomyCapability Capabilities =>
        AnatomyCapability.Speech | AnatomyCapability.Handcraft | AnatomyCapability.Abstraction;

    public List<BodyPart> CreateBodyParts() => new()
    {
        new EncephalonBodyPart(),
        new VisageBodyPart(),
        new TrunkBodyPart(),
        new UpperLimbsBodyPart(),
        new LowerLimbsBodyPart(),
    };

    public List<DerivedStat> CreateDerivedStats() => DerivedStat.DiscoverAll();

    public Dictionary<char, Wound> GetWoundClassMap()
    {
        var dict = new Dictionary<char, Wound>();
        foreach (var kv in WoundRegistry.All)
            dict[kv.Key] = kv.Value;
        return dict;
    }

    public Dictionary<string, string> GetZoneToBodyPartMapping() => new()
    {
        { "zone_encephalon", "encephalon" },
        { "zone_visage",     "visage"     },
        { "zone_trunk",      "trunk"      },
        { "zone_left_arm",   "upper_limbs" },
        { "zone_right_arm",  "upper_limbs" },
        { "zone_left_leg",   "lower_limbs" },
        { "zone_right_leg",  "lower_limbs" },
    };
}
