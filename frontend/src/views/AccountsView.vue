<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { storeToRefs } from 'pinia'
import type { Message } from '@/api/messages'
import type { AccountRole, AccountSummary } from '@/api/types'
import MessageLine from '@/components/MessageLine.vue'
import { useAccountsStore, type AccountChange } from '@/stores/accounts'
import { useSessionStore } from '@/stores/session'

const store = useAccountsStore()
const { accounts, loading, failure } = storeToRefs(store)
const { account: me } = storeToRefs(useSessionStore())

const roles: AccountRole[] = ['Viewer', 'Administrator']

const newUsername = ref('')
const newRole = ref<AccountRole>('Viewer')
const newPassword = ref('')

/** The outcome of the last action, refused or done. */
const refusal = ref<Message | null>(null)
const outcome = ref<Message | null>(null)
const busy = ref(false)

/** The account whose password is being reset, and the one whose removal awaits confirmation. */
const resetting = ref<string | null>(null)
const resetPassword = ref('')
const removing = ref<string | null>(null)

onMounted(store.load)

async function act(action: () => Promise<Message | null>, done: Message) {
  busy.value = true
  outcome.value = null

  refusal.value = await action()
  outcome.value = refusal.value === null ? done : null
  busy.value = false
}

async function create() {
  const username = newUsername.value

  await act(() => store.create(username, newRole.value, newPassword.value), {
    key: 'accounts.created',
    params: { name: username.trim().toLowerCase() },
  })

  // The initial password is held no longer than the request that carries it.
  newPassword.value = ''

  if (refusal.value === null) {
    newUsername.value = ''
    newRole.value = 'Viewer'
  }
}

function change(target: AccountSummary, request: AccountChange) {
  return act(() => store.change(target.username, request), {
    key: 'accounts.changed',
    params: { name: target.username },
  })
}

async function reset(target: AccountSummary) {
  await change(target, { password: resetPassword.value })

  resetPassword.value = ''

  if (refusal.value === null) {
    resetting.value = null
  }
}

async function remove(target: AccountSummary) {
  removing.value = null

  await act(() => store.remove(target.username), { key: 'accounts.removed', params: { name: target.username } })
}
</script>

<template>
  <section>
    <h1>{{ $t('accounts.title') }}</h1>

    <p v-if="loading && accounts.length === 0">{{ $t('dashboard.loading') }}</p>

    <MessageLine :message="failure" />
    <MessageLine :message="refusal" />
    <MessageLine :message="outcome" kind="notice" />

    <ul class="accounts">
      <li v-for="item in accounts" :key="item.username">
        <div class="heading">
          <span class="name">
            {{ item.username }}
            <em v-if="item.username === me?.username">{{ $t('accounts.you') }}</em>
          </span>
          <span class="role">{{ $t(`role.${item.role}`) }}</span>
          <span class="state" :class="{ disabled: !item.enabled }">
            {{ $t(item.enabled ? 'accounts.enabled' : 'accounts.disabled') }}
          </span>
        </div>

        <p v-if="item.passwordChangeRequired" class="note">{{ $t('accounts.pendingPassword') }}</p>

        <div class="actions">
          <button type="button" :disabled="busy" @click="change(item, { enabled: !item.enabled })">
            {{ $t(item.enabled ? 'accounts.disable' : 'accounts.enable') }}
          </button>

          <button
            type="button"
            :disabled="busy"
            @click="change(item, { role: item.role === 'Administrator' ? 'Viewer' : 'Administrator' })"
          >
            {{ $t(item.role === 'Administrator' ? 'accounts.makeViewer' : 'accounts.makeAdministrator') }}
          </button>

          <button type="button" :disabled="busy" @click="resetting = resetting === item.username ? null : item.username">
            {{ $t('accounts.resetPassword') }}
          </button>

          <button type="button" :disabled="busy" @click="removing = item.username">
            {{ $t('accounts.remove') }}
          </button>
        </div>

        <form
          v-if="resetting === item.username"
          class="credentials inline"
          method="post"
          @submit.prevent="reset(item)"
        >
          <p class="hint">{{ $t('accounts.resetNote') }}</p>
          <label>
            {{ $t('password.new') }}
            <input v-model="resetPassword" type="password" autocomplete="new-password" required />
          </label>
          <p class="hint">{{ $t('password.rules') }}</p>
          <button type="submit" :disabled="busy">{{ $t('accounts.resetSubmit') }}</button>
        </form>

        <!-- Confirmed on the page, not in a browser dialog, and never undone by accident. -->
        <div v-if="removing === item.username" class="confirm" role="group">
          <p>{{ $t('accounts.confirmRemove', { name: item.username }) }}</p>
          <button type="button" :disabled="busy" @click="remove(item)">{{ $t('accounts.confirm') }}</button>
          <button type="button" @click="removing = null">{{ $t('accounts.cancel') }}</button>
        </div>
      </li>
    </ul>

    <h2>{{ $t('accounts.create') }}</h2>

    <!-- POST even without script: a form that fell back to GET would put the password in the address. -->
    <form class="credentials" method="post" @submit.prevent="create">
      <label>
        {{ $t('login.username') }}
        <input v-model="newUsername" name="username" autocomplete="off" spellcheck="false" required />
      </label>

      <label>
        {{ $t('accounts.role') }}
        <select v-model="newRole" name="role">
          <option v-for="role in roles" :key="role" :value="role">{{ $t(`role.${role}`) }}</option>
        </select>
      </label>
      <p class="hint">{{ $t(`role.note.${newRole}`) }}</p>

      <label>
        {{ $t('accounts.initialPassword') }}
        <input v-model="newPassword" name="password" type="password" autocomplete="new-password" required />
      </label>
      <p class="hint">{{ $t('password.rules') }} {{ $t('accounts.initialPasswordNote') }}</p>

      <button type="submit" :disabled="busy">{{ $t('accounts.submit') }}</button>
    </form>
  </section>
</template>

<style scoped>
.accounts {
  list-style: none;
  padding: 0;
}

.accounts li {
  padding: 0.8rem 0;
  border-bottom: 1px solid rgba(128, 128, 128, 0.3);
}

.heading {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  align-items: baseline;
}

.name {
  flex: 1;
  font-family: ui-monospace, monospace;
}

.name em {
  font-family: system-ui, sans-serif;
  font-style: normal;
  opacity: 0.7;
  margin-left: 0.4rem;
}

.role,
.state {
  font-size: 0.9rem;
  opacity: 0.8;
}

.state.disabled {
  text-decoration: line-through;
}

.note {
  margin: 0.3rem 0 0;
  font-size: 0.9rem;
  opacity: 0.85;
}

.actions {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
  margin-top: 0.5rem;
}

.actions button,
.confirm button {
  font: inherit;
  font-size: 0.85rem;
  padding: 0.2rem 0.6rem;
  border: 1px solid rgba(128, 128, 128, 0.6);
  border-radius: 4px;
  background: none;
  color: inherit;
  cursor: pointer;
}

.inline,
.confirm {
  margin-top: 0.8rem;
}

.confirm button {
  margin-right: 0.5rem;
}

h2 {
  margin-top: 2rem;
  font-size: 1.1rem;
}
</style>
