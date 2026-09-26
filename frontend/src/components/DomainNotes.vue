<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import SeenLine from '@/components/SeenLine.vue'
import type { Domain } from '@/api/types'

defineProps<{ domain: Domain }>()

const { d } = useI18n()

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

  <p class="note observation">
    <SeenLine :first-seen="domain.firstSeen" :last-seen="domain.lastSeen" :quality="domain.observationQuality" />
  </p>
</template>

<style scoped>
.note {
  margin: 0.3rem 0 0;
  font-size: 0.9rem;
  opacity: 0.85;
}
</style>
