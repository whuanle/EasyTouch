#!/usr/bin/env node

const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');

const binaryPath = path.join(__dirname, 'et.exe');

if (!fs.existsSync(binaryPath)) {
    console.error('EasyTouch binary not found: et.exe');
    process.exit(1);
}

const child = spawn(binaryPath, process.argv.slice(2), {
    stdio: 'inherit',
    windowsHide: false
});

child.on('exit', (code) => process.exit(code ?? 0));
child.on('error', () => process.exit(1));
