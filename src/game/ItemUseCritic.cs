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
/// The Item-Use Critic: a small LLM scoped to one question — whether a combined item can serve the
/// action it was offered to, and to what degree. It answers via enum-choice decision trees
/// (GBNF-constrained), picking one choice per node. Stateless — the slot is reset after every
/// evaluation.
///
/// <para>The degree it returns is not a verdict: <c>ToolCombinationRules</c> decides which degrees
/// the acting body's hands can act upon. The critic judges the implement and is told nothing about
/// who holds it, which is what keeps its question answerable.</para>
///
/// This is all the critic does now. Plausibility, difficulty, wounds, witnesses and threats moved out:
/// to the modus-mentis persona-fit enum, to per-verb penalty lists, and to deterministic coded rules.
/// </summary>
public class ItemUseCritic : IDisposable
{
    private readonly LlamaServerManager _llamaServer;
    private int _criticSlotId = -1;
    private bool _isInitialized = false;

    private int _totalEvaluations = 0;
    private double _totalDurationMs = 0;

    public ItemUseCritic(LlamaServerManager llamaServer)
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
        catch (Exception ex) { Console.Error.WriteLine($"ItemUseCritic: Failed to save trace: {ex.Message}"); }

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
                chosenId = debugOverride;
            else if (node.Counterweight != null && !PlaygroundMode.IsActive)
                chosenId = await WeighAsync(node);
            else
                chosenId = await GetChoiceAsync(node.Question, node.Choices);

            nodeResult.ChosenId = chosenId;

            // No reason is asked for: a refusal's reason is its choice's own fixed sentence
            // (CriticTreeResult.CombinedFailureReason reads ErrorMessage). See CriticTrees.CannotReason.
            var choiceObj = node.Choices.FirstOrDefault(c => c.Id == chosenId);
            nodeResult.IsFailure = choiceObj?.IsFailure ?? false;
            nodeResult.ErrorMessage = choiceObj?.ErrorMessage ?? string.Empty;

            _totalEvaluations++;
        }
        catch (Exception ex)
        {
            // Fallback: pick first non-failure choice
            var fallback = node.Choices.FirstOrDefault(c => !c.IsFailure) ?? node.Choices[0];
            nodeResult.ChosenId = fallback.Id;
            nodeResult.IsFailure = false;
            nodeResult.ErrorMessage = $"Evaluation error (fallback to '{fallback.Id}'): {ex.Message}";
            Console.Error.WriteLine($"ItemUseCritic: Error evaluating '{node.Name}': {ex.Message}");
        }

        sw.Stop();
        nodeResult.DurationMs = sw.Elapsed.TotalMilliseconds;
        return nodeResult;
    }

    /// <summary>
    /// Calls the LLM with the question and a formatted choices list.
    /// GBNF constrains the output to exactly one of the choice ids.
    /// Returns the trimmed chosen id.
    /// In playground mode, picks randomly from the available choices.
    /// </summary>
    private async Task<string> GetChoiceAsync(string question, List<CriticChoice> choices)
    {
        if (PlaygroundMode.IsActive)
            return PlaygroundMode.Pick(choices).Id;

        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
        {
            Console.Error.WriteLine("ItemUseCritic: Not initialized or server not ready");
            return choices.FirstOrDefault(c => !c.IsFailure)?.Id ?? choices[0].Id;
        }

        string prompt = BuildChoicePrompt(question, choices);
        string grammar = BuildGrammar(choices);

        string result = await _llamaServer.GenerateConstrainedStringAsync(
            _criticSlotId, prompt, grammar, maxTokens: 20, skipReset: false);

        // Validate the returned id is one of the expected choices
        result = result.Trim();
        if (!choices.Any(c => c.Id == result))
        {
            Console.Error.WriteLine($"ItemUseCritic: LLM returned unexpected id '{result}', falling back to first choice.");
            return choices[0].Id;
        }

        return result;
    }

    /// <summary>
    /// The log-odds of help against refusal above which the two questions' summed verdict passes.
    /// Zero, and deliberately not tuned: the sum of two log-odds is already the weighing, and a fitted
    /// constant here would be fitted to the bench rather than to anything about the game.
    /// <c>tools/prompts/critic_bench.py</c> reads it from here.
    /// </summary>
    public const double HelpLogOdds = 0.0;

    /// <summary>
    /// A node with a <see cref="CriticNode.Counterweight"/>: both questions read rather than sampled,
    /// their log-odds of help against refusal summed for the side, and the node's own reading giving
    /// the id on that side — the degree, or the refusal.
    ///
    /// <para><b>Why two questions.</b> Asked alone, the node's question ("compared to bare hands, does X
    /// give a meaningful advantage?") refused a third of the sensible improvisations of the survey in
    /// <c>tools/prompts/critic_bench.py</c> — a dagger driven into a stone wall's gaps, a shield as a
    /// sled — and almost never passed a wrong one. Asked alone, the counterweight ("could X do any real
    /// part of this, even clumsily?") let the improvisations through and a fifth of the wrong ones with
    /// them: a small model can always imagine a clumsy use. They err on different cases, so the sum
    /// keeps what each is right about: an item passes when the lenient question is surer of it than the
    /// strict one is against it. The lenient question gives no id, because "even clumsily" drew nearly
    /// all its answers to the clumsy degree. See <c>tools/prompts/NOTES.md</c>, "The item critic and
    /// improvised tools".</para>
    ///
    /// <para><b>Why read, not sampled.</b> One sample at temperature 0.7 is one draw: on a quarter of
    /// the survey the same item passed one time and failed the next. The first token's
    /// log-probabilities give the whole distribution in the same forward pass, so the verdict is the
    /// same every time. Ids, not letters: shown as letters A–E, the options came back "D" for most
    /// cases, a hammer to break a door among them. The two questions share their text up to the
    /// question itself, so the server reads the second from its prompt cache but for its last lines.</para>
    /// </summary>
    private async Task<string> WeighAsync(CriticNode node)
    {
        if (!_isInitialized || !_llamaServer.IsServerReady || _criticSlotId < 0)
        {
            Console.Error.WriteLine("ItemUseCritic: Not initialized or server not ready");
            return node.Choices.FirstOrDefault(c => !c.IsFailure)?.Id ?? node.Choices[0].Id;
        }

        var weightNode = node.Counterweight!;
        var strict  = await ReadAsync(node.Question, node.Choices);
        var lenient = await ReadAsync(weightNode.Question, weightNode.Choices);

        double margin = strict.Margin + lenient.Margin - HelpLogOdds;
        bool help = margin > 0;
        string id = strict.Help == help ? strict.Answer : Likeliest(node.Choices, strict.LogProbs, failure: !help);

        Console.WriteLine($"ItemUseCritic: {node.Name} -> {id} (strict {F(strict.Margin)} '{strict.Answer}', " +
                          $"lenient {F(lenient.Margin)} '{lenient.Answer}', sum {F(margin)} vs {F(HelpLogOdds)})");
        return id;
    }

    private const double AbsentLogProb = -30.0;

    private readonly record struct Reading(string Answer, bool Help, double Margin, double[] LogProbs);

    /// <summary>
    /// One question, read: each id's score is that of its first token (as the model's tokenizer splits it)
    /// among the reply's likeliest first tokens; the margin is the log-odds of the help ids' first tokens
    /// against the refusals' (a first token shared by two ids counted once); the answer is the greedy
    /// reply when it is an id on the side the margin chose, else that side's likeliest.
    /// </summary>
    private async Task<Reading> ReadAsync(string question, List<CriticChoice> choices)
    {
        var (text, top) = await _llamaServer.GetGreedyReplyWithLogProbsAsync(_criticSlotId, BuildChoicePrompt(question, choices));
        var pieces = new string[choices.Count];
        var lp = new double[choices.Count];
        for (int i = 0; i < choices.Count; i++)
        {
            pieces[i] = await _llamaServer.FirstTokenPieceAsync(choices[i].Id);
            lp[i] = AbsentLogProb;
            foreach (var (token, logProb) in top)
            {
                string t = token.Trim();
                if (t.Length > 0 && t == pieces[i] && logProb > lp[i]) lp[i] = logProb;
            }
        }

        var seen = new HashSet<(string, bool)>();
        var help = new List<double>();
        var refuse = new List<double>();
        for (int i = 0; i < choices.Count; i++)
            if (seen.Add((pieces[i], choices[i].IsFailure)))
                (choices[i].IsFailure ? refuse : help).Add(lp[i]);
        double margin = LogSumExp(help) - LogSumExp(refuse);

        bool side = margin > 0;
        text = text.Trim();
        string answer = choices.Any(c => c.Id == text && c.IsFailure != side)
            ? text
            : Likeliest(choices, lp, failure: !side);
        return new Reading(answer, side, margin, lp);
    }

    private static string Likeliest(List<CriticChoice> choices, double[] lp, bool failure)
    {
        int best = -1;
        for (int i = 0; i < choices.Count; i++)
            if (choices[i].IsFailure == failure && (best < 0 || lp[i] > lp[best])) best = i;
        return choices[best].Id;
    }

    private static double LogSumExp(List<double> xs)
    {
        if (xs.Count == 0) return double.NegativeInfinity;
        double m = xs.Max();
        return m + Math.Log(xs.Sum(x => Math.Exp(x - m)));
    }

    private static string F(double x) => x.ToString("+0.0;-0.0", System.Globalization.CultureInfo.InvariantCulture);

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
            Console.WriteLine("ItemUseCritic: Initializing...");
            _criticSlotId = await _llamaServer.CreateInstanceAsync(GetCriticSystemPrompt());
            _isInitialized = true;
            Console.WriteLine($"ItemUseCritic: Created slot {_criticSlotId}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ItemUseCritic: Failed to initialize: {ex.Message}");
            LLMLogger.LogInstanceCreated(-1, "ItemUseCritic", false, ex.Message);
            _isInitialized = false;
        }
    }

    // There was a "Be strict but fair; value plausibility over generosity." line here. On 28 labelled
    // cases (tools/prompts/critic_bench.py, 5 samples each, shipped model) the critic read it as an
    // instruction to refuse: a sword offered to cut meat, a spear to fish, a sledgehammer at an ore
    // seam all came back cannot_serve, every time. Without it the plain tree went 96.0% -> 98.7%; the
    // substitution tree needed its question changed as well (see BuildToolSubstitutionTree). Nothing
    // that should be refused started passing: the feather, the loaf, the prayer book are still refused.
    private static string GetCriticSystemPrompt() => @"You are the ITEM-USE CRITIC. You judge only one thing:
- whether a tool or object meaningfully helps the character accomplish an action, and how well.

Guidelines:
- Reason concretely about the item's physical properties and how it would actually be used.
- Judge only the specific item question asked — not the wider wisdom of the action.
- Respond with EXACTLY one of the provided option ids — nothing else.";

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
            Console.WriteLine($"ItemUseCritic: {_totalEvaluations} evaluations, avg {_totalDurationMs / _totalEvaluations:F1}ms");
    }
}
