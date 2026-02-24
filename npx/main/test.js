#!/usr/bin/env node

const { spawn } = require('child_process');
const path = require('path');
const fs = require('fs');
const os = require('os');

const platform = os.platform();
const binaryName = platform === 'win32' ? 'et.exe' : 'et';
const platformPackages = platform === 'win32'
    ? ['easytouch-windows', 'easytouch-windows']
    : platform === 'darwin'
        ? ['easytouch-mac', 'easytouch-macos', 'easytouch-darwin', 'easytouch-macos']
        : ['easytouch-linux', 'easytouch-linux'];

function findExistingPath(paths) {
    for (const p of paths) {
        if (fs.existsSync(p)) {
            return p;
        }
    }
    return null;
}

// 查找二进制文件路径
function findBinary() {
    // 1. 检查 bin 目录
    const localBinPath = path.join(__dirname, '..', 'bin', binaryName);
    if (fs.existsSync(localBinPath)) {
        return localBinPath;
    }
    
    // 2. 检查 node_modules 中的平台包（兼容 scope/非 scope）
    const localNodeModulesMatches = findExistingPath(
        platformPackages.map((pkg) => path.join(__dirname, '..', 'node_modules', pkg, binaryName))
    );
    if (localNodeModulesMatches) {
        return localNodeModulesMatches;
    }
    
    // 3. 全局安装检查
    try {
        const { execSync } = require('child_process');
        const globalPath = execSync('npm root -g', { encoding: 'utf8' }).trim();
        const globalMatches = findExistingPath(
            platformPackages.map((pkg) => path.join(globalPath, pkg, binaryName))
        );
        if (globalMatches) {
            return globalMatches;
        }
    } catch (e) {}
    
    return null;
}

console.log('🧪 EasyTouch Test Suite');
console.log('=======================\n');

const binaryPath = findBinary();
if (!binaryPath) {
    console.error('❌ EasyTouch binary not found!');
    console.log('\nPlease install EasyTouch first:');
    console.log('  npm install -g easytouch');
    process.exit(1);
}

console.log(`✓ Found binary: ${binaryPath}\n`);

// 测试用例
const tests = [
    { name: 'Help command', args: ['--help'], expectSuccess: true },
    { name: 'Version check', args: ['--version'], expectSuccess: true },
    { name: 'Mouse position', args: ['mouse_position'], expectSuccess: true },
    { name: 'OS info', args: ['os_info'], expectSuccess: true },
    { name: 'CPU info', args: ['cpu_info'], expectSuccess: true },
    { name: 'Memory info', args: ['memory_info'], expectSuccess: true },
    { name: 'Screen list', args: ['screen_list'], expectSuccess: true },
    { name: 'Process list', args: ['process_list'], expectSuccess: true },
    { name: 'Window list', args: ['window_list'], expectSuccess: true },
    { name: 'Window foreground', args: ['window_foreground'], expectSuccess: true },
    { name: 'Disk list', args: ['disk_list'], expectSuccess: true },
    { name: 'Invalid command', args: ['invalid_command_xyz'], expectSuccess: false },
];

let passed = 0;
let failed = 0;

function runTest(test, index) {
    return new Promise((resolve) => {
        console.log(`[${index + 1}/${tests.length}] Testing: ${test.name}`);
        
        const child = spawn(binaryPath, test.args, {
            timeout: 10000,
            windowsHide: true
        });
        
        let stdout = '';
        let stderr = '';
        
        child.stdout.on('data', (data) => {
            stdout += data.toString();
        });
        
        child.stderr.on('data', (data) => {
            stderr += data.toString();
        });
        
        child.on('close', (code) => {
            const success = test.expectSuccess ? code === 0 : code !== 0;
            
            if (success) {
                console.log(`  ✓ Passed`);
                passed++;
            } else {
                console.log(`  ✗ Failed (exit code: ${code})`);
                if (stderr) console.log(`  Error: ${stderr.trim()}`);
                failed++;
            }
            
            resolve();
        });
        
        child.on('error', (err) => {
            console.log(`  ✗ Failed: ${err.message}`);
            failed++;
            resolve();
        });
    });
}

async function runTests() {
    for (let i = 0; i < tests.length; i++) {
        await runTest(tests[i], i);
    }
    
    console.log('\n' + '='.repeat(50));
    console.log('Test Results:');
    console.log(`  ✓ Passed: ${passed}`);
    console.log(`  ✗ Failed: ${failed}`);
    console.log(`  Total: ${tests.length}`);
    console.log('='.repeat(50));
    
    if (failed === 0) {
        console.log('\n🎉 All tests passed!');
        process.exit(0);
    } else {
        console.log(`\n⚠️  ${failed} test(s) failed.`);
        process.exit(1);
    }
}

// MCP 模式测试
console.log('Testing CLI commands...\n');
runTests().then(() => {
    // 测试 MCP 模式
    console.log('\n\nTesting MCP mode...\n');
    
    const mcpChild = spawn(binaryPath, ['--mcp'], {
        timeout: 5000,
        windowsHide: true
    });
    
    let mcpOutput = '';
    
    mcpChild.stdout.on('data', (data) => {
        mcpOutput += data.toString();
    });
    
    // 发送初始化请求
    setTimeout(() => {
        const initRequest = JSON.stringify({
            jsonrpc: '2.0',
            id: 1,
            method: 'initialize',
            params: {
                protocolVersion: '2024-11-05',
                capabilities: {},
                clientInfo: { name: 'test-client', version: '1.0.0' }
            }
        });
        
        try {
            mcpChild.stdin.write(initRequest + '\n');
        } catch (e) {
            console.log('MCP test: Could not write to stdin (expected)');
        }
    }, 1000);
    
    setTimeout(() => {
        mcpChild.kill();
        
        if (mcpOutput.includes('jsonrpc') || mcpOutput.includes('result') || mcpOutput.includes('tools')) {
            console.log('✓ MCP mode appears to be working\n');
        } else {
            console.log('⚠️  MCP mode test inconclusive (may require manual verification)\n');
        }
        
        console.log('='.repeat(50));
        console.log('Test Complete!');
        console.log('='.repeat(50));
    }, 3000);
});
