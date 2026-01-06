#pragma warning disable MEAI001 // Experimental API

using Microsoft.Extensions.AI;

namespace AgentFramework.Core;

/// <summary>
/// A delegating chat client that integrates conversation memory,
/// injecting history before requests and storing messages after responses.
/// </summary>
public class MemoryDelegatingChatClient : DelegatingChatClient
{
    private readonly ConversationMemory _memory;

    /// <summary>
    /// Initializes a new instance of MemoryDelegatingChatClient.
    /// </summary>
    /// <param name="innerClient">The inner chat client to delegate to.</param>
    /// <param name="memory">The conversation memory to use for history management.</param>
    public MemoryDelegatingChatClient(IChatClient innerClient, ConversationMemory memory)
        : base(innerClient)
    {
        _memory = memory ?? throw new ArgumentNullException(nameof(memory));
    }

    /// <summary>
    /// Gets the conversation memory instance.
    /// </summary>
    public ConversationMemory Memory => _memory;

    /// <inheritdoc/>
    public override async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        
        // Inject history before the request
        var messagesWithHistory = _memory.InjectHistory(messagesList);

        try
        {
            // Call the inner client
            var response = await base.GetResponseAsync(messagesWithHistory, options, cancellationToken);

            // Store messages after successful response
            await _memory.StoreMessagesAsync(messagesList, response, cancellationToken);

            return response;
        }
        catch (Exception ex) when (IsContextOverflowError(ex))
        {
            // On context overflow, reduce history and retry
            await _memory.ReduceHistoryAsync(cancellationToken);
            
            // Re-inject with reduced history
            var reducedMessagesWithHistory = _memory.InjectHistory(messagesList);
            
            var response = await base.GetResponseAsync(reducedMessagesWithHistory, options, cancellationToken);
            
            // Store messages after successful retry
            await _memory.StoreMessagesAsync(messagesList, response, cancellationToken);
            
            return response;
        }
    }

    /// <inheritdoc/>
    public override async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chatMessages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var messagesList = chatMessages.ToList();
        
        // Inject history before the request
        var messagesWithHistory = _memory.InjectHistory(messagesList);

        // Collect response chunks to build complete response for storage
        var responseContent = new System.Text.StringBuilder();
        ChatFinishReason? finishReason = null;

        await foreach (var update in base.GetStreamingResponseAsync(messagesWithHistory, options, cancellationToken))
        {
            // Accumulate content for storage
            if (update.Text is not null)
            {
                responseContent.Append(update.Text);
            }
            
            if (update.FinishReason is not null)
            {
                finishReason = update.FinishReason;
            }

            yield return update;
        }

        // Create a synthetic ChatResponse for storage
        var assistantMessage = new ChatMessage(ChatRole.Assistant, responseContent.ToString());
        var syntheticResponse = new ChatResponse([assistantMessage])
        {
            FinishReason = finishReason
        };

        // Store messages after streaming completes
        await _memory.StoreMessagesAsync(messagesList, syntheticResponse, cancellationToken);
    }

    /// <summary>
    /// Determines if an exception indicates a context/token overflow error.
    /// </summary>
    private static bool IsContextOverflowError(Exception ex)
    {
        var message = ex.Message.ToLowerInvariant();
        return message.Contains("context") ||
               message.Contains("token") ||
               message.Contains("length") ||
               message.Contains("exceed") ||
               message.Contains("maximum") ||
               message.Contains("limit");
    }
}

#pragma warning restore MEAI001
