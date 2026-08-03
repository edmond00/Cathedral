using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// Kills somebody in their sleep.
///
/// <para>Difficulty 2 against <c>slay</c>'s 3, and that is the whole design: a sleeping person is
/// easier to kill than a waking one, and the game should say so plainly rather than pretending
/// otherwise. It is the one verb whose <i>advantage</i> is the condition of the victim.</para>
///
/// <para>Recorded as murder, like <c>slay</c>. Doing it at night in a locked house is not a mitigation
/// and the witness system does not treat it as one.</para>
/// </summary>
public class MurderVerb : Verb
{
    public override string VerbId         => "murder";
    public override string DisplayName    => "Murder";
    public override int    BaseDifficulty => 2;

    public override bool IsLegal => false;

    /// <summary>What a success teaches: doing lethal harm to somebody who could not answer it.</summary>
    public override string? GrantedModusMentisId(Element? target) => "foul_play";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
        => SleeperGate.Sleeper(scene, pov, target) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => "kill them where they lie";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => $"kill {SleeperGate.Name(target)} in their sleep";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var sleeper = SleeperGate.Sleeper(scene, pov, target);
        return sleeper == null
            ? System.Array.Empty<OutcomeReport>()
            : new OutcomeReport[] { new NpcSlaynOutcome(sleeper) };
    }

    /// <summary>
    /// A sleeper who wakes at the wrong moment fights back from arm's length, which is the worst
    /// range to be surprised at.
    /// </summary>
    public override IReadOnlyList<Wound?> FailurePenalties(Element? target) => new Wound?[]
    {
        null, null, new ContusionWound(), new CutWound(),
    };

    public override RoutinePhaseKind RoutineTriggeredPhase(Scene scene, PoV pov, Element target)
        => RoutinePhaseKind.Narration;
}

/// <summary>
/// Wakes somebody up — the only conversation a sleeping person will have.
///
/// <para>Every other dialogue verb refuses a sleeper (see <c>SleeperGate</c>), so at night in
/// somebody's bedroom this is the whole of what talking can be. Succeeding rouses them for the rest
/// of the visit and every normal conversation opens up; failing means they wake badly, which the
/// tree turns into a fight.</para>
/// </summary>
public class WakeUpVerb : DialogueVerb
{
    public override string VerbId         => "wake_up";
    public override string DisplayName    => "Wake";
    public override int    BaseDifficulty => 1;

    protected override string DialogueTreeId => "wake_up";

    public override bool IsPossible(Scene scene, PoV pov, Element target, Protagonist? actor = null)
    {
        var sleeper = SleeperGate.Sleeper(scene, pov, target);
        if (sleeper == null) return false;
        if (sleeper.Entity is not NpcEntity entity || !entity.CanSpeak) return false;

        // Whether the character can speak at all is ZeroRepliesDialogueRule's job, and it refuses
        // with an explanation. Gating it here too would make the action silently absent instead.
        return true;
    }

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => "wake them";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbView? view = null)
        => $"wake {SleeperGate.Name(target)}";

    public override IReadOnlyList<OutcomeReport> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var sleeper = SleeperGate.Sleeper(scene, pov, target);
        if (sleeper?.Entity is not NpcEntity npc) return System.Array.Empty<OutcomeReport>();

        return new OutcomeReport[]
        {
            new SleeperRousedOutcome(sleeper),
            new DialogueTriggerOutcome(npc, "wake_up"),
        };
    }
}

/// <summary>
/// The gate every other conversation goes through: you cannot bargain with, befriend, provoke or
/// beg from somebody who is asleep.
///
/// <para>A static helper rather than a base class because the dialogue verbs already derive from
/// <see cref="DialogueVerb"/> and each writes its own <c>IsPossible</c>; this is one line added to
/// each of them.</para>
/// </summary>
public static class SleeperGate
{
    /// <summary>True when the target is a sleeping person, and so not available for conversation.</summary>
    public static bool IsAsleep(Scene scene, PoV pov, Element target)
        => Sleeper(scene, pov, target) != null;

    /// <summary>
    /// The sleeping person behind a target, in either of the two forms one can arrive in.
    ///
    /// <para>While somebody is asleep, placement swaps them and their bed for a single
    /// <see cref="SleepingNpcPointOfInterest"/> — so a sleeper reaches these verbs as that merged
    /// object, not as a <see cref="SceneNpc"/>. Both are accepted because the other dialogue verbs
    /// still ask "is this NPC asleep?" of a live NPC target, and because a scene that has not yet
    /// been re-placed for the period can still be holding the un-merged form.</para>
    /// </summary>
    public static SceneNpc? Sleeper(Scene scene, PoV pov, Element target)
    {
        if (target is SleepingNpcPointOfInterest merged)
            return pov.Where.PointsOfInterest.Contains(merged) && merged.Sleeper.IsAlive
                ? merged.Sleeper
                : null;

        return target is SceneNpc npc && npc.IsSleeping(scene, pov) ? npc : null;
    }

    /// <summary>The sleeper's name, for verbatims that must read before the merge is understood.</summary>
    public static string Name(Element target)
        => target is SleepingNpcPointOfInterest merged ? merged.Sleeper.Entity.DisplayName
         : target is SceneNpc npc                      ? npc.Entity.DisplayName
         : "them";
}
