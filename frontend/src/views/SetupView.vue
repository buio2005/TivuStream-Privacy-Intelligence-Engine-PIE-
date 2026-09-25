<script setup lang="ts">
import { ref } from 'vue'
import type { Message } from '@/api/messages'
import MessageLine from '@/components/MessageLine.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()

const code = ref('')
const username = ref('')
const password = ref('')
const refusal = ref<Message | null>(null)
const busy = ref(false)

async function submit() {
  busy.value = true

  refusal.value = await session.setup(code.value, username.value, password.value)

  // The password is held no longer than the request that carries it.
  password.value = ''
  busy.value = false
}
</script>

<template>
  <section>
    <h1>{{ $t('setup.title') }}</h1>
    <p>{{ $t('setup.intro') }}</p>

    <!-- POST even without script: a form that fell back to GET would put the password in the address. -->
    <form class="credentials" method="post" @submit.prevent="submit">
      <label>
        {{ $t('setup.code') }}
        <input v-model="code" name="setupCode" autocomplete="one-time-code" spellcheck="false" required />
      </label>

      <label>
        {{ $t('login.username') }}
        <input v-model="username" name="username" autocomplete="username" spellcheck="false" required />
      </label>

      <label>
        {{ $t('login.password') }}
        <input v-model="password" name="password" type="password" autocomplete="new-password" required />
      </label>
      <p class="hint">{{ $t('password.rules') }}</p>

      <MessageLine :message="refusal" />

      <button type="submit" :disabled="busy">{{ $t('setup.submit') }}</button>
    </form>
  </section>
</template>
