using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Rules.Choice;

/// <summary>
/// A modus mentis with the <see cref="ModusMentisFunction.Speaking"/> function turns toward people.
/// At every stage of the chain it holds an office in, it puts conversation first:
/// <list type="bullet">
/// <item>observing, it attends to a living person over anything else in view, and does not turn away
///       from one it is handed (<c>ObservationPhaseController</c>);</item>
/// <item>thinking, it is offered only the conversations when the object offers any
///       (<see cref="SpeakingMindPrefersDialogueRule"/>);</item>
/// <item>choosing the means of a conversation, the action modi mentis that speak are the only ones
///       offered when any is held (<c>ThinkingExecutor</c>);</item>
/// <item>acting, it never refuses a conversation (<see cref="SpeakingActionNeverRefusesDialogueRule"/>).</item>
/// </list>
/// Each is a narrowing, never an addition: where there is nobody to talk to, nothing changes.
/// </summary>
public static class SpeakingPriority
{
    public static bool Speaks(ModusMentis mm) => mm.Functions.Contains(ModusMentisFunction.Speaking);

    /// <summary>Every verb that opens a conversation derives from <see cref="DialogueVerb"/>.</summary>
    public static bool IsDialogue(NarrativeAnchor? goal) => goal is VerbAction { Verb: DialogueVerb };

    /// <summary>A living human — asleep or awake, stranger or companion. Beasts and corpses are not.</summary>
    public static bool IsPerson(NarrativeAnchor? anchor)
        => anchor is SyntheticNpcObservationObject { SceneNpc: { IsAlive: true, Entity: NpcEntity e } }
           && e.Archetype.Species is HumanSpecies;
}

/// <summary>
/// A thinking modus mentis that speaks, looking at someone it could talk to, is offered only the
/// conversations. Registered after <see cref="LowMoralityPrefersCrimeRule"/> on purpose: an
/// unscrupulous speaking mind shown a purse to lift has already been narrowed to the crime, finds no
/// conversation left on the list, and this rule stays out of its way.
/// </summary>
public class SpeakingMindPrefersDialogueRule : IGoalChoiceRule
{
    public IReadOnlyList<NarrativeAnchor> Filter(IReadOnlyList<NarrativeAnchor> offered, ChoiceRuleContext ctx)
    {
        if (!SpeakingPriority.Speaks(ctx.ModusMentis)) return offered;

        var talks = offered.Where(SpeakingPriority.IsDialogue).ToList();
        if (talks.Count == 0 || talks.Count == offered.Count) return offered;

        Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] speaks — offering only "
                          + $"the {talks.Count} conversation(s) of {offered.Count} goal(s).");
        return talks;
    }
}

/// <summary>
/// An action modus mentis that speaks cannot refuse a conversation on principle — talking is what it
/// is for. The refusal is withheld; reluctance survives, as +1 difficulty, the way it does for an
/// unscrupulous mind asked to commit a crime. Refusals of circumstance (a hostile listener, nothing
/// left to say) are action rules and still apply.
/// </summary>
public class SpeakingActionNeverRefusesDialogueRule : IWillingnessRule
{
    public WillingnessOptions Filter(WillingnessOptions offered, ChoiceRuleContext ctx)
    {
        if (!SpeakingPriority.Speaks(ctx.ModusMentis)) return offered;
        if (offered.DeclineOption == null || offered.ImposedRefusal != null) return offered;
        if (!SpeakingPriority.IsDialogue(ctx.Goal)) return offered;

        if (!ctx.Probe)
            Console.WriteLine($"ChoiceRules: [{ctx.ModusMentis.DisplayName}] speaks — no refusal offered for a conversation.");
        return offered.WithoutDecline();
    }
}
