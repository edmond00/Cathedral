using System.Linq;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// A precondition/cost recorded on a routine step. Constraints make a step possible (a required
/// item, a learned modus mentis, the acting member still being in the party) and may consume a
/// resource on full replay. This is the extension point for future constraint kinds (e.g. paying
/// gold): add a subclass implementing <see cref="IsSatisfied"/> and <see cref="Consume"/>.
/// </summary>
public abstract class RoutineConstraint
{
    /// <summary>Stable discriminator for the constraint kind (used for display/serialization).</summary>
    public abstract string Kind { get; }

    /// <summary>Check-only: can this constraint be satisfied right now? Never mutates real state.</summary>
    public abstract bool IsSatisfied(RoutineReplayContext ctx);

    /// <summary>
    /// Apply the constraint's cost. On <see cref="RoutineReplayContext.DryRun"/> only the virtual
    /// ledger is touched; on full replay the real acting member / scene is mutated.
    /// </summary>
    public abstract void Consume(RoutineReplayContext ctx);

    /// <summary>Whether this constraint should be surfaced in the replay outcome popup.</summary>
    public virtual bool ShowInOutcome => false;

    /// <summary>Outcome popup line (only used when <see cref="ShowInOutcome"/> is true).</summary>
    public virtual string OutcomeText => "";
}

/// <summary>Requires (and consumes) one instance of an item the acting member holds.</summary>
public sealed class ItemConstraint : RoutineConstraint
{
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";

    public ItemConstraint() { }
    public ItemConstraint(string itemId, string itemName) { ItemId = itemId; ItemName = itemName; }

    public override string Kind => "item";

    public override bool IsSatisfied(RoutineReplayContext ctx) => ctx.AvailableItemCount(ItemId) > 0;

    public override void Consume(RoutineReplayContext ctx)
    {
        if (ctx.DryRun)
        {
            ctx.VirtualLedger.TryGetValue(ItemId, out int spent);
            ctx.VirtualLedger[ItemId] = spent + 1;
            return;
        }

        var item = ctx.ActingMember.GetAllItems().FirstOrDefault(i => i.ItemId == ItemId);
        if (item != null) ctx.ActingMember.RemoveItem(item);
    }

    public override bool ShowInOutcome => true;
    public override string OutcomeText => $"Used: {ItemName}";
}

// There was a ModusMentisConstraint here, requiring the acting member to still hold the skill the
// step was recorded with. It is gone: the id is now plain data on the step
// (RoutineStep.ActionModusMentisId). A constraint is two things at once — a precondition of replay
// and a line in the routine's requirements list — and the modus mentis should be neither. A routine
// is a thing the character learned to do, not a thing one skill learned to do.

/// <summary>
/// Records which party member performed the step and binds that member as the actor at replay.
/// The step is unreplayable if the recorded member is no longer in the party.
/// </summary>
public sealed class ActingMemberConstraint : RoutineConstraint
{
    public string MemberKey { get; set; } = "";

    public ActingMemberConstraint() { }
    public ActingMemberConstraint(string memberKey) { MemberKey = memberKey; }

    public override string Kind => "acting_member";

    public override bool IsSatisfied(RoutineReplayContext ctx)
    {
        var member = ctx.ResolveMember(MemberKey);
        if (member == null) return false;
        ctx.ActingMember = member; // bind the actor for the rest of this step
        return true;
    }

    public override void Consume(RoutineReplayContext ctx)
    {
        var member = ctx.ResolveMember(MemberKey);
        if (member != null) ctx.ActingMember = member;
    }
}
