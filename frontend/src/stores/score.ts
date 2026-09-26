import { defineStore } from 'pinia'
import { ref } from 'vue'
import { read } from '@/api/client'
import type { Npss, ObservationPeriod, ObservedScore } from '@/api/types'

/**
 * Holds the latest evaluation.
 *
 * The absence of a score is a state of its own, kept apart from an error and
 * from a load still running. Collapsing the three would let a refusal to judge
 * look like a failure.
 */
export const useScoreStore = defineStore('score', () => {
  const score = ref<Npss | null>(null)

  /** The window the score evaluated, told apart from what was asked for. */
  const period = ref<ObservationPeriod | null>(null)
  const periodsObserved = ref(0)
  const periodsRequested = ref(0)
  const loading = ref(false)
  const failure = ref<string | null>(null)

  async function load() {
    loading.value = true
    failure.value = null

    try {
      const observed = await read<ObservedScore>('npss')

      score.value = observed.score
      period.value = observed.period
      periodsObserved.value = observed.periodsObserved
      periodsRequested.value = observed.periodsRequested
    } catch (error) {
      score.value = null
      period.value = null
      periodsObserved.value = 0
      failure.value = error instanceof Error ? error.message : 'UnknownError'
    } finally {
      loading.value = false
    }
  }

  /** Forgets everything read. Called when the session ends. */
  function reset() {
    score.value = null
    period.value = null
    periodsObserved.value = 0
    periodsRequested.value = 0
    loading.value = false
    failure.value = null
  }

  return { score, period, periodsObserved, periodsRequested, loading, failure, load, reset }
})
