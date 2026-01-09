<script setup lang="ts">
import { computed } from 'vue'
import MarkdownIt from 'markdown-it'
import hljs from 'highlight.js'
import type { DisplayMessage } from '@/stores/chat'

const props = defineProps<{
  message: DisplayMessage
}>()

// Configure markdown-it with syntax highlighting
const md = new MarkdownIt({
  html: false,
  linkify: true,
  typographer: true,
  highlight: (str: string, lang: string) => {
    if (lang && hljs.getLanguage(lang)) {
      try {
        return hljs.highlight(str, { language: lang, ignoreIllegals: true }).value
      } catch {
        // Fall through to default
      }
    }
    return '' // Use external default escaping
  },
})

const isUser = computed(() => props.message.role === 'user')
const isAssistant = computed(() => props.message.role === 'assistant')

const renderedContent = computed(() => {
  if (isUser.value) {
    return props.message.content
  }
  return md.render(props.message.content || '')
})

const formattedTime = computed(() => {
  return props.message.timestamp.toLocaleTimeString([], {
    hour: '2-digit',
    minute: '2-digit',
  })
})
</script>

<template>
  <div class="flex w-full" :class="isUser ? 'justify-end' : 'justify-start'">
    <div class="flex max-w-[85%] gap-3" :class="isUser ? 'flex-row-reverse' : 'flex-row'">
      <!-- Avatar -->
      <div
        class="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full text-sm font-semibold"
        :class="isUser ? 'bg-blue-600 text-white' : 'bg-gray-200 text-gray-700'"
      >
        {{ isUser ? 'U' : 'A' }}
      </div>

      <!-- Message bubble -->
      <div
        class="rounded-2xl px-4 py-2"
        :class="[
          isUser ? 'bg-blue-600 text-white' : 'bg-gray-100 text-gray-900',
          isUser ? 'rounded-br-md' : 'rounded-bl-md',
        ]"
      >
        <!-- User message (plain text) -->
        <p v-if="isUser" class="whitespace-pre-wrap text-sm">{{ message.content }}</p>

        <!-- Assistant message (markdown) -->
        <div
          v-else
          class="prose prose-sm max-w-none dark:prose-invert"
          :class="{ 'typing-cursor': message.isStreaming && message.content }"
          v-html="renderedContent"
        />

        <!-- Streaming indicator when empty -->
        <div v-if="isAssistant && message.isStreaming && !message.content" class="flex gap-1 py-1">
          <span class="h-2 w-2 animate-bounce rounded-full bg-gray-400" style="animation-delay: 0ms"></span>
          <span class="h-2 w-2 animate-bounce rounded-full bg-gray-400" style="animation-delay: 150ms"></span>
          <span class="h-2 w-2 animate-bounce rounded-full bg-gray-400" style="animation-delay: 300ms"></span>
        </div>

        <!-- Timestamp -->
        <p
          class="mt-1 text-[10px]"
          :class="isUser ? 'text-blue-200' : 'text-gray-400'"
        >
          {{ formattedTime }}
        </p>
      </div>
    </div>
  </div>
</template>

<style>
/* Import highlight.js theme */
@import 'highlight.js/styles/github-dark.css';
</style>
