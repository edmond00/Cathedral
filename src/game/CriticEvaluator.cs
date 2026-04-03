using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game;

/// <summary>
/// LLM-based critic that evaluates game actions using enum-choice decision trees.
/// Each node presents a question with a constrained set of choices (GBNF-constrained).
/// The LLM picks one choice; the matching branch determines the next node.
/// Stateless — instance is reset after every evaluation.
/// </summary>
public class CriticEvaluator : IDisposable
{
    private readonly LlamaServerManager _llamaServer;
    private int _criticSlotId = -1;
    private bool _isInitialized = false;

    private int _totalEvaluations = 0;
    private double _totalDurationMs = 0;

    public CriticEvaluator(LlamaServerManager llamaServer)
    {
        _llamaServer = llamaServer ?? throw new ArgumentNullException(nameof(llamaServer));
    }

    /// <summary>
    /// Evaluates a decision tree rooted at the given node.
    /// Traverses by following the branch matching the LLM's chosen id at each node.
    /// Stops on a failure choice or when a branch leads to null (terminal).
    /// When <paramref name="continueOnFailure"/> is true, a failing choice is still
    /// recorded but traversal continues via the success branch so all nodes are visited.
    /// The result is still a failure if any node failed.
    /// </summary>
    public async Task<CriticTreeResult> EvaluateTreeAsync(CriticNode rootNode, bool continueOnFailure = false)
    {
        if (rootNode == null) throw new ArgumentNullException(nameof(rootNode));

        var result = new CriticTreeResult();
        var currentNode = rootNode;

        Console.WriteLine($"\n🌳 Critic Tree: {rootNode.Name}");

        while (currentNode != null)
        {
            Console.WriteLine($"  📍 {currentNode.Name}");

            var nodeResult = await EvaluateNodeAsync(currentNode);
            result.Trace.Add(nodeResult);

            Console.WriteLine($"     {nodeResult}");

            if (nodeResult.IsFailure && !continueOnFailure)
                break;

            // On failure with continueOnFailure: follow the success branch to reach the next node.
            string navigateId = (nodeResult.IsFailure && continueOnFailure)
                ? (currentNode.Choices.FirstOrDefault(c => !c.IsFailure)?.Id ?? nodeResult.ChosenId)
                : nodeResult.ChosenId;

            // Navigate to next node via branch map; null = terminal
            currentNode = currentNode.Branches.TryGetValue(navigateId, out var next) ? next : null;
            if (currentNode != null)
                Console.WriteLine($"     → {currentNode.Name}");
        }

        Console.WriteLine($"\n🌳 Done: {(result.OverallSuccess ? "SUCCESS ✓" : $"FAILURE ✗ — {result.FirstErrorMessage}")} ({result.TotalDurationMs:F0}ms)");

        try { SaveTreeTraceToFile(result, rootNode.Name); }
        catch (Exception ex) { Console.Error.WriteLine($"CriticEvaluator: Failed to save trace: {ex.Message}"); }

        return result;
    }

    /// <summary>Evaluates a single node: builds the prompt, calls the LLM, returns the chosen id.</summary>
    private async Task<CriticNodeResult> EvaluateNodeAsync(CriticNode node)
    {
        var sw = Stopwatch.StartNew();
        var nodeResult = new CriticNodeResult
        {
            NodeName = node.Name,
            Question = node.Question
        };

        try
        {
            bool isPlausibilityNode = node.Choices.Any(c => c.IsFailure);

            string chosenId;
            string? debugOverride = (DebugMode.IsActive && !DebugMode.IsAutoStrategy)
                ? DebugMode.GetCriticOverride(node.Name, node.Question, node.Choices, isPlausibilityNode)
                : null;

            if (debugOverride != null)
            {
                chosenId = debugOverride;
            }
            else
            {
                chosenId = await GetChoiceAsync(node.Question, node.Choices);
            }

            nodeResult.ChosenId = chosenId;

            var choiceObj = node.Choices.FirstOrDefault(c => c.Id == chosenId);
            nodeResult.IsFailure = choiceObj?.IsFailure ?? false;
            nodeResult.ErrorMessage = choiceObj?.ErrorMessage ?? string.Empty;

            if (nodeResult.IsFailure)
            {
                // Ask the critic why while its answer is still in context; resets slot afterwards
                nodeResult.FailureReason = await GetFailureReasonAsync();
                Console.WriteLine($"     Reason: {nodeResult.FailureReason}");
            }
            else
            {
                // No follow-up needed — reset now (skipReset=true was used in GetChoiceAsync)
                _llamaServer.ResetInstance(_criticSlotId);
            }

            _totalEvaluations++;
        }
        catch (Exception ex)
        {
            // Fallback: pick first non-failure choice
            var fallback = node.Choices.FirstOrDefault(c => !c.IsFailure) ?? node.Choices[0];
            nodeResult.ChosenId = fallback.Id;
            nodeResult.IsFailure = false;
            nodeResult.ErrorMessage = $"Evaluation error (fallback to '{fallback.Id}'): {ex.Message}";
            Console.Error.WriteLine($"CriticEvaluator: Error evaluating '{node.Name}': {ex.Message}");
        }

        sw.Stop();
        nodeResult.DurationMs = sw.Elapsed.TotalMilliseconds;
        return nodeResult;
    }

    /// <summary>
    /// Calls the LLM with the question and a formatted choices list.
    /// GBNF constrains the output to exactly one of the choice ids.
    /// Returns the trimmed chosen id.
    /// </summary>
    private async Task<string> GetChoiceAsync(string question, List<CriticChoice> choices)
    {
        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
        {
            Console.Error.WriteLine("CriticEvaluator: Not initialized or server not ready");
            return choices.FirstOrDefault(c => !c.IsFailure)?.Id ?? choices[0].Id;
        }

        string prompt = BuildChoicePrompt(question, choices);
        string grammar = BuildGrammar(choices);

        // skipReset=true so context is preserved for a follow-up "why" call if needed.
        // The caller (EvaluateNodeAsync) resets manually after the optional follow-up.
        string result = await _llamaServer.GenerateConstrainedStringAsync(
            _criticSlotId, prompt, grammar, maxTokens: 20, skipReset: true);

        // Validate the returned id is one of the expected choices
        result = result.Trim();
        if (!choices.Any(c => c.Id == result))
        {
            Console.Error.WriteLine($"CriticEvaluator: LLM returned unexpected id '{result}', falling back to first choice.");
            _llamaServer.ResetInstance(_criticSlotId);
            return choices[0].Id;
        }

        return result;
    }

    /// <summary>
    /// Asks the critic a follow-up "why" question while its previous answer is still in context.
    /// Resets the slot afterwards. Returns a short free-text sentence, or empty on failure.
    /// </summary>
    private async Task<string> GetFailureReasonAsync()
    {
        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
            return string.Empty;

        try
        {
            string reason = await _llamaServer.GenerateConstrainedStringAsync(
                _criticSlotId,
                "In one short sentence (around 12 words), explain to the character why you answered that way (address him directly in the second person).",
                gbnfGrammar: string.Empty,
                maxTokens: 60,
                skipReset: false); // reset after this call
            return reason.Trim();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CriticEvaluator: Failed to get failure reason: {ex.Message}");
            _llamaServer.ResetInstance(_criticSlotId);
            return string.Empty;
        }
    }

    private static string BuildChoicePrompt(string question, List<CriticChoice> choices)
    {
        var sb = new StringBuilder();
        sb.AppendLine(question);
        sb.AppendLine();
        sb.AppendLine("Choose one of the following options. Respond with ONLY the option id, nothing else:");
        foreach (var choice in choices)
        {
            if (!string.IsNullOrEmpty(choice.Description))
                sb.AppendLine($"- {choice.Id}: {choice.Description}");
            else
                sb.AppendLine($"- {choice.Id}");
        }
        return sb.ToString().TrimEnd();
    }

    private static string BuildGrammar(List<CriticChoice> choices)
    {
        var options = string.Join(" | ", choices.Select(c => $"\"{EscapeGbnf(c.Id)}\""));
        return $"root ::= response\nresponse ::= {options}";
    }

    private static string EscapeGbnf(string id)
    {
        // Escape backslash and double-quote for GBNF string literals
        return id.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>Initializes the Critic LLM slot. Must be called before use.</summary>
    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        try
        {
            Console.WriteLine("CriticEvaluator: Initializing...");
            _criticSlotId = await _llamaServer.CreateInstanceAsync(GetCriticSystemPrompt());
            _isInitialized = true;
            Console.WriteLine($"CriticEvaluator: Created slot {_criticSlotId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CriticEvaluator: Failed to initialize: {ex.Message}");
            LLMLogger.LogInstanceCreated(-1, "CriticEvaluator", false, ex.Message);
            _isInitialized = false;
        }
    }

    private static string GetCriticSystemPrompt() => @"You are a CRITIC evaluating game content for coherence and quality.

Your role is simple:
- Answer questions about game actions, consequences, and narratives
- Evaluate coherence, plausibility, and appropriateness
- Respond ONLY with one of the provided option ids — nothing else

Guidelines:
- Be strict but fair in your evaluations
- Consider logical consistency
- Value plausibility over creativity
- Focus on the specific question asked

You must respond with exactly one of the provided option ids.";

    private void SaveTreeTraceToFile(CriticTreeResult result, string treeName)
    {
        var sessionLogDir = _llamaServer.SessionLogDir;
        if (sessionLogDir == null || _criticSlotId < 0) return;

        var slotDir = Path.Combine(sessionLogDir, $"slot_{_criticSlotId}");
        if (!Directory.Exists(slotDir)) Directory.CreateDirectory(slotDir);

        var timestamp = DateTime.Now.ToString("HH-mm-ss-fff");
        var filePath = Path.Combine(slotDir, $"tree_trace_{timestamp}_{treeName}.txt");

        var sb = new StringBuilder();
        sb.AppendLine($"Critic Tree — {(result.OverallSuccess ? "✓ SUCCESS" : "✗ FAILURE")}");
        sb.AppendLine($"Tree: {treeName}");
        sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}");
        sb.AppendLine(new string('=', 80));
        sb.AppendLine();

        for (int i = 0; i < result.Trace.Count; i++)
        {
            var node = result.Trace[i];
            sb.AppendLine($"[{i + 1}] {node}");
            sb.AppendLine($"    Question: {node.Question}");
            sb.AppendLine();
        }

        sb.AppendLine(new string('=', 80));
        sb.AppendLine($"Total nodes: {result.Trace.Count}  |  Duration: {result.TotalDurationMs:F0}ms");
        if (!result.OverallSuccess)
            sb.AppendLine($"Failure: {result.FirstErrorMessage}");

        File.WriteAllText(filePath, sb.ToString());
        Console.WriteLine($"   Trace saved to: {filePath}");
    }

    public (int totalEvaluations, double totalDurationMs) GetStatistics() =>
        (_totalEvaluations, _totalDurationMs);

    public void Dispose()
    {
        if (_totalEvaluations > 0)
            Console.WriteLine($"CriticEvaluator: {_totalEvaluations} evaluations, avg {_totalDurationMs / _totalEvaluations:F1}ms");
    }
}
