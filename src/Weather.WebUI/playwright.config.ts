import { defineConfig, devices } from '@playwright/test';

const PORT = 5300;
const APP_URL = `http://127.0.0.1:${PORT}`;

export default defineConfig({
  testDir: './tests/visual',
  timeout: 60_000,
  expect: { timeout: 10_000 },
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],
  use: {
    baseURL: APP_URL,
    trace: 'off',
    screenshot: 'off',
  },
  projects: [{ name: 'chromium', use: { ...devices['Desktop Chrome'] } }],
  webServer: {
    command: 'dotnet run --project . --no-launch-profile',
    url: `${APP_URL}/`,
    reuseExistingServer: false,
    timeout: 180_000,
    env: {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: APP_URL,
      // Enforce (not report-only) so the harness also proves the CSP keeps the circuit alive.
      Csp__EnforceInDevelopment: 'true',
      OpenWeather__ApiKey: 'visual-test-key',
    },
  },
});
