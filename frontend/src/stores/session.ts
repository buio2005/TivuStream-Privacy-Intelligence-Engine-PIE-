import { defineStore } from 'pinia'
import { computed, ref } from 'vue'
import { call, EngineUnreachable, onSessionRefusal } from '@/api/client'
import { describeRefusal, type Message } from '@/api/messages'
import type { SessionInfo } from '@/api/types'
import { useAccountsStore } from './accounts'
import { useDomainDetailStore } from './domainDetail'
import { useDomainsStore } from './domains'
import { useScoreStore } from './score'

/**
 * The state the application is in. Always exactly one, as the Authentication
 * Specification lists them.
 */
export type SessionState =
  | 'Checking'
  | 'SetupRequired'
  | 'Unauthenticated'
  | 'Authenticated'
  | 'PasswordChangeRequired'

/**
 * Who is signed in, and what the application may show.
 *
 * Three situations are never confused: credentials that were wrong, an engine
 * that did not answer, and a session that ended. Saying "wrong credentials"
 * when the engine never answered would be a false statement.
 */
export const useSessionStore = defineStore('session', () => {
  const state = ref<SessionState>('Checking')
  const account = ref<SessionInfo | null>(null)

  /** Something to tell the person on the screen they are sent to, such as a session that ended. */
  const notice = ref<Message | null>(null)

  /** The engine did not answer while the session was being checked. */
  const unreachable = ref(false)

  const isAdministrator = computed(() => account.value?.role === 'Administrator')

  // Registered once: a session can end in answer to any request, from any
  // screen, and what that means is decided here.
  onSessionRefusal((code) => {
    if (code === 'AuthenticationRequired') {
      end({ key: 'auth.sessionEnded' })
    } else if (code === 'PasswordChangeRequired' && account.value) {
      account.value = { ...account.value, passwordChangeRequired: true }
      state.value = 'PasswordChangeRequired'
    }
  })

  /** Asks the engine whether a session exists. */
  async function check() {
    state.value = 'Checking'
    unreachable.value = false

    const outcome = await call<SessionInfo>('GET', 'auth/session')

    if (outcome.ok) {
      enter(outcome.data)
    } else if (outcome.code === 'SetupRequired') {
      state.value = 'SetupRequired'
    } else if (outcome.code === EngineUnreachable) {
      // Not "signed out": nobody knows yet whether a session exists.
      unreachable.value = true
    } else {
      // Whatever the engine said, it said that there is no session. Not
      // "ended": the interface does not know that there ever was one.
      account.value = null
      state.value = 'Unauthenticated'
    }
  }

  /** Signs in. Returns why it was refused, or `null`. */
  async function login(username: string, password: string): Promise<Message | null> {
    notice.value = null

    const outcome = await call<SessionInfo>('POST', 'auth/login', { username, password })

    if (outcome.ok) {
      enter(outcome.data)

      return null
    }

    if (outcome.code === 'SetupRequired') {
      state.value = 'SetupRequired'

      return null
    }

    return describeRefusal(outcome, 'credentials')
  }

  /** Creates the first administrator. Returns why it was refused, or `null`. */
  async function setup(setupCode: string, username: string, password: string): Promise<Message | null> {
    const outcome = await call<SessionInfo>('POST', 'setup', { setupCode, username, password })

    if (outcome.ok) {
      enter(outcome.data)

      return null
    }

    if (outcome.code === 'SetupAlreadyCompleted') {
      state.value = 'Unauthenticated'
      notice.value = { key: 'setup.alreadyCompleted' }

      return null
    }

    return describeRefusal(outcome, 'credentials')
  }

  /** Changes the password of the account signed in. Returns why it was refused, or `null`. */
  async function changePassword(currentPassword: string, newPassword: string): Promise<Message | null> {
    const outcome = await call<SessionInfo>('POST', 'auth/password', { currentPassword, newPassword })

    if (outcome.ok) {
      enter(outcome.data)

      return null
    }

    return describeRefusal(outcome, 'change')
  }

  /** Signs out. */
  async function logout() {
    // Whatever the engine answers, this browser forgets. A session the engine
    // could not close ends by itself at its expiry.
    await call('POST', 'auth/logout')

    end(null)
  }

  function enter(info: SessionInfo) {
    account.value = info
    notice.value = null
    state.value = info.passwordChangeRequired ? 'PasswordChangeRequired' : 'Authenticated'
  }

  /**
   * Leaves the session and forgets every datum read in it.
   *
   * Another person using the same browser must not find what the previous
   * one was looking at.
   */
  function end(message: Message | null) {
    // A 401 on a screen that is not signed in says nothing new: it is not a
    // session that ended.
    const wasSignedIn = state.value === 'Authenticated' || state.value === 'PasswordChangeRequired'

    useScoreStore().reset()
    useDomainsStore().reset()
    useDomainDetailStore().reset()
    useAccountsStore().reset()

    account.value = null
    state.value = 'Unauthenticated'

    if (message === null || wasSignedIn) {
      notice.value = message
    }
  }

  return {
    state,
    account,
    notice,
    unreachable,
    isAdministrator,
    check,
    login,
    setup,
    changePassword,
    logout,
  }
})
