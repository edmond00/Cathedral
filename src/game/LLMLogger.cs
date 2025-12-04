using System;
using System.IO;
using System.Text;

namespace Cathedral.Game;

/// <summary>
/// Logger for LLM communications (requests and responses).
/// Creates a detailed log file for debugging and analysis.
/// </summary>
public static class LLMLogger
{
    private static readonly object _lockObject = new object();
    private static string? _logFilePath = null;
    private static bool _isEnabled = true;
    
    /// <summary>
    /// Initializes the logger with a timestamped log file.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logsDir = Path.Combine(Environment.CurrentDirectory, "logs");
            
            // Create logs directory if it doesn't exist
            if (!Directory.Exists(logsDir))
            {
                Directory.CreateDirectory(logsDir);
            }
            
            _logFilePath = Path.Combine(logsDir, $"llm_communication_{timestamp}.log");
            
            // Write header
            lock (_lockObject)
            {
                File.WriteAllText(_logFilePath, $"=== LLM Communication Log ===\n");
                File.AppendAllText(_logFilePath, $"Session started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
                File.AppendAllText(_logFilePath, $"OS: {Environment.OSVersion}\n");
                File.AppendAllText(_logFilePath, $"Platform: {Environment.Is64BitOperatingSystem} bit\n");
                File.AppendAllText(_logFilePath, $"{'=',-80}\n\n");
            }
            
            Console.WriteLine($"LLMLogger: Logging to {_logFilePath}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to initialize: {ex.Message}");
            _isEnabled = false;
        }
    }
    
    /// <summary>
    /// Logs server initialization start.
    /// </summary>
    public static void LogServerInitStart(string modelAlias, string serverPath, string modelPath)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\n[{DateTime.Now:HH:mm:ss.fff}] SERVER INITIALIZATION START");
            sb.AppendLine($"{'-',-80}");
            sb.AppendLine($"Model Alias: {modelAlias}");
            sb.AppendLine($"Server Path: {serverPath}");
            sb.AppendLine($"Model Path: {modelPath}");
            sb.AppendLine($"Server Exists: {File.Exists(serverPath)}");
            sb.AppendLine($"Model Exists: {File.Exists(modelPath)}");
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log server init: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs server initialization result.
    /// </summary>
    public static void LogServerInitResult(bool success, string message, double durationSeconds)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            var status = success ? "✓ SUCCESS" : "✗ FAILED";
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] SERVER INITIALIZATION - {status}");
            sb.AppendLine($"{'-',-80}");
            sb.AppendLine($"Duration: {durationSeconds:F1}s");
            sb.AppendLine($"Message: {message}");
            sb.AppendLine($"Server Ready: {success}");
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log server result: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs slot/instance creation.
    /// </summary>
    public static void LogInstanceCreated(int slotId, string role, bool success, string? errorMessage = null, string? systemPrompt = null)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            var status = success ? "✓" : "✗";
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] {status} Instance Created: Slot {slotId} ({role})");
            if (!success && errorMessage != null)
            {
                sb.AppendLine($"  Error: {errorMessage}");
            }
            if (success && !string.IsNullOrEmpty(systemPrompt))
            {
                sb.AppendLine($"{'-',-80}");
                sb.AppendLine($"SYSTEM PROMPT:");
                sb.AppendLine(WrapText(systemPrompt, 76));
                sb.AppendLine($"{'=',-80}");
            }
            sb.AppendLine();
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log instance creation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs slot busy/empty response issues.
    /// </summary>
    public static void LogSlotIssue(int slotId, string issueType, string details)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] ⚠ SLOT ISSUE - Slot {slotId}");
            sb.AppendLine($"{'-',-80}");
            sb.AppendLine($"Issue Type: {issueType}");
            sb.AppendLine($"Details: {details}");
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log slot issue: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a request to the LLM.
    /// </summary>
    public static void LogRequest(string role, int slotId, string userPrompt, string? gbnf = null)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] REQUEST → {role} (Slot {slotId})");
            sb.AppendLine($"{'-',-80}");
            sb.AppendLine($"USER PROMPT:");
            sb.AppendLine(WrapText(userPrompt, 76));
            
            if (!string.IsNullOrEmpty(gbnf))
            {
                sb.AppendLine();
                sb.AppendLine($"GBNF GRAMMAR:");
                sb.AppendLine(WrapText(gbnf, 76));
            }
            
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log request: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a response from the LLM.
    /// </summary>
    public static void LogResponse(string role, int slotId, string response, bool success, double durationMs)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            var status = success ? "✓ SUCCESS" : "✗ FAILED";
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] RESPONSE ← {role} (Slot {slotId}) - {status}");
            sb.AppendLine($"Duration: {durationMs:F0}ms");
            sb.AppendLine($"{'-',-80}");
            
            if (success)
            {
                sb.AppendLine($"RESPONSE:");
                sb.AppendLine(WrapText(response, 76));
                sb.AppendLine();
                sb.AppendLine($"Length: {response.Length} characters");
                sb.AppendLine($"Words: {CountWords(response)}");
            }
            else
            {
                sb.AppendLine($"ERROR: {response}");
            }
            
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log response: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a fallback event (when LLM fails and SimpleActionExecutor is used).
    /// </summary>
    public static void LogFallback(string reason)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] FALLBACK → Using SimpleActionExecutor");
            sb.AppendLine($"{'-',-80}");
            sb.AppendLine($"REASON: {reason}");
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log fallback: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a parsing error (when JSON response is invalid).
    /// </summary>
    public static void LogParseError(string role, string response, string error)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] PARSE ERROR → {role}");
            sb.AppendLine($"{'-',-80}");
            sb.AppendLine($"ERROR: {error}");
            sb.AppendLine();
            sb.AppendLine($"RAW RESPONSE:");
            sb.AppendLine(WrapText(response, 76));
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log parse error: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs a Critic evaluation with probability scores.
    /// </summary>
    public static void LogCriticEvaluation(int slotId, string question, double ratio, double pYes, double pNo, double durationMs)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:HH:mm:ss.fff}] CRITIC EVALUATION (Slot {slotId})");
            sb.AppendLine($"{'-',-80}");
            sb.AppendLine($"QUESTION:");
            sb.AppendLine(WrapText(question, 76));
            sb.AppendLine();
            sb.AppendLine($"PROBABILITIES:");
            sb.AppendLine($"  Yes: {pYes:F4} ({pYes * 100:F1}%)");
            sb.AppendLine($"  No:  {pNo:F4} ({pNo * 100:F1}%)");
            sb.AppendLine();
            sb.AppendLine($"RATIO: {ratio:F4} ({ratio * 100:F1}% confidence)");
            sb.AppendLine($"Duration: {durationMs:F0}ms");
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log critic evaluation: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Logs session statistics.
    /// </summary>
    public static void LogStatistics(int totalRequests, int successCount, int failureCount, double avgDurationMs)
    {
        if (!_isEnabled || _logFilePath == null) return;
        
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\n{'=',-80}");
            sb.AppendLine($"SESSION STATISTICS");
            sb.AppendLine($"{'=',-80}");
            sb.AppendLine($"Total Requests: {totalRequests}");
            sb.AppendLine($"Successful: {successCount} ({(totalRequests > 0 ? (successCount * 100.0 / totalRequests) : 0):F1}%)");
            sb.AppendLine($"Failed: {failureCount} ({(totalRequests > 0 ? (failureCount * 100.0 / totalRequests) : 0):F1}%)");
            sb.AppendLine($"Average Duration: {avgDurationMs:F0}ms");
            sb.AppendLine($"Session ended: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"{'=',-80}\n");
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LLMLogger: Failed to log statistics: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Wraps text to a specific width for better readability.
    /// </summary>
    private static string WrapText(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) return "";
        
        var lines = text.Split('\n');
        var sb = new StringBuilder();
        
        foreach (var line in lines)
        {
            if (line.Length <= maxWidth)
            {
                sb.AppendLine("  " + line);
            }
            else
            {
                // Word wrap
                var words = line.Split(' ');
                var currentLine = "  ";
                
                foreach (var word in words)
                {
                    if (currentLine.Length + word.Length + 1 > maxWidth)
                    {
                        sb.AppendLine(currentLine);
                        currentLine = "  " + word;
                    }
                    else
                    {
                        if (currentLine.Length > 2)
                            currentLine += " ";
                        currentLine += word;
                    }
                }
                
                if (currentLine.Length > 2)
                    sb.AppendLine(currentLine);
            }
        }
        
        return sb.ToString().TrimEnd();
    }
    
    /// <summary>
    /// Counts words in a string.
    /// </summary>
    private static int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
