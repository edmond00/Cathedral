namespace Cathedral.Game.Npc.Naming;

/// <summary>
/// Word pools for great-reptile names (crocodile, carnotaurus, stegosaurus, …), glued together the
/// beast's way (<see cref="NameGenerator.GenerateReptile"/>): Mudjaw, Oldscale, Stonehorn. Kept apart
/// from <see cref="BeastNameData"/> because half of that one is fur, paws and manes.
/// </summary>
public static class ReptileNameData
{
    /// <summary>First half of a reptile name (Old-, Mud-, Sun-, Stone-).</summary>
    public static readonly string[] Adjectives =
    {
        "Old", "Scarred", "Mud", "Sun", "Stone", "Slow", "Cold", "Green",
        "Grey", "Black", "Pale", "Long", "Broad", "Iron", "Moss", "Silt",
        "Dust", "Ash", "Ochre", "Sand", "Reed", "Deep", "Still", "Grim",
        "Hollow", "Knotted", "Bone", "Copper", "Rust", "Dun", "Torn", "Ancient",
    };

    /// <summary>Second half of a reptile name (-scale, -horn, -jaw, -tail).</summary>
    public static readonly string[] Nouns =
    {
        "scale", "hide", "horn", "spike", "tail", "jaw", "maw", "eye",
        "back", "crest", "plate", "club", "tooth", "belly", "brow", "frill",
        "basker", "sleeper", "lasher", "gorer", "crusher", "wader", "stalker", "tread",
        "shell", "thorn", "ridge", "gullet", "coil", "drowner", "roller", "bane",
    };
}
