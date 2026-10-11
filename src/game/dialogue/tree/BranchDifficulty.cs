namespace Cathedral.Game.Dialogue.Tree;

/// <summary>
/// Turns a branch's <b>depth</b> (how many player replies it took to reach the resolution) into the
/// authored difficulty of that resolution.
///
/// <para>
/// The dice pool grows with depth — every chosen reply adds its Modus Mentis level
/// (<c>DialogueTreeController</c>) — so a fixed difficulty would make long branches nearly free.
/// Difficulty therefore rises with depth, but slightly more slowly than the pool does, so committing
/// to a longer exchange stays modestly the better play instead of being a pure tax.
/// </para>
///
/// <para>
/// Three ladders. <see cref="Simple"/> for the conversations that are a formality — an introduction,
/// asking to buy or sell. <see cref="Easy"/> for asks the player should usually manage, and
/// <see cref="Hard"/> for the ones with real stakes (reconciling with an enemy, asking for work,
/// talking your way out of a crime).
/// </para>
///
/// <para>
/// <b>Easy and Hard sit above the pool's other sources.</b> The replies are not the only dice: the
/// listener's regard, a flattering garment and a fair face all add to the pool
/// (<c>DialogueTreeController.StartResolution</c>), so a ladder tuned to the replies alone was won
/// nearly every time. Both carry a fixed <see cref="Raise"/> over where they began; Simple does not.
/// </para>
/// </summary>
public static class BranchDifficulty
{
    /// <summary>Sixes added to Easy and Hard over the replies-only ladders they started as.</summary>
    public const int Raise = 2;

    /// <summary>Formalities: 2 replies → 1 six, 3 → 2, 4 → 2.</summary>
    public static int Simple(int depth) => depth <= 2 ? 1 : 2;

    /// <summary>Ordinary asks: 2 replies → 3 sixes, 3 → 4, 4 → 4.</summary>
    public static int Easy(int depth) => Simple(depth) + Raise;

    /// <summary>Stakes on the table: 2 replies → 4 sixes, 3 → 4, 4 → 5.</summary>
    public static int Hard(int depth) => (depth >= 4 ? 3 : 2) + Raise;
}
