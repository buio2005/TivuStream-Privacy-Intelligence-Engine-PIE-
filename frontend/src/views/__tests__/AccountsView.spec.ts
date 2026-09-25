import { afterEach, describe, expect, it, vi } from 'vitest'
import { enableAutoUnmount, flushPromises, mount } from '@vue/test-utils'
import { createPinia, setActivePinia } from 'pinia'
import { i18n } from '@/i18n'
import router from '@/router'
import type { AccountSummary } from '@/api/types'
import { useSessionStore } from '@/stores/session'
import AccountsView from '@/views/AccountsView.vue'
import { answer, refused, stubEngine, type Received } from './support'

// Authentication Specification, Account management: the screen says what the
// engine did, never what was hoped. A refusal is shown with its reason, and an
// engine that did not answer is not taken to have refused or applied anything.

afterEach(() => {
  vi.unstubAllGlobals()
})

// The router is shared by every test. An application left mounted would go on
// reacting to its navigations, with a store of its own.
enableAutoUnmount(afterEach)

function summary(username: string, overrides: Partial<AccountSummary> = {}): AccountSummary {
  return {
    username,
    role: 'Viewer',
    enabled: true,
    passwordChangeRequired: false,
    createdAt: '2026-09-01T12:00:00Z',
    ...overrides,
  }
}

const list = answer([summary('maria', { passwordChangeRequired: true }), summary('root', { role: 'Administrator' })])

async function open(routes: Parameters<typeof stubEngine>[0], locale: 'en' | 'it' = 'en') {
  i18n.global.locale.value = locale

  const received: Received[] = stubEngine({ 'GET accounts': list, ...routes })

  const pinia = createPinia()
  setActivePinia(pinia)

  useSessionStore().account = { username: 'root', role: 'Administrator', passwordChangeRequired: false, expiresAt: '2026-09-01T20:00:00Z' }

  const view = mount(AccountsView, { global: { plugins: [pinia, i18n] } })

  await flushPromises()

  return { view, received }
}

function row(view: ReturnType<typeof mount>, username: string) {
  return view.findAll('li').find((item) => item.find('.name').text().startsWith(username))!
}

function button(scope: { findAll: (selector: string) => { text: () => string; trigger: (event: string) => Promise<void> }[] }, text: string) {
  return scope.findAll('button').find((candidate) => candidate.text() === text)!
}

describe('Accounts', () => {
  it('lists every account with its role, its state, and whether its password must be changed', async () => {
    const { view } = await open({}, 'it')

    expect(row(view, 'root').text()).toContain('(tu)')
    expect(row(view, 'root').text()).toContain('Amministratore')
    expect(row(view, 'maria').text()).toContain('Lettore')
    expect(row(view, 'maria').text()).toContain('Deve cambiare la password')
    expect(row(view, 'maria').text()).not.toContain('(tu)')
  })

  it('creates an account, and does not keep the initial password in the form', async () => {
    const { view, received } = await open({ 'POST accounts': { status: 201, body: answer(summary('giulia')) } })

    await view.find('input[name=username]').setValue('Giulia')
    await view.find('select').setValue('Administrator')
    await view.find('input[name=password]').setValue('an-initial-password')
    const forms = view.findAll('form')
    await forms[forms.length - 1]!.trigger('submit')
    await flushPromises()

    expect(received.find((request) => request.method === 'POST')!.body).toEqual({
      username: 'Giulia',
      role: 'Administrator',
      password: 'an-initial-password',
    })
    expect(view.find('[role=status]').text()).toBe('Account giulia created.')
    expect((view.find('input[name=password]').element as HTMLInputElement).value).toBe('')

    // The list is read again rather than patched with what was hoped.
    expect(received.filter((request) => request.method === 'GET')).toHaveLength(2)
  })

  it('says what the person being created will see, and that the creator knows the password', async () => {
    const { view } = await open({})

    expect(view.text()).toContain('Does not see which device contacted what.')
    expect(view.text()).toContain('Until then, you know it too.')

    await view.find('select').setValue('Administrator')

    expect(view.text()).toContain('including the activity of each device')
  })

  it('shows the reason the engine refused', async () => {
    const { view } = await open({ 'PATCH accounts/root': { status: 409, body: refused('LastAdministrator') } })

    await button(row(view, 'root'), 'Disable').trigger('click')
    await flushPromises()

    expect(view.find('[role=alert]').text()).toBe(
      'Refused: the installation would be left without an administrator able to sign in.',
    )
    expect(view.find('[role=status]').exists()).toBe(false)
  })

  it('does not claim a change was or was not applied when the engine did not answer', async () => {
    const { view } = await open({ 'PATCH accounts/maria': 'unreachable' })

    await button(row(view, 'maria'), 'Make administrator').trigger('click')
    await flushPromises()

    expect(view.find('[role=alert]').text()).toBe(
      'The engine did not answer: there is no way to know whether the change was applied. Reload the list to check.',
    )
  })

  it('shows a refusal it does not know by its code, rather than hiding it', async () => {
    const { view } = await open({ 'PATCH accounts/maria': { status: 400, body: refused('SomethingNew') } })

    await button(row(view, 'maria'), 'Disable').trigger('click')
    await flushPromises()

    expect(view.find('[role=alert]').text()).toBe('The engine answered unexpectedly (SomethingNew).')
  })

  it('removes an account only once the removal is confirmed', async () => {
    const { view, received } = await open({ 'DELETE accounts/maria': answer(null) })

    await button(row(view, 'maria'), 'Remove').trigger('click')

    expect(received.some((request) => request.method === 'DELETE')).toBe(false)
    expect(view.text()).toContain('Remove the account maria? Its sessions are closed and this cannot be undone.')

    await button(row(view, 'maria'), 'Confirm').trigger('click')
    await flushPromises()

    expect(received.find((request) => request.method === 'DELETE')!.path).toBe('accounts/maria')
  })

  it('resets a password with a body the engine reads as a reset', async () => {
    const { view, received } = await open({ 'PATCH accounts/maria': answer(summary('maria', { passwordChangeRequired: true })) })

    await button(row(view, 'maria'), 'Reset password').trigger('click')
    await row(view, 'maria').find('input[type=password]').setValue('a-password-set-by-root')
    await row(view, 'maria').find('form').trigger('submit')
    await flushPromises()

    expect(received.find((request) => request.method === 'PATCH')!.body).toEqual({ password: 'a-password-set-by-root' })
  })
})

describe('Accounts screen and roles', () => {
  it('is not offered to a viewer who asks for it by address', async () => {
    stubEngine({})

    const pinia = createPinia()
    setActivePinia(pinia)

    const session = useSessionStore()
    session.account = { username: 'maria', role: 'Viewer', passwordChangeRequired: false, expiresAt: '2026-09-01T20:00:00Z' }
    session.state = 'Authenticated'

    await router.push('/accounts')

    expect(router.currentRoute.value.name).toBe('dashboard')
  })
})
