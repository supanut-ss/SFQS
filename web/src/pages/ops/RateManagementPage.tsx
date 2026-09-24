import { useEffect, useRef, useState } from 'react'
import { Badge, Button, EmptyState, Select, Skeleton, Table, useToast } from '../../components/ui'
import { ApiError, api } from '../../lib/api'
import { RateFormDialog, type RateFormValues } from './RateFormDialog'
import type { Carrier, Currency, CsvImportResult, FreightRate, Port } from './types'
import { useCrud } from './useCrud'

function toPayload(values: RateFormValues) {
  return {
    originPortId: values.originPortId,
    destinationPortId: values.destinationPortId,
    mode: values.mode,
    direction: values.direction,
    carrierId: values.carrierId,
    containerSize: values.containerSize || null,
    weightBreakMin: values.weightBreakMin === '' ? null : Number(values.weightBreakMin),
    weightBreakMax: values.weightBreakMax === '' ? null : Number(values.weightBreakMax),
    priceMin: Number(values.priceMin),
    priceMax: Number(values.priceMax),
    currencyCode: values.currencyCode,
    validFrom: values.validFrom,
    validTo: values.validTo,
  }
}

/** ui-plan.md IA page 5: rate table + route filter + CRUD + CSV import + audit log link.
 * "Delete" here calls DELETE /api/rates/{id}, which RatesController implements as a soft
 * deactivate (IsActive=false) — never a hard delete, since past quotes may reference the rate
 * via QuotationLine.SourceRateId. */
export function RateManagementPage() {
  const rates = useCrud<FreightRate>('/api/rates')
  const [ports, setPorts] = useState<Port[]>([])
  const [carriers, setCarriers] = useState<Carrier[]>([])
  const [currencies, setCurrencies] = useState<Currency[]>([])
  const [dialogOpen, setDialogOpen] = useState(false)
  const [editing, setEditing] = useState<FreightRate | null>(null)
  const [activeOnly, setActiveOnly] = useState(true)
  const [importResult, setImportResult] = useState<CsvImportResult | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)
  const toast = useToast()

  useEffect(() => {
    Promise.all([api.get<Port[]>('/api/master/ports'), api.get<Carrier[]>('/api/master/carriers'), api.get<Currency[]>('/api/master/currencies')]).then(
      ([p, c, cur]) => {
        setPorts(p)
        setCarriers(c)
        setCurrencies(cur)
      },
    )
  }, [])

  const portName = (id: number) => ports.find((p) => p.id === id)?.code ?? String(id)
  const carrierName = (id: number) => carriers.find((c) => c.id === id)?.name ?? String(id)

  const handleSave = async (values: RateFormValues) => {
    try {
      if (editing) {
        await rates.update(editing.id, toPayload(values))
      } else {
        await rates.create(toPayload(values))
      }
      toast.show('Rate saved', 'success')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to save rate', 'destructive')
      throw err
    }
  }

  const handleDeactivate = async (rate: FreightRate) => {
    if (!confirm(`Deactivate the ${rate.mode.toUpperCase()} rate ${portName(rate.originPortId)} → ${portName(rate.destinationPortId)}?`)) return
    try {
      await rates.remove(rate.id)
      toast.show('Rate deactivated', 'success')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to deactivate rate', 'destructive')
    }
  }

  const handleImport = async (file: File) => {
    const formData = new FormData()
    formData.append('file', file)
    try {
      const res = await fetch('/api/rates/import', { method: 'POST', body: formData })
      const body = (await res.json()) as CsvImportResult
      setImportResult(body)
      if (res.ok) {
        toast.show(`Imported ${body.imported} rate(s)`, 'success')
        rates.reload()
      } else {
        toast.show('CSV import had errors — see details below', 'destructive')
      }
    } catch {
      toast.show('CSV import failed', 'destructive')
    } finally {
      if (fileInputRef.current) fileInputRef.current.value = ''
    }
  }

  const visibleRates = activeOnly ? rates.items.filter((r) => r.isActive) : rates.items

  return (
    <div className="flex flex-col gap-4 w-full max-w-4xl">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="font-heading text-lg font-semibold">Rate management</h2>
        <div className="flex items-center gap-3">
          <Select
            label="Filter"
            options={[
              { value: 'active', label: 'Active only' },
              { value: 'all', label: 'All' },
            ]}
            value={activeOnly ? 'active' : 'all'}
            onChange={(e) => setActiveOnly(e.target.value === 'active')}
          />
          <input
            ref={fileInputRef}
            type="file"
            accept=".csv"
            className="hidden"
            onChange={(e) => e.target.files?.[0] && handleImport(e.target.files[0])}
          />
          <Button variant="outline" onClick={() => fileInputRef.current?.click()}>
            Import CSV
          </Button>
          <a className="btn btn-outline" href="/templates/rates-import-template.csv" download>
            Download template
          </a>
          <Button
            onClick={() => {
              setEditing(null)
              setDialogOpen(true)
            }}
          >
            New rate
          </Button>
        </div>
      </div>

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

      {rates.loading ? (
        <div className="flex flex-col gap-2">
          <Skeleton height="2rem" />
          <Skeleton height="2rem" />
          <Skeleton height="2rem" />
        </div>
      ) : rates.error ? (
        <EmptyState title="Could not load rates" description={rates.error} />
      ) : (
        <Table
          columns={[
            { key: 'route', header: 'Route', render: (r: FreightRate) => `${portName(r.originPortId)} → ${portName(r.destinationPortId)}` },
            { key: 'mode', header: 'Mode', render: (r: FreightRate) => <Badge variant={r.mode}>{r.mode.toUpperCase()}</Badge> },
            { key: 'carrier', header: 'Carrier', render: (r: FreightRate) => carrierName(r.carrierId) },
            { key: 'slot', header: 'Slot', render: (r: FreightRate) => r.containerSize ?? (r.weightBreakMax ? `≤${r.weightBreakMax}kg` : '—') },
            {
              key: 'price',
              header: 'Price',
              align: 'right',
              render: (r: FreightRate) => `${r.priceMin === r.priceMax ? r.priceMin : `${r.priceMin}–${r.priceMax}`} ${r.currencyCode}`,
            },
            { key: 'validity', header: 'Valid', render: (r: FreightRate) => `${r.validFrom.slice(0, 10)} → ${r.validTo.slice(0, 10)}` },
            { key: 'status', header: 'Status', render: (r: FreightRate) => (r.isActive ? <Badge variant="success">Active</Badge> : <Badge variant="destructive">Inactive</Badge>) },
            {
              key: 'actions',
              header: '',
              render: (r: FreightRate) => (
                <div className="flex gap-2">
                  <Button
                    variant="outline"
                    onClick={() => {
                      setEditing(r)
                      setDialogOpen(true)
                    }}
                  >
                    Revise
                  </Button>
                  {r.isActive && (
                    <Button variant="outline-destructive" onClick={() => handleDeactivate(r)}>
                      Deactivate
                    </Button>
                  )}
                </div>
              ),
            },
          ]}
          rows={visibleRates}
          rowKey={(r) => r.id}
          emptyTitle="No rates yet"
          emptyDescription="Add one manually or import a CSV file."
        />
      )}

      <RateFormDialog
        open={dialogOpen}
        onClose={() => setDialogOpen(false)}
        onSubmit={handleSave}
        ports={ports}
        carriers={carriers}
        currencies={currencies}
        editing={editing}
      />
    </div>
  )
}
