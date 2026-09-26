import { defineStore } from 'pinia'
import { ref } from 'vue'
import { call } from '@/api/client'
import type { DomainDetail } from '@/api/types'

/**
 * Holds the detail of one domain.
 *
 * Kept apart from the list: the two are read at different moments, and
 * forgetting one must not depend on the other.
 */
export const useDomainDetailStore = defineStore('domainDetail', () => {
  const detail = ref<DomainDetail | null>(null)
  const loading = ref(false)

  /** Code of the refusal, or `EngineUnreachable`. Null when the read succeeded. */
  const failure = ref<string | null>(null)

  // The domain last asked for. An answer about another one arrived late and
  // is dropped, so a slow reply never shows under the wrong name.
  let requested: string | null = null

  async function load(domain: string) {
    requested = domain

    // The previous domain is not left on screen while the next one is read.
    detail.value = null
    failure.value = null
    loading.value = true

    const outcome = await call<DomainDetail>('GET', `domains/${encodeURIComponent(domain)}`)

    if (requested !== domain) {
      return
    }

    if (outcome.ok && outcome.data !== null) {
      detail.value = outcome.data
    } else {
      failure.value = outcome.ok ? 'UnknownError' : outcome.code
    }

    loading.value = false
  }

  /** Forgets everything read. Called when the session ends. */
  function reset() {
    requested = null
    detail.value = null
    loading.value = false
    failure.value = null
  }

  return { detail, loading, failure, load, reset }
})
