import { FullConfig } from '@playwright/test';
import { spawn } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

async function globalSetup(config: FullConfig) {
  const frontendURL = config.projects[0].use.baseURL || 'http://localhost:4200';
  const probeTimeoutMs = 5000;
  let appHostPid: number | undefined;

  // Check if frontend is already running (AppHost started externally)
  let alreadyRunning = false;
  try {
    const resp = await fetch(frontendURL, { signal: AbortSignal.timeout(probeTimeoutMs) });
    if (resp.ok || resp.status === 302) {
      alreadyRunning = true;
      console.log(`Frontend already running at ${frontendURL}, skipping AppHost startup.`);
    }
  } catch { /* not running */ }

  if (!alreadyRunning) {
    console.log('Starting .NET Aspire AppHost...');
    const projectPath = path.resolve(__dirname, '../../src/Aspire/Marketplace.AppHost/Marketplace.AppHost.csproj');

    const child = spawn('dotnet', ['run', '--project', projectPath], {
      env: { ...process.env, ASPNETCORE_ENVIRONMENT: 'Testing' },
      detached: true,
      stdio: 'pipe'
    });

    child.unref();
    appHostPid = child.pid;
    fs.writeFileSync(path.join(__dirname, 'server.pid'), child.pid?.toString() || '');

    child.stdout?.on('data', () => {});
    child.stderr?.on('data', (data) => {
      console.error(`[AppHost Error]: ${data}`);
    });

    // Wait for frontend to be ready
    console.log(`Waiting for frontend at ${frontendURL}...`);
    const timeout = 300 * 1000;
    const start = Date.now();
    let ready = false;

    while (!ready && Date.now() - start < timeout) {
      try {
        const resp = await fetch(frontendURL, { signal: AbortSignal.timeout(probeTimeoutMs) });
        if (resp.ok || resp.status === 302) {
          ready = true;
        }
      } catch { /* ignore */ }
      if (!ready) await new Promise(r => setTimeout(r, 2000));
    }

    if (!ready) throw new Error(`Frontend failed to start at ${frontendURL} within ${timeout}ms`);
    console.log('Frontend is ready!');

    // Wait additional time for backend services to stabilize
    // The frontend (Angular SSR) responds before backend APIs are fully ready
    console.log('Waiting for backend services to stabilize...');
    await new Promise(r => setTimeout(r, 10_000));
    console.log('Server is ready!');
  }

  // Save whether we started the AppHost (for teardown)
  fs.writeFileSync(path.join(__dirname, 'external-host'), alreadyRunning ? 'true' : 'false');
}

export default globalSetup;
