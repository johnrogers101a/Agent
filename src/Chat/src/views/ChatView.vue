<script setup lang="ts">
import { ref, watch, nextTick, onMounted } from 'vue'
import { useToast } from 'vue-toastification'
import { useChatStore } from '@/stores/chat'
import ChatMessage from '@/components/ChatMessage.vue'
import MessageInput from '@/components/MessageInput.vue'

const chatStore = useChatStore()
const toast = useToast()

const messagesContainer = ref<HTMLDivElement>()
const inputRef = ref<InstanceType<typeof MessageInput>>()

// Auto-scroll to bottom when new messages arrive
watch(
  () => chatStore.messages.length,
  () => {
    nextTick(() => {
      scrollToBottom()
    })
  },
)

// Also scroll when streaming content updates
watch(
  () => chatStore.streamingContent,
  () => {
    nextTick(() => {
      scrollToBottom()
    })
  },
)

// Watch for errors and show toast
watch(
  () => chatStore.error,
  (error) => {
    if (error) {
      toast.error(error)
      chatStore.clearError()
    }
  },
)

function scrollToBottom() {
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight
  }
}

async function handleSubmit(message: string) {
  try {
    await chatStore.sendUserMessage(message)
  } catch {
    // Error is handled via the watcher
  }
}

function handleReset() {
  chatStore.clearMessages()
  toast.info('Conversation cleared')
}

function handleCancel() {
  chatStore.cancelRequest()
}

onMounted(() => {
  inputRef.value?.focus()
})
</script>

<template>
  <div class="flex h-screen flex-col bg-gray-50">
    <!-- Header -->
    <header class="flex flex-shrink-0 items-center justify-between border-b border-gray-200 bg-white px-4 py-3 shadow-sm">
      <div class="flex items-center gap-3">
        <div class="flex h-10 w-10 items-center justify-center rounded-full bg-gradient-to-br from-blue-500 to-purple-600">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="h-6 w-6 text-white">
            <path stroke-linecap="round" stroke-linejoin="round" d="M9.813 15.904 9 18.75l-.813-2.846a4.5 4.5 0 0 0-3.09-3.09L2.25 12l2.846-.813a4.5 4.5 0 0 0 3.09-3.09L9 5.25l.813 2.846a4.5 4.5 0 0 0 3.09 3.09L15.75 12l-2.846.813a4.5 4.5 0 0 0-3.09 3.09ZM18.259 8.715 18 9.75l-.259-1.035a3.375 3.375 0 0 0-2.455-2.456L14.25 6l1.036-.259a3.375 3.375 0 0 0 2.455-2.456L18 2.25l.259 1.035a3.375 3.375 0 0 0 2.456 2.456L21.75 6l-1.035.259a3.375 3.375 0 0 0-2.456 2.456Z" />
          </svg>
        </div>
        <div>
          <h1 class="text-lg font-semibold text-gray-900">Personal Agent</h1>
          <p class="text-xs text-gray-500">Azure OpenAI Test Harness</p>
        </div>
      </div>

      <div class="flex items-center gap-2">
        <!-- Cancel button (shown during streaming) -->
        <button
          v-if="chatStore.isLoading"
          type="button"
          class="rounded-lg border border-red-200 bg-red-50 px-3 py-1.5 text-sm font-medium text-red-600 transition-colors hover:bg-red-100"
          @click="handleCancel"
        >
          Cancel
        </button>

        <!-- Reset button -->
        <button
          type="button"
          :disabled="!chatStore.hasMessages"
          class="rounded-lg border border-gray-200 bg-white px-3 py-1.5 text-sm font-medium text-gray-600 transition-colors hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-50"
          @click="handleReset"
        >
          Clear Chat
        </button>
      </div>
    </header>

    <!-- Messages area -->
    <div
      ref="messagesContainer"
      class="chat-scrollbar flex-1 overflow-y-auto p-4"
    >
      <!-- Empty state -->
      <div
        v-if="!chatStore.hasMessages"
        class="flex h-full flex-col items-center justify-center text-center"
      >
        <div class="mb-4 flex h-16 w-16 items-center justify-center rounded-full bg-gradient-to-br from-blue-100 to-purple-100">
          <svg xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24" stroke-width="1.5" stroke="currentColor" class="h-8 w-8 text-blue-600">
            <path stroke-linecap="round" stroke-linejoin="round" d="M8.625 12a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm0 0H8.25m4.125 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm0 0H12m4.125 0a.375.375 0 1 1-.75 0 .375.375 0 0 1 .75 0Zm0 0h-.375M21 12c0 4.556-4.03 8.25-9 8.25a9.764 9.764 0 0 1-2.555-.337A5.972 5.972 0 0 1 5.41 20.97a5.969 5.969 0 0 1-.474-.065 4.48 4.48 0 0 0 .978-2.025c.09-.457-.133-.901-.467-1.226C3.93 16.178 3 14.189 3 12c0-4.556 4.03-8.25 9-8.25s9 3.694 9 8.25Z" />
          </svg>
        </div>
        <h2 class="mb-2 text-xl font-semibold text-gray-900">Start a Conversation</h2>
        <p class="mb-6 max-w-md text-sm text-gray-500">
          Test the deployed Personal Agent. Ask about the weather or try out other capabilities.
        </p>
        <div class="flex flex-wrap justify-center gap-2">
          <button
            class="rounded-full border border-gray-200 bg-white px-4 py-2 text-sm text-gray-700 transition-colors hover:bg-gray-50"
            @click="handleSubmit('What can you help me with?')"
          >
            What can you help me with?
          </button>
          <button
            class="rounded-full border border-gray-200 bg-white px-4 py-2 text-sm text-gray-700 transition-colors hover:bg-gray-50"
            @click="handleSubmit('What is the weather in Seattle, WA?')"
          >
            Weather in Seattle
          </button>
          <button
            class="rounded-full border border-gray-200 bg-white px-4 py-2 text-sm text-gray-700 transition-colors hover:bg-gray-50"
            @click="handleSubmit('Tell me a joke')"
          >
            Tell me a joke
          </button>
        </div>
      </div>

      <!-- Messages list -->
      <div v-else class="mx-auto max-w-3xl space-y-4">
        <ChatMessage
          v-for="message in chatStore.messages"
          :key="message.id"
          :message="message"
        />
      </div>
    </div>

    <!-- Input area -->
    <div class="flex-shrink-0 border-t border-gray-200 bg-white p-4">
      <div class="mx-auto max-w-3xl">
        <MessageInput
          ref="inputRef"
          :disabled="chatStore.isLoading"
          placeholder="Type a message... (Enter to send, Shift+Enter for new line)"
          @submit="handleSubmit"
        />
        <p class="mt-2 text-center text-xs text-gray-400">
          Connected to Azure OpenAI deployment: gpt-oss-120b
        </p>
      </div>
    </div>
  </div>
</template>
