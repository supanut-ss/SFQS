import { useState } from 'react'
import { Badge, Button, Skeleton } from '../../components/ui'
import { useAuth } from '../../lib/useAuth'
import { LoginPage } from './LoginPage'
import { RateManagementPage } from './RateManagementPage'
import { LocalChargeManagementPage } from './LocalChargeManagementPage'
import { MasterDataPage } from './MasterDataPage'

type OpsTab = 'rates' | 'local-charges' | 'master-data'

/** Internal (Sale/Operation/Admin) area — ui-plan.md IA pages 5-8. No router yet, so this is
 * its own small tab switcher gated by the logged-in user's role, same pattern as
 * InstantQuotePage's linear flow (T13). */
export function OpsApp() {
  const { user, loading, logout } = useAuth()
  const [tab, setTab] = useState<OpsTab>('rates')

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

  const canSeeOperation = user.role === 'Operation' || user.role === 'Admin'
  const canSeeAdmin = user.role === 'Admin'

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

      {(canSeeOperation || canSeeAdmin) && (
        <nav className="w-full max-w-4xl flex gap-2 border-b border-border pb-2">
          {canSeeOperation && (
            <>
              <TabLink active={tab === 'rates'} onClick={() => setTab('rates')} label="Rates" />
              <TabLink active={tab === 'local-charges'} onClick={() => setTab('local-charges')} label="Local charges" />
            </>
          )}
          {canSeeAdmin && <TabLink active={tab === 'master-data'} onClick={() => setTab('master-data')} label="Master data" />}
        </nav>
      )}

      {!canSeeOperation && !canSeeAdmin && (
        <p className="text-sm text-muted-foreground">This area is for Operation and Admin accounts. Sale's inbox is coming in a later release.</p>
      )}

      {canSeeOperation && tab === 'rates' && <RateManagementPage />}
      {canSeeOperation && tab === 'local-charges' && <LocalChargeManagementPage />}
      {canSeeAdmin && tab === 'master-data' && <MasterDataPage />}
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
