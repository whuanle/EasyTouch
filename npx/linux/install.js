#!/usr/bin/env node

const fs = require('fs');
const path = require('path');

const binaryPath = path.join(__dirname, 'et');

try {
    if (!fs.existsSync(binaryPath)) {
        console.error(`❌ EasyTouch binary not found: ${binaryPath}`);
        process.exit(1);
    }

    fs.chmodSync(binaryPath, 0o755);
    console.log(`✓ Set executable permission: ${binaryPath}`);
} catch (error) {
    console.error(`❌ Failed to set executable permission: ${error.message}`);
    process.exit(1);
}
