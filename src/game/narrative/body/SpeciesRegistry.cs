namespace Cathedral.Game.Narrative;

/// <summary>
/// Provides static access to all available species definitions.
/// </summary>
public static class SpeciesRegistry
{
    public static readonly Species Human = new HumanSpecies();
    public static readonly Species Wolf  = new WolfSpecies();
    public static readonly Species Fox   = new FoxSpecies();
    public static readonly Species Cat   = new CatSpecies();
    public static readonly Species Dog   = new DogSpecies();
    public static readonly Species Bear  = new BearSpecies();
    public static readonly Species Boar  = new BoarSpecies();

    // The hot and cold country's beasts. The white bear and white wolf are Bear and Wolf.
    public static readonly Species Lion        = new LionSpecies();
    public static readonly Species Tiger       = new TigerSpecies();
    public static readonly Species Jaguar      = new JaguarSpecies();
    public static readonly Species Puma        = new PumaSpecies();
    public static readonly Species SnowLeopard = new SnowLeopardSpecies();
    public static readonly Species Hyena       = new HyenaSpecies();
    public static readonly Species Warthog     = new WarthogSpecies();

    /// <summary>Every species, in declaration order.</summary>
    public static readonly Species[] All =
    {
        Human, Wolf, Fox, Cat, Dog, Bear, Boar,
        Lion, Tiger, Jaguar, Puma, SnowLeopard, Hyena, Warthog,
    };

    /// <summary>
    /// A stable key for <paramref name="species"/>, for saving. Keyed on the type name rather than on
    /// <see cref="Species.DisplayName"/>, which is player-facing prose and free to be reworded, and
    /// rather than on an <c>Id</c> property, which would mean editing every subclass to record
    /// something the type name already says.
    /// </summary>
    public static string IdOf(Species species) => species.GetType().Name;

    /// <summary>
    /// The species for a key from <see cref="IdOf"/>, or null when nothing matches — which for a save
    /// means the file was written by a build that had a species this one does not.
    /// </summary>
    public static Species? ById(string id) =>
        System.Array.Find(All, s => string.Equals(IdOf(s), id, System.StringComparison.Ordinal));
}
