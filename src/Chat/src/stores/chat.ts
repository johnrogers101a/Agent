import { defineStore } from 'pinia'
import { ref, computed } from 'vue'
import { sendMessage, clearConversation, type ChatMessage } from '@/api/chatService'

export interface DisplayMessage {
  id: string
  role: 'user' | 'assistant'
  content: string
  timestamp: Date
  isStreaming?: boolean
}

export const useChatStore = defineStore('chat', () => {
  // State
  const messages = ref<DisplayMessage[]>([])
  const isLoading = ref(false)
  const streamingContent = ref('')
  const error = ref<string | null>(null)
  const abortController = ref<AbortController | null>(null)

  // Getters
  const hasMessages = computed(() => messages.value.length > 0)
  const lastMessage = computed(() => messages.value[messages.value.length - 1])

  // Convert display messages to API format
  const apiMessages = computed((): ChatMessage[] => {
    return messages.value
      .filter((m) => !m.isStreaming)
      .map((m) => ({
        role: m.role,
        content: m.content,
      }))
  })

  // Actions
  function generateId(): string {
    return `msg-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`
  }

  function addUserMessage(content: string): DisplayMessage {
    const message: DisplayMessage = {
      id: generateId(),
      role: 'user',
      content,
      timestamp: new Date(),
    }
    messages.value.push(message)
    return message
  }

  function addAssistantMessage(content: string, isStreaming = false): DisplayMessage {
    const message: DisplayMessage = {
      id: generateId(),
      role: 'assistant',
      content,
      timestamp: new Date(),
      isStreaming,
    }
    messages.value.push(message)
    return message
  }

  function updateLastAssistantMessage(content: string, isStreaming = false) {
    const lastAssistant = [...messages.value].reverse().find((m) => m.role === 'assistant')
    if (lastAssistant) {
      lastAssistant.content = content
      lastAssistant.isStreaming = isStreaming
    }
  }

  async function sendUserMessage(content: string): Promise<void> {
    if (isLoading.value || !content.trim()) return

    // Add user message
    addUserMessage(content.trim())

    // Start loading
    isLoading.value = true
    error.value = null
    streamingContent.value = ''

    // Add placeholder for assistant response
    addAssistantMessage('', true)

    // Create abort controller for cancellation
    abortController.value = new AbortController()

    try {
      // Send message with streaming
      await sendMessage(
        apiMessages.value,
        (chunk) => {
          streamingContent.value += chunk
          updateLastAssistantMessage(streamingContent.value, true)
        },
        abortController.value.signal,
      )

      // Finalize the message
      updateLastAssistantMessage(streamingContent.value, false)
    } catch (err) {
      if (err instanceof Error && err.name === 'AbortError') {
        // Request was cancelled - update message with partial content
        if (streamingContent.value) {
          updateLastAssistantMessage(streamingContent.value + ' [cancelled]', false)
        } else {
          // Remove empty assistant message
          messages.value.pop()
        }
      } else {
        // Real error - remove placeholder and set error
        messages.value.pop()
        error.value = err instanceof Error ? err.message : 'An unexpected error occurred'
        throw err
      }
    } finally {
      isLoading.value = false
      streamingContent.value = ''
      abortController.value = null
    }
  }

  function cancelRequest() {
    if (abortController.value) {
      abortController.value.abort()
    }
  }

  function clearMessages() {
    messages.value = []
    error.value = null
    streamingContent.value = ''
    // Clear conversation on the server (fire and forget)
    void clearConversation()
  }

  function clearError() {
    error.value = null
  }

  return {
    // State
    messages,
    isLoading,
    streamingContent,
    error,

    // Getters
    hasMessages,
    lastMessage,

    // Actions
    sendUserMessage,
    cancelRequest,
    clearMessages,
    clearError,
  }
})
