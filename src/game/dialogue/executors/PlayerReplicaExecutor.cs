using System;
using System.Text.Json;
using System.Threading.Tasks;
using Cathedral.Game.Narrative;
using Cathedral.LLM;
using Cathedral.LLM.JsonConstraints;

namespace Cathedral.Game.Dialogue.Executors;

/// <summary>
/// Generates a single player dialogue replica from a specific ModusMentis perspective,
/// aimed at a specific conversation outcome.
/// Schema: { "replica": string }
/// The replica is written in direct first-person speech, to be shown in quotes.
/// </summary>
public class PlayerReplicaExecutor
{
    private readonly LlamaServerManager _llmManager;
    private readonly ModusMentisSlotManager _slotManager;

    public PlayerReplicaExecutor(LlamaServerManager llmManager, ModusMentisSlotManager slotManager)
    {
        _llmManager = llmManager;
        _slotManager = slotManager;
    }

    /// <summary>
    /// Generate a replica sentence.
    /// </summary>
    /// <param name="speakingSkill">The ModusMentis driving the replica's style.</param>
    /// <param name="subject">Current conversation subject.</param>
    /// <param name="targetOutcome">The outcome this replica is aiming toward.</param>
    /// <param name="npcPersonaTone">Short description of the NPC so the reply is targeted.</param>
    public async Task<string> GenerateReplicaAsync(
        ModusMentis speakingSkill,
        ConversationSubjectNode subject,
        ConversationOutcome targetOutcome,
        string npcPersonaTone)
    {
        int slotId = await _slotManager.GetOrCreateSlotForModusMentisAsync(speakingSkill);

        string prompt = BuildPrompt(speakingSkill, subject, targetOutcome, npcPersonaTone);

        var schema = new CompositeField("PlayerReplica",
            new StringField("replica", MinLength: 10, MaxLength: 250,
                Hint: "A short first-person direct speech sentence the protagonist says to the NPC. No quotation marks, no stage directions."));
        string gbnf = JsonConstraintGenerator.GenerateGBNF(schema);

        string? json = await RequestFromLLMAsync(slotId, prompt, gbnf);

        if (string.IsNullOrWhiteSpace(json))
            return FallbackReplica(subject);

        try
        {
            using var doc = JsonDocument.Parse(json);
            string replica = doc.RootElement.GetProperty("replica").GetString() ?? "";
            return string.IsNullOrWhiteSpace(replica) ? FallbackReplica(subject) : replica;
        }
        catch (JsonException)
        {
            return FallbackReplica(subject);
        }
    }

    private static string BuildPrompt(
        ModusMentis skill,
        ConversationSubjectNode subject,
        ConversationOutcome targetOutcome,
        string npcPersonaTone)
    {
        return $"""
            Conversation context: {subject.ContextDescription}.
            You are speaking to {npcPersonaTone}.
            Your persona: {skill.PersonaTone ?? skill.ShortDescription}.
            Goal of this reply: {targetOutcome.OutcomeHint}.

            Write a single short sentence of direct speech (1-2 sentences max) that the protagonist would say to the NPC.
            The sentence must sound like YOUR persona voice — not generic, but with your distinctive perspective.
            Do NOT include quotation marks. Do NOT narrate — only write the words spoken.
            """;
    }

    private static string FallbackReplica(ConversationSubjectNode subject)
        => $"I wanted to ask about {subject.ContextDescription}.";

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
            Console.WriteLine($"PlayerReplicaExecutor: LLM request failed: {ex.Message}");
            _llmManager.TokenStreamed -= OnToken;
            _llmManager.RequestCompleted -= OnCompleted;
            return null;
        }
    }
}
