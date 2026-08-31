<script setup lang="ts">
import { onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { useDomainsStore } from '@/stores/domains'
import TermNote from '@/components/TermNote.vue'
import type { Domain } from '@/api/types'

const store = useDomainsStore()
const { domains, period, loading, failure } = storeToRefs(store)

const { d } = useI18n()

onMounted(store.load)

/** A domain nobody recognised. Told apart from a domain found harmless. */
function isUnclassified(domain: Domain): boolean {
  return domain.category === 'Unknown'
}

function moment(value: string): string {
  return d(new Date(value), 'short')
}

function day(value: string): string {
  return d(new Date(value), 'day')
}
</script>

<template>
  <section>
    <h1>{{ $t('domains.title') }}</h1>

    <p v-if="loading">{{ $t('dashboard.loading') }}</p>

    <p v-else-if="failure">
      <strong>{{ $t('dashboard.unavailable') }}</strong>
    </p>

    <!--
      An empty list is reported together with the interval it is empty of.
      Without the period, "nothing observed" reads as a statement about the
      network when it is a statement about the moment of looking.
    -->
    <p v-else-if="domains.length === 0 && period" class="empty">
      {{ $t('domains.emptyInPeriod', { from: moment(period.start), to: moment(period.end) }) }}
      <br />
      <em>{{ $t('domains.hourly') }}</em>
    </p>

    <p v-else-if="domains.length === 0">{{ $t('domains.emptyEver') }}</p>

    <nav v-else class="terms">
      <TermNote term="unclassified" />
      <TermNote term="periodBounded" />
    </nav>

    <p v-if="period && domains.length > 0" class="period">
      {{ $t('domains.observedPeriod', { from: moment(period.start), to: moment(period.end) }) }}
    </p>

    <ul v-if="!loading && !failure && domains.length > 0" class="domains">
      <li v-for="domain in domains" :key="domain.name">
        <div class="heading">
          <span class="name">{{ domain.name }}</span>

          <!--
            The category is a label, never a verdict of safety. An unclassified
            domain carries no colour that could read as approval.
          -->
          <span class="category" :class="{ unclassified: isUnclassified(domain) }">
            {{ $t(`category.${domain.category}`) }}
          </span>

          <span class="count">
            {{ $t('domains.occurrences', domain.occurrences) }}
          </span>
        </div>

        <template v-if="!isUnclassified(domain)">
          <p v-if="domain.categoryConfidence" class="note">
            <strong>{{ $t(`domains.confidence.${domain.categoryConfidence}`) }}.</strong>
            {{ $t(`domains.confidenceNote.${domain.categoryConfidence}`) }}
          </p>

          <p v-if="domain.categorySource" class="note">
            <template v-if="domain.categorySourceUpdatedAt">
              {{
                $t('domains.source', {
                  list: domain.categorySource,
                  date: day(domain.categorySourceUpdatedAt),
                })
              }}
            </template>
            <template v-else>
              {{ $t('domains.sourceNever', { list: domain.categorySource }) }}
            </template>
          </p>
        </template>

        <!--
          The instants are bounded to the period, so they are shown as a span
          and never as the moment something happened.
        -->
        <p class="note observation">
          {{ $t('domains.period', { from: moment(domain.firstSeen), to: moment(domain.lastSeen) }) }}
        </p>
      </li>
    </ul>
  </section>
</template>

<style scoped>
.terms {
  margin: 1rem 0 0.5rem;
}

.period,
.empty {
  opacity: 0.85;
}

.empty em {
  font-style: normal;
  opacity: 0.75;
  font-size: 0.9rem;
}

.domains {
  list-style: none;
  padding: 0;
}

.domains li {
  padding: 0.9rem 0;
  border-bottom: 1px solid rgba(128, 128, 128, 0.3);
}

.heading {
  display: flex;
  gap: 1rem;
  align-items: baseline;
}

.name {
  flex: 1;
  font-family: ui-monospace, monospace;
}

.category {
  border: 1px solid currentColor;
  border-radius: 4px;
  padding: 0 0.4rem;
  font-size: 0.85rem;
}

.category.unclassified {
  opacity: 0.6;
  border-style: dashed;
}

.count {
  opacity: 0.7;
  font-size: 0.9rem;
}

.note {
  margin: 0.3rem 0 0;
  font-size: 0.9rem;
  opacity: 0.85;
}

.observation em {
  font-style: normal;
  opacity: 0.7;
}
</style>
