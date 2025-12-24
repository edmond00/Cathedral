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
    // State
    private readonly Phase6NarrationState _narrationState = new();
    private readonly NarrationScrollBuffer _scrollBuffer;
    private readonly Phase6ObservationUI _ui;
    private readonly TerminalThinkingSkillPopup _skillPopup;
    
    // Dependencies
    private readonly Avatar _avatar;
    private NarrationNode _currentNode;
    private readonly ObservationPhaseController _observationController;
    private readonly GlyphSphereCore _core;
    
    // Mouse tracking
    private int _lastMouseX = 0;
    private int _lastMouseY = 0;
    
    public Phase6ForestController(
        TerminalHUD terminal,
        PopupTerminalHUD popup,
        GlyphSphereCore core,
        LlamaServerManager llamaServer,
        SkillSlotManager slotManager)
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
        
        _ui = new Phase6ObservationUI(terminal);
        _scrollBuffer = new NarrationScrollBuffer(maxWidth: 96); // Terminal width - 4 for margins
        _skillPopup = new TerminalThinkingSkillPopup(popup);
        _core = core;
        
        // Initialize avatar with random skills
        _avatar = new Avatar();
        _avatar.InitializeSkills(SkillRegistry.Instance, skillCount: 50);
        
        // Get random entry node
        _currentNode = NodeRegistry.GetRandomEntryNode();
        
        // Initialize observation controller
        _observationController = new ObservationPhaseController(llamaServer, slotManager);
        
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
        if (_narrationState.IsLoadingObservations)
        {
            _ui.ShowLoadingIndicator(_narrationState.LoadingMessage);
            _ui.RenderStatusBar("Generating observations...");
            return;
        }
        
        // Render observation blocks with keywords
        _ui.RenderObservationBlocks(
            _scrollBuffer,
            _narrationState.ScrollOffset,
            _narrationState.HoveredKeyword
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
        
        // If popup is visible, update popup hover
        if (_skillPopup.IsVisible)
        {
            // Simple hover detection: row 1-N corresponds to skill indices
            // This is a simplified version - proper implementation would need popup bounds
            var skills = _skillPopup.GetSkills();
            var fixedPos = _skillPopup.GetFixedPosition();
            
            // For now, just update popup hover (simplified)
            _skillPopup.UpdateHover(mouseX, mouseY);
            return;
        }
        
        // Update hovered keyword
        string? newHoveredKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (newHoveredKeyword != _narrationState.HoveredKeyword)
        {
            _narrationState.HoveredKeyword = newHoveredKeyword;
            // UI will re-render on next Update() call
        }
    }
    
    /// <summary>
    /// Handle mouse click event.
    /// </summary>
    public void OnMouseClick(int mouseX, int mouseY)
    {
        // If popup is visible, handle popup click (closes popup for now)
        if (_skillPopup.IsVisible)
        {
            _skillPopup.HandleClick(mouseX, mouseY);
            Console.WriteLine("Phase6ForestController: Popup closed");
            return;
        }
        
        // Check if clicked on a keyword
        string? clickedKeyword = _ui.GetHoveredKeyword(mouseX, mouseY);
        
        if (clickedKeyword != null && _narrationState.ThinkingAttemptsRemaining > 0)
        {
            Console.WriteLine($"Phase6ForestController: Clicked keyword: {clickedKeyword}");
            
            // Show thinking skill selection popup
            var thinkingSkills = _avatar.GetThinkingSkills();
            
            // Convert terminal coordinates to screen coordinates
            // Terminal cell (mouseX, mouseY) needs to be converted to pixel coordinates
            // Using the core's window to get cell size
            float borderHeight = _core.GetWindowBorderHeight();
            int cellSize = 16; // TODO: Get from terminal
            float screenX = mouseX * cellSize;
            float screenY = mouseY * cellSize + borderHeight;
            
            _skillPopup.Show(new Vector2(screenX, screenY), thinkingSkills);
            Console.WriteLine($"Phase6ForestController: Showing {thinkingSkills.Count} thinking skills");
        }
    }
    
    /// <summary>
    /// Handle mouse wheel scroll event.
    /// TODO: Wire this up in GlyphSphereCore when mouse wheel events are added.
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
    public Phase6NarrationState GetState() => _narrationState;
}
