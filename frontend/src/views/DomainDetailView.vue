<script setup lang="ts">
import { watch } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { useDomainDetailStore } from '@/stores/domainDetail'
import CategoryBadge from '@/components/CategoryBadge.vue'
import DomainNotes from '@/components/DomainNotes.vue'
import PeriodLine from '@/components/PeriodLine.vue'
import type { DeviceIdentification } from '@/api/types'

const props = defineProps<{ domain: string }>()

const store = useDomainDetailStore()
const { detail, loading, failure } = storeToRefs(store)

const { t, te, d } = useI18n()

// Changing domain without leaving the page reads the new one; the store drops
// the previous one first.
watch(() => props.domain, store.load, { immediate: true })

function moment(value: string): string {
  return d(new Date(value), 'stamp')
}

/**
 * A transport named by the catalogue. One the catalogue does not know is shown
 * as the source names it rather than dropped.
 */
function transport(protocol: string): string {
  if (protocol === '') {
    return t('domainDetail.transportUnknown')
  }

  return te(`transport.${protocol}`) ? t(`transport.${protocol}`) : protocol
}

/** The name when there is one, with the address beside it. */
function deviceName(device: DeviceIdentification): string {
  if (device.ipAddress === null) {
    return t('domainDetail.deviceUndescribed', { id: device.deviceId.slice(0, 8) })
  }

  return device.hostname ? `${device.hostname} (${device.ipAddress})` : device.ipAddress
}
</script>

<template>
  <section>
    <RouterLink class="back" :to="{ name: 'domains' }">{{ $t('domainDetail.back') }}</RouterLink>

    <h1 class="name">{{ domain }}</h1>

    <p v-if="loading">{{ $t('dashboard.loading') }}</p>

    <p v-else-if="failure === 'DomainNotObserved'" class="not-observed">
      {{ $t('domainDetail.notObserved') }}
    </p>

    <!-- A read that failed says nothing about the network. -->
    <p v-else-if="failure" class="unavailable">
      <strong>{{ $t('domainDetail.unavailable') }}</strong><br />
      {{ $t('domains.unavailableReason') }}
    </p>

    <template v-else-if="detail">
      <PeriodLine
        v-if="detail.period"
        :period="detail.period"
        :periods-observed="detail.periodsObserved"
        :periods-requested="detail.periodsRequested"
      />

      <div class="heading">
        <CategoryBadge :category="detail.domain.category" />
        <span class="count">{{ $t('domains.occurrences', detail.domain.occurrences) }}</span>
      </div>

      <DomainNotes :domain="detail.domain" />

      <!-- A statement about the system, not about the domain. -->
      <p class="note reputation">
        <template v-if="detail.domain.reputation">
          {{ $t('domainDetail.reputation', { value: detail.domain.reputation }) }}
        </template>
        <template v-else>{{ $t('domainDetail.reputationUnassessed') }}</template>
      </p>

      <h2>{{ $t('domainDetail.activityTitle') }}</h2>

      <!--
        Three ways of having no rows, and none of them is an empty list without
        an explanation, nor an error.
      -->
      <p v-if="detail.activityAccess === 'Withheld'" class="activity-note">
        {{ $t('domains.activityWithheld') }}
      </p>

      <p v-else-if="detail.activityAccess === 'Unavailable'" class="activity-note">
        {{ $t('domainDetail.activityUnavailable') }}
      </p>

      <p v-else-if="detail.activities.length === 0" class="activity-note">
        {{ $t('domainDetail.activityEmpty') }}
      </p>

      <!--
        No total per device: the total of the domain is stated above, and a sum
        per device would suggest the identity holds across addresses even where
        it rests on the address.
      -->
      <table v-else class="activities">
        <thead>
          <tr>
            <th>{{ $t('domainDetail.device') }}</th>
            <th>{{ $t('domainDetail.queries') }}</th>
            <th>{{ $t('domainDetail.outcome') }}</th>
            <th>{{ $t('domainDetail.transport') }}</th>
            <th>{{ $t('domainDetail.seen') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr
            v-for="activity in detail.activities"
            :key="`${activity.device.deviceId}-${activity.blocked}-${activity.protocol}`"
          >
            <td :data-label="$t('domainDetail.device')">
              <span class="device">{{ deviceName(activity.device) }}</span>

              <!--
                Attributing traffic to a device is the strongest statement the
                system makes. How solid it is shows rather than being implied.
              -->
              <small v-if="activity.device.ipAddress === null" class="identity">
                {{ $t('domainDetail.deviceUndescribedNote') }}
              </small>
              <small v-else-if="activity.device.identityBasis === 'NetworkAddress'" class="identity">
                {{ $t('domainDetail.identityNetworkAddress') }}
              </small>
            </td>
            <td :data-label="$t('domainDetail.queries')">{{ activity.queryCount }}</td>
            <td :data-label="$t('domainDetail.outcome')" class="outcome">
              {{ activity.blocked ? $t('domainDetail.blocked') : $t('domainDetail.notBlocked') }}
            </td>
            <td :data-label="$t('domainDetail.transport')" class="transport">
              {{ transport(activity.protocol) }}
            </td>
            <td :data-label="$t('domainDetail.seen')">
              <template v-if="activity.firstSeen === activity.lastSeen">
                {{ $t('domains.seenOnce', { from: moment(activity.firstSeen) }) }}
              </template>
              <template v-else>
                {{ $t('domains.seen', { from: moment(activity.firstSeen), to: moment(activity.lastSeen) }) }}
              </template>
            </td>
          </tr>
        </tbody>
      </table>
    </template>
  </section>
</template>

<style scoped>
.back {
  font-size: 0.9rem;
}

.name {
  font-family: ui-monospace, monospace;
  overflow-wrap: anywhere;
}

.heading {
  display: flex;
  gap: 1rem;
  align-items: baseline;
  margin-top: 0.5rem;
}

.count {
  opacity: 0.7;
  font-size: 0.9rem;
}

.note,
.activity-note {
  margin: 0.3rem 0 0;
  font-size: 0.9rem;
  opacity: 0.85;
}

h2 {
  margin-top: 1.5rem;
  font-size: 1.1rem;
}

.activities {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}

.activities th,
.activities td {
  text-align: left;
  padding: 0.5rem 0.6rem 0.5rem 0;
  border-bottom: 1px solid rgba(128, 128, 128, 0.3);
  vertical-align: top;
}

.device {
  font-family: ui-monospace, monospace;
}

.identity {
  display: block;
  opacity: 0.75;
  margin-top: 0.2rem;
}

/* On a narrow screen each activity becomes a block, labelled field by field. */
@media (max-width: 640px) {
  .activities thead {
    display: none;
  }

  .activities tr,
  .activities td {
    display: block;
  }

  .activities tr {
    padding: 0.6rem 0;
    border-bottom: 1px solid rgba(128, 128, 128, 0.3);
  }

  .activities td {
    border: none;
    padding: 0.15rem 0;
  }

  .activities td::before {
    content: attr(data-label) ': ';
    opacity: 0.7;
  }
}
</style>
