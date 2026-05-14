import { FullConfig } from '@playwright/test';
import { spawn } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

async function globalSetup(config: FullConfig) {
  console.log('Starting .NET Aspire AppHost...');
  
  const projectPath = path.resolve(__dirname, '../../src/Aspire/Marketplace.AppHost/Marketplace.AppHost.csproj');
  
  const child = spawn('dotnet', ['run', '--project', projectPath], {
    env: {
      ...process.env,
      ASPNETCORE_ENVIRONMENT: 'Testing'
    },
    detached: true,
    stdio: 'pipe'
  });

  // Unref so Node.js can exit cleanly without waiting for the child
  child.unref();

  // Save PID to a file for teardown
  fs.writeFileSync(path.join(__dirname, 'server.pid'), child.pid?.toString() || '');

  child.stdout?.on('data', (data) => {
    // console.log(`[AppHost]: ${data}`);
  });

  child.stderr?.on('data', (data) => {
    console.error(`[AppHost Error]: ${data}`);
  });

  // Wait for the server to be ready
  const baseURL = config.projects[0].use.baseURL || 'http://localhost:4200';
  console.log(`Waiting for server at ${baseURL}...`);
  
  let ready = false;
  const timeout = 300 * 1000;
  const start = Date.now();
  const probeTimeoutMs = 5000;

  while (!ready && Date.now() - start < timeout) {
    try {
      const [frontendResponse, healthResponse] = await Promise.all([
        fetch(baseURL, { signal: AbortSignal.timeout(probeTimeoutMs) }),
        fetch(`${baseURL}/bff/health`, { signal: AbortSignal.timeout(probeTimeoutMs) })
      ]);

      if (frontendResponse.ok && healthResponse.ok) {
        ready = true;
      }
    } catch (e) {
      // Ignore errors while waiting
    }
    if (!ready) {
      await new Promise(resolve => setTimeout(resolve, 2000));
    }
  }

  if (!ready) {
    throw new Error(`Server failed to start at ${baseURL} within ${timeout}ms`);
  }

  console.log('Server is ready!');
}

export default globalSetup;
