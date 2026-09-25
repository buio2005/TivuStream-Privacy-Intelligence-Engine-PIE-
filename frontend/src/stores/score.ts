import { defineStore } from 'pinia'
import { ref } from 'vue'
import { read } from '@/api/client'
import type { Npss } from '@/api/types'

/**
 * Holds the latest evaluation.
 *
 * The absence of a score is a state of its own, kept apart from an error and
 * from a load still running. Collapsing the three would let a refusal to judge
 * look like a failure.
 */
export const useScoreStore = defineStore('score', () => {
  const score = ref<Npss | null>(null)
  const loading = ref(false)
  const failure = ref<string | null>(null)

  async function load() {
    loading.value = true
    failure.value = null

    try {
      score.value = await read<Npss>('npss')
    } catch (error) {
      score.value = null
      failure.value = error instanceof Error ? error.message : 'UnknownError'
    } finally {
      loading.value = false
    }
  }

  /** Forgets everything read. Called when the session ends. */
  function reset() {
    score.value = null
    loading.value = false
    failure.value = null
  }

  return { score, loading, failure, load, reset }
})
