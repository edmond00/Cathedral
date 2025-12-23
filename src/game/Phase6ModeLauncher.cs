using System;
using System.Threading.Tasks;
using Cathedral.Engine;
using Cathedral.Terminal;
using Cathedral.LLM;
using Cathedral.Game.Narrative;
using Cathedral.Glyph;

namespace Cathedral.Game;

/// <summary>
/// Launcher for Phase 6 Chain-of-Thought narrative RPG system
/// Integrates with Location Travel Mode (world view → forest → Phase 6 narration)
/// </summary>
public static class Phase6ModeLauncher
{
    /// <summary>
    /// Launch Phase 6 in standalone mode (for testing)
    /// </summary>
    public static async Task LaunchStandaloneAsync(Camera camera, int windowWidth, int windowHeight, bool useLLM = true)
    {
        Console.WriteLine("\n=== Phase 6: Chain-of-Thought Narrative System ===");
        Console.WriteLine("Launching console demo mode...\n");
        
        // Use the existing console demo for standalone testing
        await NarrativeSystemDemo.RunDemo();
    }

    /// <summary>
    /// Launch Phase 6 from Location Travel Mode (when player enters forest)
    /// Returns the game controller to allow LocationTravelGameController to manage it.
    /// </summary>
    public static async Task<Phase6GameController?> LaunchFromLocationTravelAsync(
        GlyphSphereCore core,
        Avatar avatar,
        LlamaServerManager llamaServer)
    {
        Console.WriteLine("\n=== Entering Forest (Phase 6) ===");

        try
        {
            // Create UI components
            var terminal = core.Terminal;
            var popup = core.PopupTerminal;
            if (terminal == null || popup == null)
            {
                Console.WriteLine("ERROR: Terminal components not initialized");
                return null;
            }
            
            var uiRenderer = new Phase6UIRenderer(terminal);
            var thinkingPopup = new TerminalThinkingSkillPopup(popup, avatar.GetThinkingSkills());

            // Create phase controllers
            var slotManager = new SkillSlotManager(llamaServer);
            var observationController = new ObservationPhaseController(llamaServer, slotManager);
            
            // Create thinking system components
            var thinkingPromptConstructor = new ThinkingPromptConstructor();
            var thinkingExecutor = new ThinkingExecutor(llamaServer, thinkingPromptConstructor, slotManager);
            var thinkingController = new ThinkingPhaseController(thinkingExecutor, avatar);
            
            // Create action execution components
            var criticEvaluator = new CriticEvaluator(llamaServer);
            await criticEvaluator.InitializeAsync();
            var actionScorer = new ActionScorer(criticEvaluator);
            var difficultyEvaluator = new ActionDifficultyEvaluator(criticEvaluator);
            var outcomeNarrator = new OutcomeNarrator(llamaServer, slotManager);
            var outcomeApplicator = new OutcomeApplicator();
            var actionController = new ActionExecutionController(
                actionScorer,
                difficultyEvaluator,
                outcomeNarrator,
                outcomeApplicator,
                avatar
            );

            // Create Phase 6 game controller
            var gameController = new Phase6GameController(
                uiRenderer,
                thinkingPopup,
                avatar,
                observationController,
                thinkingController,
                actionController
            );

            // Enable terminal visibility
            terminal.Visible = true;

            // Start Phase 6 (generates initial observations)
            Console.WriteLine("Starting Phase 6 narrative...");
            await gameController.StartAsync();
            
            return gameController;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR in Phase 6: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            return null;
        }
    }
}
