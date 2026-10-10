using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Game.Npc;
using Cathedral.Game.Npc.Trade;
using Cathedral.Game.Narrative.Work;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game.Narrative.Routines;

/// <summary>
/// Learns routines from one narration session. A routine is an entry point — an area, an hour and
/// what opens there — so the recorder does not follow the player's steps at all: it waits for the
/// five moments that make one, and notes where and when the player stood.
///
/// <list type="bullet">
/// <item><b>Gather</b> — a successful gather, dig, mine, fish or cut-wood, on its source.</item>
/// <item><b>Meet</b> — a conversation opened with somebody.</item>
/// <item><b>Buy / Sell</b> — a merchant agreeing to trade.</item>
/// <item><b>Work</b> — a master agreeing to take the player on.</item>
/// <item><b>Go to</b> — leaving the location from an area other than the one the session opened in.</item>
/// </list>
///
/// <para><b>One rule decides whether anything is learned: <see cref="IsRecordable"/>.</b> Nothing is
/// learned where the player has no business standing — a private area — because a routine there
/// would be a standing invitation to trespass, walked with none of the risk.</para>
///
/// <para>The same routine learned twice is kept once (<see cref="Routine.Signature"/>), and a full
/// kind evicts its oldest unlocked routine — see <see cref="Protagonist.RecordRoutine"/>.</para>
/// </summary>
public class RoutineRecorder
{
    private readonly Protagonist _protagonist;
    private readonly int _locationId;
    private readonly string _locationName;
    private readonly Area? _openedAt;

    public RoutineRecorder(Protagonist protagonist, int locationId, string locationName, Area? openedAt)
    {
        _protagonist  = protagonist;
        _locationId   = locationId;
        _locationName = locationName;
        _openedAt     = openedAt;
    }

    /// <summary>
    /// Whether a routine may be learned — and walked — here: anywhere the player may stand without
    /// trespassing. The same test the narration header shows the player.
    /// </summary>
    public static bool IsRecordable(Area area) => !PrivacyModel.IsTrespassing(area);

    // ── The five moments ──────────────────────────────────────────────────────

    /// <summary>
    /// Called after a verb has SUCCEEDED and before its reports apply, so the item is still on its
    /// source. Learns a gathering routine when the verb takes something that grows back.
    /// </summary>
    public void OnVerbSucceeded(ParsedNarrativeAction action, Scene.Scene scene, PoV pov)
    {
        var verb   = action.Verb;
        var target = action.PreselectedOutcome.Target;

        PointOfInterest? source;
        ItemElement? item;
        switch (verb)
        {
            case GatherVerb when target is ItemElement element:
                item   = element;
                source = ItemPickup.FindHoldingPoI(pov, element);
                break;
            case ExtractionVerb extraction when target is PointOfInterest poi && extraction.Works(poi):
                source = poi;
                item   = extraction.YieldOf(poi);
                break;
            default:
                return;
        }

        // What play made is not in the next visit's scene, so a routine on it could never be walked.
        if (source == null || item == null || source.SpawnedDuringVisit || item.SpawnedDuringVisit) return;

        var tool = action.CombinedItem;
        Record(new GatherRoutine
        {
            VerbId              = verb.VerbId,
            SourceName          = source.DisplayName,
            ItemId              = item.Item.ItemId,
            ItemName            = item.Item.DisplayName,
            ToolItemId          = tool?.ItemId ?? "",
            ToolItemName        = tool?.DisplayName ?? "",
            ChainModusMentisIds = ChainOf(action),
            Name                = $"{verb.DisplayName} {item.Item.DisplayName} at {source.DisplayName}",
        }, pov);
    }

    /// <summary>A conversation opened with <paramref name="npc"/>: the player knows where to find them.</summary>
    public void OnConversation(PoV pov, NpcEntity npc)
        => Record(Person(new MeetRoutine(), npc, $"Meet {npc.DisplayName}"), pov);

    /// <summary>A merchant agreed to trade.</summary>
    public void OnTrade(PoV pov, NpcEntity npc, TradeMode mode)
    {
        var catalog = mode == TradeMode.Sell ? npc.BuyCatalog : npc.SellCatalog;
        var routine = Person(new TradeRoutine { Mode = mode }, npc,
            mode == TradeMode.Sell ? $"Sell to {npc.DisplayName}" : $"Buy from {npc.DisplayName}");
        if (catalog != null)
            routine.Catalogue = catalog.Offers
                .Select(o => new TradeLine { Name = o.DisplayName, Price = o.UnitPrice, Coin = o.Coin })
                .ToList();
        Record(routine, pov);
    }

    /// <summary>A master agreed to take the player on.</summary>
    public void OnWork(PoV pov, NpcEntity npc, Job job)
        => Record(Person(new WorkRoutine { JobId = job.Id, JobTitle = job.Title }, npc,
                         $"Work as {job.Title} for {npc.DisplayName}"), pov);

    /// <summary>
    /// The session is ending. Leaving from somewhere other than where it opened is a place worth
    /// going back to — following a track to the vegetable beds and leaving from there is a "go to
    /// the beds" routine.
    /// </summary>
    public void FinalizeAtNarrationEnd(PoV? pov)
    {
        if (pov == null || (_openedAt != null && pov.Where.Id == _openedAt.Id)) return;
        Record(new GoToRoutine { Name = $"Go to {pov.Where.DisplayName}" }, pov);
    }

    // ── Plumbing ──────────────────────────────────────────────────────────────

    private static T Person<T>(T routine, NpcEntity npc, string name) where T : NpcRoutine
    {
        routine.NpcId          = npc.PersistentId;
        routine.NpcName        = npc.DisplayName;
        routine.NpcDescription = npc.Combatant.PartyDescription;
        routine.Name           = name;
        return routine;
    }

    private void Record(Routine routine, PoV pov)
    {
        if (!IsRecordable(pov.Where))
        {
            Console.WriteLine($"RoutineRecorder: not learning '{routine.Name}' — {pov.Where.DisplayName} is private");
            return;
        }

        routine.LocationId    = _locationId;
        routine.LocationName  = _locationName;
        routine.AreaName      = pov.Where.DisplayName;
        routine.Time          = pov.When;
        routine.RecordedOnDay = GameClock.Days;

        if (_protagonist.RecordRoutine(routine))
            Console.WriteLine($"RoutineRecorder: learned {routine.Category.CliId()} routine '{routine.Name}' " +
                              $"at {routine.AreaName} / {routine.Time}");
    }

    /// <summary>
    /// The modi mentis the act was rolled with, observation to action — every element of the chain
    /// except the implement's stand-in, whose level the gathering phase reads off the carried
    /// implement instead.
    /// </summary>
    private static List<string> ChainOf(ParsedNarrativeAction action)
    {
        var ids = new List<string>();
        for (ModusMentisChainElement? e = action; e != null; e = e.ChainOrigin)
            if (e.ChainModusMentis is { } mm and not SyntheticItemModusMentis)
                ids.Add(mm.ModusMentisId);
        ids.Reverse();
        return ids;
    }
}
