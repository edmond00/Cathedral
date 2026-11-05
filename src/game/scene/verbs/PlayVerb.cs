using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;

using Cathedral.Game.Narrative.ModiMentis;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Plays at something for the fun of it — skips stones off a creek, swings out on a liana, rolls down
/// a grassy bank, strokes a stray cat. The target's <see cref="PlayActivity"/> says everything else.
///
/// <para>The only verb that does not word itself. What a play is differs too much from one object to
/// the next for one sentence to carry it, so the verbatim, the difficulty, what the body needs, what
/// a success teaches and what a failure costs are all the play's own (<see cref="IPlayable"/>), and
/// this class reads them off.</para>
///
/// <para>A success changes nothing in the world: no item, no hour, no place. Its whole effect is the
/// lesson, which is why <c>SuccessReports</c> is empty and <c>--outcome-audit</c> lists it with the
/// senses. <c>--play-audit</c> measures where it is offered (a quarter of the areas a player
/// walks into is the aim).</para>
/// </summary>
public class PlayVerb : Verb
{
    public override string VerbId         => "play";
    public override string DisplayName    => "Play";
    public override int    BaseDifficulty => 2;

    /// <summary>The plaything is the object itself — a stone, a rope, a bank. Nothing held changes how a game goes.</summary>
    public override ToolUsage ToolUse => ToolUsage.Excluded;

    /// <summary>What a play teaches when nothing more particular fits the body: playfulness.</summary>
    public override string? GrantedModusMentisId(Element? target) => "playfulness";

    /// <summary>The play's own lessons, then the object's declaration, then playfulness.</summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Target is IPlayable { Play: { } play })
            foreach (var m in play.Lessons) yield return m;

        // The target's own declaration, then this verb's default — always last, always visible.
        foreach (var m in base.Lessons(ctx)) yield return m;
    }

    public override int DifficultyFor(Element? target)
        => target is IPlayable { Play: { } play } ? play.Difficulty : BaseDifficulty;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (target is not IPlayable { Play: { } play }) return false;
        if (actor != null && !actor.Can(play.RequiredCapabilities)) return false;

        return target switch
        {
            PointOfInterest poi => pov.Where.PointsOfInterest.Contains(poi),

            // A creature: alive, here now, and not one that counts this body an enemy — nobody strokes
            // a dog that is going for their throat.
            SceneNpc npc => npc.IsAlive
                            && !TargetIsAlreadyHostile(npc, actor)
                            && scene.GetNpcsAt(pov.Where, pov.When).Any(n => n.Id == npc.Id),

            _ => false,
        };
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
    {
        if (target is not IPlayable { Play: { } play }) return "play";
        // A person or a named beast by pronoun, introduced on the attention line as every NPC verb's
        // target is; shallow livestock — a hen, a lamb — is "it", which NpcPronoun cannot tell.
        string thing = target switch
        {
            SceneNpc { Entity: NpcEntity } => NpcPronoun(target),
            SceneNpc                       => "it",
            _                              => DefiniteTarget(target),
        };
        return play.Verbatim(thing);
    }

    /// <summary>Nothing but the lesson.</summary>
    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
        => System.Array.Empty<Outcome>();

    public override IReadOnlyList<FailurePenalty> FailurePenalties(Element? target)
        => target is IPlayable { Play: { } play } ? play.FailurePenalties : LoseFocusOnly;
}
