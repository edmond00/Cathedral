using System;
using System.Collections.Generic;
using System.Linq;
using OpenTK.Mathematics;
using System.Threading.Tasks;
using Cathedral.Terminal;
using Cathedral.LLM;
using Cathedral.Game.Narrative;

namespace Cathedral.Game;

/// <summary>
/// Orchestrates the Phase 6 Chain-of-Thought narrative RPG system
/// Manages observation ↁEthinking ↁEaction ↁEoutcome loop
/// </summary>
public class Phase6GameController
{
    private readonly Phase6UIRenderer _ui;
    private readonly TerminalThinkingSkillPopup _thinkingPopup;
    private readonly Avatar _avatar;
    
    // Phase controllers
    private readonly ObservationPhaseController _observationController;
    private readonly ThinkingPhaseController _thinkingController;
    private readonly ActionExecutionController _actionController;

    // State
    private NarrationNode? _currentNode;
    private NarrationState _state = new();
    private int _thinkingAttemptsRemaining = 3;
    private const int MaxThinkingAttempts = 3;
    private string? _selectedKeyword = null;
    private List<ParsedNarrativeAction>? _currentActions = null;
    private bool _isWaitingForAction = false;

    // Events
    public event Action? OnExitRequested;

    public Phase6GameController(
        Phase6UIRenderer ui,
        TerminalThinkingSkillPopup thinkingPopup,
        Avatar avatar,
        ObservationPhaseController observationController,
        ThinkingPhaseController thinkingController,
        ActionExecutionController actionController)
    {
        _ui = ui;
        _thinkingPopup = thinkingPopup;
        _avatar = avatar;
        _observationController = observationController;
        _thinkingController = thinkingController;
        _actionController = actionController;
    }

    /// <summary>
    /// Start Phase 6 experience at forest entrance
    /// </summary>
    public async Task StartAsync()
    {
        _ui.Clear();
        
        // Get entry node
        var entryNodes = NodeRegistry.GetAllNodes().Where(n => n.IsEntryNode).ToList();
        if (entryNodes.Count == 0)
        {
            Console.WriteLine("ERROR: No entry nodes found in forest!");
            OnExitRequested?.Invoke();
            return;
        }

        _currentNode = entryNodes[new Random().Next(entryNodes.Count)];
        await EnterNodeAsync(_currentNode);
    }

    /// <summary>
    /// Enter a narration node (start observation phase)
    /// </summary>
    private async Task EnterNodeAsync(NarrationNode node)
    {
        _currentNode = node;
        _thinkingAttemptsRemaining = MaxThinkingAttempts;
        _selectedKeyword = null;
        _currentActions = null;
        _isWaitingForAction = false;
        
        _ui.ScrollBuffer.Clear();
        _ui.SetKeywordsEnabled(false);

        // Run observation phase
        await RunObservationPhaseAsync();
    }

    /// <summary>
    /// Run observation phase (2-3 observation skills generate narration)
    /// </summary>
    private async Task RunObservationPhaseAsync()
    {
        if (_currentNode == null) return;

        _ui.ShowLoadingIndicator("Generating observations...");
        _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);

        try
        {
            // Use observation controller to execute phase
            var narrationBlocks = await _observationController.ExecuteObservationPhaseAsync(
                _currentNode,
                _avatar,
                skillCount: 3
            );

            // Add blocks to UI
            foreach (var block in narrationBlocks)
            {
                _ui.ScrollBuffer.AddBlock(block);
            }

            // All observations complete - enable keywords
            _ui.SetKeywordsEnabled(true);
            _ui.ScrollBuffer.ScrollToTop();
            _ui.HideLoadingIndicator();
            _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Observation phase error: {ex.Message}");
            _ui.HideLoadingIndicator();
            
            // Fallback: add generic observation
            var fallbackBlock = new NarrationBlock(
                NarrationBlockType.Observation,
                "Observation",
                _currentNode.GenerateNeutralDescription(_avatar.CurrentLocationId),
                _currentNode.Keywords,
                null
            );
            _ui.ScrollBuffer.AddBlock(fallbackBlock);
            _ui.SetKeywordsEnabled(true);
            _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);
        }
    }

    /// <summary>
    /// Handle keyword click (show thinking skill popup)
    /// </summary>
    public void OnKeywordClicked(string keyword, Vector2 mousePosition)
    {
        if (_thinkingAttemptsRemaining <= 0) return;
        if (_currentNode == null) return;

        _selectedKeyword = keyword;
        
        // Show thinking skill popup
        var thinkingSkills = _avatar.GetThinkingSkills();
        _thinkingPopup.Show(mousePosition);
    }

    /// <summary>
    /// Handle thinking skill selected from popup
    /// </summary>
    public async Task OnThinkingSkillSelectedAsync(Skill thinkingSkill)
    {
        if (_selectedKeyword == null || _currentNode == null) return;

        _thinkingAttemptsRemaining--;
        _ui.SetKeywordsEnabled(_thinkingAttemptsRemaining > 0);

        await RunThinkingPhaseAsync(thinkingSkill, _selectedKeyword);
    }

    /// <summary>
    /// Run thinking phase (generate CoT reasoning + actions)
    /// </summary>
    private async Task RunThinkingPhaseAsync(Skill thinkingSkill, string keyword)
    {
        if (_currentNode == null) return;

        _ui.ShowLoadingIndicator($"Thinking with {thinkingSkill.DisplayName}...");
        _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);

        try
        {
            // Use thinking controller to execute phase
            var result = await _thinkingController.ExecuteThinkingPhaseAsync(
                thinkingSkill,
                keyword,
                _currentNode,
                _state
            );

            // Add thinking block to UI
            var block = new NarrationBlock(
                NarrationBlockType.Thinking,
                thinkingSkill.DisplayName,
                result.ReasoningText,
                null,
                result.Actions
            );
            _ui.ScrollBuffer.AddBlock(block);
            _ui.ScrollBuffer.ScrollToBottom();
            
            _currentActions = result.Actions;
            _isWaitingForAction = true;

            _ui.HideLoadingIndicator();
            _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Thinking phase error: {ex.Message}");
            _ui.HideLoadingIndicator();
            _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);
        }
    }

    /// <summary>
    /// Handle action clicked
    /// </summary>
    public async Task OnActionClickedAsync(int actionIndex)
    {
        if (_currentActions == null || actionIndex >= _currentActions.Count) return;
        if (_currentNode == null) return;

        var selectedAction = _currentActions[actionIndex];
        
        // Disable all interaction during action execution
        _isWaitingForAction = false;
        _ui.SetKeywordsEnabled(false);
        _currentActions = null;

        await RunActionPhaseAsync(selectedAction);
    }

    /// <summary>
    /// Run action phase (skill check + outcome)
    /// </summary>
    private async Task RunActionPhaseAsync(ParsedNarrativeAction action)
    {
        if (_currentNode == null) return;

        _ui.ShowLoadingIndicator("Executing action...");
        _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);

        try
        {
            // Get thinking skill from action
            var thinkingSkill = action.ThinkingSkill;
            
            // Execute action
            var result = await _actionController.ExecuteActionAsync(action, _currentNode, thinkingSkill);

            // Add outcome block
            var block = new NarrationBlock(
                NarrationBlockType.Outcome,
                result.ThinkingSkill?.DisplayName ?? "Narrator",
                result.Narration,
                null,
                null
            );
            _ui.ScrollBuffer.AddBlock(block);
            _ui.ScrollBuffer.ScrollToBottom();

            _ui.HideLoadingIndicator();
            _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);

            // Brief pause to read outcome
            await Task.Delay(2000);

            // Apply outcome
            await ApplyOutcomeAsync(result.ActualOutcome);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Action phase error: {ex.Message}");
            _ui.HideLoadingIndicator();
            _ui.Render(_currentNode.NodeId, _thinkingAttemptsRemaining);
        }
    }

    /// <summary>
    /// Apply outcome and handle transitions
    /// </summary>
    private async Task ApplyOutcomeAsync(OutcomeBase outcome)
    {
        // Check if it's a NarrationNode (transition)
        if (outcome is NarrationNode nextNode)
        {
            await Task.Delay(500);
            await EnterNodeAsync(nextNode);
            return;
        }
        
        // Check if it's a HumorOutcome
        if (outcome is HumorOutcome humorOutcome)
        {
            // Apply humor changes (humor outcomes affect emotions/mental state)
            Console.WriteLine($"Applied humor outcome: {humorOutcome.DisplayName}");
            await ShowContinueAndExitAsync();
            return;
        }
        
        // FeelGoodOutcome or other generic outcomes
        await ShowContinueAndExitAsync();
    }

    /// <summary>
    /// Show "Continue" button and wait for click, then exit
    /// </summary>
    private async Task ShowContinueAndExitAsync()
    {
        // Add continue instruction to status bar
        var block = new NarrationBlock(
            NarrationBlockType.Outcome,
            "System",
            "\n[Press ESC to return to world view]",
            null,
            null
        );
        _ui.ScrollBuffer.AddBlock(block);
        _ui.ScrollBuffer.ScrollToBottom();
        _ui.Render(_currentNode?.NodeId ?? "Forest", _thinkingAttemptsRemaining);

        // Wait for ESC key (handled in main game loop)
        await Task.Delay(100);
    }

    /// <summary>
    /// Handle mouse wheel scroll
    /// </summary>
    public void OnMouseWheel(int delta)
    {
        if (delta > 0)
        {
            _ui.ScrollUp(3);
        }
        else if (delta < 0)
        {
            _ui.ScrollDown(3);
        }
        _ui.Render(_currentNode?.NodeId ?? "Forest", _thinkingAttemptsRemaining);
    }

    /// <summary>
    /// Handle mouse move (update hover state)
    /// </summary>
    public void OnMouseMove(int mouseX, int mouseY)
    {
        _ui.UpdateHover(mouseX, mouseY);
        _ui.Render(_currentNode?.NodeId ?? "Forest", _thinkingAttemptsRemaining);
    }

    /// <summary>
    /// Handle mouse click
    /// </summary>
    public async Task OnMouseClickAsync(int mouseX, int mouseY, Vector2 screenPosition)
    {
        // Check popup first
        if (_thinkingPopup.IsVisible)
        {
            var selectedSkill = _thinkingPopup.HandleMouseClick(mouseX, mouseY);
            if (selectedSkill != null)
            {
                await OnThinkingSkillSelectedAsync(selectedSkill);
            }
            return;
        }

        // Check keyword click
        string? keyword = _ui.GetClickedKeyword(mouseX, mouseY);
        if (keyword != null)
        {
            OnKeywordClicked(keyword, screenPosition);
            return;
        }

        // Check action click
        int? actionIndex = _ui.GetClickedActionIndex(mouseX, mouseY);
        if (actionIndex.HasValue)
        {
            await OnActionClickedAsync(actionIndex.Value);
            return;
        }
    }

    /// <summary>
    /// Handle keyboard input
    /// </summary>
    public async Task<bool> OnKeyPressAsync(ConsoleKey key)
    {
        // Check popup first
        if (_thinkingPopup.IsVisible)
        {
            var selectedSkill = _thinkingPopup.HandleInput(key);
            if (selectedSkill != null)
            {
                await OnThinkingSkillSelectedAsync(selectedSkill);
            }
            return false; // Don't exit
        }

        // Escape to exit
        if (key == ConsoleKey.Escape)
        {
            OnExitRequested?.Invoke();
            return true;
        }

        // Arrow keys for scrolling
        if (key == ConsoleKey.UpArrow)
        {
            _ui.ScrollUp(1);
            _ui.Render(_currentNode?.NodeId ?? "Forest", _thinkingAttemptsRemaining);
        }
        else if (key == ConsoleKey.DownArrow)
        {
            _ui.ScrollDown(1);
            _ui.Render(_currentNode?.NodeId ?? "Forest", _thinkingAttemptsRemaining);
        }

        return false;
    }

    /// <summary>
    /// Update (called every frame for animations)
    /// </summary>
    public void Update()
    {
        // Update loading animation frame
        if (_ui != null)
        {
            _ui.Render(_currentNode?.NodeId ?? "Forest", _thinkingAttemptsRemaining);
        }

        // Render popup if visible
        if (_thinkingPopup.IsVisible)
        {
            _thinkingPopup.Render();
        }
    }
}

