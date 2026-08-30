using System.Collections.Generic;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Contract for an anatomy factory. Each anatomy type (Human, Beast) has one implementation
/// that knows how to build the correct body parts, organs, derived stats and wound class map.
/// </summary>
public interface IAnatomyFactory
{
    AnatomyType AnatomyType { get; }

    /// <summary>
    /// What this anatomy is able to do beyond its organs — speech, fine manipulation, abstract
    /// thought. The single declaration point: a modus mentis or a verb states what it <i>requires</i>,
    /// and a body either has it or does not. A new anatomy adds one line here and needs no content
    /// edited anywhere else.
    /// </summary>
    AnatomyCapability Capabilities { get; }

    /// <summary>Create the full body part hierarchy (with default organ scores).</summary>
    List<BodyPart> CreateBodyParts();

    /// <summary>Create the full list of derived stats for this anatomy.</summary>
    List<DerivedStat> CreateDerivedStats();

    /// <summary>
    /// Map from single-char wound id to wound class instance.
    /// Used to pick debug wounds and to register wound types for body-art loading.
    /// </summary>
    Dictionary<char, Wound> GetWoundClassMap();

    /// <summary>
    /// Mapping from visual zone names (zone_*) found in visual_zones.csv
    /// to the canonical BodyPart.Id values used in the C# hierarchy.
    /// </summary>
    Dictionary<string, string> GetZoneToBodyPartMapping();
}
