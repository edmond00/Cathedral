using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;

// Usage: LlamaCli.exe "prompt1" "prompt2" "prompt3" ...
if (args.Length == 0)
{
    Console.Error.WriteLine("Usage: LlamaCli <prompt1> [prompt2] [prompt3] ...");
    Environment.Exit(2);
}

// Change if your llama-server runs elsewhere/other port
var baseUrl = Environment.GetEnvironmentVariable("LLAMA_SERVER") ?? "http://127.0.0.1:8080/";

Process? llamaProcess = null;

// Cleanup function to stop the server
void StopLlamaServer()
{
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
    using var http = new HttpClient { BaseAddress = new Uri(baseUrl) };

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
        
        var serverPath = Path.Combine(projectRoot, "models", "llama-b6686-bin-win-cuda-12.4-x64", "llama-server.exe");
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
        
        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = $"-m \"{modelPath}\" -c 4096 --port 8080",
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
        
        // Give the process a moment to start
        await Task.Delay(2000);
        
        // Wait for the server to start (with timeout)
        var timeout = TimeSpan.FromSeconds(60); // Increased timeout for model loading
        var startTime = DateTime.Now;
        var retryCount = 0;
        
        Console.Error.WriteLine("Waiting for llama server to load model...");
        
        while (DateTime.Now - startTime < timeout)
        {
            try
            {
                using var ping = await http.GetAsync("v1/models");
                ping.EnsureSuccessStatusCode();
                serverRunning = true;
                Console.Error.WriteLine("Llama server started successfully.");
                // Give the model a bit more time to fully load
                await Task.Delay(3000);
                break;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("503") || ex.Message.Contains("Service Unavailable"))
            {
                retryCount++;
                if (retryCount % 5 == 0) // Log every 5 retries
                {
                    Console.Error.WriteLine($"Server still loading model... (attempt {retryCount})");
                }
                await Task.Delay(2000); // Wait 2 seconds before retrying for 503 errors
            }
            catch
            {
                await Task.Delay(1000); // Wait 1 second before retrying for other errors
            }
        }
        
        if (!serverRunning)
        {
            Console.Error.WriteLine("Timeout waiting for llama server to start.");
            StopLlamaServer();
            Environment.Exit(1);
        }
    }

    // Initialize conversation history with system message
    var conversationHistory = new List<object>
    {
        new { role = "system", content = "You are a helpful assistant." }
    };
    
    // Track timing for analysis
    var requestTimes = new List<TimeSpan>();
    
    // Process each argument as a separate prompt, maintaining conversation history
    for (int argIndex = 0; argIndex < args.Length; argIndex++)
    {
        var prompt = args[argIndex];
        
        if (args.Length > 1)
        {
            Console.WriteLine($"\n=== Request {argIndex + 1}/{args.Length}: {prompt} ===\n");
        }
        
        // Start timing this request
        var requestStartTime = DateTime.Now;
        
        // Add the current user message to conversation history
        conversationHistory.Add(new { role = "user", content = prompt });
        
        // Debug: Show conversation history (only for multiple prompts)
        if (args.Length > 1)
        {
            Console.Error.WriteLine($"[DEBUG] Sending {conversationHistory.Count} messages to API");
        }
        
        // Create chat request with full conversation history
        var req = new
        {
            model = "local",
            messages = conversationHistory.ToArray(),
            max_tokens = 2048  // Increased for longer responses
        };

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

        using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
        var text = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        // Calculate and display timing information
        var requestDuration = DateTime.Now - requestStartTime;
        
        Console.WriteLine(text);
        
        // Show timing information (always show for multiple prompts, only show for single prompt if it takes more than 1 second)
        if (args.Length > 1 || requestDuration.TotalSeconds > 1.0)
        {
            Console.Error.WriteLine($"[TIMING] Request {argIndex + 1} completed in {requestDuration.TotalMilliseconds:F0}ms ({requestDuration.TotalSeconds:F1}s)");
        }
        
        // Store timing for summary
        requestTimes.Add(requestDuration);
        
        // Add the assistant's response to conversation history for next iterations
        conversationHistory.Add(new { role = "assistant", content = text });
        
        // Add separation between responses if there are multiple prompts
        if (argIndex < args.Length - 1)
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
        }
        
        if (requestTimes.Count > 1)
        {
            var firstTime = requestTimes[0].TotalMilliseconds;
            var lastTime = requestTimes[requestTimes.Count - 1].TotalMilliseconds;
            var speedup = firstTime / lastTime;
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
    // Ensure server is stopped when we exit
    StopLlamaServer();
}