import { request } from '@playwright/test'

interface Port {
  id: number
  code: string
}

interface Carrier {
  id: number
  code: string
}

interface RateItem {
  id: number
  originPortId: number
  destinationPortId: number
  mode: string
  isActive: boolean
}

export default async function globalSetup() {
  const context = await request.newContext({
    baseURL: 'http://localhost:5025',
  })

  // 1. Log in as admin
  const loginRes = await context.post('/api/auth/login', {
    data: {
      email: 'admin@freito.local',
      password: 'ChangeMe123!',
    },
  })
  if (!loginRes.ok()) {
    console.warn(`Could not log in as admin during global setup: ${loginRes.status()} ${await loginRes.text()}`)
    await context.dispose()
    return
  }

  // 2. Fetch master data to resolve dynamic IDs
  const portsRes = await context.get('/api/master/ports')
  const ports = (await portsRes.json()) as Port[]
  const bkkPort = ports.find((p) => p.code === 'THBKK')
  const sinPort = ports.find((p) => p.code === 'SGSIN')

  const carriersRes = await context.get('/api/master/carriers')
  const carriers = (await carriersRes.json()) as Carrier[]
  const maersk = carriers.find((c) => c.code === 'MAERSK')

  if (!bkkPort || !sinPort || !maersk) {
    console.warn('Could not find required ports (THBKK, SGSIN) or carrier (MAERSK) for e2e seed.')
    await context.dispose()
    return
  }

  // 3. Ensure test rate exists
  const ratesRes = await context.get('/api/rates')
  if (ratesRes.ok()) {
    const data = (await ratesRes.json()) as { items?: RateItem[] }
    const items = data.items || []
    const hasRate = items.some(
      (r) =>
        r.originPortId === bkkPort.id &&
        r.destinationPortId === sinPort.id &&
        r.mode.toLowerCase() === 'fcl' &&
        r.isActive
    )

    if (!hasRate) {
      console.log('Seeding e2e test freight rate (THBKK -> SGSIN 20 FCL)...')
      const createRes = await context.post('/api/rates', {
        data: {
          originPortId: bkkPort.id,
          destinationPortId: sinPort.id,
          mode: 'fcl',
          direction: 'export',
          carrierId: maersk.id,
          containerSize: '20',
          priceMin: 130,
          priceMax: 130,
          currencyCode: 'USD',
          validFrom: '2025-01-01T00:00:00Z',
          validTo: '2030-01-01T00:00:00Z',
        },
      })
      if (!createRes.ok()) {
        console.warn(`Failed to seed rate: ${createRes.status()} ${await createRes.text()}`)
      }
    }
  }

  await context.dispose()
}
