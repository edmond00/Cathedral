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
/// Two ladders: <see cref="Easy"/> for conversations the player should usually win (small talk,
/// opening a trade), and <see cref="Hard"/> for the ones with real stakes (reconciling with an
/// enemy, asking for work, talking your way out of a crime). Both start where the hand-authored
/// difficulties used to sit at depth 2, so nothing gets harder than it was before.
/// </para>
/// </summary>
public static class BranchDifficulty
{
    /// <summary>Small talk and low-stakes asks: 2 replies → 1 six, 3 → 2, 4 → 2.</summary>
    public static int Easy(int depth) => depth <= 2 ? 1 : 2;

    /// <summary>Stakes on the table: 2 replies → 2 sixes, 3 → 2, 4 → 3.</summary>
    public static int Hard(int depth) => depth >= 4 ? 3 : 2;
}
