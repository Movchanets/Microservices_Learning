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

const isWindows = process.platform === 'win32';

function killProcess(pid: string, graceful: boolean): void {
  if (isWindows) {
    execSafe(`taskkill /pid ${pid} /T${graceful ? '' : ' /F'}`);
  } else {
    execSafe(`kill ${graceful ? '-TERM' : '-9'} ${pid}`);
  }
}

function isProcessAlive(pid: string): boolean {
  if (isWindows) {
    const check = execSafe(`tasklist /FI "PID eq ${pid}" /NH`);
    return check.includes('dotnet');
  }
  try {
    process.kill(Number(pid), 0);
    return true;
  } catch {
    return false;
  }
}

async function globalTeardown(config: FullConfig) {
  const externalHostFile = path.join(__dirname, 'external-host');
  const isExternal = fs.existsSync(externalHostFile) && fs.readFileSync(externalHostFile, 'utf8').trim() === 'true';

  if (isExternal) {
    console.log('AppHost was started externally, skipping shutdown.');
    if (fs.existsSync(externalHostFile)) fs.unlinkSync(externalHostFile);
    return;
  }

  console.log('Stopping .NET Aspire AppHost...');

  const pidFile = path.join(__dirname, 'server.pid');
  if (fs.existsSync(pidFile)) {
    const pid = fs.readFileSync(pidFile, 'utf8').trim();
    if (pid) {
      console.log(`Sending graceful shutdown to PID ${pid}...`);
      killProcess(pid, true);

      let alive = true;
      for (let i = 0; i < 10; i++) {
        await new Promise(r => setTimeout(r, 1000));
        if (!isProcessAlive(pid)) {
          alive = false;
          break;
        }
      }

      if (alive) {
        console.warn(`Process ${pid} did not exit gracefully, force killing...`);
        killProcess(pid, false);
      }

      console.log(`Stopped process ${pid}.`);
    }
    fs.unlinkSync(pidFile);
  }

  if (fs.existsSync(externalHostFile)) fs.unlinkSync(externalHostFile);
}

export default globalTeardown;
