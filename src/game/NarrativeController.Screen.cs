using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Audio;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Preview;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;
using Cathedral.Game.Scene.Verbs;
using OpenTK.Mathematics;

namespace Cathedral.Game;

// The screen: what is drawn each tick, what the pointer is over, and what a click does.
public partial class NarrativeController
{
    // The pointer, and what it is over.
    private int _lastMouseX, _lastMouseY;
    private MapSymbol? _hovered;
    private bool _mapLabelShown;

    // Buttons of the boxes drawn over the map, as last rendered (Width 0: not shown this frame).
    private (int X, int Y, int Width) _previewContinueRegion;
    private bool _previewContinueHovered;
    private ((int X, int Y, int Width) Yes, (int X, int Y, int Width) No) _previewAnswerRegions;
    private bool? _previewAnswerHovered;
    private string _lastPreviewText = "";
    private (int X, int Y, int Width) _resultContinueRegion;
    private bool _resultContinueHovered;
    // The out-of-points box: its buttons as last drawn (the way out first, then a member each), and
    // the one under the pointer.
    private readonly List<(int X, int Y, int Width)> _spentRegions = new();
    private int _spentHovered = -1;
    private bool _diceButtonHovered;

    // Read once a frame, for every symbol's state.
    private Confrontation? _frameConfrontation;
    /// <summary>The areas the played member may walk to by known ways, each with how many steps away; where they stand is 0.</summary>
    private Dictionary<Guid, int> _frameReach = new();
    /// <summary>The unobserved ways drawn on their lines: the end each is looked at from, and its observation object there.</summary>
    private readonly Dictionary<string, (Area From, NarrativeAnchor Anchor, string Pair)> _frameWays = new();

    /// <summary>
    /// The ways nothing is known of that join the same two areas as the way recorded as
    /// <paramref name="key"/>, seen from the same end, that way among them — or none when it is
    /// itself known. Several of them are one line on the map and one question to a mind: which of
    /// them it looks at is its own to choose, as with the things of a kind on a card.
    /// </summary>
    private List<NarrativeAnchor> SiblingWays(string key)
    {
        if (!_frameWays.TryGetValue(key, out var mine) || ElementMemory.IsKnown(_scene, mine.From, mine.Anchor))
            return new List<NarrativeAnchor>();
        return _frameWays.Values
            .Where(w => w.Pair == mine.Pair && w.From.Id == mine.From.Id && !ElementMemory.IsKnown(_scene, w.From, w.Anchor))
            .Select(w => w.Anchor).ToList();
    }

    /// <summary>What a question about the unknown ways between two areas is remembered under.</summary>
    private string PairKey(string key) => _frameWays.TryGetValue(key, out var way) ? $"ways|{way.Pair}" : key;

    /// <summary>
    /// Whether something holds the screen: a mind at work, a text being read, a roll, the hours
    /// passing, somebody walking, a result. Nothing on the map answers a click meanwhile.
    /// </summary>
    private bool Busy => _error != null || _loading != null || _previewSession.IsActive || _dice.IsVisible
                      || _timeBox.IsVisible || _walk != null || _result != null;

    // ── Each tick ────────────────────────────────────────────────────────────

    /// <summary>Called at 10 Hz by the game controller.</summary>
    public void Update()
    {
        // The party changes under the narration — somebody recruited by a conversation, somebody
        // dead of a wound — and nothing tells it so.
        if (SyncRoster()) ReconcileTrees();
        SettleSpent();
        // A press kept on the pan pad keeps the map moving.
        _map.Hold(!Busy && !IsPopupVisible && !OutOfPoints && !_core.VirtualPointerOnly
                  && _core.MouseState.IsButtonDown(OpenTK.Windowing.GraphicsLibraryFramework.MouseButton.Left));
        RenderPanel();
        // A popup is a modal choice, so the panel behind it is greyed the way the preview box greys
        // it: recolouring what is drawn rather than darkening the whole HUD, so the popup itself (a
        // separate terminal) stays at full strength.
        if (IsPopupVisible) _ui.DimContent();
        UpdateMapLabel();
    }

    private void RenderPanel()
    {
        // The childhood is played in the dark: nothing of the screen but the memory.
        _ui.Blackout = MinimalScreen;
        _ui.Clear();

        // The dice-roll music plays only while the dice are tumbling.
        if (_ambianceEngine != null)
        {
            var desired = _dice.IsVisible && _dice.IsRolling ? MusicFilter.DiceRoll
                        : _loading != null                    ? MusicFilter.Loading
                        : MusicFilter.None;
            if (_ambianceEngine.ActiveFilter != desired) _ambianceEngine.SetFilter(desired);
        }

        _ui.RenderHeader(_active.DisplayName, Mind.Noetic, _active.MaxNoeticPoints,
            showNoeticPoints: !InOpeningPhase, trespassing: Exploring ? IsTrespassing : null);

        if (_error != null)
        {
            _ui.ShowError(_error);
            _ui.RenderStatusBar(SceneInfoLine());
            return;
        }

        AdvanceWalk();
        _map.Bare = MinimalScreen;
        _overlay.Frame = BuildFrame();
        // The way pointed at — by its symbol, by a node of its tree, or along its line — is lit whole.
        // Only a way that answers: a symbol a click would observe, or the tree of an observed one.
        _map.LitWay = Busy || IsPopupVisible ? null : _hovered switch
        {
            WaySymbol way when IsEnabled(way) => way.Key,
            NodeSymbol { Node: var over } => WayKeyOf(over.ElementNode),
            _ => null,
        };

        // The map is always what is drawn; everything else is a box over it.
        if (_walk is { } walk)
        {
            var step = walk.Steps[walk.Shown];
            _ui.RenderMap(_map, _scene, step.Here, Period, _overlay, (step.X, step.Y), WalkWithheld());
        }
        else
            _ui.RenderMap(_map, _scene, _pov.Where, Period, _overlay);

        _previewContinueRegion = default;
        _previewAnswerRegions  = default;
        _resultContinueRegion  = default;
        _spentRegions.Clear();

        if (_dice.IsVisible)
        {
            _dice.Advance();
            _ui.RenderDiceComponent(_dice, _diceButtonHovered);
            _ui.RenderStatusBar(_dice.IsRolling ? Config.LoadingMessages.RollingDice
                : _dice.IsCurrentlySuccess ? "Success! Click Continue to see the outcome"
                                           : "Failed! Click Continue to see the outcome");
            return;
        }

        if (_timeBox.IsVisible)
        {
            _timeBox.Advance();
            _ui.RenderTimePassage(_timeBox);
            _ui.RenderStatusBar(_timeBox.IsChoosing ? "Choose the hour to wait until"
                              : _timeBox.IsAnimating ? "Time passes..." : "Click Continue");
            return;
        }

        if (_walk != null)
        {
            _ui.RenderStatusBar(_walk.Status);
            return;
        }

        // The text being written, gated by CONTINUE: for the whole life of the preview session.
        if (_previewSession.IsActive)
        {
            var snap = _previewSession.Snapshot();
            // The hover tick, whenever new text streams in (typewriter feedback).
            if (snap.DisplayText != _lastPreviewText)
            {
                PlayHoverSound();
                _lastPreviewText = snap.DisplayText;
            }
            var region = _ui.RenderPreviewBox(snap, _previewContinueHovered, _previewAnswerHovered);
            _previewContinueRegion = region.Present ? (region.X, region.Y, region.Width) : default;
            _previewAnswerRegions  = _ui.PreviewAnswerRegions;
            _ui.RenderWaitingStatus(_loading ?? "");
            return;
        }
        _lastPreviewText = "";

        if (_loading != null)
        {
            _ui.DimContent();
            _ui.RenderCenterProgressBar();
            _ui.RenderWaitingStatus(_loading);
            return;
        }

        if (_result != null)
        {
            _resultContinueRegion = _ui.RenderResultBox(_result.Succeeded, _result.Act, _result.Chips, _resultContinueHovered);
            _ui.RenderStatusBar("Click Continue");
            return;
        }

        // The last point is spent: the box says what that means and holds the only ways on.
        if (SpentBoxUp)
        {
            var (message, buttons) = SpentNotice();
            _spentRegions.AddRange(_ui.RenderSpentBox(" NO NOETIC POINT LEFT ", message, buttons, _spentHovered));
            _ui.RenderStatusBar(SceneInfoLine());
            return;
        }

        // Idle: what the pointer is over is read again.
        if (!IsPopupVisible && _hovered is NodeSymbol { Node: var node } && ReadingOf(node) is { } reading)
            _ui.RenderReadingBox(reading.Title, reading.Text, node.Block?.OutcomeReports, reading.Odds);
        _ui.RenderStatusBar(SceneInfoLine());
    }

    /// <summary>The footer while nothing is happening: "Biome — area | hour".</summary>
    private string SceneInfoLine()
    {
        string biome = _worldContext.DisplayName;
        string where = $"{_pov.Where.DisplayName}  |  {Period}";
        return biome.Length > 0 ? $"{biome}  —  {where}" : where;
    }

    /// <summary>
    /// What the out-of-points box says and offers: why nothing else answers, the way out — LEAVE,
    /// or RUNAWAY from whoever confronts the member — and each companion who still has a point,
    /// to be played instead.
    /// </summary>
    private (string Message, List<string> Buttons) SpentNotice()
    {
        var others = MembersWithPoints;
        // In the played member's own voice — a mind with nothing left to give — and still saying
        // the rule in so many words: no point left, and every look, thought and act costs one.
        string message;
        if (ConfrontationOf(Mind) is { } confrontation)
            message = $"I have run out of ideas, and {NpcLabelResolver.ShortLabel(confrontation.Npc)} stands in my way. "
                    + "I have no noetic point left, and every look, every thought and every act costs one. "
                    + "All I can do is run.";
        else
            message = "I have run out of ideas. I have looked, thought and tried until my mind gave nothing more: "
                    + "I have no noetic point left, and every look, every thought and every act costs one. "
                    + "There is nothing more I can do here. It is time to leave: my mind will be clear again "
                    + "in the next place I come to.";
        if (others.Count > 0)
            message += others.Count == 1 ? $" But {others[0].DisplayName} can still act." : " But the others can still act.";

        // The way out under its own name: what the glyph at the map's edge is called.
        var buttons = new List<string> { ExitLabel };
        buttons.AddRange(others.Select(m => $"PLAY {m.DisplayName.ToUpperInvariant()}"));
        return (message, buttons);
    }

    /// <summary>A press on one of that box's buttons: the way out, or the member to play.</summary>
    private void ClickSpent(int button)
    {
        PlayClickSound();
        _spentHovered = -1;
        if (button == 0) { OnExitClicked(); return; }
        var others = MembersWithPoints;
        if (button - 1 < others.Count) Switch(others[button - 1]);
    }

    // ── What the map shows ───────────────────────────────────────────────────

    /// <summary>
    /// The childhood is the first screen a player meets, and holds only what it is about: one
    /// symbol in the dark — the one remembering — and, once it is looked at, the tree of what
    /// comes back. No card, no panel, no token, no line, no header and no sky.
    /// </summary>
    private bool MinimalScreen => _scene.Phase == NarrationPhase.ChildhoodReminescence;

    /// <summary>Reads off the game everything the overlay draws this frame.</summary>
    private MapFrame BuildFrame()
    {
        _frameConfrontation = ConfrontationOf(Mind);
        _frameReach = Reach(Mind).ToDictionary(r => r.Key.Id, r => r.Value.Count);
        _frameReach[_pov.Where.Id] = 0;

        var frame = new MapFrame
        {
            Active       = _active,
            Hovered      = Busy || IsPopupVisible ? null : _hovered,
            // "Myself" is drawn as the protagonist is everywhere else.
            ElementGlyph = node => node.Element is Scene.GetUp.SelfPointOfInterest ? Config.GlyphSphere.ProtagonistChar
                                                                                    : ElementKinds.Glyph(KindOf(node)),
            IsHostile    = node => KindOf(node) == ElementKind.Hostile,
            Contents     = ContentsOf,
            Stubs        = StubsOf,
            Commands     = CommandsOf,
            Enabled      = symbol => !Busy && IsEnabled(symbol),
            Exit         = Exploring,
            Minimal      = MinimalScreen,
        };

        var cards = new Dictionary<Guid, CardFrame>();
        CardFrame Card(Area area)
        {
            if (!cards.TryGetValue(area.Id, out var card))
            {
                cards[area.Id] = card = new CardFrame { Area = area };
                frame.Cards.Add(card);
            }
            return card;
        }

        // Where everybody stands, the one played first. Whoever is walking is on the way, not on a card.
        var walking = _walk?.Walkers ?? Array.Empty<PartyMember>();
        foreach (var mind in _minds.Values.OrderBy(m => ReferenceEquals(m, Mind) ? 0 : 1))
            if (!walking.Contains(mind.Member))
                Card(mind.Pov.Where).Tokens.Add(mind.Member);

        // What is not yet observed, in every area: what stands in a place nobody has been to is
        // shown too, and answers no click until a known way leads there.
        foreach (var area in _scene.AllAreas)
        {
            var tray = Card(area).Tray;
            foreach (var (anchor, kind, known) in Unobserved(area))
                tray.Add(new TraySymbol(area, kind, known ? anchor : null, tray.Count));
        }

        // The ways, each on its line: its tree once observed, its symbol before.
        _frameWays.Clear();
        foreach (var (a, b, key, connector) in AreaVisits.AllLinks(_scene))
        {
            if (a.Id == b.Id || connector is LandscapePointOfInterest || !_map.DrawsWay(_scene, key)) continue;
            var root = WayRoot(Mind, key);
            if (root == null && WaySide(a, b, connector) is { } side) _frameWays[key] = (side.From, side.Anchor, AreaVisits.LinkKey(a, b));
            else if (root == null || Locate(root) == null) continue;
            var way = new WayFrame
            {
                Key   = key,
                Root  = root,
                // What a way turned out to be is its symbol once it is known: seen or gone by.
                Glyph = connector != null && (_scene.SeenLinks.Contains(key) || _scene.TakenLinks.Contains(key))
                            ? LocationMap.KindLetter(connector) : Config.Symbols.MapWay,
                From  = root?.SeenFrom,
            };
            way.Strays.AddRange(Mind.WayStrays.Where(s => s.Way == key).Select(s => s.Node));
            frame.Ways.Add(way);
        }

        // The played member's other trees, each under the card of where its element now is. The
        // childhood's one tree is drawn whole wherever its elements were: most are of memories
        // whose scenes are gone.
        foreach (var root in Mind.Roots)
            if (MinimalScreen) Card(_pov.Where).Roots.Add(root);
            else if (WayKeyOf(root) == null && Locate(root) is { } at)
                Card(at.Where).Roots.Add(root);
        foreach (var (area, refusal) in Mind.Strays)
            Card(area).Strays.Add(refusal);

        return frame;
    }

    /// <summary>The kind an observed element is of, as the played member sees it now. A thing out of sight is a thing.</summary>
    private ElementKind KindOf(ThoughtNode element) => element.Element switch
    {
        SceneNpc npc => ElementKinds.IsHostile(npc, _active) ? ElementKind.Hostile : ElementKind.Person,
        SleepingNpcPointOfInterest asleep => ElementKinds.IsHostile(asleep.Sleeper, _active) ? ElementKind.Hostile : ElementKind.Person,
        Cathedral.Game.Scene.GetUp.SelfPointOfInterest => ElementKind.Person,
        LandscapePointOfInterest => ElementKind.Thing,
        Area or ConnectorPointOfInterest => ElementKind.Way,
        _ => ElementKind.Thing,
    };

    /// <summary>
    /// What an observed element offers to do next: to think about it, and to speak of it to each
    /// companion within hearing — one speech mark a listener, so the symbol says to whom.
    /// </summary>
    private IReadOnlyList<StubSymbol> StubsOf(ThoughtNode element)
    {
        var stubs = new List<StubSymbol>();
        // Thinking is offered about what affords an act now: a way since walked, a thing emptied
        // or a person asleep keeps its tree and has nothing to be thought into.
        if (ThinkingMinds.Count > 0 && Locate(element) is { } at
            && GoalsOf(at.Anchor).Any(g => g.Verb is not IgnoreVerb))
            stubs.Add(new StubSymbol(element, StubKind.Think));
        if (Exploring && _active.GetSpeakingModiMentis().Count > 0)
            foreach (var listener in WithinSpeech())
                stubs.Add(new StubSymbol(element, StubKind.Speak, listener));
        return stubs;
    }

    /// <summary>
    /// What may be done with a waiting act besides carrying it out, each a symbol beside it: reach
    /// for an implement, think it over again. Neither in the two opening phases, whose one act has
    /// one means and no implement.
    /// </summary>
    private IReadOnlyList<ActCommand> CommandsOf(ThoughtNode act)
        => !act.IsWaitingAct || InOpeningPhase ? Array.Empty<ActCommand>() : AllCommands;

    private static readonly ActCommand[] AllCommands = { ActCommand.Tool, ActCommand.Reconsider };

    /// <summary>
    /// Whether the played member can get to where a waiting act is done: there already, or by ways
    /// the party knows while nobody holds them where they stand.
    /// </summary>
    private bool CanReachAct(ThoughtNode act)
        => LocateAct(act) is { } target
        && (target.Where.Id == _pov.Where.Id || (_frameConfrontation == null && _frameReach.ContainsKey(target.Where.Id)));

    /// <summary>
    /// Whether a click on <paramref name="symbol"/> would act now.
    ///
    /// <para>Observing, thinking and acting each cost a point and are withheld without one — with
    /// none left, only the way out and another member answer (<see cref="OutOfPoints"/>). Observing
    /// and acting need the member there, and are withheld where no known way leads. Confronted, a
    /// member may look only at whoever confronts them and think only about them — though an act
    /// already proposed stays theirs to try where they stand: the coded rules judge it on the
    /// click, and a discreet means may pass where another is refused.</para>
    /// </summary>
    private bool IsEnabled(MapSymbol symbol)
    {
        var confrontation = _frameConfrontation;
        // With the last point spent only the way out and another member answer.
        if (OutOfPoints && symbol is not (ExitSymbol or TokenSymbol)) return false;
        // What an element folded away holds and offers is put away with it.
        if (symbol switch
            {
                NodeSymbol { Node: var under }       => FoldedAway(under),
                StubSymbol { Element: var of }       => FoldedAway(of, self: true),
                ContentSymbol { Container: var box } => FoldedAway(box, self: true),
                CommandSymbol { Act: var beside }    => FoldedAway(beside),
                _ => false,
            }) return false;
        switch (symbol)
        {
            // Another member is played by a click on their token — one who has a point to spend:
            // there is nothing to play a member with none for.
            case TokenSymbol token:
                return !ReferenceEquals(token.Member, _active)
                    && (!Exploring || (_minds.TryGetValue(token.Member, out var theirs) && theirs.Noetic > 0));

            // An observed element: a click folds its tree away, or brings it back.
            case NodeSymbol { Node: { Kind: ThoughtNodeKind.Element } element }:
                return CanFold(element);

            case TraySymbol tray:
                if (!HasPoint || ObservingMinds.Count == 0) return false;
                if (confrontation == null) return _frameReach.ContainsKey(tray.Area.Id);
                return tray.Area.Id == _pov.Where.Id
                    && (tray.Known != null
                        ? IsConfronter(tray.Known, confrontation)
                        : Unobserved(tray.Area).Any(u => u.Kind == tray.Kind && !u.Known && IsConfronter(u.Anchor, confrontation)));

            case ContentSymbol content:
                return HasPoint && confrontation == null && ObservingMinds.Count > 0
                    && Locate(content.Container) is { } held && _frameReach.ContainsKey(held.Where.Id);

            // A way is looked at from one of its ends: the member must be able to walk to it.
            case WaySymbol way:
                return HasPoint && confrontation == null && ObservingMinds.Count > 0
                    && _frameWays.TryGetValue(way.Key, out var side) && _frameReach.ContainsKey(side.From.Id);

            case ExitSymbol:
                return Exploring;

            case StubSymbol { Kind: StubKind.Think } think:
                return HasPoint && Locate(think.Element) is { } thought
                    && (confrontation == null || IsConfronter(thought.Anchor, confrontation));

            case StubSymbol { Kind: StubKind.Speak, Listener: { } listener } speak:
                return Mind.Noetic > 0 && confrontation == null && Locate(speak.Element) != null
                    && WithinSpeech().Contains(listener);

            // An act refused for want of an implement is not carried out bare-handed: reaching for
            // one is then the only way on.
            // Carrying it out costs a point.
            case NodeSymbol { Node: { IsWaitingAct: true } act }:
                return HasPoint && !act.Action!.NeedsTool && CanReachAct(act);

            // Reaching for an implement is part of the act already proposed: it costs nothing when
            // it serves — but the act it is for costs a point, and so does reaching for the wrong
            // thing — only one thing may ever be combined with it, and something combinable must be
            // carried.
            case CommandSymbol { Command: ActCommand.Tool, Act: { IsWaitingAct: true } act }:
                return HasPoint && act.Action!.CombinedItem == null && CanReachAct(act) && GetCombinableItems().Count > 0;

            // Reconsidering is a thinking, and costs its point.
            case CommandSymbol { Command: ActCommand.Reconsider, Act: { IsWaitingAct: true } act }:
                return CanReconsider() && CanReachAct(act);
        }
        return false;
    }

    // ── Names and texts ──────────────────────────────────────────────────────

    /// <summary>
    /// What an element is called in a label: a person by role until introduced, a way by where it
    /// leads, anything else as a sentence names it — "the cart trail", never the title "Cart Trail".
    /// </summary>
    private string NameOf(NarrativeAnchor anchor) => anchor switch
    {
        SyntheticNpcObservationObject npc => NpcLabelResolver.ShortLabel(npc.SceneNpc.Entity),
        SyntheticAreaObservationObject area => $"the way to {NeutralNarration.AreaPhrase(area.Area.DisplayName)}",
        // A sleeper is named as anybody is: by role until introduced.
        SyntheticObservationObject { PointOfInterest: SleepingNpcPointOfInterest asleep } => asleep.Label(_active),
        SyntheticObservationObject { PointOfInterest: var poi } when IsOfMyself(poi) => poi.DisplayName.ToLowerInvariant(),
        ObservationObject obs => NeutralNarration.ThingPhrase(obs.NeutralName),
        _ => NeutralNarration.ThingPhrase(anchor.DisplayName),
    };

    /// <summary>
    /// "Myself", and what it holds in the two opening phases — a fragment of a memory, a facet of
    /// exhaustion: named as they are, not as things of a place ("myself", "aching legs", "a scream").
    /// </summary>
    private static bool IsOfMyself(Element element)
        => element is Cathedral.Game.Scene.GetUp.SelfPointOfInterest || Cathedral.Game.Scene.GetUp.SelfPointOfInterest.Holds(element);

    private string NameOf(ThoughtNode element) => element.Element switch
    {
        SceneNpc npc => NpcLabelResolver.ShortLabel(npc.Entity),
        SleepingNpcPointOfInterest asleep => asleep.Label(_active),
        // A way is named from the end it is now seen from.
        Area or ConnectorPointOfInterest when Locate(element) is { } at => NameOf(at.Anchor),
        Area area    => $"the way to {NeutralNarration.AreaPhrase(area.DisplayName)}",
        ItemElement item => NeutralNarration.ItemPhrase(item.Item.DisplayName),
        { } own when IsOfMyself(own) => own.DisplayName.ToLowerInvariant(),
        { } other    => NeutralNarration.ThingPhrase(other.DisplayName),
        null         => "",
    };

    /// <summary>What a symbol says of itself under the pointer.</summary>
    private LocationMap.HoverLabel? LabelFor(MapSymbol symbol)
    {
        bool enabled = !Busy && IsEnabled(symbol);
        return symbol switch
        {
            // What stands where no known way leads is told, and cannot be looked at from here.
            TraySymbol tray when Exploring && !_frameReach.ContainsKey(tray.Area.Id)
                                            => new($"{ElementKinds.Word(tray.Kind)} · no known way there", false),
            // The one symbol the childhood opens on is whoever is remembering.
            TraySymbol when MinimalScreen   => new("observe · myself", enabled),
            TraySymbol { Known: null } tray => new($"observe · {ElementKinds.Word(tray.Kind)}", enabled),
            TraySymbol { Known: { } known } => new($"observe {NameOf(known)}", enabled),
            // What "myself" holds is not named before it is looked at: a memory, a sensation —
            // whatever it hangs from. In the childhood that is the fragment remembered last, once
            // the first memory is behind.
            // Nothing a container holds is named before it is looked at, and which is looked at
            // is the mind's to choose.
            ContentSymbol content           => new(ContentLabel(content.Container), enabled),
            WaySymbol way                   => new(WayLabel(way), enabled),
            ExitSymbol                      => new(ExitLabel, enabled),
            TokenSymbol token when ReferenceEquals(token.Member, _active) => new(token.Member.DisplayName, enabled),
            TokenSymbol token when Exploring && _minds.TryGetValue(token.Member, out var theirs) && theirs.Noetic <= 0
                                            => new($"{token.Member.DisplayName} · no noetic point left", false),
            TokenSymbol token               => new($"play {token.Member.DisplayName}", enabled),
            StubSymbol { Kind: StubKind.Think } stub => new($"think about {NameOf(stub.Element)}", enabled),
            StubSymbol stub                 => new($"speak of {NameOf(stub.Element)} to {stub.Listener?.DisplayName}", enabled),
            CommandSymbol { Command: ActCommand.Tool } tool => new(ToolLabel(tool.Act), enabled),
            CommandSymbol                   => new("think it over again", enabled),
            // An observed element says what a click on it does to its tree.
            NodeSymbol { Node: { Kind: ThoughtNodeKind.Element } element } when CanFold(element)
                                            => new($"{NameOf(element)} · {(element.Folded ? "unfold" : "fold")}", enabled),
            NodeSymbol { Node: var node }   => new(LabelOf(node), enabled),
            _ => null,
        };
    }

    /// <summary>
    /// What a symbol of something <paramref name="container"/> holds says under the pointer. Nothing
    /// held is named before it is looked at: what "myself" holds is "a memory" or "a sensation"
    /// (<see cref="Scene.GetUp.SelfPointOfInterest.Holding"/>) whatever it hangs from, and what a
    /// chest or a body holds is something in it.
    /// </summary>
    private string ContentLabel(ThoughtNode container)
    {
        bool ofMyself = ContentsOf(container).Any(p => Cathedral.Game.Scene.GetUp.SelfPointOfInterest.Holds(ElementKinds.ElementOf(p)));
        string? holding = ofMyself
            ? _pov.Where.PointsOfInterest.OfType<Cathedral.Game.Scene.GetUp.SelfPointOfInterest>().FirstOrDefault()?.Holding
            : null;
        return holding != null ? $"observe · {holding}" : $"observe · something in {NameOf(container)}";
    }

    /// <summary>An unobserved way under the pointer: named once seen or gone by, a way and no more before.</summary>
    private string WayLabel(WaySymbol way)
    {
        if (!_frameWays.TryGetValue(way.Key, out var side)) return "a way";
        if (!_frameReach.ContainsKey(side.From.Id)) return "a way · no known way there";
        return ElementMemory.IsKnown(_scene, side.From, side.Anchor) ? $"observe {NameOf(side.Anchor)}" : "observe · an unknown way";
    }

    /// <summary>The implement command under the pointer, and when it cannot be used, why.</summary>
    private string ToolLabel(ThoughtNode act)
        => act.Action!.CombinedItem is { } held ? $"already with {held.WithArticle()}"
         : GetCombinableItems().Count == 0     ? "nothing carried to do it with"
         : "do it with an implement";

    /// <summary>A card a click would walk the played member to, by ways the party knows.</summary>
    private bool CanWalkTo(Area card)
        => card.Id != _pov.Where.Id && IsAreaKnown(card) && _frameConfrontation == null && _frameReach.ContainsKey(card.Id);

    private string LabelOf(ThoughtNode node) => node.Kind switch
    {
        ThoughtNodeKind.Element => NameOf(node),
        ThoughtNodeKind.Act     => node.State switch
        {
            ActState.Waiting    => node.Action!.DisplayText,
            ActState.Done       => $"done · {node.Action!.DisplayText}",
            ActState.Superseded => $"reconsidered · {node.Action!.DisplayText}",
            _                   => $"no longer possible · {node.Action!.DisplayText}",
        },
        ThoughtNodeKind.Thought => $"a thought · {node.Mind?.DisplayName}",
        ThoughtNodeKind.Refusal => $"a refusal · {node.Mind?.DisplayName}",
        ThoughtNodeKind.Success => "it worked",
        ThoughtNodeKind.Failure => "it failed",
        ThoughtNodeKind.Emotion => $"{node.Mind?.DisplayName}",
        ThoughtNodeKind.Speech  => $"said {node.Note}",
        _ => "",
    };

    /// <summary>
    /// The text a node is read from again, with the mind that wrote it and its level; null for a
    /// node that carries none. An act is read with what it would be rolled with (<c>Odds</c>): the
    /// dice it would throw, one mark a die, and the sixes it needs.
    /// </summary>
    private (string Title, string Text, string? Odds)? ReadingOf(ThoughtNode node)
    {
        if (node.Kind == ThoughtNodeKind.Act && node.Action is { } action)
        {
            int dice = Math.Max(1, action.GetTotalModusMentisLevel(_active));
            string marks = new string(Config.Symbols.ModusMentisLevelIndicator, dice);
            string odds = action.IsAutomatic ? $"{action.DifficultyGlyph} cannot fail"
                        : action.DifficultyLevel > 0 ? $"{marks} {dice} {(dice == 1 ? "die" : "dice")} · {action.DifficultyGlyph} {action.DifficultyLevel} {(action.DifficultyLevel == 1 ? "six" : "sixes")} needed"
                        : $"{marks} {dice} {(dice == 1 ? "die" : "dice")}";
            return (PreviewTitles.For(action.ChainModusMentis), action.DisplayText, odds);
        }
        if (node.Block is { } block)
            return (block.ModusMentis is { } mind ? PreviewTitles.For(mind) : "", block.Text, null);
        return node.Note is { } note ? (NameOf(node).ToUpper(), note, null) : null;
    }

    /// <summary>
    /// The label above the pointer for what it is over on the map, while nothing sits over it. Left
    /// alone while a modal popup holds the shared popup.
    /// </summary>
    private void UpdateMapLabel()
    {
        if (IsPopupVisible) { _mapLabelShown = false; return; }
        var label = Busy ? null
                  : _map.LabelAt(_lastMouseX, _lastMouseY, tag => LabelFor((MapSymbol)tag),
                                 card => CanWalkTo(card) ? new($"walk to {NeutralNarration.AreaPhrase(card.DisplayName)}", true) : null);
        if (label == null && !_mapLabelShown) return;
        LocationMap.ShowLabel(_popup, label);
        _mapLabelShown = label != null;
    }

    /// <summary>
    /// Takes down the label the map put up, when one is up: called while another screen has the
    /// panel, which draws no map and would leave the last label following the pointer.
    /// </summary>
    public void HideMapLabel()
    {
        if (!_mapLabelShown) return;
        LocationMap.ShowLabel(_popup, null);
        _mapLabelShown = false;
    }

    // ── The pointer ──────────────────────────────────────────────────────────

    /// <summary>Where a popup opens: at the pointer.</summary>
    private Vector2 PopupAnchor() => _terminalInputHandler.CellToScreen(_lastMouseX, _lastMouseY, _core.ClientSize);

    private float CellPixels => _terminalInputHandler.GetLayoutInfo(_core.ClientSize).CellSize.X;

    private static bool Over((int X, int Y, int Width) region, int x, int y)
        => region.Width > 0 && y == region.Y && x >= region.X && x < region.X + region.Width;

    /// <summary>
    /// The pointer moved, in screen pixels. Used while a popup is up, which is a terminal of its own.
    /// </summary>
    public void OnRawMouseMove(Vector2 screenPosition)
    {
        // UpdateHover returns true when the highlighted option changed — a tick then, as elsewhere.
        bool changed = false;
        if (_modusMentisPopup.IsVisible)   changed |= _modusMentisPopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, CellPixels);
        if (_itemSelectionPopup.IsVisible) changed |= _itemSelectionPopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, CellPixels);
        if (changed) PlayHoverSound();
    }

    /// <summary>The pointer moved, in terminal cells.</summary>
    public void OnMouseMove(int mouseX, int mouseY)
    {
        _lastMouseX = mouseX;
        _lastMouseY = mouseY;
        if (IsPopupVisible) return;

        if (_dice.IsVisible)
        {
            if (_dice.IsRolling) return;
            Hover(ref _diceButtonHovered, Over(_dice.ContinueButtonRegion, mouseX, mouseY));
            _dice.HandleHumorHover(mouseX, mouseY);
            return;
        }
        if (_timeBox.IsVisible)
        {
            if (_timeBox.HandleHover(mouseX, mouseY)) PlayHoverSound();
            return;
        }

        if (_previewAnswerRegions.Yes.Width > 0)
        {
            bool? over = Over(_previewAnswerRegions.Yes, mouseX, mouseY) ? true
                       : Over(_previewAnswerRegions.No, mouseX, mouseY) ? false : null;
            if (over != _previewAnswerHovered)
            {
                _previewAnswerHovered = over;
                if (over != null) PlayHoverSound();
            }
        }
        Hover(ref _previewContinueHovered, Over(_previewContinueRegion, mouseX, mouseY));
        Hover(ref _resultContinueHovered, Over(_resultContinueRegion, mouseX, mouseY));
        if (Busy) return;

        // The out-of-points box is modal: its buttons, and nothing of the map under it.
        if (SpentBoxUp)
        {
            int over = _spentRegions.FindIndex(r => Over(r, mouseX, mouseY));
            if (over != _spentHovered)
            {
                _spentHovered = over;
                if (over >= 0) PlayHoverSound();
            }
            _hovered = null;
            return;
        }

        if (_map.SetHover(mouseX, mouseY)) PlayHoverSound();
        var under = _map.IconAt(mouseX, mouseY) as MapSymbol;
        if (!Equals(under, _hovered))
        {
            _hovered = under;
            // The tick is for what answers the pointer: a click, or a text to read.
            if (under != null && (under is NodeSymbol || IsEnabled(under))) PlayHoverSound();
        }
    }

    private void Hover(ref bool state, bool over)
    {
        if (over == state) return;
        state = over;
        if (over) PlayHoverSound();
    }

    /// <summary>A click, in screen pixels: a popup's, which takes every click while it is up.</summary>
    public void OnRawMouseClick(Vector2 screenPosition)
    {
        PlayClickSound();
        ClickPopup(screenPosition);
    }

    /// <summary>A click, in terminal cells.</summary>
    public void OnMouseClick(int mouseX, int mouseY)
    {
        if (_error != null) return;

        if (_dice.IsVisible)
        {
            if (_dice.IsRolling) return;
            if (Over(_dice.ContinueButtonRegion, mouseX, mouseY))
            {
                PlayClickSound();
                // A runaway's roll resolves the way out; any other, the act rolled.
                if (_runawayFrom != null) FinishRunaway();
                else OnDiceRollContinue();
            }
            else _dice.HandleHumorClick(mouseX, mouseY);
            return;
        }

        // The time box is modal: its period buttons while it asks, its Continue once the hours have passed.
        if (_timeBox.IsVisible)
        {
            if (_timeBox.IsOnContinue(mouseX, mouseY)) OnTimePassageContinue();
            else _timeBox.HandlePeriodClick(mouseX, mouseY);
            return;
        }

        // A click while somebody walks brings them there at once.
        if (_walk != null)
        {
            _walk.StepMs = 0;
            return;
        }

        // The doubt of a thinking: the answer goes to the waiting thinking, and the part commits.
        if (_previewAnswerRegions.Yes.Width > 0)
        {
            bool? answer = Over(_previewAnswerRegions.Yes, mouseX, mouseY) ? true
                         : Over(_previewAnswerRegions.No, mouseX, mouseY) ? false : null;
            if (answer is { } yes)
            {
                PlayClickSound();
                _previewAnswerHovered = null;
                _previewSession.TryAnswer(yes);
                return;
            }
        }
        if (Over(_previewContinueRegion, mouseX, mouseY))
        {
            PlayClickSound();
            _previewContinueHovered = false;
            _previewSession.TryContinue();
            return;
        }
        if (Over(_resultContinueRegion, mouseX, mouseY))
        {
            _resultContinueHovered = false;
            OnResultContinue();
            return;
        }
        if (Busy) return;

        if (IsPopupVisible)
        {
            ClickPopup(_terminalInputHandler.GetCorrectedMousePosition());
            return;
        }

        // The out-of-points box takes every click: one of its buttons, or nothing.
        if (SpentBoxUp)
        {
            int button = _spentRegions.FindIndex(r => Over(r, mouseX, mouseY));
            if (button >= 0) ClickSpent(button);
            return;
        }

        // The map's pan pad sits in the panel's top-right corner.
        if (_map.HandleClick(mouseX, mouseY))
        {
            PlayClickSound();
            return;
        }

        if (_map.IconAt(mouseX, mouseY) is MapSymbol symbol)
        {
            ClickSymbol(symbol);
            return;
        }

        // A card: walk there, where a known way leads and nobody holds the member where they stand.
        if (_map.AreaAt(mouseX, mouseY) is { } card && CanWalkTo(card))
        {
            PlayClickSound();
            WalkTo(card, then: () => { });
        }
    }

    /// <summary>
    /// A symbol was clicked: what kind of thing to observe, which element to think about or speak of
    /// and to whom, which act to carry out or to do something else with, whom to play. What is done
    /// is said by the symbol; a popup only ever asks by which mind, or with which thing carried.
    /// </summary>
    private void ClickSymbol(MapSymbol symbol)
    {
        if (!IsEnabled(symbol)) return;
        PlayClickSound();
        _hovered = symbol;

        switch (symbol)
        {
            case TokenSymbol token:
                Switch(token.Member);
                break;

            case ExitSymbol:
                OnExitClicked();
                break;

            case TraySymbol tray:
                AskMind(new ObservePending(tray.Area, tray.Kind, tray.Known, Container: null), ObservingMinds, "Observe with");
                break;

            // A way nothing is known of, with others like it between the same two areas: they are
            // one line and the mind chooses which of them it looks at. A way alone on its line, or
            // one already seen or gone by, is that way and no other.
            case WaySymbol way when _frameWays.TryGetValue(way.Key, out var side):
                var alike = SiblingWays(way.Key);
                AskMind(alike.Count > 1
                            ? new ObservePending(side.From, ElementKind.Way, Named: null, Container: null, Way: way.Key,
                                                 Among: alike, Pair: PairKey(way.Key))
                            : new ObservePending(side.From, ElementKind.Way, side.Anchor, Container: null, Way: way.Key),
                        ObservingMinds, "Observe with");
                break;

            // Something a container holds: which, the mind chooses.
            case ContentSymbol content when Locate(content.Container) is { } held:
                AskMind(new ObservePending(held.Where, ElementKind.Thing, Named: null, content.Container),
                        ObservingMinds, "Observe with");
                break;

            case StubSymbol { Kind: StubKind.Think } stub:
                AskMind(new ThinkPending(stub.Element), ThinkingMinds, "Think with");
                break;

            case StubSymbol { Listener: { } listener } stub:
                AskMind(new SpeakPending(stub.Element, listener), _active.GetSpeakingModiMentis(), "Speak with");
                break;

            // An observed element: its tree is folded away, or brought back.
            case NodeSymbol { Node: { Kind: ThoughtNodeKind.Element } element }:
                element.Folded = !element.Folded;
                break;

            // The act itself: carried out. Acting needs presence, so the member goes to where it
            // is to be done.
            case NodeSymbol { Node: { IsWaitingAct: true } act }:
                if (LocateAct(act) is { } target)
                    WalkTo(target.Where, then: () => _ = ActAsync(act));
                break;

            case CommandSymbol { Command: ActCommand.Tool } tool:
                _pending = new ToolPending(tool.Act);
                _itemSelectionPopup.Show(PopupAnchor(), GetCombinableItems(), "Use with this action");
                break;

            case CommandSymbol { Command: ActCommand.Reconsider } reconsider:
                AskMind(new ReconsiderPending(reconsider.Act), ThinkingMinds, "Reconsider with");
                break;
        }
    }

    /// <summary>
    /// Opens the popup that asks which mind does what was clicked. A mind that has already refused
    /// this very thing is listed greyed and cannot be picked: asked again it would refuse again, and
    /// the point would be spent to hear it twice.
    /// </summary>
    private void AskMind(Pending pending, List<ModusMentis> minds, string title)
    {
        _pending = pending;
        var (office, about) = AskedOf(pending);
        _modusMentisPopup.Show(PopupAnchor(), minds, title, minds.Where(m => Mind.HasRefused(m, office, about)).ToHashSet());
    }

    /// <summary>A click while a popup is up: its choice, or — outside it — its dismissal.</summary>
    private void ClickPopup(Vector2 screen)
    {
        if (_itemSelectionPopup.IsVisible)
        {
            var item = _itemSelectionPopup.HandleClick(screen.X, screen.Y, _core.ClientSize, CellPixels);
            var pending = _pending as ToolPending;
            _pending = null;
            if (item != null && pending != null) _ = CombineItemAsync(pending.Act, item);
        }
        else if (_modusMentisPopup.IsVisible)
        {
            var mind = _modusMentisPopup.HandleClick(screen.X, screen.Y, _core.ClientSize, CellPixels);
            if (mind != null) ApplyModusMentisSelection(mind);
            // A click on a mind that refused already picks nothing and leaves the popup up.
            else if (!_modusMentisPopup.IsVisible) _pending = null;
        }
    }

    /// <summary>The mouse wheel scrolls a popup that overflows its box.</summary>
    public void OnMouseWheel(float delta)
    {
        if (_modusMentisPopup.IsVisible) _modusMentisPopup.Scroll(delta > 0 ? -3 : 3);
        else if (_itemSelectionPopup.IsVisible) _itemSelectionPopup.Scroll(delta > 0 ? -3 : 3);
    }

    /// <summary>An arrow key pans the map. False for any other key.</summary>
    public bool PanMap(OpenTK.Windowing.GraphicsLibraryFramework.Keys key) => !IsPopupVisible && _map.HandleKey(key);

    /// <summary>Closes the popup on screen, if any. Returns whether one was.</summary>
    public bool ClosePopup()
    {
        if (!IsPopupVisible) return false;
        _modusMentisPopup.Hide();
        _itemSelectionPopup.Hide();
        _pending = null;
        return true;
    }
}
