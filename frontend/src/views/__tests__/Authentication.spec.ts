import { afterEach, describe, expect, it, vi } from 'vitest'
import { flushPromises, mount, type VueWrapper } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import App from '@/App.vue'
import { i18n } from '@/i18n'
import router from '@/router'
import { useAccountsStore } from '@/stores/accounts'
import { useDomainsStore } from '@/stores/domains'
import { useScoreStore } from '@/stores/score'
import { useSessionStore } from '@/stores/session'
import { answer, refused, sessionOf, stubEngine, type Reply } from './support'

// Authentication Specification, Frontend and F1 to F3: the application is in
// exactly one of five states; wrong credentials, an engine that did not
// answer, a session that ended and an unsuitable connection are four
// different messages; a refused sign in reads the same whatever the reason;
// nothing of the network survives leaving.

afterEach(() => {
  vi.unstubAllGlobals()
})

const score = answer({
  overallScore: 62,
  status: 'Fair',
  trend: null,
  coverage: 85,
  algorithmVersion: '3.0.0',
  generatedAt: '2026-09-01T12:00:00Z',
  breakdown: [],
})

async function start(routes: Parameters<typeof stubEngine>[0], locale: 'en' | 'it' = 'en') {
  i18n.global.locale.value = locale

  const received = stubEngine({ 'GET npss': score, ...routes })

  // Active before the first navigation: the router asks the session about roles.
  const pinia = createPinia()
  setActivePinia(pinia)

  await router.push('/')

  const app = mount(App, { global: { plugins: [pinia, router, i18n] } })

  await flushPromises()

  return { app, received }
}

async function signIn(app: VueWrapper, username = 'maria', password = 'a-password-for-the-tests') {
  await app.find('input[name=username]').setValue(username)
  await app.find('input[name=password]').setValue(password)
  await app.find('form').trigger('submit')
  await flushPromises()
}

function alertText(app: VueWrapper): string {
  return app.find('[role=alert]').text()
}

describe('One state at a time', () => {
  it('shows the setup screen, and nothing else, on an installation with no account', async () => {
    const { app } = await start({ 'GET auth/session': { status: 401, body: refused('SetupRequired') } })

    expect(app.text()).toContain('First run setup')
    expect(app.find('input[name=setupCode]').exists()).toBe(true)
    expect(app.find('nav.sections a').exists()).toBe(false)
  })

  it('shows the sign in screen, without saying a session ended, when there never was one', async () => {
    const { app } = await start({ 'GET auth/session': { status: 401, body: refused('AuthenticationRequired') } })

    expect(app.text()).toContain('Sign in')
    expect(app.text()).not.toContain('Your session has ended')
  })

  it('does not call an engine that did not answer "signed out", and offers to try again', async () => {
    const { app } = await start({ 'GET auth/session': 'unreachable' })

    expect(alertText(app)).toBe('The engine did not answer. Your credentials were not checked.')
    expect(app.find('input[name=password]').exists()).toBe(false)
    expect(app.find('button.primary').text()).toBe('Try again')
  })

  it('keeps every data screen out of reach while the password must be changed', async () => {
    const { app, received } = await start({ 'GET auth/session': sessionOf('maria', 'Viewer', true) })

    expect(app.text()).toContain('This password was chosen by someone else.')
    expect(app.find('nav.sections').exists()).toBe(false)

    // Not even asked for: the dashboard is not mounted.
    expect(received.some((request) => request.path === 'npss')).toBe(false)
  })

  it('shows the data once signed in, and the accounts only to an administrator', async () => {
    const viewer = await start({ 'GET auth/session': sessionOf('maria', 'Viewer') })

    expect(viewer.app.find('.overall').exists()).toBe(true)
    expect(viewer.app.text()).not.toContain('Accounts')

    const administrator = await start({ 'GET auth/session': sessionOf('root') })

    expect(administrator.app.find('nav.sections').text()).toContain('Accounts')
  })
})

describe('F1: four situations, four messages', () => {
  async function messageFor(login: Reply | 'unreachable', locale: 'en' | 'it') {
    const { app } = await start(
      { 'GET auth/session': { status: 401, body: refused('AuthenticationRequired') }, 'POST auth/login': login },
      locale,
    )

    await signIn(app)

    return alertText(app)
  }

  async function sessionEndedMessage(locale: 'en' | 'it') {
    let sessionAlive = true

    const { app } = await start(
      {
        'GET auth/session': sessionOf('maria'),
        'GET domains': () => (sessionAlive ? { body: answer({}) } : { status: 401, body: refused('AuthenticationRequired') }),
      },
      locale,
    )

    sessionAlive = false
    await router.push('/domains')
    await flushPromises()

    return app.find('[role=status]').text()
  }

  for (const locale of ['en', 'it'] as const) {
    it(`keeps them apart in ${locale}`, async () => {
      const messages = [
        await messageFor({ status: 401, body: refused('AuthenticationFailed') }, locale),
        await messageFor('unreachable', locale),
        await sessionEndedMessage(locale),
        await messageFor({ status: 403, body: refused('TransportNotSecure') }, locale),
      ]

      expect(new Set(messages).size).toBe(4)
      expect(messages.every((message) => message.length > 0)).toBe(true)
    })
  }

  it('says the credentials were wrong only when the engine said so', async () => {
    expect(await messageFor({ status: 401, body: refused('AuthenticationFailed') }, 'en')).toBe('Username or password incorrect.')
    expect(await messageFor('unreachable', 'en')).toBe('The engine did not answer. Your credentials were not checked.')

    // A proxy's error page is not the engine's answer either.
    expect(await messageFor({ status: 502, body: undefined }, 'en')).toBe('The engine did not answer. Your credentials were not checked.')
  })

  it('says how long to wait when there were too many attempts', async () => {
    const message = await messageFor({ status: 429, body: refused('TooManyAttempts'), headers: { 'Retry-After': '240' } }, 'it')

    expect(message).toBe('Troppi tentativi. Riprova fra 240 secondi.')
  })

  it('tells a session that ended apart from a sign in that failed', async () => {
    expect(await sessionEndedMessage('en')).toBe('Your session has ended. Sign in again.')
  })
})

describe('F3: a refused sign in reads the same whatever the reason', () => {
  it('ignores what the engine wrote and speaks only from the code', async () => {
    const texts: string[] = []

    for (const message of ['No such account.', 'Wrong password.', 'Account disabled.']) {
      const { app } = await start({
        'GET auth/session': { status: 401, body: refused('AuthenticationRequired') },
        'POST auth/login': { status: 401, body: refused('AuthenticationFailed', { message }) },
      })

      await signIn(app)
      texts.push(alertText(app))
    }

    expect(new Set(texts)).toEqual(new Set(['Username or password incorrect.']))
  })
})

describe('F2: nothing of the network survives leaving', () => {
  async function signedInWithData() {
    const result = await start({
      'GET auth/session': sessionOf('root'),
      'GET domains': answer({ period: null, periodsObserved: 3, periodsRequested: 24, domains: [
        {
          name: 'tracker.example',
          category: 'Tracking',
          categoryConfidence: 'High',
          categorySource: 'A list',
          categorySourceUpdatedAt: null,
          reputation: null,
          firstSeen: '2026-09-01T11:00:00Z',
          lastSeen: '2026-09-01T12:00:00Z',
          observationQuality: 'PeriodBounded',
          occurrences: 4,
        },
      ] }),
      'GET accounts': answer([{ username: 'root', role: 'Administrator', enabled: true, passwordChangeRequired: false, createdAt: '2026-09-01T12:00:00Z' }]),
      'POST auth/logout': answer(null),
    })

    await router.push('/domains')
    await flushPromises()
    await router.push('/accounts')
    await flushPromises()

    expect(useScoreStore().score).not.toBeNull()
    expect(useDomainsStore().domains).toHaveLength(1)
    expect(useAccountsStore().accounts).toHaveLength(1)

    return result
  }

  function expectEmpty() {
    expect(useScoreStore().score).toBeNull()
    expect(useDomainsStore().domains).toEqual([])
    expect(useDomainsStore().periodsObserved).toBe(0)
    expect(useAccountsStore().accounts).toEqual([])
    expect(useSessionStore().account).toBeNull()
  }

  it('empties every store on signing out', async () => {
    const { app } = await signedInWithData()

    await app.find('.who button').trigger('click')
    await flushPromises()

    expectEmpty()
    expect(app.text()).not.toContain('tracker.example')
    expect(app.text()).not.toContain('Your session has ended')
  })

  it('empties every store when the session ends on its own', async () => {
    const { app } = await signedInWithData()

    stubEngine({ 'GET npss': { status: 401, body: refused('AuthenticationRequired') } })

    await router.push('/')
    await flushPromises()

    expectEmpty()
    expect(app.text()).toContain('Your session has ended. Sign in again.')
  })

  it('forgets even when the engine does not answer the sign out', async () => {
    const { app } = await signedInWithData()

    stubEngine({ 'POST auth/logout': 'unreachable' })

    await app.find('.who button').trigger('click')
    await flushPromises()

    expectEmpty()
  })
})

describe('Passwords', () => {
  it('are sent in the body of a POST and are gone from the form afterwards', async () => {
    const { app, received } = await start({
      'GET auth/session': { status: 401, body: refused('AuthenticationRequired') },
      'POST auth/login': { status: 401, body: refused('AuthenticationFailed') },
    })

    expect(app.find('form').attributes('method')).toBe('post')

    await signIn(app, 'maria', 'the-secret-typed-here')

    const login = received.find((request) => request.path === 'auth/login')!

    expect(login.method).toBe('POST')
    expect(login.body).toEqual({ username: 'maria', password: 'the-secret-typed-here' })
    expect((app.find('input[name=password]').element as HTMLInputElement).value).toBe('')
    for (let index = 0; index < localStorage.length; index++) {
      expect(localStorage.getItem(localStorage.key(index)!)).not.toContain('the-secret-typed-here')
    }
  })

  it('leads from a forced change to the data once the password is changed', async () => {
    const { app, received } = await start({
      'GET auth/session': sessionOf('maria', 'Viewer', true),
      'POST auth/password': sessionOf('maria', 'Viewer', false),
    })

    await app.find('input[name=currentPassword]').setValue('the-password-set-by-root')
    await app.find('input[name=newPassword]').setValue('a-password-of-my-own')
    await app.find('input[name=repeatedPassword]').setValue('a-password-of-my-own')
    await app.find('form').trigger('submit')
    await flushPromises()

    expect(received.find((request) => request.path === 'auth/password')!.body).toEqual({
      currentPassword: 'the-password-set-by-root',
      newPassword: 'a-password-of-my-own',
    })
    expect(useSessionStore().state).toBe('Authenticated')
    expect(app.find('.overall').exists()).toBe(true)
  })

  it('does not bother the engine when the two new passwords differ', async () => {
    const { app, received } = await start({ 'GET auth/session': sessionOf('maria', 'Viewer', true) })

    await app.find('input[name=currentPassword]').setValue('the-password-set-by-root')
    await app.find('input[name=newPassword]').setValue('a-password-of-my-own')
    await app.find('input[name=repeatedPassword]').setValue('a-password-of-my-owm')
    await app.find('form').trigger('submit')
    await flushPromises()

    expect(alertText(app)).toBe('The two passwords differ.')
    expect(received.some((request) => request.path === 'auth/password')).toBe(false)
  })

  it('says why the engine refused a new password, in the reader’s language', async () => {
    const { app } = await start(
      {
        'GET auth/session': sessionOf('maria', 'Viewer', true),
        'POST auth/password': { status: 422, body: refused('PasswordRejected', { reason: 'Unchanged' }) },
      },
      'it',
    )

    await app.find('input[name=currentPassword]').setValue('the-same-password')
    await app.find('input[name=newPassword]').setValue('the-same-password')
    await app.find('input[name=repeatedPassword]').setValue('the-same-password')
    await app.find('form').trigger('submit')
    await flushPromises()

    expect(alertText(app)).toBe('Deve essere diversa dalla password attuale.')
  })

  it('moves to the forced change when the engine says so in the middle of a session', async () => {
    const { app } = await start({ 'GET auth/session': sessionOf('maria', 'Viewer') })

    stubEngine({ 'GET domains': { status: 403, body: refused('PasswordChangeRequired') } })

    await router.push('/domains')
    await flushPromises()

    expect(useSessionStore().state).toBe('PasswordChangeRequired')
    expect(app.text()).toContain('This password was chosen by someone else.')
  })
})

describe('Setup', () => {
  it('signs in the administrator it creates', async () => {
    const { app } = await start({
      'GET auth/session': { status: 401, body: refused('SetupRequired') },
      'POST setup': { status: 201, body: sessionOf('root') },
    })

    await app.find('input[name=setupCode]').setValue('ABCD-EFGH-JKLM')
    await signIn(app, 'root', 'a-password-for-the-setup')

    expect(useSessionStore().state).toBe('Authenticated')
  })

  it('says a wrong code is wrong, and that it changes at every start', async () => {
    const { app } = await start({
      'GET auth/session': { status: 401, body: refused('SetupRequired') },
      'POST setup': { status: 401, body: refused('SetupCodeRejected') },
    })

    await app.find('input[name=setupCode]').setValue('WRONG')
    await signIn(app, 'root', 'a-password-for-the-setup')

    expect(alertText(app)).toContain('It changes every time the service starts')
  })

  it('sends someone who arrived second to the sign in screen, saying why', async () => {
    const { app } = await start({
      'GET auth/session': { status: 401, body: refused('SetupRequired') },
      'POST setup': { status: 409, body: refused('SetupAlreadyCompleted') },
    })

    await app.find('input[name=setupCode]').setValue('ABCD-EFGH-JKLM')
    await signIn(app, 'root', 'a-password-for-the-setup')

    expect(useSessionStore().state).toBe('Unauthenticated')
    expect(app.text()).toContain('Setup has already been completed.')
  })
})
