using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// All context needed to build the item-use critic preamble: location, node, epoch, and goal.
/// </summary>
public class CriticContext
{
    public NarrationNode Node { get; }
    public WorldContext WorldContext { get; }
    public int LocationId { get; }
    public string GoalDescription { get; }

    public CriticContext(NarrationNode node, WorldContext worldContext, int locationId, string goalDescription)
    {
        Node = node;
        WorldContext = worldContext;
        LocationId = locationId;
        GoalDescription = goalDescription;
    }

    /// <summary>
    /// When set, this item context string ("ItemName (description)") is appended to
    /// every critic preamble so the judgment is framed around the item in use.
    /// </summary>
    public string? CombinedItemContext { get; set; } = null;

    /// <summary>
    /// What the act is done to, as its factory describes it (<see cref="CriticTarget.Describe"/>), or
    /// null. Whether a blade can bite into a wall depends on the wall: "the grove house wall" says
    /// nothing, "coursed stone with gaps enough for fingers and boot-toes" says everything.
    /// </summary>
    public string? TargetDescription { get; set; } = null;

    /// <summary>
    /// Builds the shared preamble injected at the top of every item-use question: the setting, the
    /// scene, and the thing acted on. Written in third person so the critic judges as an exterior
    /// observer.
    ///
    /// <para>The goal and the item are not here: the question states each once. The preamble used to
    /// repeat the goal ("The character's goal is to…" and then "The character wants to…") and describe
    /// the area rather than the target, so the critic judged a dagger against "the grove house wall"
    /// with no idea it was coursed stone with gaps for fingers. It is also identical for both questions
    /// a verdict asks, which lets the server reuse it from its prompt cache for the second.</para>
    /// </summary>
    public string BuildPreamble()
    {
        string worldDesc = WorldContext.GenerateContextDescription(LocationId);
        string nodeDesc  = Node.GenerateEnrichedContextDescription(LocationId);
        var lines = new List<string>
        {
            "Setting: an old world of iron and hand labour, before engines and firearms.",
            $"The scene: a {worldDesc}. The character is {nodeDesc}.",
        };
        if (!string.IsNullOrWhiteSpace(TargetDescription))
            lines.Add($"What they act on: {TargetDescription.Trim().TrimEnd('.')}.");
        return string.Join("\n", lines);
    }
}

/// <summary>
/// The thing an act is done to, described for the item-use critic.
/// </summary>
public static class CriticTarget
{
    /// <summary>
    /// The target's own first description, as its factory wrote it. A thing lying in the scene (an
    /// <see cref="Scene.ItemElement"/>) is described with what holds it — "a speckled green pear …, from
    /// a pear tree with … hanging fruit" — since where it sits is what decides whether a crook or a
    /// pruning hook helps to take it. People are not described: a description may carry a name the
    /// player has not been given, and the goal already names them by role.
    /// </summary>
    public static string? Describe(Scene.Scene? scene, Scene.Element? target)
    {
        static string? First(Scene.Element e) =>
            e.Descriptions.FirstOrDefault(d => !string.IsNullOrWhiteSpace(d))?.Trim().TrimEnd('.');

        switch (target)
        {
            case null:
            case Scene.SceneNpc:
                return null;
            case Scene.ItemElement item:
            {
                string? own = First(item);
                var holder = scene?.AllAreas.SelectMany(a => a.PointsOfInterest)
                                   .FirstOrDefault(p => p.Items.Any(i => i.Id == item.Id));
                string? where = holder == null ? null : First(holder);
                if (own == null) return where;
                return where == null ? own : $"{own}, from {char.ToLowerInvariant(where[0])}{where[1..]}";
            }
            default:
                return First(target);
        }
    }
}

/// <summary>
/// Factory for the Item-Use Critic's evaluation trees. The critic LLM is now scoped to judging
/// whether an item helps accomplish an action; all other adjudication
/// (plausibility, difficulty, wounds, witnesses, threats) has moved to the modus-mentis persona-fit
/// enum and to deterministic coded rules. A pure helper used by the difficulty display also lives here.
/// </summary>
public static class CriticTrees
{
    #region Difficulty helper (no LLM)

    /// <summary>
    /// Converts a 1–10 difficulty level to a 0.0–1.0 score used for success probability.
    /// Level 1 → 0.0, level 10 → 1.0.
    /// </summary>
    public static double DifficultyLevelToScore(int level) =>
        (Math.Clamp(level, 1, 10) - 1) / 9.0;

    #endregion

    #region Item Appropriateness Tree

    /// <summary>
    /// Asks the critic whether a combined item can help realise an act that does not require one, and
    /// to what degree. Two questions, weighed together by <c>ItemUseCritic.WeighAsync</c>:
    ///
    /// <list type="bullet">
    /// <item><b>this node</b>, the question as it has always been asked — "compared to bare hands, does
    /// X give a meaningful advantage?" — which gives the verdict: the degree, or the refusal;</item>
    /// <item><b>its counterweight</b>, "could X do any real part of this, even clumsily?", whose only
    /// part is its weight in deciding the side.</item>
    /// </list>
    ///
    /// <para><b>Why the counterweight.</b> Asked alone, the bare-hands question refused a third of the
    /// sensible improvisations in <c>tools/prompts/critic_survey.py</c> — a dagger driven into a stone
    /// wall's gaps, a staff over quicksand, a shield as a sled — whatever band the hand was, and so the
    /// bands never came into play for them. Asked alone, the lenient question let those through and a
    /// fifth of the wrong items with them (a tally stick through brambles): a small model can always
    /// imagine a clumsy use. They err on different cases, so the verdict's side is the sum of the two
    /// questions' log-odds. The lenient question cannot give the degree — "even clumsily" drew nearly
    /// all its answers to the clumsy degree, a hammer to break a door among them — so the degree stays
    /// this node's. Each "yes" of the lenient question names a physical means (edge, point, weight,
    /// length, grip), because "in a way it was not made for" and "an unusual use" let anything through.
    /// Numbers: <c>tools/prompts/NOTES.md</c>, "The item critic and improvised tools".</para>
    ///
    /// <para>The three non-failure choices are <b>degrees, not a pass</b>: which of them the acting
    /// body may act upon is decided afterwards by its hands, in
    /// <c>ToolCombinationRules.VerdictClears</c> — clearly at Low, plausibly at Medium, detoured at
    /// High. The critic judges the implement and knows nothing of who is holding it, which is what
    /// keeps this question answerable at all.</para>
    ///
    /// <para><b>A new choice id must be added to that threshold table</b>, or it fails closed and
    /// no body will ever clear it. The counterweight must use this node's ids.</para>
    /// </summary>
    public static CriticNode BuildItemAppropriatenessTree(string goalText, string itemName, CriticContext context)
    {
        string head = Head(goalText, itemName, context, referenceToolPhrase: null);
        return new CriticNode(
            name: "ItemAppropriateness",
            question: $"{head}\n\nCompared to attempting this with bare hands, does {itemName} provide a meaningful advantage for this action?",
            choices: new List<CriticChoice>
            {
                new("clearly_helps",    "the item provides a clear, direct advantage over bare hands"),
                new("plausibly_helps",  "the item offers a real but modest advantage over bare hands"),
                new("detoured_use",     "the item could help through creative use, though barely more than bare hands"),
                new("cannot_help",      "the item offers no meaningful advantage over bare hands for this action", isFailure: true, errorMessage: CannotReason),
                new("makes_no_sense",   "using this item here makes no sense compared to bare hands",             isFailure: true, errorMessage: SenselessReason),
            })
        {
            Counterweight = new CriticNode(
                name: "ItemAppropriatenessLenient",
                question: $"{head}\n\nCould {itemName} do any real part of this, even clumsily?",
                choices: new List<CriticChoice>
                {
                    new("clearly_helps",   "yes: it is the kind of thing this is done with, or near enough"),
                    new("plausibly_helps", "yes: a different thing, but its shape and strength would do the work properly"),
                    new("detoured_use",    "yes, clumsily: its edge, point, weight, length or grip would do part of the work, badly"),
                    new("cannot_help",     "no: nothing about its shape, edge, weight or length would do any of this work",
                        isFailure: true, errorMessage: CannotReason),
                    new("makes_no_sense",  "no: it has nothing to do with this work",
                        isFailure: true, errorMessage: SenselessReason),
                }),
        };
    }

    /// <summary>
    /// The refusals' reasons, first-person so they join the failure narration in the acting modus
    /// mentis's voice. They used to be asked for: a second request after every refusal, "explain why
    /// you think that way", which came back as a rationalisation of a verdict already given — "the
    /// dagger's blunt nature" of a double-edged, tapered blade — and was shown to the player as the
    /// cause. A fixed sentence per refusal says what the verdict says and costs nothing.
    /// </summary>
    private const string CannotReason    = "I think nothing about it could do any of this work.";
    private const string SenselessReason = "I think it has nothing to do with this work.";

    /// <summary>
    /// What both questions of a verdict share, word for word up to the question itself — the preamble,
    /// the goal, the reference tool where there is one, and the item described once — so that the
    /// server reads it once for the two.
    /// </summary>
    private static string Head(string goalText, string itemName, CriticContext context, string? referenceToolPhrase)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(context.BuildPreamble()).Append("\n\nThe character wants to ").Append(ThirdPerson(goalText)).Append('.');
        if (referenceToolPhrase != null)
            sb.Append("\nThis work is normally done with ").Append(referenceToolPhrase).Append('.');
        sb.Append("\nThey are holding: ").Append(context.CombinedItemContext ?? itemName).Append('.');
        return sb.ToString();
    }

    /// <summary>
    /// A verb's verbatim is written for the narration, in the first person ("make my way across the
    /// brambles", "catch the frog in my hands"); the critic judges a third party, and was asked whether
    /// "the character wants to make my way across".
    /// </summary>
    public static string ThirdPerson(string goal)
        => System.Text.RegularExpressions.Regex.Replace(
               System.Text.RegularExpressions.Regex.Replace(goal, @"\bmyself\b", "themselves"),
               @"\bmy\b", "their");

    #endregion

    #region Tool Substitution Tree

    /// <summary>
    /// Asks the LLM critic whether the item the player actually combined can serve as the tool a
    /// tool-gated verb calls for. Used instead of <see cref="BuildItemAppropriatenessTree"/> whenever
    /// the verb declares <c>ReferenceToolIds</c> — the question there is "is this better than bare
    /// hands", which is the wrong question when bare hands are not on the table at all.
    ///
    /// <para>The reference tool is named in the question so the critic judges against a standard
    /// rather than against nothing. Naming it is the whole difference: "could a rock hammer do the
    /// work of a pickaxe" is answerable, where "does a rock hammer help" is not.</para>
    ///
    /// <para>The first three choices are degrees rather than a pass, and are keyed to the same
    /// bands as the neutral tree's — <c>is_the_tool</c> with <c>clearly_helps</c> at Low,
    /// <c>serves_well</c> with <c>plausibly_helps</c> at Medium, <c>serves_poorly</c> with
    /// <c>detoured_use</c> at High. <c>serves_poorly</c> is deliberately not a refusal: improvising
    /// a tool should be allowed to a practised hand and then punished by the dice, not forbidden
    /// outright. See <c>ToolCombinationRules</c>.</para>
    ///
    /// <para><b>Asked as a stand-in, "even if less well".</b> Asked "can X do the work of Y here?", the
    /// critic read it as "is X a Y?" and refused every real substitute that was not nearly the tool
    /// itself: a sword to cut meat, a spear to fish, a sledgehammer at an ore seam — cannot_serve, in all
    /// of 20 samples. "How well could X do the work of Y" helped a little (69% -> 77% on the labelled
    /// cases of <c>tools/prompts/critic_bench.py</c>); asking whether X could be used in Y's place, even
    /// if less well, took it to 83%, with every case that should be refused still refused.</para>
    ///
    /// <para>It still refused a third of the improvised stand-ins of the survey (a flint to skin, a
    /// pitchfork to spear fish, a shield to scoop clay), so it carries the same counterweight as
    /// <see cref="BuildItemAppropriatenessTree"/>, for the same reasons.</para>
    ///
    /// <para><b>Its degree is asked, not read off its options.</b> As a five-option menu this question
    /// put nearly all its weight on <c>serves_poorly</c> for every stand-in it accepted — a dagger
    /// offered as a knife, a war axe as an axe, a heavy pick as a pick — so a Low hand could use none of
    /// the required verbs' stand-ins and a Medium hand one in 27; rewording the options ("about as well
    /// as the usual tool" / "properly, if not quite as well" / "only clumsily") moved nothing. Two plain
    /// questions answered yes or no do separate them (<see cref="CriticNode.DegreeQuestions"/>): is it
    /// a kind of the tool, or near enough (→ <c>is_the_tool</c>); would it do the work properly, if a
    /// little less well (→ <c>serves_well</c>); neither → <c>serves_poorly</c>. They are asked only of
    /// a pass this question itself accepted — one the counterweight carried is an improvisation, and
    /// clumsy without asking. On the survey's required verbs: usable by a Low hand 0 → 6 of 13, by a
    /// Medium hand 1 → 9 of 27, nothing new passing that should not. The second question still says no
    /// to most proper stand-ins (a machete for an axe, a chisel at a strongbox); two rewordings of it
    /// did no better.</para>
    /// </summary>
    public static CriticNode BuildToolSubstitutionTree(string goalText, string referenceToolPhrase,
                                                       string itemName, CriticContext context)
    {
        string head = Head(goalText, itemName, context, referenceToolPhrase);
        return new CriticNode(
            name: "ToolSubstitution",
            question: $"{head}\n\nCould {itemName} be used in place of {referenceToolPhrase} for this, even if less well?",
            choices: new List<CriticChoice>
            {
                new("is_the_tool",    "the item is that tool, or near enough to be the same thing"),
                new("serves_well",    "the item is a different thing but would do the work properly"),
                new("serves_poorly",  "the item could be made to work, clumsily and with difficulty"),
                new("cannot_serve",   "the item cannot do this work, however it is handled",
                    isFailure: true, errorMessage: CannotReason),
                new("makes_no_sense", "using this item for this work makes no sense at all",
                    isFailure: true, errorMessage: SenselessReason),
            })
        {
            Counterweight = new CriticNode(
                name: "ToolSubstitutionLenient",
                question: $"{head}\n\nCould {itemName} do any real part of this work in place of {referenceToolPhrase}, even clumsily?",
                choices: new List<CriticChoice>
                {
                    new("is_the_tool",    "yes: it is that tool, or near enough to be the same thing"),
                    new("serves_well",    "yes: a different thing, but its shape and strength would do the work properly"),
                    new("serves_poorly",  "yes, clumsily: its edge, point, weight, length or grip would do part of the work, badly"),
                    new("cannot_serve",   "no: nothing about its shape, edge, weight or length would do any of this work",
                        isFailure: true, errorMessage: CannotReason),
                    new("makes_no_sense", "no: it has nothing to do with this work",
                        isFailure: true, errorMessage: SenselessReason),
                }),
            // Asked as options, the degree came back serves_poorly for nearly every stand-in the strict
            // question accepted — a dagger offered as a knife, a war axe as an axe, fish-hooks to fish —
            // and no rewording of the options moved it. Asked as plain yes/no questions, the model
            // separates them: see the summary above.
            DegreeQuestions = new List<CriticDegreeQuestion>
            {
                new($"{head}\n\nIs {itemName} a kind of {referenceToolPhrase}, or near enough the same tool?", "is_the_tool"),
                new($"{head}\n\nWould {itemName} do this work properly, the way {referenceToolPhrase} does, if a little less well?", "serves_well"),
            },
        };
    }

    /// <summary>
    /// "a pickaxe" / "a fishing rod or a net" — the or-list of articled tool names for the critic
    /// question. Ids the registry does not know are printed as themselves rather than dropped, so a
    /// bad id shows up in the critic trace instead of silently narrowing the question.
    /// </summary>
    public static string ToolPhrase(IReadOnlyList<string> toolIds)
    {
        var names = toolIds
            .Select(id => ItemRegistry.Instance.All.FirstOrDefault(i => i.ItemId == id)?.WithArticle()
                          ?? id.Replace('_', ' '))
            .ToList();

        if (names.Count == 0) return "a proper tool";
        if (names.Count == 1) return names[0];
        return string.Join(", ", names.Take(names.Count - 1)) + " or " + names[^1];
    }

    #endregion

    // There was an Item Consumption Tree here, asked after every successful combination: "was this
    // item used up in the process?". It is gone, because the question no longer has two answers.
    //
    // What may be combined with an act is a tool, a weapon, or an item declaring itself made for
    // some single act — and none of those is spent by being used. A pick is not eaten by a seam. So
    // the tree spent a request per successful combination to arrive at "no" nearly every time, and
    // the times it did not were the interesting ones only in the sense that they were wrong: it
    // destroyed the knife a carcass had just been opened with often enough to matter.
    //
    // It was worse than a wasted request under --playground, where every critic choice is drawn at
    // random: a script combining a tool twice lost it to the first use about half the time, which
    // is a test failing for a reason with nothing to do with what it tested.
    //
    // Should a consumable ever become combinable, this comes back — but it comes back as a property
    // of the item, answerable without asking anybody.
}
