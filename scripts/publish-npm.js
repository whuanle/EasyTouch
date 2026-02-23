#!/usr/bin/env node
const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const platform = process.argv[2] || process.platform;
const version = process.argv[3];

if (!version) {
    console.log('Usage: node publish-npm.js [platform] <version>');
    console.log('');
    console.log('Platforms:');
    console.log('  win32, windows    Windows x64');
    console.log('  linux             Linux x64');
    console.log('  darwin, macos     macOS x64');
    console.log('  all               All platforms');
    console.log('');
    console.log('Examples:');
    console.log('  node publish-npm.js 1.0.0              # Auto-detect platform');
    console.log('  node publish-npm.js linux 1.0.0        # Linux only');
    console.log('  node publish-npm.js all 1.0.0          # All platforms');
    process.exit(1);
}

const scriptsDir = __dirname;

function publishPlatform(platformName, version) {
    console.log(`\n🚀 Publishing for ${platformName}...\n`);
    
    let scriptName;
    switch (platformName) {
        case 'win32':
        case 'windows':
            scriptName = 'publish-npm-win-x64.js';
            break;
        case 'linux':
            scriptName = 'publish-npm-linux-x64.js';
            break;
        case 'darwin':
        case 'macos':
            scriptName = 'publish-npm-darwin-x64.js';
            break;
        default:
            console.error(`❌ Unknown platform: ${platformName}`);
            return false;
    }
    
    const scriptPath = path.join(scriptsDir, scriptName);
    if (!fs.existsSync(scriptPath)) {
        console.error(`❌ Script not found: ${scriptPath}`);
        return false;
    }
    
    try {
        execSync(`node "${scriptPath}" ${version}`, { stdio: 'inherit' });
        return true;
    } catch (e) {
        console.error(`❌ Failed to publish for ${platformName}`);
        return false;
    }
}

// 检测当前平台
function detectPlatform() {
    switch (process.platform) {
        case 'win32': return 'windows';
        case 'linux': return 'linux';
        case 'darwin': return 'macos';
        default: return null;
    }
}

// 主逻辑
console.log('\n╔════════════════════════════════════════════════════════════╗');
console.log('║     EasyTouch NPM Publisher                               ║');
console.log('╚════════════════════════════════════════════════════════════╝\n');

if (platform === 'all') {
    // 发布所有平台
    const platforms = ['windows', 'linux', 'macos'];
    const results = {};
    
    for (const p of platforms) {
        results[p] = publishPlatform(p, version);
    }
    
    console.log('\n╔════════════════════════════════════════════════════════════╗');
    console.log('║     Publish Summary                                        ║');
    console.log('╚════════════════════════════════════════════════════════════╝\n');
    
    let allSuccess = true;
    for (const [p, success] of Object.entries(results)) {
        const icon = success ? '✅' : '❌';
        console.log(`${icon} ${p}: ${success ? 'Success' : 'Failed'}`);
        if (!success) allSuccess = false;
    }
    
    if (allSuccess) {
        console.log('\n🎉 All packages published successfully!');
    } else {
        console.log('\n⚠️  Some packages failed to publish');
        process.exit(1);
    }
} else {
    // 发布单个平台
    const targetPlatform = platform === 'win32' ? 'windows' : 
                          platform === 'darwin' ? 'macos' : platform;
    
    const detected = detectPlatform();
    if (!targetPlatform && detected) {
        console.log(`📍 Auto-detected platform: ${detected}\n`);
        publishPlatform(detected, version);
    } else if (targetPlatform) {
        publishPlatform(targetPlatform, version);
    } else {
        console.error('❌ Could not detect platform. Please specify manually.');
        process.exit(1);
    }
}
