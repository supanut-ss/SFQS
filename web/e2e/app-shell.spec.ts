import { test, expect } from '@playwright/test'

test.describe('app shell', () => {
  test('loads the Instant Quote page by default', async ({ page }) => {
    await page.goto('/')
    await expect(page.getByRole('heading', { name: 'Freito' })).toBeVisible()
    await expect(page.getByText('Instant Quote', { exact: true })).toBeVisible()
  })

  test('navigates between Instant Quote and Ops sign-in', async ({ page }) => {
    await page.goto('/')
    await page.getByRole('button', { name: 'Operation / Admin sign in' }).click()
    await expect(page).toHaveURL(/#\/ops/)
    await expect(page.getByRole('heading', { name: 'Sign in' })).toBeVisible()

    await page.getByRole('button', { name: 'Instant Quote' }).click()
    await expect(page).toHaveURL(/#\/quote/)
  })
})

test.describe('dark mode toggle', () => {
  test('switches the theme and persists it across reload', async ({ page }) => {
    await page.goto('/')
    const toggle = page.getByRole('button', { name: /Switch to (dark|light) mode/ })
    await expect(toggle).toBeVisible()

    const wasDark = await page.evaluate(() => document.documentElement.classList.contains('dark'))
    await toggle.click()
    await expect(page.locator('html')).toHaveClass(wasDark ? '' : 'dark')

    await page.reload()
    const isDarkAfterReload = await page.evaluate(() => document.documentElement.classList.contains('dark'))
    expect(isDarkAfterReload).toBe(!wasDark)
  })
})
