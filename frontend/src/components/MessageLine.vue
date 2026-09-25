<script setup lang="ts">
import type { Message } from '@/api/messages'

defineProps<{ message: Message | null; kind?: 'refusal' | 'notice' }>()
</script>

<!--
  One sentence of the catalogue, announced to assistive technology as it
  appears. A refusal and a notice look different: a session that ended is
  information, not a mistake the person made.
-->

<template>
  <p v-if="message" :role="kind === 'notice' ? 'status' : 'alert'" :class="kind ?? 'refusal'">
    {{ $t(message.key, message.params ?? {}) }}
  </p>
</template>

<style scoped>
p {
  margin: 0.8rem 0;
  padding: 0.5rem 0.8rem;
  border-left: 3px solid currentColor;
}

.notice {
  opacity: 0.85;
}
</style>
