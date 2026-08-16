using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Cathedral.Game;

namespace Cathedral.LLM;

/// <summary>
/// Manages the Llama server and provides an easy-to-use interface for LLM interactions
/// </summary>
public class LlamaServerManager : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private Process? _llamaProcess;
    private StreamWriter? _logWriter;
    private readonly Dictionary<int, LlamaInstance> _instances = new();
    private readonly object _slotLock = new();
    private int _nextSlotId = 0;
    private bool _isServerReady = false;
    private bool _disposed = false;
    private int _contextSize = 4096; // Context size per slot (--parallel 1)
    private string? _sessionLogDir = null; // Directory for this server session's logs

    /// <summary>
    /// Head-room reserved on top of a request's own <c>max_tokens</c> when bounding history against
    /// the slot context window. <see cref="LlamaInstance.EstimateConversationTokens"/> approximates a
    /// token as 4 characters, which undershoots real tokenization of this prose by a few percent;
    /// this absorbs that error rather than trimming to exactly the edge of the window.
    /// </summary>
    private const int ContextEstimateMargin = 128;

    /// <summary>
    /// Floor for the prompt budget, so a request asking for a large completion cannot drive the
    /// budget to zero and trim away history that would have fit.
    /// </summary>
    private const int MinPromptBudget = 256;

    // Loading progress tracking
    private DateTime _loadingStartTime = DateTime.MinValue;
    private volatile float _loadingProgress = 0f;
    private volatile string _loadingStatusMessage = "Starting...";
    
    // Events
    public event EventHandler<ServerStatusEventArgs>? ServerReady;
    public event EventHandler<TokenStreamedEventArgs>? TokenStreamed;
    public event EventHandler<RequestCompletedEventArgs>? RequestCompleted;
    /// <summary>Fired periodically while the model is loading. Provides progress 0 E and a status string.</summary>
    public event EventHandler<LoadingProgressEventArgs>? LoadingProgressUpdated;
    
    public bool IsServerReady => _isServerReady;
    /// <summary>Current model loading progress from 0.0 to 1.0. Reaches 1.0 only when fully ready.</summary>
    public float LoadingProgress => _loadingProgress;
    /// <summary>Human-readable description of the current loading stage.</summary>
    public string LoadingStatusMessage => _loadingStatusMessage;
    
    /// <summary>
    /// Gets the context size configured for the server and instances
    /// </summary>
    public int ContextSize => _contextSize;
    
    /// <summary>
    /// Gets the session log directory path (e.g., logs/llm_session_2026-01-20_09-42-30)
    /// </summary>
    public string? SessionLogDir => _sessionLogDir;
    
    /// <summary>
    /// Set once a log write has failed, so the reason is reported once rather than per request.
    /// </summary>
    private bool _loggingDisabled;

    /// <summary>
    /// Creates a directory for diagnostics, or returns null if it cannot be created.
    ///
    /// <para><b>Logging must never be able to stop the game working.</b> Every log path here is
    /// relative to the working directory, so an install the player cannot write to — extracted
    /// into Program Files, on read-only media, behind a locked-down profile — fails at the first
    /// <c>CreateDirectory</c>. That call used to sit unguarded inside the server-start try block,
    /// so the exception was caught as "Error starting server" and the game lost narration
    /// entirely: no LLM, and a message pointing at the wrong thing.</para>
    ///
    /// <para>Returning null instead degrades to silence. Every caller already treats a null log
    /// directory as "do not log", so the whole diagnostic tree switches off together and nothing
    /// downstream needs to know why.</para>
    ///
    /// <para>Reported once. A disk that has filled up would otherwise print per request, drowning
    /// the very output someone is reading to work out what went wrong.</para>
    /// </summary>
    private string? TryCreateLogDirectory(string path)
    {
        // A shipped build writes no logs/ tree at all — only log.txt, which the pumps below feed
        // llama-server's output into. Returning null here switches off every writer downstream,
        // because they all already test for a null session directory.
        if (!Config.Debug.VerboseFileLogging) return null;

        if (_loggingDisabled) return null;

        try
        {
            Directory.CreateDirectory(path);
            return path;
        }
        catch (Exception ex)
        {
            NoteLoggingFailure($"cannot create '{path}'", ex);
            return null;
        }
    }

    /// <summary>
    /// Turns diagnostics off for the rest of the session and says so once. Shared by
    /// <see cref="TryCreateLogDirectory"/> and by the per-request log blocks, so a disk that fills
    /// up mid-session reports a single line rather than one per LLM call.
    /// </summary>
    private void NoteLoggingFailure(string what, Exception ex)
    {
        if (_loggingDisabled) return;
        _loggingDisabled = true;
        Console.Error.WriteLine(
            $"LLM logging disabled: {what} ({ex.Message}). " +
            "The game runs normally; only diagnostics are lost. This usually means the game " +
            "folder is not writable — move the install somewhere it is if you need the logs.");
    }

    // Helper methods for logging

    /// <summary>
    /// Checks if an error message indicates a context length overflow
    /// </summary>
    private bool IsContextLengthError(string errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage)) return false;
        
        var lowerMessage = errorMessage.ToLower();
        return lowerMessage.Contains("context") && lowerMessage.Contains("length") ||
               lowerMessage.Contains("context") && lowerMessage.Contains("size") ||
               lowerMessage.Contains("exceeds") && lowerMessage.Contains("context") ||
               lowerMessage.Contains("too long") ||
               lowerMessage.Contains("max context") ||
               lowerMessage.Contains("context window");
    }
    
    private async Task LogErrorAsync(string message)
    {
        Console.Error.WriteLine(message);
        if (_logWriter != null)
        {
            try
            {
                await _logWriter.WriteLineAsync($"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}");
                await _logWriter.FlushAsync();
            }
            catch { /* Ignore log write errors */ }
        }
    }
    
    private void LogError(string message)
    {
        Console.Error.WriteLine(message);
        if (_logWriter != null)
        {
            try
            {
                _logWriter.WriteLine($"[ERROR] {DateTime.Now:HH:mm:ss.fff} {message}");
                _logWriter.Flush();
            }
            catch { /* Ignore log write errors */ }
        }
    }
    
    private async Task LogWarningAsync(string message)
    {
        Console.WriteLine($"WARNING: {message}");
        if (_logWriter != null)
        {
            try
            {
                await _logWriter.WriteLineAsync($"[WARNING] {DateTime.Now:HH:mm:ss.fff} {message}");
                await _logWriter.FlushAsync();
            }
            catch { /* Ignore log write errors */ }
        }
    }
    
    private void LogWarning(string message)
    {
        Console.WriteLine($"WARNING: {message}");
        if (_logWriter != null)
        {
            try
            {
                _logWriter.WriteLine($"[WARNING] {DateTime.Now:HH:mm:ss.fff} {message}");
                _logWriter.Flush();
            }
            catch { /* Ignore log write errors */ }
        }
    }
    
    // ── Request tracing, for the crash report ────────────────────────────────────

    /// <summary>
    /// One HTTP request to the server, as the crash report describes it.
    ///
    /// <para>This exists because the failure it was written for could only be diagnosed by
    /// correlating our own log against llama-server's <c>--verbose</c> output and noticing which of
    /// two lines came first. Recording what we did ourselves — whether the previous stream was read
    /// to its end, how long the connection had been idle, how many requests preceded it — turns that
    /// archaeology into a table.</para>
    /// </summary>
    private sealed class RequestTrace
    {
        public int Seq;
        public int Slot;
        public bool Streaming;
        public int MaxTokens;
        public DateTime StartedUtc;
        public double IdleGapMs;          // since the previous request finished: the keep-alive question
        public double DurationMs;
        public string Outcome = "in flight";
        public bool SawDone;
        public bool DrainedToEof;         // false here on a streaming request is the bug this replaced
        public bool DrainTimedOut;
        public bool Retried;          // the send was re-issued after a dead connection
        public int TailBytesAfterDone;
        public int MalformedChunks;
        public int ReplyChars;

        public override string ToString()
        {
            var detail = Streaming
                ? $"done={(SawDone ? "y" : "n")} eof={(DrainedToEof ? "y" : "n")} tail={TailBytesAfterDone}B" +
                  (DrainTimedOut ? " DRAIN-TIMEOUT" : "") +
                  (MalformedChunks > 0 ? $" malformed={MalformedChunks}" : "")
                : "buffered";
            return $"  #{Seq,-4} {StartedUtc:HH:mm:ss.fff} slot={Slot,-2} " +
                   $"{(Streaming ? "stream" : "oneshot")} maxTok={MaxTokens,-4} " +
                   $"idle={IdleGapMs,7:F0}ms dur={DurationMs,7:F0}ms reply={ReplyChars,4}c {detail}" +
                   (Retried ? " RETRIED" : "") + $"  → {Outcome}";
        }
    }

    /// <summary>
    /// How many recent requests the crash report carries. The interesting window is small — the
    /// failure this was written for was caused by the request immediately before it — but a few more
    /// show whether the run was healthy up to that point.
    /// </summary>
    private const int RecentRequestsKept = 16;

    /// <summary>
    /// How long the tail of a streamed response is given to arrive after <c>[DONE]</c>. Generous —
    /// it is a few bytes already on the wire — and its only job is to stop a server that never closes
    /// the stream from turning the drain into a ten-minute hang.
    /// </summary>
    private static readonly TimeSpan DrainAfterDoneTimeout = TimeSpan.FromSeconds(5);

    private readonly Queue<RequestTrace> _recentRequests = new();
    private readonly object _traceLock = new();
    private int _requestSeq;
    private DateTime _lastRequestEndedUtc = DateTime.MinValue;
    private int _failedRequests;

    /// <summary>
    /// The server's own <c>Keep-Alive</c> response header, verbatim, as last seen. Read rather than
    /// assumed so the crash report compares our pool timeout against what the server actually
    /// promises today, not against what it promised when this was written.
    /// </summary>
    private string? _lastKeepAliveHeader;

    /// <summary>Records the server's advertised keep-alive terms from any response.</summary>
    private void NoteKeepAlive(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Keep-Alive", out var values))
            _lastKeepAliveHeader = string.Join(", ", values);
    }

    // ── Retrying a connection that was dead on arrival ───────────────────────────

    private int _connectionRetries;

    /// <summary>
    /// Whether a failure means <b>the server never saw the request</b> — the only condition under
    /// which asking again is honest.
    ///
    /// <para>The distinction that matters: a fallback <i>invents an answer</i>, a retry <i>re-asks the
    /// question</i>. Nothing was processed, so there is no state to be inconsistent with and no
    /// substituted result to mistake for a real one. That is why this is not the kind of silent
    /// recovery <see cref="Cathedral.Game.Narrative.PersonaMatchCritic"/> had its fallback removed for.</para>
    ///
    /// <para><b>`.NET` already does this for us — but only for verbs it may safely replay, and every
    /// request we make is a POST.</b> That asymmetry is the whole shape of the bug and is why it took
    /// so long to see. Against a server that drops the second request on each connection, a GET never
    /// fails: <c>SendWithVersionDetectionAndRetryAsync</c> quietly re-sends it on a fresh connection.
    /// The identical scenario as a POST fails every time with
    /// <c>HttpRequestError.ResponseEnded</c> — "The response ended prematurely" — which is exactly
    /// the exception the crash report carried. So this is not novel behaviour being invented; it is
    /// the behaviour `.NET` would already apply if it could know a chat completion is safe to repeat.
    /// It also means <b>probing this with a GET proves nothing</b>: three separate attempts to
    /// reproduce it that way came back clean because the runtime was hiding the failure.</para>
    ///
    /// <para>It is narrow on purpose, and the exclusions are the design:</para>
    /// <list type="bullet">
    /// <item><b>Not a timeout.</b> Those arrive as <see cref="TaskCanceledException"/>, not
    /// <see cref="HttpRequestException"/>, so they cannot reach here — which is right. A server that
    /// is alive and wedged is a real problem and must surface.</item>
    /// <item><b>Not an HTTP error status.</b> The 4xx/5xx check happens outside the retry loop, so a
    /// context-size rejection or a server fault is never re-sent.</item>
    /// <item><b>Not <c>ConnectionRefused</c>.</b> That means the server itself is gone; re-asking
    /// only delays the report by one round trip.</item>
    /// <item><b>Not a mid-stream failure.</b> Only the send is retried. Once headers are in, tokens
    /// may already have reached the preview, and re-sending would double them.</item>
    /// </list>
    /// </summary>
    private static bool IsDeadOnArrival(Exception ex)
    {
        if (ex is not HttpRequestException http) return false;

        // The request went out onto a socket that was already closed: the exact shape of a pooled
        // connection the server had dropped. Nothing was processed.
        if (http.HttpRequestError == HttpRequestError.ResponseEnded) return true;

        // A connection that existed and died under us. Refusal is deliberately not in this list.
        for (var e = ex.InnerException; e != null; e = e.InnerException)
            if (e is System.Net.Sockets.SocketException sock)
                return sock.SocketErrorCode is System.Net.Sockets.SocketError.ConnectionReset
                                            or System.Net.Sockets.SocketError.ConnectionAborted
                                            or System.Net.Sockets.SocketError.Shutdown;

        return false;
    }

    /// <summary>
    /// Counts a retry and says so. <b>Every one is reported</b> — the point of retrying is to spare
    /// the player a dead run, not to stop anyone finding out. A session quietly retrying twenty times
    /// is the same news as a session crashing twenty times, and this is how that news arrives:
    /// a line per occurrence in <c>log.txt</c>, and the total in every crash report.
    /// </summary>
    private void NoteConnectionRetry(int slotId, Exception ex, RequestTrace trace)
    {
        lock (_traceLock)
        {
            _connectionRetries++;
            trace.Retried = true;
        }

        Console.Error.WriteLine(
            $"LLM slot {slotId}: the connection was dead before the request went out " +
            $"({ex.GetType().Name}: {ex.Message}) — asking again once. The server never saw it, so " +
            "nothing is lost. If this line is frequent, our pooled-connection idle timeout " +
            $"({PooledConnectionIdleTimeout.TotalSeconds:F0}s) is no longer below the server's " +
            $"keep-alive ({_lastKeepAliveHeader ?? "unknown"}).");
    }

    private RequestTrace StartTrace(int slotId, bool streaming, int maxTokens)
    {
        lock (_traceLock)
        {
            var now = DateTime.UtcNow;
            return new RequestTrace
            {
                Seq        = ++_requestSeq,
                Slot       = slotId,
                Streaming  = streaming,
                MaxTokens  = maxTokens,
                StartedUtc = now,
                IdleGapMs  = _lastRequestEndedUtc == DateTime.MinValue
                                 ? 0
                                 : (now - _lastRequestEndedUtc).TotalMilliseconds,
            };
        }
    }

    private void FinishTrace(RequestTrace trace, string outcome)
    {
        lock (_traceLock)
        {
            var now = DateTime.UtcNow;
            trace.DurationMs = (now - trace.StartedUtc).TotalMilliseconds;
            trace.Outcome = outcome;
            _lastRequestEndedUtc = now;
            if (outcome.StartsWith("FAILED")) _failedRequests++;

            _recentRequests.Enqueue(trace);
            while (_recentRequests.Count > RecentRequestsKept) _recentRequests.Dequeue();
        }
    }

    /// <summary>
    /// The LLM section of a crash report: is the server alive, does it still answer, and what were we
    /// doing just before.
    ///
    /// <para><b>The two health probes are the point.</b> One goes through the shared
    /// <see cref="HttpClient"/> — whose connection pool is the suspect — and one through a throwaway
    /// client that must open a new connection. A fresh client succeeding where the shared one fails
    /// says the server is fine and our pooled connection was not; both failing says the server is
    /// gone. That distinction was unanswerable from the log that prompted this, and it is the first
    /// thing anyone would want to know.</para>
    ///
    /// <para>Bounded: every probe has its own short timeout, because this runs while a player is
    /// looking at an error screen.</para>
    /// </summary>
    private string DescribeForCrashReport()
    {
        var sb = new StringBuilder();

        sb.AppendLine($"  Server ready flag:  {_isServerReady}");
        sb.AppendLine($"  Base URL:           {_baseUrl}");
        sb.AppendLine($"  Context size:       {_contextSize} per slot");
        sb.AppendLine($"  Instances (slots):  {_instances.Count}");

        try
        {
            if (_llamaProcess == null)
            {
                sb.AppendLine("  llama-server:       never started by this process");
            }
            else if (_llamaProcess.HasExited)
            {
                sb.AppendLine($"  llama-server:       EXITED, code {_llamaProcess.ExitCode}, at {_llamaProcess.ExitTime:HH:mm:ss.fff}");
            }
            else
            {
                sb.AppendLine($"  llama-server:       alive, pid {_llamaProcess.Id}, up {(DateTime.Now - _llamaProcess.StartTime).TotalMinutes:F1} min");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"  llama-server:       (unreadable: {ex.Message})");
        }

        sb.AppendLine($"  Health, pooled:     {ProbeHealth(_httpClient)}");
        sb.AppendLine($"  Health, fresh conn: {ProbeHealth(null)}");

        // The two halves of the keep-alive contract, printed together because their MISMATCH is a
        // whole class of failure and reading either alone says nothing. The server's figure is taken
        // live from its own response header rather than assumed, so a llama.cpp upgrade that changes
        // it shows up here instead of silently invalidating the constant beside it.
        sb.AppendLine($"  Server keep-alive:  {_lastKeepAliveHeader ?? "(not seen yet)"}");
        sb.AppendLine($"  Our pool idle max:  {PooledConnectionIdleTimeout.TotalSeconds:F0}s " +
                      "(MUST be lower than the server's timeout, or the pool holds dead connections)");

        lock (_traceLock)
        {
            sb.AppendLine();
            sb.AppendLine($"  Requests this run:  {_requestSeq} ({_failedRequests} failed, " +
                          $"{_connectionRetries} retried after a dead connection)");
            sb.AppendLine($"  Last {_recentRequests.Count} requests (oldest first):");
            if (_recentRequests.Count == 0)
                sb.AppendLine("    (none)");
            else
                foreach (var t in _recentRequests)
                    sb.AppendLine(t.ToString());

            sb.AppendLine();
            sb.AppendLine("  Reading this table:");
            sb.AppendLine("    eof=n   on a streaming request means the body was abandoned before its end,");
            sb.AppendLine("            leaving the connection unusable for whatever request came next.");
            sb.AppendLine("    idle=   is the gap since the PREVIOUS REQUEST finished — NOT the age of the");
            sb.AppendLine("            connection this one was handed. A stale-connection failure can show");
            sb.AppendLine("            idle=1ms while reusing a socket that has been dead for half a minute,");
            sb.AppendLine("            so a small idle= does NOT rule out a keep-alive problem. Read the long");
            sb.AppendLine("            gaps EARLIER in the table instead: each one over the server's keep-alive");
            sb.AppendLine("            timeout leaves another dead connection in the pool.");
            sb.AppendLine("    dur=    single-digit ms on a failure means the socket was already gone. A busy");
            sb.AppendLine("            or slow server fails slowly; a dead connection fails instantly.");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Asks <c>/health</c> and reports what came back. Pass null to probe over a brand-new connection
    /// pool, which is what distinguishes a broken connection from a broken server.
    /// </summary>
    private string ProbeHealth(HttpClient? client)
    {
        HttpClient? throwaway = null;
        try
        {
            if (client == null)
            {
                throwaway = new HttpClient
                {
                    BaseAddress = new Uri(_baseUrl),
                    Timeout = TimeSpan.FromSeconds(3)
                };
                client = throwaway;
            }

            var started = DateTime.UtcNow;
            var probeClient = client;

            // Sync-over-async, but off the calling thread and with a wall-clock cap. The report is a
            // snapshot taken at one instant, so it has to block — but it is written on the worst path
            // in the game, and a crash reporter that deadlocks or hangs the window turns a diagnosable
            // bug into an unreportable freeze. Task.Run keeps the await off whatever thread we were
            // called on; the Wait bounds it even if the request itself never returns.
            var probe = Task.Run(async () =>
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                using var response = await probeClient.GetAsync("health", cts.Token).ConfigureAwait(false);
                return $"{(int)response.StatusCode} {response.StatusCode}";
            });

            if (!probe.Wait(TimeSpan.FromSeconds(5)))
                return "NO ANSWER within 5s (request still outstanding)";

            var elapsed = (DateTime.UtcNow - started).TotalMilliseconds;
            return $"{probe.Result} in {elapsed:F0}ms";
        }
        catch (Exception ex)
        {
            // Unwrap: Task.Wait reports an AggregateException whose message ("One or more errors
            // occurred") is exactly the kind of uninformative text this whole report exists to replace.
            var real = ex is AggregateException agg ? agg.Flatten().InnerException ?? ex : ex;
            var sb = new StringBuilder($"FAILED {real.GetType().Name}: {real.Message}");
            for (var e = real.InnerException; e != null; e = e.InnerException)
            {
                sb.Append($" ← {e.GetType().Name}: {e.Message}");
                if (e is System.Net.Sockets.SocketException sock) sb.Append($" [{sock.SocketErrorCode}]");
            }
            return sb.ToString();
        }
        finally
        {
            // If the probe timed out it is still running and will fault on this; the task is not
            // awaited and an unobserved fault is ignored, which is the right cost for a bounded probe.
            throwaway?.Dispose();
        }
    }

    /// <summary>
    /// How long a connection may sit idle in our pool before we drop it.
    ///
    /// <para><b>This must stay below the server's keep-alive timeout, which is 5 seconds.</b>
    /// llama-server says so in every response — <c>Keep-Alive: timeout=5, max=100</c> — and a socket
    /// probe confirms it closes an idle connection at 5.02s. `.NET` does not honour that header, and
    /// <see cref="SocketsHttpHandler.PooledConnectionIdleTimeout"/> defaults to <b>one minute</b>: so
    /// out of the box the pool holds connections the server destroyed fifty-five seconds earlier.</para>
    ///
    /// <para><b>This is prevention, not the fix</b>, and the distinction is worth keeping straight.
    /// `.NET` checks a pooled connection for liveness before reusing it, and on Windows that check is
    /// reliable: 12 POSTs spaced 7 seconds apart against a real llama-server, with the 60-second
    /// default, produced **no** failures. So the mismatch alone is survivable — what it does is keep
    /// a supply of dead connections in the pool for that check to have to catch. Shrinking the
    /// timeout below the server's removes the supply, so nothing has to be caught.</para>
    ///
    /// <para>The failure needs the close to land inside the check's race window, which did not
    /// reproduce here but did on the reporter's machine — Wine, at about ten frames a second. Two
    /// seconds leaves three of margin for exactly that kind of scheduling skew.</para>
    ///
    /// <para>The demonstrated fix is the retry; see <see cref="IsDeadOnArrival"/>.</para>
    /// </summary>
    private static readonly TimeSpan PooledConnectionIdleTimeout = TimeSpan.FromSeconds(2);

    public LlamaServerManager(string? baseUrl = null)
    {
        _baseUrl = baseUrl ?? "http://127.0.0.1:8080/";

        var handler = new SocketsHttpHandler
        {
            PooledConnectionIdleTimeout = LlamaServerManager.PooledConnectionIdleTimeout,

            // A backstop for the other half of the server's promise, `max=100`. The server does send
            // `Connection: close` on the hundredth request and .NET honours that, so this is belt and
            // braces rather than the fix — but a connection that has lived for minutes is worth
            // nothing to us, and every one retired early is one that cannot go stale.
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
        };

        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromMinutes(10)
        };

        CrashReport.AddProvider("LLM server", DescribeForCrashReport);

        // Register cleanup handlers
        AppDomain.CurrentDomain.ProcessExit += (s, e) => StopServer();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            StopServer();
        };
    }
    
    /// <summary>
    /// Starts the Llama server and calls the provided hook when ready
    /// </summary>
    /// <param name="onServerReady">Hook called when server is ready (true) or failed (false)</param>
    /// <param name="contextSize">Maximum context size in tokens. Defaults to <see cref="Config.LLM.ContextSize"/>.</param>
    public async Task StartServerAsync(Action<bool>? onServerReady = null, int contextSize = Config.LLM.ContextSize)
    {
        var startTime = DateTime.Now;
        
        // Store the context size for use in instances
        _contextSize = contextSize;
        
        try
        {
            // Check if server is already running
            if (await IsServerRunningAsync())
            {
                _isServerReady = true;
                _loadingProgress = 1.0f;
                _loadingStatusMessage = "Model loaded!";
                Console.WriteLine("Llama server is already running.");
                
                // Still create a session log directory for this run
                var sessionTimestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                _sessionLogDir = TryCreateLogDirectory(Path.Combine("logs", $"llm_session_{sessionTimestamp}"));
                if (_sessionLogDir != null)
                    Console.WriteLine($"LLM logs will be saved to: {_sessionLogDir}");
                
                LoadingProgressUpdated?.Invoke(this, new LoadingProgressEventArgs(1.0f, "Model loaded!", 0));
                ServerReady?.Invoke(this, new ServerStatusEventArgs(true, "Server already running"));
                onServerReady?.Invoke(true);
                
                // Log to LLM logger if available
                try { LLMLogger.LogServerInitResult(true, "Server already running", (DateTime.Now - startTime).TotalSeconds); } catch { }
                return;
            }
            
            Console.WriteLine("Starting llama server...");
            
            // Create session log directory. A failure here is not fatal — see TryCreateLogDirectory.
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            _sessionLogDir = TryCreateLogDirectory(Path.Combine("logs", $"llm_session_{timestamp}"));
            if (_sessionLogDir != null)
                Console.WriteLine($"LLM logs will be saved to: {_sessionLogDir}");
            
            // Find paths
            var (resolvedServerPath, resolvedModelPath) = ResolvePaths();

            // Validate paths — both files are required; without them the game cannot run, so exit.
            if (!File.Exists(resolvedServerPath))
            {
                LogError($"Llama server executable not found at: {resolvedServerPath}");
                LogError($"Place a llama.cpp release in models/llama/ (see models/llama/BUILD.txt for the expected contents), then restart.");
                Environment.Exit(1);
            }

            if (!File.Exists(resolvedModelPath))
            {
                LogError($"Model file not found at: {resolvedModelPath}");
                LogError($"Place a GGUF model in the models/ directory named exactly '{LlamaRuntime.ModelFileName}', then restart.");
                Environment.Exit(1);
            }

            // Name the model from its own header — the file name is deliberately generic, so this
            // is the only way the log says which model actually loaded.
            var modelName = LlamaRuntime.ModelDisplayName;
            Console.WriteLine($"Using model: {modelName} ({LlamaRuntime.DescribeInstallation()})");

            try { LLMLogger.LogServerInitStart(modelName, resolvedServerPath, resolvedModelPath); } catch { }

            // First run on this machine (or the model changed): measure what to run on.
            //
            // Held until the loading screen is up. The probe runs two benchmarks that together take
            // minutes on a slow device, and it used to start before there was a window at all —
            // so the most expensive wait in the game happened with nothing on screen to explain it,
            // and then continued behind a screen that said "Loading language model" while no model
            // was loading. Gated, every second of it is in front of a player who has been told.
            //
            // Asked first, because waiting is only free on the runs that would actually probe;
            // on every other run this would delay the server for a screen it does not need.
            if (LlamaProbe.IsProbeNeeded())
                await WaitForLoadingScreenAsync();

            // On a thread of its own because the probe is synchronous and can run a benchmark,
            // while this method is deliberately started without being awaited — everything before
            // its first await would otherwise run on the caller's thread, which is the UI's.
            await Task.Run(() => LlamaProbe.EnsureProbed(ReportProbeStage));

            // Start the server, stepping down the ladder if a device fails.
            var isReady = await StartWithFallbackAsync(resolvedServerPath, resolvedModelPath);

            _isServerReady = isReady;
            var message = isReady ? "Server started successfully" : "Server failed to start";
            var duration = (DateTime.Now - startTime).TotalSeconds;
            
            Console.WriteLine(isReady ? "✁ELlama server and model loaded successfully." : "✁EFailed to start Llama server.");
            
            // Log result
            try { LLMLogger.LogServerInitResult(isReady, message, duration); } catch { }
            
            ServerReady?.Invoke(this, new ServerStatusEventArgs(isReady, message));
            onServerReady?.Invoke(isReady);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error starting server: {ex.Message}";
            var duration = (DateTime.Now - startTime).TotalSeconds;
            LogError(errorMsg);
            
            // Log error
            try { LLMLogger.LogServerInitResult(false, errorMsg, duration); } catch { }
            
            ServerReady?.Invoke(this, new ServerStatusEventArgs(false, errorMsg));
            onServerReady?.Invoke(false);
        }
    }
    
    /// <summary>
    /// Creates a new LLM instance with the given system prompt.
    /// <para>
    /// Every system prompt is closed with <see cref="Cathedral.Game.Narrative.SceneSetting.Rule"/>,
    /// so the world's register holds for every request the slot will ever serve. Applied here rather
    /// than at each call site because that is the only way a slot added later cannot forget it —
    /// personas, dialogue NPCs, critics and the sanitizer all come through this one door.
    /// </para>
    /// </summary>
    /// <param name="systemPrompt">The system prompt for this instance</param>
    /// <param name="maxContextTokens">Maximum context size in tokens (default: uses server's context size)</param>
    /// <returns>The slot ID for this instance</returns>
    public async Task<int> CreateInstanceAsync(string systemPrompt, int? maxContextTokens = null)
    {
        if (!_isServerReady)
        {
            throw new InvalidOperationException("Server is not ready. Call StartServerAsync first.");
        }

        systemPrompt = $"{systemPrompt.TrimEnd()}\n\n{Cathedral.Game.Narrative.SceneSetting.Rule}";

        var slotId = -1;
        LlamaInstance instance;
        lock (_slotLock)
        {
            slotId = _nextSlotId++;
            instance = new LlamaInstance(slotId, systemPrompt)
            {
                MaxContextTokens = maxContextTokens ?? _contextSize
            };
            _instances[slotId] = instance;
        }
        
        // Create instance log directory and save system prompt
        if (_sessionLogDir != null)
        {
            try
            {
                var instanceLogDir = Path.Combine(_sessionLogDir, $"slot_{slotId}");
                Directory.CreateDirectory(instanceLogDir);
                await File.WriteAllTextAsync(Path.Combine(instanceLogDir, "system_prompt.txt"), systemPrompt);
            }
            catch (Exception ex)
            {
                NoteLoggingFailure("cannot write a slot log", ex);
            }
        }
        
        // Pre-cache the system prompt
        try
        {
            await PreCacheSystemPromptAsync(instance);
            Console.WriteLine($"✁ECreated instance {slotId} with system prompt cached.");
            
            // Log successful creation with system prompt - extract role from system prompt
            var role = ExtractRoleFromSystemPrompt(systemPrompt);
            try { LLMLogger.LogInstanceCreated(slotId, role, true, null, systemPrompt); } catch { }
        }
        catch (Exception ex)
        {
            LogWarning($"Failed to pre-cache system prompt for instance {slotId}: {ex.Message}");
            
            // Log creation with warning - extract role from system prompt
            var role = ExtractRoleFromSystemPrompt(systemPrompt);
            try { LLMLogger.LogInstanceCreated(slotId, role, false, ex.Message); } catch { }
        }
        
        return slotId;
    }
    
    /// <summary>
    /// Gets token probabilities for the next token without streaming.
    /// Used by the Critic role for probability-based evaluation.
    /// </summary>
    /// <param name="slotId">The instance slot ID</param>
    /// <param name="userMessage">The user's message/question</param>
    /// <param name="constrainedTokens">Expected tokens to extract probabilities for (e.g., ["yes", "no"])</param>
    /// <param name="gbnfGrammar">Optional GBNF grammar to constrain output</param>
    /// <returns>Dictionary mapping tokens to their probabilities</returns>
    public async Task<Dictionary<string, double>> GetNextTokenProbabilitiesAsync(
        int slotId,
        string userMessage,
        string[] constrainedTokens,
        string? gbnfGrammar = null)
    {
        if (Cathedral.Game.PlaygroundMode.IsActive)
            return constrainedTokens.ToDictionary(t => t, _ => 0.0);

        if (!_instances.TryGetValue(slotId, out var instance))
        {
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");
        }
        
        if (instance.IsActive)
        {
            throw new InvalidOperationException($"Instance {slotId} is currently processing another request.");
        }
        
        instance.IsActive = true;
        instance.AddUserMessage(userMessage);
        instance.RequestCount++;
        
        var startTime = DateTime.Now;
        var timestamp = startTime.ToString("HH-mm-ss-fff");
        
        // Create request log directory for Critic evaluations
        string? requestLogDir = null;
        if (_sessionLogDir != null)
        {
            try
            {
                requestLogDir = Path.Combine(_sessionLogDir, $"slot_{slotId}", $"request_{instance.RequestCount:D3}_{timestamp}");
                Directory.CreateDirectory(requestLogDir);

                // Save user question
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "user_message.txt"), userMessage);
            }
            catch (Exception ex)
            {
                requestLogDir = null;
                NoteLoggingFailure("cannot write a request log", ex);
            }
        }
        
        try
        {
            // Create request with logprobs enabled
            var requestData = new Dictionary<string, object>
            {
                ["model"] = "local",
                ["messages"] = instance.GetMessages(),
                ["max_tokens"] = 1,
                ["logprobs"] = true,
                ["top_logprobs"] = Math.Max(constrainedTokens.Length, 5), // Get at least top 5
                ["stream"] = false,
                ["cache_prompt"] = true,
                ["slot_id"] = slotId,
                ["top_k"] = Config.LLM.TopK,
                ["temperature"] = Config.LLM.Temperature,
                ["top_p"] = Config.LLM.TopP
            };
            
            if (!string.IsNullOrWhiteSpace(gbnfGrammar))
            {
                requestData["grammar"] = gbnfGrammar;
            }
            
            // Save full context and GBNF to log (for Critic)
            if (requestLogDir != null)
            {
                // Save full context (all messages sent to LLM)
                var messagesArray = (object[])requestData["messages"];
                var contextJson = JsonSerializer.Serialize(messagesArray, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "full_context.json"), contextJson);
                
                // Save GBNF grammar if provided
                if (!string.IsNullOrWhiteSpace(gbnfGrammar))
                {
                    await File.WriteAllTextAsync(Path.Combine(requestLogDir, "gbnf_constraints.txt"), gbnfGrammar);
                }
            }
            
            var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", requestData);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Failed to get token probabilities: {response.StatusCode} - {errorContent}");
            }
            
            var jsonResponse = await response.Content.ReadAsStringAsync();
            
            // Parse the response to extract logprobs
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;
            
            var probabilities = new Dictionary<string, double>();
            
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                
                if (choice.TryGetProperty("logprobs", out var logprobs) &&
                    logprobs.TryGetProperty("content", out var content) &&
                    content.GetArrayLength() > 0)
                {
                    var tokenInfo = content[0];
                    
                    if (tokenInfo.TryGetProperty("top_logprobs", out var topLogprobs))
                    {
                        // First pass: collect all tokens with their probabilities
                        var tokenVariants = new Dictionary<string, List<(string original, double prob)>>();
                        
                        foreach (var logprobEntry in topLogprobs.EnumerateArray())
                        {
                            if (logprobEntry.TryGetProperty("token", out var tokenElement) &&
                                logprobEntry.TryGetProperty("logprob", out var logprobElement))
                            {
                                var token = tokenElement.GetString();
                                var logprob = logprobElement.GetDouble();
                                
                                if (token != null)
                                {
                                    // Convert log probability to probability: p = exp(logprob)
                                    var probability = Math.Exp(logprob);
                                    
                                    // Store original token
                                    probabilities[token] = probability;
                                    
                                    // Group by normalized version
                                    var normalizedToken = token.Trim().ToLower();
                                    if (!tokenVariants.ContainsKey(normalizedToken))
                                    {
                                        tokenVariants[normalizedToken] = new List<(string, double)>();
                                    }
                                    tokenVariants[normalizedToken].Add((token, probability));
                                }
                            }
                        }
                        
                        // Second pass: for normalized tokens, use the AVERAGE probability of variants
                        // This represents the model's true belief accounting for formatting uncertainty
                        foreach (var kvp in tokenVariants)
                        {
                            var normalizedToken = kvp.Key;
                            var variants = kvp.Value;
                            
                            // Use the average probability across all variants
                            var avgProbability = variants.Average(v => v.prob);
                            probabilities[normalizedToken] = avgProbability;
                        }
                    }
                }
            }
            
            // Ensure all constrained tokens are present (with 0 if not found)
            foreach (var token in constrainedTokens)
            {
                var normalizedToken = token.Trim().ToLower();
                if (!probabilities.ContainsKey(normalizedToken))
                {
                    probabilities[normalizedToken] = 0.0;
                }
            }
            
            // Log yes/no probabilities for Critic evaluations
            if (requestLogDir != null)
            {
                var probsText = new StringBuilder();
                probsText.AppendLine("Token Probabilities:");
                foreach (var kvp in probabilities.OrderByDescending(kvp => kvp.Value))
                {
                    probsText.AppendLine($"  {kvp.Key}: {kvp.Value:F6} ({kvp.Value * 100:F2}%)");
                }
                
                // Calculate yes/no ratio if applicable
                if (probabilities.ContainsKey("yes") && probabilities.ContainsKey("no"))
                {
                    var pYes = probabilities["yes"];
                    var pNo = probabilities["no"];
                    var total = pYes + pNo;
                    var ratio = total > 0 ? pYes / total : 0.5;
                    probsText.AppendLine();
                    probsText.AppendLine($"Yes/No Ratio: {ratio:F6} ({ratio * 100:F2}%)");
                }
                
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "yes_no_probs.txt"), probsText.ToString());
            }
            
            // Save timing information
            if (requestLogDir != null)
            {
                var endTime = DateTime.Now;
                var duration = endTime - startTime;
                var timingText = new StringBuilder();
                timingText.AppendLine("Request Timing:");
                timingText.AppendLine($"  Start:    {startTime:yyyy-MM-dd HH:mm:ss.fff}");
                timingText.AppendLine($"  End:      {endTime:yyyy-MM-dd HH:mm:ss.fff}");
                timingText.AppendLine($"  Duration: {duration.TotalMilliseconds:F0}ms ({duration.TotalSeconds:F2}s)");
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "timing.txt"), timingText.ToString());

                if (root.TryGetProperty("usage", out var usageEl))
                {
                    int pt = usageEl.TryGetProperty("prompt_tokens", out var ptEl) ? ptEl.GetInt32() : 0;
                    int ct = usageEl.TryGetProperty("completion_tokens", out var ctEl) ? ctEl.GetInt32() : 0;
                    int slotCtx = _contextSize;
                    double fillPct = slotCtx > 0 ? pt * 100.0 / slotCtx : 0;
                    var ctxText = $"Prompt Tokens:     {pt}\nCompletion Tokens: {ct}\nContext Size:      {slotCtx} (slot)\nContext Fill:      {fillPct.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}%\n";
                    await File.WriteAllTextAsync(Path.Combine(requestLogDir, "context_usage.txt"), ctxText);
                }
            }
            
            return probabilities;
        }
        finally
        {
            instance.IsActive = false;
        }
    }

    /// <summary>
    /// Generates a short constrained string using a GBNF grammar.
    /// Unlike GetNextTokenProbabilitiesAsync (which reads only the first token's logprobs),
    /// this generates the full constrained output  Eneeded for multi-token choices like "very_easy".
    /// Resets the instance after generation unless <paramref name="skipReset"/> is true,
    /// which allows a follow-up call before the caller resets manually.
    /// </summary>
    public async Task<string> GenerateConstrainedStringAsync(
        int slotId,
        string userMessage,
        string? gbnfGrammar,
        int maxTokens = 20,
        bool skipReset = false)
    {
        if (Cathedral.Game.PlaygroundMode.IsActive)
            return string.Empty;

        if (!_instances.TryGetValue(slotId, out var instance))
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");

        if (instance.IsActive)
            throw new InvalidOperationException($"Instance {slotId} is currently processing another request.");

        instance.IsActive = true;
        instance.AddUserMessage(userMessage);
        instance.RequestCount++;

        var trace = StartTrace(slotId, streaming: false, maxTokens);
        var startTime = DateTime.Now;
        var timestamp = startTime.ToString("HH-mm-ss-fff");

        string? requestLogDir = null;
        if (_sessionLogDir != null)
        {
            try
            {
                requestLogDir = Path.Combine(_sessionLogDir, $"slot_{slotId}", $"request_{instance.RequestCount:D3}_{timestamp}");
                Directory.CreateDirectory(requestLogDir);
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "user_message.txt"), userMessage);
            }
            catch (Exception ex)
            {
                // Clearing this switches off every later write in the request too — they are all
                // guarded by it — so one failure cannot leave half a request logged.
                requestLogDir = null;
                NoteLoggingFailure("cannot write a request log", ex);
            }
        }

        try
        {
            await EnsureContextFitsAsync(instance, maxTokens);

            var requestData = new Dictionary<string, object>
            {
                ["model"] = "local",
                ["messages"] = instance.GetMessages(),
                ["max_tokens"] = maxTokens,
                ["stream"] = false,
                ["cache_prompt"] = true,
                ["slot_id"] = slotId,
                ["top_k"] = Config.LLM.TopK,
                ["temperature"] = Config.LLM.Temperature,
                ["top_p"] = Config.LLM.TopP,
            };

            // No grammar → unconstrained free-text generation (persona reasoning). llama.cpp treats
            // an empty grammar as constraining to the empty string, so omit the key entirely.
            if (!string.IsNullOrEmpty(gbnfGrammar))
                requestData["grammar"] = gbnfGrammar;

            if (requestLogDir != null)
            {
                var messagesArray = (object[])requestData["messages"];
                var contextJson = JsonSerializer.Serialize(messagesArray, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "full_context.json"), contextJson);
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "gbnf_constraints.txt"), gbnfGrammar ?? "(none — free text)");
            }

            // Retried only for a connection that was dead on arrival; see IsDeadOnArrival. The
            // status check below is deliberately OUTSIDE the loop, so a server that answers with an
            // error is never asked twice.
            HttpResponseMessage? sent = null;
            for (int attempt = 0; ; attempt++)
            {
                try { sent = await _httpClient.PostAsJsonAsync("v1/chat/completions", requestData); break; }
                catch (Exception ex) when (attempt == 0 && IsDeadOnArrival(ex)) { NoteConnectionRetry(slotId, ex, trace); }
            }

            using var response = sent;
            NoteKeepAlive(response);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"GenerateConstrainedStringAsync failed: {response.StatusCode} - {errorContent}");
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(jsonResponse);
            var root = doc.RootElement;

            string generated = string.Empty;
            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
            {
                var choice = choices[0];
                if (choice.TryGetProperty("message", out var message) &&
                    message.TryGetProperty("content", out var content))
                {
                    generated = content.GetString() ?? string.Empty;
                }
            }

            generated = generated.Trim();

            if (requestLogDir != null)
            {
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "llm_response.txt"), generated);
                var endTime = DateTime.Now;
                var timingText = $"Duration: {(endTime - startTime).TotalMilliseconds:F0}ms";
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "timing.txt"), timingText);

                if (root.TryGetProperty("usage", out var usageEl))
                {
                    int pt = usageEl.TryGetProperty("prompt_tokens", out var ptEl) ? ptEl.GetInt32() : 0;
                    int ct = usageEl.TryGetProperty("completion_tokens", out var ctEl) ? ctEl.GetInt32() : 0;
                    int slotCtx = _contextSize;
                    double fillPct = slotCtx > 0 ? pt * 100.0 / slotCtx : 0;
                    var ctxText = $"Prompt Tokens:     {pt}\nCompletion Tokens: {ct}\nContext Size:      {slotCtx} (slot)\nContext Fill:      {fillPct.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}%\n";
                    await File.WriteAllTextAsync(Path.Combine(requestLogDir, "context_usage.txt"), ctxText);
                }
            }

            instance.AddAssistantResponse(generated);
            trace.ReplyChars = generated.Length;
            FinishTrace(trace, "ok");
            return generated;
        }
        catch (Exception ex)
        {
            FinishTrace(trace, $"FAILED {ex.GetType().Name}: {ex.Message}");
            throw;
        }
        finally
        {
            instance.IsActive = false;
            if (!skipReset)
                try { ResetInstance(slotId); } catch { /* Ignore reset errors */ }
        }
    }

    /// <summary>
    /// Streaming twin of <see cref="GenerateConstrainedStringAsync"/>: same constrained (GBNF) request,
    /// but sent with <c>stream=true</c> so each token delta is delivered to <paramref name="onTokenStreamed"/>
    /// (token, slotId) as it arrives. Returns the full assembled string, so the return contract matches the
    /// one-shot variant — callers that ignore the callback behave identically.
    ///
    /// Used by <see cref="Cathedral.Game.Narrative.PersonaRewriter"/> to drive the live generation preview.
    /// Kept separate from the one-shot method so the Critic / constrained-choice paths stay non-streaming.
    /// Unlike the (unused) <see cref="ContinueRequestAsync"/>, this reads the response with
    /// <see cref="HttpCompletionOption.ResponseHeadersRead"/> so tokens surface in real time rather than
    /// after the whole body has buffered.
    /// </summary>
    public async Task<string> GenerateConstrainedStringStreamingAsync(
        int slotId,
        string userMessage,
        string? gbnfGrammar,
        int maxTokens = 20,
        bool skipReset = false,
        Action<string, int>? onTokenStreamed = null)
    {
        if (Cathedral.Game.PlaygroundMode.IsActive)
            return string.Empty;

        if (!_instances.TryGetValue(slotId, out var instance))
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");

        if (instance.IsActive)
            throw new InvalidOperationException($"Instance {slotId} is currently processing another request.");

        instance.IsActive = true;
        instance.AddUserMessage(userMessage);
        instance.RequestCount++;

        var trace = StartTrace(slotId, streaming: true, maxTokens);
        var startTime = DateTime.Now;
        var timestamp = startTime.ToString("HH-mm-ss-fff");

        string? requestLogDir = null;
        if (_sessionLogDir != null)
        {
            try
            {
                requestLogDir = Path.Combine(_sessionLogDir, $"slot_{slotId}", $"request_{instance.RequestCount:D3}_{timestamp}");
                Directory.CreateDirectory(requestLogDir);
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "user_message.txt"), userMessage);
            }
            catch (Exception ex)
            {
                // Clearing this switches off every later write in the request too — they are all
                // guarded by it — so one failure cannot leave half a request logged.
                requestLogDir = null;
                NoteLoggingFailure("cannot write a request log", ex);
            }
        }

        try
        {
            await EnsureContextFitsAsync(instance, maxTokens);

            var requestData = new Dictionary<string, object>
            {
                ["model"] = "local",
                ["messages"] = instance.GetMessages(),
                ["max_tokens"] = maxTokens,
                ["stream"] = true,
                ["cache_prompt"] = true,
                ["slot_id"] = slotId,
                ["top_k"] = Config.LLM.TopK,
                ["temperature"] = Config.LLM.Temperature,
                ["top_p"] = Config.LLM.TopP,
            };

            // No grammar → unconstrained free-text generation. llama.cpp treats an empty grammar as
            // constraining to the empty string, so omit the key entirely (mirrors the one-shot variant).
            if (!string.IsNullOrEmpty(gbnfGrammar))
                requestData["grammar"] = gbnfGrammar;

            if (requestLogDir != null)
            {
                var messagesArray = (object[])requestData["messages"];
                var contextJson = JsonSerializer.Serialize(messagesArray, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "full_context.json"), contextJson);
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "gbnf_constraints.txt"), gbnfGrammar ?? "(none — free text)");
            }

            // Stream: read response headers first so token deltas surface as they arrive.
            //
            // Retried only for a connection that was dead on arrival; see IsDeadOnArrival. Only the
            // SEND is inside the loop: with ResponseHeadersRead, a throw here means no header and so
            // no token has reached onTokenStreamed, which is what makes asking again safe. A failure
            // while reading the body is never retried — the preview would show the reply twice.
            //
            // A fresh HttpRequestMessage per attempt because one cannot be sent twice.
            HttpRequestMessage? attemptRequest = null;
            HttpResponseMessage? sent = null;
            for (int attempt = 0; ; attempt++)
            {
                attemptRequest?.Dispose();
                attemptRequest = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
                {
                    Content = JsonContent.Create(requestData)
                };
                try { sent = await _httpClient.SendAsync(attemptRequest, HttpCompletionOption.ResponseHeadersRead); break; }
                catch (Exception ex) when (attempt == 0 && IsDeadOnArrival(ex)) { NoteConnectionRetry(slotId, ex, trace); }
            }

            using var request = attemptRequest;
            // Disposed, unlike before: an undisposed response holds its connection until the finalizer
            // gets to it, which is the same pooling hazard the drain below closes.
            using var response = sent;
            NoteKeepAlive(response);
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"GenerateConstrainedStringStreamingAsync failed: {response.StatusCode} - {errorContent}");
            }

            var sb = new StringBuilder();
            int promptTokens = 0, completionTokens = 0;

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var reader = new StreamReader(stream))
            {
                // Live only once [DONE] has been seen: generation itself may legitimately take
                // minutes, but the few bytes after [DONE] must not. Draining a stream the server
                // never ends would otherwise block until the HttpClient timeout (ten minutes) —
                // trading a rare broken connection for a certain hang, which is a worse bargain than
                // the bug being fixed. If it trips we simply stop, and the trace records it.
                CancellationTokenSource? drainCts = null;

                try
                {
                    while (true)
                    {
                        string? line;
                        try
                        {
                            line = await reader.ReadLineAsync(drainCts?.Token ?? CancellationToken.None);
                        }
                        catch (OperationCanceledException) when (drainCts != null)
                        {
                            trace.DrainTimedOut = true;
                            break;
                        }

                        if (line == null)
                        {
                            // ReadLineAsync returning null is the end of the body: drained cleanly.
                            trace.DrainedToEof = true;
                            break;
                        }

                        // Past [DONE] the body holds nothing we want — but it must still be read. See
                        // the note on the [DONE] branch below.
                        if (trace.SawDone) { trace.TailBytesAfterDone += line.Length + 1; continue; }

                        if (!line.StartsWith("data: ")) continue;
                        var jsonData = line.Substring(6);
                        if (jsonData == "[DONE]")
                        {
                            // Read on to EOF rather than breaking out here.
                            //
                            // [DONE] is the last thing we care about, but it is not the last thing on
                            // the wire: the chunked body still has its trailing blank line and
                            // terminating zero-length chunk to come. Breaking here disposed the
                            // response stream while the server was still writing them, which leaves
                            // the connection unusable and — because HttpClient pools connections —
                            // makes the NEXT request fail at the socket with a bare "An error occurred
                            // while sending the request", pointing at whatever innocent call happened
                            // to come after.
                            //
                            // That failure was rare (2 of 228 requests in the run that reported it)
                            // and reads as an LLM fault, not a bookkeeping one. Draining returns the
                            // connection cleanly; the terminating chunk arrives immediately after
                            // [DONE], and the timeout below covers a server that never sends it.
                            trace.SawDone = true;
                            drainCts = new CancellationTokenSource(DrainAfterDoneTimeout);
                            continue;
                        }

                        try
                        {
                            using var doc = JsonDocument.Parse(jsonData);
                            var root = doc.RootElement;

                            if (root.TryGetProperty("usage", out var usageEl))
                            {
                                if (usageEl.TryGetProperty("prompt_tokens", out var ptEl)) promptTokens = ptEl.GetInt32();
                                if (usageEl.TryGetProperty("completion_tokens", out var ctEl)) completionTokens = ctEl.GetInt32();
                            }

                            if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                            {
                                var choice = choices[0];
                                if (choice.TryGetProperty("delta", out var delta) &&
                                    delta.TryGetProperty("content", out var content))
                                {
                                    var token = content.GetString();
                                    if (!string.IsNullOrEmpty(token))
                                    {
                                        sb.Append(token);
                                        onTokenStreamed?.Invoke(token, slotId);
                                    }
                                }
                            }
                        }
                        catch (JsonException ex)
                        {
                            // Counted rather than merely skipped: a truncated stream otherwise reads
                            // as a short reply, and the caller sees a bad answer instead of a broken
                            // one.
                            trace.MalformedChunks++;
                            if (trace.MalformedChunks == 1)
                                Console.Error.WriteLine($"LLM slot {slotId}: malformed stream chunk ignored ({ex.Message}).");
                            continue;
                        }
                    }
                }
                finally
                {
                    drainCts?.Dispose();
                }
            }

            var generated = sb.ToString().Trim();

            if (requestLogDir != null)
            {
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "llm_response.txt"), generated);
                var timingText = $"Duration: {(DateTime.Now - startTime).TotalMilliseconds:F0}ms (streaming)";
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "timing.txt"), timingText);

                int reportedPrompt = promptTokens > 0 ? promptTokens : instance.EstimateConversationTokens();
                bool isEstimate = promptTokens == 0;
                int slotCtx = _contextSize;
                double fillPct = slotCtx > 0 ? reportedPrompt * 100.0 / slotCtx : 0;
                var ctxText = $"Prompt Tokens:     {reportedPrompt}{(isEstimate ? " (estimated)" : "")}\nCompletion Tokens: {completionTokens}\nContext Size:      {slotCtx} (slot)\nContext Fill:      {fillPct.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}%\n";
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "context_usage.txt"), ctxText);
            }

            instance.AddAssistantResponse(generated);
            trace.ReplyChars = generated.Length;
            FinishTrace(trace, "ok");
            return generated;
        }
        catch (Exception ex)
        {
            FinishTrace(trace, $"FAILED {ex.GetType().Name}: {ex.Message}");
            throw;
        }
        finally
        {
            instance.IsActive = false;
            if (!skipReset)
                try { ResetInstance(slotId); } catch { /* Ignore reset errors */ }
        }
    }

    /// <summary>
    /// Continue a conversation with an LLM instance, optionally using GBNF grammar constraints
    /// </summary>
    /// <param name="slotId">The instance slot ID</param>
    /// <param name="userMessage">The user's message</param>
    /// <param name="onTokenStreamed">Hook called for each new token (token, slotId)</param>
    /// <param name="onCompleted">Hook called when request ends (slotId, fullResponse, wasCancelled)</param>
    /// <param name="gbnfGrammar">Optional GBNF grammar string to constrain the output format</param>
    public async Task ContinueRequestAsync(
        int slotId, 
        string userMessage, 
        Action<string, int>? onTokenStreamed = null,
        Action<int, string, bool>? onCompleted = null,
        string? gbnfGrammar = null)
    {
        if (Cathedral.Game.PlaygroundMode.IsActive)
        {
            onCompleted?.Invoke(slotId, string.Empty, false);
            return;
        }

        if (!_instances.TryGetValue(slotId, out var instance))
        {
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");
        }
        
        if (instance.IsActive)
        {
            await LogWarningAsync($"Instance {slotId} is already marked as active. This may indicate a previous request didn't complete properly.");
            await LogWarningAsync($"Forcing instance to inactive state and proceeding...");
            instance.IsActive = false;
            instance.CurrentRequestCancellation?.Cancel();
            instance.CurrentRequestCancellation = null;
        }
        
        instance.IsActive = true;
        instance.AddUserMessage(userMessage);
        instance.RequestCount++;
        
        var requestStartTime = DateTime.Now;
        var timestamp = requestStartTime.ToString("HH-mm-ss-fff");
        
        // Create request log directory
        string? requestLogDir = null;
        if (_sessionLogDir != null)
        {
            try
            {
                requestLogDir = Path.Combine(_sessionLogDir, $"slot_{slotId}", $"request_{instance.RequestCount:D3}_{timestamp}");
                Directory.CreateDirectory(requestLogDir);

                // Save user message
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "user_message.txt"), userMessage);
            }
            catch (Exception ex)
            {
                requestLogDir = null;
                NoteLoggingFailure("cannot write a request log", ex);
            }
        }
        
        var cancellationToken = new CancellationTokenSource();
        instance.CurrentRequestCancellation = cancellationToken;
        
        try
        {
            var llmStartTime = DateTime.Now;
            var fullResponse = new StringBuilder();
            int promptTokens = 0;
            int completionTokens = 0;
            
            // Check if conversation history is too long and trim if needed. The estimate is taken
            // after trimming: it stands in for the prompt size in the context-usage log whenever the
            // server reports no usage block, and what actually got sent is the trimmed history.
            await EnsureContextFitsAsync(instance, Config.LLM.GenerationMaxTokens);
            int estimatedTokens = instance.EstimateConversationTokens();

            // Create the base request
            var requestData = new Dictionary<string, object>
            {
                ["model"] = "local",
                ["messages"] = instance.GetMessages(),
                ["max_tokens"] = Config.LLM.GenerationMaxTokens,
                ["stream"] = true,
                ["cache_prompt"] = true,
                ["slot_id"] = slotId,
                ["top_k"] = Config.LLM.TopK,
                ["temperature"] = Config.LLM.Temperature,
                ["top_p"] = Config.LLM.TopP
            };
            
            // Add GBNF grammar if provided
            if (!string.IsNullOrWhiteSpace(gbnfGrammar))
            {
                requestData["grammar"] = gbnfGrammar;
            }
            
            // Save full context and GBNF to log (AFTER building request to ensure user message is included)
            if (requestLogDir != null)
            {
                // Save full context (all messages sent to LLM) - use the same array from requestData
                var messagesArray = (object[])requestData["messages"];
                var contextJson = JsonSerializer.Serialize(messagesArray, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "full_context.json"), contextJson);
                
                // Save GBNF grammar if provided
                if (!string.IsNullOrWhiteSpace(gbnfGrammar))
                {
                    await File.WriteAllTextAsync(Path.Combine(requestLogDir, "gbnf_constraints.txt"), gbnfGrammar);
                }
            }
            
            // Send request
            var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", requestData, cancellationToken.Token);
            response.EnsureSuccessStatusCode();
            
            // Process streaming response
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken.Token);
            var reader = new StreamReader(stream);
            
            string? line;
            while ((line = await reader.ReadLineAsync()) != null && !cancellationToken.Token.IsCancellationRequested)
            {
                if (line.StartsWith("data: "))
                {
                    var jsonData = line.Substring(6);
                    
                    if (jsonData == "[DONE]")
                        break;
                    
                    try
                    {
                        using var doc = JsonDocument.Parse(jsonData);
                        var rootElement = doc.RootElement;

                        // Capture usage stats from the final chunk (sent by llama.cpp before [DONE])
                        if (rootElement.TryGetProperty("usage", out var usageEl))
                        {
                            if (usageEl.TryGetProperty("prompt_tokens", out var ptEl))
                                promptTokens = ptEl.GetInt32();
                            if (usageEl.TryGetProperty("completion_tokens", out var ctEl))
                                completionTokens = ctEl.GetInt32();
                        }

                        // Debug: Log the JSON structure if needed
                        if (!rootElement.TryGetProperty("choices", out var choices))
                        {
                            await LogWarningAsync($"Missing 'choices' in response: {jsonData}");
                            continue;
                        }

                        if (choices.GetArrayLength() > 0)
                        {
                            var choice = choices[0];
                            if (choice.TryGetProperty("delta", out var delta) &&
                                delta.TryGetProperty("content", out var content))
                            {
                                var token = content.GetString();
                                if (!string.IsNullOrEmpty(token))
                                {
                                    fullResponse.Append(token);

                                    // Invoke callbacks
                                    onTokenStreamed?.Invoke(token, slotId);
                                    TokenStreamed?.Invoke(this, new TokenStreamedEventArgs(token, slotId));
                                }
                            }
                        }
                    }
                    catch (JsonException)
                    {
                        // Skip malformed JSON chunks
                        continue;
                    }
                }
            }
            
            var responseText = fullResponse.ToString();
            var duration = DateTime.Now - llmStartTime;
            var wasCancelled = cancellationToken.Token.IsCancellationRequested;
            
            // Check for empty response (slot busy or other issues)
            if (!wasCancelled && string.IsNullOrWhiteSpace(responseText))
            {
                var details = $"Empty response after {duration.TotalMilliseconds:F0}ms - likely slot busy or server overload";
                await LogWarningAsync($"Slot {slotId} returned empty response after {duration.TotalMilliseconds}ms");
                await LogWarningAsync($"This usually indicates the slot was busy or the server rejected the request");
                
                // Log slot issue
                try { LLMLogger.LogSlotIssue(slotId, "Empty Response", details); } catch { }
            }
            
            if (!wasCancelled && !string.IsNullOrWhiteSpace(responseText))
            {
                instance.AddAssistantResponse(responseText);
            }
            
            // Save response to log
            if (requestLogDir != null)
            {
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "llm_response.txt"), responseText);
                
                // Save timing information
                var requestEndTime = DateTime.Now;
                var totalDuration = requestEndTime - requestStartTime;
                var timingText = new StringBuilder();
                timingText.AppendLine("Request Timing:");
                timingText.AppendLine($"  Request Start: {requestStartTime:yyyy-MM-dd HH:mm:ss.fff}");
                timingText.AppendLine($"  LLM Start:     {llmStartTime:yyyy-MM-dd HH:mm:ss.fff}");
                timingText.AppendLine($"  Response End:  {requestEndTime:yyyy-MM-dd HH:mm:ss.fff}");
                timingText.AppendLine();
                timingText.AppendLine("Durations:");
                timingText.AppendLine($"  Setup Time:    {(llmStartTime - requestStartTime).TotalMilliseconds:F0}ms");
                timingText.AppendLine($"  LLM Duration:  {duration.TotalMilliseconds:F0}ms ({duration.TotalSeconds:F2}s)");
                timingText.AppendLine($"  Total:         {totalDuration.TotalMilliseconds:F0}ms ({totalDuration.TotalSeconds:F2}s)");
                timingText.AppendLine();
                timingText.AppendLine("Status:");
                timingText.AppendLine($"  Cancelled:     {wasCancelled}");
                timingText.AppendLine($"  Empty Result:  {string.IsNullOrWhiteSpace(responseText)}");
                timingText.AppendLine($"  Response Len:  {responseText.Length} chars");
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "timing.txt"), timingText.ToString());

                int reportedPrompt = promptTokens > 0 ? promptTokens : estimatedTokens;
                bool isEstimate    = promptTokens == 0;
                int slotContextSize = _contextSize;
                double fillPct     = slotContextSize > 0 ? reportedPrompt * 100.0 / slotContextSize : 0;
                var ctxText = $"Prompt Tokens:     {reportedPrompt}{(isEstimate ? " (estimated)" : "")}\nCompletion Tokens: {completionTokens}\nContext Size:      {slotContextSize} (slot)\nContext Fill:      {fillPct.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}%\n";
                await File.WriteAllTextAsync(Path.Combine(requestLogDir, "context_usage.txt"), ctxText);
            }
            
            // Invoke completion callbacks
            onCompleted?.Invoke(slotId, responseText, wasCancelled);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, responseText, duration, wasCancelled));
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
        {
            // Request timeout - more specific than OperationCanceledException
            await LogErrorAsync($"Timeout in request for slot {slotId}: Request exceeded HttpClient timeout");
            onCompleted?.Invoke(slotId, "", false);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, "", DateTime.Now - DateTime.Now, false));
        }
        catch (OperationCanceledException)
        {
            // Request was cancelled by user
            onCompleted?.Invoke(slotId, "", true);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, "", DateTime.Now - DateTime.Now, true));
        }
        catch (HttpRequestException ex)
        {
            // Check if this is a context length error
            if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest && IsContextLengthError(ex.Message))
            {
                await LogWarningAsync($"Slot {slotId}: Context length exceeded. Attempting to trim and retry...");
                
                // Force aggressive trimming
                int removedCount = instance.TrimToFitContext(instance.MaxContextTokens / 2);
                await LogWarningAsync($"Slot {slotId}: Aggressively trimmed {removedCount} messages. Retrying request...");
                
                // Retry the request with trimmed history
                try
                {
                    // Recursive call with trimmed history (user message already added)
                    // Remove the user message first to avoid duplication
                    if (instance.ConversationHistory.Count > 0)
                    {
                        var lastMsg = instance.ConversationHistory[instance.ConversationHistory.Count - 1];
                        dynamic dynMsg = lastMsg;
                        if (dynMsg.role == "user")
                        {
                            instance.ConversationHistory.RemoveAt(instance.ConversationHistory.Count - 1);
                        }
                    }
                    
                    // Reset active state for retry
                    instance.IsActive = false;
                    instance.CurrentRequestCancellation = null;
                    
                    await LogWarningAsync($"Slot {slotId}: Retrying request after context trim...");
                    await ContinueRequestAsync(slotId, userMessage, onTokenStreamed, onCompleted, gbnfGrammar);
                    return; // Exit after retry
                }
                catch (Exception retryEx)
                {
                    await LogErrorAsync($"Slot {slotId}: Retry after context trim failed: {retryEx.Message}");
                    onCompleted?.Invoke(slotId, "", false);
                    RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, "", DateTime.Now - DateTime.Now, false));
                    return;
                }
            }
            
            // Network/HTTP error - server may be overloaded or connection lost
            await LogErrorAsync($"HTTP Error in request for slot {slotId}: {ex.Message}");
            if (ex.StatusCode.HasValue)
            {
                await LogErrorAsync($"Status code: {ex.StatusCode.Value}");
            }
            await LogErrorAsync($"This may indicate: server overload, connection timeout, or server not responding");
            onCompleted?.Invoke(slotId, "", false);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, "", DateTime.Now - DateTime.Now, false));
        }
        catch (Exception ex)
        {
            await LogErrorAsync($"Error in request for slot {slotId}: {ex.Message}");
            await LogErrorAsync($"Exception type: {ex.GetType().Name}");
            await LogErrorAsync($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                await LogErrorAsync($"Inner exception: {ex.InnerException.Message}");
            }
            onCompleted?.Invoke(slotId, "", false);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, "", DateTime.Now - DateTime.Now, false));
        }
        finally
        {
            // CRITICAL: Always clean up instance state, even if exceptions occurred
            instance.IsActive = false;
            instance.CurrentRequestCancellation = null;
            
            // Add a small delay to allow server to fully clean up the slot
            // This prevents rapid-fire requests from overwhelming the same slot
            await Task.Delay(50);
        }
    }
    
    /// <summary>
    /// Cancels a request for a specific instance
    /// </summary>
    /// <param name="slotId">The instance slot ID</param>
    /// <param name="onCancelled">Hook called when request is fully cancelled</param>
    public async Task CancelRequestAsync(int slotId, Action<int>? onCancelled = null)
    {
        if (Cathedral.Game.PlaygroundMode.IsActive)
        {
            onCancelled?.Invoke(slotId);
            return;
        }

        if (!_instances.TryGetValue(slotId, out var instance))
        {
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");
        }
        
        if (instance.CurrentRequestCancellation != null)
        {
            instance.CurrentRequestCancellation.Cancel();
            
            // Wait a moment for the cancellation to process
            await Task.Delay(100);
        }
        
        instance.IsActive = false;
        onCancelled?.Invoke(slotId);
    }
    
    /// <summary>
    /// Bounds an instance's history to what the slot's context window can hold, before a request goes
    /// out. A slot that keeps its history (<c>skipReset</c>) grows by one prompt + one reply per call,
    /// so without this a long-lived conversation eventually sends more tokens than <c>-c</c> allows and
    /// the server answers 400 <c>exceed_context_size_error</c> — which surfaces as a caller's fallback
    /// text, not as an obvious failure. Trimming keeps the system prompt (the persona) and the newest
    /// message (the pending prompt) and drops the oldest turns in between.
    /// </summary>
    /// <param name="maxTokens">The request's own completion limit, reserved on top of the prompt.</param>
    private async Task EnsureContextFitsAsync(LlamaInstance instance, int maxTokens)
    {
        int budget    = Math.Max(instance.MaxContextTokens - maxTokens - ContextEstimateMargin, MinPromptBudget);
        int estimated = instance.EstimateConversationTokens();
        if (estimated <= budget) return;

        int removed = instance.TrimToFitContext(budget);
        await LogWarningAsync(
            $"Slot {instance.SlotId}: conversation ≈{estimated} tokens exceeds the {budget}-token prompt " +
            $"budget ({instance.MaxContextTokens} ctx − {maxTokens} max_tokens − {ContextEstimateMargin} margin). " +
            $"Removed {removed} old message(s), now ≈{instance.EstimateConversationTokens()}.");
        try { LLMLogger.LogSlotIssue(instance.SlotId, "Context Trimmed", $"Removed {removed} messages, estimated {estimated} tokens"); } catch { }
    }

    /// <summary>
    /// Resets an instance, keeping the system prompt but removing other messages
    /// </summary>
    /// <param name="slotId">The instance slot ID</param>
    public void ResetInstance(int slotId)
    {
        // Fake slot IDs used by playground mode — nothing to reset.
        if (Cathedral.Game.PlaygroundMode.IsActive) return;

        if (!_instances.TryGetValue(slotId, out var instance))
        {
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");
        }
        
        if (instance.IsActive)
        {
            throw new InvalidOperationException($"Cannot reset instance {slotId} while it's processing a request.");
        }
        
        instance.Reset();
        Console.WriteLine($"✁EReset instance {slotId}.");

        // Write a marker file so the LLM monitor can show a visible cache-reset separator.
        if (_sessionLogDir != null)
        {
            var slotDir = Path.Combine(_sessionLogDir, $"slot_{slotId}");
            if (Directory.Exists(slotDir))
            {
                var ts = DateTime.Now.ToString("HH-mm-ss-fff");
                var markerPath = Path.Combine(slotDir, $"cache_reset_{instance.RequestCount:D3}_{ts}.txt");
                File.WriteAllText(markerPath, $"Cache reset at {DateTime.Now:HH:mm:ss.fff} after request {instance.RequestCount}");
            }
        }
    }
    
    /// <summary>
    /// Manually trims an instance's conversation history to fit within context window.
    /// Removes oldest messages while keeping system prompt and recent messages.
    /// </summary>
    /// <param name="slotId">The instance slot ID</param>
    /// <param name="maxTokens">Optional: custom max tokens (defaults to instance's MaxContextTokens - 512)</param>
    /// <returns>Number of messages removed</returns>
    public int TrimInstanceContext(int slotId, int? maxTokens = null)
    {
        if (Cathedral.Game.PlaygroundMode.IsActive) return 0;

        if (!_instances.TryGetValue(slotId, out var instance))
        {
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");
        }
        
        if (instance.IsActive)
        {
            throw new InvalidOperationException($"Cannot trim instance {slotId} while it's processing a request.");
        }
        
        int estimatedBefore = instance.EstimateConversationTokens();
        int removedCount = instance.TrimToFitContext(maxTokens);
        int estimatedAfter = instance.EstimateConversationTokens();
        
        Console.WriteLine($"✁ETrimmed instance {slotId}: removed {removedCount} messages ({estimatedBefore} ↁE{estimatedAfter} tokens).");
        
        return removedCount;
    }
    
    /// <summary>
    /// Gets the estimated token count for an instance's conversation history.
    /// </summary>
    /// <param name="slotId">The instance slot ID</param>
    /// <returns>Estimated token count</returns>
    public int GetInstanceTokenCount(int slotId)
    {
        if (Cathedral.Game.PlaygroundMode.IsActive) return 0;

        if (!_instances.TryGetValue(slotId, out var instance))
        {
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");
        }
        
        return instance.EstimateConversationTokens();
    }
    
    /// <summary>
    /// Gets information about an instance
    /// </summary>
    public LlamaInstance? GetInstance(int slotId)
    {
        return _instances.TryGetValue(slotId, out var instance) ? instance : null;
    }
    
    /// <summary>
    /// Gets all instances
    /// </summary>
    public IReadOnlyDictionary<int, LlamaInstance> GetAllInstances()
    {
        return _instances.AsReadOnly();
    }
    
    /// <summary>
    /// Stops the Llama server
    /// </summary>
    public void StopServer()
    {
        if (_disposed) return;
        
        try
        {
            _logWriter?.Close();
            _logWriter?.Dispose();
        }
        catch
        {
            // Ignore disposal errors
        }
        
        if (_llamaProcess != null)
        {
            try
            {
                if (!_llamaProcess.HasExited)
                {
                    Console.WriteLine("Stopping llama server...");
                    _llamaProcess.Kill();
                    _llamaProcess.WaitForExit(5000);
                }
            }
            catch (InvalidOperationException)
            {
                // Process was already disposed
            }
            finally
            {
                try
                {
                    _llamaProcess.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _llamaProcess = null;
            }
        }
        
        _isServerReady = false;
    }

    /// <summary>
    /// Tears down a failed server attempt so the next rung of the ladder can start cleanly.
    /// <para>Closes the log writer as well as killing the process. Each attempt opens
    /// <c>llama-server.log</c> with <c>append: false</c>, so leaving the previous writer open would
    /// make the retry fail on a locked file — and the failure would look like a second backend
    /// fault rather than a bookkeeping one.</para>
    /// </summary>
    private void KillServerProcess()
    {
        if (_llamaProcess != null)
        {
            try
            {
                if (!_llamaProcess.HasExited)
                {
                    _llamaProcess.Kill(entireProcessTree: true);
                    _llamaProcess.WaitForExit(5000);
                }
            }
            catch { /* already gone */ }
            finally
            {
                try { _llamaProcess.Dispose(); } catch { }
                _llamaProcess = null;
            }
        }

        try
        {
            _logWriter?.Flush();
            _logWriter?.Dispose();
        }
        catch { }
        _logWriter = null;

        _isServerReady = false;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        StopServer();
        _httpClient?.Dispose();
        
        // Cancel all active requests
        foreach (var instance in _instances.Values)
        {
            instance.CurrentRequestCancellation?.Cancel();
        }
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
    
    // Private helper methods
    
    private async Task<bool> IsServerRunningAsync()
    {
        try
        {
            using var response = await _httpClient.GetAsync("v1/models");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Resolves the llama-server executable and the single configured model file, both located
    /// under the project's <c>models</c> directory (found by walking up from the app base dir).
    /// </summary>
    /// <summary>
    /// Where the server executable and the model are. Both come from <see cref="LlamaRuntime"/>,
    /// which anchors on the application directory and bounds its search — this used to walk up
    /// from the base directory with no limit, which on a machine missing the folder does not stop
    /// at the install directory but climbs to the drive root.
    /// </summary>
    private (string serverPath, string modelPath) ResolvePaths()
    {
        ModelsDirectory.Require();   // throws with a message naming where it looked

        var serverPath = LlamaRuntime.ServerPath;
        var modelPath  = LlamaRuntime.ModelPath;

        if (serverPath == null || modelPath == null)
            throw new DirectoryNotFoundException("Could not resolve the llama.cpp runtime paths.");

        return (serverPath, modelPath);
    }
    
    /// <summary>
    /// Starts the server on the configured device, and on failure steps down to the CPU.
    ///
    /// <para>A GPU that fails at load — an outdated driver, a backend built against another
    /// llama.cpp revision, a card too small for the model, a laptop that woke up without its
    /// discrete GPU — must not be the difference between playing the game and not playing it. The
    /// CPU path always works, so a failed GPU start is a slow game rather than no game.</para>
    ///
    /// <para>A downgrade is <b>persisted</b>: whatever the probe once measured, this machine has
    /// now demonstrated otherwise, and re-attempting a device that has already failed once costs
    /// the player the same minute of loading on every launch. The Settings screen's re-detect
    /// button is how they ask for it to be tried again.</para>
    /// </summary>
    private async Task<bool> StartWithFallbackAsync(string serverPath, string modelPath)
    {
        foreach (var backend in BuildDeviceLadder())
        {
            if (backend != null)
                Console.WriteLine($"Starting llama server on {backend.Name}...");
            else
                Console.WriteLine("Starting llama server on CPU...");

            await StartServerProcessAsync(serverPath, modelPath, _contextSize, backend);

            if (await WaitForServerReadyAsync())
                return true;

            // Nothing is recoverable in place: llama-server has either exited or is wedged
            // half-loaded, and the next attempt needs the port back.
            KillServerProcess();

            if (backend != null)
            {
                LogWarning($"The {backend.Name} backend failed to serve the model. Falling back to CPU.");
                UserSettings.LlmDevice = LlamaComputeDevice.Cpu;
                UserSettings.LlmProbedDevice = LlamaComputeDevice.Cpu;
                UserSettings.LlmProbeSummary = $"CPU ({backend.Name} failed to start)";
                UserSettings.Save();
            }
        }

        return false;
    }

    /// <summary>
    /// The devices to try, best first. A GPU rung is only present when the settings ask for one
    /// <i>and</i> a matching backend is actually installed; the CPU is always last and always
    /// present, which is what makes the ladder terminate.
    /// </summary>
    private static IEnumerable<LlamaBackend?> BuildDeviceLadder()
    {
        // --cpu / --gpu win over both the setting and the probe, for this run only.
        var device = Config.Debug.ForcedLlmDevice ?? UserSettings.EffectiveLlmDevice;

        if (device == LlamaComputeDevice.Gpu)
        {
            var backends = LlamaRuntime.DiscoverBackends();

            // Prefer the one the probe picked; fall back to whatever else is installed, which
            // covers a player who chose GPU manually on a machine that was never probed.
            var chosen = backends.FirstOrDefault(b =>
                             string.Equals(b.Name, UserSettings.LlmProbedBackend, StringComparison.OrdinalIgnoreCase))
                         ?? backends.FirstOrDefault();

            if (chosen != null) yield return chosen;
        }

        yield return null;   // CPU
    }

    /// <summary>
    /// Builds the server's command line. Two arguments are deliberately <b>omitted</b> rather than
    /// given defaults, because llama.cpp's own defaults are better than any constant could be:
    ///
    /// <list type="bullet">
    /// <item><c>-ngl</c> defaults to <c>auto</c>, and <c>--fit</c> (on by default) sizes the offload
    /// to the device's free memory with a 1 GiB margin. Passing <c>-ngl 99</c>, as this did, means
    /// "all layers in VRAM" and defeats that fitting — it is the direct cause of an out-of-memory
    /// failure on any card too small for the whole model.</item>
    /// <item><c>-t</c> defaults to the host's core count. The old hardcoded 6 was wrong in both
    /// directions on most machines.</item>
    /// </list>
    ///
    /// Both are still settable — the Settings screen writes them — but only a player's explicit
    /// choice puts them on the command line.
    /// </summary>
    private static string BuildServerArguments(string modelPath, int contextSize, LlamaBackend? backend)
    {
        // Invariant formatting: the server parses these as C-locale decimals, so a French/German
        // locale writing "1,1" would make llama-server reject the argument.
        var inv = CultureInfo.InvariantCulture;
        var args = new StringBuilder();

        args.Append($"-m \"{modelPath}\" -c {contextSize} --port 8080");
        args.Append(" --cache-type-k f16 --cache-type-v f16");
        args.Append($" --repeat-penalty {Config.LLM.RepeatPenalty.ToString(inv)}");
        args.Append($" --frequency-penalty {Config.LLM.FrequencyPenalty.ToString(inv)}");
        args.Append($" --dry-multiplier {Config.LLM.DryMultiplier.ToString(inv)}");

        if (backend == null)
        {
            // Explicit, not merely absent: a backend may still be discoverable beside the
            // executable, and this run has decided not to offload to it.
            args.Append(" -ngl 0");
        }
        else if (UserSettings.LlmGpuLayers >= 0)
        {
            args.Append($" -ngl {UserSettings.LlmGpuLayers}");
        }

        if (UserSettings.LlmCpuThreads > 0)
            args.Append($" -t {UserSettings.LlmCpuThreads}");

        args.Append(" --verbose");
        return args.ToString();
    }

    private async Task StartServerProcessAsync(string serverPath, string modelPath, int contextSize, LlamaBackend? backend)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = BuildServerArguments(modelPath, contextSize, backend),
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        LlamaRuntime.ApplyBackend(startInfo, backend);

        _llamaProcess = Process.Start(startInfo);

        if (_llamaProcess == null)
        {
            throw new InvalidOperationException("Failed to start llama server process.");
        }

        // The server's own log, if diagnostics are available at all. There is deliberately no
        // fallback to the working directory: a null session directory means that directory could
        // not be written to, so falling back would fail again — and this time the exception would
        // propagate out of the start path and be reported as the server having failed.
        _logWriter = null;
        if (_sessionLogDir != null)
        {
            try
            {
                _logWriter = new StreamWriter(Path.Combine(_sessionLogDir, "llama-server.log"), append: false);
            }
            catch (Exception ex)
            {
                _loggingDisabled = true;
                Console.Error.WriteLine($"LLM logging disabled: cannot open llama-server.log ({ex.Message}).");
            }
        }

        // The two pump tasks below outlive a failed attempt: KillServerProcess clears both fields
        // so the next rung starts clean, and a task still reading the field would then throw a
        // NullReferenceException and log it as if the backend had misbehaved. Capture what they
        // need instead, so a torn-down attempt ends its pumps quietly. The writer may be null when
        // logging is unavailable, in which case the pumps still run — they also drive the loading
        // progress bar, which is not diagnostics.
        var process = _llamaProcess;
        var writer = _logWriter;

        // Record when model loading started
        _loadingStartTime = DateTime.Now;
        _loadingProgress = 0.05f;
        _loadingStatusMessage = "Launching server process...";
        
        // Log stdout in background
        _ = Task.Run(async () =>
        {
            try
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (line != null)
                    {
                        if (writer != null)
                        {
                            await writer.WriteLineAsync($"[STDOUT] {DateTime.Now:HH:mm:ss.fff} {line}");
                            await writer.FlushAsync();
                        }
                        // Into log.txt regardless, and to the file only — llama-server is far too
                        // verbose to show on screen, but it is exactly what is needed when the
                        // model fails to load. This is the only copy in a shipped build.
                        GameLog.WriteToFileOnly($"[llama] {line}");
                        // Outside both: this drives the loading bar, not the log.
                        ParseLoadingProgress(line);
                    }
                }
            }
            catch (ObjectDisposedException) { /* attempt torn down by the fallback ladder */ }
            catch (Exception ex)
            {
                LogError($"Error logging stdout: {ex.Message}");
            }
        });

        // Log stderr in background (llama.cpp writes most progress to stderr)
        _ = Task.Run(async () =>
        {
            try
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (line != null)
                    {
                        if (writer != null)
                        {
                            await writer.WriteLineAsync($"[STDERR] {DateTime.Now:HH:mm:ss.fff} {line}");
                            await writer.FlushAsync();
                        }
                        GameLog.WriteToFileOnly($"[llama] {line}");
                        // Outside both: llama.cpp reports its loading stages on stderr, so the
                        // progress bar depends on this line even when nothing is logged.
                        ParseLoadingProgress(line);
                    }
                }
            }
            catch (ObjectDisposedException) { /* attempt torn down by the fallback ladder */ }
            catch (Exception ex)
            {
                LogError($"Error logging stderr: {ex.Message}");
            }
        });
        
        // Give the process a moment to start
        await Task.Delay(2000);
    }

    /// <summary>
    /// Parses a line from the llama-server process output to update loading progress.
    /// llama.cpp writes loading stages to stderr in a predictable pattern.
    /// </summary>
    private void ParseLoadingProgress(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        // Progress milestones based on llama.cpp loading output patterns
        if (line.Contains("llm_load_print_meta") || line.Contains("print_meta"))
        {
            UpdateLoadingStage(0.20f, "Reading model metadata...");
        }
        else if (line.Contains("llm_load_tensors") && line.Contains("offload"))
        {
            UpdateLoadingStage(0.50f, "Offloading layers to GPU...");
        }
        else if (line.Contains("llm_load_tensors") && !line.Contains("offload"))
        {
            UpdateLoadingStage(0.35f, "Loading model tensors...");
        }
        else if (line.Contains("llm_load_end") || line.Contains("load_end"))
        {
            UpdateLoadingStage(0.80f, "Finalizing model load...");
        }
        else if (line.Contains("slot") && line.Contains("id") && !line.Contains("save"))
        {
            UpdateLoadingStage(0.90f, "Initializing inference slots...");
        }
        else if (line.Contains("all slots are idle") || line.Contains("server is listening"))
        {
            UpdateLoadingStage(0.95f, "Server ready, waiting for first request...");
        }
    }

    /// <summary>
    /// How long the probe will wait for the loading screen before going ahead without it. The gate
    /// is a courtesy, not a dependency: a UI path that never reaches <c>LLMLoading</c> must not be
    /// able to leave the language model unstarted forever.
    /// </summary>
    private const int LoadingScreenWaitMs = 20_000;

    private readonly TaskCompletionSource<bool> _loadingScreenVisible =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>
    /// Told by the UI that the loading screen is on screen, releasing the hardware probe. Safe to
    /// call more than once and from any thread; only the first call does anything.
    /// </summary>
    public void NotifyLoadingScreenVisible() => _loadingScreenVisible.TrySetResult(true);

    private async Task WaitForLoadingScreenAsync()
    {
        UpdateLoadingStage(0.01f, "Preparing...");
        var completed = await Task.WhenAny(_loadingScreenVisible.Task, Task.Delay(LoadingScreenWaitMs));
        if (completed != _loadingScreenVisible.Task)
            Console.WriteLine("Loading screen did not appear in time; probing anyway.");
    }

    /// <summary>
    /// Publishes one line of probe commentary. Unlike <see cref="UpdateLoadingStage"/> this does not
    /// ratchet progress: nothing is loading yet, and claiming otherwise would put the bar most of
    /// the way along before the model has been opened. The message and the spinner carry it instead.
    /// </summary>
    private void ReportProbeStage(string status)
    {
        _loadingStatusMessage = status;
        var elapsed = _loadingStartTime != DateTime.MinValue
            ? (DateTime.Now - _loadingStartTime).TotalSeconds : 0;
        LoadingProgressUpdated?.Invoke(this, new LoadingProgressEventArgs(_loadingProgress, status, elapsed));
    }

    private void UpdateLoadingStage(float minProgress, string status)
    {
        if (_loadingProgress < minProgress)
        {
            _loadingProgress = minProgress;
            _loadingStatusMessage = status;
            var elapsed = _loadingStartTime != DateTime.MinValue
                ? (DateTime.Now - _loadingStartTime).TotalSeconds : 0;
            LoadingProgressUpdated?.Invoke(this, new LoadingProgressEventArgs(_loadingProgress, status, elapsed));
        }
    }
    
    private async Task<bool> WaitForServerReadyAsync()
    {
        var timeout = TimeSpan.FromMinutes(8);
        var startTime = DateTime.Now;
        var retryCount = 0;
        
        Console.WriteLine("Waiting for llama server to load model...");
        Console.WriteLine("Note: This may take 2-5 minutes depending on your hardware.");
        
        while (DateTime.Now - startTime < timeout)
        {
            // A server that has exited will never answer, and this loop cannot tell that apart
            // from one still loading — both are a refused connection. Without this check a crashed
            // start costs the full eight-minute timeout before anything reacts, which is the
            // difference between the CPU fallback being a safety net and being indistinguishable
            // from a hang. Backends crash on load for ordinary reasons: a stale driver, a pack
            // built against another llama.cpp revision, a card too small for the model.
            try
            {
                if (_llamaProcess is { HasExited: true } exited)
                {
                    LogError($"llama-server exited during startup (code {exited.ExitCode}). " +
                             $"See llama-server.log in {_sessionLogDir ?? "the log directory"} for the reason.");
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                return false;   // process already torn down
            }

            try
            {
                var testRequest = new
                {
                    model = "local",
                    messages = new[]
                    {
                        new { role = "user", content = "test" }
                    },
                    max_tokens = 1,
                    temperature = Config.LLM.UtilityTemperature
                };
                
                var testResponse = await _httpClient.PostAsJsonAsync("v1/chat/completions", testRequest);
                
                if (testResponse.IsSuccessStatusCode)
                {
                    _loadingProgress = 1.0f;
                    _loadingStatusMessage = "Model loaded!";
                    var finalElapsed = (DateTime.Now - startTime).TotalSeconds;
                    LoadingProgressUpdated?.Invoke(this, new LoadingProgressEventArgs(1.0f, "Model loaded!", finalElapsed));
                    return true;
                }
                
                if (testResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    retryCount++;
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
                    // Time-based progress: soft-cap at 90% until confirmed ready
                    // Assumes ~90 seconds typical load time; adjust _loadingProgress upward only
                    var timeProgress = (float)Math.Min(0.90, elapsed / 90.0);
                    if (_loadingProgress < timeProgress)
                    {
                        _loadingProgress = timeProgress;
                        LoadingProgressUpdated?.Invoke(this, new LoadingProgressEventArgs(
                            _loadingProgress, _loadingStatusMessage, elapsed));
                    }
                    if (retryCount % 5 == 0)
                    {
                        Console.WriteLine($"Model still loading... ({elapsed:F0}s elapsed, attempt {retryCount})");
                    }
                    await Task.Delay(3000);
                }
                else
                {
                    await Task.Delay(1000);
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("503"))
            {
                retryCount++;
                var elapsed = (DateTime.Now - startTime).TotalSeconds;
                var timeProgress = (float)Math.Min(0.90, elapsed / 90.0);
                if (_loadingProgress < timeProgress)
                {
                    _loadingProgress = timeProgress;
                    LoadingProgressUpdated?.Invoke(this, new LoadingProgressEventArgs(
                        _loadingProgress, _loadingStatusMessage, elapsed));
                }
                if (retryCount % 5 == 0)
                {
                    Console.WriteLine($"Model still loading... ({elapsed:F0}s elapsed, attempt {retryCount})");
                }
                await Task.Delay(3000);
            }
            catch (Exception)
            {
                await Task.Delay(2000);
            }
        }
        
        return false;
    }
    
    private string ExtractRoleFromSystemPrompt(string systemPrompt)
    {
        // Extract role from "You are a [role]." pattern
        if (systemPrompt.StartsWith("You are a ", StringComparison.OrdinalIgnoreCase))
        {
            var role = systemPrompt.Substring(10).TrimEnd('.', ' ');
            // Capitalize first letter
            if (role.Length > 0)
            {
                return char.ToUpper(role[0]) + role.Substring(1);
            }
        }
        return "Instance";
    }
    
    private async Task PreCacheSystemPromptAsync(LlamaInstance instance)
    {
        var warmupRequest = new
        {
            model = "local",
            messages = new[]
            {
                new { role = "system", content = instance.SystemPrompt }
            },
            slot_id = instance.SlotId,
            cache_prompt = true,
            max_tokens = 1,
            temperature = Config.LLM.UtilityTemperature,
            stream = false
        };
        
        var response = await _httpClient.PostAsJsonAsync("v1/chat/completions", warmupRequest);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Failed to pre-cache system prompt: {response.StatusCode} - {errorContent}");
        }
    }
}
