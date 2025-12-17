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
    public static void RunPhase1Demo()
    {
        Console.WriteLine("=== Phase 1: Narrative System Foundation Demo ===\n");
        
        // Initialize components
        var registry = SkillRegistry.Instance;
        var avatar = new Avatar();
        avatar.InitializeSkills(registry, skillCount: 10); // Small set for demo
        
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
        avatar.InitializeSkills(registry, skillCount: 10);
        
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
            
            // Create observation phase controller
            var observationController = new ObservationPhaseController(llamaServer);
            
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
        }
        finally
        {
            llamaServer?.Dispose();
        }
    }
}
