using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Dialogue.Executors;

/// <summary>
/// Generates the NPC's opening greeting / subject introduction text.
/// Calls the NPC's own LLM slot (persona-as-system-prompt).
/// Schema: { "text": string }
/// </summary>
public class NpcGreetingExecutor
{
    private readonly LlamaServerManager _llmManager;

    public NpcGreetingExecutor(LlamaServerManager llmManager)
    {
        _llmManager = llmManager;
    }

    public async Task<string> GenerateGreetingAsync(
        NpcInstance npc,
        CancellationToken cancellationToken = default)
    {
        string prompt = BuildPrompt(npc.CurrentSubjectNode);
        var schema = new CompositeField("NpcGreeting",
            new StringField("text", MinLength: 20, MaxLength: 400,
                Hint: "What the NPC says to open the conversation or introduce the subject"));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? json = await RequestFromLLMAsync(npc.LlmSlotId, prompt, gbnf);

        if (string.IsNullOrWhiteSpace(json))
            return FallbackGreeting(npc);

        try
        {
            using var doc = JsonDocument.Parse(json);
            string text = doc.RootElement.GetProperty("text").GetString() ?? "";
            return string.IsNullOrWhiteSpace(text) ? FallbackGreeting(npc) : text;
        }
        catch (JsonException)
        {
            return FallbackGreeting(npc);
        }
    }

    private static string BuildPrompt(ConversationSubjectNode subject)
    {
        return $"""
            Current conversation subject: {subject.ContextDescription}.
            Speak in character. Address the traveler directly with a greeting or opening remark that fits this subject.
            Keep it to 1-3 sentences. Do not explain yourself — just speak as you would to a stranger walking up to your bar.
            """;
    }

    private static string FallbackGreeting(NpcInstance npc)
        => $"{npc.DisplayName} eyes you carefully but says nothing yet.";

    private async Task<string?> RequestFromLLMAsync(int slotId, string prompt, string grammar)
    {
        var tcs = new TaskCompletionSource<string>();
        var buffer = string.Empty;

        void OnToken(object? sender, TokenStreamedEventArgs e)
        {
            if (e.SlotId == slotId) buffer += e.Token;
        }

        void OnCompleted(object? sender, RequestCompletedEventArgs e)
        {
            if (e.SlotId != slotId) return;
            _llmManager.TokenStreamed -= OnToken;
            _llmManager.RequestCompleted -= OnCompleted;
            tcs.TrySetResult(e.WasCancelled ? string.Empty : buffer);
        }

        _llmManager.TokenStreamed += OnToken;
        _llmManager.RequestCompleted += OnCompleted;

        try
        {
            await _llmManager.ContinueRequestAsync(slotId, prompt, null, null, grammar);
            var result = await tcs.Task;
            await Task.Delay(100);
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"NpcGreetingExecutor: LLM request failed: {ex.Message}");
            _llmManager.TokenStreamed -= OnToken;
            _llmManager.RequestCompleted -= OnCompleted;
            return null;
        }
    }
}
