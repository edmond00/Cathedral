using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.Audio;
using Cathedral.Debug;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Verbs;

namespace Cathedral.Game;

// Moving about the place: the walk by known ways that costs nothing, the walk an act makes, the way
// in, and the way out — LEAVE for the whole party, RUNAWAY for whoever is confronted.
public partial class NarrativeController
{
    // ── A walk shown on the map ──────────────────────────────────────────────

    /// <summary>
    /// A move shown walked: a glyph going, a cell a step, from the card left along the way taken to
    /// the card reached. Presentation only — whoever walks is moved by whoever started it — and it
    /// goes on by itself: nothing is decided at its end, so nothing is asked there.
    /// </summary>
    private sealed class MapWalk
    {
        public required List<LocationMap.WalkStep> Steps { get; init; }
        public required IReadOnlyList<(Area To, string? Link)> Hops { get; init; }
        /// <summary>What the move recorded that the map did not know before: revealed as the walk reaches it.</summary>
        public HashSet<string> Fresh { get; init; } = new();
        public required string Status { get; init; }
        public required char Glyph { get; init; }
        /// <summary>Who is on the way, and so not drawn standing on any card meanwhile.</summary>
        public IReadOnlyList<PartyMember> Walkers { get; init; } = Array.Empty<PartyMember>();
        public Action? Then { get; init; }
        public int Shown;
        public DateTime Start = DateTime.UtcNow;
        public double StepMs;
        public DateTime? SettledAt;
        public bool Settled => Shown >= Steps.Count - 1;
    }
    private MapWalk? _walk;
    private string? _lastWalk;

    /// <summary>A cell a step, but no walk longer than <see cref="WalkMaxMs"/> however far it goes.</summary>
    private const double WalkStepMs = 90, WalkMaxMs = 6000;
    /// <summary>How long a walker stands where it arrived before what follows is drawn.</summary>
    private const int WalkDwellMs = 650;

    private void StartWalk(List<LocationMap.WalkStep> steps, IReadOnlyList<(Area To, string? Link)> hops,
                           string status, Action? then, HashSet<string>? fresh = null,
                           IReadOnlyList<PartyMember>? walkers = null)
    {
        if (steps.Count < 2) { then?.Invoke(); return; }
        walkers ??= new[] { _active };
        var walk = new MapWalk
        {
            Steps = steps, Hops = hops, Fresh = fresh ?? new(), Status = status, Then = then, Walkers = walkers,
            Glyph = MapTreeOverlay.TokenGlyph(walkers.Count > 0 ? walkers[0] : _active),
            // A script has nothing to watch: the walk is settled at once.
            StepMs = Cli.CliMode.IsActive && !Record.RecordMode.IsActive ? 0 : Math.Min(WalkStepMs, WalkMaxMs / steps.Count),
        };
        // A walk waits for the hours to finish passing, and takes over what their Continue would resume.
        if (_timeBox.IsVisible)
        {
            var after = _afterTimeBox;
            _afterTimeBox = () => { walk.Start = DateTime.UtcNow; _walk = walk; after?.Invoke(); };
            return;
        }
        _walk = walk;
    }

    /// <summary>
    /// What the walk has not reached yet at the cell shown: the ways ahead of it and the areas it has
    /// not entered. A way is reached once the glyph is on it, an area once it is in its card; anything
    /// else the move recorded (the areas a path runs past) once the walk is over.
    /// </summary>
    private HashSet<string> WalkWithheld()
    {
        if (_walk is not { Settled: false } walk) return new();
        var step = walk.Steps[walk.Shown];
        var held = new HashSet<string>(walk.Fresh);
        for (int k = 0; k <= step.Hop && k < walk.Hops.Count; k++)
        {
            var (to, link) = walk.Hops[k];
            if (link != null) held.Remove(link);
            if (k < step.Hop || step.Here == to) held.Remove(to.StableKey);
        }
        return held;
    }

    /// <summary>
    /// Moves the glyph on by the time elapsed, ticking at each card it enters, and ends the walk once
    /// the walker has stood a moment where it arrived.
    /// </summary>
    private void AdvanceWalk()
    {
        if (_walk is not { } walk) return;
        if (!walk.Settled)
        {
            int last = walk.Steps.Count - 1;
            int at = walk.StepMs <= 0 ? last
                   : Math.Min(last, (int)((DateTime.UtcNow - walk.Start).TotalMilliseconds / walk.StepMs));
            if (at != walk.Shown && walk.Steps[at].Here != walk.Steps[walk.Shown].Here)
                _ambianceEngine?.TriggerGameEvent(GameEventType.SmallInteraction);
            walk.Shown = at;
            if (!walk.Settled) return;
        }
        walk.SettledAt ??= DateTime.UtcNow;
        bool instant = Cli.CliMode.IsActive && !Record.RecordMode.IsActive;
        if (!instant && (DateTime.UtcNow - walk.SettledAt.Value).TotalMilliseconds < WalkDwellMs) return;
        _walk = null;
        walk.Then?.Invoke();
    }

    // ── Walking by known ways ────────────────────────────────────────────────

    /// <summary>
    /// Every area the played member may walk to for nothing, each with the way there: only ways the
    /// party knows (<see cref="AreaVisits.BackReach"/>) — never through a door locked against the
    /// comer, never from other ground into private. Nothing outside a real place.
    /// </summary>
    private Dictionary<Area, List<(Area To, string? Link)>> Reach(MemberMind mind)
        => Exploring ? AreaVisits.BackReach(_scene, mind.Pov.Where, Period) : new();

    private bool CanReach(Area area)
        => area.Id == _pov.Where.Id || Reach(Mind).ContainsKey(area);

    /// <summary>
    /// The way with every path opened up into the areas it runs past, each one step along the
    /// neighbour way the path follows: a path is a walk past those areas, and anybody standing in one
    /// of them is standing in the way.
    /// </summary>
    private static IReadOnlyList<(Area To, string? Link)> ThroughPaths(Area from, IReadOnlyList<(Area To, string? Link)> hops)
    {
        var opened = new List<(Area To, string? Link)>();
        var at = from;
        foreach (var (to, link) in hops)
        {
            var path = link == null ? null
                     : at.PointsOfInterest.Concat(to.PointsOfInterest).OfType<PathPointOfInterest>()
                         .FirstOrDefault(p => p.StableKey == link);
            if (path != null && path.Via.Count > 0)
            {
                var via = path.AreaA.Id == at.Id ? path.Via : path.Via.Reverse().ToList();
                foreach (var v in via) { opened.Add((v, AreaVisits.LinkKey(at, v))); at = v; }
                opened.Add((to, AreaVisits.LinkKey(at, to)));
            }
            else opened.Add((to, link));
            at = to;
        }
        return opened;
    }

    /// <summary>Who confronts <paramref name="member"/> in <paramref name="area"/> at this hour, read as if they stood there.</summary>
    private Confrontation? ConfrontationAt(Area area, PartyMember member)
        => Exploring ? ConfrontationSelector.Current(_scene, new PoV(area, Period), member) : null;

    /// <summary>The confrontation <paramref name="mind"/>'s member is in where they stand, if any.</summary>
    private Confrontation? ConfrontationOf(MemberMind mind) => ConfrontationAt(mind.Pov.Where, mind.Member);

    /// <summary>Whether <paramref name="anchor"/> is the one confronting, or — facing an enemy — any enemy there.</summary>
    private bool IsConfronter(NarrativeAnchor anchor, Confrontation confrontation)
        => anchor is SyntheticNpcObservationObject npc
        && (ReferenceEquals(npc.NpcEntity, confrontation.Npc)
            || (confrontation.Kind == ConfrontationKind.Enemy && ElementKinds.IsHostile(npc.SceneNpc, _active)));

    /// <summary><paramref name="mind"/>'s member stands in <paramref name="area"/> from now on.</summary>
    private void StandIn(MemberMind mind, Area area)
    {
        if (mind.Pov.Where.Id == area.Id) return;
        mind.Pov.Where = area;
        mind.Pov.Focus = null;
        if (!Exploring) return;
        AreaVisits.Arrive(_scene, area);
        _recorder?.OnAreaEntered(mind.Pov);
        SceneDebugManager.UpdatePoV(_pov);
    }

    /// <summary>
    /// The played member walks to <paramref name="target"/> by ways the party knows, for nothing, and
    /// <paramref name="then"/> follows on arrival — what they went there to do. Already there, it
    /// follows at once.
    ///
    /// <para><b>The walk is stopped where somebody would confront them.</b> Every area on the way but
    /// the last is weighed as though the member stood in it (the areas a path runs past among them):
    /// in the first where an enemy or a witness would stop them, they stand, confronted, and what
    /// they set out to do is dropped. No roll is made — only RUNAWAY rolls. Arriving, whatever they
    /// came to do is judged there as anything done there is.</para>
    /// </summary>
    private void WalkTo(Area target, Action then)
    {
        var mind = Mind;
        var from = mind.Pov.Where;
        if (from.Id == target.Id) { then(); return; }
        if (!Reach(mind).TryGetValue(target, out var route))
        {
            Console.WriteLine($"NarrativeController: no known way from '{from.DisplayName}' to '{target.DisplayName}'");
            _loading = null;
            return;
        }

        var hops = ThroughPaths(from, route);
        int stop = -1;
        for (int j = 0; j < hops.Count - 1 && stop < 0; j++)
            if (ConfrontationAt(hops[j].To, mind.Member) != null) stop = j;

        var leg = stop < 0 ? hops : hops.Take(stop + 1).ToList();
        var arrive = leg[^1].To;
        _lastWalk = $"walk from {from.DisplayName} to {arrive.DisplayName}" + (stop < 0 ? "" : " stopped");
        Console.WriteLine($"NarrativeController: {_lastWalk}");
        StartWalk(_map.Walk(_scene, from, leg), leg, "Walking...", then: () =>
        {
            foreach (var (to, link) in leg)
            {
                AreaVisits.Arrive(_scene, to);
                if (link != null) _scene.TakenLinks.Add(link);
            }
            StandIn(mind, arrive);
            if (stop < 0) { then(); return; }
            Console.WriteLine($"NarrativeController: stopped in '{arrive.DisplayName}' by "
                            + $"'{ConfrontationOf(mind)?.Npc.DisplayName}' — what was set out for is dropped");
            _loading = null;
        });
    }

    // ── The walk an act makes ────────────────────────────────────────────────

    /// <summary>
    /// Records a move an act made (<see cref="AreaVisits.Move"/>) and shows it walked: what the move
    /// taught the map is revealed as the walk reaches it. The member has already been moved.
    /// </summary>
    private void MoveOnMap(Area from, Area to, Element? by)
    {
        var areasBefore = new HashSet<string>(_scene.VisitedAreas);
        var linksBefore = new HashSet<string>(_scene.TakenLinks);
        var hops = AreaVisits.Move(_scene, from, to, by, Period);
        var fresh = _scene.VisitedAreas.Where(k => !areasBefore.Contains(k))
                    .Concat(_scene.TakenLinks.Where(k => !linksBefore.Contains(k)))
                    .ToHashSet();
        _lastWalk = $"walk from {from.DisplayName} to {to.DisplayName} "
                  + $"via={string.Join(",", hops.Select(h => h.Link == null ? "jump" : "line"))}";
        Console.WriteLine($"NarrativeController: {_lastWalk}");
        StartWalk(_map.Walk(_scene, from, hops), hops, "Walking...", then: null, fresh);
    }

    // ── The way in ───────────────────────────────────────────────────────────

    /// <summary>The party coming into the place, on the map: from the way in to the card it is entered by.</summary>
    private void ShowEntryWalk()
    {
        var steps = _map.WalkIn(_scene, _pov.Where);
        _lastWalk = $"walk in to {_pov.Where.DisplayName}";
        StartWalk(steps, Array.Empty<(Area, string?)>(), "Entering...", then: null, walkers: _minds.Keys.ToList());
    }

    /// <summary>
    /// A routine opens in its own area — "you know the way" — and this shows the way: the party
    /// walking on the map from where the place is entered (<paramref name="entry"/>) to the routine's
    /// area, by the ways the protagonist knows (a jump, card to card, when the map holds none at that
    /// hour). Nothing is rolled and nothing new is learned. <paramref name="then"/> runs on arrival.
    /// </summary>
    private void WalkTheApproach(Area entry, Action? then)
    {
        var steps = _map.WalkIn(_scene, entry);
        IReadOnlyList<(Area To, string? Link)> hops = Array.Empty<(Area, string?)>();
        if (entry.Id != _pov.Where.Id)
        {
            hops = AreaVisits.MoveBack(_scene, entry, _pov.Where, Period);
            var onward = _map.Walk(_scene, entry, hops);
            steps = steps.Count > 1 ? steps.Concat(onward.Skip(1)).ToList() : onward;
        }
        _lastWalk = $"walk in to {_pov.Where.DisplayName}";
        StartWalk(steps, hops, "Entering...", then, walkers: _minds.Keys.ToList());
    }

    // ── Being taken somewhere ────────────────────────────────────────────────

    /// <summary>
    /// Takes the played member to the person a go-between has just agreed to present them to, who is
    /// from then on known to them by sight. Returns false when that person is nowhere to be found at
    /// this hour — the reeve keeps his own times — in which case the standing still stands and the
    /// walk simply does not happen. Being vouched for does not conjure somebody into a room.
    /// </summary>
    public bool WalkToIntroducedNpc(NpcEntity presented)
    {
        var sceneNpc = _scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, presented));
        if (sceneNpc == null || _scene.GetAreaOf(sceneNpc, Period) is not { } where)
        {
            Console.WriteLine($"NarrativeController: {presented.DisplayName} is not about at {Period} — introduction stands, but no walk");
            return false;
        }
        LedTo(where, sceneNpc);
        return true;
    }

    /// <summary>
    /// Takes the played member to the premises of a trader who agreed to lead them there, at the
    /// first hour the trader keeps them (<see cref="TradeGate.NextOnPremises"/>) — now if they are
    /// already there by then, else later in the day, the time passing as it does when one waits.
    /// Returns false, and moves nobody, when the trader's day never brings them there.
    /// </summary>
    public bool WalkToPremises(NpcEntity trader)
    {
        var sceneNpc = _scene.Npcs.FirstOrDefault(n => ReferenceEquals(n.Entity, trader));
        if (sceneNpc == null || TradeGate.NextOnPremises(_scene, sceneNpc, Period) is not { } visit)
        {
            Console.WriteLine($"NarrativeController: {trader.DisplayName} keeps no premises at any hour — no walk");
            return false;
        }
        if (visit.When != Period)
        {
            Console.WriteLine($"NarrativeController: waiting until {visit.When} for {trader.DisplayName}'s premises");
            TurnTheDayIfWrapped(Period, visit.When);
            ShowTimePassage(Period, visit.When);
            ApplyTimePeriod(visit.When);
        }
        LedTo(visit.Area, sceneNpc);
        return true;
    }

    /// <summary>The played member is led to <paramref name="where"/>, to <paramref name="whom"/>: walked there, and that person known.</summary>
    private void LedTo(Area where, SceneNpc whom)
    {
        var from = _pov.Where;
        if (ElementMemory.KeyOf(whom) is { } key) _scene.KnownElements.Add(key);
        if (from.Id == where.Id) return;
        Mind.Pov.Where = where;
        AreaVisits.Arrive(_scene, where);
        _recorder?.OnAreaEntered(_pov);
        MoveOnMap(from, where, by: null);
        Console.WriteLine($"NarrativeController: led to {whom.Entity.DisplayName} in {where.DisplayName}");
    }

    // ── The way out ──────────────────────────────────────────────────────────

    /// <summary>What the way out is for the played member: RUNAWAY while somebody confronts them, else LEAVE.</summary>
    private string ExitLabel => ConfrontationOf(Mind) != null ? "RUNAWAY" : "LEAVE";

    /// <summary>
    /// The way out was clicked. Free, it is LEAVE: the whole party walks out. With somebody
    /// confronting the played member it is RUNAWAY, theirs alone: a roll to get away from them.
    /// </summary>
    private void OnExitClicked()
    {
        if (ConfrontationOf(Mind) is { } confrontation)
        {
            Console.WriteLine($"NarrativeController: RUNAWAY — '{confrontation.Npc.DisplayName}' ({confrontation.Kind})");
            _escape = NewEscape(new[] { Mind });
            _ = RollRunawayAsync(confrontation);
            return;
        }
        Console.WriteLine("NarrativeController: LEAVE — the party walks out");
        Leave(_minds.Values.OrderBy(m => ReferenceEquals(m, Mind) ? 0 : 1).ToList());
    }

    /// <summary>
    /// LEAVE: every member walks from where they stand to the area the place is entered by, and out.
    /// Those standing together go together. A walk out is stopped as any walk is: where somebody on
    /// the way would confront one of them they stand, confronted, the leaving ends there, and the
    /// view goes to whoever was stopped — the rest stay where they had got to.
    /// </summary>
    private void Leave(List<MemberMind> remaining)
    {
        if (remaining.Count == 0)
        {
            _requestedExit = true;
            return;
        }

        var from  = remaining[0].Pov.Where;
        var group = remaining.Where(m => m.Pov.Where.Id == from.Id).ToList();
        var rest  = remaining.Except(group).ToList();
        var entry = _map.EntryAreaOf(_scene) ?? from;
        var walkers = group.Select(m => m.Member).ToList();

        IReadOnlyList<(Area To, string? Link)> hops =
              from.Id == entry.Id ? Array.Empty<(Area, string?)>()
            : Reach(group[0]).TryGetValue(entry, out var route) ? ThroughPaths(from, route)
            : new[] { (entry, (string?)null) };

        // Standing where they are, and then each area on the way: is one of them stopped?
        MemberMind? stopped = group.FirstOrDefault(m => ConfrontationOf(m) != null);
        int stop = -1;
        for (int j = 0; stopped == null && j < hops.Count; j++)
            if ((stopped = group.FirstOrDefault(m => ConfrontationAt(hops[j].To, m.Member) != null)) != null) stop = j;

        if (stopped != null)
        {
            var leg = hops.Take(stop + 1).ToList();
            void Halt()
            {
                foreach (var m in group)
                    if (leg.Count > 0) StandIn(m, leg[^1].To);
                Switch(stopped.Member);
                Console.WriteLine($"NarrativeController: leaving stopped in '{stopped.Pov.Where.DisplayName}' by "
                                + $"'{ConfrontationOf(stopped)?.Npc.DisplayName}'");
            }
            if (leg.Count == 0) { Halt(); return; }
            _lastWalk = $"walk from {from.DisplayName} to {leg[^1].To.DisplayName} stopped";
            StartWalk(_map.Walk(_scene, from, leg), leg, "Leaving...", Halt, walkers: walkers);
            return;
        }

        _lastWalk = $"walk out from {from.DisplayName}";
        Console.WriteLine($"NarrativeController: {_lastWalk}");
        foreach (var (to, _) in hops) AreaVisits.Arrive(_scene, to);
        StartWalk(_map.WalkOut(_scene, from, hops), hops, "Leaving...", walkers: walkers, then: () =>
        {
            foreach (var m in group) GoneOut(m);
            Leave(rest);
        });
    }

    /// <summary>
    /// <paramref name="mind"/>'s member is out of the place: no longer on the map, and not to come
    /// back this visit. When nobody is left the visit is over; else somebody still inside is played.
    /// </summary>
    private void GoneOut(MemberMind mind)
    {
        _minds.Remove(mind.Member);
        if (_minds.Count == 0)
        {
            _minds[mind.Member] = mind;   // the visit ends with somebody to read it from
            _requestedExit = true;
            return;
        }
        if (ReferenceEquals(_active, mind.Member)) Switch(_minds.Keys.First());
    }

    // ── RUNAWAY ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Members on their way out past whoever would stop them: the known way from where they stand to
    /// the area the place is entered by. At every area on it where somebody confronts one of them they
    /// are stopped and roll to get past; a failed roll there ends it, and they stay. The area the
    /// escape starts from is not one of its stops: the RUNAWAY's own roll, or the fight fled, has
    /// already got them away from it.
    /// </summary>
    private sealed class EscapeRoute
    {
        public required IReadOnlyList<MemberMind> Who { get; init; }
        public required IReadOnlyList<(Area To, string? Link)> Hops { get; init; }
        /// <summary>How many of <see cref="Hops"/> have been walked.</summary>
        public int Done;
    }
    private EscapeRoute? _escape;

    // The roll in flight, read on the dice CONTINUE.
    private Confrontation? _runawayFrom;

    private EscapeRoute NewEscape(IReadOnlyList<MemberMind> who)
    {
        var here  = who[0].Pov.Where;
        var entry = _map.EntryAreaOf(_scene) ?? here;
        IReadOnlyList<(Area To, string? Link)> hops =
              here.Id == entry.Id ? Array.Empty<(Area, string?)>()
            : Reach(who[0]).TryGetValue(entry, out var route) ? ThroughPaths(here, route)
            : new[] { (entry, (string?)null) };
        return new EscapeRoute { Who = who, Hops = hops };
    }

    /// <summary>
    /// Plays the runaway roll: as many dice as the played member's feet give, a single six to get
    /// away — the same rule as in a fight. Resolved on the dice CONTINUE (<see cref="FinishRunaway"/>).
    /// </summary>
    private async Task RollRunawayAsync(Confrontation from)
    {
        _runawayFrom = from;
        int dice = _active.DerivedStats.First(s => s.Name == "runaway_dice").GetValue(_active);
        // The humors reach this roll as they reach every other.
        NarrationDiceStart(dice, 1, humorMember: _active, subtitle: "RUNAWAY CHECK — feet", difficultyVerb: "to flee");
        await Task.Delay(Config.Dice.AnimationDurationMs);

        var values = new int[dice];
        for (int i = 0; i < dice; i++) values[i] = _diceRandom.Next(1, 7);
        // --debug's preset strategy reaches this roll as it reaches the action and conversation
        // rolls, so a script can test both ends of a RUNAWAY rather than whichever its seed lands on.
        if (DebugMode.IsActive && !DebugMode.IsAutoStrategy)
        {
            if (DebugMode.CurrentStrategy == DebugStrategy.FailDiceRoll)
                for (int i = 0; i < values.Length; i++) { if (values[i] == 6) values[i] = 5; }
            else if (DebugMode.CurrentStrategy == DebugStrategy.Succeed && values.Length > 0)
                values[0] = 6;
        }
        NarrationDiceComplete(values);
    }

    /// <summary>
    /// Resolves a runaway roll on the dice box's CONTINUE. A success goes on along the way out, to the
    /// next area where somebody stands in the way or out of the place. A failure depends on where the
    /// roll was made: where RUNAWAY was clicked, the fight with the enemy or the conversation with
    /// the witness begins; at a stop further along, the escape ends there and they stay, confronted.
    /// </summary>
    private void FinishRunaway()
    {
        bool success = _dice.IsCurrentlySuccess;
        var from = _runawayFrom;
        var escape = _escape;
        _runawayFrom = null;
        NarrationDiceClear();
        if (from == null || escape == null) return;

        if (success)
        {
            Console.WriteLine($"NarrativeController: RUNAWAY succeeded in '{_pov.Where.DisplayName}' — going on");
            ContinueEscape();
            return;
        }

        _escape = null;
        if (escape.Done > 0)
        {
            Console.WriteLine($"NarrativeController: RUNAWAY failed on the way out in '{_pov.Where.DisplayName}' — stopped by '{from.Npc.DisplayName}'");
            return;
        }
        if (from.Kind == ConfrontationKind.Enemy)
        {
            Console.WriteLine($"NarrativeController: RUNAWAY failed — fight with '{from.Npc.DisplayName}'");
            _pendingFightOutcome = new FightTriggerOutcome(from.Npc, $"failed to run away from {from.Npc.DisplayName}");
        }
        else
        {
            // They came because they heard a crime: they accuse the member of that one. Otherwise they
            // caught them on private ground, and what they saw is trespass.
            var tree = ConfrontationSelector.TreeFor(_scene, from.Npc, null, _pov.Where.IsPrivate);
            Console.WriteLine($"NarrativeController: RUNAWAY failed — witness '{from.Npc.DisplayName}' confronts ({tree.TreeId})");
            SetPendingDialogue(new DialogueTriggerOutcome(from.Npc, tree: tree));
        }
    }

    /// <summary>
    /// Walks the escape on: to the next area on the way where somebody confronts one of those
    /// escaping, and the roll there; or, with nobody in the way, out of the place.
    /// </summary>
    private void ContinueEscape()
    {
        if (_escape is not { } e) return;
        var from = e.Who[0].Pov.Where;
        var walkers = e.Who.Select(m => m.Member).ToList();

        for (int j = e.Done; j < e.Hops.Count; j++)
        {
            var area = e.Hops[j].To;
            var stoppedBy = e.Who.Select(m => ConfrontationAt(area, m.Member)).FirstOrDefault(c => c != null);
            if (stoppedBy == null) continue;

            var leg = e.Hops.Skip(e.Done).Take(j - e.Done + 1).ToList();
            e.Done = j + 1;
            _lastWalk = $"walk from {from.DisplayName} to {area.DisplayName} stopped";
            Console.WriteLine($"NarrativeController: {_lastWalk}");
            StartWalk(_map.Walk(_scene, from, leg), leg, "Running away...", walkers: walkers, then: () =>
            {
                foreach (var m in e.Who) StandIn(m, area);
                Console.WriteLine($"NarrativeController: way out stopped in '{area.DisplayName}' by '{stoppedBy.Npc.DisplayName}' ({stoppedBy.Kind})");
                _ = RollRunawayAsync(stoppedBy);
            });
            return;
        }

        var rest = e.Hops.Skip(e.Done).ToList();
        _escape = null;
        foreach (var (to, _) in rest) AreaVisits.Arrive(_scene, to);
        _lastWalk = $"walk out from {from.DisplayName}";
        Console.WriteLine($"NarrativeController: {_lastWalk}");
        StartWalk(_map.WalkOut(_scene, from, rest), rest, "Running away...", walkers: walkers, then: () =>
        {
            foreach (var m in e.Who) GoneOut(m);
        });
    }

    /// <summary>
    /// The members who were in a fight have fled it: they make for the way out, checked in every area
    /// past this one (the fight itself was the getting away from here). Called by the game controller
    /// on the fight's return.
    /// </summary>
    public void FleeAfterFight()
    {
        _result = null;
        _escape = NewEscape(_minds.Values.Where(m => m.Pov.Where.Id == _pov.Where.Id)
                                         .OrderBy(m => ReferenceEquals(m, Mind) ? 0 : 1).ToList());
        ContinueEscape();
    }
}
