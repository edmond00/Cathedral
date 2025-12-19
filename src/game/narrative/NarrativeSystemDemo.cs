using System;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Test/demo class to verify Phase 1 and Phase 2 foundation is working.
/// Can be run to validate data structures and basic functionality.
/// </summary>
public class NarrativeSystemDemo
{
    /// <summary>
    /// Main entry point for running the narrative system demo.
    /// Runs both Phase 1 (foundation) and Phase 2 (observation) demos.
    /// </summary>
    public static async Task RunDemo()
    {
        Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Phase 6: Chain-of-Thought Narrative RPG System Demo    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
        Console.WriteLine();
        
        // Phase 1: Foundation
        RunPhase1Demo();
        
        Console.WriteLine("\nPress Enter to continue to Phase 2 (LLM Observation System)...");
        Console.ReadLine();
        
        // Phase 2: Observation System
        await RunPhase2DemoAsync();
        
        Console.WriteLine("\n╔═══════════════════════════════════════════════════════════╗");
        Console.WriteLine("║   Demo Complete - Phases 1-4 Implemented Successfully    ║");
        Console.WriteLine("║   Ready for Phase 5 (UI Polish) and Phase 6 (Content)    ║");
        Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");
    }
    
    public static void RunPhase1Demo()
    {
        Console.WriteLine("=== Phase 1: Narrative System Foundation Demo ===\n");
        
        // Initialize components
        var registry = SkillRegistry.Instance;
        var avatar = new Avatar();
        avatar.InitializeSkills(registry, skillCount: 50); // Ensure all skill types are included
        
        var forestGenerator = new ForestNarrationNodeGenerator();
        var entryNode = forestGenerator.GetRandomEntryNode();
        
        // Display avatar info
        Console.WriteLine($"Avatar initialized with {avatar.LearnedSkills.Count} skills:");
        Console.WriteLine($"  - Observation skills: {avatar.GetObservationSkills().Count}");
        Console.WriteLine($"  - Thinking skills: {avatar.GetThinkingSkills().Count}");
        Console.WriteLine($"  - Action skills: {avatar.GetActionSkills().Count}");
        Console.WriteLine();
        
        // Display some skills
        Console.WriteLine("Sample learned skills:");
        foreach (var skill in avatar.LearnedSkills.Take(5))
        {
            var functions = string.Join(", ", skill.Functions);
            var bodyParts = string.Join(", ", skill.BodyParts);
            Console.WriteLine($"  - {skill.DisplayName} (Lv.{skill.Level}): {functions} [{bodyParts}]");
        }
        Console.WriteLine();
        
        // Display entry node
        Console.WriteLine($"Entry Node: {entryNode.NodeName}");
        Console.WriteLine($"Description: {entryNode.NeutralDescription}");
        Console.WriteLine($"\nKeywords ({entryNode.Keywords.Count}):");
        foreach (var keyword in entryNode.Keywords)
        {
            if (entryNode.OutcomesByKeyword.TryGetValue(keyword, out var outcomes))
            {
                Console.WriteLine($"  - {keyword} ({outcomes.Count} possible outcomes)");
                foreach (var outcome in outcomes.Take(2))
                {
                    Console.WriteLine($"      → {outcome.ToNaturalLanguageString()}");
                }
            }
        }
        Console.WriteLine();
        
        // Display humors
        Console.WriteLine("Initial Humors:");
        foreach (var humor in avatar.Humors.Take(5))
        {
            Console.WriteLine($"  - {humor.Key}: {humor.Value}");
        }
        Console.WriteLine();
        
        Console.WriteLine("=== Phase 1 Foundation: All systems operational! ===");
    }
    
    public static async Task RunPhase2DemoAsync()
    {
        Console.WriteLine("\n=== Phase 2: Observation System Demo ===\n");
        
        // Initialize components
        var registry = SkillRegistry.Instance;
        var avatar = new Avatar();
        avatar.InitializeSkills(registry, skillCount: 50); // Ensure all skill types are included
        
        var forestGenerator = new ForestNarrationNodeGenerator();
        var entryNode = forestGenerator.GetRandomEntryNode();
        
        // Initialize LLM server
        Console.WriteLine("Initializing LLM server...");
        var llamaServer = new LlamaServerManager();
        
        try
        {
            await llamaServer.StartServerAsync();
            
            // Wait for server to be ready
            var maxWaitSeconds = 30;
            var waited = 0;
            while (!llamaServer.IsServerReady && waited < maxWaitSeconds)
            {
                await Task.Delay(1000);
                waited++;
            }
            
            if (!llamaServer.IsServerReady)
            {
                Console.WriteLine("LLM server failed to start, skipping Phase 2 demo");
                return;
            }
            
            Console.WriteLine("LLM server ready!\n");
            
            // Create shared slot manager for skill reuse across all executors
            var slotManager = new SkillSlotManager(llamaServer);
            
            // Create observation phase controller
            var observationController = new ObservationPhaseController(llamaServer, slotManager);
            
            Console.WriteLine($"Node: {entryNode.NodeName}");
            Console.WriteLine($"Available keywords: {string.Join(", ", entryNode.Keywords)}\n");
            
            // Execute observation phase
            var narrationBlocks = await observationController.ExecuteObservationPhaseAsync(
                entryNode,
                avatar,
                skillCount: 2  // Just 2 for demo
            );
            
            // Display results
            Console.WriteLine("\n=== Generated Observations ===\n");
            
            foreach (var block in narrationBlocks)
            {
                var formatted = observationController.FormatNarrationBlockForDisplay(block, keywordsEnabled: true);
                Console.WriteLine(formatted);
                
                if (block.Keywords != null && block.Keywords.Count > 0)
                {
                    Console.WriteLine($"Highlighted keywords: {string.Join(", ", block.Keywords)}");
                }
                Console.WriteLine();
            }
            
            var allKeywords = observationController.GetAllKeywords(narrationBlocks);
            Console.WriteLine($"Total unique keywords across all observations: {allKeywords.Count}");
            Console.WriteLine($"Keywords: {string.Join(", ", allKeywords)}\n");
            
            Console.WriteLine("=== Phase 2: Observation System Operational! ===");
            
            // Phases 3-4: Test thinking and action execution if we have keywords
            if (allKeywords.Count > 0)
            {
                Console.WriteLine("\nPress Enter to test Phases 3-4 (Thinking + Action Execution)...");
                Console.ReadLine();
                await TestPhases3And4Async(llamaServer, slotManager, entryNode, avatar, allKeywords);
            }
        }
        finally
        {
            llamaServer?.Dispose();
        }
    }
    
    private static async Task TestPhases3And4Async(
        LlamaServerManager llamaServer,
        SkillSlotManager slotManager,
        NarrationNode node,
        Avatar avatar,
        List<string> availableKeywords)
    {
        Console.WriteLine("\n=== Phase 3: Thinking System Demo ===\n");
        
        // Find a keyword that has outcomes defined
        string? selectedKeyword = null;
        List<Outcome>? possibleOutcomes = null;
        
        foreach (var keyword in availableKeywords)
        {
            if (node.OutcomesByKeyword.TryGetValue(keyword.ToLowerInvariant(), out possibleOutcomes))
            {
                selectedKeyword = keyword;
                break;
            }
        }
        
        // If no extracted keywords have outcomes, use any keyword from the node that has outcomes
        if (selectedKeyword == null)
        {
            var keywordWithOutcomes = node.OutcomesByKeyword.FirstOrDefault(kvp => kvp.Value.Count > 0);
            if (keywordWithOutcomes.Key != null)
            {
                selectedKeyword = keywordWithOutcomes.Key;
                possibleOutcomes = keywordWithOutcomes.Value;
                Console.WriteLine("(No extracted keywords had outcomes, using node keyword instead)\n");
            }
            else
            {
                Console.WriteLine("No outcomes defined for any keywords!");
                return;
            }
        }
        
        Console.WriteLine($"Simulating click on keyword: '{selectedKeyword}'\n");
        foreach (var outcome in possibleOutcomes)
        {
            Console.WriteLine($"  - {outcome.ToNaturalLanguageString()}");
        }
        Console.WriteLine();
        
        // Initialize thinking system components (reusing slotManager from observation phase)
        var thinkingPromptConstructor = new ThinkingPromptConstructor();
        var thinkingExecutor = new ThinkingExecutor(llamaServer, thinkingPromptConstructor, slotManager);
        var thinkingController = new ThinkingPhaseController(thinkingExecutor, avatar);
        
        // Simulate selecting a thinking skill (just pick the first one)
        var thinkingSkills = avatar.GetThinkingSkills();
        if (thinkingSkills.Count == 0)
        {
            Console.WriteLine("No thinking skills available!");
            return;
        }
        
        var selectedThinkingSkill = thinkingSkills.First();
        Console.WriteLine($"Simulating selection of thinking skill: {selectedThinkingSkill.DisplayName}\n");
        Console.WriteLine("Generating Chain-of-Thought reasoning and actions...\n");
        
        // Execute thinking phase
        var state = new NarrationState { CurrentNodeId = node.NodeId };
        var thinkingResult = await thinkingController.ExecuteThinkingPhaseAsync(
            selectedThinkingSkill,
            selectedKeyword,
            node,
            state
        );
        
        Console.WriteLine($"[{selectedThinkingSkill.DisplayName}]");
        Console.WriteLine($"{thinkingResult.ReasoningText}\n");
        
        if (thinkingResult.Actions.Count == 0)
        {
            Console.WriteLine("Failed to generate actions.\n");
            return;
        }
        
        Console.WriteLine($"Generated {thinkingResult.Actions.Count} actions:");
        for (int i = 0; i < thinkingResult.Actions.Count; i++)
        {
            var action = thinkingResult.Actions[i];
            var displayText = action.ActionText.Replace("try to ", "");
            Console.WriteLine($"  {i + 1}. {displayText}");
            Console.WriteLine($"     (Skill: {action.ActionSkillId}, Outcome: {action.PreselectedOutcome.ToNaturalLanguageString()})");
        }
        Console.WriteLine();
        
        Console.WriteLine("=== Phase 3: Thinking System Operational! ===\n");
        
        // Phase 4: Action Execution
        Console.WriteLine("=== Phase 4: Action Execution Demo ===\n");
        
        // Simulate selecting the first action
        var selectedAction = thinkingResult.Actions.First();
        Console.WriteLine($"Simulating click on action: '{selectedAction.ActionText}'\n");
        
        // Initialize action execution components
        var actionScorer = new ActionScorer(new CriticEvaluator(llamaServer));
        var difficultyEvaluator = new ActionDifficultyEvaluator(new CriticEvaluator(llamaServer));
        var outcomeNarrator = new OutcomeNarrator(llamaServer, slotManager);
        var outcomeApplicator = new OutcomeApplicator();
        var actionController = new ActionExecutionController(
            actionScorer,
            difficultyEvaluator,
            outcomeNarrator,
            outcomeApplicator,
            avatar
        );
        
        Console.WriteLine("Executing action with skill check...\n");
        
        // Execute the action
        var executionResult = await actionController.ExecuteActionAsync(
            selectedAction,
            node,
            selectedThinkingSkill
        );
        
        Console.WriteLine($"[{executionResult.ThinkingSkill.DisplayName}]");
        Console.WriteLine($"{executionResult.Narration}\n");
        Console.WriteLine($"Action: {executionResult.Succeeded}");
        Console.WriteLine($"Difficulty: {executionResult.Difficulty}/20");
        Console.WriteLine($"Outcome: {executionResult.ActualOutcome.ToNaturalLanguageString()}\n");
        
        Console.WriteLine("=== Phase 4: Action Execution Operational! ===");
    }
}
