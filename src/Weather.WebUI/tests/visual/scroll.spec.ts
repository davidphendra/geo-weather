import { test, expect } from '@playwright/test';

/**
 * BUG-002 regression: Fluent UI Blazor adopts a global stylesheet that sets
 * `body { height: 100dvh; overflow: hidden }`. With `html` overflow visible, that propagates to the
 * viewport and blocks user scroll input (programmatic scrollTo still works, so it went unnoticed).
 * These checks use a real wheel event rather than scrollTo.
 */
test.describe('page scrolling', () => {
  test('the wheel scrolls the page and the footer can be reached', async ({ page }) => {
    await page.goto('/favorites', { waitUntil: 'networkidle' });

    // Guarantee the document is taller than the viewport, independent of seeded data.
    await page.evaluate(() => {
      const spacer = document.createElement('div');
      spacer.style.height = '1500px';
      document.querySelector('main')?.appendChild(spacer);
    });

    const maxScrollable = await page.evaluate(() =>
      Math.round(document.documentElement.scrollHeight - window.innerHeight),
    );
    expect(maxScrollable, 'the page must overflow the viewport').toBeGreaterThan(0);

    expect(
      await page.evaluate(() => getComputedStyle(document.body).overflowY),
      'the body must not lock the viewport',
    ).not.toBe('hidden');

    await page.mouse.move(200, 300);
    await page.mouse.wheel(0, 3000);

    await expect
      .poll(() => page.evaluate(() => Math.round(window.scrollY)), { timeout: 3000 })
      .toBeGreaterThan(0);

    await expect(page.locator('footer')).toBeInViewport();
  });

  test('navigation links are not force-underlined', async ({ page }) => {
    await page.goto('/', { waitUntil: 'networkidle' });

    const decoration = await page
      .locator('nav a')
      .first()
      .evaluate((element) => getComputedStyle(element).textDecorationLine);

    expect(decoration, 'Fluent must not force-underlines on links').toBe('none');
  });
});
