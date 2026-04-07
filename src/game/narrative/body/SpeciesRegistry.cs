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
}
