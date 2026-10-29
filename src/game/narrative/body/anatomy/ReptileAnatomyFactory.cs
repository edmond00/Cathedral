using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Anatomy factory for the great reptiles (crocodile, giant monitor, carnotaurus, stegosaurus, …).
/// The beast's four regions and the beast's mind — no pineal gland, no speech, no hands, no letters —
/// with three differences: no claws, horns on the muzzle, a thagomizer at the end of the tail.
/// The wounds are the beast's minus the claws, plus the reptile's own four.
/// </summary>
public sealed class ReptileAnatomyFactory : IAnatomyFactory
{
    public static readonly ReptileAnatomyFactory Instance = new();

    public AnatomyType AnatomyType => AnatomyType.Reptile;

    /// <summary>None of the three, exactly as the beast: voice without language, feet without hands.</summary>
    public AnatomyCapability Capabilities => AnatomyCapability.None;

    public List<BodyPart> CreateBodyParts() => new()
    {
        new BeastEncephalonBodyPart(),
        new ReptileMuzzleBodyPart(),
        new ReptileTrunkBodyPart(),
        new ReptileLimbsBodyPart(),
    };

    public List<DerivedStat> CreateDerivedStats() => DerivedStat.DiscoverAll();

    public Dictionary<char, Wound> GetWoundClassMap()
    {
        var dict = new Dictionary<char, Wound>();
        void Add(Wound w) => dict[w.WoundId] = w;

        Add(new BeastPiercedEyeLeftWound());          Add(new BeastPiercedEyeRightWound());
        Add(new BeastPerforatedEardrumLeftWound());    Add(new BeastPerforatedEardrumRightWound());
        Add(new BeastSkullFractureWound());            Add(new BeastConcussionsWound());
        Add(new BeastBrokenSnoutWound());              Add(new BeastBrokenFangWound());
        Add(new BeastTornedOutTongueWound());          Add(new BeastTornedOffFangWound());
        Add(new ReptileBrokenHornWound());             Add(new ReptileTornOffHornWound());
        Add(new BeastBrokenBackboneWound());           Add(new BeastBrokenRibsWound());
        Add(new BeastEviscerationWound());             Add(new BeastPiercedPaunchWound());
        Add(new BeastPiercedHeartWound());
        Add(new ReptileBrokenSpikeWound());            Add(new ReptileSeveredTailWound());
        Add(new BeastBrokenLeftForelegWound());        Add(new BeastBrokenRightForelegWound());
        Add(new BeastCrippledLeftForelegWound());      Add(new BeastCrippledRightForelegWound());
        Add(new BeastBrokenLeftHindlegWound());        Add(new BeastBrokenRightHindlegWound());
        Add(new BeastCrippledLeftHindlegWound());      Add(new BeastCrippledRightHindlegWound());
        // Wildcards
        Add(new BeastContusionWound());
        Add(new BeastCutWound());
        Add(new BeastPunctureWound());
        Add(new BeastScrapeWound());
        Add(new BeastStrainWound());

        return dict;
    }

    public Dictionary<string, string> GetZoneToBodyPartMapping() =>
        BeastAnatomyFactory.Instance.GetZoneToBodyPartMapping();
}
