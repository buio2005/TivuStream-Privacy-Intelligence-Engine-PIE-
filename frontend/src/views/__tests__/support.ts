import { mount, flushPromises } from '@vue/test-utils'
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
 * load still in progress looks.
 */
export async function mountView(
  view: Component,
  body: ApiResponse<unknown> | null,
  locale: 'en' | 'it' = 'en',
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

  const wrapper = mount(view, { global: { plugins: [createPinia(), i18n] } })

  await flushPromises()

  return wrapper
}
