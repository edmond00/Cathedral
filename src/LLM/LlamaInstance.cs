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
}