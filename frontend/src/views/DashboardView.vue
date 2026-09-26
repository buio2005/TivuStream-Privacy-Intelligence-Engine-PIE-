<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useScoreStore } from '@/stores/score'
import FactorList from '@/components/FactorList.vue'
import PeriodLine from '@/components/PeriodLine.vue'
import TermNote from '@/components/TermNote.vue'

/**
 * Factors whose value is a lower bound, as the NPSS Specification states.
 *
 * The explanation is offered only when something on the page actually rests
 * on it. Shown always it would become furniture nobody reads.
 */
const lowerBoundCodes = new Set([
  'TrackingExposureNone',
  'TrackingExposureMeasured',
  'ThreatExposureNone',
  'ThreatExposureSuspiciousOnly',
  'ThreatExposureMeasured',
])

const hasUnmeasurable = computed(() =>
  score.value?.breakdown.some((area) => area.state !== 'Measured') ?? false,
)

const hasLowerBound = computed(() =>
  score.value?.breakdown.some((area) =>
    area.factors.some((factor) => lowerBoundCodes.has(factor.code)),
  ) ?? false,
)

const store = useScoreStore()
const { score, period, periodsObserved, periodsRequested, loading, failure } = storeToRefs(store)

onMounted(store.load)
</script>

<template>
  <section>
    <h1>{{ $t('dashboard.title') }}</h1>

    <p v-if="loading">{{ $t('dashboard.loading') }}</p>

    <p v-else-if="failure || !score">
      <strong>{{ $t('dashboard.unavailable') }}</strong><br />
      {{ $t('dashboard.unavailableReason') }}
    </p>

    <template v-else>
      <!--
        A withheld score is not a failure and not a zero. The interface says
        the system declined to judge, and shows what it did measure.
      -->
      <p v-if="score.overallScore === null" class="withheld">
        <strong>{{ $t('score.withheld') }}</strong><br />
        {{ $t('score.withheldReason', { coverage: $n(score.coverage) }) }}
      </p>

      <p v-else class="overall">
        <span class="value">{{ $n(score.overallScore) }}</span>
        <span class="scale">/ 100</span>
      </p>

      <p class="coverage">
        {{ $t('score.coverage') }}:
        {{ $t('score.coverageValue', { value: $n(score.coverage) }) }}
        &middot;
        {{ $t('score.algorithm', { version: score.algorithmVersion }) }}
      </p>

      <!--
        The score reads the last twenty-four hours, as the domains do, and
        says which hours it had. Without this line, a score that weighs one
        observed hour would look like a judgement on the whole day.
      -->
      <PeriodLine
        v-if="period"
        :period="period"
        :periods-observed="periodsObserved"
        :periods-requested="periodsRequested"
      />

      <nav class="terms">
        <TermNote term="coverage" />
        <TermNote v-if="hasUnmeasurable" term="notMeasurable" />
        <TermNote v-if="hasLowerBound" term="lowerBound" />
      </nav>

      <ul class="breakdown">
        <li v-for="area in score.breakdown" :key="area.component">
          <div class="heading">
            <span class="area">{{ $t(`component.${area.component}`) }}</span>
            <span class="state">{{ $t(`state.${area.state}`) }}</span>
            <span class="points">
              {{ $n(area.score) }} / {{ $n(area.maxScore) }}
              <em>{{ $t('score.weight', { weight: $n(area.weight) }) }}</em>
            </span>
          </div>

          <FactorList :factors="area.factors" />
        </li>
      </ul>
    </template>
  </section>
</template>

<style scoped>
.overall .value {
  font-size: 3rem;
  font-weight: 600;
}

.overall .scale,
.coverage {
  opacity: 0.7;
}

.terms {
  margin: 1.2rem 0 0.5rem;
}

.breakdown {
  list-style: none;
  padding: 0;
}

.breakdown li {
  padding: 0.8rem 0;
  border-bottom: 1px solid rgba(128, 128, 128, 0.3);
}

.heading {
  display: flex;
  gap: 1rem;
  align-items: baseline;
}

.points em {
  font-style: normal;
  opacity: 0.6;
  font-size: 0.85em;
  margin-left: 0.4rem;
}

.breakdown .area {
  flex: 1;
}

.breakdown .state {
  opacity: 0.7;
}
</style>
