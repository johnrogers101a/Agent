#pragma warning disable MEAI001 // Experimental API

using Microsoft.Extensions.AI;

namespace AgentFramework.Core;

/// <summary>
/// Manages conversation history with rolling summarization using SummarizingChatReducer.
/// Summarizes every 5 user messages to maintain context while controlling token usage.
/// </summary>
public class ConversationMemory
{
    private const int SummarizeEveryNUserMessages = 5;
    private const int DefaultContextLength = 4096;

    private readonly List<ChatMessage> _history = [];
    private readonly IChatClient _chatClient;
    private readonly SummarizingChatReducer _reducer;
    private readonly int _contextLength;
    private int _userMessageCount;

    /// <summary>
    /// Initializes a new instance of ConversationMemory.
    /// </summary>
    /// <param name="chatClient">The chat client used for summarization calls.</param>
    /// <param name="contextLength">The model's context length in tokens. Defaults to 4096 if not specified.</param>
    public ConversationMemory(IChatClient chatClient, int? contextLength = null)
    {
        _chatClient = chatClient;
        _contextLength = contextLength ?? DefaultContextLength;
        
        // Target count is ~75% of context to leave room for new messages and response
        var targetCount = (int)(_contextLength * 0.75);
        
        // Threshold count is ~90% of context - triggers reduction when exceeded
        var thresholdCount = (int)(_contextLength * 0.90);
        
        _reducer = new SummarizingChatReducer(_chatClient, targetCount, thresholdCount);
    }

    /// <summary>
    /// Gets the current conversation history.
    /// </summary>
    public IReadOnlyList<ChatMessage> History => _history.AsReadOnly();

    /// <summary>
    /// Gets the context length being used.
    /// </summary>
    public int ContextLength => _contextLength;

    /// <summary>
    /// Gets the number of user messages since last summarization.
    /// </summary>
    public int UserMessageCount => _userMessageCount;

    /// <summary>
    /// Prepends stored history to the incoming messages for context.
    /// </summary>
    /// <param name="messages">The incoming messages from the current request.</param>
    /// <returns>Combined list with history prepended.</returns>
    public IList<ChatMessage> InjectHistory(IList<ChatMessage> messages)
    {
        if (_history.Count == 0)
        {
            return messages;
        }

        var combined = new List<ChatMessage>(_history.Count + messages.Count);
        combined.AddRange(_history);
        combined.AddRange(messages);
        return combined;
    }

    /// <summary>
    /// Stores messages from the request and response, triggering summarization every 5 user messages.
    /// </summary>
    /// <param name="requestMessages">The messages sent in the request.</param>
    /// <param name="response">The response from the model.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task StoreMessagesAsync(
        IList<ChatMessage> requestMessages, 
        ChatResponse response, 
        CancellationToken cancellationToken = default)
    {
        // Add request messages that aren't already in history
        foreach (var msg in requestMessages)
        {
            // Skip if this message is already in history (was injected)
            if (!_history.Contains(msg))
            {
                _history.Add(msg);
                
                // Count user messages for summarization trigger
                if (msg.Role == ChatRole.User)
                {
                    _userMessageCount++;
                }
            }
        }

        // Add response message(s)
        foreach (var msg in response.Messages)
        {
            _history.Add(msg);
        }

        // Trigger summarization every N user messages
        if (_userMessageCount >= SummarizeEveryNUserMessages)
        {
            await ReduceHistoryAsync(cancellationToken);
            _userMessageCount = 0;
        }
    }

    /// <summary>
    /// Reduces the conversation history using the SummarizingChatReducer.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ReduceHistoryAsync(CancellationToken cancellationToken = default)
    {
        if (_history.Count == 0) return;

        var reduced = await _reducer.ReduceAsync(_history, cancellationToken);
        
        if (reduced is not null)
        {
            _history.Clear();
            _history.AddRange(reduced);
        }
    }

    /// <summary>
    /// Resets the conversation, optionally preserving a summary.
    /// </summary>
    /// <param name="preserveSummary">If true, reduces history to summary before clearing.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task ResetConversationAsync(bool preserveSummary = true, CancellationToken cancellationToken = default)
    {
        if (preserveSummary && _history.Count > 0)
        {
            // Reduce to summary first
            await ReduceHistoryAsync(cancellationToken);
        }
        else
        {
            _history.Clear();
        }
        
        _userMessageCount = 0;
    }

    /// <summary>
    /// Clears all conversation history immediately without summarization.
    /// </summary>
    public void ClearHistory()
    {
        _history.Clear();
        _userMessageCount = 0;
    }
}

#pragma warning restore MEAI001
