using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

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
    public async Task StartServerAsync(Action<bool>? onServerReady = null, string? modelAlias = null, string? modelPath = null, string? serverPath = null)
    {
        try
        {
            // Check if server is already running
            if (await IsServerRunningAsync())
            {
                _isServerReady = true;
                Console.WriteLine("Llama server is already running.");
                ServerReady?.Invoke(this, new ServerStatusEventArgs(true, "Server already running"));
                onServerReady?.Invoke(true);
                return;
            }
            
            Console.WriteLine("Starting llama server...");
            
            // Set the current model alias
            _currentModelAlias = modelAlias ?? "tiny";
            Console.WriteLine($"Using model: {_currentModelAlias} ({_modelAliases[_currentModelAlias]})");
            
            // Find paths
            var (resolvedServerPath, resolvedModelPath) = ResolvePaths(serverPath, modelPath, _currentModelAlias);
            
            // Validate paths
            if (!File.Exists(resolvedServerPath))
            {
                var errorMsg = $"Llama server not found at: {resolvedServerPath}";
                Console.Error.WriteLine(errorMsg);
                ServerReady?.Invoke(this, new ServerStatusEventArgs(false, errorMsg));
                onServerReady?.Invoke(false);
                return;
            }
            
            if (!File.Exists(resolvedModelPath))
            {
                var errorMsg = $"Model file not found at: {resolvedModelPath}";
                Console.Error.WriteLine(errorMsg);
                ServerReady?.Invoke(this, new ServerStatusEventArgs(false, errorMsg));
                onServerReady?.Invoke(false);
                return;
            }
            
            // Start the server process
            await StartServerProcessAsync(resolvedServerPath, resolvedModelPath);
            
            // Wait for server to be ready
            var isReady = await WaitForServerReadyAsync();
            
            _isServerReady = isReady;
            var message = isReady ? "Server started successfully" : "Server failed to start";
            
            Console.WriteLine(isReady ? "✓ Llama server and model loaded successfully." : "✗ Failed to start Llama server.");
            
            ServerReady?.Invoke(this, new ServerStatusEventArgs(isReady, message));
            onServerReady?.Invoke(isReady);
        }
        catch (Exception ex)
        {
            var errorMsg = $"Error starting server: {ex.Message}";
            Console.Error.WriteLine(errorMsg);
            ServerReady?.Invoke(this, new ServerStatusEventArgs(false, errorMsg));
            onServerReady?.Invoke(false);
        }
    }
    
    /// <summary>
    /// Creates a new LLM instance with the given system prompt
    /// </summary>
    /// <param name="systemPrompt">The system prompt for this instance</param>
    /// <returns>The slot ID for this instance</returns>
    public async Task<int> CreateInstanceAsync(string systemPrompt)
    {
        if (!_isServerReady)
        {
            throw new InvalidOperationException("Server is not ready. Call StartServerAsync first.");
        }
        
        var slotId = _nextSlotId++;
        var instance = new LlamaInstance(slotId, systemPrompt);
        _instances[slotId] = instance;
        
        // Pre-cache the system prompt
        try
        {
            await PreCacheSystemPromptAsync(instance);
            Console.WriteLine($"✓ Created instance {slotId} with system prompt cached.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to pre-cache system prompt for instance {slotId}: {ex.Message}");
        }
        
        return slotId;
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
            throw new InvalidOperationException($"Instance {slotId} is already processing a request.");
        }
        
        instance.IsActive = true;
        instance.AddUserMessage(userMessage);
        
        var cancellationToken = new CancellationTokenSource();
        instance.CurrentRequestCancellation = cancellationToken;
        
        try
        {
            var startTime = DateTime.Now;
            var fullResponse = new StringBuilder();
            
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
                            Console.WriteLine($"DEBUG: Missing 'choices' in response: {jsonData}");
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
            
            if (!wasCancelled)
            {
                instance.AddAssistantResponse(responseText);
            }
            
            // Invoke completion callbacks
            onCompleted?.Invoke(slotId, responseText, wasCancelled);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, responseText, duration, wasCancelled));
        }
        catch (OperationCanceledException)
        {
            // Request was cancelled
            onCompleted?.Invoke(slotId, "", true);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, "", DateTime.Now - DateTime.Now, true));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error in request for slot {slotId}: {ex.Message}");
            Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.Error.WriteLine($"Inner exception: {ex.InnerException.Message}");
            }
            onCompleted?.Invoke(slotId, "", false);
            RequestCompleted?.Invoke(this, new RequestCompletedEventArgs(slotId, "", DateTime.Now - DateTime.Now, false));
        }
        finally
        {
            instance.IsActive = false;
            instance.CurrentRequestCancellation = null;
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
            resolvedModelPath = modelPath;
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
    
    private async Task StartServerProcessAsync(string serverPath, string modelPath)
    {
        var logFilePath = Path.Combine(Environment.CurrentDirectory, "llama-server.log");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = $"-m \"{modelPath}\" -c 4096 --port 8080 --cache-type-k f16 --cache-type-v f16 --repeat-penalty 1.1 --frequency-penalty 0.5 --dry-multiplier 0.8 -ngl 99 --slot-save-path cache --verbose",
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
                Console.WriteLine($"Error logging stdout: {ex.Message}");
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
                Console.WriteLine($"Error logging stderr: {ex.Message}");
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