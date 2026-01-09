<script setup lang="ts">
import { ref, watch, nextTick } from 'vue'

const props = defineProps<{
  disabled?: boolean
  placeholder?: string
}>()

const emit = defineEmits<{
  submit: [message: string]
}>()

const inputRef = ref<HTMLTextAreaElement>()
const message = ref('')

// Auto-resize textarea
function adjustHeight() {
  const textarea = inputRef.value
  if (!textarea) return

  textarea.style.height = 'auto'
  const maxHeight = 200
  textarea.style.height = `${Math.min(textarea.scrollHeight, maxHeight)}px`
}

watch(message, () => {
  nextTick(adjustHeight)
})

function handleSubmit() {
  const trimmed = message.value.trim()
  if (!trimmed || props.disabled) return

  emit('submit', trimmed)
  message.value = ''
  nextTick(adjustHeight)
}

function handleKeydown(event: KeyboardEvent) {
  // Submit on Enter (without Shift)
  if (event.key === 'Enter' && !event.shiftKey) {
    event.preventDefault()
    handleSubmit()
  }
}

function focus() {
  inputRef.value?.focus()
}

defineExpose({ focus })
</script>

<template>
  <div class="flex items-end gap-2 rounded-2xl border border-gray-200 bg-white p-2 shadow-sm">
    <textarea
      ref="inputRef"
      v-model="message"
      :disabled="disabled"
      :placeholder="placeholder ?? 'Type a message...'"
      rows="1"
      class="max-h-[200px] min-h-[40px] flex-1 resize-none bg-transparent px-2 py-2 text-sm text-gray-900 placeholder-gray-400 focus:outline-none disabled:cursor-not-allowed disabled:opacity-50"
      @keydown="handleKeydown"
    />

    <button
      type="button"
      :disabled="disabled || !message.trim()"
      class="flex h-10 w-10 flex-shrink-0 items-center justify-center rounded-xl bg-blue-600 text-white transition-colors hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
      @click="handleSubmit"
    >
      <svg
        xmlns="http://www.w3.org/2000/svg"
        viewBox="0 0 24 24"
        fill="currentColor"
        class="h-5 w-5"
      >
        <path
          d="M3.478 2.404a.75.75 0 0 0-.926.941l2.432 7.905H13.5a.75.75 0 0 1 0 1.5H4.984l-2.432 7.905a.75.75 0 0 0 .926.94 60.519 60.519 0 0 0 18.445-8.986.75.75 0 0 0 0-1.218A60.517 60.517 0 0 0 3.478 2.404Z"
        />
      </svg>
    </button>
  </div>
</template>
