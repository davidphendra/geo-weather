import { test, expect } from '@playwright/test';

/**
 * The country/city pickers are Fluent UI `FluentCombobox`es (ADR-0020): a filterable, height-capped
 * list in place of the native `<select>`. These checks exercise the browser behaviour the offline
 * dotnet tests cannot reach.
 */
test.describe('searchable location pickers', () => {
  test('country list is capped at ~10 rows and scrolls', async ({ page }) => {
    await page.goto('/', { waitUntil: 'networkidle' });

    await page.locator('#country-selector input').click();

    const listbox = page.locator('#country-selector fluent-listbox');
    await expect(listbox).toBeVisible();

    const metrics = await listbox.evaluate((element) => ({
      maxHeight: getComputedStyle(element).maxHeight,
      client: element.clientHeight,
      scroll: element.scrollHeight,
    }));

    expect(metrics.maxHeight).toBe('256px');
    expect(metrics.client).toBeLessThanOrEqual(256);
    expect(metrics.scroll).toBeGreaterThan(metrics.client);
  });

  test('typing filters the country list', async ({ page }) => {
    await page.goto('/', { waitUntil: 'networkidle' });

    const input = page.locator('#country-selector input');
    await input.click();

    const allOptions = await page.locator('#country-selector fluent-option').count();

    await input.pressSequentially('Ind', { delay: 120 });

    const visible = page.locator('#country-selector fluent-option:visible');
    await expect(visible.filter({ hasText: 'Indonesia' }).first()).toBeVisible();
    await expect(visible.filter({ hasText: 'Afghanistan' })).toHaveCount(0);

    const filteredOptions = await visible.count();
    expect(filteredOptions).toBeGreaterThan(0);
    expect(filteredOptions).toBeLessThan(allOptions);
  });

  test('choosing a filtered country updates the field', async ({ page }) => {
    await page.goto('/', { waitUntil: 'networkidle' });

    const input = page.locator('#country-selector input');
    await input.click();
    await input.pressSequentially('Indones', { delay: 120 });

    await page.locator('#country-selector fluent-option:visible').filter({ hasText: 'Indonesia' }).first().click();

    await expect(input).toHaveValue('Indonesia');
  });
});
