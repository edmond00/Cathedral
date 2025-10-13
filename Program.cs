using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

// Single definition of the system prompt to avoid duplication
const string SYSTEM_PROMPT = @"You are a masterful fantasy RPG dungeon master and storyteller. Your role is to create immersive, engaging adventures in a rich fantasy world.";

static async Task PreCacheSystemPrompt(HttpClient http)
{
    Console.WriteLine("Pre-warming prompt cache with system message...");
    
    // Use the shared system prompt constant
    var systemPrompt = SYSTEM_PROMPT;
    
    var maxRetries = 5;
    var baseDelay = 2000;
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            // Use chat completions endpoint to match the actual conversation format
            var warmupRequest = new
            {
                model = "local",
                messages = new[]
                {
                    new { role = "system", content = systemPrompt }
                },
                slot_id = 0,
                cache_prompt = true,
                max_tokens = 1, // Minimal generation
                temperature = 0.1,
                stream = false // No need to stream for warmup
            };
            
            // Save warmup request to file for analysis (only on first attempt)
            if (attempt == 1)
            {
                var requestsDir = Path.Combine(Directory.GetCurrentDirectory(), "requests");
                Directory.CreateDirectory(requestsDir);
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                var warmupFileName = Path.Combine(requestsDir, $"warmup_{timestamp}.json");
                var warmupJson = JsonSerializer.Serialize(warmupRequest, new JsonSerializerOptions 
                { 
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                await File.WriteAllTextAsync(warmupFileName, warmupJson);
                Console.WriteLine($"[DEBUG] Warmup request saved to: {warmupFileName}");
            }

            var jsonContent = JsonContent.Create(warmupRequest);
            var response = await http.PostAsync("v1/chat/completions", jsonContent);
            
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("✓ System prompt cached in slot 0 successfully!");
                return; // Success, exit the retry loop
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                if (attempt < maxRetries)
                {
                    var delay = baseDelay * attempt; // Linear backoff
                    Console.WriteLine($"Model still loading, retrying pre-cache in {delay/1000} seconds... (attempt {attempt}/{maxRetries})");
                    await Task.Delay(delay);
                    continue;
                }
                else
                {
                    Console.WriteLine($"Warning: Failed to pre-cache system prompt after {maxRetries} attempts: {response.StatusCode}");
                    Console.WriteLine($"Response: {errorContent}");
                    return;
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Warning: Failed to pre-cache system prompt: {response.StatusCode}");
                Console.WriteLine($"Response: {errorContent}");
                return;
            }
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("503") && attempt < maxRetries)
        {
            var delay = baseDelay * attempt;
            Console.WriteLine($"Service unavailable during pre-cache, retrying in {delay/1000} seconds... (attempt {attempt}/{maxRetries})");
            await Task.Delay(delay);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Error pre-caching system prompt (attempt {attempt}/{maxRetries}): {ex.Message}");
            if (attempt >= maxRetries)
            {
                Console.WriteLine("Pre-caching failed, but continuing without cache optimization.");
                return;
            }
            await Task.Delay(baseDelay);
        }
    }
}

// Usage: LlamaCli.exe "prompt1" "prompt2" "prompt3" ... or LlamaCli.exe --duplicate "prompt"
if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: LlamaCli <prompt1> [prompt2] [prompt3] ...");
    Console.Error.WriteLine("       LlamaCli --duplicate \"prompt\" (sends same request twice for cache testing)");
    Environment.Exit(2);
}

// Check for duplicate mode
bool duplicateMode = args.Length == 2 && args[0] == "--duplicate";
if (duplicateMode)
{
    Console.Error.WriteLine("Duplicate mode: Will send the same request twice to test caching");
}

// Change if your llama-server runs elsewhere/other port
var baseUrl = Environment.GetEnvironmentVariable("LLAMA_SERVER") ?? "http://127.0.0.1:8080/";

Process? llamaProcess = null;
StreamWriter? logWriter = null;

// Cleanup function to stop the server
void StopLlamaServer()
{
    // Close log writer first
    try
    {
        logWriter?.Close();
        logWriter?.Dispose();
    }
    catch
    {
        // Ignore disposal errors
    }
    
    if (llamaProcess != null)
    {
        try
        {
            if (!llamaProcess.HasExited)
            {
                Console.Error.WriteLine("Stopping llama server...");
                llamaProcess.Kill();
                llamaProcess.WaitForExit(5000); // Wait up to 5 seconds
            }
        }
        catch (InvalidOperationException)
        {
            // Process was already disposed or never started
        }
        finally
        {
            try
            {
                llamaProcess.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
            llamaProcess = null;
        }
    }
}

// Register cleanup on exit
AppDomain.CurrentDomain.ProcessExit += (s, e) => StopLlamaServer();
Console.CancelKeyPress += (s, e) => 
{
    e.Cancel = true;
    StopLlamaServer();
    Environment.Exit(0);
};

try
{
    using var http = new HttpClient { 
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromMinutes(10) // Increased timeout for model loading
    };

    // Quick health check to see if server is already running
    bool serverRunning = false;
    try
    {
        using var ping = await http.GetAsync("v1/models");
        ping.EnsureSuccessStatusCode();
        serverRunning = true;
        Console.Error.WriteLine("Llama server is already running.");
    }
    catch
    {
        Console.Error.WriteLine("Starting llama server...");
        
        // Start the llama server - find the project root directory
        var currentDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectRoot = currentDir;
        
        // Navigate up to find the directory containing the models folder
        while (projectRoot != null && !Directory.Exists(Path.Combine(projectRoot, "models")))
        {
            projectRoot = Directory.GetParent(projectRoot)?.FullName;
        }
        
        if (projectRoot == null)
        {
            Console.Error.WriteLine("Could not find models directory. Make sure you're running from the project directory.");
            Environment.Exit(1);
        }
        
        var serverPath = Path.Combine(projectRoot, "models", "llama", "llama-server.exe");
        var modelPath = Path.Combine(projectRoot, "models", "phi-4-q2_k.gguf");
        
        if (!File.Exists(serverPath))
        {
            Console.Error.WriteLine($"Llama server not found at: {serverPath}");
            Environment.Exit(1);
        }
        
        if (!File.Exists(modelPath))
        {
            Console.Error.WriteLine($"Model file not found at: {modelPath}");
            Environment.Exit(1);
        }
        
        var logFilePath = Path.Combine(Environment.CurrentDirectory, "llama-server.log");
        Console.WriteLine($"Server output will be logged to: {logFilePath}");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = $"-m \"{modelPath}\" -c 4096 --port 8080 --cache-type-k f16 --cache-type-v f16 --repeat-penalty 1.1 --frequency-penalty 0.5 --dry-multiplier 0.8 -ngl 99 --slot-save-path cache --verbose",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        
        llamaProcess = Process.Start(startInfo);
        
        if (llamaProcess == null)
        {
            Console.Error.WriteLine("Failed to start llama server.");
            Environment.Exit(1);
        }
        
        // Create log file and start logging output
        logWriter = new StreamWriter(logFilePath, append: false);
        
        // Log stdout in background
        Task.Run(async () =>
        {
            try
            {
                while (!llamaProcess.StandardOutput.EndOfStream)
                {
                    var line = await llamaProcess.StandardOutput.ReadLineAsync();
                    if (line != null)
                    {
                        await logWriter.WriteLineAsync($"[STDOUT] {DateTime.Now:HH:mm:ss.fff} {line}");
                        await logWriter.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging stdout: {ex.Message}");
            }
        });
        
        // Log stderr in background
        Task.Run(async () =>
        {
            try
            {
                while (!llamaProcess.StandardError.EndOfStream)
                {
                    var line = await llamaProcess.StandardError.ReadLineAsync();
                    if (line != null)
                    {
                        await logWriter.WriteLineAsync($"[STDERR] {DateTime.Now:HH:mm:ss.fff} {line}");
                        await logWriter.FlushAsync();
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
        
        // Wait for the server to start (with timeout)
        var timeout = TimeSpan.FromMinutes(8); // Generous timeout for model loading (slightly less than HttpClient timeout)
        var startTime = DateTime.Now;
        var retryCount = 0;
        
        Console.Error.WriteLine("Waiting for llama server to load model...");
        Console.Error.WriteLine("Note: Phi-4 is a large model and may take 2-5 minutes to load depending on your hardware.");
        
        while (DateTime.Now - startTime < timeout)
        {
            try
            {
                // Test if the model is actually ready by making a minimal chat request
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
                
                var testResponse = await http.PostAsJsonAsync("v1/chat/completions", testRequest);
                
                if (testResponse.IsSuccessStatusCode)
                {
                    serverRunning = true;
                    Console.Error.WriteLine("Llama server and model loaded successfully.");
                    
                    // Try to pre-cache the system prompt, but don't fail if it doesn't work
                    try
                    {
                        await PreCacheSystemPrompt(http);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Warning: Pre-caching failed, continuing anyway: {ex.Message}");
                    }
                    break;
                }
                else if (testResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    retryCount++;
                    var elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                    if (retryCount % 5 == 0) // Log every 5 retries
                    {
                        Console.Error.WriteLine($"Model still loading... ({elapsedSeconds:F0}s elapsed, attempt {retryCount})");
                    }
                    await Task.Delay(3000); // Wait 3 seconds before retrying for 503 errors
                }
                else
                {
                    // Some other error, try again after shorter delay
                    await Task.Delay(1000);
                }
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("503") || ex.Message.Contains("Service Unavailable"))
            {
                retryCount++;
                var elapsedSeconds = (DateTime.Now - startTime).TotalSeconds;
                if (retryCount % 5 == 0) // Log every 5 retries
                {
                    Console.Error.WriteLine($"Model still loading... ({elapsedSeconds:F0}s elapsed, attempt {retryCount})");
                }
                await Task.Delay(3000); // Wait 3 seconds before retrying for 503 errors
            }
            catch (Exception ex)
            {
                // For other exceptions (like network issues), wait and retry
                Console.Error.WriteLine($"Server connection failed: {ex.Message}");
                await Task.Delay(2000);
            }
        }
        
        if (!serverRunning)
        {
            Console.Error.WriteLine("Timeout waiting for llama server to start.");
            StopLlamaServer();
            Environment.Exit(1);
        }
    }

    // Initialize conversation history with system message (pre-caching will help with performance)
    var conversationHistory = new List<object>
    {
        new { role = "system", content = SYSTEM_PROMPT }
    };
    
    // Track timing for analysis
    var requestTimes = new List<TimeSpan>();
    var timeToFirstTokens = new List<double>();
    var streamingRates = new List<double>();
    
    // Create requests directory for logging
    var requestsDir = Path.Combine(Directory.GetCurrentDirectory(), "requests");
    Directory.CreateDirectory(requestsDir);
    
    // Process requests - either duplicate mode or regular sequence
    var requestsToMake = new List<(string prompt, int requestNumber, int totalRequests)>();
    
    if (duplicateMode)
    {
        var prompt = args[1]; // The actual prompt is the second argument
        requestsToMake.Add((prompt, 1, 2));
        requestsToMake.Add((prompt, 2, 2));
    }
    else
    {
        for (int i = 0; i < args.Length; i++)
        {
            requestsToMake.Add((args[i], i + 1, args.Length));
        }
    }

    for (int requestIndex = 0; requestIndex < requestsToMake.Count; requestIndex++)
    {
        var (prompt, requestNumber, totalRequests) = requestsToMake[requestIndex];
        
        if (totalRequests > 1)
        {
            if (duplicateMode)
            {
                Console.WriteLine($"\n=== Duplicate Request {requestNumber}/2: {prompt} ===\n");
            }
            else
            {
                Console.WriteLine($"\n=== Request {requestNumber}/{totalRequests}: {prompt} ===\n");
            }
        }
        
        // Start timing this request
        var requestStartTime = DateTime.Now;
        
        // In duplicate mode, don't modify conversation history for the second request
        List<object> messagesToSend;
        if (duplicateMode && requestNumber == 2)
        {
            // For duplicate request, use the same conversation state as the first request
            messagesToSend = new List<object>
            {
                new { role = "system", content = SYSTEM_PROMPT },
                new { role = "user", content = prompt }
            };
        }
        else
        {
            // Add the current user message to conversation history (normal mode or first duplicate)
            conversationHistory.Add(new { role = "user", content = prompt });
            messagesToSend = conversationHistory.ToList();
        }
        
        // Debug: Show conversation history
        if (totalRequests > 1)
        {
            Console.Error.WriteLine($"[DEBUG] Sending {messagesToSend.Count} messages to API");
        }
        
        // Create chat request with session-based caching for better TTFT
        var req = new
        {
            model = "local",
            messages = messagesToSend.ToArray(),
            max_tokens = 2048,  // Increased for longer responses
            stream = true,      // Enable streaming
            cache_prompt = true, // KEY: Enable prompt caching for faster subsequent requests
            slot_id = 0         // Use consistent slot for session persistence
        };

        // Save request content to file for analysis
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        var requestFileName = Path.Combine(requestsDir, $"request_{requestNumber:D2}_{timestamp}.json");
        var requestJson = JsonSerializer.Serialize(req, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
        await File.WriteAllTextAsync(requestFileName, requestJson);
        Console.Error.WriteLine($"[DEBUG] Request saved to: {requestFileName}");

        HttpResponseMessage? resp = null;
        var maxRetries = 3;
        var retryDelay = 2000;
        
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                resp = await http.PostAsJsonAsync("v1/chat/completions", req);
                resp.EnsureSuccessStatusCode();
                break;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("503") && i < maxRetries - 1)
            {
                Console.Error.WriteLine($"Model still loading, retrying in {retryDelay/1000} seconds... (attempt {i + 1}/{maxRetries})");
                await Task.Delay(retryDelay);
                retryDelay *= 2; // Exponential backoff
            }
        }
        
        if (resp == null)
        {
            throw new InvalidOperationException("Failed to get response from server after retries");
        }

        // Handle streaming response with detailed timing analysis
        var fullText = new StringBuilder();
        var stream = await resp.Content.ReadAsStreamAsync();
        var reader = new StreamReader(stream);
        
        // Streaming timing variables
        DateTime? firstTokenTime = null;
        var tokenTimes = new List<DateTime>();
        var tokenCount = 0;
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            // Server-sent events format: "data: {json}" 
            if (line.StartsWith("data: "))
            {
                var jsonData = line.Substring(6); // Remove "data: " prefix
                
                // Debug: Log SSE events
                Console.Error.WriteLine($"[SSE] Received: {jsonData.Substring(0, Math.Min(50, jsonData.Length))}...");
                
                // Check for end of stream
                if (jsonData == "[DONE]")
                {
                    Console.Error.WriteLine($"[SSE] Stream ended");
                    break;
                }
                    
                try
                {
                    using var doc = JsonDocument.Parse(jsonData);
                    var choices = doc.RootElement.GetProperty("choices");
                    if (choices.GetArrayLength() > 0)
                    {
                        var choice = choices[0];
                        if (choice.TryGetProperty("delta", out var delta))
                        {
                            if (delta.TryGetProperty("content", out var content))
                            {
                                var chunk = content.GetString();
                                if (!string.IsNullOrEmpty(chunk))
                                {
                                    // Record timing for first token
                                    if (firstTokenTime == null)
                                    {
                                        firstTokenTime = DateTime.Now;
                                        Console.Error.WriteLine($"[STREAM] First token received at {(firstTokenTime.Value - requestStartTime).TotalMilliseconds:F0}ms");
                                    }
                                    
                                    // Record timing for each token/chunk
                                    var tokenTime = DateTime.Now;
                                    tokenTimes.Add(tokenTime);
                                    tokenCount++;
                                    
                                    // Debug: Show chunk details
                                    Console.Error.WriteLine($"[STREAM] Token {tokenCount}: '{chunk}' (length: {chunk.Length}) at {(tokenTime - requestStartTime).TotalMilliseconds:F0}ms");
                                    
                                    Console.Write(chunk); // Stream to console in real-time
                                    fullText.Append(chunk);
                                }
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
        
        var text = fullText.ToString();

        // Calculate and display timing information
        var requestDuration = DateTime.Now - requestStartTime;
        
        // Text was already streamed to console, just add a newline
        Console.WriteLine();
        
        // Calculate streaming metrics
        var timeToFirstToken = firstTokenTime.HasValue ? 
            (firstTokenTime.Value - requestStartTime).TotalMilliseconds : 0;
        
        var streamingRate = 0.0;
        var averageTokenInterval = 0.0;
        
        if (tokenTimes.Count > 1)
        {
            var streamingDuration = (tokenTimes.Last() - tokenTimes.First()).TotalSeconds;
            streamingRate = streamingDuration > 0 ? (tokenCount - 1) / streamingDuration : 0;
            
            // Calculate average interval between tokens
            var intervals = new List<double>();
            for (int i = 1; i < tokenTimes.Count; i++)
            {
                intervals.Add((tokenTimes[i] - tokenTimes[i-1]).TotalMilliseconds);
            }
            averageTokenInterval = intervals.Count > 0 ? intervals.Average() : 0;
        }
        
        // Show timing information (always show for multiple prompts, only show for single prompt if it takes more than 1 second)
        if (totalRequests > 1 || requestDuration.TotalSeconds > 1.0)
        {
            Console.Error.WriteLine($"[TIMING] Request {requestNumber} completed in {requestDuration.TotalMilliseconds:F0}ms ({requestDuration.TotalSeconds:F1}s)");
            Console.Error.WriteLine($"  • Time to first token: {timeToFirstToken:F0}ms");
            Console.Error.WriteLine($"  • Tokens received: {tokenCount}");
            if (tokenCount > 1)
            {
                Console.Error.WriteLine($"  • Streaming rate: {streamingRate:F1} tokens/sec");
                Console.Error.WriteLine($"  • Avg token interval: {averageTokenInterval:F0}ms");
            }
        }
        
        // Store timing for summary
        requestTimes.Add(requestDuration);
        timeToFirstTokens.Add(timeToFirstToken);
        streamingRates.Add(streamingRate);
        
        // Add the assistant's response to conversation history (except for duplicate mode second request)
        if (!(duplicateMode && requestNumber == 2))
        {
            conversationHistory.Add(new { role = "assistant", content = text });
        }
        
        // Add separation between responses if there are multiple requests
        if (requestIndex < requestsToMake.Count - 1)
        {
            Console.WriteLine("\n" + new string('=', 50) + "\n");
        }
    }
    
    // Show timing summary for multiple requests
    if (args.Length > 1)
    {
        Console.Error.WriteLine("\n=== TIMING SUMMARY ===");
        for (int i = 0; i < requestTimes.Count; i++)
        {
            var time = requestTimes[i];
            var messages = (i + 1) * 2; // Each request adds user + assistant messages (plus initial system)
            Console.Error.WriteLine($"Request {i + 1}: {time.TotalMilliseconds:F0}ms ({messages + 1} messages in history)");
            Console.Error.WriteLine($"  • TTFT: {timeToFirstTokens[i]:F0}ms, Rate: {streamingRates[i]:F1} tok/sec");
        }
        
        if (requestTimes.Count > 1)
        {
            var firstTime = requestTimes[0].TotalMilliseconds;
            var lastTime = requestTimes[requestTimes.Count - 1].TotalMilliseconds;
            var speedup = firstTime / lastTime;
            
            var avgTTFT = timeToFirstTokens.Average();
            var avgStreamingRate = streamingRates.Where(r => r > 0).Average();
            
            Console.Error.WriteLine($"\nCache Performance Analysis:");
            Console.Error.WriteLine($"First request: {firstTime:F0}ms");
            Console.Error.WriteLine($"Last request: {lastTime:F0}ms");
            if (lastTime < firstTime)
            {
                Console.Error.WriteLine($"Speedup: {speedup:F2}x faster (cache working!)");
            }
            else if (lastTime > firstTime)
            {
                Console.Error.WriteLine($"Slowdown: {lastTime/firstTime:F2}x slower (expected due to longer context)");
            }
            else
            {
                Console.Error.WriteLine($"Similar performance (cache likely working)");
            }
            
            Console.Error.WriteLine($"\nStreaming Performance Analysis:");
            Console.Error.WriteLine($"Average time to first token: {avgTTFT:F0}ms");
            Console.Error.WriteLine($"Average streaming rate: {avgStreamingRate:F1} tokens/sec");
        }
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine("Error: " + ex.Message);
    Environment.Exit(1);
}
finally
{
    // Keep server running for better performance across requests
    // Only stop server on Ctrl+C or process exit (handled by event handlers above)
}