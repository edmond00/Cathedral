using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Scene.Verbs;

// The verbs that open a dense city's conversations (see CityTrees). Each is offered only on the kinds
// of person its tree can be had with.

/// <summary>Opens "Commission Work" (<c>commission_work</c>) with a cobbler, tailor, potter, chandler, tanner.</summary>
public class CommissionWorkVerb : SocialDialogueVerb
{
    public override string VerbId      => "commission_work";
    public override string DisplayName => "Commission work";

    protected override string DialogueTreeId => "commission_work";

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: CobblerArchetype or TailorArchetype or PotterArchetype or ChandlerArchetype or TannerArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} to make something for me";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"commission work from {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "commission_work") };
    }
}

/// <summary>Opens "Ask for a Remedy" (<c>ask_remedy</c>) with an apothecary, barber.</summary>
public class AskRemedyVerb : SocialDialogueVerb
{
    public override string VerbId      => "ask_remedy";
    public override string DisplayName => "Ask for a remedy";

    protected override string DialogueTreeId => "ask_remedy";

    /// <summary>Asked of strangers as readily as of acquaintances; see the tree.</summary>
    protected override bool RequiresAcquaintance => false;

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: ApothecaryArchetype or BarberArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} for a remedy";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"ask {NpcName(target)} for a remedy";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "ask_remedy") };
    }
}

/// <summary>Opens "Give Alms" (<c>give_alms</c>) with a beggar.</summary>
public class GiveAlmsVerb : SocialDialogueVerb
{
    public override string VerbId      => "give_alms";
    public override string DisplayName => "Give alms";

    protected override string DialogueTreeId => "give_alms";

    /// <summary>Alms are given to strangers; that is what they are. See the tree.</summary>
    protected override bool RequiresAcquaintance => false;

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: BeggarArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"give {NpcPronoun(target)} alms";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"give alms to {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "give_alms") };
    }
}

/// <summary>Opens "Hear the Gossip" (<c>hear_gossip</c>) with a laundress, water-carrier, porter, barber.</summary>
public class HearGossipVerb : SocialDialogueVerb
{
    public override string VerbId      => "hear_gossip";
    public override string DisplayName => "Hear the gossip";

    protected override string DialogueTreeId => "hear_gossip";

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: LaundressArchetype or WaterCarrierArchetype or PorterArchetype or BarberArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} what is being said in town";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"hear the gossip from {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "hear_gossip") };
    }
}
