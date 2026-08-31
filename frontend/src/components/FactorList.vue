<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ScoreFactor } from '@/api/types'

const props = defineProps<{ factors: ScoreFactor[] }>()

const { t, te, n } = useI18n()

/**
 * Renders a value in the language being read.
 *
 * A share arrives as a fraction and becomes a percentage; how that percentage
 * is written belongs to the language, which is why the engine never writes it.
 */
function render(name: string, value: unknown): string {
  if (typeof value === 'number') {
    return name === 'share' ? n(value, 'percent') : n(value)
  }

  if (Array.isArray(value)) {
    return value.join(', ')
  }

  return String(value)
}

const sentences = computed(() =>
  props.factors.map((factor) => {
    const values: Record<string, string> = {}

    for (const [name, value] of Object.entries(factor.values ?? {})) {
      values[name] = render(name, value)
    }

    // A code the catalogue does not know is shown with its identifier rather
    // than dropped. A factor that disappears would take away a reason for the
    // score without saying so.
    const key = `factor.${factor.code}`

    return te(key) ? t(key, values) : t('factor.unknown', { code: factor.code })
  }),
)
</script>

<template>
  <ul class="factors">
    <li v-for="(sentence, index) in sentences" :key="index">{{ sentence }}</li>
  </ul>
</template>

<style scoped>
.factors {
  margin: 0.4rem 0 0;
  padding-left: 1.1rem;
  font-size: 0.9rem;
  opacity: 0.85;
}

.factors li {
  padding: 0.15rem 0;
}
</style>
