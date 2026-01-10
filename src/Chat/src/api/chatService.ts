export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

// Configuration for AgentFramework API
// Supports both Ollama-compatible API (local) and Azure Functions API (deployed)
const config = {
  // Use environment variable or default to localhost for local development
  endpoint: import.meta.env.VITE_AGENT_API_ENDPOINT || 'http://localhost:8080',
  model: import.meta.env.VITE_AGENT_MODEL || 'Personal',
  // API mode: 'ollama' for local, 'azure-functions' for deployed Azure Functions
  apiMode: (import.meta.env.VITE_AGENT_API_MODE || 'ollama') as 'ollama' | 'azure-functions',
}

// Store thread ID for Azure Functions API conversation continuity
let currentThreadId: string | null = null

interface OllamaChatRequest {
  model: string
  messages: Array<{ role: string; content: string }>
  stream?: boolean
}

interface OllamaChatResponse {
  model: string
  message: {
    role: string
    content: string
  }
  done: boolean
}

interface AzureFunctionsRunRequest {
  message: string
  thread_id?: string
  role?: string
}

interface AzureFunctionsRunResponse {
  content?: string
  thread_id?: string
  correlation_id?: string
  status?: string
  error?: string
}

/**
 * Send a chat message using the Ollama-compatible API (local mode).
 */
async function sendMessageOllama(
  messages: ChatMessage[],
  onChunk: (content: string) => void,
  signal?: AbortSignal,
): Promise<string> {
  const url = `${config.endpoint}/api/chat`

  const request: OllamaChatRequest = {
    model: config.model,
    messages: messages.map((m) => ({ role: m.role, content: m.content })),
    stream: true,
  }

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
    signal,
  })

  if (!response.ok) {
    const errorText = await response.text()
    let errorMessage = `API error: ${response.status}`
    try {
      const errorJson = JSON.parse(errorText)
      errorMessage = errorJson.error || errorMessage
    } catch {
      errorMessage = errorText || errorMessage
    }
    throw new Error(errorMessage)
  }

  // Handle streaming response (newline-delimited JSON)
  const reader = response.body?.getReader()
  if (!reader) {
    throw new Error('No response body')
  }

  const decoder = new TextDecoder()
  let fullContent = ''
  let buffer = ''

  try {
    while (true) {
      const { done, value } = await reader.read()

      if (done) break

      buffer += decoder.decode(value, { stream: true })

      // Process complete JSON lines
      const lines = buffer.split('\n')
      buffer = lines.pop() || '' // Keep incomplete line in buffer

      for (const line of lines) {
        if (!line.trim()) continue

        try {
          const chunk: OllamaChatResponse = JSON.parse(line)
          if (chunk.message?.content) {
            onChunk(chunk.message.content)
            fullContent += chunk.message.content
          }
        } catch {
          // Skip malformed JSON lines
        }
      }
    }

    // Process any remaining buffer
    if (buffer.trim()) {
      try {
        const chunk: OllamaChatResponse = JSON.parse(buffer)
        if (chunk.message?.content) {
          onChunk(chunk.message.content)
          fullContent += chunk.message.content
        }
      } catch {
        // Skip malformed JSON
      }
    }
  } finally {
    reader.releaseLock()
  }

  return fullContent
}

/**
 * Send a chat message using Azure Functions API (deployed mode).
 */
async function sendMessageAzureFunctions(
  messages: ChatMessage[],
  onChunk: (content: string) => void,
  signal?: AbortSignal,
): Promise<string> {
  // Get the latest user message
  const userMessage = messages.filter((m) => m.role === 'user').pop()
  if (!userMessage) {
    throw new Error('No user message provided')
  }

  const url = `${config.endpoint}/api/agents/${config.model}/run`

  const request: AzureFunctionsRunRequest = {
    message: userMessage.content,
    thread_id: currentThreadId || undefined,
    role: 'user',
  }

  onChunk('') // Clear any previous content indicator

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
    signal,
  })

  if (!response.ok) {
    const errorText = await response.text()
    let errorMessage = `API error: ${response.status}`
    try {
      const errorJson = JSON.parse(errorText)
      errorMessage = errorJson.error || errorMessage
    } catch {
      errorMessage = errorText || errorMessage
    }
    throw new Error(errorMessage)
  }

  const result: AzureFunctionsRunResponse = await response.json()

  // Store thread ID for conversation continuity
  if (result.thread_id) {
    currentThreadId = result.thread_id
  }

  if (result.error) {
    throw new Error(result.error)
  }

  const content = result.content || ''
  onChunk(content)
  return content
}

/**
 * Send a chat message and receive a response.
 * Automatically selects the appropriate API based on configuration.
 *
 * @param messages - The conversation history
 * @param onChunk - Callback for streaming content chunks
 * @param signal - AbortSignal to cancel the request
 */
export async function sendMessage(
  messages: ChatMessage[],
  onChunk: (content: string) => void,
  signal?: AbortSignal,
): Promise<string> {
  if (config.apiMode === 'azure-functions') {
    return sendMessageAzureFunctions(messages, onChunk, signal)
  }
  return sendMessageOllama(messages, onChunk, signal)
}

/**
 * Clear the current conversation.
 */
export async function clearConversation(): Promise<void> {
  if (config.apiMode === 'azure-functions') {
    // For Azure Functions, just clear the thread ID
    currentThreadId = null
    return
  }

  // For Ollama API, call the reset endpoint
  try {
    await fetch(`${config.endpoint}/api/reset`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({ model: config.model }),
    })
  } catch {
    // Ignore errors - conversation will be cleared locally anyway
  }
}

/**
 * Send a chat message without streaming (for simpler use cases).
 */
export async function sendMessageSync(messages: ChatMessage[]): Promise<string> {
  if (config.apiMode === 'azure-functions') {
    return sendMessageAzureFunctions(messages, () => {})
  }

  const url = `${config.endpoint}/api/chat`

  const request: OllamaChatRequest = {
    model: config.model,
    messages: messages.map((m) => ({ role: m.role, content: m.content })),
    stream: false,
  }

  const response = await fetch(url, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(request),
  })

  if (!response.ok) {
    throw new Error(`API error: ${response.status}`)
  }

  const result: OllamaChatResponse = await response.json()
  return result.message?.content || ''
}

/**
 * Get available models/agents from the API.
 */
export async function getAvailableModels(): Promise<string[]> {
  if (config.apiMode === 'azure-functions') {
    // Azure Functions doesn't have a tags endpoint, return configured model
    return [config.model]
  }

  try {
    const response = await fetch(`${config.endpoint}/api/tags`)
    if (!response.ok) return [config.model]

    const result = await response.json()
    return result.models?.map((m: { name: string }) => m.name) || [config.model]
  } catch {
    return [config.model]
  }
}
