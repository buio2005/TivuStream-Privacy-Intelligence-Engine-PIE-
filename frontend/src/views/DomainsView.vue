<script setup lang="ts">
import { onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { useDomainsStore } from '@/stores/domains'
import CategoryBadge from '@/components/CategoryBadge.vue'
import DomainNotes from '@/components/DomainNotes.vue'
import PeriodLine from '@/components/PeriodLine.vue'
import TermNote from '@/components/TermNote.vue'

const store = useDomainsStore()
const { domains, period, periodsObserved, periodsRequested, loading, failure } =
  storeToRefs(store)

const { d } = useI18n()

onMounted(store.load)

function moment(value: string): string {
  return d(new Date(value), 'stamp')
}
</script>

<template>
  <section>
    <h1>{{ $t('domains.title') }}</h1>

    <p v-if="loading">{{ $t('dashboard.loading') }}</p>

    <!--
      A read that failed is not a network that contacted nothing, and the page
      says so instead of falling silent.
    -->
    <p v-else-if="failure">
      <strong>{{ $t('domains.unavailable') }}</strong><br />
      {{ $t('domains.unavailableReason') }}
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

    <PeriodLine
      v-if="period && domains.length > 0"
      :period="period"
      :periods-observed="periodsObserved"
      :periods-requested="periodsRequested"
    />

    <ul v-if="!loading && !failure && domains.length > 0" class="domains">
      <li v-for="domain in domains" :key="domain.name">
        <div class="heading">
          <RouterLink class="name" :to="{ name: 'domain', params: { domain: domain.name } }">
            {{ domain.name }}
          </RouterLink>

          <CategoryBadge :category="domain.category" />

          <span class="count">
            {{ $t('domains.occurrences', domain.occurrences) }}
          </span>
        </div>

        <DomainNotes :domain="domain" />
      </li>
    </ul>
  </section>
</template>

<style scoped>
.terms {
  margin: 1rem 0 0.5rem;
}

.empty {
  opacity: 0.85;
  font-size: 0.9rem;
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

.count {
  opacity: 0.7;
  font-size: 0.9rem;
}
</style>
