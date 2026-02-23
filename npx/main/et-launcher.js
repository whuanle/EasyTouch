#!/usr/bin/env node

const { spawn } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');

const binaryName = os.platform() === 'win32' ? 'et.exe' : 'et';
const binaryPath = path.join(__dirname, binaryName);

if (!fs.existsSync(binaryPath)) {
    console.error('❌ EasyTouch binary not found. Please run: npm install');
    process.exit(1);
}

const child = spawn(binaryPath, process.argv.slice(2), {
    stdio: 'inherit',
    windowsHide: false
});

child.on('exit', (code) => process.exit(code));
