<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import type { Domain } from '@/api/types'

defineProps<{ domain: Domain }>()

const { d } = useI18n()

function moment(value: string): string {
  return d(new Date(value), 'stamp')
}

function day(value: string): string {
  return d(new Date(value), 'day')
}
</script>

<!--
  What a domain's classification rests on, and when it was seen. The list and
  the detail say it with the same sentences.
-->

<template>
  <template v-if="domain.category !== 'Unknown'">
    <p v-if="domain.categoryConfidence" class="note">
      <strong>{{ $t(`domains.confidence.${domain.categoryConfidence}`) }}.</strong>
      {{ $t(`domains.confidenceNote.${domain.categoryConfidence}`) }}
    </p>

    <p v-if="domain.categorySource" class="note">
      <template v-if="domain.categorySourceUpdatedAt">
        {{ $t('domains.source', { list: domain.categorySource, date: day(domain.categorySourceUpdatedAt) }) }}
      </template>
      <template v-else>
        {{ $t('domains.sourceNever', { list: domain.categorySource }) }}
      </template>
    </p>
  </template>

  <!--
    Two facts, not a span. Saying "observed between ten and nine" would
    suggest a presence throughout, while the domain may have been contacted in
    two of those hours and in none of the others.

    The instants are bounded to the hour, so neither is a moment.
  -->
  <p class="note observation">
    <template v-if="domain.firstSeen === domain.lastSeen">
      {{ $t('domains.seenOnce', { from: moment(domain.firstSeen) }) }}
    </template>
    <template v-else>
      {{ $t('domains.seen', { from: moment(domain.firstSeen), to: moment(domain.lastSeen) }) }}
    </template>
  </p>
</template>

<style scoped>
.note {
  margin: 0.3rem 0 0;
  font-size: 0.9rem;
  opacity: 0.85;
}
</style>
