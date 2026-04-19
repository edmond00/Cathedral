using System.Collections.Generic;

namespace Cathedral.Game.Scene;

/// <summary>
/// A group of related <see cref="Area"/>s within a <see cref="Scene"/>.
/// For example "Flatlands" groups Grassland and Meadow areas.
/// </summary>
public class Section : Element
{
    public override string DisplayName { get; }
    public override List<string> Descriptions { get; }

    /// <summary>Areas belonging to this section.</summary>
    public List<Area> Areas { get; } = new();

    public Section(string name, List<string> descriptions)
    {
        DisplayName  = name;
        Descriptions = descriptions;
    }
}
