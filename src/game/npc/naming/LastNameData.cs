namespace Cathedral.Game.Npc.Naming;

/// <summary>
/// Word pools for the human last-name generators in <see cref="LastNameGenerator"/>.
///
/// <list type="bullet">
///   <item><see cref="Adjectives"/> — used in "the X" bynames (Godric the Strong) and compound
///     surnames' first half is drawn from <see cref="CompoundHeads"/>.</item>
///   <item><see cref="CompoundHeads"/> + <see cref="CompoundTails"/> — glued into "XY" surnames
///     (Blackwood, Strongbridge, Smallriver).</item>
///   <item><see cref="PlaceNouns"/> — the Y in "of the Y" bynames (of the Forest, of the Lake).</item>
/// </list>
/// </summary>
public static class LastNameData
{
    /// <summary>Bare adjectives for "the X" bynames (Godric <b>the Wise</b>).</summary>
    public static readonly string[] Adjectives =
    {
        "Black", "White", "Grey", "Red", "Brown", "Fair", "Dark", "Pale",
        "Strong", "Bold", "Wise", "Tall", "Short", "Lean", "Stout", "Swift",
        "Quiet", "Grim", "Gentle", "Proud", "Silent", "Cruel", "Kind", "Just",
        "Elder", "Young", "Old", "Lame", "Blind", "Deft", "Stern", "Mild",
        "Bright", "Keen", "Hardy", "Meek", "Wild", "Wary", "Cold", "Fierce",
        "Crooked", "Nimble", "Ready", "Restless", "Sober", "Solemn", "Steady",
        "Cunning", "Dour", "Sturdy",
    };

    /// <summary>First half of a compound surname (Black-, Strong-, Small-, Iron-).</summary>
    public static readonly string[] CompoundHeads =
    {
        "Black", "White", "Grey", "Red", "Green", "Small", "Long", "Broad",
        "Strong", "Old", "New", "High", "Low", "Deep", "Fair", "Cold", "Iron",
        "Stone", "Wild", "Swift", "Bright", "Sharp", "Hard", "Under", "Over",
        "Nether", "Har", "Wind", "Frost", "Ash", "Oak", "Thorn", "Raven",
        "Wolf", "Hart", "Fox", "Crow", "Hawk", "Bear", "Moor", "Mill", "Fen",
        "Hollow", "Brook", "Marsh", "Gold", "Rye", "Barley", "Hazel", "Elder",
    };

    /// <summary>Second half of a compound surname (-wood, -bridge, -river, -stone).</summary>
    public static readonly string[] CompoundTails =
    {
        "wood", "bridge", "river", "field", "stone", "ford", "brook", "hill",
        "dale", "bourne", "mere", "marsh", "moor", "well", "gate", "wall",
        "worth", "ridge", "combe", "shaw", "thorpe", "ton", "ham", "wick",
        "bury", "leigh", "cliff", "crest", "vale", "mead", "reed", "bank",
        "beck", "fell", "garth", "holt", "lund", "row", "side", "wold",
        "bird", "fowl", "hart", "horn", "bough", "root", "briar", "hedge",
        "helm", "shield",
    };

    /// <summary>Nouns for "of the Y" bynames (of the <b>Forest</b>, of the <b>Fen</b>).</summary>
    public static readonly string[] PlaceNouns =
    {
        // Woods & wild growth
        "Forest", "Wood", "Weald", "Wold", "Grove", "Copse", "Spinney", "Thicket",
        "Brake", "Holt", "Coppice", "Greenwood", "Deadwood",
        // Water
        "Lake", "Mere", "Tarn", "Pool", "Brook", "Beck", "Burn", "Rill", "Spring",
        "Well", "Weir", "Millpond", "Ford", "Bridge", "Ferry", "Marsh", "Fen",
        "Mire", "Bog", "Slough", "Moss",
        // High & low ground
        "Hill", "Vale", "Dale", "Glen", "Hollow", "Dell", "Combe", "Downs",
        "Moor", "Heath", "Ridge", "Crag", "Tor", "Bluff", "Fell", "Scarp",
        "Gorge", "Ravine", "Pass", "Cairn", "Barrow",
        // Coast
        "Coast", "Cliffs", "Cove", "Bay", "Sands", "Strand", "Shoals", "Headland",
        // Field & husbandry
        "Meadow", "Pasture", "Common", "Green", "Croft", "Warren", "Orchard",
        "Furlong", "Hedge", "Mill", "Quarry",
        // Marks & remnants
        "Reach", "Crossing", "Crossroads", "Waystone", "Milestone", "Shrine",
        "Chapel", "Hermitage", "Watchtower", "Ashes", "Ruins", "Standing Stones",
        "Old Road", "North March", "Hanging Oak", "Broken Bridge", "Sunken Lane",
        "Boundary Stone", "Blasted Heath", "Gallows",
    };
}
