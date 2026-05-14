const { spawn } = require('child_process');
const path = require('path');

const projectPath = path.resolve(__dirname, 'src/Aspire/Marketplace.AppHost/Marketplace.AppHost.csproj');

const child = spawn('dotnet', ['run', '--project', projectPath], {
  env: {
    ...process.env,
    ASPNETCORE_ENVIRONMENT: 'Testing'
  },
  stdio: 'pipe'
});

child.stdout.on('data', (data) => {
  console.log(`[AppHost]: ${data}`);
});

child.stderr.on('data', (data) => {
  console.error(`[AppHost Error]: ${data}`);
});

setTimeout(() => {
  child.kill();
  process.exit(0);
}, 10000);
