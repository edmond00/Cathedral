using System.Collections.Generic;

namespace Cathedral.Game.Scene.GetUp;

/// <summary>
/// A point of interest in the Get-Up scene representing one facet of the protagonist's
/// exhaustion: aching legs, bone-deep fatigue, or a discouraged spirit. All three share
/// the same available verb (GET UP) regardless of which PoI is focused.
/// </summary>
public sealed class GetUpPointOfInterest : PointOfInterest
{
    public GetUpPointOfInterest(string displayName, string description)
        : base(
            displayName: displayName,
            descriptions: new List<string> { description },
            items: null,
            moods: new[] { "heavy", "aching", "spent", "hollow", "leaden", "still", "quiet" })
    {
    }
}
