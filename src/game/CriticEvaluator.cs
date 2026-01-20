using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game;

/// <summary>
/// LLM-based critic that evaluates coherence and quality using token probabilities.
/// Supports binary tree evaluation where each node is a yes/no question.
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
    /// Evaluates a binary tree of yes/no questions.
    /// Traverses the tree, evaluating each node until reaching a terminal state
    /// (no branch to follow after success or failure).
    /// </summary>
    /// <param name="rootNode">The root node of the decision tree</param>
    /// <returns>Complete evaluation result including trace of all nodes</returns>
    public async Task<CriticTreeResult> EvaluateTreeAsync(CriticNode rootNode)
    {
        if (rootNode == null)
            throw new ArgumentNullException(nameof(rootNode));
        
        var result = new CriticTreeResult();
        var currentNode = rootNode;
        
        Console.WriteLine($"\n🌳 Critic Tree Evaluation Starting...");
        
        while (currentNode != null)
        {
            Console.WriteLine($"  📍 Evaluating node: {currentNode.Name}");
            
            // Evaluate the current node
            var nodeResult = await EvaluateNodeAsync(currentNode);
            result.Trace.Add(nodeResult);
            
            Console.WriteLine($"     {nodeResult}");
            
            // Determine next node based on success/failure
            if (nodeResult.Success)
            {
                // Follow success branch if it exists
                currentNode = currentNode.SuccessBranch;
                if (currentNode != null)
                    Console.WriteLine($"     → Following success branch to: {currentNode.Name}");
            }
            else
            {
                // Follow failure branch if it exists
                currentNode = currentNode.FailureBranch;
                if (currentNode != null)
                    Console.WriteLine($"     → Following failure branch to: {currentNode.Name}");
            }
        }
        
        Console.WriteLine($"\n🌳 Tree Evaluation Complete: {(result.OverallSuccess ? "SUCCESS ✓" : "FAILURE ✗")}");
        if (!result.OverallSuccess)
        {
            Console.WriteLine($"   Failures: {result.FailureCount}");
            foreach (var error in result.AllErrorMessages)
            {
                Console.WriteLine($"   - {error}");
            }
        }
        Console.WriteLine($"   Total duration: {result.TotalDurationMs:F0}ms");
        
        // Save trace to file in critic slot folder
        try
        {
            SaveTreeTraceToFile(result, rootNode.Name);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CriticEvaluator: Failed to save trace: {ex.Message}");
        }
        
        return result;
    }
    
    /// <summary>
    /// Saves the tree trace to a file in the critic's slot folder.
    /// </summary>
    private void SaveTreeTraceToFile(CriticTreeResult result, string treeName)
    {
        var sessionLogDir = _llamaServer.SessionLogDir;
        if (sessionLogDir == null || _criticSlotId < 0)
            return;
        
        var slotDir = Path.Combine(sessionLogDir, $"slot_{_criticSlotId}");
        if (!Directory.Exists(slotDir))
            Directory.CreateDirectory(slotDir);
        
        var timestamp = DateTime.Now.ToString("HH-mm-ss-fff");
        var traceFileName = $"tree_trace_{timestamp}_{treeName}.txt";
        var traceFilePath = Path.Combine(slotDir, traceFileName);
        
        var sb = new StringBuilder();
        var status = result.OverallSuccess ? "✓ SUCCESS" : "✗ FAILURE";
        sb.AppendLine($"Critic Tree Evaluation - {status}");
        sb.AppendLine($"Tree: {treeName}");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();
        
        sb.AppendLine($"TRACE ({result.Trace.Count} nodes evaluated):");
        sb.AppendLine();
        
        for (int i = 0; i < result.Trace.Count; i++)
        {
            var node = result.Trace[i];
            var nodeStatus = node.Success ? "✓" : "✗";
            var answer = node.YesIsSuccess ? "yes=success" : "no=success";
            
            sb.AppendLine($"[{i + 1}] {nodeStatus} {node.NodeName}");
            sb.AppendLine($"    Question: {node.Question}");
            sb.AppendLine($"    Score: {node.Score:F4} (threshold: {node.Threshold:F2}, {answer})");
            sb.AppendLine($"    Probabilities: yes={node.ProbabilityYes:F4}, no={node.ProbabilityNo:F4}");
            sb.AppendLine($"    Duration: {node.DurationMs:F0}ms");
            
            if (!node.Success && !string.IsNullOrEmpty(node.ErrorMessage))
            {
                sb.AppendLine($"    Error: {node.ErrorMessage}");
            }
            sb.AppendLine();
        }
        
        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"SUMMARY:");
        sb.AppendLine($"  Total Nodes: {result.Trace.Count}");
        sb.AppendLine($"  Failures: {result.FailureCount}");
        sb.AppendLine($"  Final Result: {(result.FinalSuccess ? "Success" : "Failure")}");
        sb.AppendLine($"  Overall Success: {result.OverallSuccess}");
        sb.AppendLine($"  Total Duration: {result.TotalDurationMs:F0}ms");
        
        if (!result.OverallSuccess)
        {
            sb.AppendLine();
            sb.AppendLine($"  Failed Nodes: {string.Join(", ", result.FailedNodeNames)}");
            foreach (var error in result.AllErrorMessages)
            {
                sb.AppendLine($"  - {error}");
            }
        }
        
        File.WriteAllText(traceFilePath, sb.ToString());
        Console.WriteLine($"   Trace saved to: {traceFilePath}");
    }
    
    /// <summary>
    /// Evaluates a single CriticNode and returns the result.
    /// </summary>
    private async Task<CriticNodeResult> EvaluateNodeAsync(CriticNode node)
    {
        var sw = Stopwatch.StartNew();
        
        var nodeResult = new CriticNodeResult
        {
            NodeName = node.Name,
            Question = node.Question,
            Threshold = node.Threshold,
            YesIsSuccess = node.YesIsSuccess
        };
        
        try
        {
            // Get probabilities for yes/no
            var (pYes, pNo, score) = await GetYesNoProbabilitiesAsync(node.Question);
            
            nodeResult.ProbabilityYes = pYes;
            nodeResult.ProbabilityNo = pNo;
            nodeResult.Score = score;
            
            // Determine success based on YesIsSuccess flag and threshold
            if (node.YesIsSuccess)
            {
                // "yes" is success - score must meet threshold
                nodeResult.Success = score >= node.Threshold;
            }
            else
            {
                // "no" is success - inverted score (1 - score) must meet threshold
                nodeResult.Success = (1.0 - score) >= node.Threshold;
            }
            
            // Set error message if failed
            if (!nodeResult.Success)
            {
                nodeResult.ErrorMessage = !string.IsNullOrEmpty(node.ErrorMessage) 
                    ? node.ErrorMessage 
                    : $"Node '{node.Name}' check failed";
            }
        }
        catch (Exception ex)
        {
            nodeResult.Success = false;
            nodeResult.Score = 0.5;
            nodeResult.ErrorMessage = $"Evaluation error: {ex.Message}";
        }
        
        sw.Stop();
        nodeResult.DurationMs = sw.Elapsed.TotalMilliseconds;
        
        return nodeResult;
    }
    
    /// <summary>
    /// Gets the yes/no probabilities for a question.
    /// Returns (pYes, pNo, score) where score = pYes / (pYes + pNo).
    /// Resets the LLM instance after each call to maintain statelessness.
    /// </summary>
    private async Task<(double pYes, double pNo, double score)> GetYesNoProbabilitiesAsync(string question)
    {
        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
        {
            Console.Error.WriteLine("CriticEvaluator: Not initialized or server not ready");
            return (0.5, 0.5, 0.5);
        }
        
        try
        {
            var probabilities = await _llamaServer.GetNextTokenProbabilitiesAsync(
                _criticSlotId,
                question,
                constrainedTokens: new[] { "yes", "no" },
                gbnfGrammar: YesNoGrammar
            );
            
            double pYes = probabilities.GetValueOrDefault("yes", 0.0);
            double pNo = probabilities.GetValueOrDefault("no", 0.0);
            
            double total = pYes + pNo;
            double score = total > 0 ? pYes / total : 0.5;
            
            _totalEvaluations++;
            
            return (pYes, pNo, score);
        }
        finally
        {
            // CRITICAL: Reset conversation after each question to keep it stateless
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
            // The LlamaServerManager will normalize tokens (trim + lowercase) and also
            // store original tokens, so we'll capture variations like " yes", "Yes", etc.
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
