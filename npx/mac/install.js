#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');

// Detect architecture
const arch = process.arch;
const platform = process.platform;

if (platform !== 'darwin') {
    console.error('Error: This package is for macOS only.');
    process.exit(1);
}

const binDir = path.join(__dirname, 'bin');
const targetBinary = path.join(binDir, 'et');

// Determine which binary to use
let sourceBinary;
if (arch === 'arm64') {
    sourceBinary = path.join(binDir, 'et-arm64');
    console.log('Detected Apple Silicon (arm64), using optimized binary...');
} else if (arch === 'x64') {
    sourceBinary = path.join(binDir, 'et-x64');
    console.log('Detected Intel (x64), using x64 binary...');
} else {
    console.error(`Unsupported architecture: ${arch}`);
    process.exit(1);
}

// Check if source binary exists
if (!fs.existsSync(sourceBinary)) {
    console.error(`Binary not found: ${sourceBinary}`);
    process.exit(1);
}

// Create symlink or copy the binary
try {
    if (fs.existsSync(targetBinary)) {
        fs.unlinkSync(targetBinary);
    }
    
    // Try to create symlink first (faster, saves space)
    try {
        fs.symlinkSync(sourceBinary, targetBinary);
        console.log(`Created symlink: ${targetBinary} -> ${sourceBinary}`);
    } catch (e) {
        // Fallback to copy if symlink fails
        fs.copyFileSync(sourceBinary, targetBinary);
        fs.chmodSync(targetBinary, 0o755);
        console.log(`Copied binary: ${targetBinary}`);
    }
    
    console.log('EasyTouch macOS installed successfully!');
    console.log(`Run: et --help`);
} catch (error) {
    console.error('Installation failed:', error.message);
    process.exit(1);
}
