import { useState } from 'react'
import { Badge, Button, Dialog, EmptyState, Input, Select, SegmentedControl, Skeleton, Table, useToast } from '../../components/ui'
import { ApiError } from '../../lib/api'
import type { Carrier, Currency, ExchangeRate, OpsUser, Port, UserRole } from './types'
import { useCrud } from './useCrud'

type Tab = 'ports' | 'carriers' | 'currencies' | 'users'

/** ui-plan.md IA page 7 (Admin only — every mutation here requires the Admin role server-side,
 * MasterDataController). Four tabs instead of four separate pages since there's no router yet
 * and each resource's CRUD is small (2-5 fields). */
export function MasterDataPage() {
  const [tab, setTab] = useState<Tab>('ports')

  return (
    <div className="flex flex-col gap-4 w-full max-w-4xl">
      <h2 className="font-heading text-lg font-semibold">Master data</h2>
      <SegmentedControl
        label="Master data section"
        value={tab}
        onChange={setTab}
        options={[
          { value: 'ports', label: 'Ports' },
          { value: 'carriers', label: 'Carriers' },
          { value: 'currencies', label: 'Currencies & FX' },
          { value: 'users', label: 'Users' },
        ]}
      />
      {tab === 'ports' && <PortsTab />}
      {tab === 'carriers' && <CarriersTab />}
      {tab === 'currencies' && <CurrenciesTab />}
      {tab === 'users' && <UsersTab />}
    </div>
  )
}

function PortsTab() {
  const ports = useCrud<Port>('/api/master/ports', false)
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Port | null>(null)
  const toast = useToast()

  const [form, setForm] = useState({ code: '', name: '', city: '', country: '', type: 'sea' as Port['type'] })

  const openNew = () => {
    setEditing(null)
    setForm({ code: '', name: '', city: '', country: '', type: 'sea' })
    setOpen(true)
  }

  const openEdit = (port: Port) => {
    setEditing(port)
    setForm({ code: port.code, name: port.name, city: port.city, country: port.country, type: port.type })
    setOpen(true)
  }

  const save = async () => {
    try {
      if (editing) await ports.update(editing.id, form)
      else await ports.create(form)
      toast.show('Port saved', 'success')
      setOpen(false)
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to save port', 'destructive')
    }
  }

  const remove = async (port: Port) => {
    if (!confirm(`Delete port ${port.name}?`)) return
    try {
      await ports.remove(port.id)
      toast.show('Port deleted', 'success')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to delete port', 'destructive')
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex justify-end">
        <Button onClick={openNew}>New port</Button>
      </div>
      {ports.loading ? (
        <Skeleton height="2rem" />
      ) : ports.error ? (
        <EmptyState title="Could not load ports" description={ports.error} />
      ) : (
        <Table
          columns={[
            { key: 'code', header: 'Code', render: (p: Port) => p.code },
            { key: 'name', header: 'Name', render: (p: Port) => p.name },
            { key: 'city', header: 'City', render: (p: Port) => `${p.city}, ${p.country}` },
            { key: 'type', header: 'Type', render: (p: Port) => <Badge variant="info">{p.type === 'air' ? 'Air' : 'Sea'}</Badge> },
            {
              key: 'actions',
              header: '',
              render: (p: Port) => (
                <div className="flex gap-2">
                  <Button variant="outline" onClick={() => openEdit(p)}>Edit</Button>
                  <Button variant="outline-destructive" onClick={() => remove(p)}>Delete</Button>
                </div>
              ),
            },
          ]}
          rows={ports.items}
          rowKey={(p) => p.id}
          emptyTitle="No ports yet"
        />
      )}
      <Dialog open={open} onClose={() => setOpen(false)} title={editing ? 'Edit port' : 'New port'}>
        <div className="flex flex-col gap-4">
          <Input label="Code (UN/LOCODE)" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} />
          <Input label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          <div className="form-row">
            <Input label="City" value={form.city} onChange={(e) => setForm({ ...form, city: e.target.value })} />
            <Input label="Country" value={form.country} onChange={(e) => setForm({ ...form, country: e.target.value })} />
          </div>
          <Select
            label="Type"
            options={[{ value: 'sea', label: 'Sea' }, { value: 'air', label: 'Air' }]}
            value={form.type}
            onChange={(e) => setForm({ ...form, type: e.target.value as Port['type'] })}
          />
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
            <Button onClick={save}>Save</Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

function CarriersTab() {
  const carriers = useCrud<Carrier>('/api/master/carriers', false)
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<Carrier | null>(null)
  const [form, setForm] = useState({ code: '', name: '', type: 'shippingLine' as Carrier['type'] })
  const toast = useToast()

  const openNew = () => {
    setEditing(null)
    setForm({ code: '', name: '', type: 'shippingLine' })
    setOpen(true)
  }
  const openEdit = (carrier: Carrier) => {
    setEditing(carrier)
    setForm({ code: carrier.code, name: carrier.name, type: carrier.type })
    setOpen(true)
  }
  const save = async () => {
    try {
      if (editing) await carriers.update(editing.id, form)
      else await carriers.create(form)
      toast.show('Carrier saved', 'success')
      setOpen(false)
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to save carrier', 'destructive')
    }
  }
  const remove = async (carrier: Carrier) => {
    if (!confirm(`Delete carrier ${carrier.name}?`)) return
    try {
      await carriers.remove(carrier.id)
      toast.show('Carrier deleted', 'success')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to delete carrier', 'destructive')
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex justify-end">
        <Button onClick={openNew}>New carrier</Button>
      </div>
      {carriers.loading ? (
        <Skeleton height="2rem" />
      ) : carriers.error ? (
        <EmptyState title="Could not load carriers" description={carriers.error} />
      ) : (
        <Table
          columns={[
            { key: 'code', header: 'Code', render: (c: Carrier) => c.code },
            { key: 'name', header: 'Name', render: (c: Carrier) => c.name },
            { key: 'type', header: 'Type', render: (c: Carrier) => <Badge variant="info">{c.type === 'airline' ? 'Airline' : 'Shipping line'}</Badge> },
            {
              key: 'actions',
              header: '',
              render: (c: Carrier) => (
                <div className="flex gap-2">
                  <Button variant="outline" onClick={() => openEdit(c)}>Edit</Button>
                  <Button variant="outline-destructive" onClick={() => remove(c)}>Delete</Button>
                </div>
              ),
            },
          ]}
          rows={carriers.items}
          rowKey={(c) => c.id}
          emptyTitle="No carriers yet"
        />
      )}
      <Dialog open={open} onClose={() => setOpen(false)} title={editing ? 'Edit carrier' : 'New carrier'}>
        <div className="flex flex-col gap-4">
          <Input label="Code" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} />
          <Input label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          <Select
            label="Type"
            options={[{ value: 'shippingLine', label: 'Shipping line' }, { value: 'airline', label: 'Airline' }]}
            value={form.type}
            onChange={(e) => setForm({ ...form, type: e.target.value as Carrier['type'] })}
          />
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
            <Button onClick={save}>Save</Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

function CurrenciesTab() {
  const currencies = useCrud<Currency>('/api/master/currencies', false)
  const rates = useCrud<ExchangeRate>('/api/master/exchange-rates', false)
  const [open, setOpen] = useState(false)
  const [rateOpen, setRateOpen] = useState(false)
  const [form, setForm] = useState({ code: '', name: '', decimalDigits: 2 })
  const [rateForm, setRateForm] = useState({ currencyCode: '', rateToBase: '', effectiveDate: '' })
  const toast = useToast()

  const save = async () => {
    try {
      await currencies.create(form)
      toast.show('Currency added', 'success')
      setOpen(false)
      setForm({ code: '', name: '', decimalDigits: 2 })
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to add currency', 'destructive')
    }
  }

  const saveRate = async () => {
    try {
      await rates.create({ ...rateForm, rateToBase: Number(rateForm.rateToBase) })
      toast.show('Exchange rate added', 'success')
      setRateOpen(false)
      setRateForm({ currencyCode: '', rateToBase: '', effectiveDate: '' })
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to add exchange rate', 'destructive')
    }
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-col gap-3">
        <div className="flex justify-between items-center">
          <h3 className="font-semibold text-sm">Currencies</h3>
          <Button onClick={() => setOpen(true)}>New currency</Button>
        </div>
        {currencies.loading ? (
          <Skeleton height="2rem" />
        ) : (
          <Table
            columns={[
              { key: 'code', header: 'Code', render: (c: Currency) => c.code },
              { key: 'name', header: 'Name', render: (c: Currency) => c.name },
              { key: 'decimals', header: 'Decimals', align: 'right', render: (c: Currency) => c.decimalDigits },
            ]}
            rows={currencies.items}
            rowKey={(c) => c.code}
            emptyTitle="No currencies yet"
          />
        )}
      </div>

      <div className="flex flex-col gap-3">
        <div className="flex justify-between items-center">
          <h3 className="font-semibold text-sm">Exchange rates (history — never overwritten)</h3>
          <Button onClick={() => setRateOpen(true)}>New rate</Button>
        </div>
        {rates.loading ? (
          <Skeleton height="2rem" />
        ) : (
          <Table
            columns={[
              { key: 'currency', header: 'Currency', render: (r: ExchangeRate) => r.currencyCode },
              { key: 'rate', header: 'Rate to USD', align: 'right', render: (r: ExchangeRate) => r.rateToBase },
              { key: 'date', header: 'Effective date', render: (r: ExchangeRate) => r.effectiveDate.slice(0, 10) },
            ]}
            rows={rates.items}
            rowKey={(r) => r.id}
            emptyTitle="No exchange rates yet"
          />
        )}
      </div>

      <Dialog open={open} onClose={() => setOpen(false)} title="New currency">
        <div className="flex flex-col gap-4">
          <Input label="Code (ISO 4217)" value={form.code} onChange={(e) => setForm({ ...form, code: e.target.value })} />
          <Input label="Name" value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} />
          <Input
            label="Decimal digits"
            type="number"
            value={form.decimalDigits}
            onChange={(e) => setForm({ ...form, decimalDigits: Number(e.target.value) })}
          />
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
            <Button onClick={save}>Save</Button>
          </div>
        </div>
      </Dialog>

      <Dialog open={rateOpen} onClose={() => setRateOpen(false)} title="New exchange rate">
        <div className="flex flex-col gap-4">
          <Select
            label="Currency"
            placeholder="Select currency"
            options={currencies.items.map((c) => ({ value: c.code, label: c.code }))}
            value={rateForm.currencyCode}
            onChange={(e) => setRateForm({ ...rateForm, currencyCode: e.target.value })}
          />
          <Input
            label="Rate to USD (1 unit = ? USD)"
            type="number"
            step="0.00000001"
            value={rateForm.rateToBase}
            onChange={(e) => setRateForm({ ...rateForm, rateToBase: e.target.value })}
          />
          <Input
            label="Effective date"
            type="date"
            value={rateForm.effectiveDate}
            onChange={(e) => setRateForm({ ...rateForm, effectiveDate: e.target.value })}
          />
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setRateOpen(false)}>Cancel</Button>
            <Button onClick={saveRate}>Save</Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}

function UsersTab() {
  const users = useCrud<OpsUser>('/api/master/users', false)
  const [open, setOpen] = useState(false)
  const [editing, setEditing] = useState<OpsUser | null>(null)
  const [form, setForm] = useState({ email: '', password: '', role: 'Sale' as UserRole, isActive: true })
  const toast = useToast()

  const openNew = () => {
    setEditing(null)
    setForm({ email: '', password: '', role: 'Sale', isActive: true })
    setOpen(true)
  }
  const openEdit = (user: OpsUser) => {
    setEditing(user)
    setForm({ email: user.email, password: '', role: user.role, isActive: user.isActive })
    setOpen(true)
  }

  const save = async () => {
    try {
      if (editing) {
        await users.update(editing.id, { role: form.role, isActive: form.isActive, password: form.password || undefined })
      } else {
        await users.create({ email: form.email, password: form.password, role: form.role })
      }
      toast.show('User saved', 'success')
      setOpen(false)
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to save user', 'destructive')
    }
  }

  const deactivate = async (user: OpsUser) => {
    if (!confirm(`Deactivate ${user.email}?`)) return
    try {
      await users.remove(user.id)
      toast.show('User deactivated', 'success')
    } catch (err) {
      toast.show(err instanceof ApiError ? err.message : 'Failed to deactivate user', 'destructive')
    }
  }

  return (
    <div className="flex flex-col gap-3">
      <div className="flex justify-end">
        <Button onClick={openNew}>New user</Button>
      </div>
      {users.loading ? (
        <Skeleton height="2rem" />
      ) : users.error ? (
        <EmptyState title="Could not load users" description={users.error} />
      ) : (
        <Table
          columns={[
            { key: 'email', header: 'Email', render: (u: OpsUser) => u.email },
            { key: 'role', header: 'Role', render: (u: OpsUser) => <Badge variant="info">{u.role}</Badge> },
            { key: 'status', header: 'Status', render: (u: OpsUser) => (u.isActive ? <Badge variant="success">Active</Badge> : <Badge variant="destructive">Inactive</Badge>) },
            {
              key: 'actions',
              header: '',
              render: (u: OpsUser) => (
                <div className="flex gap-2">
                  <Button variant="outline" onClick={() => openEdit(u)}>Edit</Button>
                  {u.isActive && <Button variant="outline-destructive" onClick={() => deactivate(u)}>Deactivate</Button>}
                </div>
              ),
            },
          ]}
          rows={users.items}
          rowKey={(u) => u.id}
          emptyTitle="No users yet"
        />
      )}
      <Dialog open={open} onClose={() => setOpen(false)} title={editing ? 'Edit user' : 'New user'}>
        <div className="flex flex-col gap-4">
          {!editing && <Input label="Email" type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />}
          <Input
            label={editing ? 'New password (leave blank to keep current)' : 'Password'}
            type="password"
            value={form.password}
            onChange={(e) => setForm({ ...form, password: e.target.value })}
          />
          <Select
            label="Role"
            options={[{ value: 'Sale', label: 'Sale' }, { value: 'Operation', label: 'Operation' }, { value: 'Admin', label: 'Admin' }]}
            value={form.role}
            onChange={(e) => setForm({ ...form, role: e.target.value as UserRole })}
          />
          {editing && (
            <Select
              label="Status"
              options={[{ value: 'true', label: 'Active' }, { value: 'false', label: 'Inactive' }]}
              value={String(form.isActive)}
              onChange={(e) => setForm({ ...form, isActive: e.target.value === 'true' })}
            />
          )}
          <div className="flex justify-end gap-3">
            <Button variant="outline" onClick={() => setOpen(false)}>Cancel</Button>
            <Button onClick={save}>Save</Button>
          </div>
        </div>
      </Dialog>
    </div>
  )
}
