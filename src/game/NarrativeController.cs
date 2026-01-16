using System;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
using Cathedral.LLM;
using Cathedral.Terminal;
using Cathedral.Glyph;
using OpenTK.Mathematics;

namespace Cathedral.Game;

/// <summary>
/// Controls the Chain-of-Thought narration system for all locations.
/// Manages observation phase lifecycle and UI rendering.
/// </summary>
public class NarrativeController
{
    // State
    private readonly NarrativeState _narrationState = new();
    private readonly NarrationScrollBuffer _scrollBuffer;
    private readonly NarrativeUI _ui;
    private readonly TerminalThinkingSkillPopup _skillPopup;
    
    // Dependencies
    private readonly Avatar _avatar;
    private NarrationNode _currentNode;
    private readonly ObservationPhaseController _observationController;
    private readonly ThinkingExecutor _thinkingExecutor;
    private readonly ActionExecutionController _actionExecutor;
    private readonly GlyphSphereCore _core;
    private readonly TerminalInputHandler _terminalInputHandler;
    
    // Mouse tracking
    private int _lastMouseX = 0;
    private int _lastMouseY = 0;
    
    public NarrativeController(
        TerminalHUD terminal,
        PopupTerminalHUD popup,
        GlyphSphereCore core,
        LlamaServerManager llamaServer,
        SkillSlotManager slotManager,
        TerminalInputHandler terminalInputHandler,
        ThinkingExecutor thinkingExecutor,
        ActionExecutionController actionExecutor)
    {
        if (terminal == null)
            throw new ArgumentNullException(nameof(terminal));
        if (popup == null)
            throw new ArgumentNullException(nameof(popup));
        if (core == null)
            throw new ArgumentNullException(nameof(core));
        if (llamaServer == null)
            throw new ArgumentNullException(nameof(llamaServer));
        if (slotManager == null)
            throw new ArgumentNullException(nameof(slotManager));
        if (terminalInputHandler == null)
            throw new ArgumentNullException(nameof(terminalInputHandler));
        if (thinkingExecutor == null)
            throw new ArgumentNullException(nameof(thinkingExecutor));
        if (actionExecutor == null)
            throw new ArgumentNullException(nameof(actionExecutor));
        
        _ui = new NarrativeUI(terminal);
        // Content width: 100 (terminal) - 4 (left margin) - 4 (right margin) - 1 (scrollbar) = 91
        _scrollBuffer = new NarrationScrollBuffer(maxWidth: 91);
        _skillPopup = new TerminalThinkingSkillPopup(popup);
        _core = core;
        _terminalInputHandler = terminalInputHandler;
        
        // Initialize avatar with random skills
        _avatar = new Avatar();
        _avatar.InitializeSkills(SkillRegistry.Instance, skillCount: 50);
        
        // Get random entry node
        _currentNode = NodeRegistry.GetRandomEntryNode();
        
        // Initialize controllers
        _observationController = new ObservationPhaseController(llamaServer, slotManager);
        _thinkingExecutor = thinkingExecutor;
        _actionExecutor = actionExecutor;
        
        Console.WriteLine($"NarrativeController: Initialized with node {_currentNode.NodeId}");
        Console.WriteLine($"NarrativeController: Avatar has {_avatar.Skills.Count} skills");
    }
    
    /// <summary>
    /// Start the observation phase (generates observations asynchronously).
    /// This clears all history - use for initial start only.
    /// </summary>
    public void StartObservationPhase()
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        _narrationState.IsLoadingObservations = true;
        _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;
        
        // Fire-and-forget async task
        _ = GenerateObservationsAsync();
        
        Console.WriteLine("NarrativeController: Started observation phase");
    }
    
    /// <summary>
    /// Start the observation phase while preserving scroll buffer history.
    /// Used when transitioning to a new node after a successful action.
    /// </summary>
    private void StartObservationPhaseWithHistory()
    {
        // Note: ResetForNewNode() should already be called before this
        // Just set loading state and start generation
        _narrationState.IsLoadingObservations = true;
        _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;
        
        Console.WriteLine($"NarrativeController: Started observation phase (with history preserved)");
        Console.WriteLine($"  History lines: {_scrollBuffer.HistoryLineCount}");
        Console.WriteLine($"  Total lines: {_scrollBuffer.TotalLines}");
        Console.WriteLine($"  Scroll offset: {_scrollBuffer.ScrollOffset}");
        
        // Fire-and-forget async task
        _ = GenerateObservationsAsync();
    }
    
    /// <summary>
    /// Generate observations from selected skills (async).
    /// </summary>
    private async Task GenerateObservationsAsync()
    {
        try
        {
            Console.WriteLine("NarrativeController: Calling ObservationPhaseController...");
            
            // Generate ONE overall observation (one keyword per outcome)
            var blocks = await _observationController.ExecuteObservationPhaseAsync(
                _currentNode,
                _avatar,
                skillCount: 1
            );
            
            Console.WriteLine($"NarrativeController: Generated {blocks.Count} observation blocks");
            
            // Add blocks to scroll buffer
            foreach (var block in blocks)
            {
                _scrollBuffer.AddBlock(block);
                _narrationState.AddBlock(block);
            }
            
            // Update state
            _narrationState.IsLoadingObservations = false;
            _narrationState.ErrorMessage = null;
            
            Console.WriteLine("NarrativeController: Observation phase complete");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error generating observations: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            _narrationState.IsLoadingObservations = false;
            _narrationState.ErrorMessage = $"Failed to generate observations: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Execute thinking phase with selected skill and keyword (async).
    /// </summary>
    private async Task ExecuteThinkingPhaseAsync(Skill thinkingSkill, string keyword)
    {
        try
        {
            Console.WriteLine($"NarrativeController: Executing thinking with {thinkingSkill.DisplayName} on keyword '{keyword}'");
            
            // Get possible outcomes for this keyword
            var possibleOutcomes = _currentNode.GetOutcomesForKeyword(keyword);
            
            // Always add FeelGoodOutcome as a fallback option
            var feelGoodOutcome = new FeelGoodOutcome();
            if (!possibleOutcomes.Any(o => o is FeelGoodOutcome))
            {
                possibleOutcomes.Add(feelGoodOutcome);
            }
            
            // Get action skills
            var actionSkills = _avatar.GetActionSkills();
            
            Console.WriteLine($"NarrativeController: Found {possibleOutcomes.Count} outcomes, {actionSkills.Count} action skills");
            
            // Call ThinkingExecutor to generate reasoning + actions
            var response = await _thinkingExecutor.GenerateThinkingAsync(
                thinkingSkill,
                keyword,
                _currentNode,
                possibleOutcomes,
                actionSkills,
                _avatar,
                CancellationToken.None);
            
            if (response == null || response.Actions.Count == 0)
            {
                // Display error - no fallback as per user request
                throw new Exception("Thinking LLM returned no actions");
            }
            
            Console.WriteLine($"NarrativeController: Generated {response.Actions.Count} actions");
            
            // Create thinking block with reasoning + actions
            var thinkingBlock = new NarrationBlock(
                Type: NarrationBlockType.Thinking,
                SkillName: thinkingSkill.DisplayName,
                Text: response.ReasoningText,
                Keywords: null,
                Actions: response.Actions
            );
            
            // Add to scroll buffer
            _scrollBuffer.AddBlock(thinkingBlock);
            _narrationState.AddBlock(thinkingBlock);
            
            // Auto-scroll to bottom to show new thinking block
            _scrollBuffer.ScrollToBottom();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset; // Sync scroll position
            
            // Update state
            _narrationState.IsLoadingThinking = false;
            _narrationState.ThinkingAttemptsRemaining--;
            _narrationState.ErrorMessage = null;
            
            Console.WriteLine($"NarrativeController: Thinking phase complete ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during thinking phase: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            _narrationState.IsLoadingThinking = false;
            _narrationState.ErrorMessage = $"Thinking failed: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Execute action phase: skill check, outcome determination, and narration (async).
    /// </summary>
    private async Task ExecuteActionPhaseAsync(ParsedNarrativeAction action)
    {
        try
        {
            Console.WriteLine($"NarrativeController: Starting action execution for '{action.ActionText}'");
            
            // Set loading state with progress messages
            _narrationState.IsLoadingAction = true;
            _narrationState.LoadingMessage = Config.LoadingMessages.EvaluatingAction;
            
            // Execute action via ActionExecutionController
            var result = await _actionExecutor.ExecuteActionAsync(
                action,
                _currentNode,
                action.ThinkingSkill,
                CancellationToken.None
            );
            
            Console.WriteLine($"NarrativeController: Action {(result.Succeeded ? "SUCCEEDED" : "FAILED")}");
            
            // Add outcome narration block (using action skill name since it narrates from action skill's perspective)
            var outcomeBlock = new NarrationBlock(
                Type: NarrationBlockType.Outcome,
                SkillName: result.ActionSkill?.DisplayName ?? "Unknown Skill",
                Text: $"[{(result.Succeeded ? "SUCCESS" : "FAILURE")}] {result.Narration}",
                Keywords: null,
                Actions: null
            );
            _scrollBuffer.AddBlock(outcomeBlock);
            _narrationState.AddBlock(outcomeBlock);
            
            // Auto-scroll to bottom to show outcome
            _scrollBuffer.ScrollToBottom();
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
            
            // Handle outcome based on type - always show continue button first
            if (result.ActualOutcome is NarrationNode nextNode)
            {
                Console.WriteLine($"NarrativeController: Transition outcome to node {nextNode.NodeId}, showing continue button");
                
                // Store pending transition - will execute when continue is clicked
                _narrationState.PendingTransitionNode = nextNode;
                _narrationState.ShowContinueButton = true;
            }
            else
            {
                Console.WriteLine("NarrativeController: Non-transition outcome, showing continue button");
                
                // No transition pending, continue button will exit or restart
                _narrationState.PendingTransitionNode = null;
                _narrationState.ShowContinueButton = true;
            }
            
            // Update state
            _narrationState.IsLoadingAction = false;
            _narrationState.ErrorMessage = null;
            
            Console.WriteLine("NarrativeController: Action phase complete");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during action execution: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            _narrationState.IsLoadingAction = false;
            _narrationState.ErrorMessage = $"Action execution failed: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Execute focus observation phase: generate a detailed observation for a specific outcome (async).
    /// Triggered by right-clicking a keyword and selecting an observation skill.
    /// </summary>
    private async Task ExecuteFocusObservationAsync(Skill observationSkill, string keyword)
    {
        try
        {
            Console.WriteLine($"NarrativeController: Executing focus observation with {observationSkill.DisplayName} on keyword '{keyword}'");
            
            // Generate focus observation
            var block = await _observationController.GenerateFocusObservationAsync(
                keyword,
                observationSkill,
                _currentNode,
                _avatar
            );
            
            if (block != null)
            {
                // Add to scroll buffer
                _scrollBuffer.AddBlock(block);
                _narrationState.AddBlock(block);
                
                // Auto-scroll to bottom to show new observation
                _scrollBuffer.ScrollToBottom();
                _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                
                Console.WriteLine($"NarrativeController: Focus observation complete");
            }
            else
            {
                Console.WriteLine("NarrativeController: Focus observation returned null");
            }
            
            // Consume a thinking point (same pool as thinking)
            _narrationState.ThinkingAttemptsRemaining--;
            
            // Update state
            _narrationState.IsLoadingFocusObservation = false;
            _narrationState.ErrorMessage = null;
            
            Console.WriteLine($"NarrativeController: Focus observation phase complete ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"NarrativeController: Error during focus observation: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            _narrationState.IsLoadingFocusObservation = false;
            _narrationState.ErrorMessage = $"Focus observation failed: {ex.Message}";
        }
    }
    
    /// <summary>
    /// Update loop - called at 10 Hz by game controller.
    /// </summary>
    public void Update()
    {
        // Clear terminal
        _ui.Clear();
        
        // Render header
        _ui.RenderHeader(_currentNode.DisplayName, _narrationState.ThinkingAttemptsRemaining);
        
        // Show error if present
        if (_narrationState.ErrorMessage != null)
        {
            _ui.ShowError(_narrationState.ErrorMessage);
            _ui.RenderStatusBar("Press ESC to return to world view");
            return;
        }
        
        // Show loading indicator if generating
        if (_narrationState.IsLoadingObservations || _narrationState.IsLoadingThinking || _narrationState.IsLoadingAction || _narrationState.IsLoadingFocusObservation)
        {
            _ui.ShowLoadingIndicator(_narrationState.LoadingMessage);
            string loadingStatus = _narrationState.IsLoadingObservations 
                ? "Generating observations..." 
                : _narrationState.IsLoadingThinking
                    ? "Generating thinking and actions..."
                    : _narrationState.IsLoadingFocusObservation
                        ? "Generating focus observation..."
                        : "Executing action...";
            _ui.RenderStatusBar(loadingStatus);
            return;
        }
        
        // Show continue button if flagged
        if (_narrationState.ShowContinueButton)
        {
            // Render narration blocks (non-interactive)
            _ui.RenderObservationBlocks(
                _scrollBuffer,
                _narrationState.ScrollOffset,
                _narrationState.ThinkingAttemptsRemaining,
                null, // No keyword hover
                null  // No action hover
            );
            
            // Render scrollbar (still visible when continue button shown)
            _narrationState.ScrollbarThumb = _ui.RenderScrollbar(
                _scrollBuffer,
                _narrationState.ScrollOffset,
                _narrationState.IsScrollbarThumbHovered
            );
            
            // Render continue button
            var buttonRegion = _ui.RenderContinueButton(_narrationState.IsContinueButtonHovered);
            
            // Track button region for click detection (reuse ActionRegion for simplicity)
            _narrationState.ActionRegions.Clear();
            _narrationState.ActionRegions.Add(new ActionRegion(
                0, 
                buttonRegion.Y, 
                buttonRegion.Y, 
                buttonRegion.X, 
                buttonRegion.X + buttonRegion.Width
            ));
            
            _ui.RenderStatusBar("Click Continue to return to world view");
            return;
        }
        
        // Render observation blocks with keywords
        _ui.RenderObservationBlocks(
            _scrollBuffer,
            _narrationState.ScrollOffset,
            _narrationState.ThinkingAttemptsRemaining,
            _narrationState.HoveredKeyword,
            _narrationState.HoveredAction
        );
        
        // Render scrollbar and update thumb region
        _narrationState.ScrollbarThumb = _ui.RenderScrollbar(
            _scrollBuffer,
            _narrationState.ScrollOffset,
            _narrationState.IsScrollbarThumbHovered
        );
        
        // Render status bar
        string statusMessage = _narrationState.ThinkingAttemptsRemaining > 0
            ? $"Hover keywords to highlight • Click keywords to think ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)"
            : "No thinking attempts remaining • Explore keywords to continue";
        _ui.RenderStatusBar(statusMessage);
    }
    
    /// <summary>
    /// Handle raw mouse move event with screen pixel coordinates.
    /// Used when popup is visible to bypass terminal cell coordinate system.
    /// </summary>
    public void OnRawMouseMove(Vector2 screenPosition)
    {
        // If popup is visible, use raw screen coordinates for accurate hit detection
        if (_skillPopup.IsVisible)
        {
            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.Size);
            int cellPixelSize = (int)layoutInfo.CellSize.X; // Assume square cells
            
            _skillPopup.UpdateHover(screenPosition.X, screenPosition.Y, _core.Size, cellPixelSize);
        }
    }
    
    /// <summary>
    /// Handle raw mouse click event with screen pixel coordinates.
    /// Used when popup is visible to bypass terminal cell coordinate system.
    /// </summary>
    public void OnRawMouseClick(Vector2 screenPosition)
    {
        // If popup is visible, handle popup click with screen coordinates
        if (_skillPopup.IsVisible)
        {
            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.Size);
            int cellPixelSize = (int)layoutInfo.CellSize.X; // Assume square cells
            
            var selectedSkill = _skillPopup.HandleClick(screenPosition.X, screenPosition.Y, _core.Size, cellPixelSize);
            if (selectedSkill != null)
            {
                Console.WriteLine($"NarrativeController: Selected skill: {selectedSkill.DisplayName}");
                
                // Get the keyword that was clicked (stored before popup appeared)
                if (_narrationState.HoveredKeyword != null)
                {
                    string keyword = _narrationState.HoveredKeyword.Keyword;
                    
                    // Check if we're selecting an observation skill (right-click) or thinking skill (left-click)
                    if (_narrationState.IsSelectingObservationSkill)
                    {
                        // Focus observation phase
                        _narrationState.IsLoadingFocusObservation = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;
                        _narrationState.IsSelectingObservationSkill = false;
                        
                        // Fire-and-forget async task
                        _ = ExecuteFocusObservationAsync(selectedSkill, keyword);
                    }
                    else
                    {
                        // Thinking phase (left-click)
                        _narrationState.IsLoadingThinking = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.ThinkingDeeply;
                        
                        // Fire-and-forget async task
                        _ = ExecuteThinkingPhaseAsync(selectedSkill, keyword);
                    }
                }
            }
            else
            {
                Console.WriteLine("NarrativeController: Popup closed (clicked outside)");
                _narrationState.IsSelectingObservationSkill = false;
            }
        }
    }
    
    /// <summary>
    /// Handle mouse move event.
    /// </summary>
    public void OnMouseMove(int mouseX, int mouseY)
    {
        _lastMouseX = mouseX;
        _lastMouseY = mouseY;
        
        // If popup is visible, raw mouse events are handled separately
        if (_skillPopup.IsVisible)
        {
            return;
        }
        
        // Stop dragging if mouse button was released
        if (_narrationState.IsScrollbarDragging && !_terminalInputHandler.IsLeftMouseDown)
        {
            _narrationState.IsScrollbarDragging = false;
            Console.WriteLine("NarrativeController: Stopped scrollbar drag");
        }
        
        // Handle scrollbar dragging
        if (_narrationState.IsScrollbarDragging)
        {
            int deltaY = mouseY - _narrationState.ScrollbarDragStartY;
            
            int trackHeight = NarrativeLayout.SCROLLBAR_TRACK_HEIGHT;
            int totalLines = _scrollBuffer.TotalLines;
            int visibleLines = NarrativeLayout.NARRATIVE_HEIGHT;
            
            int maxScrollOffset = NarrativeLayout.CalculateMaxScrollOffset(totalLines);
            
            // Calculate thumb size for proper scaling
            float visibleRatio = (float)visibleLines / totalLines;
            int thumbHeight = Math.Max(2, (int)(trackHeight * visibleRatio));
            int maxThumbY = trackHeight - thumbHeight;
            
            // Convert mouse delta to scroll offset delta
            float scrollRatio = maxThumbY > 0 ? (float)deltaY / maxThumbY : 0f;
            int newOffset = _narrationState.ScrollbarDragStartOffset + (int)(maxScrollOffset * scrollRatio);
            
            // Clamp and update scroll offset
            newOffset = Math.Clamp(newOffset, 0, maxScrollOffset);
            if (newOffset != _scrollBuffer.ScrollOffset)
            {
                _scrollBuffer.SetScrollOffset(newOffset);
                _narrationState.ScrollOffset = newOffset;
            }
            return;
        }
        
        // Update scrollbar thumb hover state
        bool isOverThumb = _ui.IsMouseOverScrollbarThumb(mouseX, mouseY, _narrationState.ScrollbarThumb);
        if (isOverThumb != _narrationState.IsScrollbarThumbHovered)
        {
            _narrationState.IsScrollbarThumbHovered = isOverThumb;
        }
        
        // If continue button is shown, check if mouse is over it
        if (_narrationState.ShowContinueButton && _narrationState.ActionRegions.Count > 0)
        {
            var buttonRegion = _narrationState.ActionRegions[0];
            bool isOverButton = mouseY == buttonRegion.StartY && 
                                mouseX >= buttonRegion.StartX && 
                                mouseX <= buttonRegion.EndX;
            
            if (isOverButton != _narrationState.IsContinueButtonHovered)
            {
                _narrationState.IsContinueButtonHovered = isOverButton;
            }
            
            // Don't process keyword/action hover when continue button is shown
            return;
        }
        
        // Update hovered keyword region
        KeywordRegion? newHoveredKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (newHoveredKeyword != _narrationState.HoveredKeyword)
        {
            _narrationState.HoveredKeyword = newHoveredKeyword;
            // UI will re-render on next Update() call
        }
        
        // Update hovered action region
        ActionRegion? newHoveredAction = _ui.GetHoveredAction(mouseX, mouseY);
        
        if (newHoveredAction != _narrationState.HoveredAction)
        {
            _narrationState.HoveredAction = newHoveredAction;
            // UI will re-render on next Update() call
        }
    }
    
    /// <summary>
    /// Handle mouse click event.
    /// </summary>
    public void OnMouseClick(int mouseX, int mouseY)
    {
        // If continue button is shown, check if clicked
        if (_narrationState.ShowContinueButton && _narrationState.ActionRegions.Count > 0)
        {
            var buttonRegion = _narrationState.ActionRegions[0];
            if (mouseY == buttonRegion.StartY && mouseX >= buttonRegion.StartX && mouseX <= buttonRegion.EndX)
            {
                // Check if there's a pending transition to a new node
                if (_narrationState.PendingTransitionNode != null)
                {
                    Console.WriteLine($"NarrativeController: Continue button clicked, transitioning to {_narrationState.PendingTransitionNode.NodeId}");
                    
                    // Perform the transition
                    _currentNode = _narrationState.PendingTransitionNode;
                    
                    // Convert current narration to history (grayed out, non-interactive)
                    _scrollBuffer.ConvertToHistory();
                    _narrationState.ResetForNewNode();
                    _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
                    
                    // Start new observation phase WITHOUT clearing history
                    StartObservationPhaseWithHistory();
                }
                else
                {
                    Console.WriteLine("NarrativeController: Continue button clicked, exiting to world view");
                    // Signal exit by setting a flag that the game controller can check
                    _narrationState.RequestedExit = true;
                    // The calling controller should check HasRequestedExit() and exit mode
                }
            }
            return;
        }
        
        // If popup is visible, handle popup click with screen coordinates
        if (_skillPopup.IsVisible)
        {
            // Get corrected screen mouse position (includes border height offset)
            Vector2 correctedScreenPos = _terminalInputHandler.GetCorrectedMousePosition();
            
            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.Size);
            int cellPixelSize = (int)layoutInfo.CellSize.X; // Assume square cells
            
            var selectedSkill = _skillPopup.HandleClick(correctedScreenPos.X, correctedScreenPos.Y, _core.Size, cellPixelSize);
            if (selectedSkill != null)
            {
                Console.WriteLine($"NarrativeController: Selected skill: {selectedSkill.DisplayName}");
                
                // Get the keyword that was clicked (stored before popup appeared)
                if (_narrationState.HoveredKeyword != null)
                {
                    string keyword = _narrationState.HoveredKeyword.Keyword;
                    
                    // Check if we're selecting an observation skill (right-click) or thinking skill (left-click)
                    if (_narrationState.IsSelectingObservationSkill)
                    {
                        // Focus observation phase
                        _narrationState.IsLoadingFocusObservation = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.GeneratingObservations;
                        _narrationState.IsSelectingObservationSkill = false;
                        
                        // Fire-and-forget async task
                        _ = ExecuteFocusObservationAsync(selectedSkill, keyword);
                    }
                    else
                    {
                        // Thinking phase (left-click)
                        _narrationState.IsLoadingThinking = true;
                        _narrationState.LoadingMessage = Config.LoadingMessages.ThinkingDeeply;
                        
                        // Fire-and-forget async task
                        _ = ExecuteThinkingPhaseAsync(selectedSkill, keyword);
                    }
                }
            }
            else
            {
                Console.WriteLine("NarrativeController: Popup closed (clicked outside)");
                _narrationState.IsSelectingObservationSkill = false;
            }
            return;
        }
        
        // Check if clicked on scrollbar thumb (start drag)
        if (_ui.IsMouseOverScrollbarThumb(mouseX, mouseY, _narrationState.ScrollbarThumb))
        {
            _narrationState.IsScrollbarDragging = true;
            _narrationState.ScrollbarDragStartY = mouseY;
            _narrationState.ScrollbarDragStartOffset = _narrationState.ScrollOffset;
            Console.WriteLine($"NarrativeController: Started scrollbar drag at Y={mouseY}");
            return;
        }
        
        // Check if clicked on scrollbar track (jump scroll)
        if (_ui.IsMouseOverScrollbarTrack(mouseX, mouseY, _narrationState.ScrollbarThumb))
        {
            int newOffset = _ui.CalculateScrollOffsetFromMouseY(mouseY, _scrollBuffer);
            _scrollBuffer.SetScrollOffset(newOffset);
            _narrationState.ScrollOffset = newOffset;
            Console.WriteLine($"NarrativeController: Jump scrolled to offset {newOffset}");
            return;
        }
        
        // Check if clicked on an action
        ActionRegion? clickedAction = _ui.GetHoveredAction(mouseX, mouseY);
        if (clickedAction != null)
        {
            // Collect all actions from all thinking blocks (globally indexed)
            var allActions = new List<ParsedNarrativeAction>();
            foreach (var block in _narrationState.Blocks)
            {
                if (block.Type == NarrationBlockType.Thinking && block.Actions != null)
                {
                    allActions.AddRange(block.Actions);
                }
            }
            
            if (clickedAction.ActionIndex < allActions.Count)
            {
                var action = allActions[clickedAction.ActionIndex];
                Console.WriteLine($"NarrativeController: Executing action '{action.ActionText}' with skill '{action.ActionSkillId}'");
                
                // Fire-and-forget async task
                _ = ExecuteActionPhaseAsync(action);
            }
            else
            {
                Console.WriteLine($"NarrativeController: Failed to find action at index {clickedAction.ActionIndex}");
            }
            return;
        }
        
        // Check if clicked on a keyword
        KeywordRegion? clickedKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (clickedKeyword != null && _narrationState.ThinkingAttemptsRemaining > 0)
        {
            Console.WriteLine($"NarrativeController: Clicked keyword: {clickedKeyword}");
            
            // Show thinking skill selection popup (left-click = thinking)
            _narrationState.IsSelectingObservationSkill = false;
            var thinkingSkills = _avatar.GetThinkingSkills();
            
            // Convert terminal cell coordinates to screen pixel coordinates
            Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.Size);
            
            _skillPopup.Show(screenPos, thinkingSkills, "Select Thinking Skill");
            Console.WriteLine($"NarrativeController: Showing {thinkingSkills.Count} thinking skills at screen position ({screenPos.X}, {screenPos.Y})");;
        }
    }
    
    /// <summary>
    /// Handle right mouse click event - triggers focus observation on keywords.
    /// </summary>
    public void OnRightClick(int mouseX, int mouseY)
    {
        // Don't handle right-clicks if popup is visible or in loading state
        if (_skillPopup.IsVisible)
            return;
        
        if (_narrationState.IsLoadingObservations || _narrationState.IsLoadingThinking || 
            _narrationState.IsLoadingAction || _narrationState.IsLoadingFocusObservation)
            return;
        
        // Don't handle if continue button is shown
        if (_narrationState.ShowContinueButton)
            return;
        
        // Check if right-clicked on a keyword
        KeywordRegion? clickedKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (clickedKeyword != null && _narrationState.ThinkingAttemptsRemaining > 0)
        {
            Console.WriteLine($"NarrativeController: Right-clicked keyword: {clickedKeyword.Keyword}");
            
            // Show observation skill selection popup (right-click = focus observation)
            _narrationState.IsSelectingObservationSkill = true;
            _narrationState.HoveredKeyword = clickedKeyword;  // Store for later use
            var observationSkills = _avatar.GetObservationSkills();
            
            // Convert terminal cell coordinates to screen pixel coordinates
            Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.Size);
            
            _skillPopup.Show(screenPos, observationSkills, "Select Observation Skill");
            Console.WriteLine($"NarrativeController: Showing {observationSkills.Count} observation skills for focus observation at screen position ({screenPos.X}, {screenPos.Y})");
        }
    }
    
    /// <summary>
    /// Handle mouse wheel scroll event.
    /// </summary>
    public void OnMouseWheel(float delta)
    {
        if (delta > 0)
        {
            // Scroll up
            _scrollBuffer.ScrollUp(3);
        }
        else if (delta < 0)
        {
            // Scroll down
            _scrollBuffer.ScrollDown(3);
        }
        
        _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset;
    }
    
    /// <summary>
    /// Check if we're still in loading state.
    /// </summary>
    public bool IsLoading => _narrationState.IsLoadingObservations;
    
    /// <summary>
    /// Check if there's an error.
    /// </summary>
    public bool HasError => _narrationState.ErrorMessage != null;
    
    /// <summary>
    /// Check if the thinking skill popup is visible.
    /// </summary>
    public bool IsPopupVisible => _skillPopup.IsVisible;
    
    /// <summary>
    /// Close the thinking skill popup if it's open.
    /// Returns true if popup was closed, false if it wasn't open.
    /// </summary>
    public bool ClosePopup()
    {
        if (_skillPopup.IsVisible)
        {
            _skillPopup.Hide();
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// Get the current narration state.
    /// </summary>
    public NarrativeState GetState() => _narrationState;
    
    /// <summary>
    /// Check if player has requested to exit Phase 6 (clicked Continue button).
    /// </summary>
    public bool HasRequestedExit() => _narrationState.RequestedExit;
}
