#!/usr/bin/env node

/**
 * EasyTouch Browser Automation Test Suite
 * 测试浏览器自动化功能
 * 
 * 需要 Playwright 已安装
 * 用法: node test-browser.js [options]
 */

const { spawn, execSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');

const PLATFORM = os.platform();
const IS_WINDOWS = PLATFORM === 'win32';
const TEMP_DIR = os.tmpdir();

// 配置
const CONFIG = {
    verbose: process.argv.includes('--verbose'),
    headless: !process.argv.includes('--headed'),
    timeout: 30000,
};

// 获取 EasyTouch 路径
function getEasyTouchPath() {
    // 尝试多个位置
    const binaryName = IS_WINDOWS ? 'et.exe' : 'et';
    const tryPaths = [
        binaryName,
        path.join(__dirname, '..', 
            IS_WINDOWS ? 'EasyTouch-Windows' : PLATFORM === 'darwin' ? 'EasyTouch-Mac' : 'EasyTouch-Linux',
            'bin', 'Release', 'net10.0',
            IS_WINDOWS ? 'win-x64' : PLATFORM === 'darwin' ? 'osx-x64' : 'linux-x64',
            'publish', binaryName),
    ];
    
    for (const tryPath of tryPaths) {
        try {
            if (fs.existsSync(tryPath) || tryPath === binaryName) {
                execSync(`"${tryPath}" --version`, { stdio: 'pipe' });
                return tryPath;
            }
        } catch (e) {}
    }
    
    return null;
}

// 运行 EasyTouch 命令
function runCommand(args, timeout = CONFIG.timeout) {
    return new Promise((resolve) => {
        const etPath = getEasyTouchPath();
        if (!etPath) {
            resolve({ success: false, error: 'EasyTouch not found' });
            return;
        }

        const spawnOptions = {
            timeout: timeout,
            windowsHide: true,
            env: { ...process.env }
        };
        
        if (IS_WINDOWS) {
            spawnOptions.shell = true;
        }

        const child = spawn(etPath, args, spawnOptions);

        let stdout = '';
        let stderr = '';

        child.stdout.on('data', (data) => {
            stdout += data.toString();
        });

        child.stderr.on('data', (data) => {
            stderr += data.toString();
        });

        child.on('close', (code) => {
            resolve({
                success: code === 0,
                exitCode: code,
                output: stdout.trim(),
                error: stderr.trim()
            });
        });

        child.on('error', (err) => {
            resolve({
                success: false,
                exitCode: -1,
                error: err.message
            });
        });
    });
}

// 解析 JSON 输出
function parseJson(output) {
    try {
        // 尝试找到 JSON 部分
        const lines = output.split('\n');
        for (const line of lines) {
            const trimmed = line.trim();
            if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
                return JSON.parse(trimmed);
            }
        }
        return null;
    } catch {
        return null;
    }
}

// 浏览器测试套件
async function runBrowserTests() {
    console.log('\n╔════════════════════════════════════════════════════════════╗');
    console.log('║     EasyTouch Browser Automation Tests                    ║');
    console.log('╚════════════════════════════════════════════════════════════╝\n');
    
    const etPath = getEasyTouchPath();
    if (!etPath) {
        console.error('❌ EasyTouch not found!');
        process.exit(1);
    }
    
    console.log(`✓ Using: ${etPath}`);
    console.log(`Mode: ${CONFIG.headless ? 'Headless' : 'Headed'}\n`);
    
    let browserId = null;
    const results = [];
    
    // 测试 1: 启动浏览器
    console.log('Test 1/8: Launching browser...');
    const launchResult = await runCommand([
        'browser_launch',
        '--browser', 'chromium',
        '--headless', CONFIG.headless.toString()
    ]);
    
    if (launchResult.success) {
        const data = parseJson(launchResult.output);
        if (data?.Success && data?.BrowserId) {
            browserId = data.BrowserId;
            console.log('✓ Browser launched:', browserId);
            results.push({ name: 'Launch browser', status: 'PASS' });
        } else {
            console.log('✗ Failed to get browser ID');
            results.push({ name: 'Launch browser', status: 'FAIL', error: launchResult.output });
        }
    } else {
        console.log('✗ Launch failed:', launchResult.error || launchResult.output);
        results.push({ name: 'Launch browser', status: 'FAIL', error: launchResult.error });
    }
    
    if (!browserId) {
        console.log('\n❌ Cannot continue without browser. Exiting.');
        printSummary(results);
        process.exit(1);
    }
    
    // 测试 2: 导航到页面
    console.log('\nTest 2/8: Navigating to example.com...');
    const navResult = await runCommand([
        'browser_navigate',
        '--browser-id', browserId,
        '--url', 'https://example.com',
        '--timeout', '10000'
    ]);
    
    if (navResult.success) {
        console.log('✓ Navigation successful');
        results.push({ name: 'Navigate', status: 'PASS' });
    } else {
        console.log('✗ Navigation failed:', navResult.error || navResult.output);
        results.push({ name: 'Navigate', status: 'FAIL', error: navResult.error });
    }
    
    // 测试 3: 查找元素
    console.log('\nTest 3/8: Finding elements...');
    const findResult = await runCommand([
        'browser_find',
        '--browser-id', browserId,
        '--selector', 'h1',
        '--timeout', '5000'
    ]);
    
    if (findResult.success) {
        console.log('✓ Element found');
        results.push({ name: 'Find element', status: 'PASS' });
    } else {
        console.log('✗ Find failed:', findResult.error || findResult.output);
        results.push({ name: 'Find element', status: 'FAIL', error: findResult.error });
    }
    
    // 测试 4: 获取文本
    console.log('\nTest 4/8: Getting text...');
    const textResult = await runCommand([
        'browser_get_text',
        '--browser-id', browserId,
        '--selector', 'h1'
    ]);
    
    if (textResult.success) {
        const data = parseJson(textResult.output);
        console.log('✓ Text retrieved:', data?.Text || 'N/A');
        results.push({ name: 'Get text', status: 'PASS' });
    } else {
        console.log('✗ Get text failed:', textResult.error || textResult.output);
        results.push({ name: 'Get text', status: 'FAIL', error: textResult.error });
    }
    
    // 测试 5: 执行 JavaScript
    console.log('\nTest 5/8: Evaluating JavaScript...');
    const evalResult = await runCommand([
        'browser_evaluate',
        '--browser-id', browserId,
        '--script', 'document.title'
    ]);
    
    if (evalResult.success) {
        const data = parseJson(evalResult.output);
        console.log('✓ JavaScript executed:', data?.Result || 'N/A');
        results.push({ name: 'Evaluate JS', status: 'PASS' });
    } else {
        console.log('✗ Eval failed:', evalResult.error || evalResult.output);
        results.push({ name: 'Evaluate JS', status: 'FAIL', error: evalResult.error });
    }
    
    // 测试 6: 截图
    console.log('\nTest 6/8: Taking screenshot...');
    const screenshotPath = path.join(TEMP_DIR, 'browser-test-screenshot.png');
    const screenshotResult = await runCommand([
        'browser_screenshot',
        '--browser-id', browserId,
        '--output', screenshotPath,
        '--type', 'png'
    ]);
    
    if (screenshotResult.success) {
        if (fs.existsSync(screenshotPath)) {
            const stats = fs.statSync(screenshotPath);
            console.log(`✓ Screenshot saved: ${screenshotPath} (${stats.size} bytes)`);
            results.push({ name: 'Screenshot', status: 'PASS' });
            // 清理
            fs.unlinkSync(screenshotPath);
        } else {
            console.log('✗ Screenshot file not created');
            results.push({ name: 'Screenshot', status: 'FAIL' });
        }
    } else {
        console.log('✗ Screenshot failed:', screenshotResult.error || screenshotResult.output);
        results.push({ name: 'Screenshot', status: 'FAIL', error: screenshotResult.error });
    }
    
    // 测试 7: 列表浏览器
    console.log('\nTest 7/8: Listing browsers...');
    const listResult = await runCommand(['browser_list']);
    
    if (listResult.success) {
        const data = parseJson(listResult.output);
        const count = data?.Browsers?.length || 0;
        console.log(`✓ Found ${count} browser(s)`);
        results.push({ name: 'List browsers', status: 'PASS' });
    } else {
        console.log('✗ List failed:', listResult.error || listResult.output);
        results.push({ name: 'List browsers', status: 'FAIL', error: listResult.error });
    }
    
    // 测试 8: 关闭浏览器
    console.log('\nTest 8/8: Closing browser...');
    const closeResult = await runCommand([
        'browser_close',
        '--browser-id', browserId,
        '--force', 'false'
    ]);
    
    if (closeResult.success) {
        console.log('✓ Browser closed');
        results.push({ name: 'Close browser', status: 'PASS' });
    } else {
        console.log('✗ Close failed:', closeResult.error || closeResult.output);
        results.push({ name: 'Close browser', status: 'FAIL', error: closeResult.error });
    }
    
    // 打印摘要
    printSummary(results);
    
    const failed = results.filter(r => r.status === 'FAIL').length;
    process.exit(failed > 0 ? 1 : 0);
}

function printSummary(results) {
    console.log('\n' + '='.repeat(70));
    console.log('📊 Test Summary');
    console.log('-'.repeat(70));
    
    const passed = results.filter(r => r.status === 'PASS').length;
    const failed = results.filter(r => r.status === 'FAIL').length;
    
    console.log(`Total:   ${results.length}`);
    console.log(`Passed:  ${passed} ✓`);
    console.log(`Failed:  ${failed} ✗`);
    console.log('-'.repeat(70));
    
    if (failed > 0) {
        console.log('\n✗ Failed Tests:');
        results.filter(r => r.status === 'FAIL').forEach(r => {
            console.log(`  - ${r.name}${r.error ? ': ' + r.error : ''}`);
        });
    }
    
    if (passed === results.length) {
        console.log('\n🎉 All browser tests passed!');
    }
}

// 主程序
if (process.argv.includes('--help')) {
    console.log(`
EasyTouch Browser Automation Test Suite

Usage: node test-browser.js [options]

Options:
  --headed        Run browser in headed mode (visible)
  --verbose       Show detailed output
  --help          Show this help

Examples:
  node test-browser.js
  node test-browser.js --headed
  node test-browser.js --verbose
`);
    process.exit(0);
}

runBrowserTests().catch(err => {
    console.error('Test error:', err);
    process.exit(1);
});
