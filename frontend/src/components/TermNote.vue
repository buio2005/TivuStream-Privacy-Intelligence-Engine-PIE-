<script setup lang="ts">
defineProps<{ term: string }>()
</script>

<!--
  A term of ours with its plain explanation, available where the term appears.

  Most people using the tool are not technical, and the sentences the engine
  produces are exact rather than obvious. The explanation is added to the
  precise wording, never substituted for it: a simplified phrase that dropped
  the qualification would break the promise the phrase exists to keep.

  Repeating the same explanation on every row turns it into wallpaper that
  nobody reads. It is stated once, and stays reachable.
-->

<template>
  <details class="term">
    <summary>{{ $t(`term.${term}.label`) }}</summary>
    <p>{{ $t(`term.${term}.note`) }}</p>
  </details>
</template>

<style scoped>
.term {
  display: inline-block;
  margin-right: 1.2rem;
}

summary {
  cursor: pointer;
  font-size: 0.9rem;
  opacity: 0.8;
}

summary:hover {
  opacity: 1;
}

p {
  margin: 0.4rem 0 0;
  max-width: 44rem;
  font-size: 0.9rem;
  opacity: 0.85;
}
</style>
