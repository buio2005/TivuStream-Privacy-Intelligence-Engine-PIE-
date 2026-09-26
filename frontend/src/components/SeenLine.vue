<script setup lang="ts">
import { computed } from 'vue'
import { useI18n } from 'vue-i18n'
import type { MeasurementQuality } from '@/api/types'

const props = defineProps<{
  firstSeen: string
  lastSeen: string
  quality: MeasurementQuality
}>()

const { d } = useI18n()

const hour = 60 * 60 * 1000

/**
 * Bounded to the hour: the source reports by hour, so what is known is the
 * hour, and the hour is shown whole. The first instant starts the first hour
 * and the last one ends the last hour; read as instants, an hour seen once
 * would look like two sightings an hour apart.
 */
const hourly = computed(() => props.quality === 'PeriodBounded')

const first = computed(() => new Date(props.firstSeen))
const last = computed(() => new Date(props.lastSeen))

const once = computed(() =>
  hourly.value
    ? last.value.getTime() - first.value.getTime() <= hour
    : first.value.getTime() === last.value.getTime(),
)

/** "26 Sept, 13:00–14:00" for an hour, "26 Sept, 13:07" for an instant. */
function when(start: Date): string {
  return hourly.value
    ? `${d(start, 'stamp')}–${d(new Date(start.getTime() + hour), 'short')}`
    : d(start, 'stamp')
}

const firstText = computed(() => when(first.value))
const lastText = computed(() => when(hourly.value ? new Date(last.value.getTime() - hour) : last.value))
</script>

<template>
  <!--
    Two facts, not a span. Saying "seen between ten and nine" would suggest a
    presence throughout, while the domain may have been contacted in two of
    those hours and in none of the others.
  -->
  <span class="seen">
    <template v-if="once">{{ $t('domains.seenOnce', { when: firstText }) }}</template>
    <template v-else>{{ $t('domains.seen', { first: firstText, last: lastText }) }}</template>
  </span>
</template>
