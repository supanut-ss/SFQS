import { useEffect } from 'react'
import Box from '@mui/material/Box'
import { Badge, Button, Skeleton } from '../../components/ui'
import { useAuth } from '../../lib/useAuth'
import { useRouter } from '../../lib/useRouter'
import { QuotationsPage } from '../sale/QuotationsPage'
import { LoginPage } from './LoginPage'
import { RateManagementPage } from './RateManagementPage'
import { LocalChargeManagementPage } from './LocalChargeManagementPage'
import { MasterDataPage } from './MasterDataPage'

type OpsTab = 'quotations' | 'rates' | 'local-charges' | 'master-data'

/** Internal (Sale/Operation/Admin) area — ui-plan.md IA pages 3-8 with full URL routing support. */
export function OpsApp() {
  const { user, loading, logout } = useAuth()
  const { path, navigate } = useRouter()

  const canSeeSale = user?.role === 'Sale' || user?.role === 'Admin'
  const canSeeOperation = user?.role === 'Operation' || user?.role === 'Admin'
  const canSeeAdmin = user?.role === 'Admin'

  const availableTabs = ([
    canSeeSale && 'quotations',
    canSeeOperation && 'rates',
    canSeeOperation && 'local-charges',
    canSeeAdmin && 'master-data',
  ] as const).filter((t): t is OpsTab => t !== false)

  let tabFromRoute: OpsTab = 'quotations'
  if (path.startsWith('/ops/rates')) tabFromRoute = 'rates'
  else if (path.startsWith('/ops/local-charges')) tabFromRoute = 'local-charges'
  else if (path.startsWith('/ops/master-data')) tabFromRoute = 'master-data'
  else if (path.startsWith('/ops/quotations') || path === '/ops') tabFromRoute = 'quotations'

  const activeTab = availableTabs.includes(tabFromRoute) ? tabFromRoute : availableTabs[0]

  useEffect(() => {
    if (user && activeTab && (path === '/ops' || !availableTabs.includes(tabFromRoute))) {
      navigate(`/ops/${activeTab}`)
    }
  }, [user, path, tabFromRoute, activeTab, availableTabs, navigate])

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

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, width: '100%', alignItems: 'center' }}>
      <Box sx={{ width: '100%', maxWidth: 1024, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, fontSize: '0.875rem' }}>
          <span>{user.email}</span>
          <Badge variant="info">{user.role}</Badge>
        </Box>
        <Button variant="outline" onClick={logout}>
          Sign out
        </Button>
      </Box>

      <Box component="nav" sx={{ width: '100%', maxWidth: 1024, display: 'flex', gap: 1, borderBottom: '1px solid', borderColor: 'divider', pb: 1 }}>
        {canSeeSale && <TabLink active={activeTab === 'quotations'} onClick={() => navigate('/ops/quotations')} label="Quotations" />}
        {canSeeOperation && (
          <>
            <TabLink active={activeTab === 'rates'} onClick={() => navigate('/ops/rates')} label="Rates" />
            <TabLink active={activeTab === 'local-charges'} onClick={() => navigate('/ops/local-charges')} label="Local charges" />
          </>
        )}
        {canSeeAdmin && <TabLink active={activeTab === 'master-data'} onClick={() => navigate('/ops/master-data')} label="Master data" />}
      </Box>

      {activeTab === 'quotations' && canSeeSale && <QuotationsPage />}
      {activeTab === 'rates' && canSeeOperation && <RateManagementPage />}
      {activeTab === 'local-charges' && canSeeOperation && <LocalChargeManagementPage />}
      {activeTab === 'master-data' && canSeeAdmin && <MasterDataPage />}
    </Box>
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
