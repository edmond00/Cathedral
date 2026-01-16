using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// Orchestrates the observation phase of narration.
/// Generates "overall observation" (one keyword per outcome) and "focus observation" (all keywords from one outcome).
/// </summary>
public class ObservationPhaseController
{
    private readonly ObservationExecutor _observationExecutor;
    private readonly KeywordRenderer _keywordRenderer;
    private readonly Random _random = new();
    
    // Stores the keyword-to-outcome mapping from the last overall observation
    private Dictionary<string, ConcreteOutcome> _keywordToOutcomeMap = new();
    
    public ObservationPhaseController(LlamaServerManager llamaServer, SkillSlotManager slotManager)
    {
        _observationExecutor = new ObservationExecutor(llamaServer, slotManager);
        _keywordRenderer = new KeywordRenderer();
    }
    
    /// <summary>
    /// Executes the observation phase: selects ONE observation skill and generates an "overall observation".
    /// The overall observation uses one representative keyword from each possible outcome.
    /// Returns a single narration block ready for display.
    /// </summary>
    public async Task<List<NarrationBlock>> ExecuteObservationPhaseAsync(
        NarrationNode currentNode,
        Avatar avatar,
        int skillCount = 1)  // Default to 1 skill now
    {
        Console.WriteLine($"ObservationPhaseController: Starting overall observation phase for {currentNode.NodeId}");
        
        // Select ONE observation skill randomly
        var observationSkills = avatar.GetObservationSkills()
            .OrderBy(_ => _random.Next())
            .Take(1)
            .ToList();
        
        if (observationSkills.Count == 0)
        {
            Console.WriteLine("ObservationPhaseController: No observation skills available!");
            return new List<NarrationBlock>();
        }
        
        var skill = observationSkills[0];
        Console.WriteLine($"ObservationPhaseController: Selected observation skill: {skill.DisplayName}");
        
        // Get representative keywords (one per outcome)
        var representativeKeywords = currentNode.GetRepresentativeKeywordsPerOutcome(_random);
        var targetKeywords = representativeKeywords.Select(r => r.Keyword).ToList();
        
        // Build keyword-to-outcome map for later focus observations
        _keywordToOutcomeMap.Clear();
        foreach (var (keyword, outcome) in representativeKeywords)
        {
            _keywordToOutcomeMap[keyword.ToLowerInvariant()] = outcome;
        }
        
        Console.WriteLine($"ObservationPhaseController: Using {targetKeywords.Count} representative keywords: {string.Join(", ", targetKeywords)}");
        
        var narrationBlocks = new List<NarrationBlock>();
        
        try
        {
            Console.WriteLine($"ObservationPhaseController: Generating overall observation with {skill.DisplayName}...");
            
            var observation = await _observationExecutor.GenerateObservationAsync(
                skill,
                currentNode,
                avatar,
                targetKeywords
            );
            
            // Extract keywords from narration text
            var segments = _keywordRenderer.ParseNarrationWithKeywords(
                observation.NarrationText,
                targetKeywords
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
            
            Console.WriteLine($"ObservationPhaseController: Generated overall observation with {foundKeywords.Count} keywords");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Failed to generate overall observation with {skill.DisplayName}: {ex.Message}");
            
            // Add a fallback observation
            narrationBlocks.Add(new NarrationBlock(
                Type: NarrationBlockType.Observation,
                SkillName: skill.DisplayName,
                Text: "You observe the environment carefully, taking in the details.",
                Keywords: new List<string>(),
                Actions: null
            ));
        }
        
        Console.WriteLine($"ObservationPhaseController: Overall observation phase complete");
        
        return narrationBlocks;
    }
    
    /// <summary>
    /// Generates a "focus observation" for a specific outcome triggered by right-clicking a keyword.
    /// Uses all keywords from the outcome that owns the clicked keyword.
    /// </summary>
    /// <param name="clickedKeyword">The keyword that was right-clicked</param>
    /// <param name="observationSkill">The observation skill selected by the player</param>
    /// <param name="currentNode">The current narration node</param>
    /// <param name="avatar">The player avatar</param>
    /// <returns>A narration block for the focus observation, or null on failure</returns>
    public async Task<NarrationBlock?> GenerateFocusObservationAsync(
        string clickedKeyword,
        Skill observationSkill,
        NarrationNode currentNode,
        Avatar avatar)
    {
        Console.WriteLine($"ObservationPhaseController: Starting focus observation for keyword '{clickedKeyword}'");
        
        // Find the outcome that owns this keyword
        var outcome = currentNode.GetOutcomeOwningKeyword(clickedKeyword);
        
        if (outcome == null)
        {
            Console.Error.WriteLine($"ObservationPhaseController: No outcome found for keyword '{clickedKeyword}'");
            return null;
        }
        
        // Get keywords specific to this outcome
        // For NarrationNodes, use NodeKeywords (not the aggregated OutcomeKeywords which includes children)
        // For Items, use OutcomeKeywords directly
        List<string> focusKeywords;
        if (outcome is NarrationNode node)
        {
            focusKeywords = node.NodeKeywords;
            Console.WriteLine($"ObservationPhaseController: Focus observation targeting {focusKeywords.Count} NodeKeywords from NarrationNode '{outcome.DisplayName}': {string.Join(", ", focusKeywords)}");
        }
        else
        {
            focusKeywords = outcome.OutcomeKeywords;
            Console.WriteLine($"ObservationPhaseController: Focus observation targeting {focusKeywords.Count} OutcomeKeywords from outcome '{outcome.DisplayName}': {string.Join(", ", focusKeywords)}");
        }
        
        try
        {
            var observation = await _observationExecutor.GenerateObservationAsync(
                observationSkill,
                currentNode,
                avatar,
                focusKeywords
            );
            
            // Extract keywords from narration text
            var segments = _keywordRenderer.ParseNarrationWithKeywords(
                observation.NarrationText,
                focusKeywords
            );
            
            var foundKeywords = segments
                .Where(s => s.IsKeyword)
                .Select(s => s.KeywordValue!)
                .Distinct()
                .ToList();
            
            var block = new NarrationBlock(
                Type: NarrationBlockType.Observation,
                SkillName: observationSkill.DisplayName,
                Text: observation.NarrationText,
                Keywords: foundKeywords,
                Actions: null
            );
            
            Console.WriteLine($"ObservationPhaseController: Generated focus observation with {foundKeywords.Count} keywords");
            
            return block;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ObservationPhaseController: Failed to generate focus observation: {ex.Message}");
            
            // Return fallback observation
            return new NarrationBlock(
                Type: NarrationBlockType.Observation,
                SkillName: observationSkill.DisplayName,
                Text: $"You focus your attention on the {clickedKeyword}, examining it more closely.",
                Keywords: new List<string> { clickedKeyword },
                Actions: null
            );
        }
    }
    
    /// <summary>
    /// Gets the outcome associated with a keyword from the current observation context.
    /// </summary>
    public ConcreteOutcome? GetOutcomeForKeyword(string keyword)
    {
        var normalizedKeyword = keyword.ToLowerInvariant();
        return _keywordToOutcomeMap.TryGetValue(normalizedKeyword, out var outcome) ? outcome : null;
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
