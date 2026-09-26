import { test, expect, type Page } from '@playwright/test'

async function loginAsAdmin(page: Page) {
  await page.goto('/#/ops')
  await page.getByLabel('Email *').fill('admin@freito.local')
  await page.getByLabel('Password *').fill('ChangeMe123!')
  await page.getByRole('button', { name: 'Sign in', exact: true }).click()
  await expect(page.getByText('admin@freito.local')).toBeVisible()
}

test.describe('ops area (requires the API + seeded dev DB running)', () => {
  test('logs in and switches sections via the tab list', async ({ page }) => {
    await loginAsAdmin(page)

    const tablist = page.getByRole('tablist', { name: 'Operation area sections' })
    await expect(tablist).toBeVisible()

    const ratesTab = page.getByRole('tab', { name: 'Rates' })
    await ratesTab.click()
    await expect(ratesTab).toHaveAttribute('aria-selected', 'true')
    await expect(page).toHaveURL(/#\/ops\/rates/)
    await expect(page.getByRole('heading', { name: 'Rate management' })).toBeVisible()

    const localChargesTab = page.getByRole('tab', { name: 'Local charges' })
    await localChargesTab.click()
    await expect(localChargesTab).toHaveAttribute('aria-selected', 'true')
    await expect(page).toHaveURL(/#\/ops\/local-charges/)
  })

  test('opens the Deactivate confirmation dialog and closes it with Escape', async ({ page }) => {
    await loginAsAdmin(page)
    await page.getByRole('tab', { name: 'Rates' }).click()

    const dialogTitle = page.getByRole('heading', { name: 'Deactivate freight rate' })
    await page.getByRole('button', { name: 'Deactivate' }).first().click()
    await expect(dialogTitle).toBeVisible()

    await page.keyboard.press('Escape')
    await expect(dialogTitle).not.toBeVisible()
  })

  test('closes the confirmation dialog via the Cancel button', async ({ page }) => {
    await loginAsAdmin(page)
    await page.getByRole('tab', { name: 'Rates' }).click()

    await page.getByRole('button', { name: 'Deactivate' }).first().click()
    const dialogTitle = page.getByRole('heading', { name: 'Deactivate freight rate' })
    await expect(dialogTitle).toBeVisible()

    await page.getByRole('button', { name: 'Cancel' }).click()
    await expect(dialogTitle).not.toBeVisible()
  })
})
