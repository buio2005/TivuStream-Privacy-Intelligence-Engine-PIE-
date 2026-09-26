<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { ObservationPeriod } from '@/api/types'

const props = defineProps<{
  period: ObservationPeriod
  periodsObserved: number
  periodsRequested: number
}>()

const { d } = useI18n()

/** The window includes the current hour, which has not elapsed. */
const inProgress = computed(() => new Date(props.period.end) > new Date())

/**
 * Hours inside the interval that were not observed. Without a word about
 * them, "3 hours observed" under "from 13:00 to 18:00" reads as a mistake.
 */
const missing = computed(
  () => Math.round((new Date(props.period.end).getTime() - new Date(props.period.start).getTime()) / 3_600_000) > props.periodsObserved,
)

function moment(value: string): string {
  return d(new Date(value), 'stamp')
}
</script>

<template>
  <p class="period">
    {{ $t('domains.observedPeriod', { from: moment(period.start), to: moment(period.end) }) }}
    &middot;
    <!--
      What was asked for and what exists are told apart. An installation
      running for six hours must not report a day.
    -->
    {{ $t('domains.hoursObserved', { observed: periodsObserved, requested: periodsRequested }, periodsObserved) }}
    <em v-if="inProgress">{{ $t('domains.inProgress') }}</em>
    <em v-if="missing" class="missing">{{ $t('domains.hoursMissing') }}</em>
  </p>
</template>

<style scoped>
.period {
  opacity: 0.85;
  font-size: 0.9rem;
}

.period em {
  font-style: normal;
  opacity: 0.75;
}

.period .missing {
  display: block;
}
</style>
