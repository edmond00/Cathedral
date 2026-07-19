namespace Cathedral.Game.Npc.Naming;

/// <summary>
/// Word pools for beast names (wolf, bear, boar, …). Beasts get a single descriptive
/// "XY" generator (<see cref="NameGenerator.GenerateBeast"/>) — an adjective glued to an
/// anatomical/behavioural noun: Sharptooth, Greyfur, Scarredmaw, Blackclaw.
/// </summary>
public static class BeastNameData
{
    /// <summary>First half of a beast name (Sharp-, Grey-, Scarred-, Old-).</summary>
    public static readonly string[] Adjectives =
    {
        "Sharp", "Grey", "Lean", "Scarred", "Old", "Black", "White", "Red",
        "Brown", "Swift", "Silent", "Grim", "Broad", "Long", "Iron", "Ragged",
        "Pale", "Dark", "Wild", "Fierce", "Cruel", "Hollow", "Torn", "Bristle",
        "Frost", "Ash", "Storm", "Night", "Blood", "Dusk", "Shadow", "Bramble",
        "Gnarled", "Rime", "Yellow", "Amber", "Dun", "Brindle", "Tawny", "Mangy",
    };

    /// <summary>Second half of a beast name (-tooth, -fur, -claw, -maw).</summary>
    public static readonly string[] Nouns =
    {
        "tooth", "teeth", "fur", "claw", "fang", "maw", "eye", "hide", "pelt",
        "paw", "hackle", "muzzle", "snout", "tail", "mane", "jaw", "gorge",
        "growl", "howl", "shadow", "hunger", "prowl", "stride", "leap", "bane",
        "ripper", "render", "stalker", "biter", "gnasher", "runner", "hunter",
        "back", "flank", "throat", "brow", "horn", "tusk", "bristle", "shank",
    };
}
