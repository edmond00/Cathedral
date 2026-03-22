using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Game;
using Cathedral.Game.Narrative;
using Cathedral.Game.Npc;
using Cathedral.Fight.Generators;
using Cathedral.Terminal;

namespace Cathedral.Fight;

/// <summary>
/// Result of a fight, reported back to the narrative controller.
/// </summary>
public enum FightAdapterResult
{
    /// <summary>Fight is still ongoing.</summary>
    Ongoing,
    /// <summary>Player won the fight.</summary>
    Victory,
    /// <summary>Player died.</summary>
    Death,
    /// <summary>Player ran away.</summary>
    Runaway
}

/// <summary>
/// Embeds the fight system into the main game window's terminal.
/// Extracts the game logic from <see cref="FightModeWindow"/> into a standalone controller
/// that operates on a provided <see cref="TerminalHUD"/> instead of owning its own window.
/// </summary>
public class FightModeAdapter
{
    // ── Core objects ─────────────────────────────────────────────────
    private readonly TerminalHUD _terminal;
    private readonly PopupTerminalHUD? _popup;
    private readonly FightState _state;
    private readonly FightingSkillRegistry _skillRegistry;
    private readonly DiceRollComponent _dice = new();
    private readonly Random _rng = new();

    // ── Source NPC (for outcome reporting) ───────────────────────────
    private readonly NpcEntity _targetNpc;

    // ── Action mode ─────────────────────────────────────────────────
    private bool _isMoveMode = true;
    private int _selectedSkillIndex = -1;
    private HashSet<(int X, int Y)>? _highlightCells;
    private bool _isAttackHighlight;

    // ── UI state ────────────────────────────────────────────────────
    private int _actionLogScrollOffset;
    private IReadOnlyList<FightingSkill> _currentUnlockedSkills = Array.Empty<FightingSkill>();
    private IReadOnlyList<string>? _bodyPartMenu;
    private bool _continueHovered;
    private Fighter? _hoveredFighter;
    private int _hoveredButtonRow = -1;
    private List<(int X, int Y)>? _previewPath;
    private double _hoverTimer;
    private bool _popupVisible;

    // ── Blink ───────────────────────────────────────────────────────
    private double _blinkTimer;
    private bool _blinkOn = true;

    // ── AI delay ────────────────────────────────────────────────────
    private int _aiDelayFrames;
    private const int AiDelay = 40;

    // ── Movement animation ──────────────────────────────────────────
    private int _movementFrameTimer;
    private const int PlayerMoveFramesPerTile = 10;
    private const int AiMoveFramesPerTile = 1;

    // ── Dice timing ─────────────────────────────────────────────────
    private const float DiceRollDuration = 2.0f;
    private double _diceElapsed;

    // ── Elapsed time tracking (caller must provide delta) ───────────
    private double _lastDeltaTime;

    /// <summary>
    /// The result of the fight once it's over. <see cref="FightAdapterResult.Ongoing"/> while in progress.
    /// </summary>
    public FightAdapterResult Result { get; private set; } = FightAdapterResult.Ongoing;

    /// <summary>Whether the fight has ended.</summary>
    public bool IsOver => Result != FightAdapterResult.Ongoing;

    /// <summary>The NPC that was fought.</summary>
    public NpcEntity TargetNpc => _targetNpc;

    public FightModeAdapter(
        TerminalHUD terminal,
        PopupTerminalHUD? popup,
        NpcEntity targetNpc,
        Protagonist protagonist)
    {
        _terminal = terminal;
        _popup = popup;
        _targetNpc = targetNpc;
        _skillRegistry = FightingSkillRegistry.Instance;

        // Generate arena
        var generator = new ArenaGenerator { Seed = Environment.TickCount };
        var area = generator.Generate();

        // Build fighters
        var fighters = BuildFighters(protagonist, targetNpc);

        // Roll initiative
        foreach (var f in fighters)
            f.InitiativeRoll = _rng.Next(1, 7) + f.InitiativeValue;

        fighters.Sort((a, b) =>
        {
            int cmp = b.InitiativeRoll.CompareTo(a.InitiativeRoll);
            return cmp != 0 ? cmp : (a.Faction == FighterFaction.Party ? -1 : 1);
        });

        _state = new FightState(area, fighters);
        _state.AddLog("Fight begins!");

        // Render initial arena terrain
        FightAreaRenderer.Render(_terminal, area, "fight", 0);

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

        Console.WriteLine($"FightModeAdapter: Fight started against {targetNpc.DisplayName}");
    }

    private static List<Fighter> BuildFighters(Protagonist protagonist, NpcEntity npc)
    {
        var fighters = new List<Fighter>();

        // Player party
        var partyFighter = new Fighter(protagonist,
            FightArea.ZoneColStart + 2, FightArea.PlayerRowStart + 1,
            isPlayerControlled: true, FighterFaction.Party);
        fighters.Add(partyFighter);

        // Add companions as party fighters
        int companionOffset = 0;
        foreach (var companion in protagonist.CompanionParty)
        {
            companionOffset++;
            var cf = new Fighter(companion,
                FightArea.ZoneColStart + 2 + companionOffset * 2, FightArea.PlayerRowStart + 1,
                isPlayerControlled: false, FighterFaction.Party);
            fighters.Add(cf);
        }

        // Enemy NPC
        var enemyFighter = new Fighter(npc.Combatant,
            FightArea.ZoneColStart + 2, FightArea.EnemyRowStart + 1,
            isPlayerControlled: false, FighterFaction.Enemy);
        fighters.Add(enemyFighter);

        return fighters;
    }

    /// <summary>
    /// Called every frame. Pass the frame delta time for animations.
    /// </summary>
    public void Update(double deltaTime)
    {
        _lastDeltaTime = deltaTime;

        // ── Fight ended ───────────────────────────────────────────
        if (_state.IsOver && Result == FightAdapterResult.Ongoing)
        {
            Result = _state.Result switch
            {
                FightResult.PartyWon => FightAdapterResult.Victory,
                FightResult.EnemyWon => FightAdapterResult.Death,
                FightResult.PartyFled => FightAdapterResult.Runaway,
                _ => FightAdapterResult.Victory
            };
            FullRedraw();
            return;
        }

        if (_state.IsOver)
        {
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
                    int prevX = _state.MovingFighter.X;
                    int prevY = _state.MovingFighter.Y;
                    var (nx, ny) = _state.MovementPath[_state.MovementPathIndex++];
                    _state.MovingFighter.X = nx;
                    _state.MovingFighter.Y = ny;

                    var terrCell = _state.Area.GetCell(prevX, prevY);
                    _terminal.SetCell(20 + prevX, 20 + prevY,
                        terrCell.Glyph, terrCell.TextColor, terrCell.BgColor);
                }
                else
                {
                    var mover = _state.MovingFighter;
                    _state.MovementPath = null;
                    _state.MovingFighter = null;
                    _state.MovementPathIndex = 0;
                    _state.Phase = TurnPhase.SelectingAction;
                    RefreshSkillList();
                    RecomputeHighlight();
                    if (!mover.IsPlayerControlled)
                        _aiDelayFrames = 5;
                }
            }
            FullRedraw();
            return;
        }

        // ── Blink ─────────────────────────────────────────────────
        _blinkTimer += deltaTime;
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
            _diceElapsed += deltaTime;
            if (_diceElapsed >= DiceRollDuration)
            {
                var finalValues = GenerateDiceValues(_state.DiceNumberOfDice);
                _dice.Complete(finalValues);
                _state.DiceFinalValues = finalValues;
                _state.IsDiceRolling = false;
                _state.Phase = TurnPhase.WaitingForDiceComplete;
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

        // ── Hover popup delay ──────────────────────────────────────
        if (!_popupVisible && (_hoveredFighter != null || _hoveredButtonRow >= 0))
        {
            _hoverTimer += deltaTime;
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

    /// <summary>Called by the game loop when a terminal cell is clicked.</summary>
    public void OnCellClicked(int x, int y)
    {
        if (_state.IsOver) return;
        if (_state.Phase == TurnPhase.AnimatingMovement) return;

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
                var target = _state.Fighters.FirstOrDefault(
                    f => f.IsAlive && f.Faction != active.Faction &&
                         f.X == ax && f.Y == ay);
                if (target != null)
                    TryUseSkillOnTarget(active, target, skill);
            }
        }
    }

    /// <summary>Called by the game loop when a terminal cell is hovered.</summary>
    public void OnCellHovered(int x, int y)
    {
        // ── Dice continue button ─────────────────────────────────────
        if (_state.Phase == TurnPhase.WaitingForDiceComplete)
        {
            var region = _dice.ContinueButtonRegion;
            _continueHovered = (y == region.Y && x >= region.X && x < region.X + region.Width);
        }

        bool canInteract = _state.Phase == TurnPhase.SelectingAction
                        && _state.ActiveFighter?.IsPlayerControlled == true;

        Fighter? newFighter = null;
        int newButton = -1;
        List<(int X, int Y)>? newPath = null;

        if (x < 20 && canInteract)
        {
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
            newFighter = _state.Fighters.FirstOrDefault(f => f.IsAlive && f.X == ax && f.Y == ay);

            if (canInteract && _isMoveMode && _highlightCells?.Contains((ax, ay)) == true)
            {
                var active = _state.ActiveFighter!;
                var path = FightResolver.BfsPath(_state.Area, active.X, active.Y,
                                                    ax, ay, _state.Fighters, active);
                if (path != null && path.Count > 0)
                {
                    double budget = active.CurrentCineticPoints * (double)Math.Max(1, active.MoveSpeed);
                    int px = active.X, py = active.Y;
                    double acc = 0;
                    int affordable = 0;
                    foreach (var (nx, ny) in path)
                    {
                        double step = (nx != px && ny != py) ? 1.5 : 1.0;
                        if (acc + step > budget + 1e-9) break;
                        acc += step; affordable++; px = nx; py = ny;
                    }
                    if (affordable > 0)
                        newPath = path.Take(affordable).ToList();
                }
            }
        }
        else if (y < 20)
        {
            newFighter = FindFighterLabelAt(x, y);
        }

        bool targetChanged = newFighter != _hoveredFighter || newButton != _hoveredButtonRow;
        if (targetChanged)
        {
            _hoverTimer = 0;
            _popupVisible = false;
        }

        _hoveredFighter = newFighter;
        _hoveredButtonRow = newButton;
        _previewPath = newPath;
    }

    /// <summary>Called by the game loop for keyboard input.</summary>
    public void OnKeyPress(OpenTK.Windowing.GraphicsLibraryFramework.Keys key)
    {
        var active = _state.ActiveFighter;
        if (active == null || !active.IsPlayerControlled) return;

        // When fight is over, Enter or Escape acknowledges the result
        if (_state.IsOver)
        {
            if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Enter ||
                key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape)
            {
                // Result already set in Update()
            }
            return;
        }

        if (_state.Phase != TurnPhase.SelectingAction) return;

        for (int i = 0; i < 9; i++)
        {
            if (key == (OpenTK.Windowing.GraphicsLibraryFramework.Keys)((int)OpenTK.Windowing.GraphicsLibraryFramework.Keys.D1 + i))
            {
                if (i < _currentUnlockedSkills.Count)
                    SetSkillMode(i);
                return;
            }
        }

        if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.M) { SetMoveMode(); return; }
        if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.E)
        {
            ExecuteAction(new Actions.EndTurnAction(active));
            return;
        }
        if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.R)
        {
            if (active.X == FightArea.ExitCol && active.Y == FightArea.ExitRow)
                ExecuteAction(new Actions.RunawayAction(active));
            else
                _state.AddLog("Must reach the exit tile (⎆) to run away.");
            return;
        }
        if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.Escape)
        {
            if (!_isMoveMode) SetMoveMode();
            return;
        }

        if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.PageUp)
            _actionLogScrollOffset = Math.Min(_actionLogScrollOffset + 5, _state.ActionLog.Count);
        if (key == OpenTK.Windowing.GraphicsLibraryFramework.Keys.PageDown)
            _actionLogScrollOffset = Math.Max(0, _actionLogScrollOffset - 5);
    }

    /// <summary>Called by the game loop for mouse wheel scrolling.</summary>
    public void OnMouseWheel(float delta)
    {
        _actionLogScrollOffset = Math.Max(0, _actionLogScrollOffset - (int)delta);
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
        double budget = fighter.CurrentCineticPoints * (double)Math.Max(1, fighter.MoveSpeed);
        if (budget <= 0) return result;

        var dist = new Dictionary<(int, int), double>();
        var pq = new PriorityQueue<(int, int), double>();
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
                double stepCost = (nx != cx && ny != cy) ? 1.5 : 1.0;
                double newCost = curCost + stepCost;
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

        double budget = fighter.CurrentCineticPoints * (double)Math.Max(1, fighter.MoveSpeed);
        if (budget <= 0) return;

        int px = fighter.X, py = fighter.Y;
        double accCost = 0;
        int affordable = 0;
        foreach (var (nx, ny) in path)
        {
            double step = (nx != px && ny != py) ? 1.5 : 1.0;
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
            _state.PendingSkill = skill;
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
            return;

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

        _isMoveMode = true;
        _selectedSkillIndex = -1;
        RecomputeHighlight();
        _actionLogScrollOffset = 0;
        _previewPath = null;
        _hoveredButtonRow = -1;
        _popupVisible = false;
        _hoverTimer = 0;
    }

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
