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
  const externalHostFile = path.join(__dirname, 'external-host');
  const isExternal = fs.existsSync(externalHostFile) && fs.readFileSync(externalHostFile, 'utf8').trim() === 'true';

  if (isExternal) {
    console.log('AppHost was started externally, skipping shutdown.');
    fs.unlinkSync(externalHostFile);
    return;
  }

  console.log('Stopping .NET Aspire AppHost...');

  const pidFile = path.join(__dirname, 'server.pid');
  if (fs.existsSync(pidFile)) {
    const pid = fs.readFileSync(pidFile, 'utf8').trim();
    if (pid) {
      console.log(`Sending graceful shutdown to PID ${pid}...`);
      execSafe(`taskkill /pid ${pid} /T`);

      let alive = true;
      for (let i = 0; i < 10; i++) {
        await new Promise(r => setTimeout(r, 1000));
        const check = execSafe(`tasklist /FI "PID eq ${pid}" /NH`);
        if (!check.includes('dotnet')) {
          alive = false;
          break;
        }
      }

      if (alive) {
        console.warn(`Process ${pid} did not exit gracefully, force killing...`);
        execSafe(`taskkill /pid ${pid} /T /F`);
      }

      console.log(`Stopped process ${pid}.`);
    }
    fs.unlinkSync(pidFile);
  }

  if (fs.existsSync(externalHostFile)) fs.unlinkSync(externalHostFile);
}

export default globalTeardown;
