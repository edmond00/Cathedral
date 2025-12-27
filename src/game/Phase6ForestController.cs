using System;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
using Cathedral.LLM;
using Cathedral.Terminal;
using Cathedral.Glyph;
using OpenTK.Mathematics;

namespace Cathedral.Game;

/// <summary>
/// Controls the Phase 6 Chain-of-Thought narration system for forest locations.
/// Manages observation phase lifecycle and UI rendering.
/// </summary>
public class Phase6ForestController
{
    // UI constants
    private const int NARRATIVE_HEIGHT = 26; // Terminal height (30) - header (2) - status bar (1) - margin (1)
    
    // State
    private readonly Phase6NarrationState _narrationState = new();
    private readonly NarrationScrollBuffer _scrollBuffer;
    private readonly Phase6ObservationUI _ui;
    private readonly TerminalThinkingSkillPopup _skillPopup;
    
    // Dependencies
    private readonly Avatar _avatar;
    private NarrationNode _currentNode;
    private readonly ObservationPhaseController _observationController;
    private readonly ThinkingExecutor _thinkingExecutor;
    private readonly GlyphSphereCore _core;
    private readonly TerminalInputHandler _terminalInputHandler;
    
    // Mouse tracking
    private int _lastMouseX = 0;
    private int _lastMouseY = 0;
    
    public Phase6ForestController(
        TerminalHUD terminal,
        PopupTerminalHUD popup,
        GlyphSphereCore core,
        LlamaServerManager llamaServer,
        SkillSlotManager slotManager,
        TerminalInputHandler terminalInputHandler,
        ThinkingExecutor thinkingExecutor)
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
        
        _ui = new Phase6ObservationUI(terminal);
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
        
        Console.WriteLine($"Phase6ForestController: Initialized with node {_currentNode.NodeId}");
        Console.WriteLine($"Phase6ForestController: Avatar has {_avatar.Skills.Count} skills");
    }
    
    /// <summary>
    /// Start the observation phase (generates observations asynchronously).
    /// </summary>
    public void StartObservationPhase()
    {
        _narrationState.Clear();
        _scrollBuffer.Clear();
        _narrationState.IsLoadingObservations = true;
        _narrationState.LoadingMessage = "Generating observations...";
        
        // Fire-and-forget async task
        _ = GenerateObservationsAsync();
        
        Console.WriteLine("Phase6ForestController: Started observation phase");
    }
    
    /// <summary>
    /// Generate observations from selected skills (async).
    /// </summary>
    private async Task GenerateObservationsAsync()
    {
        try
        {
            Console.WriteLine("Phase6ForestController: Calling ObservationPhaseController...");
            
            // Generate 2-3 observations
            var blocks = await _observationController.ExecuteObservationPhaseAsync(
                _currentNode,
                _avatar,
                skillCount: 3
            );
            
            Console.WriteLine($"Phase6ForestController: Generated {blocks.Count} observation blocks");
            
            // Add blocks to scroll buffer
            foreach (var block in blocks)
            {
                _scrollBuffer.AddBlock(block);
                _narrationState.AddBlock(block);
            }
            
            // Update state
            _narrationState.IsLoadingObservations = false;
            _narrationState.ErrorMessage = null;
            
            Console.WriteLine("Phase6ForestController: Observation phase complete");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Phase6ForestController: Error generating observations: {ex.Message}");
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
            Console.WriteLine($"Phase6ForestController: Executing thinking with {thinkingSkill.DisplayName} on keyword '{keyword}'");
            
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
            
            Console.WriteLine($"Phase6ForestController: Found {possibleOutcomes.Count} outcomes, {actionSkills.Count} action skills");
            
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
            
            Console.WriteLine($"Phase6ForestController: Generated {response.Actions.Count} actions");
            
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
            _scrollBuffer.ScrollToBottom(NARRATIVE_HEIGHT);
            _narrationState.ScrollOffset = _scrollBuffer.ScrollOffset; // Sync scroll position
            
            // Update state
            _narrationState.IsLoadingThinking = false;
            _narrationState.ThinkingAttemptsRemaining--;
            _narrationState.ErrorMessage = null;
            
            Console.WriteLine($"Phase6ForestController: Thinking phase complete ({_narrationState.ThinkingAttemptsRemaining} attempts remaining)");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Phase6ForestController: Error during thinking phase: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            
            _narrationState.IsLoadingThinking = false;
            _narrationState.ErrorMessage = $"Thinking failed: {ex.Message}";
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
        if (_narrationState.IsLoadingObservations || _narrationState.IsLoadingThinking)
        {
            _ui.ShowLoadingIndicator(_narrationState.LoadingMessage);
            string loadingStatus = _narrationState.IsLoadingObservations 
                ? "Generating observations..." 
                : "Generating thinking and actions...";
            _ui.RenderStatusBar(loadingStatus);
            return;
        }
        
        // Render observation blocks with keywords
        _ui.RenderObservationBlocks(
            _scrollBuffer,
            _narrationState.ScrollOffset,
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
    /// Handle mouse move event.
    /// </summary>
    public void OnMouseMove(int mouseX, int mouseY)
    {
        _lastMouseX = mouseX;
        _lastMouseY = mouseY;
        
        // If popup is visible, update popup hover with screen coordinates
        if (_skillPopup.IsVisible)
        {
            // Convert terminal cell coordinates to screen pixel coordinates
            Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.Size);
            
            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.Size);
            int cellPixelSize = (int)layoutInfo.CellSize.X; // Assume square cells
            
            _skillPopup.UpdateHover(screenPos.X, screenPos.Y, _core.Size, cellPixelSize);
            return;
        }
        
        // Stop dragging if mouse button was released
        if (_narrationState.IsScrollbarDragging && !_terminalInputHandler.IsLeftMouseDown)
        {
            _narrationState.IsScrollbarDragging = false;
            Console.WriteLine("Phase6ForestController: Stopped scrollbar drag");
        }
        
        // Handle scrollbar dragging
        if (_narrationState.IsScrollbarDragging)
        {
            int deltaY = mouseY - _narrationState.ScrollbarDragStartY;
            int trackHeight = 27; // NARRATIVE_HEIGHT
            int totalLines = _scrollBuffer.TotalLines;
            int visibleLines = 27;
            // Add 5-line margin to ensure last lines are visible
            int maxScrollOffset = Math.Max(0, totalLines - visibleLines + 5);
            
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
                _scrollBuffer.SetScrollOffset(newOffset, NARRATIVE_HEIGHT);
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
        // If popup is visible, handle popup click with screen coordinates
        if (_skillPopup.IsVisible)
        {
            // Convert terminal cell coordinates to screen pixel coordinates
            Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.Size);
            
            // Get cell size for hit detection
            var layoutInfo = _terminalInputHandler.GetLayoutInfo(_core.Size);
            int cellPixelSize = (int)layoutInfo.CellSize.X; // Assume square cells
            
            var selectedSkill = _skillPopup.HandleClick(screenPos.X, screenPos.Y, _core.Size, cellPixelSize);
            if (selectedSkill != null)
            {
                Console.WriteLine($"Phase6ForestController: Selected skill: {selectedSkill.DisplayName}");
                
                // Get the keyword that was clicked (stored before popup appeared)
                if (_narrationState.HoveredKeyword != null)
                {
                    string keyword = _narrationState.HoveredKeyword.Keyword;
                    
                    // Start thinking phase
                    _narrationState.IsLoadingThinking = true;
                    _narrationState.LoadingMessage = "Thinking deeply...";
                    
                    // Fire-and-forget async task
                    _ = ExecuteThinkingPhaseAsync(selectedSkill, keyword);
                }
            }
            else
            {
                Console.WriteLine("Phase6ForestController: Popup closed (clicked outside)");
            }
            return;
        }
        
        // Check if clicked on scrollbar thumb (start drag)
        if (_ui.IsMouseOverScrollbarThumb(mouseX, mouseY, _narrationState.ScrollbarThumb))
        {
            _narrationState.IsScrollbarDragging = true;
            _narrationState.ScrollbarDragStartY = mouseY;
            _narrationState.ScrollbarDragStartOffset = _narrationState.ScrollOffset;
            Console.WriteLine($"Phase6ForestController: Started scrollbar drag at Y={mouseY}");
            return;
        }
        
        // Check if clicked on scrollbar track (jump scroll)
        if (_ui.IsMouseOverScrollbarTrack(mouseX, mouseY, _narrationState.ScrollbarThumb))
        {
            int newOffset = _ui.CalculateScrollOffsetFromMouseY(mouseY, _scrollBuffer);
            _scrollBuffer.SetScrollOffset(newOffset, NARRATIVE_HEIGHT);
            _narrationState.ScrollOffset = newOffset;
            Console.WriteLine($"Phase6ForestController: Jump scrolled to offset {newOffset}");
            return;
        }
        
        // Check if clicked on an action
        ActionRegion? clickedAction = _ui.GetHoveredAction(mouseX, mouseY);
        if (clickedAction != null)
        {
            // TODO: Execute action phase
            Console.WriteLine($"Phase6ForestController: Clicked action index {clickedAction.ActionIndex} at Y={clickedAction.StartY}");
            // Action execution will be implemented later
            return;
        }
        
        // Check if clicked on a keyword
        KeywordRegion? clickedKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (clickedKeyword != null && _narrationState.ThinkingAttemptsRemaining > 0)
        {
            Console.WriteLine($"Phase6ForestController: Clicked keyword: {clickedKeyword}");
            
            // Show thinking skill selection popup
            var thinkingSkills = _avatar.GetThinkingSkills();
            
            // Convert terminal cell coordinates to screen pixel coordinates
            Vector2 screenPos = _terminalInputHandler.CellToScreen(mouseX, mouseY, _core.Size);
            
            _skillPopup.Show(screenPos, thinkingSkills);
            Console.WriteLine($"Phase6ForestController: Showing {thinkingSkills.Count} thinking skills at screen position ({screenPos.X}, {screenPos.Y})");
        }
    }
    
    /// <summary>
    /// Handle mouse wheel scroll event.
    /// </summary>
    public void OnMouseWheel(float delta)
    {
        const int VIEWPORT_HEIGHT = 26; // NARRATIVE_HEIGHT - SEPARATOR_HEIGHT
        
        if (delta > 0)
        {
            // Scroll up
            _scrollBuffer.ScrollUp(3, VIEWPORT_HEIGHT);
        }
        else if (delta < 0)
        {
            // Scroll down
            _scrollBuffer.ScrollDown(3, VIEWPORT_HEIGHT);
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
    public Phase6NarrationState GetState() => _narrationState;
}
