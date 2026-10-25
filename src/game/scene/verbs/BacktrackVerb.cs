using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Goes back by a way already known — to any area already visited that known ways reach from here,
/// chosen on the location map once the act has succeeded (<see cref="Scene.BacktrackTo"/>), or, for a
/// will's step, the one its path needs (the action's variant, <see cref="Settle"/>).
///
/// <para>Offered on a way out the party knows (<see cref="AreaVisits.IsWayBack"/>): a neighbour
/// already stood in, a door, stair, path or climb already gone by. On such a way it <b>replaces</b>
/// the verb that would walk it (<see cref="Verb.MovesAlongWay"/>): a way known is gone back by, not
/// explored again. It never passes a door locked against the comer, and never steps from public into
/// private ground, so the private rooms it reaches are only those of the private ground the party
/// stands in.</para>
///
/// <para>Difficulty 2, one above walking to a neighbour, whatever the distance: the price of crossing
/// several areas in one act, as a path's.</para>
/// </summary>
public class BacktrackVerb : Verb
{
    /// <summary>What going back teaches depends on what it was done under.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Hostile == ThreatLevel.Visual) yield return Mm<WithdrawalModusMentis>();
        if (ctx.Night) yield return Mm<NightwalkingModusMentis>();
        if (ctx.IsPrivate) yield return Mm<HousewaysModusMentis>();
        foreach (var m in base.Lessons(ctx)) yield return m;
    }

    public override string VerbId         => "backtrack";
    public override string DisplayName    => "Backtrack";
    public override int    BaseDifficulty => 2;

    /// <summary>Losing the way one thought one knew, or the hour, or the thread.</summary>
    public override IReadOnlyList<FailurePenalty> FailurePenalties(Element? target) => new FailurePenalty[]
    {
        FailurePenalty.GetLost, FailurePenalty.WasteTime, FailurePenalty.LoseFocus,
    };

    /// <summary>Walking a known way. Nothing carried makes the memory of it better.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>What a success teaches on an ordinary day.</summary>
    public override string? GrantedModusMentisId(Element? target) => "retracing";

    /// <summary>A beast goes back by its scent, a person by the ways remembered — the beast's first, see <see cref="Verb.GrantedModusMentisIds"/>.</summary>
    public override IReadOnlyList<string> GrantedModusMentisIds(Element? target)
        => new[] { "backtrail", "retracing" };

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => AreaVisits.IsWayBack(scene, pov.Where, target, pov.When)
           && AreaVisits.BackReach(scene, pov.Where, pov.When).Count > 0;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"go back by {DefiniteTarget(target)}, the way I know";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => Reports(scene, pov, scene.BacktrackTo);

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target,
                                                         VerbAction view, Item? tool = null)
        => Reports(scene, pov, scene.BacktrackTo ?? view.Variant as Area);

    private static IReadOnlyList<Outcome> Reports(Scene scene, PoV pov, Area? to)
    {
        var reach = AreaVisits.BackReach(scene, pov.Where, pov.When);
        if (to == null || !reach.ContainsKey(to)) to = reach.Keys.FirstOrDefault();
        return to == null ? System.Array.Empty<Outcome>() : new Outcome[] { new AreaMoveOutcome(to) };
    }

    /// <summary>A will's step settled on the area its path needs, which nobody is asked to choose.</summary>
    public override VerbAction? Settle(VerbAction action, object settled)
        => settled is Area to ? To(action, to) : null;

    /// <summary><paramref name="action"/>, settled on going back to <paramref name="to"/>.</summary>
    public static VerbAction To(VerbAction action, Area to)
        => new(action.Verb, $"go back to {NeutralNarration.AreaPhrase(to.DisplayName)}, the way I know", action.Target, to)
           {
               ContextLabel = action.ContextLabel,
           };
}
