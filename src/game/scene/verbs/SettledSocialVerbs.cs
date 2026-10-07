using System.Collections.Generic;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Routines;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Archetypes;

namespace Cathedral.Game.Scene.Verbs;

// The verbs that open the settled country's conversations (see SettledTrees). Each is offered only on
// the kinds of person its tree can be had with.

/// <summary>Opens "Ask a Blessing" (<c>ask_blessing</c>) with a priest, monk.</summary>
public class AskBlessingVerb : SocialDialogueVerb
{
    public override string VerbId      => "ask_blessing";
    public override string DisplayName => "Ask a blessing";

    protected override string DialogueTreeId => "ask_blessing";

    /// <summary>Asked of strangers as readily as of acquaintances; see the tree.</summary>
    protected override bool RequiresAcquaintance => false;

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: PriestArchetype or MonkArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} for a blessing";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"ask a blessing of {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "ask_blessing") };
    }
}

/// <summary>Opens "Confess" (<c>confess</c>) with a priest, monk.</summary>
public class ConfessVerb : SocialDialogueVerb
{
    public override string VerbId      => "confess";
    public override string DisplayName => "Confess";

    protected override string DialogueTreeId => "confess";

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: PriestArchetype or MonkArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"confess something to {NpcPronoun(target)}";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"confess to {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "confess") };
    }
}

/// <summary>Opens "Petition" (<c>petition</c>) with a lord, steward, captain.</summary>
public class PetitionVerb : SocialDialogueVerb
{
    public override string VerbId      => "petition";
    public override string DisplayName => "Petition";

    protected override string DialogueTreeId => "petition";

    /// <summary>Asked of strangers as readily as of acquaintances; see the tree.</summary>
    protected override bool RequiresAcquaintance => false;

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: LordArchetype or StewardArchetype or CaptainArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"put a petition to {NpcPronoun(target)}";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"petition {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "petition") };
    }
}

/// <summary>Opens "Talk Soldiering" (<c>talk_soldiering</c>) with a guard, captain.</summary>
public class TalkSoldieringVerb : SocialDialogueVerb
{
    public override string VerbId      => "talk_soldiering";
    public override string DisplayName => "Talk soldiering";

    protected override string DialogueTreeId => "talk_soldiering";

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: GuardArchetype or CaptainArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"talk soldiering with {NpcPronoun(target)}";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"talk soldiering with {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "talk_soldiering") };
    }
}

/// <summary>Opens "Talk of Far Places" (<c>talk_far_places</c>) with a merchant, sailor.</summary>
public class TalkOfFarPlacesVerb : SocialDialogueVerb
{
    public override string VerbId      => "talk_far_places";
    public override string DisplayName => "Talk of far places";

    protected override string DialogueTreeId => "talk_far_places";

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: MerchantArchetype or SailorArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} about the places they have been";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"talk of far places with {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "talk_far_places") };
    }
}

/// <summary>Opens "Offer a Bribe" (<c>offer_bribe</c>) with a guard, clerk, steward, captain.</summary>
public class OfferBribeVerb : SocialDialogueVerb
{
    public override string VerbId      => "offer_bribe";
    public override string DisplayName => "Offer a bribe";

    protected override string DialogueTreeId => "offer_bribe";

    /// <summary>Asked of strangers as readily as of acquaintances; see the tree.</summary>
    protected override bool RequiresAcquaintance => false;

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: GuardArchetype or ClerkArchetype or StewardArchetype or CaptainArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"slip {NpcPronoun(target)} something to look the other way";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"offer {NpcName(target)} a bribe";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "offer_bribe") };
    }
}

/// <summary>Opens "Ask to Be Taught" (<c>ask_teaching</c>) with a scholar, clerk, monk.</summary>
public class AskTeachingVerb : SocialDialogueVerb
{
    public override string VerbId      => "ask_teaching";
    public override string DisplayName => "Ask to be taught";

    protected override string DialogueTreeId => "ask_teaching";

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: ScholarArchetype or ClerkArchetype or MonkArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"ask {NpcPronoun(target)} to teach me something";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"ask {NpcName(target)} to teach you";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "ask_teaching") };
    }
}

/// <summary>Opens "Sing Along" (<c>sing_along</c>) with a sailor, innkeeper, picker, drover, monk.</summary>
public class SingAlongVerb : SocialDialogueVerb
{
    public override string VerbId      => "sing_along";
    public override string DisplayName => "Sing along";

    protected override string DialogueTreeId => "sing_along";

    private NpcEntity? Who(Scene scene, PoV pov, Element target, PartyMember? actor)
        => Available(scene, pov, target, actor) is { Archetype: SailorArchetype or InnkeeperArchetype or PickerArchetype or DroverArchetype or MonkArchetype } npc ? npc : null;

    protected override bool IsPossibleFor(Scene scene, PoV pov, Element target, PartyMember? actor = null)
        => Who(scene, pov, target, actor) != null;

    public override string Verbatim(Scene scene, PoV pov, Element target)
        => $"join in with whatever {NpcPronoun(target)} is singing";

    public override string RoutineLabel(Scene scene, PoV pov, Element target, VerbAction? view = null)
        => $"sing along with {NpcName(target)}";

    public override IReadOnlyList<Outcome> SuccessReports(Scene scene, PoV pov, PartyMember actor, Element target)
    {
        var npc = Who(scene, pov, target, actor);
        return npc == null
            ? System.Array.Empty<Outcome>()
            : new Outcome[] { new DialogueTriggerOutcome(npc, "sing_along") };
    }
}
