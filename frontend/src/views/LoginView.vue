<script setup lang="ts">
import { ref } from 'vue'
import { storeToRefs } from 'pinia'
import type { Message } from '@/api/messages'
import MessageLine from '@/components/MessageLine.vue'
import { useSessionStore } from '@/stores/session'

const session = useSessionStore()
const { notice } = storeToRefs(session)

const username = ref('')
const password = ref('')
const refusal = ref<Message | null>(null)
const busy = ref(false)

async function submit() {
  busy.value = true
  refusal.value = null

  refusal.value = await session.login(username.value, password.value)

  // The password is held no longer than the request that carries it.
  password.value = ''
  busy.value = false
}
</script>

<template>
  <section>
    <h1>{{ $t('login.title') }}</h1>

    <!--
      A session that ended is told apart from a sign in that failed: the first
      is news about the past, the second about what was just typed.
    -->
    <MessageLine :message="notice" kind="notice" />

    <!-- POST even without script: a form that fell back to GET would put the password in the address. -->
    <form class="credentials" method="post" @submit.prevent="submit">
      <label>
        {{ $t('login.username') }}
        <input v-model="username" name="username" autocomplete="username" spellcheck="false" required />
      </label>

      <label>
        {{ $t('login.password') }}
        <input v-model="password" name="password" type="password" autocomplete="current-password" required />
      </label>

      <MessageLine :message="refusal" />

      <button type="submit" :disabled="busy">{{ $t('login.submit') }}</button>
    </form>
  </section>
</template>
