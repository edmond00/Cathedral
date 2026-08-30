namespace Cathedral.Game.Narrative.Work;

/// <summary>
/// A job a master or reeve can hire the player into. A job is an abstract exchange of the player's
/// time (in days) for coins and modus-mentis experience — not a specific task or quest.
///
/// Pay is fixed per job and expressed as <see cref="DaysPerCoin"/> — how long the player must work
/// to earn a single coin of <see cref="PayCoin"/>. Stating the rate this way keeps the low-paying
/// village trades readable (a bellows-hand earns one silver every 120 days) without fractional
/// per-day rates. Coins earned for a stint of <c>days</c> is <c>floor(days / DaysPerCoin)</c>.
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

    /// <summary>Days that must be worked to earn one coin of <see cref="PayCoin"/>.</summary>
    public float DaysPerCoin { get; }

    /// <summary>The three modus-mentis ids this job trains, ordered by importance (first is most defining).</summary>
    public string[] ModusMentisIds { get; }

    public Job(string id, string title, CoinType payCoin, float daysPerCoin, string[] modusMentisIds)
    {
        Id             = id;
        Title          = title;
        PayCoin        = payCoin;
        DaysPerCoin    = daysPerCoin;
        ModusMentisIds = modusMentisIds;
    }

    /// <summary>The indefinite article ("a"/"an") for this job's title.</summary>
    public string Article =>
        Title.Length > 0 && "aeiou".Contains(char.ToLowerInvariant(Title[0])) ? "an" : "a";

    /// <summary>"a bellows-hand" / "an oven-firer" — for the request verbatim.</summary>
    public string WithArticle() => $"{Article} {Title}";
}
