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
    private HashSet<(int X, int Y)>? _highlightCells;
    private bool _isAttackHighlight;   // red vs green tint on highlighted tiles

    // ── UI state ──────────────────────────────────────────────────────
    private int _actionLogScrollOffset;
    private IReadOnlyList<FightingSkill> _currentUnlockedSkills = Array.Empty<FightingSkill>();
    private IReadOnlyList<string>? _bodyPartMenu;
    private bool _continueHovered;
    private Fighter? _hoveredFighter;
    private int  _hoveredButtonRow = -1;      // terminal row of the hovered left-panel button
    private List<(int X, int Y)>? _previewPath; // arena-coord path preview dots
    private double _hoverTimer;               // seconds hovering same target
    private bool   _popupVisible;

    // ── Blink ─────────────────────────────────────────────────────────
    private double _blinkTimer;
    private bool _blinkOn = true;

    // ── AI delay ─────────────────────────────────────────────────────
    private int _aiDelayFrames;
    private const int AiDelay = 40;

    // ── Movement animation ────────────────────────────────────────────
    private int _movementFrameTimer;
    private const int PlayerMoveFramesPerTile = 10;
    private const int AiMoveFramesPerTile     = 1;

    // ── Dice timing ──────────────────────────────────────────────────
    private const float DiceRollDuration = 2.0f;
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
        _blinkTimer += args.Time;
        bool newBlink = (_blinkTimer % 0.8) < 0.4;
        if (newBlink != _blinkOn)
        {
            _blinkOn = newBlink;
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
                _dice.Complete(finalValues);
                _state.DiceFinalValues = finalValues;
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

        // ── Fighter / button hover popup delay ──────────────────────
        if (!_popupVisible && (_hoveredFighter != null || _hoveredButtonRow >= 0))
        {
            _hoverTimer += args.Time;
            if (_hoverTimer >= 1.2)
            {
                _popupVisible = true;
                if (_popup != null)
                {
                    if (_hoveredButtonRow >= 0)
                        FightModeUI.RenderActionPopup(_popup, _hoveredButtonRow,
                            _currentUnlockedSkills, _state.ActiveFighter);
                    else if (_hoveredFighter != null)
                        FightModeUI.RenderFighterPopup(_popup, _hoveredFighter);
                }
            }
        }

        FullRedraw();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _terminal?.Render(new Vector2i(ClientSize.X, ClientSize.Y));
        if (_popupVisible)
            _popup?.Render(new Vector2i(ClientSize.X, ClientSize.Y));
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
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        _actionLogScrollOffset = Math.Max(0, _actionLogScrollOffset - (int)e.OffsetY);
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
        // ── Dice continue button ─────────────────────────────────────
        if (_state.Phase == TurnPhase.WaitingForDiceComplete)
        {
            var region = _dice.ContinueButtonRegion;
            _continueHovered = (y == region.Y && x >= region.X && x < region.X + region.Width);
        }

        bool canInteract = _state.Phase == TurnPhase.SelectingAction
                        && _state.ActiveFighter?.IsPlayerControlled == true;

        // ── Detect hover target ──────────────────────────────────────
        Fighter? newFighter  = null;
        int      newButton   = -1;
        List<(int X, int Y)>? newPath = null;

        if (x < 20 && canInteract)
        {
            // Left panel: button hover
            int skillIdx = y - FightModeUI.SkillButtonsStart;
            if (y == FightModeUI.MoveButtonRow
                || y == FightModeUI.EndTurnButtonRow
                || y == FightModeUI.RunButtonRow
                || (skillIdx >= 0 && skillIdx < _currentUnlockedSkills.Count))
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
                    int maxSteps = active.CurrentCineticPoints * Math.Max(1, active.MoveSpeed);
                    newPath = path.Take(maxSteps).ToList();
                }
            }
        }
        else if (y < 20)
        {
            newFighter = FindFighterLabelAt(x, y);
        }

        // ── Reset timer when target changes ──────────────────────────
        bool targetChanged = newFighter != _hoveredFighter || newButton != _hoveredButtonRow;
        if (targetChanged)
        {
            _hoverTimer  = 0;
            _popupVisible = false;
        }

        _hoveredFighter  = newFighter;
        _hoveredButtonRow = newButton;
        _previewPath     = newPath;
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
                FinishAttackResolution(active);
            return;
        }

        // ── Body part menu ────────────────────────────────────────
        if (_state.Phase == TurnPhase.WaitingForBodyPartChoice && _bodyPartMenu != null)
        {
            var (startRow, _) = FightModeUI.BodyPartMenuItemOrigin();
            int menuRow = y - startRow;
            if (menuRow >= 0 && menuRow < _bodyPartMenu.Count)
            {
                _state.PendingBodyPartId = _bodyPartMenu[menuRow];
                _bodyPartMenu = null;
                BeginDiceRoll();
            }
            return;
        }

        if (_state.Phase != TurnPhase.SelectingAction) return;
        if (!active.IsPlayerControlled) return;

        // ── Left panel buttons ────────────────────────────────────
        if (x < 20)
        {
            int skillIdx = y - FightModeUI.SkillButtonsStart;
            if (y == FightModeUI.MoveButtonRow)
            {
                SetMoveMode();
            }
            else if (skillIdx >= 0 && skillIdx < _currentUnlockedSkills.Count)
            {
                SetSkillMode(skillIdx);
            }
            else if (y == FightModeUI.EndTurnButtonRow)
            {
                ExecuteAction(new Actions.EndTurnAction(active));
            }
            else if (y == FightModeUI.RunButtonRow)
            {
                // Runaway only allowed from the exit tile
                if (active.X == FightArea.ExitCol && active.Y == FightArea.ExitRow)
                    ExecuteAction(new Actions.RunawayAction(active));
                else
                    _state.AddLog("Must reach the exit tile (⎆) to run away.");
            }
            return;
        }

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
            if (skill.EffectType == FightingSkillEffect.DefensePosture)
            {
                ExecuteAction(new Actions.SkillAction(active, active, skill));
            }
            else
            {
                // Find fighter at the clicked cell
                var target = _state.Fighters.FirstOrDefault(
                    f => f.IsAlive && f.Faction != active.Faction &&
                         f.X == ax && f.Y == ay);
                if (target != null)
                    TryUseSkillOnTarget(active, target, skill);
            }
        }
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
                    SetSkillMode(i);
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
        RecomputeHighlight();
    }

    private void SetSkillMode(int skillIndex)
    {
        _isMoveMode = false;
        _selectedSkillIndex = skillIndex;
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

    private HashSet<(int X, int Y)> ComputeReachableCells(Fighter fighter)
    {
        var result = new HashSet<(int, int)>();
        int maxSteps = fighter.CurrentCineticPoints * Math.Max(1, fighter.MoveSpeed);
        if (maxSteps <= 0) return result;

        var queue   = new Queue<(int x, int y, int steps)>();
        var visited = new HashSet<(int, int)>();
        queue.Enqueue((fighter.X, fighter.Y, 0));
        visited.Add((fighter.X, fighter.Y));

        while (queue.Count > 0)
        {
            var (cx, cy, steps) = queue.Dequeue();
            if (steps > 0) result.Add((cx, cy));
            if (steps >= maxSteps) continue;
            foreach (var (nx, ny) in new[]
            {
                (cx-1,cy),(cx+1,cy),(cx,cy-1),(cx,cy+1), // cardinal
                (cx-1,cy-1),(cx+1,cy-1),(cx-1,cy+1),(cx+1,cy+1) // diagonal
            })
            {
                if (!visited.Contains((nx, ny)) &&
                    FightResolver.CanMoveTo(_state.Area, nx, ny, _state.Fighters, fighter))
                {
                    visited.Add((nx, ny));
                    queue.Enqueue((nx, ny, steps + 1));
                }
            }
        }
        return result;
    }

    private HashSet<(int X, int Y)> ComputeSkillTargetCells(Fighter attacker, FightingSkill skill)
    {
        var result = new HashSet<(int, int)>();
        if (skill.EffectType == FightingSkillEffect.DefensePosture)
        {
            result.Add((attacker.X, attacker.Y));
            return result;
        }
        foreach (var f in _state.Fighters)
        {
            if (f.Faction == attacker.Faction || !f.IsAlive) continue;
            int dist = Math.Abs(f.X - attacker.X) + Math.Abs(f.Y - attacker.Y);
            if (dist <= skill.Range)
                result.Add((f.X, f.Y));
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

        int maxSteps = fighter.CurrentCineticPoints * Math.Max(1, fighter.MoveSpeed);
        if (maxSteps <= 0) return;

        var trimmed = path.Take(maxSteps).ToList();
        ExecuteAction(new Actions.MoveAction(fighter, trimmed));
        // After MoveAction, phase is AnimatingMovement — clear highlights
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
            return;
        }

        _state.PendingTarget = target;
        ExecuteAction(new Actions.SkillAction(attacker, target, skill));
    }

    private void BeginDiceRoll()
    {
        _diceElapsed = 0;
        _dice.Start(_state.DiceNumberOfDice, _state.DiceDifficulty);
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
            _state.DiceFinalValues, _state.PendingBodyPartId, _rng);

        if (result.IsHit && result.Wound != null)
        {
            FightResolver.ApplyWound(_state.PendingTarget, result.Wound);
            _state.AddLog($"HIT! {result.Wound.WoundName} on {_state.PendingTarget.DisplayName}. ({result.SixesCount} sixes vs DEF {result.NaturalDefense})");
        }
        else
        {
            _state.AddLog($"MISS. ({result.SixesCount} sixes vs DEF {result.NaturalDefense})");
        }

        _state.CheckFightEnd();
        if (!_state.IsOver)
            EndTurn(active);
    }

    private void EndTurn(Fighter active)
    {
        active.HasActedThisTurn = true;
        _state.AdvanceToNextFighter();
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
            var finalValues = GenerateDiceValues(_state.DiceNumberOfDice);
            _state.DiceFinalValues = finalValues;
            _state.IsDiceRolling = false;
            FinishAttackResolution(ai);
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

        FightModeUI.RenderTopPanel(_terminal, _state);

        if (active != null)
        {
            if (_state.Phase == TurnPhase.WaitingForBodyPartChoice && _state.PendingTarget != null)
            {
                _bodyPartMenu = FightModeUI.RenderBodyPartMenu(_terminal, _state.PendingTarget);
            }
            else
            {
                _bodyPartMenu = null;
                bool isMove = _isMoveMode || !active.IsPlayerControlled ||
                              _state.Phase == TurnPhase.AnimatingMovement;
                FightModeUI.RenderLeftPanel(_terminal, active, _currentUnlockedSkills,
                    isMove, _selectedSkillIndex, _hoveredButtonRow);
            }
        }

        FightModeUI.RenderCenterPanel(_terminal, _state.Area, _state.Fighters,
            active, _blinkOn, _highlightCells, _isAttackHighlight, _previewPath);

        FightModeUI.RenderRightPanel(_terminal, _state.Area);
        FightModeUI.RenderBottomPanel(_terminal, _state.ActionLog, _actionLogScrollOffset);

        if (_dice.IsVisible)
            FightModeUI.RenderDiceOverlay(_terminal, _dice, _continueHovered);

        if (_state.IsOver)
            FightModeUI.RenderFightEnd(_terminal, _state.Result);
    }

    private void RefreshSkillList()
    {
        var active = _state.ActiveFighter;
        _currentUnlockedSkills = active != null
            ? active.GetUnlockedSkills(_skillRegistry).ToList()
            : new List<FightingSkill>();

        // Default: MOVE mode at start of turn
        _isMoveMode = true;
        _selectedSkillIndex = -1;
        RecomputeHighlight();
        _actionLogScrollOffset = 0;
        _previewPath = null;
        _hoveredButtonRow = -1;
        _popupVisible = false;
        _hoverTimer = 0;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    /// <summary>
    /// Mirrors the RenderTopPanel label layout to find which fighter, if any, is under terminal cell (hx, hy).
    /// </summary>
    private Fighter? FindFighterLabelAt(int hx, int hy)
    {
        const int RightStart = 80;
        int x = 2, y = 3;
        foreach (var f in _state.Fighters)
        {
            string mark = f == _state.ActiveFighter ? "▶ " : "  ";
            string label = $"{mark}{f.DisplayChar} {f.DisplayName} HP:{f.CurrentHp}/{f.MaxHp} CP:{f.CurrentCineticPoints}/{f.MaxCineticPoints}";
            if (!f.IsAlive) label += " [DEAD]";
            if (x + label.Length + 2 > RightStart) { x = 2; y += 2; }
            if (hy == y && hx >= x && hx < x + label.Length)
                return f;
            x += label.Length + 3;
        }
        return null;
    }

    private int[] GenerateDiceValues(int count)
    {
        var vals = new int[count];
        for (int i = 0; i < count; i++)
            vals[i] = _rng.Next(1, 7);
        return vals;
    }
}
