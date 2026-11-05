using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// A modus mentis with the <see cref="ModusMentisFunction.Will"/> function wants one thing, and at
/// every stage of the chain it holds an office in, it wants only that: a short path of actions to its
/// <see cref="ModusMentis.Will"/> target (<see cref="ActionPathPlanner"/>), and of that path, only the
/// first step. Observing, it attends by the shortest path alone; thinking, it accepts a slightly
/// longer one through the object it is asked about.
/// <list type="bullet">
/// <item>observing, it is never the one that opens a phase while any other observer is held; given a
///       focus, it keeps it only when the object offers the path's first step, and otherwise turns to
///       the object that does (<c>ObservationPhaseController</c>);</item>
/// <item>thinking, it is offered no goal but the first step of a way to it through what the object
///       offers — a way of at most <see cref="Config.Narrative.WillMaxPathSteps"/> actions, or one as short
///       as the place's shortest however long — and of those, only the shortest; between equally short
///       ones the will chooses (<see cref="WaysOn"/>, <c>ThinkingExecutor</c>). On an object offering no
///       such way it proposes nothing. A step on the way has its means judged like any goal's, and is
///       tried again with another means when one refuses it;</item>
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
        => TargetsOf(mm, scene, pov, actor) is { } will ? ActionPathPlanner.Plan(scene, pov, actor, will) : null;

    /// <summary>
    /// The target this visit was made for (<see cref="Scene.Scene.WillPursuit"/>), when it is this
    /// will's and can still be reached from here; null otherwise — no pursuit, another will's, or the
    /// target met or gone, after which the will wants all its targets again as on any visit.
    /// </summary>
    public static WillTargets? PursuedOf(ModusMentis mm, Scene.Scene scene, PoV pov, PartyMember actor)
    {
        if (scene.WillPursuit is not { } p || p.ModusMentisId != mm.ModusMentisId || mm.Will is not { } will) return null;
        if (will.FirstOrDefault(t => t.Describe() == p.TargetKey) is not { } target) return null;
        WillTargets pursued = target;
        return ActionPathPlanner.Plan(scene, pov, actor, pursued) != null ? pursued : null;
    }

    /// <summary>
    /// What the will steers by here: the pursued target alone when this visit was made for one of its
    /// targets (<see cref="PursuedOf"/>) — its main target, however far — else every target it has.
    /// The plan, the phase opener and a focus all go by this.
    /// </summary>
    public static WillTargets? TargetsOf(ModusMentis mm, Scene.Scene scene, PoV pov, PartyMember actor)
        => PursuedOf(mm, scene, pov, actor) ?? mm.Will;

    /// <summary>
    /// The shortest path to the will that begins with something <paramref name="anchor"/> — an observed
    /// object — offers, or null when nothing it offers begins one. Not only the plan's own first step:
    /// of two people equally near to strike, either is on the way (<see cref="ActionPathPlanner.PathThrough"/>).
    /// </summary>
    public static ActionPath? PathOn(NarrativeAnchor anchor, ModusMentis mm, Scene.Scene scene, PoV pov,
                                     PartyMember actor, ActionPath path)
        => anchor is ObservationObject o
            ? ActionPathPlanner.PathThrough(scene, pov, actor, TargetsOf(mm, scene, pov, actor)!, path, o.SubOutcomes.OfType<VerbAction>())
            : null;

    /// <summary>
    /// The ways to the will a thinking mind may take through what an object offers — the shortest of
    /// those within reach, one per first action (<see cref="ActionPathPlanner.WaysThrough"/>). Empty when
    /// the object offers none, or the will cannot be done in this place at all.
    ///
    /// <para>On a visit made for one of its targets, that target comes first: a way to it through the
    /// object is taken however long the place makes it, and only when the object offers none is a way
    /// to the will's other targets looked for — and then only one of at most
    /// <see cref="Config.Narrative.WillMaxPathSteps"/>, never "as short as the place's shortest".</para>
    /// </summary>
    public static IReadOnlyList<ActionPath> WaysOn(ModusMentis mm, Scene.Scene scene, PoV pov, PartyMember actor,
                                                   IEnumerable<VerbAction> offered)
    {
        var actions = offered.ToList();
        if (PursuedOf(mm, scene, pov, actor) is { } pursued)
        {
            var main = ActionPathPlanner.Plan(scene, pov, actor, pursued)!;
            var ways = ActionPathPlanner.WaysThrough(scene, pov, actor, pursued, main, actions);
            if (ways.Count > 0) return ways;
            var others = WillTargets.Of(mm.Will!.Where(t => t.Describe() != pursued[0].Describe()).ToArray());
            if (others.Count == 0 || ActionPathPlanner.Plan(scene, pov, actor, others) is not { } rest)
                return Array.Empty<ActionPath>();
            return ActionPathPlanner.WaysThrough(scene, pov, actor, others, rest, actions,
                                                 reachCap: Config.Narrative.WillMaxPathSteps);
        }
        return PathOf(mm, scene, pov, actor) is { } path
            ? ActionPathPlanner.WaysThrough(scene, pov, actor, mm.Will!, path, actions)
            : Array.Empty<ActionPath>();
    }

    /// <summary>Whether <paramref name="goal"/> is the act the will wants, or the first step of a way to it it would take.</summary>
    public static bool OnItsWay(ModusMentis mm, Scene.Scene scene, PoV pov, PartyMember actor, VerbAction goal)
        => ActionPathPlanner.IsGoal(mm.Will!, goal, actor) || WaysOn(mm, scene, pov, actor, new[] { goal }).Count > 0;

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

        if (!WillPriority.OnItsWay(ctx.ModusMentis, ctx.Scene, ctx.PoV, ctx.Actor, goal)) return offered;

        if (!ctx.Probe)
            Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] wills it — no refusal offered for its own path.");
        var assent = offered.WithoutDecline() with { ImposedRefusal = null };
        return WillPriority.ActsItself(ctx.ModusMentis) && ActionPathPlanner.IsGoal(ctx.ModusMentis.Will!, goal, ctx.Actor)
               && assent.Stances.Contains(EagerStance)
            ? assent with { Stances = new[] { EagerStance } }
            : assent;
    }

    private const string EagerStance = "eager to do it";
}
