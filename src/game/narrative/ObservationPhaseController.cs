using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates the observation phase of narration.
/// Selects observation skills, generates narrations, and manages the flow.
/// </summary>
public class ObservationPhaseController
{
    private readonly ObservationExecutor _observationExecutor;
    private readonly KeywordRenderer _keywordRenderer;
    private readonly Random _random = new();
    
    public ObservationPhaseController(LlamaServerManager llamaServer, SkillSlotManager slotManager)
    {
        _observationExecutor = new ObservationExecutor(llamaServer, slotManager);
        _keywordRenderer = new KeywordRenderer();
    }
    
    /// <summary>
    /// Executes the observation phase: selects 2-3 observation skills and generates narrations.
    /// Returns list of narration blocks ready for display.
    /// </summary>
    public async Task<List<NarrationBlock>> ExecuteObservationPhaseAsync(
        NarrationNode currentNode,
        Avatar avatar,
        int skillCount = 3)
    {
        Console.WriteLine($"ObservationPhaseController: Starting observation phase for {currentNode.NodeId}");
        
        // Select 2-3 observation skills randomly
        var observationSkills = avatar.GetObservationSkills()
            .OrderBy(_ => _random.Next())
            .Take(skillCount)
            .ToList();
        
        if (observationSkills.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: No observation skills available!");
            return new List<NarrationBlock>();
        }
        
        Console.WriteLine($"ObservationPhaseController: Selected {observationSkills.Count} observation skills");
        
        var narrationBlocks = new List<NarrationBlock>();
        
        // Generate observation from each selected skill
        foreach (var skill in observationSkills)
        {
            try
            {
                Console.WriteLine($"ObservationPhaseController: Generating observation with {skill.DisplayName}...");
                
                var observation = await _observationExecutor.GenerateObservationAsync(
                    skill,
                    currentNode,
                    avatar
                );
                
                // Extract keywords from narration text
                var segments = _keywordRenderer.ParseNarrationWithKeywords(
                    observation.NarrationText,
                    currentNode.Keywords
                );
                
                var foundKeywords = segments
                    .Where(s => s.IsKeyword)
                    .Select(s => s.KeywordValue!)
                    .Distinct()
                    .ToList();
                
                var block = new NarrationBlock(
                    Type: NarrationBlockType.Observation,
                    SkillName: skill.DisplayName,
                    Text: observation.NarrationText,
                    Keywords: foundKeywords,
                    Actions: null
                );
                
                narrationBlocks.Add(block);
                
                Console.WriteLine($"ObservationPhaseController: Generated observation with {foundKeywords.Count} keywords");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ObservationPhaseController: Failed to generate observation with {skill.DisplayName}: {ex.Message}");
                
                // Add a fallback observation
                narrationBlocks.Add(new NarrationBlock(
                    Type: NarrationBlockType.Observation,
                    SkillName: skill.DisplayName,
                    Text: "You observe the environment carefully, taking in the details.",
                    Keywords: new List<string>(),
                    Actions: null
                ));
            }
        }
        
        Console.WriteLine($"ObservationPhaseController: Observation phase complete with {narrationBlocks.Count} blocks");
        
        return narrationBlocks;
    }
    
    /// <summary>
    /// Formats narration blocks for terminal display with keyword highlighting.
    /// </summary>
    public string FormatNarrationBlockForDisplay(NarrationBlock block, bool keywordsEnabled = true)
    {
        var formattedText = _keywordRenderer.FormatForTerminal(
            block.Text,
            block.Keywords ?? new List<string>(),
            keywordsEnabled
        );
        
        return $"[{block.SkillName}]\n{formattedText}\n";
    }
    
    /// <summary>
    /// Gets all unique keywords from a list of narration blocks.
    /// </summary>
    public List<string> GetAllKeywords(List<NarrationBlock> blocks)
    {
        return blocks
            .Where(b => b.Keywords != null)
            .SelectMany(b => b.Keywords!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
