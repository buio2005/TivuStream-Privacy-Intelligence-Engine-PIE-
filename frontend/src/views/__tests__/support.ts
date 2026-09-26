import { mount, flushPromises, RouterLinkStub } from '@vue/test-utils'
import { createPinia } from 'pinia'
import { vi } from 'vitest'
import type { Component } from 'vue'
import { i18n } from '@/i18n'
import type { ApiResponse } from '@/api/types'

/** Wraps a payload the way the engine answers a request that succeeded. */
export function answer<T>(data: T): ApiResponse<T> {
  return {
    success: true,
    apiVersion: 'v1',
    timestamp: '2026-09-01T12:00:00Z',
    data,
    error: null,
  }
}

/** The engine's answer when it has nothing to give yet, as it does with a 503. */
export function refusal(code: string): ApiResponse<never> {
  return {
    success: false,
    apiVersion: 'v1',
    timestamp: '2026-09-01T12:00:00Z',
    data: null,
    error: { code, message: 'not relevant to what is shown' },
  }
}

/**
 * Mounts a view against a stand-in for the engine.
 *
 * The API is the boundary being exercised: the view, its store and the real
 * catalogue of translations run as they do in the browser, and only the
 * network is replaced.
 *
 * Pass `null` as the body to leave the request unanswered, which is how a
 * load still in progress looks. Links are stood in for: where they lead is
 * checked, not the router.
 */
export async function mountView(
  view: Component,
  body: ApiResponse<unknown> | null,
  locale: 'en' | 'it' = 'en',
  props: Record<string, unknown> = {},
) {
  i18n.global.locale.value = locale

  vi.stubGlobal(
    'fetch',
    vi.fn(() =>
      body === null
        ? new Promise(() => {})
        : Promise.resolve({ json: () => Promise.resolve(body) }),
    ),
  )

  const wrapper = mount(view, {
    props,
    global: { plugins: [createPinia(), i18n], stubs: { RouterLink: RouterLinkStub } },
  })

  await flushPromises()

  return wrapper
}

/** How the stand-in engine answers one request. */
export interface Reply {
  status?: number
  body?: unknown
  headers?: Record<string, string>
}

/** A request the stand-in engine received. */
export interface Received {
  method: string
  path: string
  body: unknown
}

/**
 * Stands in for the engine, answering by method and path.
 *
 * A route may answer `'unreachable'`, which is what a stopped engine looks
 * like to the browser: the request fails without any answer. A route may also
 * be a function, to answer differently the second time. A request nobody
 * planned for fails the test rather than getting an answer made up for it.
 */
type Planned = Reply | ApiResponse<unknown> | 'unreachable'

export function stubEngine(routes: Record<string, Planned | ((request: Received) => Planned)>) {
  const received: Received[] = []

  vi.stubGlobal(
    'fetch',
    vi.fn((url: string, init?: RequestInit) => {
      const method = init?.method ?? 'GET'
      const path = url.replace('/api/v1/', '')
      const request = { method, path, body: init?.body ? JSON.parse(String(init.body)) : undefined }

      received.push(request)

      const route = routes[`${method} ${path}`]
      const planned = typeof route === 'function' ? route(request) : route

      // An answer of the engine on its own stands for a 200 carrying it.
      const reply: Reply | 'unreachable' | undefined =
        typeof planned === 'object' && 'success' in planned ? { body: planned } : planned

      if (reply === undefined) {
        return Promise.reject(new Error(`No answer planned for ${method} ${path}`))
      }

      if (reply === 'unreachable') {
        return Promise.reject(new TypeError('Failed to fetch'))
      }

      return Promise.resolve({
        status: reply.status ?? 200,
        headers: new Headers(reply.headers ?? {}),
        json: () =>
          reply.body === undefined ? Promise.reject(new SyntaxError('Unexpected token <')) : Promise.resolve(reply.body),
      })
    }),
  )

  return received
}

/** The engine's refusal, as it would send it. */
export function refused(code: string, extra: { reason?: string; message?: string } = {}): unknown {
  return {
    success: false,
    apiVersion: 'v1',
    timestamp: '2026-09-01T12:00:00Z',
    data: null,
    error: { code, message: extra.message ?? 'The engine speaks English to its logs.', ...(extra.reason ? { reason: extra.reason } : {}) },
  }
}

/** A session, as `auth/session` describes it. */
export function sessionOf(username: string, role: 'Administrator' | 'Viewer' = 'Administrator', passwordChangeRequired = false) {
  return answer({ username, role, passwordChangeRequired, expiresAt: '2026-09-01T20:00:00Z' })
}
