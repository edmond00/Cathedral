using System.Diagnostics;
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
    private int _nextSlotId = 0;
    private bool _isServerReady = false;
    private bool _disposed = false;
    private int _contextSize = 4096; // Default context size for both server and instances
    
    // Model aliases and their corresponding file names
    private readonly Dictionary<string, string> _modelAliases = new()
    {
        { "tiny", "qwen2-0_5b-instruct-q4_k_m.gguf" },
        { "medium", "phi-4-q2_k.gguf" }
    };
    
    private string _currentModelAlias = "tiny"; // Default model
    
    // Events
    public event EventHandler<ServerStatusEventArgs>? ServerReady;
    public event EventHandler<TokenStreamedEventArgs>? TokenStreamed;
    public event EventHandler<RequestCompletedEventArgs>? RequestCompleted;
    
    public bool IsServerReady => _isServerReady;
    
    /// <summary>
    /// Gets the context size configured for the server and instances
    /// </summary>
    public int ContextSize => _contextSize;
    
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
    
    public LlamaServerManager(string? baseUrl = null)
    {
        _baseUrl = baseUrl ?? "http://127.0.0.1:8080/";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromMinutes(10)
        };
        
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
    /// <param name="modelAlias">Model alias to use ("tiny" or "medium"). Defaults to "tiny"</param>
    /// <param name="modelPath">Optional custom model path (overrides alias)</param>
    /// <param name="serverPath">Optional custom server executable path</param>
    /// <param name="contextSize">Maximum context size in tokens (default: 4096). Used for both server and all instances.</param>
    public async Task StartServerAsync(Action<bool>? onServerReady = null, string? modelAlias = null, string? modelPath = null, string? serverPath = null, int contextSize = 4096)
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
                Console.WriteLine("Llama server is already running.");
                ServerReady?.Invoke(this, new ServerStatusEventArgs(true, "Server already running"));
                onServerReady?.Invoke(true);
                
                // Log to LLM logger if available
                try { LLMLogger.LogServerInitResult(true, "Server already running", (DateTime.Now - startTime).TotalSeconds); } catch { }
                return;
            }
            
            Console.WriteLine("Starting llama server...");
            
            // Set the current model alias - auto-select largest if null
            if (modelAlias == null)
            {
                var largestModel = FindLargestGgufModel();
                if (largestModel != null)
                {
                    // Check if this model is in our aliases
                    var matchingAlias = _modelAliases.FirstOrDefault(kvp => kvp.Value == largestModel).Key;
                    if (matchingAlias != null)
                    {
                        _currentModelAlias = matchingAlias;
                        // Get file size for display
                        try
                        {
                            var projectRoot = AppDomain.CurrentDomain.BaseDirectory;
                            while (projectRoot != null && !Directory.Exists(Path.Combine(projectRoot, "models")))
                            {
                                projectRoot = Directory.GetParent(projectRoot)?.FullName;
                            }
                            if (projectRoot != null)
                            {
                                var fileInfo = new FileInfo(Path.Combine(projectRoot, "models", largestModel));
                                Console.WriteLine($"Auto-selected largest model: {_currentModelAlias} ({_modelAliases[_currentModelAlias]}) - {fileInfo.Length / (1024.0 * 1024.0):F1} MB");
                            }
                            else
                            {
                                Console.WriteLine($"Auto-selected largest model: {_currentModelAlias} ({_modelAliases[_currentModelAlias]})");
                            }
                        }
                        catch
                        {
                            Console.WriteLine($"Auto-selected largest model: {_currentModelAlias} ({_modelAliases[_currentModelAlias]})");
                        }
                    }
                    else
                    {
                        // Model not in aliases - this shouldn't happen with the current logic,
                        // but handle it gracefully by falling back to tiny
                        _currentModelAlias = "tiny";
                        Console.WriteLine($"Auto-selected model not in aliases, using default: {_currentModelAlias} ({_modelAliases[_currentModelAlias]})");
                    }
                }
                else
                {
                    // Fallback to tiny if no models found
                    _currentModelAlias = "tiny";
                    Console.WriteLine($"No models found for auto-selection, using default: {_currentModelAlias} ({_modelAliases[_currentModelAlias]})");
                }
            }
            else
            {
                _currentModelAlias = modelAlias;
                Console.WriteLine($"Using model: {_currentModelAlias} ({_modelAliases[_currentModelAlias]})");
            }
            
            // Find paths
            var (resolvedServerPath, resolvedModelPath) = ResolvePaths(serverPath, modelPath, _currentModelAlias);
            
            // Log initialization start
            try { LLMLogger.LogServerInitStart(_currentModelAlias, resolvedServerPath, resolvedModelPath); } catch { }
            
            // Validate paths
            if (!File.Exists(resolvedServerPath))
            {
                var errorMsg = $"Llama server not found at: {resolvedServerPath}";
                LogError(errorMsg);
                ServerReady?.Invoke(this, new ServerStatusEventArgs(false, errorMsg));
                onServerReady?.Invoke(false);
                return;
            }
            
            if (!File.Exists(resolvedModelPath))
            {
                var errorMsg = $"Model file not found at: {resolvedModelPath}";
                LogError(errorMsg);
                ServerReady?.Invoke(this, new ServerStatusEventArgs(false, errorMsg));
                onServerReady?.Invoke(false);
                return;
            }
            
            // Start the server process
            await StartServerProcessAsync(resolvedServerPath, resolvedModelPath, _contextSize);
            
            // Wait for server to be ready
            var isReady = await WaitForServerReadyAsync();
            
            _isServerReady = isReady;
            var message = isReady ? "Server started successfully" : "Server failed to start";
            var duration = (DateTime.Now - startTime).TotalSeconds;
            
            Console.WriteLine(isReady ? "✓ Llama server and model loaded successfully." : "✗ Failed to start Llama server.");
            
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
    /// Creates a new LLM instance with the given system prompt
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
        
        var slotId = _nextSlotId++;
        var instance = new LlamaInstance(slotId, systemPrompt)
        {
            MaxContextTokens = maxContextTokens ?? _contextSize
        };
        _instances[slotId] = instance;
        
        // Pre-cache the system prompt
        try
        {
            await PreCacheSystemPromptAsync(instance);
            Console.WriteLine($"✓ Created instance {slotId} with system prompt cached.");
            
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
                ["slot_id"] = slotId
            };
            
            if (!string.IsNullOrWhiteSpace(gbnfGrammar))
            {
                requestData["grammar"] = gbnfGrammar;
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
                                    
                                    // Store both the original token and normalized version
                                    probabilities[token] = probability;
                                    
                                    // Also store case-insensitive version for easier lookup
                                    var normalizedToken = token.Trim().ToLower();
                                    if (!probabilities.ContainsKey(normalizedToken))
                                    {
                                        probabilities[normalizedToken] = probability;
                                    }
                                    else
                                    {
                                        // Accumulate probability if multiple tokens normalize to same string
                                        probabilities[normalizedToken] += probability;
                                    }
                                }
                            }
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
            
            return probabilities;
        }
        finally
        {
            instance.IsActive = false;
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
        
        var cancellationToken = new CancellationTokenSource();
        instance.CurrentRequestCancellation = cancellationToken;
        
        try
        {
            var startTime = DateTime.Now;
            var fullResponse = new StringBuilder();
            
            // Check if conversation history is too long and trim if needed
            int estimatedTokens = instance.EstimateConversationTokens();
            if (estimatedTokens > instance.MaxContextTokens - 512)
            {
                await LogWarningAsync($"Slot {slotId}: Conversation exceeds context window ({estimatedTokens} tokens). Trimming history...");
                int removedCount = instance.TrimToFitContext();
                await LogWarningAsync($"Slot {slotId}: Removed {removedCount} old messages to fit context window");
                
                // Log to LLM logger
                try { LLMLogger.LogSlotIssue(slotId, "Context Trimmed", $"Removed {removedCount} messages, estimated {estimatedTokens} tokens"); } catch { }
            }
            
            // Create the base request
            var requestData = new Dictionary<string, object>
            {
                ["model"] = "local",
                ["messages"] = instance.GetMessages(),
                ["max_tokens"] = 2048,
                ["stream"] = true,
                ["cache_prompt"] = true,
                ["slot_id"] = slotId
            };
            
            // Add GBNF grammar if provided
            if (!string.IsNullOrWhiteSpace(gbnfGrammar))
            {
                requestData["grammar"] = gbnfGrammar;
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
            var duration = DateTime.Now - startTime;
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
    /// Resets an instance, keeping the system prompt but removing other messages
    /// </summary>
    /// <param name="slotId">The instance slot ID</param>
    public void ResetInstance(int slotId)
    {
        if (!_instances.TryGetValue(slotId, out var instance))
        {
            throw new ArgumentException($"Instance with slot ID {slotId} not found.");
        }
        
        if (instance.IsActive)
        {
            throw new InvalidOperationException($"Cannot reset instance {slotId} while it's processing a request.");
        }
        
        instance.Reset();
        Console.WriteLine($"✓ Reset instance {slotId}.");
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
        
        Console.WriteLine($"✓ Trimmed instance {slotId}: removed {removedCount} messages ({estimatedBefore} → {estimatedAfter} tokens).");
        
        return removedCount;
    }
    
    /// <summary>
    /// Gets the estimated token count for an instance's conversation history.
    /// </summary>
    /// <param name="slotId">The instance slot ID</param>
    /// <returns>Estimated token count</returns>
    public int GetInstanceTokenCount(int slotId)
    {
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
    /// Gets all available model aliases
    /// </summary>
    public IReadOnlyDictionary<string, string> GetAvailableModels()
    {
        return _modelAliases.AsReadOnly();
    }
    
    /// <summary>
    /// Gets the current model alias
    /// </summary>
    public string GetCurrentModelAlias()
    {
        return _currentModelAlias;
    }
    
    /// <summary>
    /// Gets the current model file name
    /// </summary>
    public string GetCurrentModelFileName()
    {
        return _modelAliases.TryGetValue(_currentModelAlias, out var fileName) ? fileName : "Unknown";
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
    
    /// <summary>
    /// Finds the largest GGUF model file in the models directory
    /// </summary>
    /// <returns>The filename of the largest GGUF model, or null if none found</returns>
    private string? FindLargestGgufModel()
    {
        try
        {
            var currentDir = AppDomain.CurrentDomain.BaseDirectory;
            var projectRoot = currentDir;
            
            // Navigate up to find the directory containing the models folder
            while (projectRoot != null && !Directory.Exists(Path.Combine(projectRoot, "models")))
            {
                projectRoot = Directory.GetParent(projectRoot)?.FullName;
            }
            
            if (projectRoot == null)
            {
                return null;
            }
            
            var modelsDir = Path.Combine(projectRoot, "models");
            var ggufFiles = Directory.GetFiles(modelsDir, "*.gguf", SearchOption.TopDirectoryOnly);
            
            if (ggufFiles.Length == 0)
            {
                return null;
            }
            
            // Find the largest file by size
            var largestFile = ggufFiles
                .Select(f => new FileInfo(f))
                .OrderByDescending(fi => fi.Length)
                .First();
            
            return largestFile.Name;
        }
        catch (Exception ex)
        {
            LogWarning($"Error finding largest GGUF model: {ex.Message}");
            return null;
        }
    }
    
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
    
    private (string serverPath, string modelPath) ResolvePaths(string? serverPath, string? modelPath, string modelAlias)
    {
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = currentDir;
        
        // Navigate up to find the directory containing the models folder
        while (projectRoot != null && !Directory.Exists(Path.Combine(projectRoot, "models")))
        {
            projectRoot = Directory.GetParent(projectRoot)?.FullName;
        }
        
        if (projectRoot == null)
        {
            throw new DirectoryNotFoundException("Could not find models directory.");
        }
        
        var resolvedServerPath = serverPath ?? Path.Combine(projectRoot, "models", "llama", "llama-server.exe");
        
        // Resolve model path using alias if no custom path is provided
        string resolvedModelPath;
        if (modelPath != null)
        {
            // If modelPath is just a filename (not a full path), resolve it relative to models directory
            if (!Path.IsPathRooted(modelPath) && !modelPath.Contains(Path.DirectorySeparatorChar) && !modelPath.Contains(Path.AltDirectorySeparatorChar))
            {
                resolvedModelPath = Path.Combine(projectRoot, "models", modelPath);
            }
            else
            {
                resolvedModelPath = modelPath;
            }
        }
        else if (_modelAliases.TryGetValue(modelAlias, out var modelFileName))
        {
            resolvedModelPath = Path.Combine(projectRoot, "models", modelFileName);
        }
        else
        {
            throw new ArgumentException($"Unknown model alias '{modelAlias}'. Available aliases: {string.Join(", ", _modelAliases.Keys)}");
        }
        
        return (resolvedServerPath, resolvedModelPath);
    }
    
    private async Task StartServerProcessAsync(string serverPath, string modelPath, int contextSize)
    {
        var logFilePath = Path.Combine(Environment.CurrentDirectory, "llama-server.log");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = $"-m \"{modelPath}\" -c {contextSize} --port 8080 --cache-type-k f16 --cache-type-v f16 --repeat-penalty 1.1 --frequency-penalty 0.5 --dry-multiplier 0.8 -ngl 99 --slot-save-path cache --verbose",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        _llamaProcess = Process.Start(startInfo);
        
        if (_llamaProcess == null)
        {
            throw new InvalidOperationException("Failed to start llama server process.");
        }
        
        // Create log file and start logging
        _logWriter = new StreamWriter(logFilePath, append: false);
        
        // Log stdout in background
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_llamaProcess.StandardOutput.EndOfStream)
                {
                    var line = await _llamaProcess.StandardOutput.ReadLineAsync();
                    if (line != null)
                    {
                        await _logWriter.WriteLineAsync($"[STDOUT] {DateTime.Now:HH:mm:ss.fff} {line}");
                        await _logWriter.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error logging stdout: {ex.Message}");
            }
        });
        
        // Log stderr in background
        _ = Task.Run(async () =>
        {
            try
            {
                while (!_llamaProcess.StandardError.EndOfStream)
                {
                    var line = await _llamaProcess.StandardError.ReadLineAsync();
                    if (line != null)
                    {
                        await _logWriter.WriteLineAsync($"[STDERR] {DateTime.Now:HH:mm:ss.fff} {line}");
                        await _logWriter.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                LogError($"Error logging stderr: {ex.Message}");
            }
        });
        
        // Give the process a moment to start
        await Task.Delay(2000);
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
                    temperature = 0.1
                };
                
                var testResponse = await _httpClient.PostAsJsonAsync("v1/chat/completions", testRequest);
                
                if (testResponse.IsSuccessStatusCode)
                {
                    return true;
                }
                
                if (testResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    retryCount++;
                    if (retryCount % 5 == 0)
                    {
                        var elapsed = (DateTime.Now - startTime).TotalSeconds;
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
                if (retryCount % 5 == 0)
                {
                    var elapsed = (DateTime.Now - startTime).TotalSeconds;
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
            temperature = 0.1,
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