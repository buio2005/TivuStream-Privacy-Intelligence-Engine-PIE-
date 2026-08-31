<script setup lang="ts">
import { useI18n } from 'vue-i18n'
import { supportedLocales } from '@/i18n'

const { locale } = useI18n()

/** Remembers the choice, so the interface does not forget it on reload. */
function choose(next: string) {
  locale.value = next
  localStorage.setItem('locale', next)
}
</script>

<template>
  <header>
    <span class="name">{{ $t('app.name') }}</span>

    <nav aria-label="Language">
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

  <main>
    <RouterView />
  </main>
</template>

<style scoped>
header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 1rem 0;
  border-bottom: 1px solid rgba(128, 128, 128, 0.3);
}

.name {
  font-weight: 600;
}

button {
  background: none;
  border: 1px solid rgba(128, 128, 128, 0.5);
  border-radius: 4px;
  padding: 0.2rem 0.6rem;
  margin-left: 0.3rem;
  cursor: pointer;
  color: inherit;
}

button.active {
  border-color: currentColor;
  font-weight: 600;
}
</style>
