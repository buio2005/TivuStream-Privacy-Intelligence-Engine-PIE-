import { createRouter, createWebHistory, type RouteLocationNormalized } from 'vue-router'
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

/**
 * Whether the account signed in may be shown a screen.
 *
 * The engine refuses anyway. Not showing the screen spares a page of refusals
 * to someone who could never have used it. While the session is still being
 * checked the role is not known: the screen is checked again once it is.
 */
export function mayShow(route: RouteLocationNormalized): boolean {
  const session = useSessionStore()

  return !(route.meta.administratorOnly && session.state === 'Authenticated' && !session.isAdministrator)
}

router.beforeEach((to) => (mayShow(to) ? true : { name: 'dashboard' }))

export default router
