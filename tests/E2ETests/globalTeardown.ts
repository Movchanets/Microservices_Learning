import { FullConfig } from '@playwright/test';
import { execSync } from 'child_process';
import * as path from 'path';
import * as fs from 'fs';

async function globalTeardown(config: FullConfig) {
  console.log('Stopping .NET Aspire AppHost...');
  
  const pidFile = path.join(__dirname, 'server.pid');
  if (fs.existsSync(pidFile)) {
    const pid = fs.readFileSync(pidFile, 'utf8');
    if (pid) {
      try {
        // Use taskkill on Windows to kill the process and its children (/T) forcefully (/F)
        execSync(`taskkill /pid ${pid} /T /F`);
        console.log(`Stopped process ${pid} and its children.`);
      } catch (e: any) {
        console.warn(`Could not kill process ${pid}: ${e.message}`);
      }
    }
    fs.unlinkSync(pidFile);
  }
}

export default globalTeardown;
