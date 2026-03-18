using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Anatomy factory for beast companions (wolf, dog, cat, bear, boar, …).
/// Produces four beast body parts, the shared derived stats (whose <c>IsUsable</c>
/// method will return false for stats tied to absent organs), and the beast wound class map.
/// Beast-specific derived stats (bite force, scratch power, …) will be added here in future.
/// </summary>
public sealed class BeastAnatomyFactory : IAnatomyFactory
{
    public static readonly BeastAnatomyFactory Instance = new();

    public AnatomyType AnatomyType => AnatomyType.Beast;

    public List<BodyPart> CreateBodyParts() => new()
    {
        new EncephalonBodyPart(),
        new MuzzleBodyPart(),
        new BeastTrunkBodyPart(),
        new LimbsBodyPart(),
    };

    /// <summary>
    /// Returns the same 20 shared derived stats.
    /// Stats tied to absent organs (genitories) will report <c>IsUsable = false</c>.
    /// </summary>
    public List<DerivedStat> CreateDerivedStats() => new()
    {
        // Memory capacity stats
        new WorkingMemoryCapacityStat(),
        new ProceduralMemoryCapacityStat(),
        new SemanticMemoryCapacityStat(),
        new SensoryMemoryCapacityStat(),
        new ResidualMemoryCapacityStat(),
        // Secretion percentage stats
        new HeparBloodSecretionStat(),        new HeparPhlegmSecretionStat(),
        new HeparYellowBileSecretionStat(),   new HeparBlackBileSecretionStat(),
        new PaunchBloodSecretionStat(),       new PaunchPhlegmSecretionStat(),
        new PaunchYellowBileSecretionStat(),  new PaunchBlackBileSecretionStat(),
        new PulmonesBloodSecretionStat(),     new PulmonesPhlegmSecretionStat(),
        new PulmonesYellowBileSecretionStat(),new PulmonesBlackBileSecretionStat(),
        new SpleenBloodSecretionStat(),       new SpleenPhlegmSecretionStat(),
        new SpleenYellowBileSecretionStat(),  new SpleenBlackBileSecretionStat(),
    };

    public Dictionary<char, Wound> GetWoundClassMap()
    {
        var dict = new Dictionary<char, Wound>();
        void Add(Wound w) => dict[w.WoundId] = w;

        Add(new BeastPiercedEyeLeftWound());          Add(new BeastPiercedEyeRightWound());
        Add(new BeastPerforatedEardrumLeftWound());    Add(new BeastPerforatedEardrumRightWound());
        Add(new BeastSkullFractureWound());            Add(new BeastConcussionsWound());
        Add(new BeastBrokenSnoutWound());              Add(new BeastBrokenFangWound());
        Add(new BeastTornedOutTongueWound());
        Add(new BeastBrokenBackboneWound());           Add(new BeastTailAmputeeWound());
        Add(new BeastBrokenRibsWound());
        Add(new BeastEviscerationWound());             Add(new BeastPiercedPaunchWound());
        Add(new BeastTornedOffFangWound());
        Add(new BeastBrokenLeftForelegWound());        Add(new BeastBrokenRightForelegWound());
        Add(new BeastCrippledLeftForelegWound());      Add(new BeastCrippledRightForelegWound());
        Add(new BeastBrokenLeftHindlegWound());        Add(new BeastBrokenRightHindlegWound());
        Add(new BeastCrippledLeftHindlegWound());      Add(new BeastCrippledRightHindlegWound());
        Add(new BeastBrokenLeftForeclawsWound());      Add(new BeastBrokenRightForeclawsWound());
        Add(new BeastTornOffLeftForeclawsWound());     Add(new BeastTornOffRightForeclawsWound());
        Add(new BeastBrokenRightHindclawsWound());     Add(new BeastBrokenLeftHindclawsWound());
        Add(new BeastTornOffRightHindclawsWound());    Add(new BeastTornOffLeftHindclawsWound());
        // Wildcards
        Add(new BeastContusionWound());
        Add(new BeastCutWound());
        Add(new BeastPunctureWound());

        return dict;
    }

    public Dictionary<string, string> GetZoneToBodyPartMapping() => new()
    {
        { "zone_encephalon",      "encephalon" },
        { "zone_muzzle",          "muzzle"     },
        { "zone_trunk",           "trunk"      },
        { "zone_left_forelimbs",  "limbs"      },
        { "zone_right_forelimbs", "limbs"      },
        { "zone_left_hindlimbs",  "limbs"      },
        { "zone_right_hindlimbs", "limbs"      },
    };
}
