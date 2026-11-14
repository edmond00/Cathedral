using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// The kinds of routine, in the order the menus list them. The kind is the routine's
/// <b>type</b> (see the subclasses of <see cref="Routine"/>); this enum only names it for the menus,
/// the map icons and the CLI.
/// </summary>
public enum RoutineCategory { GoTo, Gather, Buy, Sell, Work, Craft }

public static class RoutineCategories
{
    /// <summary>Menu order.</summary>
    public static readonly RoutineCategory[] All =
    {
        RoutineCategory.GoTo, RoutineCategory.Gather, RoutineCategory.Buy,
        RoutineCategory.Sell, RoutineCategory.Work,   RoutineCategory.Craft,
    };

    public static string Label(this RoutineCategory c) => c switch
    {
        RoutineCategory.GoTo   => "Go to",
        RoutineCategory.Gather => "Gather",
        RoutineCategory.Buy    => "Buy",
        RoutineCategory.Sell   => "Sell",
        RoutineCategory.Craft  => "Craft",
        _                      => "Work",
    };

    /// <summary>The handle a script names the category by — <c>inspect routines</c> and the CLI.</summary>
    public static string CliId(this RoutineCategory c) => c switch
    {
        RoutineCategory.GoTo   => "goto",
        RoutineCategory.Gather => "gather",
        RoutineCategory.Buy    => "buy",
        RoutineCategory.Sell   => "sell",
        RoutineCategory.Craft  => "craft",
        _                      => "work",
    };

    /// <summary>The letter its icon carries on a location map, black on yellow.</summary>
    public static char Icon(this RoutineCategory c) => c switch
    {
        RoutineCategory.GoTo   => 'G',
        RoutineCategory.Gather => 'H',
        RoutineCategory.Buy    => 'B',
        RoutineCategory.Sell   => 'S',
        RoutineCategory.Craft  => 'C',
        _                      => 'W',
    };

    /// <summary>How a routine of this kind is learned.</summary>
    public static string HowLearned(this RoutineCategory c) => c switch
    {
        RoutineCategory.GoTo   => "Walk somewhere public and leave from there.",
        RoutineCategory.Gather => "Gather, dig, mine, fish or cut wood in the open.",
        RoutineCategory.Buy    => "Agree to buy from a merchant.",
        RoutineCategory.Sell   => "Agree to sell to a merchant.",
        RoutineCategory.Craft  => "Set to work at a station you may use.",
        _                      => "Be taken on for work.",
    };
}

/// <summary>
/// What a routine is checked against: a scene freshly built for the routine's location, the
/// protagonist who would walk it, and the day it would be walked on.
///
/// <para><b>The scene is built with no depletion applied.</b> A gathering routine has to see every
/// slot its source has — the empty ones too, since a stay of some days lets them grow back — and
/// reads each one's state off <see cref="Scene.Scene.ItemDepletions"/> instead. Nothing else a
/// routine checks depends on depletion.</para>
/// </summary>
public sealed record RoutineCheckContext(Scene.Scene Scene, Protagonist Protagonist, double Now);

/// <summary>
/// A learned way into a location: an <b>area</b>, a <b>time of day</b>, and what happens there —
/// held in the <see cref="MentalMap"/> of that location, and forgotten with it —
/// the phase it opens. Walking one puts the player there and then, past every path, door and climb
/// between the location's edge and the area: the character knows the way.
///
/// <para>This replaced a recorded chain of verbs replayed headlessly. That design had to re-run the
/// whole narration rulebook on every step and asked every verb and every outcome how it bore on a
/// recording; an entry point asks one question at record time (<see cref="RoutineRecorder.IsRecordable"/>)
/// and one per kind at replay time (<see cref="Unavailability"/>).</para>
///
/// <para><b>A new subclass must be registered with <c>[JsonDerivedType]</c> below.</b> Routines go
/// into the save, and <c>System.Text.Json</c> writes a list of an abstract type by its declared type
/// — silently dropping every subclass field — and refuses to read one back at all, which the save
/// treats as corruption. An earlier routine type made the whole save unloadable exactly that way.
/// Everything computed here is <c>[JsonIgnore]</c>d: it is display, not state.</para>
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(GoToRoutine),   "go_to")]
[JsonDerivedType(typeof(TradeRoutine),  "trade")]
[JsonDerivedType(typeof(WorkRoutine),   "work")]
[JsonDerivedType(typeof(GatherRoutine), "gather")]
[JsonDerivedType(typeof(CraftRoutine),  "craft")]
public abstract class Routine
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>World vertex (location id) the routine enters.</summary>
    public int LocationId { get; set; }

    /// <summary>The location's name as the player knew it, for the menu.</summary>
    public string LocationName { get; set; } = "";

    /// <summary>
    /// The area entered, by display name — the one per-location unique area identifier (the lemma is
    /// generic: every building's roof is "roof"). <c>--building-audit</c> keeps display names unique.
    /// </summary>
    public string AreaName { get; set; } = "";

    /// <summary>The time of day the routine enters at.</summary>
    public TimePeriod Time { get; set; }

    /// <summary>What the menu calls it, settled at record time while every name is to hand.</summary>
    public string Name { get; set; } = "";

    /// <summary>
    /// What every menu shows. <see cref="Name"/> unless a kind can say it better from what it holds —
    /// which is how a routine saved under an older wording reads correctly the moment it is shown,
    /// instead of only after it is learned again.
    /// </summary>
    [JsonIgnore] public virtual string DisplayName => Name;

    /// <summary>The game day it was learned on.</summary>
    public double RecordedOnDay { get; set; }

    [JsonIgnore] public abstract RoutineCategory Category { get; }

    /// <summary>What, within the area and the hour, the routine is about — the NPC, the source.</summary>
    [JsonIgnore] protected abstract string TargetKey { get; }

    /// <summary>
    /// Identity for duplicate detection: one routine per kind, place, hour and target. Learning the
    /// same thing twice keeps the first.
    /// </summary>
    [JsonIgnore]
    public string Signature => $"{Category}|{LocationId}|{AreaName}|{Time}|{TargetKey}";

    /// <summary>
    /// Why this routine cannot be walked right now, or null when it can. The checks every kind shares
    /// are here — the area is still there and still somewhere one may stand — and each kind adds its
    /// own in <see cref="TargetUnavailability"/>.
    /// </summary>
    public string? Unavailability(RoutineCheckContext ctx)
    {
        var area = FindArea(ctx.Scene);
        if (area == null) return $"{AreaName} is no longer there";
        if (!RoutineRecorder.IsRecordable(ctx.Scene, area)) return $"{AreaName} is not yours to enter";
        return TargetUnavailability(ctx, area);
    }

    protected abstract string? TargetUnavailability(RoutineCheckContext ctx, Area area);

    /// <summary>
    /// One assertable line for <c>inspect routines</c>. Kind, area and hour are stable in the test
    /// location; <see cref="CliTarget"/> names what the routine is about, which for a person carries a
    /// generated name — content, so a script should assert the rest.
    /// </summary>
    public string CliLine()
        => $"routine category={Category.CliId()} location={LocationId} area=\"{AreaName}\" time={Time}"
         + (CliTarget.Length > 0 ? $" {CliTarget}" : "");

    [JsonIgnore] protected virtual string CliTarget => "";

    public Area? FindArea(Scene.Scene scene)
        => scene.AllAreas.FirstOrDefault(a => string.Equals(a.DisplayName, AreaName, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// An enemy standing in the area at the routine's hour, or null. A routine that opens straight
    /// onto a menu has no narration to meet them in, so it is refused rather than walked into a
    /// fight; one that opens narration lets the narration's own threat opener deal with them.
    /// </summary>
    protected NpcEntity? EnemyThere(RoutineCheckContext ctx, Area area)
        => ctx.Scene.GetNpcsAt(area, Time)
              .Select(n => n.Entity as NpcEntity)
              .FirstOrDefault(n => n != null && n.IsAlive && n.AffinityTable.IsEnemy(ctx.Protagonist.AffinityKey));
}

/// <summary>Opens narration in an area at an hour. Learned by leaving a location from somewhere else than where it opened.</summary>
public sealed class GoToRoutine : Routine
{
    [JsonIgnore] public override RoutineCategory Category => RoutineCategory.GoTo;
    [JsonIgnore] protected override string TargetKey => "";

    protected override string? TargetUnavailability(RoutineCheckContext ctx, Area area) => null;
}

/// <summary>A routine about one particular person, who must be found where and when they were.</summary>
public abstract class NpcRoutine : Routine
{
    /// <summary><see cref="INpcEntity.PersistentId"/> — name-derived, so stable across rebuilds.</summary>
    public string NpcId { get; set; } = "";
    public string NpcName { get; set; } = "";
    /// <summary>What they are ("a farmer", "a brewer"), for the menu.</summary>
    public string NpcDescription { get; set; } = "";

    [JsonIgnore] protected override string TargetKey => NpcId;
    [JsonIgnore] protected override string CliTarget => $"npc=\"{NpcId}\"";

    /// <summary>The person, alive in the rebuilt scene, or null.</summary>
    public SceneNpc? FindNpc(Scene.Scene scene)
        => scene.Npcs.FirstOrDefault(n => n.IsAlive && n.Entity is NpcEntity e && e.PersistentId == NpcId);

    /// <summary>The checks every person-routine shares: they are alive, there at the hour, and not hostile.</summary>
    protected string? PersonUnavailability(RoutineCheckContext ctx, Area area, out NpcEntity? npc)
    {
        npc = null;
        var sceneNpc = FindNpc(ctx.Scene);
        if (sceneNpc?.Entity is not NpcEntity entity) return $"{NpcName} is gone";
        var where = ctx.Scene.GetAreaOf(sceneNpc, Time);
        if (where == null || where.Id != area.Id) return $"{NpcName} is not there at {Time.Label().ToLowerInvariant()}";
        if (entity.AffinityTable.IsEnemy(ctx.Protagonist.AffinityKey)) return $"{NpcName} counts you an enemy";
        npc = entity;
        return null;
    }
}

/// <summary>One line of a merchant's catalogue, as it stood when the routine was learned.</summary>
public sealed class TradeLine
{
    public string Name { get; set; } = "";
    public int Price { get; set; }
    public CoinType Coin { get; set; }
}

/// <summary>
/// Opens the trade menu with a merchant, past the conversation that would otherwise open it.
///
/// <para>The catalogue is kept on the routine for the menu to show without building the scene. It
/// cannot go stale: <see cref="NpcTradeCatalog"/> is seeded from the NPC's id and the direction.</para>
/// </summary>
public sealed class TradeRoutine : NpcRoutine
{
    public TradeMode Mode { get; set; }
    public List<TradeLine> Catalogue { get; set; } = new();

    [JsonIgnore]
    public override RoutineCategory Category => Mode == TradeMode.Sell ? RoutineCategory.Sell : RoutineCategory.Buy;

    protected override string? TargetUnavailability(RoutineCheckContext ctx, Area area)
    {
        var why = PersonUnavailability(ctx, area, out var npc);
        if (why != null) return why;
        // Trade happens on premises: a trade routine recorded where the trader keeps their goods is
        // walkable only at an hour that finds them there.
        if (!npc!.TradesIn(area)) return $"{NpcName} does not trade here";
        if (!TradeGate.CanTrade(npc!, ctx.Protagonist, area)) return $"{NpcName} will not trade with you";
        // BuyCatalog is what the NPC sells — what the player BUYS — as in TradeMenuRenderer.
        if ((Mode == TradeMode.Buy ? npc!.BuyCatalog : npc!.SellCatalog) == null) return $"{NpcName} no longer trades";
        if (EnemyThere(ctx, area) is { } enemy) return $"{Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(enemy)} is there";
        return null;
    }
}

/// <summary>Opens the work menu for a job, past the conversation that would otherwise open it.</summary>
public sealed class WorkRoutine : NpcRoutine
{
    public string JobId { get; set; } = "";
    public string JobTitle { get; set; } = "";

    [JsonIgnore] public override RoutineCategory Category => RoutineCategory.Work;
    [JsonIgnore] protected override string TargetKey => $"{NpcId}:{JobId}";
    [JsonIgnore] protected override string CliTarget => $"npc=\"{NpcId}\" job={JobId}";

    protected override string? TargetUnavailability(RoutineCheckContext ctx, Area area)
    {
        var why = PersonUnavailability(ctx, area, out var npc);
        if (why != null) return why;
        if (JobRegistry.Instance.GetById(JobId) == null) return "that work no longer exists";
        // Work needs the introduction only — a job is offered by a person, wherever they are met.
        if (!TradeGate.IsAcquainted(npc!, ctx.Protagonist)) return $"{NpcName} will not take you on";
        if (EnemyThere(ctx, area) is { } enemy) return $"{Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(enemy)} is there";
        return null;
    }
}

/// <summary>
/// Opens the gathering phase on a source — a patch, a seam, a stretch of water — where the player
/// stays some days and takes what grows, a roll per item. See <see cref="GatherYield"/>.
/// </summary>
public sealed class GatherRoutine : Routine
{
    /// <summary>The verb the source was worked with: gather, or one of the extraction verbs.</summary>
    public string VerbId { get; set; } = "";
    /// <summary>The holding point of interest, by display name (unique within an area after the factory merge).</summary>
    public string SourceName { get; set; } = "";
    public string ItemId { get; set; } = "";
    public string ItemName { get; set; } = "";

    /// <summary>The implement it was done with, or empty — the one requirement a routine carries.</summary>
    public string ToolItemId { get; set; } = "";
    public string ToolItemName { get; set; } = "";

    [JsonIgnore] public override RoutineCategory Category => RoutineCategory.Gather;
    [JsonIgnore] protected override string TargetKey => $"{VerbId}:{SourceName}:{ItemId}";
    [JsonIgnore] protected override string CliTarget
        => $"verb={VerbId} source=\"{SourceName}\" item={ItemId} tool={(NeedsTool ? ToolItemId : "-")}";

    [JsonIgnore] public bool NeedsTool => ToolItemId.Length > 0;

    /// <summary>
    /// Rebuilt from what the routine holds rather than read from <see cref="Routine.Name"/>, so a
    /// routine saved as "Gather Reindeer Moss at Reindeer Moss" no longer says the item twice.
    /// </summary>
    [JsonIgnore] public override string DisplayName
        => RoutineRecorder.GatherName(VerbRegistry.Instance.Get(VerbId)?.DisplayName ?? Name.Split(' ')[0],
                                      ItemName, SourceName);

    protected override string? TargetUnavailability(RoutineCheckContext ctx, Area area)
    {
        var verb = VerbRegistry.Instance.Get(VerbId);
        if (verb == null) return "that work is no longer done";
        if (!ctx.Protagonist.Can(verb.EffectiveCapabilities)) return "your body cannot do it";
        if (NeedsTool && GatherYield.CarriedTool(this, ctx.Protagonist) == null) return $"needs {ToolItemName}";

        var source = GatherYield.FindSource(this, area);
        if (source == null) return $"{SourceName} is no longer there";
        if (GatherYield.Slots(this, verb, source, ctx.Scene, ctx.Now).Count == 0) return $"nothing of it grows there now";
        if (EnemyThere(ctx, area) is { } enemy) return $"{Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(enemy)} is there";
        return null;
    }
}

/// <summary>
/// Craft — straight back to a station's crafting menu, as a Buy routine goes straight back to the trade
/// menu. Learned when USE succeeds somewhere the player may stand: a public workshop, a workshop whose
/// master has given leave (kept for good), or a station nobody owns.
///
/// <para>Walkable while the station is still there, the trade is still held, its implement (or the one
/// it was learned with) is still carried, the station may still be used and no enemy is standing by it.
/// Nothing is rolled on the way in: the routine is the USE already won.</para>
/// </summary>
public sealed class CraftRoutine : Routine
{
    public string StationName { get; set; } = "";
    public string TradeId { get; set; } = "";
    public string TradeName { get; set; } = "";
    /// <summary>The implement USE was won with, or "" when it was the trade's own.</summary>
    public string ToolItemId { get; set; } = "";
    public string ToolItemName { get; set; } = "";

    [JsonIgnore] public override RoutineCategory Category => RoutineCategory.Craft;
    [JsonIgnore] protected override string TargetKey => $"{StationName}:{TradeId}";
    [JsonIgnore] protected override string CliTarget => $"trade={TradeId} tool={(ToolItemId.Length > 0 ? ToolItemId : "-")} station=\"{StationName}\"";

    /// <summary>The station in <paramref name="area"/>, by its name and its being a station.</summary>
    public PointOfInterest? FindStation(Area area)
        => area.PointsOfInterest.FirstOrDefault(p => CraftingStations.IsStation(p)
                                                  && string.Equals(p.DisplayName, StationName, StringComparison.OrdinalIgnoreCase));

    protected override string? TargetUnavailability(RoutineCheckContext ctx, Area area)
    {
        var station = FindStation(area);
        if (station == null) return $"{StationName} is no longer there";
        var trade = ctx.Protagonist.ModiMentis.OfType<Crafting.CraftingModusMentis>().FirstOrDefault(m => m.ModusMentisId == TradeId);
        if (trade == null) return $"you no longer know {TradeName.ToLowerInvariant()}";
        string toolId = ToolItemId.Length > 0 ? ToolItemId : trade.ToolItemId;
        if (!ctx.Protagonist.GetAllItems().Any(i => i.ItemId == toolId))
            return $"needs {(ToolItemName.Length > 0 ? ToolItemName : trade.ToolPrototype?.DisplayName ?? toolId).ToLowerInvariant()}";
        if (!Crafting.CraftingAccess.MayUse(ctx.Scene, new PoV(area, Time), station)) return $"the {StationName.ToLowerInvariant()} is not yours to use";
        if (EnemyThere(ctx, area) is { } enemy) return $"{Cathedral.Game.Npc.NpcLabelResolver.ShortLabelCapitalized(enemy)} is there";
        return null;
    }
}
