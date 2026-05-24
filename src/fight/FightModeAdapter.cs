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
    // (popup terminal kept in the constructor signature for caller compatibility; no longer rendered)
    private readonly FightState _state;
    private readonly FightingSkillRegistry _skillRegistry;
    private readonly DiceRollComponent _dice = new();
    private readonly Random _rng = new();

    // ── Source NPC (for outcome reporting) ───────────────────────────
    private readonly NpcEntity _targetNpc;
    private readonly IReadOnlyList<NpcEntity> _allies;

    /// <summary>All enemy NPCs in this fight: main target + allies.</summary>
    public IReadOnlyList<NpcEntity> AllEnemyNpcs { get; }

    // ── Action mode ─────────────────────────────────────────────────
    private bool _isMoveMode = true;
    private int _selectedSkillIndex = -1;
    private HashSet<(int X, int Y)>? _highlightCells;
    private bool _isAttackHighlight;
    private HashSet<(int X, int Y)>? _hoverSkillCells; // hover-preview blink on map

    // ── UI state ────────────────────────────────────────────────────
    private int _actionLogScrollOffset;
    private IReadOnlyList<FightingSkill> _currentUnlockedSkills = Array.Empty<FightingSkill>();
    private IReadOnlyList<FightingSkill> _currentLearnableSkills = Array.Empty<FightingSkill>();
    private int _selectedLearnableSkillIndex = -1;
    private string? _expandedMediumKey;
    private IReadOnlyList<LeftPanelRow> _leftPanelLayout = Array.Empty<LeftPanelRow>();
    private IReadOnlyList<(int Y, Fighter Fighter)> _rightPanelRows = Array.Empty<(int, Fighter)>();
    private Fighter? _topPanelFighter;
    private IReadOnlyList<string>? _bodyPartMenu;
    private bool _continueHovered;
    private Fighter? _hoveredFighter;
    private int _hoveredButtonRow = -1;
    private List<(int X, int Y)>? _previewPath;

    // ── Blink ───────────────────────────────────────────────────────
    private double _blinkTimer;
    private bool _blinkOn = true;

    // ── AI delay ────────────────────────────────────────────────────
    private int _aiDelayFrames;
    private const int AiDelay = 15;

    // ── Movement animation ──────────────────────────────────────────
    private int _movementFrameTimer;
    private const int PlayerMoveFramesPerTile = 3;
    private const int AiMoveFramesPerTile = 1;

    // ── Dice timing ─────────────────────────────────────────────────
    private const float DiceRollDuration = 0.6f;
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
        Protagonist protagonist,
        IReadOnlyList<NpcEntity>? allies = null)
    {
        _terminal = terminal;
        _ = popup; // unused (popups removed; the param is kept for caller compatibility)
        _targetNpc = targetNpc;
        _allies = allies ?? Array.Empty<NpcEntity>();

        // Build AllEnemyNpcs: main target + all allies
        var allEnemies = new List<NpcEntity> { targetNpc };
        allEnemies.AddRange(_allies);
        AllEnemyNpcs = allEnemies;

        _skillRegistry = FightingSkillRegistry.Instance;

        // Generate arena
        var generator = new ArenaGenerator { Seed = Environment.TickCount };
        var area = generator.Generate();

        // Build fighters
        var fighters = BuildFighters(protagonist, targetNpc, _allies);

        // Roll initiative
        foreach (var f in fighters)
            f.InitiativeRoll = _rng.Next(1, 7) + f.InitiativeValue;

        fighters.Sort((a, b) =>
        {
            int cmp = b.InitiativeRoll.CompareTo(a.InitiativeRoll);
            return cmp != 0 ? cmp : (a.Faction == FighterFaction.Party ? -1 : 1);
        });

        _state = new FightState(area, fighters);
        _state.AddLog("Fight begins!", LogEntryType.Normal);

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

    private static List<Fighter> BuildFighters(Protagonist protagonist, NpcEntity npc, IReadOnlyList<NpcEntity> allies)
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

        // Main enemy NPC
        var enemyFighter = new Fighter(npc.Combatant,
            FightArea.ZoneColStart + 2, FightArea.EnemyRowStart + 1,
            isPlayerControlled: false, FighterFaction.Enemy);
        fighters.Add(enemyFighter);

        // Ally NPCs — spread horizontally in the top half (rows 0–29, near EnemyRowStart)
        int allyOffset = 0;
        foreach (var ally in allies)
        {
            allyOffset++;
            int allyCol = FightArea.ZoneColStart + 2 + allyOffset * 3;
            int allyRow = Math.Max(1, FightArea.EnemyRowStart - 3);  // above the main enemy zone
            var allyFighter = new Fighter(ally.Combatant,
                allyCol, allyRow,
                isPlayerControlled: false, FighterFaction.Enemy);
            fighters.Add(allyFighter);
        }

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

                    // Auto-end turn if the player has no CP left to spend
                    if (mover.IsPlayerControlled && mover.CurrentCineticPoints <= 0)
                    {
                        _state.AddLog($"{mover.DisplayName} has no Cinetic Points left — turn ends.");
                        EndTurn(mover);
                    }
                }
            }
            FullRedraw();
            return;
        }

        // ── Blink ─────────────────────────────────────────────────
        _blinkTimer += deltaTime;
        bool newBlink = (_blinkTimer % 0.06) < 0.03;
        if (newBlink != _blinkOn)
        {
            _blinkOn = newBlink;
            // FullRedraw is called at the end of Update() every frame anyway
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
            {
                if (_state.PendingLearnSkill != null && _state.PendingSkill == null)
                    FinishLearningRoll(active);
                else
                    FinishAttackResolution(active);
            }
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
                if (active.X == FightArea.ExitCol && active.Y == FightArea.ExitRow)
                    ExecuteAction(new Actions.RunawayAction(active));
                else
                    _state.AddLog("Must reach the exit tile (⎆) to run away.");
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
                        if (row.SkillIndex >= 0 && row.SkillIndex < _currentUnlockedSkills.Count
                            && _currentUnlockedSkills[row.SkillIndex].IsSelfTargeting)
                            ExecuteAction(new Actions.SkillAction(active, active, _currentUnlockedSkills[row.SkillIndex]));
                        else
                            SetSkillMode(row.SkillIndex);
                        break;
                    case LeftPanelRowKind.LearnableSkill:
                        SetLearnableSkillMode(row.SkillIndex);
                        break;
                }
                return;
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
            if (skill.IsSelfTargeting)
            {
                ExecuteAction(new Actions.SkillAction(active, active, skill));
            }
            else
            {
                if (_highlightCells != null && !_highlightCells.Contains((ax, ay))) return;
                var target = _state.Fighters.FirstOrDefault(
                    f => f.IsAlive && f.Faction != active.Faction &&
                         f.X == ax && f.Y == ay);
                if (target != null)
                    TryUseSkillOnTarget(active, target, skill);
                else
                {
                    int cost = skill.CineticPointsCost;
                    active.CurrentCineticPoints = Math.Max(0, active.CurrentCineticPoints - cost);
                    _state.AddLog($"{active.DisplayName} uses {skill.DisplayName} — nothing there.  [-{cost} CP]", LogEntryType.Miss);
                    _state.Phase = TurnPhase.TurnEnding;
                    AfterActionUpdate();
                }
            }
        }
        else if (_selectedLearnableSkillIndex >= 0 && _selectedLearnableSkillIndex < _currentLearnableSkills.Count)
        {
            var skill = _currentLearnableSkills[_selectedLearnableSkillIndex];
            // For DefensePosture-type learnable skills, learn without targeting
            if (skill.EffectType == FightingSkillEffect.DefensePosture)
            {
                StartLearningAttempt(active, null, skill);
            }
            else
            {
                var target = _state.Fighters.FirstOrDefault(
                    f => f.IsAlive && f.Faction != active.Faction &&
                         f.X == ax && f.Y == ay);
                if (target != null)
                    StartLearningAttempt(active, target, skill);
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
            if (y == FightModeUI.MoveButtonRow
                || y == FightModeUI.EndTurnButtonRow
                || y == FightModeUI.RunButtonRow
                || _leftPanelLayout.Any(r => r.Y == y))
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
        else if (x >= 80 && y >= 40)
        {
            // Right-bottom initiative list — hover a fighter row
            var hit = _rightPanelRows.FirstOrDefault(r => r.Y == y);
            if (hit.Fighter != null) newFighter = hit.Fighter;
        }

        _hoveredFighter   = newFighter;
        _hoveredButtonRow = newButton;
        _previewPath      = newPath;
        // _hoverSkillCells is recomputed each frame inside FullRedraw after _leftPanelLayout is fresh

        // Detail panel follows the hovered fighter; null falls back to the active fighter in FullRedraw
        _topPanelFighter = newFighter;
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
        _selectedLearnableSkillIndex = -1;
        RecomputeHighlight();
    }

    private void SetSkillMode(int skillIndex)
    {
        _isMoveMode = false;
        _selectedSkillIndex = skillIndex;
        _selectedLearnableSkillIndex = -1;
        RecomputeHighlight();
    }

    private void SetLearnableSkillMode(int learnIndex)
    {
        _isMoveMode = false;
        _selectedSkillIndex = -1;
        _selectedLearnableSkillIndex = learnIndex;
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
        else if (_selectedLearnableSkillIndex >= 0 && _selectedLearnableSkillIndex < _currentLearnableSkills.Count)
        {
            // Learnable skill: highlight the same targets as the skill would require (for starting the attempt)
            _isAttackHighlight = true;
            var skill = _currentLearnableSkills[_selectedLearnableSkillIndex];
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
            if (r.Kind == LeftPanelRowKind.LearnableSkill
                && r.SkillIndex >= 0 && r.SkillIndex < _currentLearnableSkills.Count)
                return ComputeSkillTargetCells(active, _currentLearnableSkills[r.SkillIndex]);
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
        if (skill.IsSelfTargeting)
        {
            result.Add((attacker.X, attacker.Y));
            return result;
        }
        // Include every cell within Manhattan range + LOS (not just enemy positions).
        int range = skill.Range;
        for (int dy = -range; dy <= range; dy++)
        for (int dx = -range; dx <= range; dx++)
        {
            if (Math.Abs(dx) + Math.Abs(dy) > range) continue;
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
        if (_state.PendingLearnSkill != null && _state.PendingSkill == null)
        {
            var skill = _state.PendingLearnSkill;
            var accent = new OpenTK.Mathematics.Vector4(0.0f, 0.9f, 1.0f, 1.0f); // cyan — matches LogEntryType.Learning
            string subtitle = $"LEARNING CHECK — {skill.RequiredModusMentisId} (cerebellum)";
            _dice.Start(_state.DiceNumberOfDice, _state.DiceDifficulty,
                subtitle: subtitle, difficultyVerb: "to learn", accentColor: accent);
        }
        else
        {
            _dice.Start(_state.DiceNumberOfDice, _state.DiceDifficulty);
        }
    }

    /// <summary>
    /// Initiates a skill-learning dice roll.
    /// Sets up PendingLearnSkill, clears PendingSkill, kicks off the dice animation.
    /// </summary>
    private void StartLearningAttempt(Fighter attacker, Fighter? target, FightingSkill skill)
    {
        _state.PendingLearnSkill = skill;
        _state.PendingSkill = null;  // signals "this is a learning roll, not an attack roll"
        _state.PendingTarget = target;
        _state.LearningDiceCount = Math.Max(1, attacker.FightLearningStat);
        _state.LearningDifficulty = Math.Max(0, skill.MediumPosition - 1);
        _state.DiceNumberOfDice = _state.LearningDiceCount;
        _state.DiceDifficulty = _state.LearningDifficulty;
        _state.Phase = TurnPhase.AnimatingDice;
        _state.AddLog(
            $"{attacker.DisplayName} attempts to learn '{skill.RequiredModusMentisId}' " +
            $"(cerebellum {_state.LearningDiceCount}d, need {_state.LearningDifficulty} sixes).",
            LogEntryType.Learning);
        BeginDiceRoll();
        _highlightCells = null;
        _selectedLearnableSkillIndex = -1;
    }

    /// <summary>
    /// Resolves the dice result of a skill-learning attempt.
    /// On success: adds the skill's ModusMentis to the fighter and refreshes the skill list.
    /// On failure: ends the turn.
    /// </summary>
    private void FinishLearningRoll(Fighter active)
    {
        _dice.Hide();
        var skill = _state.PendingLearnSkill!;
        var diceValues = _state.DiceFinalValues ?? Array.Empty<int>();
        int difficulty = _state.LearningDifficulty;

        var result = FightResolver.AttemptSkillLearning(active, difficulty, diceValues);

        if (result.Success)
        {
            var template = ModusMentisRegistry.Instance.GetModusMentis(skill.RequiredModusMentisId);
            if (template != null && !active.Member.LearnedModiMentis.Any(m => m.ModusMentisId == template.ModusMentisId))
            {
                var instance = (ModusMentis)Activator.CreateInstance(template.GetType())!;
                instance.Level = 1;
                active.Member.AcquireModusMentis(instance);
            }

            _state.AddLog(
                $"LEARNED {skill.DisplayName}! ({result.SixesCount}/{result.DiceValues.Length} sixes vs diff {result.Difficulty})",
                LogEntryType.Learning);
            _state.PendingLearnSkill = null;
            RefreshSkillList();
        }
        else
        {
            _state.AddLog(
                $"Failed to learn {skill.DisplayName}. ({result.SixesCount}/{result.DiceValues.Length} sixes vs diff {result.Difficulty})",
                LogEntryType.Learning);
            _state.PendingLearnSkill = null;
            EndTurn(active);
        }
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
            _state.AddLog($"HIT! {result.Wound.WoundName} on {_state.PendingTarget.DisplayName}. ({result.SixesCount} sixes vs DEF {result.NaturalDefense})", LogEntryType.Wound);
        }
        else
        {
            _state.AddLog($"MISS. ({result.SixesCount} sixes vs DEF {result.NaturalDefense})", LogEntryType.Miss);
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

    /// <summary>
    /// Decide what the left-bottom info box should describe: the hovered action
    /// if the mouse is over one, otherwise the currently selected action.
    /// </summary>
    private (FightModeUI.LeftInfoKind, FightingSkill?) ResolveLeftInfo(Fighter active)
    {
        if (!active.IsPlayerControlled) return (FightModeUI.LeftInfoKind.None, null);

        // 1. Hovered button (priority)
        if (_hoveredButtonRow == FightModeUI.MoveButtonRow)
            return (FightModeUI.LeftInfoKind.Move, null);
        if (_hoveredButtonRow == FightModeUI.EndTurnButtonRow)
            return (FightModeUI.LeftInfoKind.EndTurn, null);
        if (_hoveredButtonRow == FightModeUI.RunButtonRow)
            return (FightModeUI.LeftInfoKind.Run, null);

        if (_hoveredButtonRow >= 0)
        {
            foreach (var r in _leftPanelLayout)
            {
                if (r.Y != _hoveredButtonRow) continue;
                if (r.Kind == LeftPanelRowKind.UnlockedSkill
                    && r.SkillIndex >= 0 && r.SkillIndex < _currentUnlockedSkills.Count)
                    return (FightModeUI.LeftInfoKind.Skill, _currentUnlockedSkills[r.SkillIndex]);
                if (r.Kind == LeftPanelRowKind.LearnableSkill
                    && r.SkillIndex >= 0 && r.SkillIndex < _currentLearnableSkills.Count)
                    return (FightModeUI.LeftInfoKind.LearnableSkill, _currentLearnableSkills[r.SkillIndex]);
                break;
            }
        }

        // 2. Selected action (fallback)
        if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _currentUnlockedSkills.Count)
            return (FightModeUI.LeftInfoKind.Skill, _currentUnlockedSkills[_selectedSkillIndex]);
        if (_selectedLearnableSkillIndex >= 0 && _selectedLearnableSkillIndex < _currentLearnableSkills.Count)
            return (FightModeUI.LeftInfoKind.LearnableSkill, _currentLearnableSkills[_selectedLearnableSkillIndex]);
        if (_isMoveMode)
            return (FightModeUI.LeftInfoKind.Move, null);

        return (FightModeUI.LeftInfoKind.None, null);
    }

    private void FullRedraw()
    {
        var active = _state.ActiveFighter;

        // Top panel: hovered fighter if any, otherwise the active fighter
        var detailFighter = _topPanelFighter ?? active;
        FightModeUI.RenderDetailPanel(_terminal, detailFighter,
            isHoverOverride: _topPanelFighter != null && _topPanelFighter != active);

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
                _leftPanelLayout = FightModeUI.RenderLeftPanel(_terminal, active,
                    _currentUnlockedSkills, _currentLearnableSkills,
                    isMove, _selectedSkillIndex, _selectedLearnableSkillIndex,
                    _expandedMediumKey, _hoveredButtonRow);

                // Recompute hover-blink cells now that the layout is current
                _hoverSkillCells = ComputeHoverSkillCells(_hoveredButtonRow, active);

                // Bottom-half info panel — hovered action > selected action > none
                var (infoKind, infoSkill) = ResolveLeftInfo(active);
                FightModeUI.RenderLeftInfoPanel(_terminal, infoKind, infoSkill, active);
            }
        }

        FightModeUI.RenderCenterPanel(_terminal, _state.Area, _state.Fighters,
            active, _blinkOn, _highlightCells, _isAttackHighlight, _previewPath, _hoverSkillCells);

        int initHoverY = _hoveredFighter != null
            ? _rightPanelRows.FirstOrDefault(r => r.Fighter == _hoveredFighter).Y
            : -1;
        _rightPanelRows = FightModeUI.RenderRightPanel(_terminal, _state.Area, _state, initHoverY);
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
        _currentLearnableSkills = active != null
            ? active.GetLearnableSkills(_skillRegistry).ToList()
            : new List<FightingSkill>();

        _isMoveMode = true;
        _selectedSkillIndex = -1;
        _selectedLearnableSkillIndex = -1;
        _expandedMediumKey = null;
        RecomputeHighlight();
        _actionLogScrollOffset = 0;
        _previewPath = null;
        _hoverSkillCells = null;
        _hoveredButtonRow = -1;
        _topPanelFighter = null;
    }

    private int[] GenerateDiceValues(int count)
    {
        var vals = new int[count];
        for (int i = 0; i < count; i++)
            vals[i] = _rng.Next(1, 7);
        return vals;
    }
}
