<script setup lang="ts">
import { onMounted } from 'vue'
import { storeToRefs } from 'pinia'
import { useI18n } from 'vue-i18n'
import { supportedLocales } from '@/i18n'
import { useSessionStore } from '@/stores/session'
import LoginView from '@/views/LoginView.vue'
import PasswordView from '@/views/PasswordView.vue'
import SetupView from '@/views/SetupView.vue'

const { locale } = useI18n()

const session = useSessionStore()
const { state, account, unreachable, isAdministrator } = storeToRefs(session)

onMounted(session.check)

/** Remembers the choice, so the interface does not forget it on reload. */
function choose(next: string) {
  locale.value = next
  localStorage.setItem('locale', next)
}
</script>

<template>
  <header>
    <span class="name">{{ $t('app.name') }}</span>

    <nav v-if="state === 'Authenticated'" class="sections">
      <RouterLink to="/">{{ $t('nav.dashboard') }}</RouterLink>
      <RouterLink to="/domains">{{ $t('nav.domains') }}</RouterLink>
      <RouterLink v-if="isAdministrator" to="/accounts">{{ $t('nav.accounts') }}</RouterLink>
    </nav>
    <span v-else class="sections" />

    <div v-if="account" class="who">
      <RouterLink v-if="state === 'Authenticated'" to="/password">
        {{ $t('app.signedInAs', { name: account.username }) }}
      </RouterLink>
      <span v-else>{{ $t('app.signedInAs', { name: account.username }) }}</span>
      <button type="button" @click="session.logout">{{ $t('app.signOut') }}</button>
    </div>

    <nav :aria-label="$t('app.language')">
      <button
        v-for="option in supportedLocales"
        :key="option"
        type="button"
        :aria-pressed="locale === option"
        :class="{ active: locale === option }"
        @click="choose(option)"
      >
        {{ option.toUpperCase() }}
      </button>
    </nav>
  </header>

  <!--
    One state at a time, and nothing of the network is shown in any state but
    Authenticated: the data screens are not even mounted before.
  -->
  <main>
    <template v-if="state === 'Checking'">
      <template v-if="unreachable">
        <p role="alert">{{ $t('auth.engineUnreachable') }}</p>
        <button type="button" class="primary" @click="session.check">{{ $t('auth.retry') }}</button>
      </template>
      <p v-else>{{ $t('auth.checking') }}</p>
    </template>

    <SetupView v-else-if="state === 'SetupRequired'" />
    <LoginView v-else-if="state === 'Unauthenticated'" />
    <PasswordView v-else-if="state === 'PasswordChangeRequired'" />
    <RouterView v-else />
  </main>
</template>

<style scoped>
header {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  justify-content: space-between;
  gap: 0.8rem;
  padding: 1rem 0;
  border-bottom: 1px solid rgba(128, 128, 128, 0.3);
}

.name {
  font-weight: 600;
}

.sections {
  flex: 1;
  margin-left: 2rem;
  display: flex;
  flex-wrap: wrap;
  gap: 1.2rem;
}

.sections a,
.who a {
  color: inherit;
  text-decoration: none;
  opacity: 0.7;
}

.sections a.router-link-exact-active {
  opacity: 1;
  font-weight: 600;
}

.who {
  display: flex;
  align-items: center;
  gap: 0.6rem;
  font-size: 0.9rem;
}

button {
  background: none;
  border: 1px solid rgba(128, 128, 128, 0.5);
  border-radius: 4px;
  padding: 0.2rem 0.6rem;
  margin-left: 0.3rem;
  cursor: pointer;
  color: inherit;
  font: inherit;
}

button.active {
  border-color: currentColor;
  font-weight: 600;
}
</style>
