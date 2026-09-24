import { useState } from 'react'
import { Badge, Button, Skeleton } from '../../components/ui'
import { useAuth } from '../../lib/useAuth'
import { QuotationsPage } from '../sale/QuotationsPage'
import { LoginPage } from './LoginPage'
import { RateManagementPage } from './RateManagementPage'
import { LocalChargeManagementPage } from './LocalChargeManagementPage'
import { MasterDataPage } from './MasterDataPage'

type OpsTab = 'quotations' | 'rates' | 'local-charges' | 'master-data'

/** Internal (Sale/Operation/Admin) area — ui-plan.md IA pages 3-8. No router yet, so this is
 * its own small tab switcher gated by the logged-in user's role, same pattern as
 * InstantQuotePage's linear flow (T13). */
export function OpsApp() {
  const { user, loading, logout } = useAuth()
  const [tab, setTab] = useState<OpsTab>('quotations')

  if (loading) {
    return (
      <div className="quote-form flex flex-col gap-3">
        <Skeleton height="2rem" width="40%" />
        <Skeleton height="2.5rem" />
      </div>
    )
  }

  if (!user) {
    return <LoginPage />
  }

  const canSeeSale = user.role === 'Sale' || user.role === 'Admin'
  const canSeeOperation = user.role === 'Operation' || user.role === 'Admin'
  const canSeeAdmin = user.role === 'Admin'

  // A plain Operation account has no access to "quotations" (the default tab), which only
  // matters the first time they land here — fall back to the first tab their role can see.
  const availableTabs = ([
    canSeeSale && 'quotations',
    canSeeOperation && 'rates',
    canSeeOperation && 'local-charges',
    canSeeAdmin && 'master-data',
  ] as const).filter((t): t is OpsTab => t !== false)
  const activeTab = availableTabs.includes(tab) ? tab : availableTabs[0]

  return (
    <div className="flex flex-col gap-4 w-full items-center">
      <div className="w-full max-w-4xl flex items-center justify-between">
        <div className="flex items-center gap-2 text-sm">
          <span>{user.email}</span>
          <Badge variant="info">{user.role}</Badge>
        </div>
        <Button variant="outline" onClick={logout}>
          Sign out
        </Button>
      </div>

      <nav className="w-full max-w-4xl flex gap-2 border-b border-border pb-2">
        {canSeeSale && <TabLink active={activeTab === 'quotations'} onClick={() => setTab('quotations')} label="Quotations" />}
        {canSeeOperation && (
          <>
            <TabLink active={activeTab === 'rates'} onClick={() => setTab('rates')} label="Rates" />
            <TabLink active={activeTab === 'local-charges'} onClick={() => setTab('local-charges')} label="Local charges" />
          </>
        )}
        {canSeeAdmin && <TabLink active={activeTab === 'master-data'} onClick={() => setTab('master-data')} label="Master data" />}
      </nav>

      {activeTab === 'quotations' && canSeeSale && <QuotationsPage />}
      {activeTab === 'rates' && canSeeOperation && <RateManagementPage />}
      {activeTab === 'local-charges' && canSeeOperation && <LocalChargeManagementPage />}
      {activeTab === 'master-data' && canSeeAdmin && <MasterDataPage />}
    </div>
  )
}

function TabLink({ active, onClick, label }: { active: boolean; onClick: () => void; label: string }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="px-3 py-2 text-sm font-semibold"
      style={{
        color: active ? 'var(--color-primary)' : 'var(--color-muted-foreground)',
        borderBottom: active ? '2px solid var(--color-primary)' : '2px solid transparent',
      }}
    >
      {label}
    </button>
  )
}
