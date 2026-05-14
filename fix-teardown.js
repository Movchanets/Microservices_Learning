const fs = require('fs');

let teardownContent = fs.readFileSync('tests/E2ETests/globalTeardown.ts', 'utf8');
teardownContent = teardownContent.replace(/taskkill \/pid \$\{pid\} \/T/g, "kill -15 ${pid}");
teardownContent = teardownContent.replace(/tasklist \/FI "PID eq \$\{pid\}" \/NH/g, "ps -p ${pid} -o comm=");
teardownContent = teardownContent.replace(/taskkill \/pid \$\{pid\} \/T \/F/g, "kill -9 ${pid}");
fs.writeFileSync('tests/E2ETests/globalTeardown.ts', teardownContent);
