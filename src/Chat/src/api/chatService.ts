export interface ChatMessage {
  role: 'user' | 'assistant'
  content: string
}

const config = {
  endpoint: import.meta.env.VITE_AZURE_OPENAI_ENDPOINT,
  apiKey: import.meta.env.VITE_AZURE_OPENAI_KEY,
  assistantId: import.meta.env.VITE_AZURE_OPENAI_ASSISTANT_ID,
  apiVersion: import.meta.env.VITE_AZURE_OPENAI_API_VERSION || '2024-05-01-preview',
}

interface ThreadMessage {
  id: string
  role: 'user' | 'assistant'
  content: Array<{ type: string; text?: { value: string } }>
}

interface RunStatus {
  id: string
  status: 'queued' | 'in_progress' | 'requires_action' | 'completed' | 'failed' | 'cancelled' | 'expired'
  required_action?: {
    type: string
    submit_tool_outputs?: {
      tool_calls: Array<{
        id: string
        type: string
        function: {
          name: string
          arguments: string
        }
      }>
    }
  }
  last_error?: {
    code: string
    message: string
  }
}

// Store thread ID for conversation continuity
let currentThreadId: string | null = null

async function apiCall<T>(
  path: string,
  method: 'GET' | 'POST' = 'GET',
  body?: unknown,
): Promise<T> {
  const url = `${config.endpoint}/openai${path}?api-version=${config.apiVersion}`

  const response = await fetch(url, {
    method,
    headers: {
      'Content-Type': 'application/json',
      'api-key': config.apiKey,
    },
    body: body ? JSON.stringify(body) : undefined,
  })

  if (!response.ok) {
    const errorText = await response.text()
    let errorMessage = `API error: ${response.status}`
    try {
      const errorJson = JSON.parse(errorText)
      errorMessage = errorJson.error?.message || errorMessage
    } catch {
      errorMessage = errorText || errorMessage
    }
    throw new Error(errorMessage)
  }

  return response.json()
}

/**
 * Create a new thread for conversation.
 */
async function createThread(): Promise<string> {
  const result = await apiCall<{ id: string }>('/threads', 'POST', {})
  return result.id
}

/**
 * Add a message to the thread.
 */
async function addMessage(threadId: string, content: string): Promise<void> {
  await apiCall(`/threads/${threadId}/messages`, 'POST', {
    role: 'user',
    content,
  })
}

/**
 * Create a run for the assistant on the thread.
 */
async function createRun(threadId: string): Promise<string> {
  const result = await apiCall<{ id: string }>(`/threads/${threadId}/runs`, 'POST', {
    assistant_id: config.assistantId,
  })
  return result.id
}

/**
 * Get the status of a run.
 */
async function getRunStatus(threadId: string, runId: string): Promise<RunStatus> {
  return apiCall<RunStatus>(`/threads/${threadId}/runs/${runId}`)
}

/**
 * Get the latest assistant message from the thread.
 */
async function getLatestAssistantMessage(threadId: string): Promise<string> {
  const result = await apiCall<{ data: ThreadMessage[] }>(`/threads/${threadId}/messages`)

  // Find the latest assistant message
  for (const msg of result.data) {
    if (msg.role === 'assistant') {
      const textContent = msg.content.find((c) => c.type === 'text')
      return textContent?.text?.value || ''
    }
  }

  return ''
}

/**
 * Poll until the run is complete, handling tool calls.
 */
async function waitForRunCompletion(
  threadId: string,
  runId: string,
  onStatus?: (status: string) => void,
  signal?: AbortSignal,
): Promise<void> {
  const maxAttempts = 120 // 2 minutes with 1s polling
  let attempts = 0

  while (attempts < maxAttempts) {
    if (signal?.aborted) {
      throw new Error('Request cancelled')
    }

    const status = await getRunStatus(threadId, runId)
    onStatus?.(status.status)

    switch (status.status) {
      case 'completed':
        return

      case 'failed':
      case 'cancelled':
      case 'expired':
        throw new Error(status.last_error?.message || `Run ${status.status}`)

      case 'requires_action':
        // The Azure OpenAI service handles tool execution via MCP triggers
        // This should not happen if tools are properly configured as Azure Functions
        // But if it does, we need to wait for Azure to complete the tool calls
        onStatus?.('Executing tools...')
        break

      case 'queued':
      case 'in_progress':
        // Continue polling
        break
    }

    await new Promise((resolve) => setTimeout(resolve, 1000))
    attempts++
  }

  throw new Error('Run timed out')
}

/**
 * Send a chat message and receive a response.
 * Uses the Azure OpenAI Assistants API with tool execution.
 *
 * @param messages - The conversation history (used for display, not sent to API since thread maintains state)
 * @param onChunk - Callback for status updates (not streaming tokens)
 * @param signal - AbortSignal to cancel the request
 */
export async function sendMessage(
  messages: ChatMessage[],
  onChunk: (content: string) => void,
  signal?: AbortSignal,
): Promise<string> {
  // Get the latest user message
  const userMessage = messages.filter((m) => m.role === 'user').pop()
  if (!userMessage) {
    throw new Error('No user message provided')
  }

  // Create a new thread if we don't have one
  if (!currentThreadId) {
    onChunk('Creating conversation...\n')
    currentThreadId = await createThread()
  }

  // Add the user message to the thread
  await addMessage(currentThreadId, userMessage.content)

  // Create a run
  onChunk('Thinking...\n')
  const runId = await createRun(currentThreadId)

  // Wait for completion, handling tool calls
  await waitForRunCompletion(
    currentThreadId,
    runId,
    (status) => {
      if (status === 'in_progress') {
        onChunk('')
      }
    },
    signal,
  )

  // Get the response
  const response = await getLatestAssistantMessage(currentThreadId)

  return response
}

/**
 * Clear the current conversation thread.
 */
export function clearConversation(): void {
  currentThreadId = null
}

/**
 * Send a chat message without status updates (for simpler use cases).
 */
export async function sendMessageSync(messages: ChatMessage[]): Promise<string> {
  return sendMessage(messages, () => {})
}
