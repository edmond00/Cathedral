using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using Cathedral.Audio;
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
    private string? _selectedMediumKey;
    // Medium key captured when a learnable skill is attempted, so the auto-performed action
    // after a successful learn uses the same organ part the player picked.
    private string? _pendingLearnMediumKey;
    private HashSet<(int X, int Y)>? _highlightCells;
    private bool _isAttackHighlight;
    private HashSet<(int X, int Y)>? _hoverSkillCells; // hover-preview blink on map

    // ── UI state ────────────────────────────────────────────────────
    private int _actionLogScrollOffset;
    private int _actionMenuScrollOffset;          // vertical scroll of the top-left action menu
    private int _actionMenuMaxScroll;             // max scroll for the action menu (set each redraw)
    private bool _draggingMenuScrollbar;          // true while the user drags the action-menu scrollbar
    private int _hoverX = -1, _hoverY = -1;       // last hovered terminal cell (for wheel routing)
    private IReadOnlyList<FightingSkill> _currentUnlockedSkills    = Array.Empty<FightingSkill>();
    private IReadOnlyList<FightingSkill> _currentLearnableSkills   = Array.Empty<FightingSkill>();
    private IReadOnlyList<FightingSkill> _currentUnaffordableSkills = Array.Empty<FightingSkill>();
    private int _selectedLearnableSkillIndex = -1;
    private string? _expandedMediumKey;
    private IReadOnlyList<LeftPanelRow> _leftPanelLayout = Array.Empty<LeftPanelRow>();
    private IReadOnlyList<(int Y, Fighter Fighter)> _rightPanelRows = Array.Empty<(int, Fighter)>();
    private Fighter? _topPanelFighter;
    private int _hoveredStateY = -1;
    private IReadOnlyList<(int Y, FightStatusEffect Effect)> _stateRows = Array.Empty<(int, FightStatusEffect)>();
    private string? _terrainInterruptMsg;
    private Fighter? _terrainInterruptMover;
    // Bleed turn-start popup state
    private bool _bleedPopupActive;
    private Fighter? _bleedPopupFighter;
    private int _bleedPopupLevel;
    private int _bleedPopupHumors;
    private int _bleedPopupVH;
    private bool _bleedPopupCollapsed;
    private FightLocalizationOverlay? _localizationOverlay;
    private bool _continueHovered;
    private Fighter? _hoveredFighter;
    private int _hoveredButtonRow = -1;
    private List<(int X, int Y)>? _previewPath;
    private (int X, int Y)? _previewAttackCell;

    // ── Sound effects ───────────────────────────────────────────────
    private readonly Action<GameEventType>? _sfx;
    private readonly Action<MusicFilter>? _setMusicFilter;
    private object? _lastHoverKey;

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
        IFightAreaGenerator arenaGenerator,
        IReadOnlyList<NpcEntity>? allies = null,
        Action<GameEventType>? sfxTrigger = null,
        Action<MusicFilter>? setMusicFilter = null)
    {
        _terminal = terminal;
        _ = popup; // unused (popups removed; the param is kept for caller compatibility)
        _targetNpc = targetNpc;
        _allies = allies ?? Array.Empty<NpcEntity>();
        _sfx = sfxTrigger;
        _setMusicFilter = setMusicFilter;
        _dice.OnDiceTick = () => _sfx?.Invoke(GameEventType.SmallInteraction);

        // Build AllEnemyNpcs: main target + all allies
        var allEnemies = new List<NpcEntity> { targetNpc };
        allEnemies.AddRange(_allies);
        AllEnemyNpcs = allEnemies;

        _skillRegistry = FightingSkillRegistry.Instance;

        // Generate arena (caller supplies the generator, already seeded)
        var area = arenaGenerator.Generate();

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

        // Add companions as party fighters. Like the protagonist, they are player-controlled:
        // the player takes each companion's turn when it comes up in the initiative order.
        int companionOffset = 0;
        foreach (var companion in protagonist.CompanionParty)
        {
            companionOffset++;
            var cf = new Fighter(companion,
                FightArea.ZoneColStart + 2 + companionOffset * 2, FightArea.PlayerRowStart + 1,
                isPlayerControlled: true, FighterFaction.Party);
            fighters.Add(cf);
        }

        // Main enemy NPC
        var enemyFighter = new Fighter(npc.Combatant,
            FightArea.ZoneColStart + 2, FightArea.EnemyRowStart + 1,
            isPlayerControlled: false, FighterFaction.Enemy);
        enemyFighter.Personality = ResolvePersonality(npc);
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
            allyFighter.Personality = ResolvePersonality(ally);
            fighters.Add(allyFighter);
        }

        return fighters;
    }

    /// <summary>
    /// Resolve the combat personality for an enemy NPC: an archetype override wins,
    /// otherwise the personality is derived from its IsBrave / AuthorityLevel flags.
    /// </summary>
    private static AiPersonality ResolvePersonality(NpcEntity npc) =>
        npc.Archetype.AiPersonalityOverride
        ?? AiPersonality.FromArchetypeFlags(npc.IsBrave, npc.AuthorityLevel);

    /// <summary>
    /// Called every frame. Pass the frame delta time for animations.
    /// </summary>
    public void Update(double deltaTime)
    {
        _lastDeltaTime = deltaTime;

        // End a scrollbar drag once the mouse button is released.
        if (_draggingMenuScrollbar && !_terminal.IsLeftMouseDown)
            _draggingMenuScrollbar = false;

        // Music filter: DiceRoll while the dice overlay is visible, otherwise Fighting.
        // SetFilter no-ops when the requested filter matches the active one, so calling
        // it every frame is safe.
        if (_setMusicFilter != null && !_state.IsOver)
            _setMusicFilter(_dice.IsVisible ? MusicFilter.DiceRoll : MusicFilter.Fighting);

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
            _sfx?.Invoke(Result switch
            {
                FightAdapterResult.Victory => GameEventType.PositiveOutcome,
                FightAdapterResult.Death   => GameEventType.NegativeOutcome,
                FightAdapterResult.Runaway => GameEventType.NeutralOutcome,
                _                          => GameEventType.NeutralOutcome,
            });
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

                    // Tick on every single-tile step (party or enemy)
                    _sfx?.Invoke(GameEventType.SmallInteraction);

                    // Terrain slip check on the newly-entered cell
                    CheckTerrainInterrupt(_state.MovingFighter, nx, ny);
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
        // Skip the in-arena exit blink while the localization overlay is up so the
        // SetCell doesn't punch through the body art.
        _blinkTimer += deltaTime;
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
            _diceElapsed += deltaTime;
            if (_diceElapsed >= DiceRollDuration)
            {
                var finalValues = GenerateDiceValues(_state.DiceNumberOfDice);
                _state.DiceFinalValues = finalValues;
                if (_dice.IsDual)
                {
                    var defenseValues = GenerateDiceValues(_state.DiceSecondaryNumberOfDice);
                    _state.DiceSecondaryFinalValues = defenseValues;
                    // Enemy attack: display is swapped (defense=primary), so pass defense first.
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

        // Terrain-interrupt popup is modal — any click dismisses it and ends the turn.
        if (_terrainInterruptMsg != null)
        {
            var mover = _terrainInterruptMover;
            _terrainInterruptMsg = null;
            _terrainInterruptMover = null;
            if (mover != null) EndTurn(mover);
            return;
        }

        // Bleeding turn-start popup is modal — any click dismisses it; turn continues.
        if (_bleedPopupActive)
        {
            _bleedPopupActive = false;
            _bleedPopupFighter = null;
            return;
        }

        var active = _state.ActiveFighter;
        if (active == null) return;

        // ── Continue dice result ─────────────────────────────────
        if (_state.Phase == TurnPhase.WaitingForDiceComplete)
        {
            var region = _dice.ContinueButtonRegion;
            if (y == region.Y && x >= region.X && x < region.X + region.Width)
            {
                if (_state.PendingKnockdownRecovery)
                    FinishKnockdownRecoveryRoll(active);
                else if (_state.PendingRunaway)
                    FinishRunawayRoll(active);
                else if (_state.PendingLearnSkill != null && _state.PendingSkill == null)
                    FinishLearningRoll(active);
                else
                    FinishAttackResolution(active);
            }
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
                        var skill = _currentUnlockedSkills[row.SkillIndex];
                        if (_state.UsedActionsThisTurn.Contains((row.MediumKey, skill.SkillId))) break;
                        if (skill.IsSelfTargeting)
                        {
                            _state.UsedActionsThisTurn.Add((row.MediumKey, skill.SkillId));
                            ExecuteAction(new Actions.SkillAction(active, active, skill,
                                FightModeUI.OrganPartIdFromKey(row.MediumKey)));
                        }
                        else
                        {
                            SetSkillMode(row.SkillIndex, row.MediumKey);
                        }
                        break;
                    }
                    case LeftPanelRowKind.LearnableSkill:
                    {
                        if (row.SkillIndex < 0 || row.SkillIndex >= _currentLearnableSkills.Count) break;
                        var skill = _currentLearnableSkills[row.SkillIndex];
                        if (_state.UsedActionsThisTurn.Contains((row.MediumKey, skill.SkillId))) break;
                        SetLearnableSkillMode(row.SkillIndex, row.MediumKey);
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
                    FightModeUI.OrganPartIdFromKey(mediumKey)));
            }
            else
            {
                if (_highlightCells != null && !_highlightCells.Contains((ax, ay))) return;
                var target = _state.Fighters.FirstOrDefault(
                    f => f.IsAlive && f.Faction != active.Faction &&
                         f.X == ax && f.Y == ay);
                if (target != null)
                {
                    _state.UsedActionsThisTurn.Add((mediumKey, skill.SkillId));
                    TryUseSkillOnTarget(active, target, skill, mediumKey);
                }
                else
                {
                    int cost = skill.CineticPointsCost;
                    active.CurrentCineticPoints = Math.Max(0, active.CurrentCineticPoints - cost);
                    _state.UsedActionsThisTurn.Add((mediumKey, skill.SkillId));
                    _state.AddLog($"{active.DisplayName} uses {skill.DisplayName} — nothing there.  [-{cost} CP]", LogEntryType.Miss);
                    ContinueTurnOrEnd(active);
                }
            }
        }
        else if (_selectedLearnableSkillIndex >= 0 && _selectedLearnableSkillIndex < _currentLearnableSkills.Count)
        {
            var skill = _currentLearnableSkills[_selectedLearnableSkillIndex];
            string mediumKey = _selectedMediumKey ?? DefaultMediumKeyFor(skill);
            _pendingLearnMediumKey = mediumKey;
            // For DefensePosture-type learnable skills, learn without targeting
            if (skill.EffectType == FightingSkillEffect.DefensePosture)
            {
                _state.UsedActionsThisTurn.Add((mediumKey, skill.SkillId));
                StartLearningAttempt(active, null, skill);
            }
            else
            {
                if (_highlightCells != null && !_highlightCells.Contains((ax, ay))) return;
                var target = _state.Fighters.FirstOrDefault(
                    f => f.IsAlive && f.Faction != active.Faction &&
                         f.X == ax && f.Y == ay);
                if (target != null)
                {
                    _state.UsedActionsThisTurn.Add((mediumKey, skill.SkillId));
                    StartLearningAttempt(active, target, skill);
                }
            }
        }
    }

    /// <summary>
    /// Default medium key for a skill — used when no UI tab supplies one (keyboard shortcuts).
    /// Mirrors how the left panel keys its tabs: per-part organs resolve to the first part,
    /// all other organs to the whole-organ key.
    /// </summary>
    private string DefaultMediumKeyFor(FightingSkill s)
    {
        if (s.Medium.Type == MediumType.OrganMedium)
        {
            string organId = s.Medium.OrganId ?? s.SkillId;
            var organ = _state.ActiveFighter?.Member.GetOrganById(organId);
            if (organ != null && organ.PartsAreIndependentMediums && organ.Parts.Count > 1)
                return FightModeUI.OrganPartKey(organ.Parts[0].Id);
            return FightModeUI.OrganKey(organId);
        }
        if (s.Medium.Type == MediumType.BodyPartMedium)
            return FightModeUI.BodyPartKey(s.Medium.BodyPartId ?? s.SkillId);
        return $"mm:{s.RequiredModusMentisId}";
    }

    /// <summary>Called by the game loop when a terminal cell is hovered.</summary>
    public void OnCellHovered(int x, int y)
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
        _previewAttackCell = null;

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
                        double step = FightResolver.MovementStepCost(_state.Area, px, py, nx, ny);
                        if (acc + step > budget + 1e-9) break;
                        acc += step; affordable++; px = nx; py = ny;
                    }
                    if (affordable > 0)
                        newPath = path.Take(affordable).ToList();
                }
            }
            // Attack preview: in skill mode, hovering a valid target inverts that cell's colors
            if (canInteract && !_isMoveMode && _highlightCells?.Contains((ax, ay)) == true)
                _previewAttackCell = (ax, ay);
            else
                _previewAttackCell = null;
        }
        else if (x >= 80 && y >= 40)
        {
            // Right-bottom initiative list — hover a fighter row
            var hit = _rightPanelRows.FirstOrDefault(r => r.Y == y);
            if (hit.Fighter != null) newFighter = hit.Fighter;
        }

        // Left-pan fighter detail: STATE row hover sets the description target
        if (x < 20 && y >= 20 && _stateRows.Any(r => r.Y == y))
            _hoveredStateY = y;
        else
            _hoveredStateY = -1;

        _hoveredFighter   = newFighter;
        _hoveredButtonRow = newButton;
        _previewPath      = newPath;
        // _hoverSkillCells is recomputed each frame inside FullRedraw after _leftPanelLayout is fresh

        // Detail panel follows the hovered fighter; null falls back to the active fighter in FullRedraw
        _topPanelFighter = newFighter;

        // Hover-tick: play SmallInteraction once when entering a new hoverable target.
        object? hoverKey =
            newButton >= 0                          ? (object)("btn:" + newButton) :
            _previewAttackCell.HasValue              ? (object)("atk:" + _previewAttackCell.Value) :
            (newPath != null && newPath.Count > 0)   ? (object)("mov:" + newPath[^1]) :
            newFighter;
        if (hoverKey != null && !hoverKey.Equals(_lastHoverKey))
            _sfx?.Invoke(GameEventType.SmallInteraction);
        _lastHoverKey = hoverKey;
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
                {
                    var s = _currentUnlockedSkills[i];
                    string mk = DefaultMediumKeyFor(s);
                    if (!_state.UsedActionsThisTurn.Contains((mk, s.SkillId)))
                        SetSkillMode(i, mk);
                }
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
        // When the cursor is over the top-left action menu during action selection, the wheel
        // scrolls that menu; otherwise it scrolls the action log.
        bool overActionMenu = _hoverX >= 0 && _hoverX < FightModeUI.ActionMenuRight
                           && _hoverY >= 0 && _hoverY < 20
                           && _state.Phase == TurnPhase.SelectingAction
                           && _state.ActiveFighter?.IsPlayerControlled == true;
        if (overActionMenu)
            _actionMenuScrollOffset = Math.Clamp(_actionMenuScrollOffset - (int)delta, 0, _actionMenuMaxScroll);
        else
            _actionLogScrollOffset = Math.Max(0, _actionLogScrollOffset - (int)delta);
    }

    // ── Action-menu scrollbar geometry / drag helpers ─────────────────
    /// <summary>Inner column the action-menu scrollbar occupies (matches FightModeUI layout).</summary>
    private static int ActionScrollbarX => FightModeUI.ActionMenuRight - 2;
    private static int ActionScrollbarTop => FightModeUI.SkillButtonsStart;
    private static int ActionScrollbarRows => (FightModeUI.EndTurnButtonRow - 1) - FightModeUI.SkillButtonsStart;

    /// <summary>True when (x,y) is on the action-menu scrollbar track (only when scrollable).</summary>
    private bool IsOnActionScrollbar(int x, int y) =>
        _actionMenuMaxScroll > 0
        && x == ActionScrollbarX
        && y >= ActionScrollbarTop
        && y < ActionScrollbarTop + ActionScrollbarRows;

    /// <summary>Maps a track row to a scroll offset (top of track = 0, bottom = max).</summary>
    private void SetMenuScrollFromRow(int y)
    {
        if (_actionMenuMaxScroll <= 0) return;
        int rel = Math.Clamp(y - ActionScrollbarTop, 0, ActionScrollbarRows - 1);
        double frac = ActionScrollbarRows > 1 ? (double)rel / (ActionScrollbarRows - 1) : 0;
        _actionMenuScrollOffset = Math.Clamp((int)Math.Round(frac * _actionMenuMaxScroll), 0, _actionMenuMaxScroll);
    }

    // ── Action mode switching ─────────────────────────────────────────

    private void SetMoveMode()
    {
        _isMoveMode = true;
        _selectedSkillIndex = -1;
        _selectedLearnableSkillIndex = -1;
        _selectedMediumKey = null;
        RecomputeHighlight();
    }

    private void SetSkillMode(int skillIndex, string? mediumKey = null)
    {
        _isMoveMode = false;
        _selectedSkillIndex = skillIndex;
        _selectedLearnableSkillIndex = -1;
        _selectedMediumKey = mediumKey;
        RecomputeHighlight();
    }

    private void SetLearnableSkillMode(int learnIndex, string? mediumKey = null)
    {
        _isMoveMode = false;
        _selectedSkillIndex = -1;
        _selectedLearnableSkillIndex = learnIndex;
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
                double stepCost = FightResolver.MovementStepCost(_state.Area, cx, cy, nx, ny);
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

    private void TryUseSkillOnTarget(Fighter attacker, Fighter target, FightingSkill skill,
                                      string? mediumKey = null)
    {
        string? organPartId = FightModeUI.OrganPartIdFromKey(mediumKey);
        if (skill.WoundTargetMode == WoundTargetMode.PlayerChooses)
        {
            string usedKey = mediumKey ?? DefaultMediumKeyFor(skill);
            _state.PendingSkill = skill;
            _state.PendingTarget = target;
            _state.Phase = TurnPhase.WaitingForBodyPartChoice;
            _highlightCells = null;
            _localizationOverlay = new FightLocalizationOverlay(
                _terminal, target, skill.DisplayName,
                onSelected: localization =>
                {
                    _state.PendingBodyPartId = localization;
                    _localizationOverlay = null;
                    // Same path normal attacks take: deducts CP, configures dice, starts the roll.
                    ExecuteAction(new Actions.SkillAction(attacker, target, skill, organPartId));
                },
                onCancel: () =>
                {
                    _localizationOverlay = null;
                    // Cancelling frees the once-per-turn lock so the player may pick again.
                    _state.UsedActionsThisTurn.Remove((usedKey, skill.SkillId));
                    _state.PendingSkill = null;
                    _state.PendingTarget = null;
                    _state.PendingBodyPartId = null;
                    _state.Phase = TurnPhase.SelectingAction;
                    RecomputeHighlight();
                },
                sfx: _sfx);
            _localizationOverlay.Render();
            return;
        }

        _state.PendingTarget = target;
        ExecuteAction(new Actions.SkillAction(attacker, target, skill, organPartId));
    }

    private void BeginDiceRoll()
    {
        _diceElapsed = 0;

        // Install the per-roll outcome callback (fires when dice settle, before player clicks Continue).
        _dice.OnResultRevealed = MakeDiceOutcomeMapping();

        if (_state.PendingLearnSkill != null && _state.PendingSkill == null)
        {
            var skill = _state.PendingLearnSkill;
            var accent = Config.Colors.BrightPurple; // matches the learnable-skill UI palette
            string subtitle = $"LEARNING CHECK — {skill.RequiredModusMentisId} (cerebellum)";
            _dice.Start(_state.DiceNumberOfDice, _state.DiceDifficulty,
                subtitle: subtitle, difficultyVerb: "to learn", accentColor: accent);
        }
        else if (_state.PendingRunaway)
        {
            _dice.Start(_state.DiceNumberOfDice, 1,
                subtitle: "RUNAWAY CHECK — feet",
                difficultyVerb: "to flee");
        }
        else if (_state.PendingKnockdownRecovery)
        {
            _dice.Start(_state.DiceNumberOfDice, 1,
                subtitle: "KNOCKDOWN RECOVERY — heart",
                difficultyVerb: "to recover");
        }
        else if (_state.PendingSkill != null
                 && _state.PendingSkill.EffectType == FightingSkillEffect.Attack)
        {
            bool isEnemyAttack = _state.ActiveFighter?.IsPlayerControlled == false;
            if (isEnemyAttack)
            {
                // Enemy attacking: show Defense (player) as primary so the player focuses on their own roll.
                // Attack dice are secondary — more enemy sixes = bad for player.
                string subtitle = $"{_state.PendingSkill.DisplayName} → {_state.PendingTarget?.DisplayName}";
                _dice.StartDual(
                    primaryDice: _state.DiceSecondaryNumberOfDice,
                    secondaryDice: _state.DiceNumberOfDice,
                    primaryLabel: "Defense",
                    secondaryLabel: "Attack",
                    subtitle: subtitle);
            }
            else
            {
                // Player attacking: Attack is primary (player wants sixes here).
                string subtitle = $"{_state.PendingSkill.DisplayName} → {_state.PendingTarget?.DisplayName}";
                _dice.StartDual(
                    primaryDice: _state.DiceNumberOfDice,
                    secondaryDice: _state.DiceSecondaryNumberOfDice,
                    primaryLabel: "Attack",
                    secondaryLabel: "Defense",
                    subtitle: subtitle);
            }
        }
        else
        {
            _dice.Start(_state.DiceNumberOfDice, _state.DiceDifficulty);
        }

        // Neutral "box opened" cue — every dice roll greets the player with the same sound.
        _sfx?.Invoke(GameEventType.NeutralOutcome);
    }

    /// <summary>
    /// Build a player-POV outcome mapping for the in-flight dice roll based on what the
    /// pending state says we're rolling for. Returns <c>null</c> when no SFX wiring is set up.
    /// </summary>
    private Action<bool>? MakeDiceOutcomeMapping()
    {
        if (_sfx == null) return null;
        var active = _state.ActiveFighter;
        // Learning + runaway are always player-driven and self-evaluating
        if (_state.PendingLearnSkill != null && _state.PendingSkill == null)
            return isSuccess => _sfx(isSuccess ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
        if (_state.PendingRunaway)
            return isSuccess => _sfx(isSuccess ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
        // Attack: player POV depends on who's attacking
        if (_state.PendingSkill != null && _state.PendingSkill.EffectType == FightingSkillEffect.Attack && active != null)
        {
            bool partyAttacking = active.Faction == FighterFaction.Party;
            if (partyAttacking)
            {
                // Player attacks: primary=Attack, isSuccess = attack wins = good.
                return isSuccess => _sfx(isSuccess ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
            }
            else
            {
                // Enemy attacks: primary=Defense (swapped), isSuccess = defense wins = good for player.
                return isSuccess => _sfx(isSuccess ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);
            }
        }
        return null;
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
    /// <summary>
    /// Resolves a runaway dice roll. Success on at least one six.
    /// On success, sets <see cref="FightResult.PartyFled"/> so the controller exits the fight.
    /// On failure, returns to action selection (or auto-ends turn if no CP left).
    /// </summary>
    private void FinishRunawayRoll(Fighter active)
    {
        _dice.Hide();
        _sfx?.Invoke(GameEventType.NeutralOutcome); // box closes
        var diceValues = _state.DiceFinalValues ?? Array.Empty<int>();
        int sixes = diceValues.Count(v => v == 6);
        _state.PendingRunaway = false;

        if (sixes >= 1)
        {
            _state.AddLog($"{active.DisplayName} escapes the fight! ({sixes}/{diceValues.Length} sixes)");
            _state.Result = FightResult.PartyFled;
            // Game-end detection in Update() will pick up the new result on the next frame.
        }
        else
        {
            _state.AddLog($"{active.DisplayName} tries to flee but fails. ({sixes}/{diceValues.Length} sixes)", LogEntryType.Miss);
            ContinueTurnOrEnd(active);
        }
    }

    /// <summary>
    /// Resolves a knockdown-recovery dice roll for a player-controlled fighter. ≥1 six clears
    /// the knockdown so the turn can proceed; failure skips the turn (knockdown persists).
    /// </summary>
    private void FinishKnockdownRecoveryRoll(Fighter active)
    {
        _dice.Hide();
        _sfx?.Invoke(GameEventType.NeutralOutcome); // box closes
        var diceValues = _state.DiceFinalValues ?? Array.Empty<int>();
        int sixes = diceValues.Count(v => v == 6);
        _state.PendingKnockdownRecovery = false;
        _state.DiceFinalValues = null;

        if (sixes >= 1)
        {
            var knock = active.ActiveEffects.OfType<KnockdownEffect>().FirstOrDefault();
            if (knock != null) active.ActiveEffects.Remove(knock);
            active.IsKnockedDown = false;
            _state.AddLog($"{active.DisplayName} recovers from knockdown ({sixes}/{diceValues.Length} sixes).",
                LogEntryType.SpecialEffect);
            _state.Phase = TurnPhase.SelectingAction;
            RefreshSkillList();
        }
        else
        {
            _state.AddLog($"{active.DisplayName} fails to recover from knockdown ({sixes}/{diceValues.Length} sixes) — turn skipped.",
                LogEntryType.SpecialEffect);
            EndTurn(active);
        }
    }

    private void FinishLearningRoll(Fighter active)
    {
        _dice.Hide();
        _sfx?.Invoke(GameEventType.NeutralOutcome); // box closes
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

            // Now that the MM is learned, automatically perform the action the player wanted.
            // For attack skills this fires the attack dice roll; DefensePosture applies immediately.
            var learnedTarget = _state.PendingTarget;
            string? learnedMediumKey = _pendingLearnMediumKey;
            _pendingLearnMediumKey = null;
            _state.PendingTarget = null;
            _state.Phase = TurnPhase.SelectingAction;
            RefreshSkillList();

            if (skill.EffectType == FightingSkillEffect.DefensePosture)
            {
                ExecuteAction(new Actions.SkillAction(active, active, skill,
                    FightModeUI.OrganPartIdFromKey(learnedMediumKey)));
            }
            else if (learnedTarget != null && learnedTarget.IsAlive)
            {
                TryUseSkillOnTarget(active, learnedTarget, skill, learnedMediumKey);
            }
            // else: no valid target stored — return to action selection so the player can pick one
        }
        else
        {
            _state.AddLog(
                $"Failed to learn {skill.DisplayName}. ({result.SixesCount}/{result.DiceValues.Length} sixes vs diff {result.Difficulty})",
                LogEntryType.Learning);
            _state.PendingLearnSkill = null;
            // Spend the skill's CP cost and continue — player may still have CP left
            active.CurrentCineticPoints = Math.Max(0, active.CurrentCineticPoints - skill.CineticPointsCost);
            ContinueTurnOrEnd(active);
        }
    }

    private void FinishAttackResolution(Fighter active)
    {
        _dice.Hide();
        _sfx?.Invoke(GameEventType.NeutralOutcome); // box closes
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
            ContinueTurnOrEnd(active);
    }

    /// <summary>
    /// After an action completes: if the fighter still has Cinetic Points, return to action
    /// selection so they can chain another move/skill. Otherwise auto-end the turn (matches
    /// the behavior already used after movement). Used after attack resolution so a failed
    /// or low-cost skill doesn't waste the whole turn.
    /// </summary>
    private void ContinueTurnOrEnd(Fighter active)
    {
        // Clear pending action state so the next selection starts clean
        _state.PendingSkill = null;
        _state.PendingTarget = null;
        _state.PendingBodyPartId = null;
        _state.DiceFinalValues = null;
        _state.DiceSecondaryFinalValues = null;
        _state.DiceNumberOfDice = 0;
        _state.DiceSecondaryNumberOfDice = 0;

        if (active.CurrentCineticPoints <= 0)
        {
            _state.AddLog($"{active.DisplayName} has no Cinetic Points left — turn ends.");
            EndTurn(active);
            return;
        }

        _state.Phase = TurnPhase.SelectingAction;
        RefreshSkillList();
        if (!active.IsPlayerControlled)
            _aiDelayFrames = 5; // brief pause then AI continues
    }

    /// <summary>
    /// Roll an equilibrium check when the mover lands on Treacherous/Dangerous terrain.
    /// On failure: apply FallOver, optionally add a low lower_limbs wound, stop the path,
    /// and either pop the interrupt overlay (party) or end the turn directly (enemy).
    /// </summary>
    private void CheckTerrainInterrupt(Fighter mover, int x, int y)
    {
        var terrain = _state.Area.GetCell(x, y).Type;
        if (terrain != TerrainType.TreacherousTerrain && terrain != TerrainType.DangerousTerrain)
            return;

        int slipRiskPct = FightResolver.EstimateSlipRiskPct(terrain, mover.EquilibriumValue);
        if (slipRiskPct <= 0) return;
        if (_rng.Next(100) >= slipRiskPct) return; // kept footing

        // Apply fall-over status (clears at start of next turn)
        var fallOver = new FallOverEffect();
        mover.ActiveEffects.Add(fallOver);
        fallOver.OnApply(mover, mover, _state, _rng);

        string popupMsg;
        if (terrain == TerrainType.DangerousTerrain)
        {
            var lowWounds = WoundRegistry.All.Values
                .Where(w => w.Handicap == WoundHandicap.Low)
                .ToList();
            if (lowWounds.Count > 0)
            {
                var w = lowWounds[_rng.Next(lowWounds.Count)];
                FightResolver.ApplyWound(mover, w);
                _state.AddLog($"{mover.DisplayName} stumbles on dangerous ground — {w.WoundName}.", LogEntryType.Wound);
            }
            popupMsg = $"{mover.DisplayName} stumbles on dangerous ground and suffers a wound. The turn is cut short.";
        }
        else
        {
            _state.AddLog($"{mover.DisplayName} slips on treacherous ground — turn cut short.", LogEntryType.SpecialEffect);
            popupMsg = $"{mover.DisplayName} slips on treacherous ground. The turn is cut short.";
        }

        // Stop the in-flight movement
        _state.MovementPath = null;
        _state.MovingFighter = null;
        _state.MovementPathIndex = 0;
        _state.Phase = TurnPhase.SelectingAction;

        if (mover.IsPlayerControlled)
        {
            // Show popup; click anywhere → dismiss → EndTurn.
            _terrainInterruptMsg = popupMsg;
            _terrainInterruptMover = mover;
        }
        else
        {
            EndTurn(mover);
        }
    }

    private void EndTurn(Fighter active)
    {
        active.HasActedThisTurn = true;
        _state.AdvanceToNextFighter(_rng);
        RefreshSkillList();

        // Check fight end immediately (bleeding-induced collapse during the new fighter's
        // StartTurn() may have set HP to 0 — let CheckFightEnd surface it before we route input).
        _state.CheckFightEnd();

        var next = _state.ActiveFighter;
        if (next != null && !next.IsPlayerControlled)
            _aiDelayFrames = AiDelay;

        // Bleeding popup — only for party fighters who actually lost humors this turn.
        if (next != null && next.Faction == FighterFaction.Party)
        {
            var bleed = next.ActiveEffects.OfType<BleedingEffect>().FirstOrDefault();
            if (bleed != null && bleed.LastDrainedHumors > 0)
            {
                _bleedPopupActive    = true;
                _bleedPopupFighter   = next;
                _bleedPopupLevel     = bleed.Level;
                _bleedPopupHumors    = bleed.LastDrainedHumors;
                _bleedPopupVH        = bleed.LastDrainedVitalHeat;
                _bleedPopupCollapsed = bleed.LastDrainPushedToCritical;
            }
        }

        _actionLogScrollOffset = 0;

        // Knockdown recovery — runs after bleed-popup capture so its dice flow takes priority.
        MaybeStartKnockdownRecovery(next);
    }

    /// <summary>
    /// If <paramref name="fighter"/> is knocked down at turn start, roll the recovery check.
    /// Protagonist sees the dice overlay; companions and enemies roll silently. On success the
    /// knockdown clears and the turn proceeds; on failure the turn is skipped.
    /// </summary>
    private void MaybeStartKnockdownRecovery(Fighter? fighter)
    {
        if (fighter == null || !fighter.IsKnockedDown) return;
        var knock = fighter.ActiveEffects.OfType<KnockdownEffect>().FirstOrDefault();
        if (knock == null) return;

        int dice = fighter.KnockdownRecoveryDiceCount;

        // Protagonist (player-controlled): show dice overlay, wait for Continue.
        if (fighter.IsPlayerControlled)
        {
            _state.PendingKnockdownRecovery = true;
            _state.PendingSkill = null;
            _state.PendingTarget = null;
            _state.PendingLearnSkill = null;
            _state.DiceNumberOfDice = dice;
            _state.DiceDifficulty = 1;
            _state.DiceSecondaryNumberOfDice = 0;
            _state.DiceFinalValues = null;
            _state.DiceSecondaryFinalValues = null;
            _state.IsDiceRolling = true;
            _state.Phase = TurnPhase.AnimatingDice;
            BeginDiceRoll();
            return;
        }

        // Non-player (companions and enemies): roll silently, log, skip the turn on failure.
        int sixes = 0;
        for (int i = 0; i < dice; i++)
            if (_rng.Next(1, 7) == 6) sixes++;
        if (sixes >= 1)
        {
            fighter.IsKnockedDown = false;
            knock.GetType(); // (keep reference to ensure we resolve below)
            // Mark expired by removing from the list — there's no public setter, so re-create the
            // status entry via Remove. Looping over ActiveEffects is safe here since we exit after.
            fighter.ActiveEffects.Remove(knock);
            _state.AddLog($"{fighter.DisplayName} recovers from knockdown ({sixes}/{dice} sixes).", LogEntryType.SpecialEffect);
        }
        else
        {
            _state.AddLog($"{fighter.DisplayName} fails to recover from knockdown ({sixes}/{dice} sixes) — turn skipped.", LogEntryType.SpecialEffect);
            EndTurn(fighter);
        }
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

        // After an AI action completes (or when continuing the same turn), re-jitter the
        // next-action delay so brutes (low cunning) act briskly and cunning archetypes pause
        // as though deliberating. ±5 frames scaled by (1 - Cunning).
        int jitter = (int)Math.Round((_rng.NextDouble() * 10 - 5) * (1.0 - ai.Personality.Cunning));
        _aiDelayFrames = Math.Max(1, AiDelay + jitter);

        if (_state.Phase == TurnPhase.AnimatingMovement)
            return;

        if (_state.Phase == TurnPhase.AnimatingDice)
        {
            BeginDiceRoll();
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
    private (FightModeUI.LeftInfoKind Kind, FightingSkill? Skill, string? OrganPartId) ResolveLeftInfo(Fighter active)
    {
        if (!active.IsPlayerControlled) return (FightModeUI.LeftInfoKind.None, null, null);

        // 1. Hovered button (priority)
        if (_hoveredButtonRow == FightModeUI.MoveButtonRow)
            return (FightModeUI.LeftInfoKind.Move, null, null);
        if (_hoveredButtonRow == FightModeUI.EndTurnButtonRow)
            return (FightModeUI.LeftInfoKind.EndTurn, null, null);
        if (_hoveredButtonRow == FightModeUI.RunButtonRow)
            return (FightModeUI.LeftInfoKind.Run, null, null);

        if (_hoveredButtonRow >= 0)
        {
            foreach (var r in _leftPanelLayout)
            {
                if (r.Y != _hoveredButtonRow) continue;
                string? partId = FightModeUI.OrganPartIdFromKey(r.MediumKey);
                if (r.Kind == LeftPanelRowKind.UnlockedSkill
                    && r.SkillIndex >= 0 && r.SkillIndex < _currentUnlockedSkills.Count)
                    return (FightModeUI.LeftInfoKind.Skill, _currentUnlockedSkills[r.SkillIndex], partId);
                if (r.Kind == LeftPanelRowKind.LearnableSkill
                    && r.SkillIndex >= 0 && r.SkillIndex < _currentLearnableSkills.Count)
                    return (FightModeUI.LeftInfoKind.LearnableSkill, _currentLearnableSkills[r.SkillIndex], partId);
                if (r.Kind == LeftPanelRowKind.UnaffordableSkill
                    && r.SkillIndex >= 0 && r.SkillIndex < _currentUnaffordableSkills.Count)
                    return (FightModeUI.LeftInfoKind.Skill, _currentUnaffordableSkills[r.SkillIndex], partId);
                break;
            }
        }

        // 2. Selected action (fallback)
        string? selPartId = FightModeUI.OrganPartIdFromKey(_selectedMediumKey);
        if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _currentUnlockedSkills.Count)
            return (FightModeUI.LeftInfoKind.Skill, _currentUnlockedSkills[_selectedSkillIndex], selPartId);
        if (_selectedLearnableSkillIndex >= 0 && _selectedLearnableSkillIndex < _currentLearnableSkills.Count)
            return (FightModeUI.LeftInfoKind.LearnableSkill, _currentLearnableSkills[_selectedLearnableSkillIndex], selPartId);
        if (_isMoveMode)
            return (FightModeUI.LeftInfoKind.Move, null, null);

        return (FightModeUI.LeftInfoKind.None, null, null);
    }

    private void FullRedraw()
    {
        var active = _state.ActiveFighter;

        // Top panel: hovered fighter if any, otherwise the active fighter
        var detailFighter = _topPanelFighter ?? active;
        _stateRows = FightModeUI.RenderDetailPanel(_terminal, detailFighter,
            isHoverOverride: _topPanelFighter != null && _topPanelFighter != active,
            hoveredStateY: _hoveredStateY);

        if (active != null)
        {
            bool isMove = _isMoveMode || !active.IsPlayerControlled ||
                          _state.Phase == TurnPhase.AnimatingMovement;
            _leftPanelLayout = FightModeUI.RenderLeftPanel(_terminal, active,
                _currentUnlockedSkills, _currentLearnableSkills, _currentUnaffordableSkills,
                isMove, _selectedSkillIndex, _selectedLearnableSkillIndex,
                _expandedMediumKey, _hoveredButtonRow,
                _state.UsedActionsThisTurn, _state.RunUsedThisTurn,
                _actionMenuScrollOffset,
                _draggingMenuScrollbar || IsOnActionScrollbar(_hoverX, _hoverY),
                out _actionMenuMaxScroll);
            // Keep the stored offset within range as content height changes.
            _actionMenuScrollOffset = Math.Min(_actionMenuScrollOffset, _actionMenuMaxScroll);

            // Recompute hover-blink cells now that the layout is current
            _hoverSkillCells = ComputeHoverSkillCells(_hoveredButtonRow, active);

            // Bottom-half info panel — hovered action > selected action > none
            var (infoKind, infoSkill, infoPartId) = ResolveLeftInfo(active);
            FightModeUI.RenderLeftInfoPanel(_terminal, infoKind, infoSkill, active, infoPartId);
        }

        FightModeUI.RenderCenterPanel(_terminal, _state.Area, _state.Fighters,
            active, _blinkOn, _highlightCells, _isAttackHighlight, _previewPath, _hoverSkillCells,
            _previewAttackCell);

        // Localization picker is rendered via the overlay path below; nothing to do here.

        // Terrain-interrupt purple overlay — sits on top of everything until dismissed
        if (_terrainInterruptMsg != null)
            FightModeUI.RenderTerrainInterruptPopup(_terminal, _terrainInterruptMsg);

        // Bleeding turn-start popup (party only) — gates input until dismissed
        if (_bleedPopupActive && _bleedPopupFighter != null)
            FightModeUI.RenderBleedingDrainPopup(_terminal, _bleedPopupFighter.DisplayName,
                _bleedPopupLevel, _bleedPopupHumors, _bleedPopupVH, _bleedPopupCollapsed);

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
        _currentLearnableSkills = active != null
            ? active.GetLearnableSkills(_skillRegistry).ToList()
            : new List<FightingSkill>();
        _currentUnaffordableSkills = active != null
            ? active.GetUnaffordableKnownSkills(_skillRegistry).ToList()
            : new List<FightingSkill>();

        _isMoveMode = true;
        _selectedSkillIndex = -1;
        _selectedLearnableSkillIndex = -1;
        _expandedMediumKey = null;
        _actionMenuScrollOffset = 0;
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
