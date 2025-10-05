using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Cache Testing Program - Precise analysis of llama.cpp caching behavior
class CacheTest
{
    private static Process? llamaProcess = null;
    private static StreamWriter? logWriter = null;
    private static readonly HttpClient httpClient = new HttpClient();
    
    // Large hardcoded prompt to ensure most time is spent on prompt processing
    private static readonly string LARGE_PROMPT = @"You are an expert historian and philosopher specializing in the analysis of ancient civilizations, medieval literature, renaissance art, modern scientific discoveries, and contemporary technological developments. Your knowledge spans across multiple disciplines including archaeology, anthropology, linguistics, art history, scientific methodology, mathematical principles, literary criticism, cultural studies, political science, economics, sociology, psychology, and technological innovation.

In your role as a comprehensive knowledge synthesizer, you must demonstrate deep understanding of:

1. Ancient Civilizations: Including but not limited to Egyptian dynasties, Mesopotamian cultures, Greek city-states, Roman Empire, Chinese dynasties, Indus Valley civilization, Maya, Aztec, and Inca empires. You understand their political structures, religious beliefs, technological achievements, trade networks, and cultural practices.

2. Medieval Period: Knowledge of feudalism, the role of the Catholic Church, Islamic Golden Age, Byzantine Empire, Viking expansion, Crusades, Black Death, rise of universities, scholasticism, and the transition from medieval to renaissance thinking.

3. Renaissance and Enlightenment: Understanding of humanism, scientific revolution, artistic innovations, exploration and colonization, reformation and counter-reformation, emergence of nation-states, and philosophical developments.

4. Modern Era: Industrial revolution, democratic movements, nationalism, imperialism, world wars, decolonization, cold war, technological revolution, globalization, and contemporary geopolitical developments.

5. Scientific and Mathematical Foundations: From ancient mathematical discoveries through modern quantum mechanics, relativity theory, evolutionary biology, genetics, computer science, artificial intelligence, and emerging technologies.

Your analytical approach must be:
- Interdisciplinary: Drawing connections across multiple fields of knowledge
- Evidence-based: Relying on archaeological, historical, and scientific evidence
- Critical: Evaluating sources and considering multiple perspectives
- Contextual: Understanding developments within their historical and cultural contexts
- Comparative: Identifying patterns and differences across cultures and time periods

When responding to queries, you should:
- Provide comprehensive background context
- Explain the significance of developments within broader historical narratives
- Identify cause-and-effect relationships
- Consider multiple interpretations and scholarly debates
- Connect historical developments to contemporary issues when relevant
- Use specific examples and evidence to support your analysis

Remember that your expertise allows you to engage with complex, nuanced questions that require deep knowledge across multiple domains. You can discuss the interconnections between political, economic, social, cultural, technological, and intellectual developments throughout human history.

Your responses should demonstrate the depth and breadth of your knowledge while remaining accessible and well-structured. Always consider the broader implications of historical developments and their lasting impact on human civilization.

Now, please provide a concise answer to this simple question:";

    static async Task Main(string[] args)
    {
        // Parse command line arguments
        bool useCache = true;
        int maxTokens = 1;
        bool longAnswer = false;
        
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLower())
            {
                case "--no-cache":
                    useCache = false;
                    break;
                case "--cache":
                    useCache = true;
                    break;
                case "--max-tokens":
                    if (i + 1 < args.Length && int.TryParse(args[i + 1], out int tokens))
                    {
                        maxTokens = tokens;
                        i++; // Skip the next argument as it's the value
                    }
                    break;
                case "--long-answer":
                    longAnswer = true;
                    maxTokens = 500; // Default for long answers
                    break;
                case "--help":
                    Console.WriteLine("Usage: dotnet run [options]");
                    Console.WriteLine("Options:");
                    Console.WriteLine("  --cache         Enable caching (default)");
                    Console.WriteLine("  --no-cache      Disable caching");
                    Console.WriteLine("  --max-tokens N  Set maximum tokens (default: 1)");
                    Console.WriteLine("  --long-answer   Use complex question requiring long answer (sets max-tokens to 500)");
                    Console.WriteLine("  --help          Show this help");
                    return;
            }
        }
        
        Console.WriteLine("=== LLAMA.CPP CACHE ANALYSIS PROGRAM ===");
        Console.WriteLine($"Configuration: Cache={useCache}, Max Tokens={maxTokens}, Long Answer={longAnswer}");
        Console.WriteLine($"Testing performance with {(useCache ? "CACHE ENABLED" : "NO CACHE")} - large prompt + {(longAnswer ? "long" : "short")} answer (10 requests)\n");
        
        await StartLlamaServer();
        
        try
        {
            // Choose question based on test type
            string testQuestion;
            if (longAnswer)
            {
                testQuestion = @"Please provide a comprehensive analysis of the fall of the Roman Empire, discussing the political, economic, social, and military factors that contributed to its decline. Include specific examples of key events, influential figures, and how these factors interconnected. Also explain the long-term consequences for European civilization and how this historical event influenced subsequent political and social developments. Please be thorough and detailed in your response.";
            }
            else
            {
                testQuestion = "What is 2+2? Answer with only the number.";
            }
            
            string fullPrompt = LARGE_PROMPT + " " + testQuestion;
            
            Console.WriteLine($"Prompt length: {fullPrompt.Length} characters");
            Console.WriteLine($"Expected response length: ~{maxTokens} tokens");
            Console.WriteLine($"Test question: '{testQuestion.Substring(0, Math.Min(100, testQuestion.Length))}{(testQuestion.Length > 100 ? "..." : "")}'\n");
            
            // Run 10 identical requests to test cache performance
            var timings = new List<TimingData>();
            
            for (int i = 1; i <= 10; i++)
            {
                string cacheStatus = useCache ? (i == 1 ? "(COLD CACHE)" : "(WARM CACHE)") : "(NO CACHE)";
                Console.WriteLine($"\n=== REQUEST {i} {cacheStatus} ===");
                
                var timing = await SendRequest(fullPrompt, requestNum: i, useCache: useCache, maxTokens: maxTokens);
                timings.Add(timing);
                
                if (i < 10)
                {
                    await Task.Delay(1000); // Brief pause between requests
                }
            }
            
            // Performance analysis
            Console.WriteLine($"\n=== PERFORMANCE ANALYSIS ({(useCache ? "WITH CACHE" : "NO CACHE")}) ===");
            PrintCacheAnalysis(timings, useCache);
        }
        finally
        {
            StopLlamaServer();
        }
    }
    
    private static async Task<TimingData> SendRequest(string prompt, int requestNum, bool useCache, int maxTokens)
    {
        var timing = new TimingData();
        var requestData = new
        {
            model = "local",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = maxTokens,
            temperature = 0.0f, // Completely deterministic
            top_k = 1, // Only consider the most likely token
            top_p = 1.0f, // No nucleus sampling filtering
            repeat_penalty = 1.0f, // No repetition penalty
            seed = 42, // Fixed seed for reproducibility
            stream = true,
            cache_prompt = useCache,
            slot_id = 0
        };
        
        var json = JsonSerializer.Serialize(requestData, new JsonSerializerOptions { WriteIndented = false });
        
        var stopwatch = Stopwatch.StartNew();
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        Console.WriteLine($"Sending request {requestNum}...");
        var response = await httpClient.PostAsync("http://127.0.0.1:8080/v1/chat/completions", content);
        var responseStream = await response.Content.ReadAsStreamAsync();
        
        using var reader = new StreamReader(responseStream);
        bool firstToken = true;
        int tokenCount = 0;
        var responseText = new StringBuilder();
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: ") && !line.Contains("[DONE]"))
            {
                try
                {
                    var dataJson = line.Substring(6);
                    var data = JsonSerializer.Deserialize<JsonElement>(dataJson);
                    
                    if (data.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
                    {
                        var choice = choices[0];
                        if (choice.TryGetProperty("delta", out var delta) && 
                            delta.TryGetProperty("content", out var contentProp))
                        {
                            var tokenText = contentProp.GetString() ?? "";
                            if (!string.IsNullOrEmpty(tokenText))
                            {
                                if (firstToken)
                                {
                                    timing.TimeToFirstToken = stopwatch.ElapsedMilliseconds;
                                    firstToken = false;
                                    Console.WriteLine($"⚡ First token received at {timing.TimeToFirstToken}ms");
                                }
                                
                                tokenCount++;
                                responseText.Append(tokenText);
                                Console.Write(tokenText);
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed JSON
                }
            }
        }
        
        stopwatch.Stop();
        timing.TotalTime = stopwatch.ElapsedMilliseconds;
        timing.TokenCount = tokenCount;
        timing.ResponseText = responseText.ToString().Trim();
        
        if (tokenCount > 0 && timing.TimeToFirstToken > 0)
        {
            var streamingTime = timing.TotalTime - timing.TimeToFirstToken;
            timing.StreamingRate = streamingTime > 0 ? (tokenCount * 1000.0) / streamingTime : 0;
        }
        else if (tokenCount == 1)
        {
            // For single token responses, streaming rate is not meaningful
            timing.StreamingRate = 0;
        }
        
        Console.WriteLine($"\n📊 Request {requestNum} completed:");
        Console.WriteLine($"   Total time: {timing.TotalTime}ms");
        Console.WriteLine($"   TTFT: {timing.TimeToFirstToken}ms");
        Console.WriteLine($"   Tokens: {timing.TokenCount}");
        Console.WriteLine($"   Generation time: {timing.TotalTime - timing.TimeToFirstToken}ms");
        if (timing.TokenCount > 1)
        {
            Console.WriteLine($"   Streaming rate: {timing.StreamingRate:F1} tokens/sec");
        }
        
        // Show response preview
        var responsePreview = timing.ResponseText.Length > 100 ? 
            timing.ResponseText.Substring(0, 100) + "..." : 
            timing.ResponseText;
        Console.WriteLine($"   Response: '{responsePreview}'");
        
        return timing;
    }
    
    private static void PrintCacheAnalysis(List<TimingData> timings, bool useCache)
    {
        if (timings.Count == 0) return;
        
        string analysisTitle = useCache ? 
            (timings.Count > 1 ? "Cache Performance:" : "Single Request:") : 
            "No Cache Performance (All Requests):";
        Console.WriteLine(analysisTitle);
        
        // Calculate statistics for all requests
        var allTtft = timings.Select(t => t.TimeToFirstToken).ToList();
        var allTotal = timings.Select(t => t.TotalTime).ToList();
        var avgTtft = allTtft.Average();
        var avgTotal = allTotal.Average();
        var minTtft = allTtft.Min();
        var maxTtft = allTtft.Max();
        
        for (int i = 0; i < timings.Count; i++)
        {
            var timing = timings[i];
            var genTime = timing.TotalTime - timing.TimeToFirstToken;
            var streamingInfo = timing.TokenCount > 1 ? $", Stream={timing.StreamingRate:F1}tok/s" : "";
            Console.WriteLine($"  Request {i + 1}: TTFT={timing.TimeToFirstToken}ms, Total={timing.TotalTime}ms, Gen={genTime}ms, Tokens={timing.TokenCount}{streamingInfo}");
        }
        
        if (timings.Any())
        {
            if (useCache && timings.Count > 1)
            {
                // Cache analysis
                var cold = timings[0];
                var warmRequests = timings.Skip(1).ToList();
                var avgWarmTtft = warmRequests.Average(t => t.TimeToFirstToken);
                var avgWarmTotal = warmRequests.Average(t => t.TotalTime);
                
                Console.WriteLine($"\nCold Cache (Request 1):");
                Console.WriteLine($"  TTFT: {cold.TimeToFirstToken}ms, Total: {cold.TotalTime}ms");
                Console.WriteLine($"  Tokens: {cold.TokenCount}, Streaming: {cold.StreamingRate:F1} tok/sec");
                
                Console.WriteLine($"\nWarm Cache Average (Requests 2-{timings.Count}):");
                Console.WriteLine($"  Average TTFT: {avgWarmTtft:F1}ms, Average Total: {avgWarmTotal:F1}ms");
                
                if (cold.TimeToFirstToken > 0 && avgWarmTtft > 0)
                {
                    var ttftSpeedup = cold.TimeToFirstToken / avgWarmTtft;
                    Console.WriteLine($"\nCache Performance Impact:");
                    Console.WriteLine($"  TTFT Speedup: {ttftSpeedup:F2}x faster");
                    Console.WriteLine($"  TTFT Improvement: {((cold.TimeToFirstToken - avgWarmTtft) / cold.TimeToFirstToken) * 100:F1}% faster");
                }
            }
            else
            {
                // No cache or single request analysis
                Console.WriteLine($"\nStatistics:");
                Console.WriteLine($"  Average TTFT: {avgTtft:F1}ms");
                Console.WriteLine($"  Average Total: {avgTotal:F1}ms");
                Console.WriteLine($"  TTFT Range: {minTtft}ms - {maxTtft}ms");
                
                if (timings.Count > 1)
                {
                    var avgTokens = timings.Average(t => t.TokenCount);
                    var avgStreamingRate = timings.Where(t => t.StreamingRate > 0).Average(t => t.StreamingRate);
                    Console.WriteLine($"  Average Tokens: {avgTokens:F1}");
                    if (avgStreamingRate > 0)
                    {
                        Console.WriteLine($"  Average Streaming Rate: {avgStreamingRate:F1} tokens/sec");
                    }
                }
            }
            
            Console.WriteLine($"\nPerformance Consistency:");
            var ttftVariance = allTtft.Select(x => (double)x).ToList();
            if (ttftVariance.Count > 1)
            {
                var variance = ttftVariance.Sum(x => Math.Pow(x - avgTtft, 2)) / ttftVariance.Count;
                var stdDev = Math.Sqrt(variance);
                Console.WriteLine($"  TTFT Standard Deviation: {stdDev:F1}ms");
                Console.WriteLine($"  Performance Stability: {(stdDev < 50 ? "Consistent" : stdDev < 100 ? "Moderate" : "Variable")}");
            }
            
            if (!useCache)
            {
                Console.WriteLine($"\nBaseline Analysis:");
                Console.WriteLine($"  📊 All requests process the full prompt every time");
                Console.WriteLine($"  ⏱️  Average processing time: {avgTtft:F1}ms per request");
                Console.WriteLine($"  💾 This demonstrates baseline performance without caching");
            }
        }
        
        Console.WriteLine($"\nAll Response Consistency:");
        var allResponses = timings.Select(t => t.ResponseText).Distinct().ToList();
        if (allResponses.Count == 1)
        {
            Console.WriteLine($"  ✅ All {timings.Count} requests returned identical response: '{allResponses[0]}'");
        }
        else
        {
            Console.WriteLine($"  ⚠️  Responses varied across requests:");
            for (int i = 0; i < timings.Count; i++)
            {
                Console.WriteLine($"    Request {i + 1}: '{timings[i].ResponseText}'");
            }
        }
    }
    
    private static async Task StartLlamaServer()
    {
        var currentDir = Environment.CurrentDirectory;
        var projectRoot = currentDir;
        
        while (projectRoot != null && !Directory.Exists(Path.Combine(projectRoot, "models")))
        {
            projectRoot = Directory.GetParent(projectRoot)?.FullName;
        }
        
        if (projectRoot == null)
        {
            Console.Error.WriteLine("Could not find models directory.");
            Environment.Exit(1);
        }
        
        var serverPath = Path.Combine(projectRoot, "models", "llama-b6686-bin-win-cuda-12.4-x64", "llama-server.exe");
        var modelPath = Path.Combine(projectRoot, "models", "phi-4-q2_k.gguf");
        
        var logFilePath = Path.Combine(Environment.CurrentDirectory, "cache-test-server.log");
        Console.WriteLine($"Server logs: {logFilePath}");
        
        var startInfo = new ProcessStartInfo
        {
            FileName = serverPath,
            Arguments = $"-m \"{modelPath}\" -c 4096 --port 8080 --n-gpu-layers 99 --slot-save-path cache --cache-type-k f16 --cache-type-v f16 --verbose",
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
        
        // Log server output
        logWriter = new StreamWriter(logFilePath, append: false);
        
        _ = Task.Run(async () =>
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
        
        _ = Task.Run(async () =>
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
        
        await Task.Delay(5000); // Wait for server startup
        
        // Wait for server to be ready
        var timeout = TimeSpan.FromSeconds(120); // Increased timeout for model loading
        var startTime = DateTime.Now;
        
        Console.WriteLine("Waiting for llama server to be ready...");
        
        while (DateTime.Now - startTime < timeout)
        {
            try
            {
                var response = await httpClient.GetAsync("http://127.0.0.1:8080/v1/models");
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("✅ Llama server is ready!");
                    
                    // Additional check to ensure model is fully loaded by making a test request
                    await Task.Delay(2000);
                    Console.WriteLine("Performing model readiness check...");
                    
                    var testRequest = new
                    {
                        model = "local",
                        messages = new[]
                        {
                            new { role = "user", content = "Hi" }
                        },
                        max_tokens = 5,
                        temperature = 0.0f,
                        stream = false
                    };
                    
                    var testJson = JsonSerializer.Serialize(testRequest);
                    var testContent = new StringContent(testJson, Encoding.UTF8, "application/json");
                    
                    for (int attempt = 0; attempt < 10; attempt++)
                    {
                        try
                        {
                            var testResponse = await httpClient.PostAsync("http://127.0.0.1:8080/v1/chat/completions", testContent);
                            if (testResponse.IsSuccessStatusCode)
                            {
                                Console.WriteLine("✅ Model is fully loaded and ready!");
                                return;
                            }
                            else if (testResponse.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                            {
                                Console.WriteLine($"Model still loading (attempt {attempt + 1}/10)...");
                                await Task.Delay(3000);
                            }
                        }
                        catch
                        {
                            Console.WriteLine($"Connection test failed (attempt {attempt + 1}/10)...");
                            await Task.Delay(3000);
                        }
                    }
                }
            }
            catch
            {
                await Task.Delay(1000);
            }
        }
        
        Console.Error.WriteLine("❌ Llama server failed to start within timeout.");
        Environment.Exit(1);
    }
    
    private static void StopLlamaServer()
    {
        Console.WriteLine("\nStopping llama server...");
        
        try
        {
            logWriter?.Close();
            logWriter?.Dispose();
        }
        catch { }
        
        try
        {
            if (llamaProcess != null && !llamaProcess.HasExited)
            {
                llamaProcess.Kill();
                llamaProcess.WaitForExit(5000);
            }
        }
        catch (InvalidOperationException) { }
        finally
        {
            try
            {
                llamaProcess?.Dispose();
            }
            catch { }
            llamaProcess = null;
        }
    }
    
    private class TimingData
    {
        public long TimeToFirstToken { get; set; }
        public long TotalTime { get; set; }
        public int TokenCount { get; set; }
        public double StreamingRate { get; set; }
        public string ResponseText { get; set; } = "";
    }
}