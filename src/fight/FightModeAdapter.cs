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
/// This is the single home for fight control flow: it operates on a provided
/// <see cref="TerminalHUD"/> rather than owning a window, so the host controller drives it
/// through <c>OnCellClicked</c>/<c>OnCellHovered</c>/<c>OnKeyPress</c>/<c>OnMouseWheel</c>.
/// </summary>
public class FightModeAdapter
{
    // ── Core objects ─────────────────────────────────────────────────
    private readonly TerminalHUD _terminal;
    // (popup terminal kept in the constructor signature for caller compatibility; no longer rendered)
    private readonly FightState _state;
    private readonly FightingSkillRegistry _skillRegistry;
    private readonly DiceRollComponent _dice = new();
    private readonly Random _rng = GameRng.Stream("fight-mode");

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
    /// <summary>
    /// Full on+off blink cycle, in real seconds. Stated as a duration rather than tuned against a
    /// tick count: the caller's delta is the controller's real update interval (~0.1 s), not a
    /// 60 FPS frame, and the old 0.06/0.03 pair only looked right because the fight was being fed
    /// a hard-coded 1/60 that had nothing to do with elapsed time.
    /// </summary>
    private const double BlinkPeriodSeconds = 0.36;

    // ── AI delay ────────────────────────────────────────────────────
    private int _aiDelayFrames;
    private const int AiDelay = 15;

    // ── Movement animation ──────────────────────────────────────────
    private int _movementFrameTimer;
    private const int PlayerMoveFramesPerTile = 3;
    private const int AiMoveFramesPerTile = 1;

    // ── Dice timing ─────────────────────────────────────────────────
    private const float DiceRollDuration = Config.Dice.AnimationDurationSeconds;
    private double _diceElapsed;

    // ── Vital-heat box timing ───────────────────────────────────────
    /// <summary>
    /// How long the buff's vital-heat consumption box stays up, in real seconds. Deliberately much
    /// shorter than a dice roll: there is no outcome to await, only a cost to witness.
    /// </summary>
    private const float VitalHeatBoxDuration = 1.6f;
    private double _vitalHeatElapsed;


    /// <summary>
    /// The result of the fight once it's over. <see cref="FightAdapterResult.Ongoing"/> while in progress.
    /// </summary>
    public FightAdapterResult Result { get; private set; } = FightAdapterResult.Ongoing;

    /// <summary>Whether the fight has ended.</summary>
    public bool IsOver => Result != FightAdapterResult.Ongoing;

    /// <summary>The NPC that was fought.</summary>
    public NpcEntity TargetNpc => _targetNpc;

    /// <summary>
    /// The turn-by-turn combat log (capped at 200 entries by <see cref="FightState.AddLog"/>).
    /// Read it before the adapter is dropped — the caller archives a tail of it into the shared
    /// narration history so the fight leaves a trace the player can scroll back to.
    /// </summary>
    public IReadOnlyList<(string Text, LogEntryType Type)> ActionLog => _state.ActionLog;

    /// <summary>
    /// Force the fight to a result without playing it out (--cli <c>fight-end</c>). Driving a whole
    /// tactical battle from a script is impractical, but the fight→narration transition still needs
    /// testing, so this jumps to the end state the transition reads.
    /// On victory every enemy NPC is marked slain, which is what makes
    /// <c>NarrativeController.OnFightCompleted</c> spawn their corpses into the scene.
    /// </summary>
    public void CliForceEnd(FightResult result)
    {
        // Marking the NpcEntity slain is what OnFightCompleted checks when spawning corpses; the
        // Fighter's own HP is a read-only projection of its member and does not need touching,
        // since the fight ends here rather than continuing to evaluate end conditions.
        if (result == FightResult.PartyWon)
            foreach (var npc in AllEnemyNpcs) npc.IsAlive = false;
        _state.AddLog($"[cli] fight force-ended: {result}");
        _state.Result = result;
    }

    public FightModeAdapter(
        TerminalHUD terminal,
        PopupTerminalHUD? popup,
        NpcEntity targetNpc,
        Protagonist protagonist,
        IFightAreaGenerator arenaGenerator,
        IReadOnlyList<NpcEntity>? allies = null,
        Action<GameEventType>? sfxTrigger = null,
        Action<MusicFilter>? setMusicFilter = null,
        bool enemyInitiative = false)
    {
        _terminal = terminal;
        _ = popup; // unused (popups removed; the param is kept for caller compatibility)
        _targetNpc = targetNpc;
        _allies = allies ?? Array.Empty<NpcEntity>();
        _sfx = sfxTrigger;
        _setMusicFilter = setMusicFilter;
        _dice.OnDiceTick = () => _sfx?.Invoke(GameEventType.SmallInteraction);
        _dice.OnButtonHover = () => _sfx?.Invoke(GameEventType.SmallInteraction);
        _dice.OnButtonClick = () => _sfx?.Invoke(GameEventType.StrongInteraction);
        _dice.OnResultChanged = success => _sfx?.Invoke(success ? GameEventType.PositiveOutcome : GameEventType.NegativeOutcome);

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

        // When the enemy has the initiative (surprise round — the fight started because an action
        // failed under threat), enemy fighters act before the party regardless of the roll.
        fighters.Sort((a, b) =>
        {
            if (enemyInitiative && a.Faction != b.Faction)
                return a.Faction == FighterFaction.Enemy ? -1 : 1;
            int cmp = b.InitiativeRoll.CompareTo(a.InitiativeRoll);
            return cmp != 0 ? cmp : (a.Faction == FighterFaction.Party ? -1 : 1);
        });

        _state = new FightState(area, fighters);
        _state.AddLog(enemyInitiative ? "The enemy strikes first!" : "Fight begins!", LogEntryType.Normal);

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
    /// Called on every controller tick. <paramref name="deltaTime"/> is elapsed REAL seconds since
    /// the last call (roughly <see cref="Config.GlyphSphere.UpdateInterval"/>), and every animation here is
    /// paced against it — so a caller that passes a made-up constant stretches or compresses the
    /// dice roll and the blink by whatever the ratio happens to be.
    /// </summary>
    public void Update(double deltaTime)
    {
        // End a scrollbar drag once the mouse button is released.
        if (_draggingMenuScrollbar && !_terminal.IsLeftMouseDown)
            _draggingMenuScrollbar = false;

        // Music filter: Fighting for the whole fight, INCLUDING while the dice tumble.
        // Filters are mutually exclusive (AmbianceEngine.SetFilter cancels the running one), so
        // asking for DiceRoll here used to tear down the entire Fighting layer — drone, saw pulse
        // and drums — for the length of every roll, then rebuild it from scratch. The dice ticks
        // are SFX and layer over the music on their own channel, which is what MusicFilter.DiceRoll
        // already promises ("the ambient music continues underneath unchanged").
        // SetFilter no-ops when the requested filter matches the active one, so calling it every
        // frame is safe.
        if (_setMusicFilter != null && !_state.IsOver)
            _setMusicFilter(MusicFilter.Fighting);

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
        bool newBlink = (_blinkTimer % BlinkPeriodSeconds) < (BlinkPeriodSeconds / 2);
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
                    var defenseValues = GenerateDefenseDiceValues(_state.DiceSecondaryNumberOfDice);
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

        // ── Vital-heat consumption box ─────────────────────────────
        // A buff's cost, played out one humor at a time. No Continue button: there is nothing to
        // decide, so it runs its length and hands the turn straight back.
        if (_state.Phase == TurnPhase.AnimatingVitalHeat)
        {
            _vitalHeatElapsed += deltaTime;
            if (_vitalHeatElapsed >= VitalHeatBoxDuration)
            {
                var payer = _state.VitalHeatFighter;
                _state.ClearVitalHeatConsumption();
                _vitalHeatElapsed = 0;
                if (payer != null) ContinueTurnOrEnd(payer);
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
        // The vital-heat box plays out on its own clock; clicking through it would leave the buff
        // applied but the turn mid-resolution.
        if (_state.Phase == TurnPhase.AnimatingVitalHeat) return;

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
                return;
            }
            // Not the Continue button — let the humor layer handle selection / die clicks.
            // Modifiers mutate the shared dice arrays in place, so resolution picks them up.
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
                // Survival Instinct lifts the once-per-turn limit on the runaway check, so a
                // failed roll can be attempted again for as long as the turn lasts.
                bool mayRetry = active.ActiveEffects.Any(e => e.AllowsRunawayRetry);
                if (_state.RunUsedThisTurn && !mayRetry) return;
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
                        if (_state.IsActionUsed(active, row.MediumKey, skill.SkillId)) break;
                        if (skill.IsSelfTargeting)
                        {
                            _state.MarkActionUsed(active, row.MediumKey, skill.SkillId);
                            // The tab's medium must be passed here too — dropping it made a
                            // multi-medium self skill compute its level off the primary medium.
                            ExecuteAction(new Actions.SkillAction(active, active, skill,
                                FightModeUI.OrganPartIdFromKey(row.MediumKey),
                                ActiveMediumFromKey(skill, row.MediumKey)));
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
                        if (_state.IsActionUsed(active, row.MediumKey, skill.SkillId)) break;
                        // A self-targeting skill has no target to pick, so the attempt starts here —
                        // exactly as for one already known. Arming targeting instead left the
                        // fighter's own tile as the only highlight, so clicking the action appeared
                        // to do nothing at all until you also clicked your own symbol.
                        if (skill.IsSelfTargeting)
                        {
                            _pendingLearnMediumKey = row.MediumKey;
                            _state.MarkActionUsed(active, row.MediumKey, skill.SkillId);
                            StartLearningAttempt(active, null, skill);
                        }
                        else
                        {
                            SetLearnableSkillMode(row.SkillIndex, row.MediumKey);
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
            if (_highlightCells != null && !_highlightCells.Contains((ax, ay))) return;
            TryMoveActiveFighter(active, ax, ay);
        }
        else if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _currentUnlockedSkills.Count)
        {
            var skill = _currentUnlockedSkills[_selectedSkillIndex];
            string mediumKey = _selectedMediumKey ?? DefaultMediumKeyFor(skill);
            if (skill.IsSelfTargeting)
            {
                _state.MarkActionUsed(active, mediumKey, skill.SkillId);
                ExecuteAction(new Actions.SkillAction(active, active, skill,
                    FightModeUI.OrganPartIdFromKey(mediumKey),
                    ActiveMediumFromKey(skill, mediumKey)));
            }
            else
            {
                if (_highlightCells != null && !_highlightCells.Contains((ax, ay))) return;
                var target = _state.Fighters.FirstOrDefault(
                    f => f.IsAlive && f.Faction != active.Faction &&
                         f.X == ax && f.Y == ay);
                if (target != null)
                {
                    _state.MarkActionUsed(active, mediumKey, skill.SkillId);
                    TryUseSkillOnTarget(active, target, skill, mediumKey);
                }
                else
                {
                    int cost = skill.CineticPointsCost;
                    active.CurrentCineticPoints = Math.Max(0, active.CurrentCineticPoints - cost);
                    _state.MarkActionUsed(active, mediumKey, skill.SkillId);
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
            // Learn a self-targeting skill without picking a target. This used to test only for
            // DefensePosture, which left every other self-targeting skill — the buffs, parry,
            // dodge — waiting for an enemy click that their own targeting rules never highlight.
            if (skill.IsSelfTargeting)
            {
                _state.MarkActionUsed(active, mediumKey, skill.SkillId);
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
                    _state.MarkActionUsed(active, mediumKey, skill.SkillId);
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

    /// <summary>
    /// Moved to <see cref="FightModeUI.ActiveMediumFromKey"/>, beside the other medium-key parsers,
    /// so the info panel resolves the medium exactly the way the roll does. Kept as a forwarder
    /// because the adapter reads far better without the prefix everywhere.
    /// </summary>
    private static FightingMedium? ActiveMediumFromKey(FightingSkill skill, string? mediumKey)
        => FightModeUI.ActiveMediumFromKey(skill, mediumKey);

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

        // ── Dice continue button + humor modifier layer ──────────────
        if (_state.Phase == TurnPhase.WaitingForDiceComplete)
        {
            var region = _dice.ContinueButtonRegion;
            _continueHovered = (y == region.Y && x >= region.X && x < region.X + region.Width);
            _dice.HandleHumorHover(x, y);
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
                    double budget = active.CurrentCineticPoints * (double)active.EffectiveMoveSpeed;
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
                    if (_state.IsActionUsed(active, mk, s.SkillId)) return;
                    // Mirror the panel path: a self-targeting skill has no target to pick, so it
                    // fires here. Arming targeting instead left the fighter's own tile as the only
                    // highlight and demanded a click on their own symbol to get anywhere.
                    if (s.IsSelfTargeting)
                    {
                        _state.MarkActionUsed(active, mk, s.SkillId);
                        ExecuteAction(new Actions.SkillAction(active, active, s,
                            FightModeUI.OrganPartIdFromKey(mk), ActiveMediumFromKey(s, mk)));
                    }
                    else
                    {
                        SetSkillMode(i, mk);
                    }
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
        // ESC is routed through TryCancelSelection by the launcher, not handled here — see that
        // method. The log is not scrollable any more (it shows the most recent lines and nothing
        // else), so PageUp/PageDown are gone with it.
    }

    /// <summary>
    /// True while the fight is advancing on its own and a CLI script should keep waiting: dice in
    /// flight, a fighter stepping along a path, the vital-heat box playing, or the AI about to act.
    ///
    /// <para>
    /// Every self-advancing phase has to be listed here. One that is missed makes <c>wait</c>
    /// return mid-animation, and the failure surfaces as an unrelated assertion further down the
    /// script — see the note in CLAUDE.md about <c>CliIsIdle</c>.
    /// </para>
    /// </summary>
    public bool CliIsBusy =>
        !_state.IsOver
        && (_state.Phase == TurnPhase.AnimatingDice
         || _state.Phase == TurnPhase.AnimatingMovement
         || _state.Phase == TurnPhase.AnimatingVitalHeat
         || _state.Phase == TurnPhase.SkillLearningRoll
         || _state.Phase == TurnPhase.TurnEnding
         // An AI fighter holding the turn is about to move without any input from us.
         || (_state.Phase == TurnPhase.SelectingAction
             && _state.ActiveFighter is { IsPlayerControlled: false }));

    /// <summary>
    /// ESC's first job: back out of whatever is armed. Returns true when something was actually
    /// cancelled, so the caller can fall through to opening the main menu when nothing was.
    /// </summary>
    public bool TryCancelSelection()
    {
        if (_state.IsOver) return false;
        // Only the action-selection phase has anything to back out of; while dice are animating or
        // a picker is up, ESC would otherwise strand the turn half-resolved.
        if (_state.Phase != TurnPhase.SelectingAction) return true;
        if (_isMoveMode && _selectedSkillIndex < 0 && _selectedLearnableSkillIndex < 0) return false;
        SetMoveMode();
        return true;
    }

    /// <summary>
    /// Called by the game loop for mouse wheel scrolling.
    /// Only the top-left action menu scrolls — the log always shows its most recent lines.
    /// </summary>
    public void OnMouseWheel(float delta)
    {
        bool overActionMenu = _hoverX >= 0 && _hoverX < FightModeUI.ActionMenuRight
                           && _hoverY >= 0 && _hoverY < 20
                           && _state.Phase == TurnPhase.SelectingAction
                           && _state.ActiveFighter?.IsPlayerControlled == true;
        if (overActionMenu)
            _actionMenuScrollOffset = Math.Clamp(_actionMenuScrollOffset - (int)delta, 0, _actionMenuMaxScroll);
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
        double budget = fighter.CurrentCineticPoints * (double)fighter.EffectiveMoveSpeed;
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
        // Every reachable cell + LOS. Range is FightResolver's call, not ours — the AI's candidate
        // filter asks the same question, and the two answering it differently is how an enemy could
        // stand diagonally adjacent to someone it was unable to attack.
        int range = skill.Range;
        for (int dy = -range; dy <= range; dy++)
        for (int dx = -range; dx <= range; dx++)
        {
            if (!FightResolver.IsInSkillRange(dx, dy, skill)) continue;
            int tx = attacker.X + dx, ty = attacker.Y + dy;
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

        double budget = fighter.CurrentCineticPoints * (double)fighter.EffectiveMoveSpeed;
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
        string? organPartId  = FightModeUI.OrganPartIdFromKey(mediumKey);
        var     activeMedium = ActiveMediumFromKey(skill, mediumKey);
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
                    ExecuteAction(new Actions.SkillAction(attacker, target, skill, organPartId, activeMedium));
                },
                onCancel: () =>
                {
                    _localizationOverlay = null;
                    _state.UnmarkActionUsed(attacker, usedKey, skill.SkillId);
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
        ExecuteAction(new Actions.SkillAction(attacker, target, skill, organPartId, activeMedium));
    }

    private void BeginDiceRoll()
    {
        _diceElapsed = 0;

        // Install the per-roll outcome callback (fires when dice settle, before player clicks Continue).
        _dice.OnResultRevealed = MakeDiceOutcomeMapping();

        // Which fighter owns the PRIMARY dice group (the only group humor may modify)?
        Fighter? primaryOwner;
        if (_state.PendingLearnSkill != null && _state.PendingSkill == null)
        {
            var skill = _state.PendingLearnSkill;
            var accent = Config.Colors.BrightPurple; // matches the learnable-skill UI palette
            string subtitle = $"LEARNING CHECK — {skill.RequiredModusMentisId} (cerebellum)";
            _dice.Start(_state.DiceNumberOfDice, _state.DiceDifficulty,
                subtitle: subtitle, difficultyVerb: "to learn", accentColor: accent);
            primaryOwner = _state.ActiveFighter;
        }
        else if (_state.PendingRunaway)
        {
            _dice.Start(_state.DiceNumberOfDice, 1,
                subtitle: "RUNAWAY CHECK — feet",
                difficultyVerb: "to flee");
            primaryOwner = _state.ActiveFighter;
        }
        else if (_state.PendingKnockdownRecovery)
        {
            _dice.Start(_state.DiceNumberOfDice, 1,
                subtitle: "KNOCKDOWN RECOVERY — heart",
                difficultyVerb: "to recover");
            primaryOwner = _state.ActiveFighter;
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
                primaryOwner = _state.PendingTarget; // the defender is the player
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
                primaryOwner = _state.ActiveFighter;
            }
        }
        else
        {
            _dice.Start(_state.DiceNumberOfDice, _state.DiceDifficulty);
            primaryOwner = _state.ActiveFighter;
        }

        // Humor modifiers are only ever available on the player's own (primary) dice.
        EnableHumorIfPlayer(primaryOwner);

        // Neutral "box opened" cue — every dice roll greets the player with the same sound.
        _sfx?.Invoke(GameEventType.NeutralOutcome);
    }

    /// <summary>
    /// Enable the humor-modifier layer for the player's primary dice group. No-op when the owner
    /// is null, AI-controlled, or has no viscera modifier budget. Enemies never get humor buttons.
    /// </summary>
    private void EnableHumorIfPlayer(Fighter? owner)
    {
        if (owner == null || !owner.IsPlayerControlled) return;
        var member = owner.Member;
        int limit = member.DerivedStats.First(s => s.Name == "humor_modifier_limit").GetValue(member);
        if (limit > 0) _dice.EnableHumorModifiers(member.HumorQueues, limit);
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
        // Difficulty comes from position in the specific medium the player accessed the skill through.
        string? organId     = FightModeUI.OrganIdFromKey(_pendingLearnMediumKey);
        string? bodyPartId  = FightModeUI.BodyPartIdFromKey(_pendingLearnMediumKey);
        int position = organId    != null ? skill.GetMediumPositionForOrganId(organId)
                     : bodyPartId != null ? skill.GetMediumPositionForBodyPartId(bodyPartId)
                     : skill.MediumPosition;
        _state.LearningDifficulty = Math.Max(0, position - 1);
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
                // Fight-learned modiMentis enter working memory (FIFO input module), not the
                // long-term procedural/sensory/semantic modules — they must be consolidated later.
                active.Member.LearnModusMentis(instance);
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

            // Self-targeting covers every buff, both guards and both postures — all of which store
            // no target, so testing only for DefensePosture dropped the rest here: the modus mentis
            // was learned and then the skill the player had actually asked for never happened.
            if (skill.IsSelfTargeting)
            {
                ExecuteAction(new Actions.SkillAction(active, active, skill,
                    FightModeUI.OrganPartIdFromKey(learnedMediumKey),
                    ActiveMediumFromKey(skill, learnedMediumKey)));
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

        // ── Self-targeted rolls never wound the roller ────────────────────────────
        // Feint is the only skill left here: it rolls (someone must be convinced) but has no one
        // to hurt. Routing it through the attack resolver would measure it against the roller's own
        // defence and, on a six, wound them — which is exactly how parry used to injure the person
        // parrying. The sixes become an effect instead.
        if (_state.PendingTarget == active)
        {
            var skill = _state.PendingSkill;
            int sixes = _state.DiceFinalValues.Count(v => v == 6);
            var rolled = skill.CreateRolledEffect(active, sixes);
            if (rolled != null)
            {
                active.ActiveEffects.Add(rolled);
                rolled.OnApply(active, active, _state, _rng);
                if (rolled.IsExpired) active.ActiveEffects.Remove(rolled);
            }
            else
            {
                _state.AddLog($"{skill.DisplayName} comes to nothing. ({sixes} sixes)", LogEntryType.Miss);
            }
            _state.CheckFightEnd();
            if (!_state.IsOver) ContinueTurnOrEnd(active);
            return;
        }

        // `state` MUST be passed: FightResolver gates the whole SpecialEffects block on it being
        // non-null, so omitting it (which a named `defenseDiceValues:` argument silently does)
        // makes bleeding, knockdown, immobilize and pushback dead code.
        var result = FightResolver.ResolveAttack(
            active, _state.PendingTarget, _state.PendingSkill,
            _state.DiceFinalValues, _state.PendingBodyPartId, _rng,
            state: _state,
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
        if (_state.IsOver) return;

        // Cold Blood on the defender: a blow turned aside breaks the attacker off outright.
        if (result.AttackerTurnEnded)
        {
            _state.AddLog($"{active.DisplayName}'s turn is broken off.", LogEntryType.SpecialEffect);
            ContinueTurnOrEnd(active, forceEnd: true);
            return;
        }

        ContinueTurnOrEnd(active);
    }

    /// <summary>
    /// After an action completes: if the fighter still has Cinetic Points, return to action
    /// selection so they can chain another move/skill. Otherwise auto-end the turn (matches
    /// the behavior already used after movement). Used after attack resolution so a failed
    /// or low-cost skill doesn't waste the whole turn.
    /// </summary>
    /// <param name="forceEnd">
    /// End the turn regardless of remaining Cinetic Points — used when something outside the
    /// fighter's control cuts them off, such as a defender's Cold Blood breaking the attack.
    /// </param>
    private void ContinueTurnOrEnd(Fighter active, bool forceEnd = false)
    {
        // Clear pending action state so the next selection starts clean
        _state.PendingSkill = null;
        _state.PendingTarget = null;
        _state.PendingBodyPartId = null;
        _state.DiceFinalValues = null;
        _state.DiceSecondaryFinalValues = null;
        _state.DiceNumberOfDice = 0;
        _state.DiceSecondaryNumberOfDice = 0;

        if (forceEnd)
        {
            EndTurn(active);
            return;
        }

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
            // The stumbler's own catalogue — a wolf grazes itself on wolf wounds.
            var lowWounds = WoundRegistry.ForAnatomy(mover.Member)
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
        // Turn-scoped buffs expire HERE, not at the departing fighter's next StartTurn — that pass
        // only comes round after everyone else has acted, which would stretch every "this turn"
        // buff across a full round.
        active.EndTurn(_state, _rng);
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
            return;
        }

        // An action that resolved without a dice roll and left the fighter free to act again — a
        // parry, a dodge — still ends the turn once there is nothing left to spend. Same rule
        // ContinueTurnOrEnd applies after a roll; without it a fighter at 0 CP would sit on a
        // turn they cannot use.
        if (_state.Phase == TurnPhase.SelectingAction
            && _state.ActiveFighter is { } active && active.CurrentCineticPoints <= 0)
        {
            _state.AddLog($"{active.DisplayName} has no Cinetic Points left — turn ends.");
            EndTurn(active);
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
    private (FightModeUI.LeftInfoKind Kind, FightingSkill? Skill, string? OrganPartId, string? ActiveOrganId, string? MediumKey) ResolveLeftInfo(Fighter active)
    {
        if (!active.IsPlayerControlled) return (FightModeUI.LeftInfoKind.None, null, null, null, null);

        if (_hoveredButtonRow == FightModeUI.MoveButtonRow)
            return (FightModeUI.LeftInfoKind.Move, null, null, null, null);
        if (_hoveredButtonRow == FightModeUI.EndTurnButtonRow)
            return (FightModeUI.LeftInfoKind.EndTurn, null, null, null, null);
        if (_hoveredButtonRow == FightModeUI.RunButtonRow)
            return (FightModeUI.LeftInfoKind.Run, null, null, null, null);

        if (_hoveredButtonRow >= 0)
        {
            foreach (var r in _leftPanelLayout)
            {
                if (r.Y != _hoveredButtonRow) continue;
                string? partId   = FightModeUI.OrganPartIdFromKey(r.MediumKey);
                string? organId  = FightModeUI.OrganIdFromKey(r.MediumKey);
                if (r.Kind == LeftPanelRowKind.UnlockedSkill
                    && r.SkillIndex >= 0 && r.SkillIndex < _currentUnlockedSkills.Count)
                    return (FightModeUI.LeftInfoKind.Skill, _currentUnlockedSkills[r.SkillIndex], partId, organId, r.MediumKey);
                if (r.Kind == LeftPanelRowKind.LearnableSkill
                    && r.SkillIndex >= 0 && r.SkillIndex < _currentLearnableSkills.Count)
                    return (FightModeUI.LeftInfoKind.LearnableSkill, _currentLearnableSkills[r.SkillIndex], partId, organId, r.MediumKey);
                if (r.Kind == LeftPanelRowKind.UnaffordableSkill
                    && r.SkillIndex >= 0 && r.SkillIndex < _currentUnaffordableSkills.Count)
                    return (FightModeUI.LeftInfoKind.Skill, _currentUnaffordableSkills[r.SkillIndex], partId, organId, r.MediumKey);
                break;
            }
        }

        string? selPartId  = FightModeUI.OrganPartIdFromKey(_selectedMediumKey);
        string? selOrganId = FightModeUI.OrganIdFromKey(_selectedMediumKey);
        if (_selectedSkillIndex >= 0 && _selectedSkillIndex < _currentUnlockedSkills.Count)
            return (FightModeUI.LeftInfoKind.Skill, _currentUnlockedSkills[_selectedSkillIndex], selPartId, selOrganId, _selectedMediumKey);
        if (_selectedLearnableSkillIndex >= 0 && _selectedLearnableSkillIndex < _currentLearnableSkills.Count)
            return (FightModeUI.LeftInfoKind.LearnableSkill, _currentLearnableSkills[_selectedLearnableSkillIndex], selPartId, selOrganId, _selectedMediumKey);
        if (_isMoveMode)
            return (FightModeUI.LeftInfoKind.Move, null, null, null, null);

        return (FightModeUI.LeftInfoKind.None, null, null, null, null);
    }

    /// <summary>Repaint the whole fight UI now — used when returning from the pause menu.</summary>
    public void Redraw() => FullRedraw();

    // ── CLI driving surface ───────────────────────────────────────────────────
    // Named handles for --cli scripts. Without these a fight can only be driven by `click cell`,
    // counting rows off a dump — which CLAUDE.md warns against precisely because it breaks on the
    // next layout change and reads as nothing at all.

    /// <summary>
    /// Every skill the active fighter can act on right now, as (display name, medium key, usable).
    /// Expanding a medium tab is not required — this lists what <see cref="CliClickSkill"/> accepts.
    /// </summary>
    public IReadOnlyList<(string Name, string MediumKey, bool Usable)> CliSkills()
    {
        var active = _state.ActiveFighter;
        if (active == null || !active.IsPlayerControlled || _state.IsOver)
            return Array.Empty<(string, string, bool)>();

        var rows = new List<(string, string, bool)>();
        // Learnable skills are listed too, marked "(learn)": attempting one is an ordinary thing a
        // player does every fight, and leaving them out of the CLI meant the whole learn-then-perform
        // path — where two bugs were hiding — could not be driven by a script at all.
        foreach (var skill in _currentUnlockedSkills)
        {
            string key = DefaultMediumKeyFor(skill);
            rows.Add((skill.DisplayName, key,
                      !_state.IsActionUsed(active, key, skill.SkillId)
                      && active.CurrentCineticPoints >= skill.CineticPointsCost));
        }
        foreach (var skill in _currentLearnableSkills)
        {
            string key = DefaultMediumKeyFor(skill);
            rows.Add((skill.DisplayName + " (learn)", key,
                      !_state.IsActionUsed(active, key, skill.SkillId)
                      && active.CurrentCineticPoints >= skill.CineticPointsCost));
        }
        return rows;
    }

    /// <summary>Fighters in initiative order, for `click fighter &lt;name&gt;`.</summary>
    public IReadOnlyList<(string Name, int X, int Y, bool Alive, bool IsEnemy)> CliFighters()
        => _state.Fighters
            .Select(f => (f.DisplayName, f.X, f.Y, f.IsAlive, f.Faction == FighterFaction.Enemy))
            .ToList();

    /// <summary>
    /// Use the named skill (case-insensitive prefix match). Self-targeting skills execute at once;
    /// targeted ones arm targeting, and the caller follows with <see cref="CliClickFighter"/>.
    /// Returns an error string, or null on success.
    /// </summary>
    public string? CliClickSkill(string name)
    {
        var active = _state.ActiveFighter;
        if (active == null || !active.IsPlayerControlled) return "not the player's turn";
        if (_state.Phase != TurnPhase.SelectingAction) return $"busy (phase={_state.Phase})";

        // `regions` prints learnables with a "(learn)" marker; accept it back so its output can be
        // pasted straight into a script.
        if (name.EndsWith("(learn)", StringComparison.OrdinalIgnoreCase))
            name = name[..^"(learn)".Length].TrimEnd();

        int idx = _currentUnlockedSkills
            .Select((s, i) => (s, i))
            .Where(t => t.s.DisplayName.StartsWith(name, StringComparison.OrdinalIgnoreCase))
            .Select(t => t.i)
            .DefaultIfEmpty(-1)
            .First();

        if (idx < 0)
        {
            // Not a known skill — try the learnable list, so `click skill <name>` drives an attempt
            // exactly as clicking the row does.
            int learnIdx = _currentLearnableSkills
                .Select((s, i) => (s, i))
                .Where(t => t.s.DisplayName.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                .Select(t => t.i)
                .DefaultIfEmpty(-1)
                .First();
            if (learnIdx < 0) return $"no usable or learnable skill matching \"{name}\"";

            var learnable = _currentLearnableSkills[learnIdx];
            string learnKey = DefaultMediumKeyFor(learnable);
            if (_state.IsActionUsed(active, learnKey, learnable.SkillId))
                return $"{learnable.DisplayName} already attempted this turn";
            if (active.CurrentCineticPoints < learnable.CineticPointsCost)
                return $"{learnable.DisplayName} costs more CP than remains";

            _pendingLearnMediumKey = learnKey;
            _state.MarkActionUsed(active, learnKey, learnable.SkillId);
            if (learnable.IsSelfTargeting) StartLearningAttempt(active, null, learnable);
            else                           SetLearnableSkillMode(learnIdx, learnKey);
            return null;
        }

        var skill = _currentUnlockedSkills[idx];
        string key = DefaultMediumKeyFor(skill);
        if (_state.IsActionUsed(active, key, skill.SkillId)) return $"{skill.DisplayName} already used this turn";
        if (active.CurrentCineticPoints < skill.CineticPointsCost) return $"{skill.DisplayName} costs more CP than remains";

        if (skill.IsSelfTargeting)
        {
            _state.MarkActionUsed(active, key, skill.SkillId);
            ExecuteAction(new Actions.SkillAction(active, active, skill,
                FightModeUI.OrganPartIdFromKey(key), ActiveMediumFromKey(skill, key)));
        }
        else
        {
            SetSkillMode(idx, key);
        }
        return null;
    }

    /// <summary>
    /// Press the settled dice box's CONTINUE. The turn does not resolve until this is pressed, so a
    /// script that skips it stalls in <see cref="TurnPhase.WaitingForDiceComplete"/> forever —
    /// which reads as "the enemy did nothing" rather than "the box is still up".
    /// </summary>
    public string? CliDiceContinue()
    {
        if (_state.Phase != TurnPhase.WaitingForDiceComplete)
            return $"no settled dice box (phase={_state.Phase})";
        var region = _dice.ContinueButtonRegion;
        if (region.Width <= 0) return "dice box has no Continue button";
        OnCellClicked(region.X, region.Y);
        return null;
    }

    /// <summary>End the active fighter's turn — the END TURN button.</summary>
    public string? CliEndTurn()
    {
        var active = _state.ActiveFighter;
        if (active == null || !active.IsPlayerControlled) return "not the player's turn";
        if (_state.Phase != TurnPhase.SelectingAction) return $"busy (phase={_state.Phase})";
        ExecuteAction(new Actions.EndTurnAction(active));
        return null;
    }

    /// <summary>Click the named fighter's map cell — the target step for a non-self skill.</summary>
    public string? CliClickFighter(string name)
    {
        var target = _state.Fighters.FirstOrDefault(
            f => f.DisplayName.StartsWith(name, StringComparison.OrdinalIgnoreCase));
        if (target == null) return $"no fighter matching \"{name}\"";
        OnCellClicked(FightModeUI.CenterX + target.X, FightModeUI.CenterY + target.Y);
        return null;
    }

    /// <summary>One-line summary of the fight for `state`.</summary>
    public string CliState()
    {
        var active = _state.ActiveFighter;
        string fx = active != null && active.ActiveEffects.Count > 0
            ? " fx=" + string.Join(",", active.ActiveEffects.Select(e => e.EffectId))
            : "";
        return $"phase={_state.Phase} active={active?.DisplayName ?? "-"} "
             + $"cp={active?.CurrentCineticPoints}/{active?.MaxCineticPoints} "
             + $"hp={active?.CurrentHp}/{active?.MaxHp}{fx}";
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
                _state,
                // Survival Instinct makes the RUN button live again after a failed check.
                _state.RunUsedThisTurn && !active.ActiveEffects.Any(e => e.AllowsRunawayRetry),
                _actionMenuScrollOffset,
                _draggingMenuScrollbar || IsOnActionScrollbar(_hoverX, _hoverY),
                out _actionMenuMaxScroll);
            // Keep the stored offset within range as content height changes.
            _actionMenuScrollOffset = Math.Min(_actionMenuScrollOffset, _actionMenuMaxScroll);

            // Recompute hover-blink cells now that the layout is current
            _hoverSkillCells = ComputeHoverSkillCells(_hoveredButtonRow, active);

            // Bottom-half info panel — hovered action > selected action > none
            var (infoKind, infoSkill, infoPartId, infoOrganId, infoMediumKey) = ResolveLeftInfo(active);
            FightModeUI.RenderLeftInfoPanel(_terminal, infoKind, infoSkill, active, infoPartId, infoOrganId, infoMediumKey);
        }

        FightModeUI.RenderCenterPanel(_terminal, _state.Area, _state.Fighters,
            active, _blinkOn, _highlightCells, _isAttackHighlight, _previewPath, _hoverSkillCells,
            _previewAttackCell, _hoveredFighter);

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
        FightModeUI.RenderBottomPanel(_terminal, _state.ActionLog);

        if (_dice.IsVisible)
            FightModeUI.RenderDiceOverlay(_terminal, _dice, _continueHovered);

        if (_state.Phase == TurnPhase.AnimatingVitalHeat && _state.VitalHeatFighter != null)
            FightModeUI.RenderVitalHeatBox(_terminal, _state.VitalHeatFighter.DisplayName,
                _state.VitalHeatSkillName, _state.VitalHeatRequired, _state.VitalHeatDrawn);

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
        _previewPath = null;
        _hoverSkillCells = null;
        _hoveredButtonRow = -1;
        _topPanelFighter = null;
    }

    /// <summary>
    /// Roll <paramref name="count"/> d6 for the ATTACK pool. Under <c>--debug</c> the preset
    /// strategy pins the result, exactly as it does for narration checks: `strategy succeed` makes
    /// every die a six, `strategy fail-dice` makes none of them one.
    ///
    /// <para>
    /// Fight dice used to ignore the strategy entirely, which made every combat consequence — a
    /// wound, a bleed, a knockdown, and therefore healing — unreachable from a script except by
    /// waiting for luck.
    /// </para>
    /// </summary>
    private int[] GenerateDiceValues(int count) => RollPool(count, forAttacker: true);

    /// <summary>
    /// Roll the DEFENCE pool. The strategy is inverted here: forcing the attack to succeed means
    /// forcing the defence to fail, or the extra sixes would cancel each other out.
    /// </summary>
    private int[] GenerateDefenseDiceValues(int count) => RollPool(count, forAttacker: false);

    private int[] RollPool(int count, bool forAttacker)
    {
        if (DebugMode.IsActive)
        {
            // A six is a success; anything else is not. 1 is used for a forced failure so the
            // rendered dice read as plainly bad rather than near-misses.
            int? forced = DebugMode.CurrentStrategy switch
            {
                DebugStrategy.Succeed      => forAttacker ? 6 : 1,
                DebugStrategy.FailDiceRoll => forAttacker ? 1 : 6,
                _                          => null,
            };
            if (forced is int v)
            {
                // `strategy succeed` must actually succeed. A fighter whose pool is empty — an
                // unlevelled organ with no contributing modus mentis — would otherwise roll no
                // dice, produce no sixes, and miss no matter what the strategy asked for, which
                // makes every downstream consequence (wounds, bleeds, healing) untestable.
                int n = forced == 6 ? Math.Max(1, count) : count;
                var pinned = new int[n];
                for (int i = 0; i < n; i++) pinned[i] = v;
                return pinned;
            }
        }

        var vals = new int[count];
        for (int i = 0; i < count; i++)
            vals[i] = _rng.Next(1, 7);
        return vals;
    }
}
