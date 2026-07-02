namespace Cathedral.Game.Narrative.Work;

/// <summary>
/// A job a master or reeve can hire the player into. A job is an abstract exchange of the player's
/// time (in months) for coins and modus-mentis experience — not a specific task or quest.
///
/// Pay is fixed per job: <see cref="CoinsPerMonth"/> is a float so a rate below one coin per month
/// can be expressed (e.g. 0.25 = one coin every four months). Coins earned for a stint of
/// <c>months</c> is <c>floor(CoinsPerMonth * months)</c>.
///
/// Each job trains exactly three modi mentis, listed most-defining first. The XP a stint grants is
/// front-loaded onto the first skill (see <see cref="WorkOutcome"/>).
/// </summary>
public sealed class Job
{
    /// <summary>Stable identifier (e.g. "bellows_hand").</summary>
    public string Id { get; }

    /// <summary>Display title used in the request verbatim (e.g. "bellows-hand").</summary>
    public string Title { get; }

    /// <summary>Coin denomination this job pays in.</summary>
    public CoinType PayCoin { get; }

    /// <summary>Coins of <see cref="PayCoin"/> earned per month worked (may be fractional).</summary>
    public float CoinsPerMonth { get; }

    /// <summary>The three modus-mentis ids this job trains, ordered by importance (first is most defining).</summary>
    public string[] ModusMentisIds { get; }

    public Job(string id, string title, CoinType payCoin, float coinsPerMonth, string[] modusMentisIds)
    {
        Id             = id;
        Title          = title;
        PayCoin        = payCoin;
        CoinsPerMonth  = coinsPerMonth;
        ModusMentisIds = modusMentisIds;
    }

    /// <summary>The indefinite article ("a"/"an") for this job's title.</summary>
    public string Article =>
        Title.Length > 0 && "aeiou".Contains(char.ToLowerInvariant(Title[0])) ? "an" : "a";

    /// <summary>"a bellows-hand" / "an oven-firer" — for the request verbatim.</summary>
    public string WithArticle() => $"{Article} {Title}";
}
