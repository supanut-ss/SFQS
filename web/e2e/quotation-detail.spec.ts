import { test, expect, type Page } from '@playwright/test'

async function loginAsAdmin(page: Page) {
  await page.goto('/#/ops')
  await page.getByLabel('Email *').fill('admin@freito.local')
  await page.getByLabel('Password *').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Sign in', exact: true }).click()
  await expect(page.getByText('admin@freito.local')).toBeVisible()
}

/** Submits the public Instant Quote form and returns the resulting quote reference (e.g. Q20260926-0001). */
async function createQuotationViaInstantQuote(page: Page): Promise<string> {
  await page.goto('/#/quote')

  const originOption = page.getByLabel('Origin *').locator('option', { hasText: 'THBKK' })
  await page.getByLabel('Origin *').selectOption(await originOption.getAttribute('value') || '')

  const destOption = page.getByLabel('Destination *').locator('option', { hasText: 'SGSIN' })
  await page.getByLabel('Destination *').selectOption(await destOption.getAttribute('value') || '')
  await page.getByLabel('Incoterm *').selectOption({ value: 'FOB' })
  await page.getByLabel('Container size *').selectOption({ label: '20' })
  await page.getByLabel('Container quantity *').fill('1')
  await page.getByLabel('Ready date *').fill('2026-10-01')
  await page.getByRole('button', { name: 'Calculate quote' }).click()

  const requestButton = page.getByRole('button', { name: 'Request this quote' })
  await expect(requestButton).toBeVisible()
  await requestButton.click()

  await page.getByLabel('Your name *').fill('E2E Test Contact')
  await page.getByLabel('Email *').fill('e2e@example.com')
  await page.getByLabel('Phone *').fill('0800000000')
  await page.getByLabel('Cargo type *').selectOption({ label: 'General Cargo' })
  await requestButton.click()

  const reference = page.getByText(/^Reference Q\d+-\d+/)
  await expect(reference).toBeVisible()
  const text = await reference.textContent()
  const match = text?.match(/Q\d+-\d+/)
  if (!match) throw new Error(`Could not parse quote reference from: ${text}`)
  return match[0]
}

test.describe('quotation detail — modular sections menu (requires API + seeded dev DB)', () => {
  test('adds and removes a modular section via the accessible Add Section menu', async ({ page }) => {
    const reference = await createQuotationViaInstantQuote(page)

    await loginAsAdmin(page)
    await page.getByRole('tab', { name: 'Quotations' }).click()
    await page.getByRole('row', { name: new RegExp(reference) }).getByRole('button', { name: 'View' }).click()
    await expect(page.getByRole('heading', { name: reference })).toBeVisible()

    const addSectionButton = page.getByRole('button', { name: '+ Add Section' })

    // Escape closes the menu without adding anything.
    await addSectionButton.click()
    const insuranceItem = page.getByRole('menuitem', { name: /Cargo Insurance/ })
    await expect(insuranceItem).toBeVisible()
    await page.keyboard.press('Escape')
    await expect(insuranceItem).not.toBeVisible()
    await expect(page.getByText('Cargo Insurance', { exact: true })).not.toBeVisible()

    // Selecting an item adds that section and removes it from the menu next time.
    await addSectionButton.click()
    await page.getByRole('menuitem', { name: /Cargo Insurance/ }).click()
    await expect(page.getByText('Cargo Insurance', { exact: true })).toBeVisible()
    await expect(page.getByLabel('Insurance Status')).toBeVisible()

    await addSectionButton.click()
    await expect(page.getByRole('menuitem', { name: /Cargo Insurance/ })).not.toBeVisible()
    await page.keyboard.press('Escape')

    // Remove puts it back in the menu.
    await page.getByRole('button', { name: 'Remove', exact: true }).click()
    await expect(page.getByText('Cargo Insurance', { exact: true })).not.toBeVisible()
    await addSectionButton.click()
    await expect(page.getByRole('menuitem', { name: /Cargo Insurance/ })).toBeVisible()
  })
})
