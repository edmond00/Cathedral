using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using Cathedral.Game;
using Cathedral.Game.Narrative;
using Cathedral.Terminal;

namespace Cathedral.Fight;

/// <summary>
/// OpenTK GameWindow that runs the full turn-based fight loop.
/// Manage state via <see cref="FightState"/> and delegate rendering to <see cref="FightModeUI"/>.
/// </summary>
internal class FightModeWindow : GameWindow
{
    // ── Core objects ─────────────────────────────────────────────────
    private TerminalHUD? _terminal;
    private PopupTerminalHUD? _popup;
    private readonly FightState _state;
    private readonly FightingSkillRegistry _skillRegistry;
    private readonly DiceRollComponent _dice = new();
    private readonly Random _rng = new();

    // ── Action mode (what happens when the player clicks the center panel) ─
    private bool _isMoveMode = true;   // true = MOVE selected; false = skill selected
    private int  _selectedSkillIndex = -1;
    private string? _selectedMediumKey;
    private HashSet<(int X, int Y)>? _highlightCells;
    private bool _isAttackHighlight;   // red vs green tint on highlighted tiles
    private HashSet<(int X, int Y)>? _hoverSkillCells; // hover-preview blink on map

    // ── UI state ──────────────────────────────────────────────────────
    private int _actionLogScrollOffset;
    private int _actionMenuScrollOffset;          // vertical scroll of the top-left action menu
    private int _actionMenuMaxScroll;             // max scroll for the action menu (set each redraw)
    private bool _draggingMenuScrollbar;          // true while the user drags the action-menu scrollbar
    private int _hoverX = -1, _hoverY = -1;       // last hovered terminal cell (for wheel routing)
    private IReadOnlyList<FightingSkill> _currentUnlockedSkills    = Array.Empty<FightingSkill>();
    private IReadOnlyList<FightingSkill> _currentUnaffordableSkills = Array.Empty<FightingSkill>();
    private string? _expandedMediumKey;
    private IReadOnlyList<LeftPanelRow> _leftPanelLayout = Array.Empty<LeftPanelRow>();
    private IReadOnlyList<(int Y, Fighter Fighter)> _rightPanelRows = Array.Empty<(int, Fighter)>();
    private Fighter? _topPanelFighter;
    private FightLocalizationOverlay? _localizationOverlay;
    private bool _continueHovered;
    private Fighter? _hoveredFighter;
    private int  _hoveredButtonRow = -1;
    private List<(int X, int Y)>? _previewPath;

    // ── Blink ─────────────────────────────────────────────────────────
    private double _blinkTimer;
    private bool _blinkOn = true;

    // ── AI delay ─────────────────────────────────────────────────────
    private int _aiDelayFrames;
    private const int AiDelay = 15;

    // ── Movement animation ────────────────────────────────────────────
    private int _movementFrameTimer;
    private const int PlayerMoveFramesPerTile = 3;
    private const int AiMoveFramesPerTile     = 1;

    // ── Dice timing ──────────────────────────────────────────────────
    private const float DiceRollDuration = 0.6f;
    private double _diceElapsed;

    public FightModeWindow(GameWindowSettings gs, NativeWindowSettings ns, FightState state)
        : base(gs, ns)
    {
        _state      = state;
        _skillRegistry = FightingSkillRegistry.Instance;
    }

    // ── Lifecycle ─────────────────────────────────────────────────────

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0f, 0f, 0f, 1f);
        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _terminal = new TerminalHUD(
            Config.Terminal.MainWidth,
            Config.Terminal.MainHeight,
            Config.Terminal.MainCellSize,
            Config.Terminal.MainFontSize);

        _popup = new PopupTerminalHUD(
            28, 16,
            Config.Terminal.MainCellSize,
            _terminal.Atlas,
            Config.Terminal.MainWidth,
            Config.Terminal.MainHeight);

        _terminal.CellClicked += OnCellClicked;
        _terminal.CellHovered += OnCellHovered;

        // Render arena terrain once at load
        FightAreaRenderer.Render(_terminal, _state.Area, "fight", 0);

        // Start first turn
        var first = _state.ActiveFighter;
        if (first != null)
        {
            first.StartTurn();
            if (!first.IsPlayerControlled)
                _aiDelayFrames = AiDelay;
        }

        RefreshSkillList();
        FullRedraw();
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (_terminal == null) return;

        // ── Fight ended ───────────────────────────────────────────
        if (_state.IsOver)
        {
            if (KeyboardState.IsKeyDown(Keys.Enter) || KeyboardState.IsKeyDown(Keys.Escape))
            {
                Close();
                return;
            }
            FullRedraw();
            return;
        }

        // ── Movement animation ────────────────────────────────────
        if (_state.Phase == TurnPhase.AnimatingMovement &&
            _state.MovementPath != null && _state.MovingFighter != null)
        {
            _movementFrameTimer++;
            int framesPerTile = _state.MovingFighter.IsPlayerControlled
                ? PlayerMoveFramesPerTile : AiMoveFramesPerTile;
            if (_movementFrameTimer >= framesPerTile)
            {
                _movementFrameTimer = 0;
                if (_state.MovementPathIndex < _state.MovementPath.Count)
                {
                    // Restore terrain at old position before moving
                    int prevX = _state.MovingFighter.X;
                    int prevY = _state.MovingFighter.Y;
                    var (nx, ny) = _state.MovementPath[_state.MovementPathIndex++];
                    _state.MovingFighter.X = nx;
                    _state.MovingFighter.Y = ny;
                    if (_terminal != null)
                    {
                        var terrCell = _state.Area.GetCell(prevX, prevY);
                        _terminal.SetCell(20 + prevX, 20 + prevY,
                            terrCell.Glyph, terrCell.TextColor, terrCell.BgColor);
                    }
                }
                else
                {
                    // Animation complete
                    var mover = _state.MovingFighter;
                    _state.MovementPath = null;
                    _state.MovingFighter = null;
                    _state.MovementPathIndex = 0;
                    _state.Phase = TurnPhase.SelectingAction;
                    RefreshSkillList();
                    RecomputeHighlight();
                    if (!mover.IsPlayerControlled)
                        _aiDelayFrames = 5; // brief pause then AI continues
                }
            }
            FullRedraw();
            return;
        }

        // ── Keyboard shortcuts ────────────────────────────────────
        HandleKeyboard();

        // ── Blink ─────────────────────────────────────────────────
        // Skip the in-arena exit blink while the localization overlay is up so the
        // SetCell doesn't punch through the body art.
        _blinkTimer += args.Time;
        bool newBlink = (_blinkTimer % 0.06) < 0.03;
        if (newBlink != _blinkOn)
        {
            _blinkOn = newBlink;
            if (_state.Phase != TurnPhase.WaitingForBodyPartChoice)
                FightAreaRenderer.UpdateBlink(_terminal, _blinkOn);
        }

        // ── Dice animation ────────────────────────────────────────
        if (_state.Phase == TurnPhase.AnimatingDice && _dice.IsRolling)
        {
            _dice.Advance();
            _diceElapsed += args.Time;
            if (_diceElapsed >= DiceRollDuration)
            {
                var finalValues = GenerateDiceValues(_state.DiceNumberOfDice);
                _state.DiceFinalValues = finalValues;
                if (_dice.IsDual)
                {
                    var defenseValues = GenerateDiceValues(_state.DiceSecondaryNumberOfDice);
                    _state.DiceSecondaryFinalValues = defenseValues;
                    bool isEnemyAttack = _state.ActiveFighter?.IsPlayerControlled == false
                                     && _state.PendingSkill?.EffectType == FightingSkillEffect.Attack;
                    if (isEnemyAttack)
                        _dice.CompleteDual(defenseValues, finalValues);
                    else
                        _dice.CompleteDual(finalValues, defenseValues);
                }
                else
                {
                    _dice.Complete(finalValues);
                }
                _state.IsDiceRolling   = false;
                _state.Phase           = TurnPhase.WaitingForDiceComplete;
            }
        }

        // ── AI turn ────────────────────────────────────────────────
        if (_state.Phase == TurnPhase.SelectingAction &&
            _state.ActiveFighter is { IsPlayerControlled: false })
        {
            _aiDelayFrames--;
            if (_aiDelayFrames <= 0)
                ExecuteAiTurn();
        }

        FullRedraw();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _terminal?.Render(new Vector2i(ClientSize.X, ClientSize.Y));
        SwapBuffers();
    }

    protected override void OnMouseMove(MouseMoveEventArgs e)
    {
        base.OnMouseMove(e);
        _terminal?.HandleMouseMove(MousePosition, ClientSize);
        _popup?.SetMousePosition(MousePosition);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        _terminal?.HandleMouseDown(MousePosition, ClientSize, e.Button);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        _terminal?.HandleMouseUp(e.Button);
        if (e.Button == MouseButton.Left) _draggingMenuScrollbar = false;
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        // Over the top-left action menu during action selection → scroll the menu; else the log.
        bool overActionMenu = _hoverX >= 0 && _hoverX < FightModeUI.ActionMenuRight
                           && _hoverY >= 0 && _hoverY < 20
                           && _state.Phase == TurnPhase.SelectingAction
                           && _state.ActiveFighter?.IsPlayerControlled == true;
        if (overActionMenu)
            _actionMenuScrollOffset = Math.Clamp(_actionMenuScrollOffset - (int)e.OffsetY, 0, _actionMenuMaxScroll);
        else
            _actionLogScrollOffset = Math.Max(0, _actionLogScrollOffset - (int)e.OffsetY);
    }

    // ── Action-menu scrollbar geometry / drag helpers ─────────────────
    private static int ActionScrollbarX => FightModeUI.ActionMenuRight - 2;
    private static int ActionScrollbarTop => FightModeUI.SkillButtonsStart;
    private static int ActionScrollbarRows => (FightModeUI.EndTurnButtonRow - 1) - FightModeUI.SkillButtonsStart;

    private bool IsOnActionScrollbar(int x, int y) =>
        _actionMenuMaxScroll > 0
        && x == ActionScrollbarX
        && y >= ActionScrollbarTop
        && y < ActionScrollbarTop + ActionScrollbarRows;

    private void SetMenuScrollFromRow(int y)
    {
        if (_actionMenuMaxScroll <= 0) return;
        int rel = Math.Clamp(y - ActionScrollbarTop, 0, ActionScrollbarRows - 1);
        double frac = ActionScrollbarRows > 1 ? (double)rel / (ActionScrollbarRows - 1) : 0;
        _actionMenuScrollOffset = Math.Clamp((int)Math.Round(frac * _actionMenuMaxScroll), 0, _actionMenuMaxScroll);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
        _terminal?.ForceRefresh();
    }

    protected override void OnUnload()
    {
        _terminal?.Dispose();
        _popup?.Dispose();
        base.OnUnload();
    }

    // ── Input handlers ────────────────────────────────────────────────

    private void OnCellHovered(int x, int y)
    {
        _hoverX = x;
        _hoverY = y;

        // ── Dragging the action-menu scrollbar: follow the cursor's row ──
        if (_draggingMenuScrollbar)
        {
            SetMenuScrollFromRow(y);
            _hoveredButtonRow = -1; // suppress row highlight while dragging
            return;
        }

        // ── Localization picker overlay owns the entire cursor area ──
        if (_state.Phase == TurnPhase.WaitingForBodyPartChoice && _localizationOverlay != null)
        {
            _localizationOverlay.OnMouseMove(x, y);
            return;
        }

        // ── Dice continue button + humor modifier layer ──────────────
        if (_state.Phase == TurnPhase.WaitingForDiceComplete)
        {
            var region = _dice.ContinueButtonRegion;
            _continueHovered = (y == region.Y && x >= region.X && x < region.X + region.Width);
            _dice.HandleHumorHover(x, y);
        }

        bool canInteract = _state.Phase == TurnPhase.SelectingAction
                        && _state.ActiveFighter?.IsPlayerControlled == true;

        // ── Detect hover target ──────────────────────────────────────
        Fighter? newFighter  = null;
        int      newButton   = -1;
        List<(int X, int Y)>? newPath = null;

        if (x < FightModeUI.ActionMenuRight && y < 20 && canInteract)
        {
            if (y == FightModeUI.MoveButtonRow
                || y == FightModeUI.EndTurnButtonRow
                || y == FightModeUI.RunButtonRow
                || _leftPanelLayout.Any(r => r.Y == y))
                newButton = y;
        }
        else if (x >= 20 && x < 80 && y >= 20 && y < 80)
        {
            int ax = x - 20, ay = y - 20;
            // Fighter under cursor?
            newFighter = _state.Fighters.FirstOrDefault(f => f.IsAlive && f.X == ax && f.Y == ay);

            // Path preview: move mode, reachable cell
            if (canInteract && _isMoveMode && _highlightCells?.Contains((ax, ay)) == true)
            {
                var active = _state.ActiveFighter!;
                var path   = FightResolver.BfsPath(_state.Area, active.X, active.Y,
                                                    ax, ay, _state.Fighters, active);
                if (path != null && path.Count > 0)
                {
                    double budget = active.CurrentCineticPoints * (double)Math.Max(1, active.MoveSpeed);
                    int px = active.X, py = active.Y;
                    double acc = 0;
                    int affordable = 0;
                    foreach (var (nx, ny) in path)
                    {
                        double step = FightResolver.MovementStepCost(_state.Area, px, py, nx, ny);
                        if (acc + step > budget + 1e-9) break;
                        acc += step; affordable++; px = nx; py = ny;
                    }
                    if (affordable > 0)
                        newPath = path.Take(affordable).ToList();
                }
            }
        }
        else if (x >= 80 && y >= 40)
        {
            var hit = _rightPanelRows.FirstOrDefault(r => r.Y == y);
            if (hit.Fighter != null) newFighter = hit.Fighter;
        }

        _hoveredFighter   = newFighter;
        _hoveredButtonRow = newButton;
        _previewPath      = newPath;
        // _hoverSkillCells is recomputed each frame inside FullRedraw after _leftPanelLayout is fresh
        _topPanelFighter  = newFighter;
    }

    private void OnCellClicked(int x, int y)
    {
        if (_state.IsOver) return;
        if (_state.Phase == TurnPhase.AnimatingMovement) return; // block during animation

        var active = _state.ActiveFighter;
        if (active == null) return;

        // ── Continue dice result ─────────────────────────────────
        if (_state.Phase == TurnPhase.WaitingForDiceComplete)
        {
            var region = _dice.ContinueButtonRegion;
            if (y == region.Y && x >= region.X && x < region.X + region.Width)
            {
                FinishAttackResolution(active);
                return;
            }
            _dice.HandleHumorClick(x, y);
            return;
        }

        // ── Localization picker overlay ──────────────────────────
        if (_state.Phase == TurnPhase.WaitingForBodyPartChoice && _localizationOverlay != null)
        {
            _localizationOverlay.OnMouseClick(x, y);
            return;
        }

        if (_state.Phase != TurnPhase.SelectingAction) return;
        if (!active.IsPlayerControlled) return;

        // ── Top-left action menu buttons ──────────────────────────
        if (x < FightModeUI.ActionMenuRight && y < 20)
        {
            // Scrollbar: clicking/dragging the bar scrolls the menu instead of selecting a row.
            if (IsOnActionScrollbar(x, y))
            {
                _draggingMenuScrollbar = true;
                SetMenuScrollFromRow(y);
                return;
            }

            if (y == FightModeUI.MoveButtonRow)
            {
                SetMoveMode();
                return;
            }
            if (y == FightModeUI.EndTurnButtonRow)
            {
                ExecuteAction(new Actions.EndTurnAction(active));
                return;
            }
            if (y == FightModeUI.RunButtonRow)
            {
                if (_state.RunUsedThisTurn) return;
                if (active.X == FightArea.ExitCol && active.Y == FightArea.ExitRow)
                {
                    _state.RunUsedThisTurn = true;
                    ExecuteAction(new Actions.RunawayAction(active));
                }
                else
                {
                    _state.AddLog("Must reach the exit tile (⎆) to run away.");
                }
                return;
            }

            foreach (var row in _leftPanelLayout)
            {
                if (row.Y != y) continue;
                switch (row.Kind)
                {
                    case LeftPanelRowKind.Medium:
                        _expandedMediumKey = _expandedMediumKey == row.MediumKey ? null : row.MediumKey;
                        SetMoveMode();
                        break;
                    case LeftPanelRowKind.UnlockedSkill:
                    {
                        if (row.SkillIndex < 0 || row.SkillIndex >= _currentUnlockedSkills.Count) break;
                        var s = _currentUnlockedSkills[row.SkillIndex];
                        if (_state.UsedActionsThisTurn.Contains((row.MediumKey, s.SkillId))) break;
                        if (s.IsSelfTargeting)
                        {
                            _state.UsedActionsThisTurn.Add((row.MediumKey, s.SkillId));
                            ExecuteAction(new Actions.SkillAction(active, active, s,
                                FightModeUI.OrganPartIdFromKey(row.MediumKey),
                                ActiveMediumFromKey(s, row.MediumKey)));
                        }
                        else
                        {
                            SetSkillMode(row.SkillIndex, row.MediumKey);
                        }
                        break;
                    }
                }
                return;
            }
            return;
        }

        // Inert regions: left pan (fighter detail) + top-right (info)
        if (x < 20 && y >= 20) return;
        if (x >= FightModeUI.ActionMenuRight && y < 20) return;

        // ── Center panel targeting ────────────────────────────────
        int ax = x - 20, ay = y - 20;
        if (ax < 0 || ax >= FightArea.Width || ay < 0 || ay >= FightArea.Height) return;

        if (_isMoveMode)
        {
            // Only move to highlighted (reachable) cells
            if (_highlightCells != null && !_highlightCells.Contains((ax, ay))) return;
            TryMoveActiveFighter(active, ax, ay);
        }
        else if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _currentUnlockedSkills.Count)
        {
            var skill = _currentUnlockedSkills[_selectedSkillIndex];
            string mediumKey = _selectedMediumKey ?? DefaultMediumKeyFor(skill);
            if (skill.IsSelfTargeting)
            {
                _state.UsedActionsThisTurn.Add((mediumKey, skill.SkillId));
                ExecuteAction(new Actions.SkillAction(active, active, skill,
                    FightModeUI.OrganPartIdFromKey(mediumKey),
                    ActiveMediumFromKey(skill, mediumKey)));
            }
            else
            {
                // Only act on highlighted cells
                if (_highlightCells != null && !_highlightCells.Contains((ax, ay))) return;

                var target = _state.Fighters.FirstOrDefault(
                    f => f.IsAlive && f.Faction != active.Faction &&
                         f.X == ax && f.Y == ay);
                if (target != null)
                {
                    _state.UsedActionsThisTurn.Add((mediumKey, skill.SkillId));
                    TryUseSkillOnTarget(active, target, skill);
                }
                else
                {
                    // Swing at empty air — deduct CP, log miss, end turn
                    int cost = skill.CineticPointsCost;
                    active.CurrentCineticPoints = Math.Max(0, active.CurrentCineticPoints - cost);
                    _state.UsedActionsThisTurn.Add((mediumKey, skill.SkillId));
                    _state.AddLog($"{active.DisplayName} uses {skill.DisplayName} — nothing there.  [-{cost} CP]", LogEntryType.Miss);
                    _state.Phase = TurnPhase.TurnEnding;
                    AfterActionUpdate();
                }
            }
        }
    }

    /// <summary>Default medium key for a skill used by the legacy fight window.</summary>
    private static string DefaultMediumKeyFor(FightingSkill s) =>
        s.Medium.Type == MediumType.OrganMedium
            ? $"organ:{s.Medium.OrganId ?? s.SkillId}"
            : s.Medium.Type == MediumType.BodyPartMedium
                ? $"bodypart:{s.Medium.BodyPartId ?? s.SkillId}"
                : $"mm:{s.RequiredModusMentisId}";

    private static FightingMedium? ActiveMediumFromKey(FightingSkill skill, string? mediumKey)
    {
        if (mediumKey == null) return null;
        if (mediumKey.StartsWith(FightModeUI.OrganKeyPrefix, StringComparison.Ordinal))
            return skill.GetMediumForOrganId(mediumKey[FightModeUI.OrganKeyPrefix.Length..]);
        if (mediumKey.StartsWith(FightModeUI.OrganPartKeyPrefix, StringComparison.Ordinal))
            return skill.Mediums.FirstOrDefault(m => m.Type == MediumType.OrganMedium);
        if (mediumKey.StartsWith(FightModeUI.BodyPartKeyPrefix, StringComparison.Ordinal))
            return skill.GetMediumForBodyPartId(mediumKey[FightModeUI.BodyPartKeyPrefix.Length..]);
        return null;
    }

    // ── Keyboard shortcuts ────────────────────────────────────────────

    private void HandleKeyboard()
    {
        var active = _state.ActiveFighter;
        if (active == null || !active.IsPlayerControlled) return;
        if (_state.Phase != TurnPhase.SelectingAction) return;

        // 1-9: select skill mode
        for (int i = 0; i < 9; i++)
        {
            if (KeyboardState.IsKeyPressed((Keys)(Keys.D1 + i)))
            {
                if (i < _currentUnlockedSkills.Count)
                {
                    var s = _currentUnlockedSkills[i];
                    string mk = DefaultMediumKeyFor(s);
                    if (!_state.UsedActionsThisTurn.Contains((mk, s.SkillId)))
                        SetSkillMode(i, mk);
                }
                return;
            }
        }

        // M: switch to move mode
        if (KeyboardState.IsKeyPressed(Keys.M)) { SetMoveMode(); return; }

        // E: end turn
        if (KeyboardState.IsKeyPressed(Keys.E))
        {
            ExecuteAction(new Actions.EndTurnAction(active));
            return;
        }

        // R: run
        if (KeyboardState.IsKeyPressed(Keys.R))
        {
            ExecuteAction(new Actions.RunawayAction(active));
            return;
        }

        // ESC: cancel skill mode → revert to move
        if (KeyboardState.IsKeyPressed(Keys.Escape))
        {
            if (!_isMoveMode) SetMoveMode();
            else Close();
            return;
        }

        if (KeyboardState.IsKeyPressed(Keys.PageUp))
            _actionLogScrollOffset = Math.Min(_actionLogScrollOffset + 5, _state.ActionLog.Count);
        if (KeyboardState.IsKeyPressed(Keys.PageDown))
            _actionLogScrollOffset = Math.Max(0, _actionLogScrollOffset - 5);
    }

    // ── Action mode switching ─────────────────────────────────────────

    private void SetMoveMode()
    {
        _isMoveMode = true;
        _selectedSkillIndex = -1;
        _selectedMediumKey = null;
        RecomputeHighlight();
    }

    private void SetSkillMode(int skillIndex, string? mediumKey = null)
    {
        _isMoveMode = false;
        _selectedSkillIndex = skillIndex;
        _selectedMediumKey = mediumKey;
        RecomputeHighlight();
    }

    private void RecomputeHighlight()
    {
        var active = _state.ActiveFighter;
        if (active == null || !active.IsPlayerControlled ||
            _state.Phase != TurnPhase.SelectingAction)
        {
            _highlightCells = null;
            return;
        }

        if (_isMoveMode)
        {
            _isAttackHighlight = false;
            _highlightCells = ComputeReachableCells(active);
        }
        else if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _currentUnlockedSkills.Count)
        {
            _isAttackHighlight = true;
            var skill = _currentUnlockedSkills[_selectedSkillIndex];
            _highlightCells = ComputeSkillTargetCells(active, skill);
        }
        else
        {
            _highlightCells = null;
        }
    }

    private HashSet<(int X, int Y)>? ComputeHoverSkillCells(int buttonRow, Fighter active)
    {
        if (buttonRow < 0) return null;
        if (_state.Phase != TurnPhase.SelectingAction || !active.IsPlayerControlled) return null;

        if (buttonRow == FightModeUI.MoveButtonRow)
            return ComputeReachableCells(active);

        foreach (var r in _leftPanelLayout)
        {
            if (r.Y != buttonRow) continue;
            if (r.Kind == LeftPanelRowKind.UnlockedSkill
                && r.SkillIndex >= 0 && r.SkillIndex < _currentUnlockedSkills.Count)
                return ComputeSkillTargetCells(active, _currentUnlockedSkills[r.SkillIndex]);
            break;
        }
        return null;
    }

    private HashSet<(int X, int Y)> ComputeReachableCells(Fighter fighter)
    {
        var result = new HashSet<(int, int)>();
        double budget = fighter.CurrentCineticPoints * (double)Math.Max(1, fighter.MoveSpeed);
        if (budget <= 0) return result;

        var dist = new Dictionary<(int, int), double>();
        var pq   = new PriorityQueue<(int, int), double>();
        var start = (fighter.X, fighter.Y);
        dist[start] = 0;
        pq.Enqueue(start, 0);

        while (pq.Count > 0)
        {
            pq.TryDequeue(out var cur, out var curCost);
            var (cx, cy) = cur;
            if (curCost > dist.GetValueOrDefault(cur, double.MaxValue)) continue;
            if (cx != fighter.X || cy != fighter.Y) result.Add((cx, cy));

            foreach (var (nx, ny) in new[]
            {
                (cx-1,cy),(cx+1,cy),(cx,cy-1),(cx,cy+1),
                (cx-1,cy-1),(cx+1,cy-1),(cx-1,cy+1),(cx+1,cy+1)
            })
            {
                if (!FightResolver.CanMoveTo(_state.Area, nx, ny, _state.Fighters, fighter)) continue;
                double stepCost = FightResolver.MovementStepCost(_state.Area, cx, cy, nx, ny);
                double newCost  = curCost + stepCost;
                if (newCost > budget) continue;
                var neighbor = (nx, ny);
                if (newCost < dist.GetValueOrDefault(neighbor, double.MaxValue))
                {
                    dist[neighbor] = newCost;
                    pq.Enqueue(neighbor, newCost);
                }
            }
        }
        return result;
    }

    private HashSet<(int X, int Y)> ComputeSkillTargetCells(Fighter attacker, FightingSkill skill)
    {
        var result = new HashSet<(int, int)>();
        if (skill.IsSelfTargeting)
        {
            result.Add((attacker.X, attacker.Y));
            return result;
        }
        // Include every cell within Euclidean range (donut: MinRange <= dist <= Range) + LOS.
        int range = skill.Range;
        int minR  = Math.Max(1, skill.MinRange);
        int rangeSq = range * range;
        int minSq   = minR * minR;
        for (int dy = -range; dy <= range; dy++)
        for (int dx = -range; dx <= range; dx++)
        {
            int distSq = dx * dx + dy * dy;
            if (distSq > rangeSq) continue;
            if (distSq < minSq)   continue;
            int tx = attacker.X + dx, ty = attacker.Y + dy;
            if (tx == attacker.X && ty == attacker.Y) continue;
            if (!_state.Area.IsInBounds(tx, ty)) continue;
            if (_state.Area.GetCell(tx, ty).Type == TerrainType.HardObstacle) continue;
            if (!FightResolver.HasLineOfSight(_state.Area, attacker.X, attacker.Y, tx, ty)) continue;
            result.Add((tx, ty));
        }
        return result;
    }

    // ── Action execution ──────────────────────────────────────────────

    private void ExecuteAction(Actions.IFightAction action)
    {
        action.Execute(_state, _rng);
        AfterActionUpdate();
    }

    private void TryMoveActiveFighter(Fighter fighter, int ax, int ay)
    {
        var path = FightResolver.BfsPath(_state.Area, fighter.X, fighter.Y, ax, ay, _state.Fighters, fighter);
        if (path == null || path.Count == 0) return;

        double budget = fighter.CurrentCineticPoints * (double)Math.Max(1, fighter.MoveSpeed);
        if (budget <= 0) return;

        // Trim to what fits in the cost budget
        int px = fighter.X, py = fighter.Y;
        double accCost = 0;
        int affordable = 0;
        foreach (var (nx, ny) in path)
        {
            double step = FightResolver.MovementStepCost(_state.Area, px, py, nx, ny);
            if (accCost + step > budget + 1e-9) break;
            accCost += step; affordable++; px = nx; py = ny;
        }
        if (affordable == 0) return;

        ExecuteAction(new Actions.MoveAction(fighter, path.Take(affordable).ToList()));
        _highlightCells = null;
    }

    private void TryUseSkillOnTarget(Fighter attacker, Fighter target, FightingSkill skill)
    {
        if (skill.WoundTargetMode == WoundTargetMode.PlayerChooses)
        {
            _state.PendingSkill  = skill;
            _state.PendingTarget = target;
            _state.Phase = TurnPhase.WaitingForBodyPartChoice;
            _highlightCells = null;
            _localizationOverlay = new FightLocalizationOverlay(
                _terminal!, target, skill.DisplayName,
                onSelected: localization =>
                {
                    _state.PendingBodyPartId = localization;
                    _localizationOverlay = null;
                    ExecuteAction(new Actions.SkillAction(attacker, target, skill,
                        null, ActiveMediumFromKey(skill, _selectedMediumKey)));
                },
                onCancel: () =>
                {
                    _localizationOverlay = null;
                    _state.PendingSkill = null;
                    _state.PendingTarget = null;
                    _state.Phase = TurnPhase.SelectingAction;
                    RecomputeHighlight();
                });
            _localizationOverlay.Render();
            return;
        }

        _state.PendingTarget = target;
        ExecuteAction(new Actions.SkillAction(attacker, target, skill,
            null, ActiveMediumFromKey(skill, _selectedMediumKey)));
    }

    private void BeginDiceRoll()
    {
        _diceElapsed = 0;
        Fighter? primaryOwner;
        if (_state.PendingSkill != null
            && _state.PendingSkill.EffectType == FightingSkillEffect.Attack)
        {
            bool isEnemyAttack = _state.ActiveFighter?.IsPlayerControlled == false;
            if (isEnemyAttack)
            {
                _dice.StartDual(
                    primaryDice: _state.DiceSecondaryNumberOfDice,
                    secondaryDice: _state.DiceNumberOfDice,
                    primaryLabel: "Defense",
                    secondaryLabel: "Attack",
                    subtitle: $"{_state.PendingSkill.DisplayName} → {_state.PendingTarget?.DisplayName}");
                primaryOwner = _state.PendingTarget; // the defender is the player
            }
            else
            {
                _dice.StartDual(
                    primaryDice: _state.DiceNumberOfDice,
                    secondaryDice: _state.DiceSecondaryNumberOfDice,
                    primaryLabel: "Attack",
                    secondaryLabel: "Defense",
                    subtitle: $"{_state.PendingSkill.DisplayName} → {_state.PendingTarget?.DisplayName}");
                primaryOwner = _state.ActiveFighter;
            }
        }
        else
        {
            _dice.Start(_state.DiceNumberOfDice, _state.DiceDifficulty);
            primaryOwner = _state.ActiveFighter;
        }

        EnableHumorIfPlayer(primaryOwner);
    }

    /// <summary>
    /// Enable the humor-modifier layer for the player's primary dice group. No-op when the owner
    /// is null, AI-controlled, or has no viscera modifier budget.
    /// </summary>
    private void EnableHumorIfPlayer(Fighter? owner)
    {
        if (owner == null || !owner.IsPlayerControlled) return;
        var member = owner.Member;
        int limit = member.DerivedStats.First(s => s.Name == "humor_modifier_limit").GetValue(member);
        if (limit > 0) _dice.EnableHumorModifiers(member.HumorQueues, limit);
    }

    private void FinishAttackResolution(Fighter active)
    {
        _dice.Hide();
        if (_state.PendingSkill == null || _state.PendingTarget == null || _state.DiceFinalValues == null)
        {
            _state.Phase = TurnPhase.TurnEnding;
            AfterActionUpdate();
            return;
        }

        var result = FightResolver.ResolveAttack(
            active, _state.PendingTarget, _state.PendingSkill,
            _state.DiceFinalValues, _state.PendingBodyPartId, _rng,
            defenseDiceValues: _state.DiceSecondaryFinalValues);

        if (result.IsHit && result.Wound != null)
        {
            FightResolver.ApplyWound(_state.PendingTarget, result.Wound);
            _state.AddLog($"HIT! {result.Wound.WoundName} on {_state.PendingTarget.DisplayName}. (atk {result.SixesCount} vs def {result.DefenseSixes})", LogEntryType.Wound);
        }
        else
        {
            _state.AddLog($"MISS. (atk {result.SixesCount} vs def {result.DefenseSixes})", LogEntryType.Miss);
        }

        _state.CheckFightEnd();
        if (!_state.IsOver)
            EndTurn(active);
    }

    private void EndTurn(Fighter active)
    {
        active.HasActedThisTurn = true;
        _state.AdvanceToNextFighter(_rng);
        RefreshSkillList();

        var next = _state.ActiveFighter;
        if (next != null && !next.IsPlayerControlled)
            _aiDelayFrames = AiDelay;

        _actionLogScrollOffset = 0;
    }

    private void AfterActionUpdate()
    {
        _state.CheckFightEnd();
        if (_state.IsOver) return;

        if (_state.Phase == TurnPhase.AnimatingDice)
        {
            BeginDiceRoll();
            return;
        }

        if (_state.Phase == TurnPhase.TurnEnding)
        {
            var act = _state.ActiveFighter;
            if (act != null) EndTurn(act);
        }
    }

    private void ExecuteAiTurn()
    {
        var ai = _state.ActiveFighter;
        if (ai == null) return;

        var action = FightAI.DecideAction(ai, _state, _skillRegistry, _rng);
        action.Execute(_state, _rng);

        _state.CheckFightEnd();
        if (_state.IsOver) return;

        if (_state.Phase == TurnPhase.AnimatingMovement)
            return; // let animation proceed; AI will continue after it finishes

        if (_state.Phase == TurnPhase.AnimatingDice)
        {
            BeginDiceRoll();
            return;
        }

        if (_state.Phase == TurnPhase.TurnEnding)
            EndTurn(ai);
    }

    // ── Rendering ─────────────────────────────────────────────────────

    private void FullRedraw()
    {
        if (_terminal == null) return;
        var active = _state.ActiveFighter;

        var detailFighter = _topPanelFighter ?? active;
        _ = FightModeUI.RenderDetailPanel(_terminal, detailFighter,
            isHoverOverride: _topPanelFighter != null && _topPanelFighter != active);

        if (active != null)
        {
            bool isMove = _isMoveMode || !active.IsPlayerControlled ||
                          _state.Phase == TurnPhase.AnimatingMovement;
            _leftPanelLayout = FightModeUI.RenderLeftPanel(_terminal, active,
                _currentUnlockedSkills, Array.Empty<FightingSkill>(), _currentUnaffordableSkills,
                isMove, _selectedSkillIndex, -1,
                _expandedMediumKey, _hoveredButtonRow,
                _state.UsedActionsThisTurn, _state.RunUsedThisTurn,
                _actionMenuScrollOffset,
                _draggingMenuScrollbar || IsOnActionScrollbar(_hoverX, _hoverY),
                out _actionMenuMaxScroll);
            _actionMenuScrollOffset = Math.Min(_actionMenuScrollOffset, _actionMenuMaxScroll);

            // Recompute hover-blink cells now that the layout is current
            _hoverSkillCells = ComputeHoverSkillCells(_hoveredButtonRow, active);

            // Bottom-half info — simple version (no learnable tracking in legacy window)
            FightModeUI.LeftInfoKind infoKind = FightModeUI.LeftInfoKind.None;
            FightingSkill? infoSkill = null;
            if (_hoveredButtonRow == FightModeUI.MoveButtonRow) infoKind = FightModeUI.LeftInfoKind.Move;
            else if (_hoveredButtonRow == FightModeUI.EndTurnButtonRow) infoKind = FightModeUI.LeftInfoKind.EndTurn;
            else if (_hoveredButtonRow == FightModeUI.RunButtonRow) infoKind = FightModeUI.LeftInfoKind.Run;
            else if (_hoveredButtonRow >= 0)
            {
                foreach (var r in _leftPanelLayout)
                {
                    if (r.Y != _hoveredButtonRow) continue;
                    if (r.Kind == LeftPanelRowKind.UnlockedSkill
                        && r.SkillIndex >= 0 && r.SkillIndex < _currentUnlockedSkills.Count)
                    {
                        infoKind = FightModeUI.LeftInfoKind.Skill;
                        infoSkill = _currentUnlockedSkills[r.SkillIndex];
                    }
                    else if (r.Kind == LeftPanelRowKind.UnaffordableSkill
                        && r.SkillIndex >= 0 && r.SkillIndex < _currentUnaffordableSkills.Count)
                    {
                        infoKind = FightModeUI.LeftInfoKind.Skill;
                        infoSkill = _currentUnaffordableSkills[r.SkillIndex];
                    }
                    break;
                }
            }
            if (infoKind == FightModeUI.LeftInfoKind.None)
            {
                if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _currentUnlockedSkills.Count)
                {
                    infoKind = FightModeUI.LeftInfoKind.Skill;
                    infoSkill = _currentUnlockedSkills[_selectedSkillIndex];
                }
                else if (_isMoveMode) infoKind = FightModeUI.LeftInfoKind.Move;
            }
            FightModeUI.RenderLeftInfoPanel(_terminal, infoKind, infoSkill, active);
        }

        FightModeUI.RenderCenterPanel(_terminal, _state.Area, _state.Fighters,
            active, _blinkOn, _highlightCells, _isAttackHighlight, _previewPath, _hoverSkillCells);

        // Localization picker is rendered via the overlay path below; nothing to do here.

        int initHoverY = _hoveredFighter != null
            ? _rightPanelRows.FirstOrDefault(r => r.Fighter == _hoveredFighter).Y
            : -1;
        _rightPanelRows = FightModeUI.RenderRightPanel(_terminal, _state.Area, _state, initHoverY);
        FightModeUI.RenderBottomPanel(_terminal, _state.ActionLog, _actionLogScrollOffset);

        if (_dice.IsVisible)
            FightModeUI.RenderDiceOverlay(_terminal, _dice, _continueHovered);

        // Localization picker box — drawn last so it sits on top of the fight UI.
        if (_localizationOverlay != null && _state.Phase == TurnPhase.WaitingForBodyPartChoice)
            _localizationOverlay.Render();

        if (_state.IsOver)
            FightModeUI.RenderFightEnd(_terminal, _state.Result);
    }

    private void RefreshSkillList()
    {
        var active = _state.ActiveFighter;
        _currentUnlockedSkills = active != null
            ? active.GetUnlockedSkills(_skillRegistry).ToList()
            : new List<FightingSkill>();
        _currentUnaffordableSkills = active != null
            ? active.GetUnaffordableKnownSkills(_skillRegistry).ToList()
            : new List<FightingSkill>();

        // Default: MOVE mode at start of turn
        _isMoveMode = true;
        _selectedSkillIndex = -1;
        _expandedMediumKey = null;
        _actionMenuScrollOffset = 0;
        RecomputeHighlight();
        _actionLogScrollOffset = 0;
        _previewPath = null;
        _hoverSkillCells = null;
        _hoveredButtonRow = -1;
        _topPanelFighter = null;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private int[] GenerateDiceValues(int count)
    {
        var vals = new int[count];
        for (int i = 0; i < count; i++)
            vals[i] = _rng.Next(1, 7);
        return vals;
    }
}
