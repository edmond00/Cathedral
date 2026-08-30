using System;
using System.Collections.Generic;
using Cathedral.Fight;

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

    /// <summary>
    /// Whether these areas are the inside of a building. Set only by <c>BuildingFactory</c>, which is
    /// the only thing that makes one.
    ///
    /// <para>Exists because outdoor content must not be scattered indoors. A bench in a hall is fine;
    /// a bramble thicket between a workshop store and a house pantry is not, and worse, it is a way
    /// between two buildings that walks straight past both their locked doors. Scene furnishing asks
    /// this rather than guessing from names.</para>
    /// </summary>
    public bool IsInterior { get; init; }

    /// <summary>
    /// Factory that produces the fight-arena generator used when a fight breaks out in
    /// this section. The caller passes a seed (e.g. the current Area.Id hash) and gets a
    /// fully-configured generator back. Mandatory so every section declares its own arena
    /// flavor — corridor/rooms inside buildings, geometric in villages, wave on mountains, etc.
    /// </summary>
    public Func<int, IFightAreaGenerator> ArenaGeneratorFactory { get; }

    public Section(
        string name,
        List<string> descriptions,
        Func<int, IFightAreaGenerator> arenaGeneratorFactory)
    {
        DisplayName  = name;
        Descriptions = descriptions;
        ArenaGeneratorFactory = arenaGeneratorFactory
            ?? throw new ArgumentNullException(nameof(arenaGeneratorFactory));
    }
}
