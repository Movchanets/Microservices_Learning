import { FullConfig } from '@playwright/test';
import { spawn } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

// ── Configuration ───────────────────────────────────────────

const BFF_URL = 'http://localhost:4201';
const FRONTEND_URL = 'http://localhost:4201';
const PROBE_TIMEOUT_MS = 5_000;
const APP_HOST_STARTUP_TIMEOUT_MS = 300_000;
const BACKEND_READINESS_TIMEOUT_MS = 120_000;
const BACKEND_PROBE_INTERVAL_MS = 3_000;

// Endpoints that must return 2xx before tests can run.
// /bff/health is the aggregated health endpoint that probes ALL downstream
// microservices in parallel and returns 200 only when every service is healthy,
// 503 if any are degraded. This is proxied by the Angular dev server (/bff/*).
//
// Previously used "/" (not proxied → unreliable) and accepted ANY HTTP status
// as "alive", causing auth.setup to run before services were truly ready.
const HEALTH_ENDPOINTS = [
  `${BFF_URL}/bff/health`,                   // Aggregated: 200 when ALL services healthy
] as const;

// ── Helpers ─────────────────────────────────────────────────

async function probe(url: string, timeoutMs = PROBE_TIMEOUT_MS): Promise<boolean> {
  try {
    const resp = await fetch(url, { signal: AbortSignal.timeout(timeoutMs) });
    // Only 2xx responses mean the service is actually healthy and ready
    // to handle requests. A 502 from the Angular proxy or 503 from the
    // health endpoint means the backend is still warming up.
    return resp.ok;
  } catch {
    return false;
  }
}

async function probeAll(urls: readonly string[]): Promise<{ ready: boolean; failed: string[] }> {
  const results = await Promise.all(urls.map(async (url) => ({
    url,
    ok: await probe(url),
  })));
  const failed = results.filter(r => !r.ok).map(r => r.url);
  return { ready: failed.length === 0, failed };
}

// ── Main ────────────────────────────────────────────────────

async function globalSetup(config: FullConfig) {
  const frontendURL = config.projects[0].use.baseURL || FRONTEND_URL;

  // Step 1: Check if frontend is already running (AppHost started externally)
  let alreadyRunning = false;
  try {
    const resp = await fetch(frontendURL, { signal: AbortSignal.timeout(PROBE_TIMEOUT_MS) });
    if (resp.ok || resp.status === 302) {
      alreadyRunning = true;
      console.log(`[globalSetup] Frontend already running at ${frontendURL}, skipping AppHost startup.`);
    }
  } catch { /* not running */ }

  // Step 2: Start AppHost if needed
  if (!alreadyRunning) {
    console.log('[globalSetup] Starting .NET Aspire AppHost...');
    const projectPath = path.resolve(__dirname, '../../src/Aspire/Marketplace.AppHost/Marketplace.AppHost.csproj');

    const child = spawn('dotnet', ['run', '--project', projectPath], {
      env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Testing' },
      detached: true,
      stdio: 'pipe'
    });

    child.unref();
    fs.writeFileSync(path.join(__dirname, 'server.pid'), child.pid?.toString() || '');

    child.stdout?.on('data', () => {});
    child.stderr?.on('data', (data) => {
      console.error(`[AppHost Error]: ${data}`);
    });

    // Wait for frontend to be ready
    console.log(`[globalSetup] Waiting for frontend at ${frontendURL}...`);
    const start = Date.now();
    let frontendReady = false;

    while (!frontendReady && Date.now() - start < APP_HOST_STARTUP_TIMEOUT_MS) {
      frontendReady = await probe(frontendURL);
      if (!frontendReady) await new Promise(r => setTimeout(r, 2_000));
    }

    if (!frontendReady) {
      throw new Error(`[globalSetup] Frontend failed to start at ${frontendURL} within ${APP_HOST_STARTUP_TIMEOUT_MS}ms`);
    }
    console.log('[globalSetup] Frontend is ready!');
  }

  // Step 3: Wait for backend services to be fully ready (replaces blind 30s sleep)
  // Always runs — even if AppHost was started externally, backend may still be warming up.
  console.log('[globalSetup] Waiting for backend services to be ready...');
  const backendStart = Date.now();

  while (Date.now() - backendStart < BACKEND_READINESS_TIMEOUT_MS) {
    const { ready, failed } = await probeAll(HEALTH_ENDPOINTS);
    if (ready) {
      const elapsed = ((Date.now() - backendStart) / 1000).toFixed(1);
      console.log(`[globalSetup] All backend services ready! (${elapsed}s)`);
      break;
    }
    const elapsed = ((Date.now() - backendStart) / 1000).toFixed(0);
    console.log(`[globalSetup] Waiting for: ${failed.join(', ')} (${elapsed}s elapsed)`);
    await new Promise(r => setTimeout(r, BACKEND_PROBE_INTERVAL_MS));
  }

  // Final check — fail fast if backend never came up
  const { ready, failed } = await probeAll(HEALTH_ENDPOINTS);
  if (!ready) {
    throw new Error(
      `[globalSetup] Backend services failed to become ready within ${BACKEND_READINESS_TIMEOUT_MS / 1000}s.\n` +
      `Still failing: ${failed.join(', ')}`
    );
  }

  // Save whether we started the AppHost (for teardown)
  fs.writeFileSync(path.join(__dirname, 'external-host'), alreadyRunning ? 'true' : 'false');
}

export default globalSetup;