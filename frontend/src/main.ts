import { createApp } from 'vue'
import { createPinia } from 'pinia'
import { i18n } from '@/i18n'
import router from '@/router'
import App from '@/App.vue'
import '@/assets/main.css'

createApp(App).use(createPinia()).use(router).use(i18n).mount('#app')
