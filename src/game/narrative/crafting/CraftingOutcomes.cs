using Cathedral.Game.Scene;

namespace Cathedral.Game.Narrative.Crafting;

/// <summary>
/// USE succeeded: the crafting menu opens at <see cref="Station"/> for <see cref="Trade"/>. Queues the
/// request on the scene (<see cref="Scene.Scene.PendingCraftingRequest"/>), as a dialogue verb queues a
/// conversation; the narrative controller turns it into the menu once the action's reports are applied.
/// </summary>
public sealed class CraftingOpenedOutcome : Outcome
{
    public PointOfInterest Station { get; }
    public CraftingModusMentis Trade { get; }
    public string? ToolItemId { get; }

    public CraftingOpenedOutcome(PointOfInterest station, CraftingModusMentis trade, string? toolItemId)
        : base($"At work: {station.DisplayName} ({trade.DisplayName})", OutcomeSeverity.Positive,
               $"set to work at the {station.DisplayName.ToLowerInvariant()}")
    {
        Station    = station;
        Trade      = trade;
        ToolItemId = toolItemId;
    }

    protected override void Apply(OutcomeContext ctx)
    {
        if (ctx.Scene == null) return;
        ctx.Scene.PendingCraftingRequest = new CraftingRequest(Station, Trade.ModusMentisId, ToolItemId);
    }
}

/// <summary>
/// A master let the player use their workshop. A conversation's outcome reaches the NPC and nothing
/// else, so it promotes <see cref="Npc.NpcEntity.PendingWorkshop"/> to <see cref="Npc.NpcEntity.GrantedWorkshop"/>
/// and the controller writes the leave into the scene — and so into the location's saved state — when
/// the conversation closes.
/// </summary>
public sealed class WorkshopAuthorizedOutcome : Outcome
{
    public WorkshopAuthorizedOutcome()
        : base("Leave to use the workshop", OutcomeSeverity.Positive, "was given the use of the workshop") { }

    public override bool ShowInUI => Reported;

    protected override void Apply(OutcomeContext ctx)
    {
        var npc = ctx.Npc;
        if (npc?.PendingWorkshop is not { } workshop) return;
        npc.GrantedWorkshop = workshop;
        npc.PendingWorkshop = null;
        Report($"{Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(npc)} lets you use the {workshop.ToLowerInvariant()}");
    }
}
