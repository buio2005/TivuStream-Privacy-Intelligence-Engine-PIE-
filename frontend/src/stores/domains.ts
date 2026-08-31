import { defineStore } from 'pinia'
import { ref } from 'vue'
import { read } from '@/api/client'
import type { Domain, ObservationPeriod, ObservedDomains } from '@/api/types'

/**
 * Holds the domains observed, together with the period they belong to.
 *
 * The period is kept because an empty list on its own says nothing: it cannot
 * be told apart from an hour that has only just begun.
 */
export const useDomainsStore = defineStore('domains', () => {
  const domains = ref<Domain[]>([])
  const period = ref<ObservationPeriod | null>(null)
  const periodsObserved = ref(0)
  const periodsRequested = ref(0)
  const loading = ref(false)
  const failure = ref<string | null>(null)

  async function load() {
    loading.value = true
    failure.value = null

    try {
      const observed = await read<ObservedDomains>('domains')

      domains.value = observed.domains
      period.value = observed.period
      periodsObserved.value = observed.periodsObserved
      periodsRequested.value = observed.periodsRequested
    } catch (error) {
      domains.value = []
      period.value = null
      periodsObserved.value = 0
      failure.value = error instanceof Error ? error.message : 'UnknownError'
    } finally {
      loading.value = false
    }
  }

  return { domains, period, periodsObserved, periodsRequested, loading, failure, load }
})
