import { FullConfig } from '@playwright/test';
import { execSync } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

function execSafe(cmd: string): string {
  try {
    return execSync(cmd, { encoding: 'utf8', timeout: 15000 }).trim();
  } catch {
    return '';
  }
}

async function globalTeardown(config: FullConfig) {
  console.log('Stopping .NET Aspire AppHost...');

  const pidFile = path.join(__dirname, 'server.pid');
  if (fs.existsSync(pidFile)) {
    const pid = fs.readFileSync(pidFile, 'utf8').trim();
    if (pid) {
      // Step 1: Graceful shutdown — no /F flag, gives Aspire time to stop containers
      console.log(`Sending graceful shutdown to PID ${pid}...`);
      execSafe(`taskkill /pid ${pid} /T`);

      // Step 2: Wait up to 10s for the process to exit cleanly
      let alive = true;
      for (let i = 0; i < 10; i++) {
        await new Promise(r => setTimeout(r, 1000));
        const check = execSafe(`tasklist /FI "PID eq ${pid}" /NH`);
        if (!check.includes('dotnet')) {
          alive = false;
          break;
        }
      }

      // Step 3: Force kill if still alive
      if (alive) {
        console.warn(`Process ${pid} did not exit gracefully, force killing...`);
        execSafe(`taskkill /pid ${pid} /T /F`);
      }

      console.log(`Stopped process ${pid}.`);
    }
    fs.unlinkSync(pidFile);
  }

  // Step 4: Safety net — clean up any orphaned Docker containers from this AppHost run.
  // Aspire containers that survive a hard kill have no parent process to stop them.
  console.log('Cleaning up orphaned Docker containers...');
  const aspireContainers = execSafe(
    'docker ps -q --filter "label=aspire.resource.name"'
  );
  if (aspireContainers) {
    const ids = aspireContainers.split(/\s+/).filter(Boolean);
    console.log(`Found ${ids.length} orphaned Aspire container(s), removing...`);
    execSafe(`docker rm -f ${ids.join(' ')}`);
  }
}

export default globalTeardown;
