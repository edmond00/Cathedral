using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Scene;

namespace Cathedral.Game.Npc.Corpse;

/// <summary>
/// A point of interest representing a harvestable body part on a <see cref="CorpseSpot"/>.
/// Items inside this PoI require the <c>cut</c> verb rather than the <c>grab</c> verb.
/// Examples: muzzle with fangs, body with hide, limb with claws.
/// </summary>
public class CorpseBodyPartPoI : PointOfInterest
{
    public CorpseBodyPartPoI(
        string displayName,
        List<string> descriptions,
        List<KeywordInContext> keywords,
        List<ItemElement>? items = null)
        : base(displayName, descriptions, keywords, items)
    { }
}
