import { useEffect, useRef, useState } from 'react'
import Box from '@mui/material/Box'
import { Badge, Button, ConfirmDialog, EmptyState, Input, Select, Skeleton, Table, useToast } from '../../components/ui'
import { ApiError, api } from '../../lib/api'
import { LocalChargeFormDialog, type LocalChargeFormValues } from './LocalChargeFormDialog'
import type { CsvImportResult, Currency, LocalCharge, Port } from './types'
import { useCrud } from './useCrud'

function toPayload(values: LocalChargeFormValues) {
  return {
    portId: values.portId,
    direction: values.direction,
    mode: values.mode,
    chargeType: values.chargeType,
    calcBasis: values.calcBasis,
    amountMin: values.amountMin === '' ? 0 : Number(values.amountMin),
    amountMax: values.amountMax === '' ? 0 : Number(values.amountMax),
    minimumCharge: values.minimumCharge === '' ? null : Number(values.minimumCharge),
    currencyCode: values.currencyCode,
    chargeSide: values.chargeSide,
  }
}

/** ui-plan.md IA page 6 — same pattern as RateManagementPage (page 5), for LocalCharge. */
export function LocalChargeManagementPage() {
  const charges = useCrud<LocalCharge>('/api/local-charges')
  const [ports, setPorts] = useState<Port[]>([])
  const [currencies, setCurrencies] = useState<Currency[]>([])
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<LocalCharge | null>(null)
  const [deletingCharge, setDeletingCharge] = useState<LocalCharge | null>(null)
  const [search, setSearch] = useState('')
  const [modeFilter, setModeFilter] = useState<'all' | 'fcl' | 'lcl' | 'air'>('all')
  const [directionFilter, setDirectionFilter] = useState<'all' | 'export' | 'import'>('all')
  const [sideFilter, setSideFilter] = useState<'all' | 'origin' | 'destination'>('all')
  const [importResult, setImportResult] = useState<CsvImportResult | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const toast = useToast()

  useEffect(() => {
    Promise.all([api.get<Port[]>('/api/master/ports'), api.get<Currency[]>('/api/master/currencies')]).then(([p, cur]) => {
      setPorts(p)
      setCurrencies(cur)
    })
  }, [])

  const portName = (id: number) => ports.find((p) => p.id === id)?.code ?? String(id)

  const handleSave = async (values: LocalChargeFormValues) => {
    try {
      if (editing) {
        await charges.update(editing.id, toPayload(values))
      } else {
        await charges.create(toPayload(values))
      }
      toast.show('Local charge saved', 'success')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to save local charge', 'destructive')
      throw err
    }
  }

  const handleDelete = async () => {
    if (!deletingCharge) return
    try {
      await charges.remove(deletingCharge.id)
      toast.show('Local charge deleted', 'success')
      setDeletingCharge(null)
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to delete local charge', 'destructive')
    }
  }

  const handleImport = async (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    try {
      const res = await fetch('/api/local-charges/import', { method: 'POST', body: formData })
      const body = (await res.json()) as CsvImportResult
      setImportResult(body)
      if (res.ok) {
        toast.show(`Imported ${body.imported} charge(s)`, 'success')
        charges.reload()
      } else {
        toast.show('CSV import had errors — see details below', 'destructive')
      }
    } catch {
      toast.show('CSV import failed', 'destructive')
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const visibleCharges = charges.items.filter((c) => {
    if (modeFilter !== 'all' && c.mode !== modeFilter) return false
    if (directionFilter !== 'all' && c.direction !== directionFilter) return false
    if (sideFilter !== 'all' && c.chargeSide !== sideFilter) return false

    if (search.trim()) {
      const q = search.trim().toLowerCase()
      const port = ports.find((p) => p.id === c.portId)
      const matches =
        (port?.code && port.code.toLowerCase().includes(q)) ||
        (port?.name && port.name.toLowerCase().includes(q)) ||
        (port?.city && port.city.toLowerCase().includes(q)) ||
        c.chargeType.toLowerCase().includes(q) ||
        c.currencyCode.toLowerCase().includes(q) ||
        c.calcBasis.toLowerCase().includes(q) ||
        c.chargeSide.toLowerCase().includes(q) ||
        c.mode.toLowerCase().includes(q) ||
        c.direction.toLowerCase().includes(q)

      if (!matches) return false
    }

    return true
  })

  const hasFilterActive = Boolean(search || modeFilter !== 'all' || directionFilter !== 'all' || sideFilter !== 'all')

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 3, width: '100%', maxWidth: 1024 }}>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
        <h2 className="font-heading text-lg font-semibold">Local charge management</h2>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, flexWrap: 'wrap' }}>
          <input ref={fileInputRef} type="file" accept=".csv" className="hidden" onChange={(e) => e.target.files?.[0] && handleImport(e.target.files[0])} />
          <Button variant="outline" onClick={() => fileInputRef.current?.click()}>
            Import CSV
          </Button>
          <a className="btn btn-outline" href="/templates/local-charges-import-template.csv" download>
            Download template
          </a>
          <Button
            onClick={() => {
              setEditing(null)
              setDialogOpen(true)
            }}
          >
            New charge
          </Button>
        </Box>
      </Box>

      <Box className="card-sample" sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
        <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))', md: 'repeat(4, minmax(0, 1fr))' }, gap: 2 }}>
          <Input
            label="Search"
            placeholder="Port, charge type..."
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
          <Select
            label="Mode"
            options={[
              { value: 'all', label: 'All modes' },
              { value: 'fcl', label: 'FCL' },
              { value: 'lcl', label: 'LCL' },
              { value: 'air', label: 'Air' },
            ]}
            value={modeFilter}
            onChange={(e) => setModeFilter(e.target.value as 'all' | 'fcl' | 'lcl' | 'air')}
          />
          <Select
            label="Direction"
            options={[
              { value: 'all', label: 'All directions' },
              { value: 'export', label: 'Export' },
              { value: 'import', label: 'Import' },
            ]}
            value={directionFilter}
            onChange={(e) => setDirectionFilter(e.target.value as 'all' | 'export' | 'import')}
          />
          <Select
            label="Charge side"
            options={[
              { value: 'all', label: 'All sides' },
              { value: 'origin', label: 'Origin' },
              { value: 'destination', label: 'Destination' },
            ]}
            value={sideFilter}
            onChange={(e) => setSideFilter(e.target.value as 'all' | 'origin' | 'destination')}
          />
        </Box>
        <div className="flex justify-between items-center text-xs text-muted-foreground">
          <span>
            Showing {visibleCharges.length} of {charges.items.length} local charges
          </span>
          {hasFilterActive && (
            <button
              type="button"
              className="text-xs text-primary underline"
              onClick={() => {
                setSearch('')
                setModeFilter('all')
                setDirectionFilter('all')
                setSideFilter('all')
              }}
            >
              Reset filters
            </button>
          )}
        </div>
      </Box>

      {importResult && importResult.errors.length > 0 && (
        <div className="card-sample" style={{ width: 'auto' }}>
          <p className="text-sm font-semibold text-destructive mb-2">{importResult.errors.length} row(s) failed:</p>
          <ul className="text-sm text-muted-foreground list-disc pl-5">
            {importResult.errors.map((issue) => (
              <li key={issue.row}>
                Row {issue.row}: {issue.errors.join('; ')}
              </li>
            ))}
          </ul>
        </div>
      )}

      {charges.loading ? (
        <div className="flex flex-col gap-2">
          <Skeleton height="2rem" />
          <Skeleton height="2rem" />
        </div>
      ) : charges.error ? (
        <EmptyState title="Could not load local charges" description={charges.error} />
      ) : (
        <Table
          columns={[
            { key: 'port', header: 'Port', render: (c: LocalCharge) => portName(c.portId) },
            { key: 'side', header: 'Side', render: (c: LocalCharge) => c.chargeSide },
            { key: 'mode', header: 'Mode', render: (c: LocalCharge) => <Badge variant={c.mode}>{c.mode.toUpperCase()}</Badge> },
            { key: 'type', header: 'Charge type', render: (c: LocalCharge) => c.chargeType },
            { key: 'basis', header: 'Basis', render: (c: LocalCharge) => (c.calcBasis === 'notQuotable' ? <Badge variant="warning">Not quotable</Badge> : c.calcBasis) },
            {
              key: 'amount',
              header: 'Amount',
              align: 'right',
              render: (c: LocalCharge) => (c.calcBasis === 'notQuotable' ? '—' : `${c.amountMin === c.amountMax ? c.amountMin : `${c.amountMin}–${c.amountMax}`} ${c.currencyCode}`),
            },
            {
              key: 'actions',
              header: '',
              render: (c: LocalCharge) => (
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setEditing(c)
                      setDialogOpen(true)
                    }}
                  >
                    Edit
                  </Button>
                  <Button variant="outline-destructive" onClick={() => setDeletingCharge(c)}>
                    Delete
                  </Button>
                </div>
              ),
            },
          ]}
          rows={visibleCharges}
          rowKey={(c) => c.id}
          emptyTitle={hasFilterActive && charges.items.length > 0 ? 'No matching local charges' : 'No local charges yet'}
          emptyDescription={
            hasFilterActive && charges.items.length > 0
              ? 'Try adjusting or resetting your search and filter criteria.'
              : 'Add one manually or import a CSV file.'
          }
        />
      )}

      <LocalChargeFormDialog open={dialogOpen} onClose={() => setDialogOpen(false)} onSubmit={handleSave} ports={ports} currencies={currencies} editing={editing} />

      <ConfirmDialog
        open={Boolean(deletingCharge)}
        onClose={() => setDeletingCharge(null)}
        onConfirm={handleDelete}
        title="Delete local charge"
        description={
          deletingCharge ? (
            <p>
              Are you sure you want to delete the <strong>{deletingCharge.chargeType}</strong> charge at{' '}
              <strong>{portName(deletingCharge.portId)}</strong>?
            </p>
          ) : undefined
        }
        confirmLabel="Delete"
      />
    </Box>
  )
}
