<script setup lang="ts">
import { ref } from 'vue'
import { storeToRefs } from 'pinia'
import type { Message } from '@/api/messages'
import MessageLine from '@/components/MessageLine.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
const { state } = storeToRefs(session)

const current = ref('')
const next = ref('')
const repeated = ref('')
const refusal = ref<Message | null>(null)
const done = ref(false)
const busy = ref(false)

async function submit() {
  done.value = false

  // Checked here only because two fields are the interface's own idea. Every
  // rule about the password itself is the engine's.
  if (next.value !== repeated.value) {
    refusal.value = { key: 'password.mismatch' }

    return
  }

  busy.value = true

  refusal.value = await session.changePassword(current.value, next.value)
  done.value = refusal.value === null

  // Held no longer than the request that carries them.
  current.value = ''
  next.value = ''
  repeated.value = ''
  busy.value = false
}
</script>

<template>
  <section>
    <h1>{{ $t('password.title') }}</h1>

    <p v-if="state === 'PasswordChangeRequired'" class="required">{{ $t('password.required') }}</p>

    <p v-if="done" role="status">{{ $t('password.done') }}</p>

    <!-- POST even without script: a form that fell back to GET would put the password in the address. -->
    <form class="credentials" method="post" @submit.prevent="submit">
      <label>
        {{ $t('password.current') }}
        <input v-model="current" name="currentPassword" type="password" autocomplete="current-password" required />
      </label>

      <label>
        {{ $t('password.new') }}
        <input v-model="next" name="newPassword" type="password" autocomplete="new-password" required />
      </label>

      <label>
        {{ $t('password.repeat') }}
        <input v-model="repeated" name="repeatedPassword" type="password" autocomplete="new-password" required />
      </label>
      <p class="hint">{{ $t('password.rules') }}</p>

      <MessageLine :message="refusal" />

      <button type="submit" :disabled="busy">{{ $t('password.submit') }}</button>
    </form>
  </section>
</template>

<style scoped>
.required {
  font-weight: 600;
}
</style>
