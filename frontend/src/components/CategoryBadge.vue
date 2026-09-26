<script setup lang="ts">
defineProps<{ category: string }>()
</script>

<!--
  The category is a label, never a verdict of safety. An unclassified domain
  carries no colour that could read as approval.
-->

<template>
  <span class="category" :class="{ unclassified: category === 'Unknown' }">
    {{ $t(`category.${category}`) }}
  </span>
</template>

<style scoped>
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
</style>
