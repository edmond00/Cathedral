using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Crafting;
using Cathedral.Game.Npc;

namespace Cathedral.Game.Scene.Verbs;

/// <summary>
/// USE — set to work at a crafting station. Offered only to a body holding a crafting modus mentis for
/// the station's kind, and where the station may be worked (<see cref="CraftingAccess.MayUse"/>):
/// nobody's, or its master's leave given. On a protected floor it is offered without leave too, and
/// is then a crime (<see cref="PrivacyModel.IsIntrusion"/>).
///
/// <para><b>One action per trade.</b> A body holding both the blacksmith and the bladesmith is offered
/// the anvil twice, the trade being the action's variant, so a will wants the one that is itself and a
/// thinking mind chooses between them as between any two goals.</para>
///
/// <para><b>It needs an implement</b> (<see cref="ToolUsage.Required"/>). The trade's own tool is
/// accepted without a question (<see cref="ReferenceToolIdsFor"/>); anything else goes to the item
/// critic and the hands' proficiency like every other substitute — a smith may try his trade with an
/// axe. And since no two trades share a station and a tool, the implement combined settles which trade
/// is at work: tongs on the anvil open the bladesmith's recipes even when the blacksmith's action was
/// the one clicked.</para>
///
/// <para>Success opens the crafting menu (<see cref="CraftingOpenedOutcome"/>), as a won conversation
/// opens the trade menu. What happens there rolls nothing: the modus mentis and the tool count only
/// here, at the door.</para>
/// </summary>
public sealed class UseStationVerb : Verb
{
    public override string VerbId         => "use";
    public override string DisplayName    => "Use";
    public override int    BaseDifficulty => 2;

    /// <summary>Works a station that belongs to the place.</summary>
    public override VerbConduct Conduct => VerbConduct.Intrusive;

    public override ToolUsage ToolUse => ToolUsage.Required;

    /// <summary>Every trade's implement — what the path planner looks for when it has nothing to work with.</summary>
    public override IReadOnlyList<string> ReferenceToolIds
        => _allTools ??= CraftingModusMentis.All.Select(mm => mm.ToolItemId).Where(id => id.Length > 0).Distinct().ToList();
    private List<string>? _allTools;

    /// <summary>The implement of the trade this action is offered as.</summary>
    public override IReadOnlyList<string> ReferenceToolIdsFor(VerbAction? view)
        => view?.Variant is CraftingModusMentis mm && mm.ToolItemId.Length > 0 ? new[] { mm.ToolItemId } : ReferenceToolIds;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Trades(scene, pov, target, actor).Any();

    public override string Verbatim(Scene scene, PoV pov, Element target) => $"set to work at {DefiniteTarget(target)}";

    public override IEnumerable<VerbAction> ExpandViews(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (!IsPossible(scene, pov, target, actor)) yield break;
        foreach (var mm in Trades(scene, pov, target, actor))
            yield return new VerbAction(this, $"set to work at {DefiniteTarget(target)} as {Practitioner(mm)}", target, variant: mm);
    }

    /// <summary>"a blacksmith", "an …": the trade named by its practitioner (its persona reminder).</summary>
    private static string Practitioner(CraftingModusMentis mm)
    {
        var who = mm.PersonaReminder ?? mm.DisplayName.ToLowerInvariant();
        return ("aeiou".Contains(who[0]) ? "an " : "a ") + who;
    }

    /// <summary>
    /// The trades that may work <paramref name="target"/> here. With no actor (the audits, the probe) every
    /// trade of the station's kind; with one, the trades that body holds.
    /// </summary>
    private static IEnumerable<CraftingModusMentis> Trades(Scene scene, PoV pov, Element target, PartyMember? actor)
    {
        if (target is not PointOfInterest station || !CraftingStations.IsStation(station)) return Enumerable.Empty<CraftingModusMentis>();
        if (!pov.Where.IsProtected && !CraftingAccess.MayUse(scene, pov, station)) return Enumerable.Empty<CraftingModusMentis>();
        var trades = actor == null
            ? CraftingModusMentis.All.Where(mm => mm.Station.IsInstanceOfType(station))
            : CraftingModusMentis.HeldFor(actor, station);
        return trades.ToList();
    }

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target,
                                                         VerbAction view, Item? tool = null)
    {
        if (target is not PointOfInterest station) return System.Array.Empty<Outcome>();

        // The implement names the trade when it is one of the actor's own for this station.
        var trade = (tool != null
                        ? CraftingModusMentis.HeldFor(actor, station).FirstOrDefault(mm => mm.Tool == tool.GetType())
                        : null)
                    ?? view.Variant as CraftingModusMentis
                    ?? CraftingModusMentis.HeldFor(actor, station).FirstOrDefault();
        if (trade == null) return System.Array.Empty<Outcome>();
        return new Outcome[] { new CraftingOpenedOutcome(station, trade, tool?.ItemId) };
    }

    /// <summary>
    /// The trade at work is the lesson: with the action known, its own modus mentis; without (the
    /// audits), every trade of the station's kind, which is what the station could teach.
    /// </summary>
    public override IEnumerable<ModusMentis> Lessons(LessonContext ctx)
    {
        if (ctx.Action?.Variant is CraftingModusMentis used
            && ModusMentisRegistry.Instance.GetModusMentis(used.ModusMentisId) is { } mm)
            yield return mm;
        foreach (var lesson in base.Lessons(ctx)) yield return lesson;
    }

    public override IReadOnlyList<string> GrantedModusMentisIds(Element? target)
        => target is PointOfInterest station
            ? CraftingModusMentis.All.Where(mm => mm.Station.IsInstanceOfType(station)).Select(mm => mm.ModusMentisId).ToList()
            : CraftingModusMentis.All.Select(mm => mm.ModusMentisId).ToList();   // no station named: every trade it can teach

    /// <summary>A slip at a station: a scrape on the hands, the hour gone, or the work lost track of.</summary>
    public override IReadOnlyList<FailurePenalty> FailurePenalties(Element? target)
        => new FailurePenalty[] { FailurePenalty.WasteTime, FailurePenalty.LoseFocus, new ScrapeWound() };
}

/// <summary>
/// ASK TO USE THE WORKSHOP — ask a master for the use of their equipment. Offered only to a body holding
/// a crafting modus mentis for a station in a workshop the master owns, and only while that leave is not
/// given; any other mind has no reason to ask. Opens <see cref="AskToUseWorkshopTree"/>, whose success
/// gives the leave for good.
///
/// <para>To a crafting will this is <b>a step on the way</b>, never a target: the path planner reads the
/// leave it would give (<c>ActionPathPlanner</c>) and so plans "ask the smith, then work the anvil" as
/// one path. Being a step, its means is judged among the speaking minds held — which is what lets a
/// crafting mind that cannot speak get its leave.</para>
/// </summary>
public sealed class AskToUseWorkshopVerb : DialogueVerb
{
    public const string TreeId = "ask_to_use_workshop";

    public override string VerbId            => "ask_to_use_workshop";
    public override string DisplayName       => "Ask to use workshop";
    public override int    BaseDifficulty    => 0;   // the action only meets the master, which is not rolled; the dialogue carries the stakes
    protected override string DialogueTreeId => TreeId;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Workshops(scene, pov, target, actor).Any();

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} for the use of the workshop";

    public override IEnumerable<VerbAction> ExpandViews(Scene scene, PoV pov, Element target, PartyMember? actor = null)
    {
        if (!IsPossible(scene, pov, target, actor)) yield break;
        foreach (var section in Workshops(scene, pov, target, actor))
            yield return new VerbAction(this, $"ask {NpcPronoun(target)} for the use of the {section.DisplayName.ToLowerInvariant()}",
                                        target, variant: section.DisplayName);
    }

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target,
                                                         VerbAction view, Item? tool = null)
    {
        if (target is not SceneNpc { Entity: NpcEntity npc } || view.Variant is not string workshop)
            return System.Array.Empty<Outcome>();
        npc.PendingWorkshop = workshop;   // the conversation's success promotes it to GrantedWorkshop
        return new Outcome[] { new DialogueTriggerOutcome(npc, TreeId) };
    }

    /// <summary>
    /// The workshops this master could let <paramref name="actor"/> use. With no actor (the audits) every
    /// unauthorized workshop the master owns that holds any station at all.
    /// </summary>
    private static IEnumerable<Section> Workshops(Scene scene, PoV pov, Element target, PartyMember? actor)
    {
        if (target is not SceneNpc sceneNpc || sceneNpc.Entity is not NpcEntity npc) return Enumerable.Empty<Section>();
        if (!npc.CanSpeak || !npc.IsAlive) return Enumerable.Empty<Section>();
        if (SleeperGate.IsAsleep(scene, pov, target)) return Enumerable.Empty<Section>();
        if (!scene.GetNpcsAt(pov.Where, pov.When).Exists(n => n.Id == sceneNpc.Id)) return Enumerable.Empty<Section>();
        if (actor != null && npc.AffinityTable.IsEnemy(actor.AffinityKey)) return Enumerable.Empty<Section>();
        if (actor != null) return CraftingAccess.WorkshopsToAsk(scene, npc, actor).ToList();
        return scene.Sections
            .Where(s => npc.OwnedSectionIds.Contains(s.Id.ToString()) && !CraftingAccess.IsAuthorized(scene, s)
                     && CraftingAccess.MasterOf(scene, s) == npc
                     && s.Areas.SelectMany(a => a.PointsOfInterest).Any(CraftingStations.IsStation))
            .ToList();
    }
}
