using System;
using System.Collections.Generic;
using System.Linq;
using Cathedral.Audio;
using Cathedral.Game.Narrative;
using Cathedral.Game.Narrative.Tree;
using Cathedral.Game.Npc;
using Cathedral.Game.Scene;
using Cathedral.Game.Scene.Building;
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
    private bool _diceButtonHovered;

    // Read once a frame, for every symbol's state.
    private Confrontation? _frameConfrontation;
    private HashSet<Guid> _frameReach = new();

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
        RenderPanel();
        // A popup is a modal choice, so the panel behind it is greyed the way the preview box greys
        // it: recolouring what is drawn rather than darkening the whole HUD, so the popup itself (a
        // separate terminal) stays at full strength.
        if (IsPopupVisible) _ui.DimContent();
        UpdateMapLabel();
    }

    private void RenderPanel()
    {
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
        _overlay.Frame = BuildFrame();

        // The map is always what is drawn; everything else is a box over it.
        if (_walk is { } walk)
        {
            var step = walk.Steps[walk.Shown];
            _ui.RenderMap(_map, _scene, step.Here, Period, _overlay, (step.X, step.Y), walk.Glyph, WalkWithheld());
        }
        else
            _ui.RenderMap(_map, _scene, _pov.Where, Period, _overlay);

        _previewContinueRegion = default;
        _previewAnswerRegions  = default;
        _resultContinueRegion  = default;

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
            _resultContinueRegion = _ui.RenderResultBox(_result.Title, _result.Succeeded, _result.Chips, _resultContinueHovered);
            _ui.RenderStatusBar("Click Continue");
            return;
        }

        // Idle: what the pointer is over is read again.
        if (!IsPopupVisible && _hovered is NodeSymbol { Node: var node } && ReadingOf(node) is { } reading)
            _ui.RenderReadingBox(reading.Title, reading.Text, node.Block?.OutcomeReports);
        _ui.RenderStatusBar(SceneInfoLine());
    }

    /// <summary>The footer while nothing is happening: "Biome — area | hour".</summary>
    private string SceneInfoLine()
    {
        string biome = _worldContext.DisplayName;
        string where = $"{_pov.Where.DisplayName}  |  {Period}";
        return biome.Length > 0 ? $"{biome}  —  {where}" : where;
    }

    // ── What the map shows ───────────────────────────────────────────────────

    /// <summary>Reads off the game everything the overlay draws this frame.</summary>
    private MapFrame BuildFrame()
    {
        _frameConfrontation = ConfrontationOf(Mind);
        _frameReach = Reach(Mind).Keys.Select(a => a.Id).ToHashSet();
        _frameReach.Add(_pov.Where.Id);

        var frame = new MapFrame
        {
            Active       = _active,
            Hovered      = Busy || IsPopupVisible ? null : _hovered,
            ElementGlyph = node => ElementKinds.Glyph(KindOf(node)),
            IsHostile    = node => KindOf(node) == ElementKind.Hostile,
            Contents     = ContentsOf,
            Stubs        = StubsOf,
            Enabled      = symbol => !Busy && IsEnabled(symbol),
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

        // What is not yet observed, in every area the party knows.
        foreach (var area in _scene.AllAreas.Where(IsAreaKnown))
        {
            var tray = Card(area).Tray;
            foreach (var (anchor, kind, known) in Unobserved(area))
                tray.Add(new TraySymbol(area, kind, known ? anchor : null, tray.Count));
        }

        // The played member's trees, each under the card of where its element now is.
        foreach (var root in Mind.Roots)
            if (Locate(root) is { } at && IsAreaKnown(at.Where))
                Card(at.Where).Roots.Add(root);
        foreach (var (area, refusal) in Mind.Strays)
            Card(area).Strays.Add(refusal);

        return frame;
    }

    /// <summary>The kind an observed element is of, as the played member sees it now. A thing out of sight is a thing.</summary>
    private ElementKind KindOf(ThoughtNode element) => element.Element switch
    {
        SceneNpc npc => ElementKinds.IsHostile(npc, _active) ? ElementKind.Hostile : ElementKind.Person,
        Area or ConnectorPointOfInterest => ElementKind.Way,
        _ => ElementKind.Thing,
    };

    /// <summary>What an observed element offers to do next: to think about it, and to speak of it where anybody could be told.</summary>
    private IReadOnlyList<StubKind> StubsOf(ThoughtNode element)
    {
        var stubs = new List<StubKind>();
        if (_active.GetThinkingModiMentis().Count > 0) stubs.Add(StubKind.Think);
        if (Exploring && _minds.Count > 1 && _active.GetSpeakingModiMentis().Count > 0) stubs.Add(StubKind.Speak);
        return stubs;
    }

    /// <summary>
    /// Whether a click on <paramref name="symbol"/> would act now.
    ///
    /// <para>Observing and thinking cost a point and are withheld without one; observing and acting
    /// need the member there, and are withheld where no known way leads. Confronted, a member may
    /// look only at whoever confronts them and think only about them — though an act already
    /// proposed stays theirs to try where they stand: the coded rules judge it on the click, and a
    /// discreet means may pass where another is refused.</para>
    /// </summary>
    private bool IsEnabled(MapSymbol symbol)
    {
        var confrontation = _frameConfrontation;
        switch (symbol)
        {
            case TokenSymbol token:
                return !ReferenceEquals(token.Member, _active);

            case TraySymbol tray:
                if (!HasPoint || _active.GetObservationModiMentis().Count == 0) return false;
                if (confrontation == null) return _frameReach.Contains(tray.Area.Id);
                return tray.Area.Id == _pov.Where.Id
                    && (tray.Known != null
                        ? IsConfronter(tray.Known, confrontation)
                        : Unobserved(tray.Area).Any(u => u.Kind == tray.Kind && !u.Known && IsConfronter(u.Anchor, confrontation)));

            case ContentSymbol content:
                return HasPoint && confrontation == null && _active.GetObservationModiMentis().Count > 0
                    && Locate(content.Container) is { } held && _frameReach.Contains(held.Where.Id);

            case StubSymbol { Kind: StubKind.Think } think:
                return HasPoint && Locate(think.Element) is { } thought
                    && (confrontation == null || IsConfronter(thought.Anchor, confrontation));

            case StubSymbol { Kind: StubKind.Speak } speak:
                return Mind.Noetic > 0 && confrontation == null && Locate(speak.Element) != null
                    && WithinSpeech().Count > 0;

            case NodeSymbol { Node: { IsWaitingAct: true } act }:
                return Locate(act.ElementNode) is { } target
                    && (target.Where.Id == _pov.Where.Id || (confrontation == null && _frameReach.Contains(target.Where.Id)));
        }
        return false;
    }

    // ── Names and texts ──────────────────────────────────────────────────────

    /// <summary>What an element is called on the map: a person by role until introduced, a way by where it leads.</summary>
    private string NameOf(NarrativeAnchor anchor) => anchor switch
    {
        SyntheticNpcObservationObject npc => NpcLabelResolver.ShortLabel(npc.SceneNpc.Entity),
        SyntheticAreaObservationObject area => $"the way to {area.Area.DisplayName}",
        ObservationObject obs => obs.NeutralName,
        _ => anchor.DisplayName,
    };

    private string NameOf(ThoughtNode element) => element.Element switch
    {
        SceneNpc npc => NpcLabelResolver.ShortLabel(npc.Entity),
        Area area    => $"the way to {area.DisplayName}",
        ItemElement item => item.Item.DisplayName,
        { } other    => other.DisplayName,
        null         => "",
    };

    /// <summary>What a symbol says of itself under the pointer.</summary>
    private LocationMap.HoverLabel? LabelFor(MapSymbol symbol)
    {
        bool enabled = !Busy && IsEnabled(symbol);
        return symbol switch
        {
            TraySymbol { Known: null } tray => new($"observe · {ElementKinds.Word(tray.Kind)}", enabled),
            TraySymbol { Known: { } known } => new($"observe {NameOf(known)}", enabled),
            ContentSymbol content           => new($"observe {NameOf(content.Part)}", enabled),
            TokenSymbol token               => new(ReferenceEquals(token.Member, _active) ? token.Member.DisplayName : $"play {token.Member.DisplayName}", enabled),
            StubSymbol { Kind: StubKind.Think } stub => new($"think about {NameOf(stub.Element)}", enabled),
            StubSymbol stub                 => new($"speak of {NameOf(stub.Element)}", enabled),
            NodeSymbol { Node: var node }   => new(LabelOf(node), enabled),
            _ => null,
        };
    }

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

    /// <summary>The text a node is read from again, with the mind that wrote it; null for a node that carries none.</summary>
    private (string Title, string Text)? ReadingOf(ThoughtNode node)
    {
        if (node.Kind == ThoughtNodeKind.Act && node.Action is { } action)
        {
            int dice = Math.Max(1, action.GetTotalModusMentisLevel(_active));
            string odds = action.IsAutomatic ? "cannot fail"
                        : action.DifficultyLevel > 0 ? $"{dice} {(dice == 1 ? "die" : "dice")}, {action.DifficultyLevel} {(action.DifficultyLevel == 1 ? "six" : "sixes")} needed"
                        : $"{dice} {(dice == 1 ? "die" : "dice")}";
            return (action.ChainModusMentis.DisplayName.ToUpper(), $"{action.DisplayText}\n{odds}");
        }
        if (node.Block is { } block)
            return (block.ModusMentis?.DisplayName.ToUpper() ?? "", block.Text);
        return node.Note is { } note ? (NameOf(node).ToUpper(), note) : null;
    }

    /// <summary>
    /// The label above the pointer for what it is over on the map, while nothing sits over it. Left
    /// alone while a modal popup holds the shared popup.
    /// </summary>
    private void UpdateMapLabel()
    {
        if (IsPopupVisible) { _mapLabelShown = false; return; }
        var label = Busy ? null
                  : _map.LabelAt(_lastMouseX, _lastMouseY, tag => LabelFor((MapSymbol)tag), Exploring ? ExitLabel : null);
        if (label == null && !_mapLabelShown) return;
        LocationMap.ShowLabel(_popup, label);
        _mapLabelShown = label != null;
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
        if (_choicePopup.IsVisible)        changed |= _choicePopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.ClientSize, CellPixels);
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

        if (_map.SetHover(mouseX, mouseY)) PlayHoverSound();
        var under = _map.IconAt(mouseX, mouseY) as MapSymbol;
        if (!Equals(under, _hovered))
        {
            _hovered = under;
            if (under != null) PlayHoverSound();
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

        // The map's pan pad sits in the panel's top-right corner.
        if (_map.HandleClick(mouseX, mouseY))
        {
            PlayClickSound();
            return;
        }

        if (Exploring && _map.IsExitAt(mouseX, mouseY))
        {
            PlayClickSound();
            OnExitClicked();
            return;
        }

        if (_map.IconAt(mouseX, mouseY) is MapSymbol symbol)
        {
            ClickSymbol(symbol);
            return;
        }

        // A card: walk there, where a known way leads and nobody holds the member where they stand.
        if (_map.AreaAt(mouseX, mouseY) is { } card && card.Id != _pov.Where.Id && IsAreaKnown(card)
            && _frameConfrontation == null && _frameReach.Contains(card.Id))
        {
            PlayClickSound();
            WalkTo(card, then: () => { });
        }
    }

    /// <summary>
    /// A symbol was clicked: what kind of thing to observe, which element to think about or speak of,
    /// which act to carry out, whom to play. The mind that does it is asked for next.
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

            case TraySymbol tray:
                _pending = new ObservePending(tray.Area, tray.Kind, tray.Known, Container: null);
                _modusMentisPopup.Show(PopupAnchor(), _active.GetObservationModiMentis(), "Observe with");
                break;

            case ContentSymbol content when Locate(content.Container) is { } held:
                _pending = new ObservePending(held.Where, ElementKind.Thing, content.Part, content.Container);
                _modusMentisPopup.Show(PopupAnchor(), _active.GetObservationModiMentis(), "Observe with");
                break;

            case StubSymbol { Kind: StubKind.Think } stub:
                _pending = new ThinkPending(stub.Element);
                _modusMentisPopup.Show(PopupAnchor(), _active.GetThinkingModiMentis(), "Think with");
                break;

            case StubSymbol stub:
                _pending = new SpeakPending(stub.Element, Mind: null, WithinSpeech());
                _modusMentisPopup.Show(PopupAnchor(), _active.GetSpeakingModiMentis(), "Speak with");
                break;

            case NodeSymbol { Node: { IsWaitingAct: true } act }:
                // Reaching for a tool is part of the act already proposed, not a second thought: the
                // point was spent when the act was thought of, and only one item may ever be combined
                // with it. A refusal for want of a tool leaves Use Tool as the only way on; and
                // reconsidering is a thinking, which costs its point.
                var disabled = new HashSet<int>();
                if (act.Action!.NeedsTool) disabled.Add(0);
                if (act.Action.CombinedItem != null || GetCombinableItems().Count == 0) disabled.Add(1);
                if (!CanReconsider()) disabled.Add(2);
                _pending = new ActPending(act);
                _choicePopup.Show(PopupAnchor(), new List<string> { "Execute", "Use Tool", "Reconsider" }, "Action", disabled);
                break;
        }
    }

    /// <summary>A click while a popup is up: its choice, or — outside it — its dismissal.</summary>
    private void ClickPopup(Vector2 screen)
    {
        if (_choicePopup.IsVisible)
        {
            DispatchChoice(_choicePopup.HandleClick(screen.X, screen.Y, _core.ClientSize, CellPixels));
        }
        else if (_itemSelectionPopup.IsVisible)
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
            else _pending = null;
        }
    }

    /// <summary>The choice popup's answer: what to do with an act, or whom to speak to.</summary>
    private void DispatchChoice(int? index)
    {
        var pending = _pending;
        _pending = null;

        switch (pending)
        {
            case SpeakPending { Mind: { } speakingMind } speak when index is { } i && i >= 0 && i < speak.Listeners.Count:
                _loading = Config.LoadingMessages.Speaking;
                _ = SpeakAsync(speak.Element, speakingMind, speak.Listeners[i]);
                break;

            case ActPending { Act: var act } when index == 0:
                // Acting needs presence: the member goes to where the act is to be done.
                if (Locate(act.ElementNode) is { } target)
                    WalkTo(target.Where, then: () => _ = ActAsync(act));
                break;

            case ActPending { Act: var act } when index == 1:
                var items = GetCombinableItems();
                if (items.Count == 0) break;
                _pending = new ToolPending(act);
                _itemSelectionPopup.Show(PopupAnchor(), items, "Use with this action");
                break;

            case ActPending { Act: var act } when index == 2 && CanReconsider():
                _pending = new ReconsiderPending(act);
                _modusMentisPopup.Show(PopupAnchor(), _active.GetThinkingModiMentis(), "Reconsider with");
                break;
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
        _choicePopup.Hide();
        _modusMentisPopup.Hide();
        _itemSelectionPopup.Hide();
        _pending = null;
        return true;
    }
}
