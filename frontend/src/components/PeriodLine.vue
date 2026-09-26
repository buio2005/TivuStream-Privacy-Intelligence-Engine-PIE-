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
</style>
