import { useState } from 'react'
import {
  Badge,
  Button,
  Dialog,
  EmptyState,
  Input,
  Select,
  SegmentedControl,
  Skeleton,
  Table,
  useToast,
} from './components/ui'

type Mode = 'fcl' | 'lcl' | 'air'

interface RateRow {
  id: number
  route: string
  carrier: string
  price: string
}

const sampleRows: RateRow[] = [
  { id: 1, route: 'BKK → SIN', carrier: 'Maersk', price: '$200.00' },
  { id: 2, route: 'BKK → HKG', carrier: 'Evergreen', price: '$180.00' },
]

/** Dev-only showcase of every T11 primitive against real tokens, light + dark — not a real
 * app page (those are T12-T14). Verified manually via the browser preview; kept in the tree so
 * future pages have a live reference instead of only style-guide.html's static markup. */
export function ComponentGallery() {
  const [mode, setMode] = useState<Mode>('fcl')
  const [dialogOpen, setDialogOpen] = useState(false)
  const [showEmptyTable, setShowEmptyTable] = useState(false)
  const toast = useToast()

  return (
    <section className="flex flex-col gap-8 p-8 w-full max-w-3xl mx-auto">
      <div>
        <h2 className="font-heading text-lg font-semibold mb-3">Buttons</h2>
        <div className="flex flex-wrap gap-3 items-center">
          <Button>Default</Button>
          <Button disabled>Disabled</Button>
          <Button variant="secondary">Secondary</Button>
          <Button variant="outline">Outline</Button>
          <Button variant="outline-destructive">Reject</Button>
          <Button variant="success">Approve &amp; send</Button>
        </div>
      </div>

      <div>
        <h2 className="font-heading text-lg font-semibold mb-3">Badges</h2>
        <div className="flex flex-wrap gap-2">
          <Badge variant="import">Import</Badge>
          <Badge variant="export">Export</Badge>
          <Badge variant="fcl">FCL</Badge>
          <Badge variant="lcl">LCL</Badge>
          <Badge variant="air">Air</Badge>
          <Badge variant="warning">Pending Sale Approval</Badge>
          <Badge variant="success">Approved &amp; Sent</Badge>
          <Badge variant="destructive">Rejected</Badge>
          <Badge variant="status-in-transit">In Transit</Badge>
          <Badge variant="status-pending-do">Pending D/O</Badge>
        </div>
      </div>

      <div>
        <h2 className="font-heading text-lg font-semibold mb-3">Segmented control</h2>
        <SegmentedControl
          label="Transport mode"
          value={mode}
          onChange={setMode}
          options={[
            { value: 'fcl', label: 'FCL' },
            { value: 'lcl', label: 'LCL' },
            { value: 'air', label: 'Air' },
          ]}
        />
        <p className="text-sm text-muted-foreground mt-2">Selected: {mode.toUpperCase()}</p>
      </div>

      <div>
        <h2 className="font-heading text-lg font-semibold mb-3">Form fields</h2>
        <div className="grid grid-cols-2 gap-4 max-w-xl">
          <Input label="Customer name" placeholder="Somchai Exports" required />
          <Input label="Email" type="email" error="Enter a valid email address" defaultValue="not-an-email" />
          <Select
            label="Incoterm"
            placeholder="Select an Incoterm"
            options={[
              { value: 'CIF', label: 'CIF' },
              { value: 'FOB', label: 'FOB' },
              { value: 'EXW', label: 'EXW' },
            ]}
          />
          <Input label="Ready date" type="date" help="Rates are matched by validity window" />
        </div>
      </div>

      <div>
        <h2 className="font-heading text-lg font-semibold mb-3">Table</h2>
        <div className="flex items-center gap-3 mb-2">
          <Button variant="outline" onClick={() => setShowEmptyTable((v) => !v)}>
            Toggle empty state
          </Button>
        </div>
        <Table
          columns={[
            { key: 'route', header: 'Route', render: (r: RateRow) => r.route },
            { key: 'carrier', header: 'Carrier', render: (r: RateRow) => r.carrier },
            { key: 'price', header: 'Price', align: 'right', render: (r: RateRow) => r.price },
          ]}
          rows={showEmptyTable ? [] : sampleRows}
          rowKey={(r) => r.id}
          emptyTitle="No rates found"
          emptyDescription="Try a different route or add one from Operation's rate management."
        />
      </div>

      <div>
        <h2 className="font-heading text-lg font-semibold mb-3">Dialog &amp; Toast</h2>
        <div className="flex gap-3">
          <Button onClick={() => setDialogOpen(true)}>Open dialog</Button>
          <Button variant="outline" onClick={() => toast.show('Rate saved successfully', 'success')}>
            Show success toast
          </Button>
          <Button variant="outline" onClick={() => toast.show('Failed to save rate', 'destructive')}>
            Show error toast
          </Button>
        </div>
        <Dialog open={dialogOpen} onClose={() => setDialogOpen(false)} title="Reject quotation">
          <p className="text-sm text-muted-foreground mb-4">This action cannot be undone.</p>
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setDialogOpen(false)}>
              Cancel
            </Button>
            <Button
              variant="outline-destructive"
              onClick={() => {
                setDialogOpen(false)
                toast.show('Quotation rejected', 'destructive')
              }}
            >
              Confirm reject
            </Button>
          </div>
        </Dialog>
      </div>

      <div>
        <h2 className="font-heading text-lg font-semibold mb-3">Skeleton</h2>
        <div className="flex flex-col gap-2 max-w-sm">
          <Skeleton height="1.25rem" width="60%" />
          <Skeleton height="1.25rem" />
          <Skeleton height="1.25rem" width="80%" />
        </div>
      </div>

      <div>
        <h2 className="font-heading text-lg font-semibold mb-3">Empty state</h2>
        <EmptyState
          title="No quotations yet"
          description="Guest submissions will show up here once they're queued for approval."
          action={<Button variant="outline">Refresh</Button>}
        />
      </div>
    </section>
  )
}
