using System.Linq;
using System.Text.Json.Serialization;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// A precondition/cost recorded on a routine step. Constraints make a step possible (a required
/// item, a learned modus mentis, the acting member still being in the party) and may consume a
/// resource on full replay. This is the extension point for future constraint kinds (e.g. paying
/// gold): add a subclass implementing <see cref="IsSatisfied"/> and <see cref="Consume"/>.
///
/// <para><b>A new subclass must be registered with <c>[JsonDerivedType]</c> below.</b> Routines go
/// into the save file, and a base class this abstract is the one shape <c>System.Text.Json</c>
/// cannot handle on its own — it failed in <i>both</i> directions and only one of them was loud. On
/// write it serialises by the <i>declared</i> type, so a constraint's own fields (an
/// <see cref="ItemConstraint"/>'s item, an <see cref="ActingMemberConstraint"/>'s member) were
/// dropped in silence and every saved routine was already unreplayable. On read it throws
/// <c>NotSupportedException</c> — "Deserialization of interface or abstract types is not supported"
/// — which <see cref="Cathedral.Game.Save.SaveFile.Read"/> catches as corruption, so a single
/// recorded routine made the whole save unloadable and Continue greyed out.</para>
///
/// <para>The discriminator is the default <c>$type</c> rather than <see cref="Kind"/>, though the
/// two carry the same values: a discriminator sharing its name with a serialised property is a
/// hard error inside the serialiser, and <see cref="Kind"/> is overridden per subclass, where
/// <c>[JsonIgnore]</c> on this declaration does not reach. Every computed member here is therefore
/// ignored again on each override — they are display, not state.</para>
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(ItemConstraint), "item")]
[JsonDerivedType(typeof(ActingMemberConstraint), "acting_member")]
public abstract class RoutineConstraint
{
    /// <summary>Stable discriminator for the constraint kind (used for display).</summary>
    [JsonIgnore] public abstract string Kind { get; }

    /// <summary>Check-only: can this constraint be satisfied right now? Never mutates real state.</summary>
    public abstract bool IsSatisfied(RoutineReplayContext ctx);

    /// <summary>
    /// Apply the constraint's cost. On <see cref="RoutineReplayContext.DryRun"/> only the virtual
    /// ledger is touched; on full replay the real acting member / scene is mutated.
    /// </summary>
    public abstract void Consume(RoutineReplayContext ctx);

    /// <summary>Whether this constraint should be surfaced in the replay outcome popup.</summary>
    [JsonIgnore] public virtual bool ShowInOutcome => false;

    /// <summary>Outcome popup line (only used when <see cref="ShowInOutcome"/> is true).</summary>
    [JsonIgnore] public virtual string OutcomeText => "";
}

/// <summary>Requires (and consumes) one instance of an item the acting member holds.</summary>
public sealed class ItemConstraint : RoutineConstraint
{
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";

    public ItemConstraint() { }
    public ItemConstraint(string itemId, string itemName) { ItemId = itemId; ItemName = itemName; }

    [JsonIgnore] public override string Kind => "item";

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

    [JsonIgnore] public override bool ShowInOutcome => true;
    [JsonIgnore] public override string OutcomeText => $"Used: {ItemName}";
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

    [JsonIgnore] public override string Kind => "acting_member";

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
