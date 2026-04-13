using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.Game.Dialogue.Tree;
using Cathedral.Game.Narrative;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Dialogue.Runtime;

/// <summary>
/// Asks a modus mentis LLM to choose the best next branch for an intermediate node.
/// Only called when a node has more than one branch.
/// Schema: { "branch_index": int }
/// </summary>
public class MmBranchSelectorExecutor
{
    private readonly LlamaServerManager _llmManager;

    public MmBranchSelectorExecutor(LlamaServerManager llmManager) => _llmManager = llmManager;

    /// <summary>
    /// Returns the chosen branch index (0-based, clamped to valid range).
    /// Falls back to 0 on LLM failure.
    /// </summary>
    public async Task<int> ExecuteAsync(
        ModusMentis mm,
        int slotId,
        DialogueTreeNode node,
        string treeDescription,
        CancellationToken ct = default)
    {
        if (node.Branches.Count <= 1) return 0;

        var branchList = string.Join("\n", System.Linq.Enumerable.Range(0, node.Branches.Count)
            .Select(i => $"{i}: {node.Branches[i].TargetNode.Description}"));

        string prompt = $"""
            Dialogue subject: {treeDescription}.
            You are currently at: {node.Description}.
            Your perspective: {mm.PersonaTone ?? mm.ShortDescription}.

            Available next steps:
            {branchList}

            Choose the index of the next step that best fits your perspective and the dialogue subject.
            Reply with just the index number.
            """;

        // DigitField generates a single digit 0-9; we clamp to valid range after parsing
        var schema = new CompositeField("BranchChoice",
            new DigitField("branch_index", DigitCount: 1,
                Hint: "Index of the chosen branch (single digit)"));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? json = await RequestAsync(slotId, prompt, gbnf, ct);
        if (string.IsNullOrWhiteSpace(json)) return 0;

        try
        {
            using var doc = JsonDocument.Parse(json);
            int idx = doc.RootElement.GetProperty("branch_index").GetInt32();
            return Math.Clamp(idx, 0, node.Branches.Count - 1);
        }
        catch { return 0; }
    }

    private async Task<string?> RequestAsync(int slotId, string prompt, string grammar, CancellationToken ct)
    {
        var tcs    = new TaskCompletionSource<string>();
        var buffer = string.Empty;

        void OnToken(object? s, TokenStreamedEventArgs e)    { if (e.SlotId == slotId) buffer += e.Token; }
        void OnDone (object? s, RequestCompletedEventArgs e) {
            if (e.SlotId != slotId) return;
            _llmManager.TokenStreamed     -= OnToken;
            _llmManager.RequestCompleted  -= OnDone;
            tcs.TrySetResult(e.WasCancelled ? string.Empty : buffer);
        }

        _llmManager.TokenStreamed    += OnToken;
        _llmManager.RequestCompleted += OnDone;
        try
        {
            await _llmManager.ContinueRequestAsync(slotId, prompt, null, null, grammar);
            return await tcs.Task;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"MmBranchSelectorExecutor: {ex.Message}");
            _llmManager.TokenStreamed    -= OnToken;
            _llmManager.RequestCompleted -= OnDone;
            return null;
        }
    }
}
