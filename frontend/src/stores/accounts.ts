import { defineStore } from 'pinia'
import { ref } from 'vue'
import { call } from '@/api/client'
import { describeRefusal, type Message } from '@/api/messages'
import type { AccountRole, AccountSummary } from '@/api/types'

/** A change to an account. What is left out stays as it is. */
export interface AccountChange {
  role?: AccountRole
  enabled?: boolean
  password?: string
}

/**
 * Holds the accounts, for an administrator.
 *
 * After every change the list is read again rather than patched locally: the
 * engine may have refused part of what was asked, and what is shown must be
 * what is, not what was hoped.
 */
export const useAccountsStore = defineStore('accounts', () => {
  const accounts = ref<AccountSummary[]>([])
  const loading = ref(false)

  /** Why the list could not be read, when it could not. */
  const failure = ref<Message | null>(null)

  async function load() {
    loading.value = true
    failure.value = null

    const outcome = await call<AccountSummary[]>('GET', 'accounts')

    if (outcome.ok) {
      accounts.value = outcome.data
    } else {
      accounts.value = []
      failure.value = describeRefusal(outcome, 'change')
    }

    loading.value = false
  }

  /** Creates an account. Returns why it was refused, or `null`. */
  async function create(username: string, role: AccountRole, password: string): Promise<Message | null> {
    return settle(await call('POST', 'accounts', { username, role, password }))
  }

  /** Changes an account. Returns why it was refused, or `null`. */
  async function change(username: string, request: AccountChange): Promise<Message | null> {
    return settle(await call('PATCH', `accounts/${encodeURIComponent(username)}`, request))
  }

  /** Removes an account. Returns why it was refused, or `null`. */
  async function remove(username: string): Promise<Message | null> {
    return settle(await call('DELETE', `accounts/${encodeURIComponent(username)}`))
  }

  async function settle(outcome: { ok: true } | { ok: false; code: string; reason?: string; retryAfter?: number }) {
    await load()

    return outcome.ok ? null : describeRefusal(outcome, 'change')
  }

  /** Forgets everything read. Called when the session ends. */
  function reset() {
    accounts.value = []
    loading.value = false
    failure.value = null
  }

  return { accounts, loading, failure, load, create, change, remove, reset }
})
