using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// A modus mentis with the <see cref="ModusMentisFunction.Will"/> function wants one thing, and at
/// every stage of the chain it holds an office in, it wants only that: the shortest path of actions
/// to its <see cref="ModusMentis.Will"/> target (<see cref="ActionPathPlanner"/>), and of that path,
/// only the first step.
/// <list type="bullet">
/// <item>observing, it is never the one that opens a phase while any other observer is held; given a
///       focus, it keeps it only when the object offers the path's first step, and otherwise turns to
///       the object that does (<c>ObservationPhaseController</c>);</item>
/// <item>thinking, it is offered no goal but the path's first step, and proposes nothing on an object
///       that does not offer it (<c>ThinkingExecutor</c>). A step on the way has its means judged like
///       any goal's, and is tried again with another means when one refuses it;</item>
/// <item>acting, when it bears the Action function it carries out the act it wants — the means is not
///       chosen — and never refuses a step of its own path (<see cref="WillActionNeverRefusesItsPathRule"/>);</item>
/// <item>speaking, it is any other modus mentis: one bearing the Speaking function replies in every
///       conversation, whatever its target, and one without it replies in none.</item>
/// </list>
/// When no path exists in this place, every stage refuses and says why: the will cannot be done here,
/// and the body would have to leave.
///
/// <para>Only in exploration. The childhood reminescence and the get-up phase have one verb each, and
/// a will that refused it would leave the player with no way onward.</para>
/// </summary>
public static class WillPriority
{
    /// <summary>Whether <paramref name="mm"/> chooses by its will here — a will, in an exploration phase.</summary>
    public static bool Steers(ModusMentis mm, Scene.Scene? scene)
        => mm.Will != null && scene?.Phase == NarrationPhase.Exploration;

    /// <summary>The will's path from this standing, or null when it cannot be done in this place.</summary>
    public static ActionPath? PathOf(ModusMentis mm, Scene.Scene scene, PoV pov, PartyMember actor)
        => mm.Will is { } will ? ActionPathPlanner.Plan(scene, pov, actor, will) : null;

    /// <summary>
    /// The shortest path to the will that begins with something <paramref name="anchor"/> — an observed
    /// object — offers, or null when nothing it offers begins one. Not only the plan's own first step:
    /// of two people equally near to strike, either is on the way (<see cref="ActionPathPlanner.PathThrough"/>).
    /// </summary>
    public static ActionPath? PathOn(NarrativeAnchor anchor, ModusMentis mm, Scene.Scene scene, PoV pov,
                                     PartyMember actor, ActionPath path)
        => anchor is ObservationObject o
            ? ActionPathPlanner.PathThrough(scene, pov, actor, mm.Will!, path, o.SubOutcomes.OfType<VerbAction>())
            : null;

    /// <summary>
    /// Whether a will carries its step out itself: it bears the Action function and the body can still
    /// use it. Otherwise the means is chosen as for any goal.
    /// </summary>
    public static bool ActsItself(ModusMentis mm) => mm.IsWill && mm.Functions.Contains(ModusMentisFunction.Action);
}

/// <summary>
/// An action modus mentis with a will does not refuse a step of its own path — the goal itself, or the
/// way to it. The refusal is withheld and any imposed one lifted: what the body is asked to do is what
/// this mind wants. Refusals of circumstance (a locked door, a watching witness) are action rules and
/// still apply.
///
/// <para>The act it wants, as against a step on the way, it is eager for, with no question asked: a mind
/// whose whole nature is wanting that act cannot be merely willing to do it (−1 difficulty).</para>
/// </summary>
public class WillActionNeverRefusesItsPathRule : IWillingnessRule
{
    public WillingnessOptions Filter(WillingnessOptions offered, ChoiceRuleContext ctx)
    {
        if (!WillPriority.Steers(ctx.ModusMentis, ctx.Scene)) return offered;
        if (ctx.Goal is not VerbAction goal) return offered;

        bool own = ctx.ModusMentis.Will!.Matches(goal)
                   || WillPriority.PathOf(ctx.ModusMentis, ctx.Scene, ctx.PoV, ctx.Actor) is { } path
                      && ActionPathPlanner.PathThrough(ctx.Scene, ctx.PoV, ctx.Actor, ctx.ModusMentis.Will!, path, new[] { goal }) != null;
        if (!own) return offered;

        if (!ctx.Probe)
            Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] wills it — no refusal offered for its own path.");
        var assent = offered.WithoutDecline() with { ImposedRefusal = null };
        return WillPriority.ActsItself(ctx.ModusMentis) && ctx.ModusMentis.Will!.Matches(goal)
               && assent.Stances.Contains(EagerStance)
            ? assent with { Stances = new[] { EagerStance } }
            : assent;
    }

    private const string EagerStance = "eager to do it";
}
