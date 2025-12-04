using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game;

/// <summary>
/// LLM-based critic that evaluates coherence and quality using token probabilities.
/// Returns probability ratios rather than generating text.
/// Stateless - conversation is reset after each evaluation.
/// </summary>
public class CriticEvaluator : IDisposable
{
    private readonly LlamaServerManager _llamaServer;
    private int _criticSlotId = -1;
    private bool _isInitialized = false;
    
    // GBNF grammar for yes/no responses
    private const string YesNoGrammar = @"root ::= response
response ::= ""yes"" | ""no""";
    
    // Statistics
    private int _totalEvaluations = 0;
    private double _totalDurationMs = 0;
    
    public CriticEvaluator(LlamaServerManager llamaServer)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
    }
    
    /// <summary>
    /// Initializes the Critic LLM slot. Must be called before using the evaluator.
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;
        
        try
        {
            Console.WriteLine("CriticEvaluator: Initializing Critic slot...");
            
            var criticSystemPrompt = GetCriticSystemPrompt();
            _criticSlotId = await _llamaServer.CreateInstanceAsync(criticSystemPrompt);
            
            _isInitialized = true;
            Console.WriteLine($"CriticEvaluator: Created Critic slot {_criticSlotId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CriticEvaluator: Failed to initialize: {ex.Message}");
            LLMLogger.LogInstanceCreated(-1, "CriticEvaluator", false, ex.Message);
            _isInitialized = false;
        }
    }
    
    /// <summary>
    /// Evaluates if an action is coherent with a skill.
    /// Returns a probability ratio between 0.0 (not coherent) and 1.0 (fully coherent).
    /// </summary>
    /// <param name="action">The action text</param>
    /// <param name="skill">The skill name</param>
    /// <returns>Coherence score (0.0 to 1.0)</returns>
    public async Task<double> EvaluateActionSkillCoherence(string action, string skill)
    {
        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
        {
            Console.Error.WriteLine("CriticEvaluator: Not initialized or server not ready");
            return 0.5; // Neutral score as fallback
        }
        
        var question = $"Is the action '{action}' coherent with and appropriate for the skill '{skill}'?";
        return await EvaluateYesNoQuestion(question);
    }
    
    /// <summary>
    /// Evaluates if an action plausibly leads to a consequence.
    /// Returns a probability ratio between 0.0 (implausible) and 1.0 (very plausible).
    /// </summary>
    /// <param name="action">The action text</param>
    /// <param name="consequence">The consequence description</param>
    /// <returns>Plausibility score (0.0 to 1.0)</returns>
    public async Task<double> EvaluateActionConsequencePlausibility(string action, string consequence)
    {
        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
        {
            Console.Error.WriteLine("CriticEvaluator: Not initialized or server not ready");
            return 0.5; // Neutral score as fallback
        }
        
        var question = $"Could the action '{action}' plausibly lead to the consequence '{consequence}'?";
        return await EvaluateYesNoQuestion(question);
    }
    
    /// <summary>
    /// Evaluates narrative quality or appropriateness.
    /// Returns a probability ratio between 0.0 (poor quality) and 1.0 (high quality).
    /// </summary>
    /// <param name="narrative">The narrative text to evaluate</param>
    /// <param name="criterion">What to evaluate (e.g., "atmospheric", "concise", "coherent")</param>
    /// <returns>Quality score (0.0 to 1.0)</returns>
    public async Task<double> EvaluateNarrativeQuality(string narrative, string criterion)
    {
        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
        {
            Console.Error.WriteLine("CriticEvaluator: Not initialized or server not ready");
            return 0.5; // Neutral score as fallback
        }
        
        var question = $"Is this narrative {criterion}? \"{narrative}\"";
        return await EvaluateYesNoQuestion(question);
    }
    
    /// <summary>
    /// Generic yes/no question evaluator.
    /// Returns the probability ratio: p(yes) / (p(yes) + p(no))
    /// </summary>
    /// <param name="question">The yes/no question to evaluate</param>
    /// <returns>Score between 0.0 (no) and 1.0 (yes)</returns>
    public async Task<double> EvaluateYesNoQuestion(string question)
    {
        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
        {
            Console.Error.WriteLine("CriticEvaluator: Not initialized or server not ready");
            return 0.5; // Neutral score as fallback
        }
        
        var startTime = DateTime.Now;
        
        try
        {
            // Get token probabilities for "yes" and "no"
            var probabilities = await _llamaServer.GetNextTokenProbabilitiesAsync(
                _criticSlotId,
                question,
                constrainedTokens: new[] { "yes", "no" },
                gbnfGrammar: YesNoGrammar
            );
            
            double pYes = probabilities.GetValueOrDefault("yes", 0.0);
            double pNo = probabilities.GetValueOrDefault("no", 0.0);
            
            // Calculate ratio
            double total = pYes + pNo;
            double ratio = total > 0 ? pYes / total : 0.5;
            
            var duration = (DateTime.Now - startTime).TotalMilliseconds;
            _totalEvaluations++;
            _totalDurationMs += duration;
            
            // Log evaluation
            try 
            { 
                LLMLogger.LogCriticEvaluation(_criticSlotId, question, ratio, pYes, pNo, duration); 
            } 
            catch { /* Ignore logging errors */ }
            
            return ratio;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CriticEvaluator: Error evaluating question: {ex.Message}");
            return 0.5; // Neutral score on error
        }
        finally
        {
            // CRITICAL: Reset conversation to keep it stateless
            // This ensures each evaluation is independent
            try
            {
                _llamaServer.ResetInstance(_criticSlotId);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"CriticEvaluator: Error resetting instance: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Gets the Critic's system prompt.
    /// </summary>
    private static string GetCriticSystemPrompt()
    {
        return @"You are a CRITIC evaluating game content for coherence and quality.

Your role is simple:
- Answer yes/no questions about game actions, skills, consequences, and narratives
- Evaluate coherence, plausibility, and appropriateness
- Respond ONLY with 'yes' or 'no' - nothing else

Guidelines:
- Be strict but fair in your evaluations
- Consider logical consistency
- Value plausibility over creativity
- Focus on the specific question asked

You must answer with exactly one word: 'yes' or 'no'.";
    }
    
    /// <summary>
    /// Gets the current evaluation statistics.
    /// </summary>
    public (int totalEvaluations, double totalDurationMs) GetStatistics()
    {
        return (_totalEvaluations, _totalDurationMs);
    }
    
    /// <summary>
    /// Disposes the evaluator and logs statistics.
    /// </summary>
    public void Dispose()
    {
        if (_totalEvaluations > 0)
        {
            var avgDuration = _totalDurationMs / _totalEvaluations;
            Console.WriteLine($"CriticEvaluator: {_totalEvaluations} evaluations, avg duration: {avgDuration:F1}ms");
        }
    }
}
