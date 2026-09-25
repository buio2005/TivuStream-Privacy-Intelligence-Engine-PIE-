import { createRouter, createWebHistory } from 'vue-router'
import DashboardView from '@/views/DashboardView.vue'
import { useSessionStore } from '@/stores/session'

export const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: [
    {
      path: '/',
      name: 'dashboard',
      component: DashboardView,
    },
    {
      path: '/domains',
      name: 'domains',
      component: () => import('@/views/DomainsView.vue'),
    },
    {
      path: '/password',
      name: 'password',
      component: () => import('@/views/PasswordView.vue'),
    },
    {
      path: '/accounts',
      name: 'accounts',
      component: () => import('@/views/AccountsView.vue'),
      meta: { administratorOnly: true },
    },
  ],
})

// The engine refuses anyway. Not showing the screen spares a page of refusals
// to someone who could never have used it. While the session is still being
// checked the role is not known, and the engine's refusal is left to speak.
router.beforeEach((to) => {
  const session = useSessionStore()

  if (to.meta.administratorOnly && session.state === 'Authenticated' && !session.isAdministrator) {
    return { name: 'dashboard' }
  }
})

export default router
