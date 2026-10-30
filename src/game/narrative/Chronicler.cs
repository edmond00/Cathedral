using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cathedral.LLM;

namespace Cathedral.Game.Narrative;

/// <summary>
/// The chronicler: a background LLM session that works through the journal's queue of closed visits
/// and writes each up as one paragraph (<see cref="ChroniclerPrompt"/>).
///
/// <para><b>It must never slow the narration.</b> Every request goes through
/// <see cref="LlamaServerManager.GenerateBackgroundAsync"/>, which starts only after the foreground
/// has been quiet a while and is cancelled the instant a foreground request begins. A cancelled job
/// returns nothing and is simply started again later, from the start: the queue is the journal's
/// pending entries, so nothing about a job needs remembering, and a quit loses nothing.</para>
///
/// <para><b>All journal writes happen on the game thread</b>, in <see cref="Pump"/>: the generation
/// runs on a task over a snapshot of the group, and its result is applied when the next frame finds
/// it done. The journal is never touched from two threads.</para>
///
/// <para>Under <c>--playground</c> no request is made; a group is written up at once with a fixed
/// placeholder, which is what lets a script test the queue and the save.</para>
/// </summary>
public sealed class Chronicler : IDisposable
{
    private readonly Func<LlamaServerManager?> _llm;
    private readonly Func<Protagonist?> _protagonist;
    private readonly CancellationTokenSource _stop = new();
    private int _slot = -1;
    private Task<(int Group, string? Text)>? _job;

    public Chronicler(Func<LlamaServerManager?> llm, Func<Protagonist?> protagonist)
    {
        _llm = llm;
        _protagonist = protagonist;
    }

    /// <summary>Whether a job is out. For <c>inspect journal</c>.</summary>
    public bool Busy => _job != null;

    /// <summary>Called every frame on the game thread: applies a finished job, starts the next.</summary>
    public void Pump()
    {
        if (Config.Debug.ChroniclerDisabled) return;
        var journal = _protagonist()?.Journal;
        if (journal == null) return;

        if (_job != null)
        {
            if (!_job.IsCompleted) return;
            var job = _job;
            _job = null;
            if (job.IsCompletedSuccessfully && job.Result.Text is { Length: > 0 } text
                && journal.GroupEntries(job.Result.Group).Any(e => e.State == JournalEntryState.Pending))
                journal.Write(job.Result.Group, text);
            // A null or failed job leaves its group pending: it is taken up again from the start.
        }

        if (journal.NextPendingGroup() is not { } group) return;
        var entries = journal.GroupEntries(group);
        string? memory = journal.Entries.LastOrDefault(e => e.Group < group && e.Written.Length > 0)?.Written;

        if (PlaygroundMode.IsActive)
        {
            journal.Write(group, Placeholder(entries));
            return;
        }

        var llm = _llm();
        if (llm == null || !llm.IsServerReady) return;

        // Snapshot on this thread; the task sees copies only.
        var config = ChroniclerPrompt.Shipped;
        string message = ChroniclerPrompt.UserMessage(entries, config, memory);
        _job = Task.Run(() => Write(llm, group, message, config));
    }

    private async Task<(int, string?)> Write(LlamaServerManager llm, int group, string message, ChronicleConfig config)
    {
        try
        {
            if (_slot < 0)
                _slot = await llm.CreateInstanceAsync(ChroniclerPrompt.System[config.Voice], precache: false);
            string? reply = await llm.GenerateBackgroundAsync(_slot, message, config.MaxTokens, _stop.Token);
            return (group, reply == null ? null : ChroniclerPrompt.Clean(reply));
        }
        catch (OperationCanceledException) { return (group, null); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Chronicler: group {group} not written ({ex.GetType().Name}: {ex.Message}); it stays queued.");
            await Task.Delay(TimeSpan.FromSeconds(30));   // a failing server is not hammered
            return (group, null);
        }
    }

    /// <summary>The playground's stand-in: deterministic, and recognisable as not prose.</summary>
    private static string Placeholder(IReadOnlyList<JournalEntry> entries)
    {
        int n = entries.Count(e => e.Kind != JournalEntryKind.Milestone);
        var place = entries.FirstOrDefault(e => e.Place.Length > 0)?.Place.ToLowerInvariant();
        return $"[chronicle] {n} notes from {(place == null ? "the road" : "the " + place)}, written up.";
    }

    public void Dispose()
    {
        _stop.Cancel();
        _stop.Dispose();
    }
}
