using System.Diagnostics;

namespace Cathedral.LLM;

/// <summary>
/// Event arguments for when new tokens are streamed from the LLM
/// </summary>
public class TokenStreamedEventArgs : EventArgs
{
    public string Token { get; }
    public int SlotId { get; }
    public DateTime Timestamp { get; }
    
    public TokenStreamedEventArgs(string token, int slotId)
    {
        Token = token;
        SlotId = slotId;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// Event arguments for when a request is completed
/// </summary>
public class RequestCompletedEventArgs : EventArgs
{
    public int SlotId { get; }
    public string FullResponse { get; }
    public TimeSpan Duration { get; }
    public DateTime Timestamp { get; }
    public bool WasCancelled { get; }
    
    public RequestCompletedEventArgs(int slotId, string fullResponse, TimeSpan duration, bool wasCancelled = false)
    {
        SlotId = slotId;
        FullResponse = fullResponse;
        Duration = duration;
        Timestamp = DateTime.Now;
        WasCancelled = wasCancelled;
    }
}

/// <summary>
/// Event arguments for server status changes
/// </summary>
public class ServerStatusEventArgs : EventArgs
{
    public bool IsReady { get; }
    public string Message { get; }
    public DateTime Timestamp { get; }
    
    public ServerStatusEventArgs(bool isReady, string message)
    {
        IsReady = isReady;
        Message = message;
        Timestamp = DateTime.Now;
    }
}

/// <summary>
/// Represents the state of an LLM conversation instance
/// </summary>
public class LlamaInstance
{
    public int SlotId { get; }
    public string SystemPrompt { get; }
    public List<object> ConversationHistory { get; private set; }
    public bool IsActive { get; internal set; }
    public DateTime CreatedAt { get; }
    public DateTime LastUsed { get; internal set; }
    public int MaxContextTokens { get; set; } = 4096; // Default context size
    public int RequestCount { get; internal set; } = 0; // Tracks number of requests made
    
    internal CancellationTokenSource? CurrentRequestCancellation { get; set; }
    
    public LlamaInstance(int slotId, string systemPrompt)
    {
        SlotId = slotId;
        SystemPrompt = systemPrompt;
        ConversationHistory = new List<object>
        {
            new { role = "system", content = systemPrompt }
        };
        IsActive = false;
        CreatedAt = DateTime.Now;
        LastUsed = DateTime.Now;
    }
    
    /// <summary>
    /// Adds a user message to the conversation history
    /// </summary>
    public void AddUserMessage(string message)
    {
        ConversationHistory.Add(new { role = "user", content = message });
        LastUsed = DateTime.Now;
    }
    
    /// <summary>
    /// Adds an assistant response to the conversation history
    /// </summary>
    public void AddAssistantResponse(string response)
    {
        ConversationHistory.Add(new { role = "assistant", content = response });
        LastUsed = DateTime.Now;
    }
    
    /// <summary>
    /// Resets the conversation while keeping the system prompt
    /// </summary>
    public void Reset()
    {
        ConversationHistory.Clear();
        ConversationHistory.Add(new { role = "system", content = SystemPrompt });
        LastUsed = DateTime.Now;
    }
    
    /// <summary>
    /// Gets the current conversation as an array suitable for the API
    /// </summary>
    public object[] GetMessages()
    {
        return ConversationHistory.ToArray();
    }
    
    /// <summary>
    /// Estimates the number of tokens in a text string.
    /// Uses rough approximation: 1 token ≈ 4 characters for English text.
    /// </summary>
    private int EstimateTokenCount(string text)
    {
        if (string.IsNullOrEmpty(text)) return 0;
        // Rough approximation: average 4 characters per token
        return (int)Math.Ceiling(text.Length / 4.0);
    }
    
    /// <summary>
    /// Estimates the total token count of the current conversation history.
    /// </summary>
    public int EstimateConversationTokens()
    {
        int total = 0;
        foreach (dynamic message in ConversationHistory)
        {
            string content = message.content;
            total += EstimateTokenCount(content);
            total += 4; // Add overhead for message formatting
        }
        return total;
    }
    
    /// <summary>
    /// Trims the conversation history to fit within the context window.
    /// Keeps the system prompt and most recent messages.
    /// </summary>
    /// <param name="maxTokens">Maximum tokens to keep (defaults to MaxContextTokens - 512 for response buffer)</param>
    /// <returns>Number of messages removed</returns>
    public int TrimToFitContext(int? maxTokens = null)
    {
        int targetTokens = maxTokens ?? (MaxContextTokens - 512); // Reserve 512 tokens for response
        int currentTokens = EstimateConversationTokens();
        
        if (currentTokens <= targetTokens)
            return 0; // No trimming needed
        
        // Always keep system prompt (first message)
        var systemPrompt = ConversationHistory[0];
        var messages = ConversationHistory.Skip(1).ToList();
        
        int removedCount = 0;
        
        // Remove oldest messages (after system prompt) until we fit
        while (messages.Count > 0 && currentTokens > targetTokens)
        {
            var removed = messages[0];
            messages.RemoveAt(0);
            removedCount++;
            
            // Recalculate token count
            dynamic msg = removed;
            string content = msg.content;
            currentTokens -= EstimateTokenCount(content) + 4;
        }
        
        // Rebuild conversation history
        ConversationHistory.Clear();
        ConversationHistory.Add(systemPrompt);
        ConversationHistory.AddRange(messages);
        
        return removedCount;
    }
    
    /// <summary>
    /// Creates a trimmed copy of the conversation history that fits in the context window.
    /// Does not modify the original history.
    /// </summary>
    /// <param name="maxTokens">Maximum tokens to keep</param>
    /// <returns>Trimmed message array</returns>
    public object[] GetTrimmedMessages(int? maxTokens = null)
    {
        int targetTokens = maxTokens ?? (MaxContextTokens - 512);
        int currentTokens = EstimateConversationTokens();
        
        if (currentTokens <= targetTokens)
            return GetMessages();
        
        // Always keep system prompt
        var result = new List<object> { ConversationHistory[0] };
        var messages = ConversationHistory.Skip(1).ToList();
        
        // Remove oldest messages until we fit
        while (messages.Count > 0 && currentTokens > targetTokens)
        {
            var removed = messages[0];
            messages.RemoveAt(0);
            
            dynamic msg = removed;
            string content = msg.content;
            currentTokens -= EstimateTokenCount(content) + 4;
        }
        
        result.AddRange(messages);
        return result.ToArray();
    }
}