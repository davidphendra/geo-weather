import { test, expect, type Page } from '@playwright/test';
import path from 'node:path';
import { pathToFileURL } from 'node:url';

const REPO_ROOT = path.resolve(process.cwd(), '..', '..');

const PROTO = {
  explorer: pathToFileURL(
    path.join(REPO_ROOT, 'specs/designs/weather_explorer_asp.net_core_client/code.html'),
  ).href,
  favorites: pathToFileURL(
    path.join(REPO_ROOT, 'specs/designs/favorites_saved_notes_ef_core_state/code.html'),
  ).href,
};

const VIEWPORTS = [
  { width: 390, height: 900 },
  { width: 768, height: 1000 },
  { width: 1600, height: 1200 },
];

interface Probe {
  selector: string;
  protoSelector?: string;
  props: string[];
  compareWidth?: boolean;
}

const CASES: Array<{ name: string; app: string; proto: string; probes: Record<string, Probe> }> = [
  {
    name: 'weather-explorer',
    app: '/',
    proto: PROTO.explorer,
    probes: {
      header: { selector: 'header', props: ['backgroundColor', 'paddingLeft'], compareWidth: true },
      footer: { selector: 'footer', props: ['backgroundColor', 'fontFamily', 'fontSize'], compareWidth: true },
      queryCard: {
        selector: 'section:has(.location-combobox)',
        protoSelector: 'section:has(select)',
        props: ['backgroundColor', 'borderTopLeftRadius', 'paddingTop', 'paddingLeft'],
        compareWidth: true,
      },
      primaryButton: {
        selector: 'button.bg-primary',
        props: ['backgroundColor', 'color', 'fontSize', 'fontWeight', 'borderTopLeftRadius'],
      },
    },
  },
  {
    name: 'favorites',
    app: '/favorites',
    proto: PROTO.favorites,
    probes: {
      header: { selector: 'header', props: ['backgroundColor', 'paddingLeft'], compareWidth: true },
      footer: { selector: 'footer', props: ['backgroundColor', 'fontFamily', 'fontSize'], compareWidth: true },
      metricCard: {
        selector: 'div.grid > div.rounded-xl',
        props: ['backgroundColor', 'borderTopLeftRadius', 'paddingTop', 'paddingLeft'],
        compareWidth: true,
      },
      primaryButton: {
        selector: 'button.bg-primary',
        props: ['backgroundColor', 'color', 'fontSize', 'fontWeight', 'borderTopLeftRadius'],
      },
      tableHead: {
        selector: 'thead th',
        props: ['color', 'fontSize', 'fontWeight', 'fontFamily'],
      },
    },
  },
];

// camelCase -> kebab-case for getComputedStyle.getPropertyValue
function cssName(property: string): string {
  return property.replace(/[A-Z]/g, (character) => `-${character.toLowerCase()}`);
}

async function measure(
  page: Page,
  probe: Probe,
): Promise<{ styles: Record<string, string>; width: number; height: number }> {
  const element = page.locator(probe.selector).first();
  await element.waitFor({ state: 'attached' });

  return element.evaluate((node, input) => {
    const computed = getComputedStyle(node as Element);
    const styles: Record<string, string> = {};
    for (const property of input.props) {
      styles[property] = computed.getPropertyValue(
        property.replace(/[A-Z]/g, (character) => `-${character.toLowerCase()}`),
      );
    }
    const box = (node as Element).getBoundingClientRect();
    return { styles, width: Math.round(box.width), height: Math.round(box.height) };
  }, probe);
}

for (const viewport of VIEWPORTS) {
  for (const testCase of CASES) {
    test(`${testCase.name} matches the prototype @ ${viewport.width}px`, async ({ browser }) => {
      const appContext = await browser.newContext({ viewport });
      const protoContext = await browser.newContext({ viewport });
      const app = await appContext.newPage();
      const proto = await protoContext.newPage();

      const violations: string[] = [];
      app.on('console', (message) => {
        if (/Content Security Policy|Refused to/i.test(message.text())) {
          violations.push(message.text());
        }
      });

      await app.goto(testCase.app, { waitUntil: 'networkidle' });
      await proto.goto(testCase.proto, { waitUntil: 'networkidle' });
      await app.evaluate(() => document.fonts.ready);
      await proto.evaluate(() => document.fonts.ready);

      for (const [label, probe] of Object.entries(testCase.probes)) {
        const actual = await measure(app, probe);
        const expected = await measure(proto, { ...probe, selector: probe.protoSelector ?? probe.selector });

        for (const [property, value] of Object.entries(expected.styles)) {
          expect
            .soft(actual.styles[property], `${testCase.name} ${label}.${cssName(property)}`)
            .toBe(value);
        }

        if (probe.compareWidth) {
          expect
            .soft(Math.abs(actual.width - expected.width), `${testCase.name} ${label}.width`)
            .toBeLessThanOrEqual(2);
        }
      }

      const header = await measure(app, { selector: 'header', props: [] });
      expect(header.height, 'header height').toBe(64);

      const container = await measure(app, { selector: '[class*="max-w-[1600px]"]', props: [] });
      const protoContainer = await measure(proto, { selector: '[class*="max-w-[1600px]"]', props: [] });
      expect(Math.abs(container.width - protoContainer.width), 'container width').toBeLessThanOrEqual(2);

      expect(violations, 'CSP violations').toEqual([]);

      await app.screenshot({
        path: path.join('tests', 'visual', 'artifacts', `${testCase.name}-${viewport.width}.png`),
        fullPage: true,
      });

      await appContext.close();
      await protoContext.close();
    });
  }
}

test('favorites circuit connects without CSP violations', async ({ page }) => {
  const failures: string[] = [];
  page.on('console', (message) => {
    if (/Content Security Policy|Refused to/i.test(message.text())) {
      failures.push(message.text());
    }
  });

  await page.goto('/favorites', { waitUntil: 'networkidle' });

  // Interactivity proves the Blazor circuit is alive under the enforced policy.
  await page.getByRole('button', { name: 'Note' }).first().click();
  await expect(page.getByText('AddWeatherNoteCommand')).toBeVisible();

  expect(failures).toEqual([]);
});
