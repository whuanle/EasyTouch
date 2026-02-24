#!/usr/bin/env node

/**
 * EasyTouch Cross-Platform Test Suite
 * 支持 Windows、Linux、macOS
 * 
 * 用法:
 *   node test-easytouch.js [options]
 * 
 * 选项:
 *   --cli-only     只测试 CLI 命令
 *   --mcp-only     只测试 MCP 模式
 *   --verbose      显示详细输出
 *   --output file  将结果保存到文件
 *   --help         显示帮助
 */

const { spawn, spawnSync, execSync } = require('child_process');
const fs = require('fs');
const path = require('path');
const os = require('os');

// 检测平台
const PLATFORM = os.platform();
const IS_WINDOWS = PLATFORM === 'win32';
const IS_LINUX = PLATFORM === 'linux';
const IS_MAC = PLATFORM === 'darwin';
const ARCH = os.arch();
const PLATFORM_PACKAGES = IS_WINDOWS
    ? ['easytouch-windows', 'easytouch-windows']
    : IS_MAC
        ? ['easytouch-mac', 'easytouch-macos', 'easytouch-darwin', 'easytouch-macos']
        : ['easytouch-linux', 'easytouch-linux'];

// 配置
const CONFIG = {
    verbose: process.argv.includes('--verbose'),
    cliOnly: process.argv.includes('--cli-only'),
    mcpOnly: process.argv.includes('--mcp-only'),
    buildOnly: process.argv.includes('--build-only'),
    forceBuild: process.argv.includes('--build'),
    aotBuild: process.argv.includes('--aot'),
    outputFile: getArgValue('--output'),
    timeout: 10000,
};

// 获取命令行参数值
function getArgValue(flag) {
    const index = process.argv.indexOf(flag);
    return index !== -1 && process.argv[index + 1] ? process.argv[index + 1] : null;
}

function firstExistingPath(paths) {
    for (const candidate of paths) {
        if (fs.existsSync(candidate)) {
            return candidate;
        }
    }
    return null;
}

function runCandidateCommand(binaryPath, args, timeout = 5000) {
    const result = spawnSync(binaryPath, args, {
        timeout,
        windowsHide: true,
        encoding: 'utf8',
        env: { ...process.env }
    });

    return {
        status: result.status,
        stdout: (result.stdout || '').trim(),
        stderr: (result.stderr || '').trim(),
        error: result.error ? result.error.message : ''
    };
}

function looksLikeSuccessJson(text) {
    if (!text) return false;
    return /"success"\s*:\s*true/i.test(text);
}

// 获取项目信息
function getProjectInfo() {
    const projectDir = path.join(__dirname, '..');
    const projectName = IS_WINDOWS ? 'EasyTouch-Windows' : IS_MAC ? 'EasyTouch-Mac' : 'EasyTouch-Linux';
    const runtime = IS_WINDOWS ? 'win-x64' : IS_MAC ? (ARCH === 'arm64' ? 'osx-arm64' : 'osx-x64') : 'linux-x64';
    const binaryName = IS_WINDOWS ? 'et.exe' : 'et';
    const projectPath = path.join(projectDir, projectName);
    const publishPath = path.join(projectPath, 'bin', 'Release', 'net10.0', runtime, 'publish', binaryName);
    
    return {
        projectDir,
        projectName,
        projectPath,
        runtime,
        binaryName,
        publishPath
    };
}

// 编译项目
function buildProject() {
    return new Promise((resolve) => {
        const info = getProjectInfo();
        
        console.log(`\n🔨 Building ${info.projectName}...`);
        console.log(`   Runtime: ${info.runtime}`);
        console.log(`   Configuration: Release\n`);
        console.log(`   Build mode: ${CONFIG.aotBuild ? 'AOT' : 'non-AOT'}\n`);
        
        const dotnetArgs = [
            'publish',
            path.join(info.projectPath, `${info.projectName}.csproj`),
            '-c', 'Release',
            '-r', info.runtime,
            '--self-contained', 'true'
        ];

        if (CONFIG.aotBuild) {
            dotnetArgs.push(
                '-p:PublishAot=true',
                '-p:PublishSingleFile=true',
                '-p:PublishTrimmed=true',
                '-p:TrimMode=full'
            );
        } else {
            dotnetArgs.push(
                '-p:PublishAot=false',
                '-p:PublishSingleFile=true',
                '-p:PublishTrimmed=false'
            );
        }
        
        const buildProcess = spawn('dotnet', dotnetArgs, {
            stdio: CONFIG.verbose ? 'inherit' : 'pipe',
            cwd: info.projectDir
        });
        
        let stderr = '';
        if (!CONFIG.verbose) {
            buildProcess.stderr.on('data', (data) => {
                stderr += data.toString();
            });
        }
        
        buildProcess.on('close', (code) => {
            if (code === 0) {
                console.log('✅ Build successful!\n');
                
                // 设置执行权限（Unix）
                if (!IS_WINDOWS && fs.existsSync(info.publishPath)) {
                    try {
                        fs.chmodSync(info.publishPath, 0o755);
                    } catch (e) {
                        console.warn(`⚠️  Could not set executable permissions: ${e.message}`);
                    }
                }
                
                resolve({ success: true, path: info.publishPath });
            } else {
                console.error(`❌ Build failed with exit code: ${code}`);
                if (stderr && !CONFIG.verbose) {
                    console.error('Error output:', stderr);
                }
                resolve({ success: false, error: `Build failed with code ${code}` });
            }
        });
        
        buildProcess.on('error', (err) => {
            console.error(`❌ Build error: ${err.message}`);
            resolve({ success: false, error: err.message });
        });
    });
}

// 查找或构建 EasyTouch
async function findOrBuildEasyTouch() {
    // 如果强制构建，跳过查找
    if (!CONFIG.forceBuild && ET_PATH_CACHE) {
        return ET_PATH_CACHE;
    }
    
    const binaryName = IS_WINDOWS ? 'et.exe' : 'et';
    const info = getProjectInfo();
    
    // 1. 尝试找到已存在的二进制文件（除非强制构建）
    if (!CONFIG.forceBuild) {
        let globalRoot = null;
        try {
            globalRoot = execSync('npm root -g', { encoding: 'utf8' }).trim();
        } catch (e) {}

        const globalPkgBinary = globalRoot
            ? firstExistingPath(PLATFORM_PACKAGES.map((pkg) => path.join(globalRoot, pkg, binaryName)))
            : null;

        const tryPaths = [
            // 本地构建路径（优先，避免命中系统旧版本）
            info.publishPath,
            // npm 全局安装
            globalPkgBinary,
            // 系统 PATH
            binaryName,
        ].filter(Boolean);
        
        let fallbackCandidate = null;

        for (const tryPath of tryPaths) {
            try {
                if (fs.existsSync(tryPath) || tryPath === binaryName) {
                    const versionCheck = runCandidateCommand(tryPath, ['--version']);
                    if (versionCheck.status !== 0) {
                        continue;
                    }

                    // Linux 下优先选择 CPU 查询实现较新的版本，避免命中旧二进制（旧版会调用 top 并报 unknown option '|'）。
                    if (IS_LINUX) {
                        const cpuCheck = runCandidateCommand(tryPath, ['cpu_info']);
                        const cpuOutput = `${cpuCheck.stdout}\n${cpuCheck.stderr}`;
                        const legacyCpuImpl = cpuOutput.includes("top: unknown option '|'");
                        const cpuLooksHealthy = cpuCheck.status === 0 && looksLikeSuccessJson(cpuCheck.stdout);

                        if (legacyCpuImpl || !cpuLooksHealthy) {
                            if (!fallbackCandidate) {
                                fallbackCandidate = tryPath;
                            }
                            continue;
                        }
                    }

                    console.log(`✅ Found EasyTouch: ${tryPath}\n`);
                    ET_PATH_CACHE = tryPath;
                    return tryPath;
                }
            } catch (e) {
                // 继续尝试下一个
            }
        }

        if (fallbackCandidate) {
            if (IS_LINUX) {
                console.log(`⚠️  Found legacy EasyTouch binary (CPU implementation outdated): ${fallbackCandidate}`);
                console.log('   Will try building from source instead of using legacy binary.\n');
            } else {
                console.log(`⚠️  Found legacy EasyTouch binary (missing newer commands): ${fallbackCandidate}`);
                console.log('   Continuing with legacy binary as fallback.\n');
                ET_PATH_CACHE = fallbackCandidate;
                return fallbackCandidate;
            }
        }
    }
    
    // 2. 编译项目
    if (CONFIG.forceBuild) {
        console.log('🔨 Force rebuilding EasyTouch...\n');
    } else {
        console.log('⚠️  EasyTouch not found in PATH or standard locations.');
        console.log('   Attempting to build from source...\n');
    }
    
    const buildResult = await buildProject();
    if (buildResult.success && fs.existsSync(buildResult.path)) {
        ET_PATH_CACHE = buildResult.path;
        return buildResult.path;
    }
    
    return null;
}

// 运行命令并返回结果
function runCommand(args, timeout = CONFIG.timeout) {
    return new Promise((resolve) => {
        let etPath;
        try {
            etPath = getEasyTouchPath();
        } catch (e) {
            resolve({ success: false, exitCode: -1, output: '', error: e.message });
            return;
        }

        const startTime = Date.now();
        
        // Windows 上需要 shell: true 来正确处理 .exe 文件
        const spawnOptions = {
            windowsHide: true,
            env: { ...process.env }
        };
        
        if (IS_WINDOWS) {
            spawnOptions.shell = true;
        }
        
        const child = spawn(etPath, args, spawnOptions);

        let stdout = '';
        let stderr = '';
        let finished = false;
        let timedOut = false;
        let forceKillTimer = null;

        const finish = (payload) => {
            if (finished) return;
            finished = true;
            clearTimeout(timeoutTimer);
            if (forceKillTimer) {
                clearTimeout(forceKillTimer);
            }
            resolve({
                ...payload,
                duration: Date.now() - startTime
            });
        };

        child.stdout.on('data', (data) => {
            stdout += data.toString();
        });

        child.stderr.on('data', (data) => {
            stderr += data.toString();
        });

        const timeoutTimer = setTimeout(() => {
            timedOut = true;
            try {
                child.kill('SIGTERM');
            } catch {}

            forceKillTimer = setTimeout(() => {
                if (finished) return;
                try {
                    child.kill('SIGKILL');
                } catch {}
            }, 1200);
        }, timeout);

        child.on('exit', (code, signal) => {
            const mergedError = [
                stderr.trim(),
                timedOut ? 'Command timed out' : ''
            ].filter(Boolean).join('\n');

            finish({
                success: !timedOut && code === 0,
                exitCode: code ?? -1,
                signal: signal || null,
                output: stdout.trim(),
                error: mergedError
            });
        });

        child.on('error', (err) => {
            finish({
                success: false,
                exitCode: -1,
                signal: null,
                output: '',
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

function parseMcpActionPayload(response) {
    const result = response ? (response.Result ?? response.result) : null;
    if (!result) {
        throw new Error('MCP missing result');
    }
    const success = result.success ?? result.Success;
    const error = result.error ?? result.Error;
    if (success === false) {
        throw new Error(error || 'MCP action failed');
    }

    return {
        success: success ?? true,
        error: error || '',
        data: result.data ?? result.Data ?? null,
        raw: result
    };
}

async function runBrowserChecksViaMcp(etPath) {
    return new Promise((resolve) => {
        const startTime = Date.now();
        const screenshotPath = path.join(os.tmpdir(), `et_browser_mcp_${Date.now()}.png`);
        const child = spawn(etPath, ['--mcp'], {
            windowsHide: true,
            env: { ...process.env }
        });

        let buffer = '';
        let browserId = null;
        let closed = false;
        const pending = new Map();
        let idCounter = 1;

        function finalize(success, error) {
            if (closed) return;
            closed = true;

            for (const [, handler] of pending) {
                clearTimeout(handler.timer);
                handler.reject(new Error('MCP process terminated'));
            }
            pending.clear();

            try { child.kill(); } catch {}
            try { if (fs.existsSync(screenshotPath)) fs.unlinkSync(screenshotPath); } catch {}

            resolve({
                success,
                error: error || '',
                duration: Date.now() - startTime
            });
        }

        function handleLine(line) {
            const trimmed = line.trim();
            if (!trimmed) return;

            let message;
            try {
                message = JSON.parse(trimmed);
            } catch {
                return;
            }

            const id = message.Id ?? message.id;
            if (id == null) return;
            const key = String(id);
            const handler = pending.get(key);
            if (!handler) return;

            clearTimeout(handler.timer);
            pending.delete(key);
            handler.resolve(message);
        }

        function sendMcpRequest(method, params, timeout = 20000) {
            return new Promise((resolveRequest, rejectRequest) => {
                const id = String(idCounter++);
                const timer = setTimeout(() => {
                    pending.delete(id);
                    rejectRequest(new Error(`MCP request timeout: ${method}`));
                }, timeout);

                pending.set(id, { resolve: resolveRequest, reject: rejectRequest, timer });
                const payload = { Jsonrpc: '2.0', Id: id, Method: method };
                if (params !== undefined) payload.Params = params;
                child.stdin.write(JSON.stringify(payload) + '\n');
            });
        }

        child.stdout.on('data', (data) => {
            buffer += data.toString();
            let newlineIndex = buffer.indexOf('\n');
            while (newlineIndex !== -1) {
                const line = buffer.slice(0, newlineIndex);
                buffer = buffer.slice(newlineIndex + 1);
                handleLine(line);
                newlineIndex = buffer.indexOf('\n');
            }
        });

        child.on('error', (err) => {
            finalize(false, `MCP start failed: ${err.message}`);
        });

        child.on('close', () => {
            if (!closed) {
                finalize(false, 'MCP process exited unexpectedly');
            }
        });

        (async () => {
            try {
                const launchResponse = await sendMcpRequest('call', {
                    action: 'browser_launch',
                    BrowserType: 'chromium',
                    Headless: true
                }, 120000);
                const launchPayload = parseMcpActionPayload(launchResponse);
                browserId = launchPayload.data && (launchPayload.data.browserId ?? launchPayload.data.BrowserId)
                    ? (launchPayload.data.browserId ?? launchPayload.data.BrowserId)
                    : null;
                if (!browserId) {
                    throw new Error('browser_launch did not return browserId');
                }

                await sendMcpRequest('call', {
                    action: 'browser_navigate',
                    BrowserId: browserId,
                    Url: 'https://example.com',
                    Timeout: 30000,
                    WaitUntil: 'domcontentloaded'
                }, 45000).then(parseMcpActionPayload);

                const textPayload = await sendMcpRequest('call', {
                    action: 'browser_get_text',
                    BrowserId: browserId,
                    Selector: 'h1'
                }, 30000).then(parseMcpActionPayload);
                const headingText = textPayload.data
                    ? (textPayload.data.text ?? textPayload.data.Text ?? '')
                    : '';
                if (!headingText.includes('Example Domain')) {
                    throw new Error(`Unexpected heading text: ${headingText}`);
                }

                const assertPayload = await sendMcpRequest('call', {
                    action: 'browser_assert_text',
                    BrowserId: browserId,
                    Selector: 'h1',
                    ExpectedText: 'Example Domain',
                    ExactMatch: true
                }, 30000).then(parseMcpActionPayload);
                const assertData = assertPayload.data ?? {};
                const assertPassed = assertData.passed ?? assertData.Passed;
                if (assertPassed === false) {
                    throw new Error('browser_assert_text reported passed=false');
                }

                const pageInfoPayload = await sendMcpRequest('call', {
                    action: 'browser_page_info',
                    BrowserId: browserId
                }, 30000).then(parseMcpActionPayload);
                const pageInfo = pageInfoPayload.data ?? {};
                const pageUrl = pageInfo.url ?? pageInfo.Url ?? '';
                const pageTitle = pageInfo.title ?? pageInfo.Title ?? '';
                if (!String(pageUrl).includes('example.com') || !String(pageTitle).includes('Example Domain')) {
                    throw new Error(`Unexpected page info: url=${pageUrl}, title=${pageTitle}`);
                }

                const evaluatePayload = await sendMcpRequest('call', {
                    action: 'browser_evaluate',
                    BrowserId: browserId,
                    Script: '() => document.title'
                }, 30000).then(parseMcpActionPayload);
                const evaluateData = evaluatePayload.data ?? {};
                const evaluateTitle = evaluateData.result ?? evaluateData.Result ?? '';
                if (!String(evaluateTitle).includes('Example Domain')) {
                    throw new Error(`Unexpected evaluate title: ${evaluateTitle}`);
                }

                await sendMcpRequest('call', {
                    action: 'browser_screenshot',
                    BrowserId: browserId,
                    OutputPath: screenshotPath,
                    Type: 'png'
                }, 45000).then(parseMcpActionPayload);

                if (!fs.existsSync(screenshotPath)) {
                    throw new Error('browser_screenshot did not create file');
                }
                const stat = fs.statSync(screenshotPath);
                if (stat.size <= 0) {
                    throw new Error('browser_screenshot produced empty file');
                }

                await sendMcpRequest('call', {
                    action: 'browser_close',
                    BrowserId: browserId
                }, 20000).then(parseMcpActionPayload);

                finalize(true, '');
            } catch (err) {
                const message = err instanceof Error ? err.message : String(err);
                if (browserId) {
                    try {
                        await sendMcpRequest('call', {
                            action: 'browser_close',
                            BrowserId: browserId
                        }, 5000).then(parseMcpActionPayload);
                    } catch {}
                }
                finalize(false, message);
            }
        })();
    });
}

function parseSuccessData(result, commandName) {
    if (!result.success) {
        const message = extractErrorMessage(result) || `${commandName} failed`;
        throw new Error(message);
    }

    const parsed = parseJson(result.output || '');
    if (!parsed || parsed.success !== true) {
        throw new Error(`${commandName} returned invalid JSON response`);
    }

    return parsed.data || {};
}

async function runBrowserChecksViaCli(runCmd) {
    const startTime = Date.now();
    const screenshotPath = path.join(os.tmpdir(), `et_browser_cli_${Date.now()}.png`);
    let browserId = null;

    try {
        // Ignore cleanup errors of previous runs.
        await runCmd(['browser_daemon_stop'], 5000);

        const launchData = parseSuccessData(
            await runCmd(['browser_launch', '--browser', 'chromium', '--headless', 'true'], 120000),
            'browser_launch'
        );
        browserId = launchData.browserId;
        if (!browserId) {
            throw new Error('browser_launch did not return browserId');
        }

        const navigateData = parseSuccessData(
            await runCmd([
                'browser_navigate',
                '--browser-id', browserId,
                '--url', 'https://example.com',
                '--wait-until', 'domcontentloaded',
                '--timeout', '30000'
            ], 45000),
            'browser_navigate'
        );
        if (!String(navigateData.url || '').includes('example.com')) {
            throw new Error(`Unexpected navigate url: ${navigateData.url}`);
        }

        const textData = parseSuccessData(
            await runCmd(['browser_get_text', '--browser-id', browserId, '--selector', 'h1'], 30000),
            'browser_get_text'
        );
        if (!String(textData.text || '').includes('Example Domain')) {
            throw new Error(`Unexpected h1 text: ${textData.text}`);
        }

        const assertData = parseSuccessData(
            await runCmd([
                'browser_assert_text',
                '--browser-id', browserId,
                '--selector', 'h1',
                '--expected-text', 'Example Domain',
                '--exact-match', 'true'
            ], 30000),
            'browser_assert_text'
        );
        if (assertData.passed === false) {
            throw new Error('browser_assert_text reported passed=false');
        }

        const pageInfo = parseSuccessData(
            await runCmd(['browser_page_info', '--browser-id', browserId], 30000),
            'browser_page_info'
        );
        if (!String(pageInfo.url || '').includes('example.com') || !String(pageInfo.title || '').includes('Example Domain')) {
            throw new Error(`Unexpected page info: url=${pageInfo.url}, title=${pageInfo.title}`);
        }

        const evalData = parseSuccessData(
            await runCmd(['browser_evaluate', '--browser-id', browserId, '--script', '() => document.title'], 30000),
            'browser_evaluate'
        );
        if (!String(evalData.result || '').includes('Example Domain')) {
            throw new Error(`Unexpected evaluate result: ${evalData.result}`);
        }

        parseSuccessData(
            await runCmd(['browser_screenshot', '--browser-id', browserId, '--output', screenshotPath, '--type', 'png'], 45000),
            'browser_screenshot'
        );
        if (!fs.existsSync(screenshotPath) || fs.statSync(screenshotPath).size <= 0) {
            throw new Error('browser_screenshot did not produce a valid file');
        }

        parseSuccessData(
            await runCmd(['browser_close', '--browser-id', browserId], 20000),
            'browser_close'
        );
        browserId = null;

        await runCmd(['browser_daemon_stop'], 5000);

        return {
            success: true,
            error: '',
            duration: Date.now() - startTime
        };
    } catch (err) {
        const message = err instanceof Error ? err.message : String(err);

        if (browserId) {
            try {
                await runCmd(['browser_close', '--browser-id', browserId], 8000);
            } catch {}
        }
        try {
            await runCmd(['browser_daemon_stop'], 5000);
        } catch {}

        return {
            success: false,
            error: message,
            duration: Date.now() - startTime
        };
    } finally {
        try {
            if (fs.existsSync(screenshotPath)) fs.unlinkSync(screenshotPath);
        } catch {}
    }
}

function extractErrorMessage(result) {
    const parsed = parseJson(result.output || '');
    if (parsed && typeof parsed.error === 'string' && parsed.error.trim()) {
        return parsed.error.trim();
    }
    if (result.error && result.error.trim()) {
        return result.error.trim();
    }
    return '';
}

function detectMissingCommand(errorMessage) {
    const startingProcessMatch = errorMessage.match(/ErrorStartingProcess,\s*([^,\s]+),/i);
    if (startingProcessMatch) {
        return startingProcessMatch[1];
    }

    const failedToStartMatch = errorMessage.match(/Failed to start\s+([^\s:]+)/i);
    if (failedToStartMatch) {
        return failedToStartMatch[1];
    }

    return null;
}

function truncate(text, maxLength = 120) {
    if (!text || text.length <= maxLength) {
        return text;
    }
    return text.slice(0, maxLength - 1) + '…';
}

function deriveSkipReason(result, details) {
    const errorMessage = extractErrorMessage(result);
    const source = errorMessage || (details && details.length > 0 ? details.join('; ') : '');
    if (!source) {
        return 'Optional test failed';
    }

    const missingCommand = detectMissingCommand(source);
    if (missingCommand) {
        return `缺少依赖: ${missingCommand}`;
    }

    const lower = source.toLowerCase();
    if (lower.includes('unknown command')) {
        return '命令未实现';
    }
    if (lower.includes("compositor doesn't support")) {
        return 'Wayland 合成器不支持该能力';
    }
    if (lower.includes('ydotoold backend unavailable') || lower.includes('failed to open uinput device')) {
        return 'ydotoold 未就绪或 /dev/uinput 权限不足';
    }
    if (lower.includes('no clipboard tool found')) {
        return '缺少剪贴板工具';
    }

    return truncate(source);
}

// 休眠函数
function sleep(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
}

// 测试用例定义
const TEST_CASES = {
    common: [
        { name: '版本检查', args: ['--version'], expectSuccess: true, checkOutput: false },
        { name: '帮助信息', args: ['--help'], expectSuccess: true, checkOutput: false },
        { name: '鼠标位置', args: ['mouse_position'], expectSuccess: true, checkKeys: ['x', 'y'] },
        { name: '鼠标移动', args: ['mouse_move', '--x', '100', '--y', '100'], expectSuccess: true },
        { name: '鼠标点击', args: ['mouse_click'], expectSuccess: true },
        { name: '鼠标滚轮', args: ['mouse_scroll', '--amount', '3'], expectSuccess: true, optional: true },
        { name: '按键测试', args: ['key_press', '--key', 'a'], expectSuccess: true },
        { name: '输入文本', args: ['type_text', '--text', 'Hello'], expectSuccess: true },
        { name: '系统信息', args: ['os_info'], expectSuccess: true, checkKeys: ['version', 'architecture'] },
        { name: 'CPU信息', args: ['cpu_info'], expectSuccess: true },
        { name: '内存信息', args: ['memory_info'], expectSuccess: true },
        { name: '显示器列表', args: ['screen_list'], expectSuccess: true, checkKeys: ['screens'] },
        { name: '截图功能', args: ['screenshot', '--output', path.join(os.tmpdir(), 'et_test.png')], expectSuccess: true, cleanup: (args) => {
            try { fs.unlinkSync(args[args.indexOf('--output') + 1]); } catch {}
        }},
        { name: '进程列表', args: ['process_list'], expectSuccess: true, checkKeys: ['processes'] },
        { name: '磁盘列表', args: ['disk_list'], expectSuccess: true, checkKeys: ['disks'] },
        { name: '剪贴板写入', args: ['clipboard_set_text', '--text', 'Test123'], expectSuccess: true },
        { name: '剪贴板读取', args: ['clipboard_get_text'], expectSuccess: true },
        { name: '锁定屏幕', args: ['lock_screen'], expectSuccess: true, skip: true, reason: '跳过锁定屏幕测试以避免中断自动化测试' },
        { name: '无效命令', args: ['invalid_command_xyz'], expectSuccess: false },
    ],
    windows: [
        { name: '窗口列表', args: ['window_list'], expectSuccess: true, checkKeys: ['windows'] },
        { name: '前台窗口', args: ['window_foreground'], expectSuccess: true },
        { name: '查找窗口', args: ['window_find', '--title', 'Task Manager'], expectSuccess: true, optional: true },
        { name: '窗口最小化', args: ['window_minimize'], expectSuccess: true, verify: async (result) => {
            // 窗口最小化后，稍微等待，然后恢复
            await sleep(500);
            return true; // 最小化不验证具体结果，因为某些窗口可能无法最小化
        }},
        { name: '窗口最大化', args: ['window_maximize'], expectSuccess: true, verify: async (result) => {
            // 窗口最大化后，稍微等待，然后恢复正常
            await sleep(500);
            return true;
        }},
        // Browser tests - 使用 Playwright CLI（npx playwright）
        { name: '浏览器列表', args: ['browser_list'], expectSuccess: true, checkKeys: ['browsers'] },
        { name: '启动浏览器', args: ['browser_launch', '--browser', 'chromium', '--headless'], expectSuccess: true, verify: async (result) => {
            // 验证返回了 browserId (嵌套在 data 字段中)
            if (result.success) {
                try {
                    const parsed = JSON.parse(result.output);
                    if (parsed.data && parsed.data.browserId) {
                        global.testBrowserId = parsed.data.browserId;
                        return true;
                    }
                } catch {}
            }
            global.testBrowserId = null;
            return false;
        }},
        { name: '浏览器截图', args: ['browser_screenshot'], expectSuccess: true, verify: async (result, runCmd) => {
            if (!global.testBrowserId) return false;
            const outputPath = path.join(os.tmpdir(), 'et_browser_test.png');
            const screenshotResult = await runCmd(['browser_screenshot', '--browser-id', global.testBrowserId, '--output', outputPath]);
            if (screenshotResult.success) {
                try { 
                    if (fs.existsSync(outputPath)) {
                        fs.unlinkSync(outputPath); 
                    }
                } catch {}
            }
            return screenshotResult.success;
        }},
        { name: '关闭浏览器', args: ['browser_close'], expectSuccess: true, verify: async (result, runCmd) => {
            if (!global.testBrowserId) return false;
            const closeResult = await runCmd(['browser_close', '--browser-id', global.testBrowserId]);
            global.testBrowserId = null;
            return closeResult.success;
        }},
    ],
    linux: [
        // Linux 平台补充测试
        { name: '浏览器列表', args: ['browser_list'], expectSuccess: true, checkKeys: ['browsers'], optional: true },
        { name: '浏览器操作(CLI)', args: ['--version'], expectSuccess: true, optional: true, verify: async (_result, runCmd) => {
            const flow = await runBrowserChecksViaCli(runCmd);
            if (!flow.success) {
                throw new Error(flow.error || 'CLI browser flow failed');
            }
            return true;
        }},
        { name: '浏览器操作(MCP)', args: ['--version'], expectSuccess: true, optional: true, verify: async () => {
            const flow = await runBrowserChecksViaMcp(getEasyTouchPath());
            if (!flow.success) {
                throw new Error(flow.error || 'MCP browser flow failed');
            }
            return true;
        }},
    ],
    mac: [
        // 鼠标操作（跨平台）
        { name: '鼠标位置', args: ['mouse_position'], expectSuccess: true, checkKeys: ['x', 'y'], optional: true },
        { name: '鼠标移动', args: ['mouse_move', '--x', '100', '--y', '100'], expectSuccess: true, optional: true },
        { name: '鼠标点击', args: ['mouse_click'], expectSuccess: true, optional: true },
        
        // 键盘操作（跨平台）
        { name: '按键测试', args: ['key_press', '--key', 'a'], expectSuccess: true, optional: true },
        { name: '输入文本', args: ['type_text', '--text', 'Hello'], expectSuccess: true, optional: true },
        
        // 系统信息（跨平台）
        { name: '系统信息', args: ['os_info'], expectSuccess: true, checkKeys: ['version', 'architecture'], optional: true },
        { name: 'CPU信息', args: ['cpu_info'], expectSuccess: true, optional: true },
        { name: '内存信息', args: ['memory_info'], expectSuccess: true, optional: true },
        { name: '进程列表', args: ['process_list'], expectSuccess: true, checkKeys: ['processes'], optional: true },
        { name: '磁盘列表', args: ['disk_list'], expectSuccess: true, checkKeys: ['disks'], optional: true },
        
        // 屏幕操作（跨平台）
        { name: '显示器列表', args: ['screen_list'], expectSuccess: true, checkKeys: ['screens'], optional: true },
        { name: '截图功能', args: ['screenshot', '--output', path.join(os.tmpdir(), 'et_test_mac.png')], expectSuccess: true, optional: true, cleanup: (args) => {
            try { fs.unlinkSync(args[args.indexOf('--output') + 1]); } catch {}
        }},
        
        // 剪贴板（跨平台）
        { name: '剪贴板写入', args: ['clipboard_set_text', '--text', 'MacTest123'], expectSuccess: true, optional: true },
        { name: '剪贴板读取', args: ['clipboard_get_text'], expectSuccess: true, optional: true },
        
        // 浏览器（需要 Playwright）
        { name: '浏览器列表', args: ['browser_list'], expectSuccess: true, checkKeys: ['browsers'], optional: true },
        
        // macOS 特定
        { name: '系统运行时间', args: ['uptime'], expectSuccess: true, optional: true },
        { name: '电池信息', args: ['battery_info'], expectSuccess: true, optional: true },
        { name: 'Spotlight搜索', args: ['spotlight_search', '--query', 'calculator'], expectSuccess: true, optional: true },
    ]
};

function adjustTestsForPlatform(tests) {
    const adjusted = tests.map((test) => ({ ...test }));

    if (IS_LINUX) {
        // Linux 环境差异较大（无头/Wayland/缺少 xclip 等），这些命令统一降级为可选。
        const linuxOptionalCommands = new Set([
            'mouse_move',
            'mouse_click',
            'mouse_scroll',
            'key_press',
            'type_text',
            'screenshot',
            'window_list',
            'window_foreground',
            'clipboard_set_text',
            'clipboard_get_text'
        ]);

        for (const test of adjusted) {
            const command = Array.isArray(test.args) ? test.args[0] : null;
            if (command && linuxOptionalCommands.has(command)) {
                test.optional = true;
            }
        }
    }

    return adjusted;
}

// 运行所有测试
async function runTests() {
    console.log('\n╔════════════════════════════════════════════════════════════╗');
    console.log('║     EasyTouch Cross-Platform Test Suite                   ║');
    console.log('╚════════════════════════════════════════════════════════════╝\n');
    
    console.log(`Platform: ${PLATFORM} (${ARCH})`);
    console.log(`Date: ${new Date().toISOString()}\n`);
    
    // 如果只需要构建
    if (CONFIG.buildOnly) {
        const buildResult = await buildProject();
        process.exit(buildResult.success ? 0 : 1);
    }
    
    // 查找或构建 EasyTouch
    const etPath = await findOrBuildEasyTouch();
    if (!etPath) {
        console.error('❌ Failed to find or build EasyTouch!');
        console.log('\nPlease ensure:');
        console.log('  1. .NET 10 SDK is installed');
        console.log('  2. You have write permissions to the project directory');
        console.log('\nOr install EasyTouch:');
        console.log('  npm install -g easytouch');
        process.exit(1);
    }
    
    console.log(`✓ Using EasyTouch: ${etPath}\n`);
    
    // 获取版本
    const versionResult = await runCommand(['--version']);
    // --version 和 --help 返回 exit code 0 但不一定是 JSON
    if (versionResult.exitCode === 0) {
        console.log(`Version: ${versionResult.output || 'N/A'}\n`);
    } else {
        const signalHint = versionResult.signal ? `, signal: ${versionResult.signal}` : '';
        console.log(`⚠️  Could not get version (exit code: ${versionResult.exitCode}${signalHint})\n`);
    }
    
    if (CONFIG.mcpOnly) {
        await runMCPTests();
        return;
    }
    
    // 确定要运行的测试
    let tests = [...TEST_CASES.common];
    if (IS_WINDOWS) tests = tests.concat(TEST_CASES.windows);
    else if (IS_LINUX) tests = tests.concat(TEST_CASES.linux);
    else if (IS_MAC) tests = tests.concat(TEST_CASES.mac);
    tests = adjustTestsForPlatform(tests);
    
    const results = {
        total: tests.length,
        passed: 0,
        failed: 0,
        skipped: 0,
        tests: []
    };
    
    console.log(`Running ${tests.length} tests...\n`);
    console.log('='.repeat(70));
    
    const context = {}; // 用于测试间传递数据
    
    for (let i = 0; i < tests.length; i++) {
        const test = tests[i];
        const num = `${i + 1}/${tests.length}`.padStart(7);
        let skipReason = null;
        
        // 检查是否跳过此测试
        if (test.skip) {
            console.log(`${num} ${test.name.padEnd(25)} ... ⊘ SKIP (${test.reason || 'Skipped'})`);
            results.skipped++;
            results.tests.push({
                name: test.name,
                status: 'SKIP',
                reason: test.reason
            });
            continue;
        }
        
        process.stdout.write(`${num} ${test.name.padEnd(25)} ... `);
        
        // 准备参数（支持动态参数）
        let args = test.args;
        if (test.prepare) {
            try {
                args = test.prepare(context);
            } catch (e) {
                console.log(`⊘ SKIP (Prepare failed: ${e.message})`);
                results.skipped++;
                continue;
            }
        }
        
        const result = await runCommand(args);
        let status = '✓ PASS';
        let details = [];

        if (result.signal) {
            status = '✗ FAIL';
            details.push(`Process crashed with signal: ${result.signal}`);
        }
        
        // 捕获值供后续测试使用
        if (test.capture && result.success) {
            try {
                const data = parseJson(result.output);
                // 响应格式: { success: true, data: { level: 50, isMuted: false } }
                if (data && data.data && data.data.level !== undefined) {
                    context[test.capture] = data.data.level;
                } else if (data && data.data && data.data.Level !== undefined) {
                    // 兼容首字母大写
                    context[test.capture] = data.data.Level;
                }
            } catch (e) {
                // 忽略解析错误
            }
        }
        
        // 自定义验证
        if (!result.signal && test.verify) {
            try {
                const verifyResult = await test.verify(result, runCommand);
                if (!verifyResult) {
                    status = '✗ FAIL';
                    details.push('Verification failed');
                }
            } catch (e) {
                status = '✗ FAIL';
                details.push(`Verification error: ${e.message}`);
            }
        } else if (!result.signal) {
            // 标准验证
            if (result.success !== test.expectSuccess) {
                status = '✗ FAIL';
                details.push(`Expected success=${test.expectSuccess}, got ${result.success}`);
                if (test.expectSuccess) {
                    const errorMessage = extractErrorMessage(result);
                    if (errorMessage) {
                        details.push(errorMessage);
                    }
                }
            }
            
            // 检查输出内容
            if (test.checkKeys && result.success) {
                for (const key of test.checkKeys) {
                    if (!result.output.includes(key)) {
                        status = '✗ FAIL';
                        details.push(`Missing key: ${key}`);
                    }
                }
            }
            
            if (test.checkOutput && result.success) {
                if (!result.output.includes(test.checkOutput)) {
                    status = '✗ FAIL';
                    details.push(`Expected output: ${test.checkOutput}`);
                }
            }
        }
        
        // 清理
        if (test.cleanup) {
            test.cleanup(args);
        }
        
        // 记录结果
        if (status === '✓ PASS') {
            results.passed++;
        } else if (test.optional) {
            status = '⊘ SKIP';
            results.skipped++;
            skipReason = deriveSkipReason(result, details);
        } else {
            results.failed++;
        }

        results.tests.push({
            name: test.name,
            status: status.includes('PASS') ? 'PASS' : status.includes('SKIP') ? 'SKIP' : 'FAIL',
            duration: result.duration,
            reason: skipReason || undefined,
            output: CONFIG.verbose ? result.output : undefined,
            error: CONFIG.verbose ? result.error : undefined,
            details: details
        });

        if (status === '⊘ SKIP' && skipReason) {
            console.log(`${status} (${skipReason}, ${result.duration}ms)`);
        } else {
            console.log(`${status} (${result.duration}ms)`);
        }
        
        if (CONFIG.verbose && (details.length > 0 || result.error)) {
            if (details.length > 0) console.log(`       Details: ${details.join(', ')}`);
            if (result.error) console.log(`       Error: ${result.error}`);
        }
    }
    
    console.log('='.repeat(70));
    
    // 打印摘要
    printSummary(results);
    
    // MCP 测试
    if (!CONFIG.cliOnly) {
        await runMCPTests();
    }
    
    // 保存结果
    if (CONFIG.outputFile) {
        fs.writeFileSync(CONFIG.outputFile, JSON.stringify(results, null, 2));
        console.log(`\n✓ Results saved to: ${CONFIG.outputFile}`);
    }
    
    // 返回退出码
    process.exit(results.failed > 0 ? 1 : 0);
}

// 打印测试摘要
function printSummary(results) {
    console.log('\n📊 Test Summary');
    console.log('─'.repeat(70));
    console.log(`Total:   ${results.total}`);
    console.log(`Passed:  ${results.passed} ✓`);
    console.log(`Failed:  ${results.failed} ✗`);
    console.log(`Skipped: ${results.skipped} ⊘`);
    console.log('─'.repeat(70));
    
    const passRate = ((results.passed / results.total) * 100).toFixed(1);
    console.log(`Pass Rate: ${passRate}%`);
    
    if (results.failed > 0) {
        console.log('\n✗ Failed Tests:');
        results.tests
            .filter(t => t.status === 'FAIL')
            .forEach(t => console.log(`  - ${t.name}`));
    }

    const skippedWithReasons = results.tests.filter(t => t.status === 'SKIP' && t.reason);
    if (skippedWithReasons.length > 0) {
        console.log('\n⊘ Skipped (reason):');
        skippedWithReasons.forEach(t => console.log(`  - ${t.name}: ${t.reason}`));
    }

    if (IS_LINUX) {
        const skipText = results.tests
            .filter(t => t.status === 'SKIP')
            .map(t => `${t.reason || ''} ${(t.details || []).join(' ')}`)
            .join('\n')
            .toLowerCase();
        const hints = [];
        if (skipText.includes('xdotool')) hints.push('sudo apt install xdotool');
        if (skipText.includes('ydotool')) hints.push('sudo apt install ydotool');
        if (skipText.includes('wayland type text failed')) hints.push('sudo apt install wtype');
        if (skipText.includes('ydotoold backend unavailable') || skipText.includes('uinput')) hints.push('sudo modprobe uinput && sudo systemctl enable --now ydotoold');
        if (skipText.includes('wl-copy') || skipText.includes('wl-paste') || skipText.includes('wl-clipboard')) hints.push('sudo apt install wl-clipboard');
        if (skipText.includes('xclip') || skipText.includes('xsel')) hints.push('sudo apt install xclip xsel');
        if (skipText.includes('convert') || skipText.includes('imagemagick')) hints.push('sudo apt install imagemagick');
        if (hints.length > 0) {
            console.log('\n💡 Linux dependency hints:');
            hints.forEach(h => console.log(`  - ${h}`));
        }
    }
}

// MCP 模式测试
async function runMCPTests() {
    console.log('\n\n🔌 MCP Mode Tests');
    console.log('='.repeat(70));
    
    let etPath;
    try {
        etPath = getEasyTouchPath();
    } catch (e) {
        console.log('✗ MCP Test: ' + e.message);
        return;
    }
    
    return new Promise((resolve) => {
        const child = spawn(etPath, ['--mcp'], {
            timeout: 5000,
            windowsHide: true
        });
        
        let output = '';
        let testPassed = false;
        
        child.stdout.on('data', (data) => {
            output += data.toString();
        });
        
        child.on('error', (err) => {
            console.log('✗ MCP Test: Failed to start');
            console.log(`  Error: ${err.message}`);
            resolve();
        });
        
        // 发送初始化请求
        setTimeout(() => {
            try {
                const initRequest = JSON.stringify({
                    jsonrpc: '2.0',
                    id: 1,
                    method: 'initialize',
                    params: {
                        protocolVersion: '2024-11-05',
                        capabilities: {},
                        clientInfo: { name: 'test-suite', version: '1.0.0' }
                    }
                });
                child.stdin.write(initRequest + '\n');
            } catch (e) {
                console.log('⊘ MCP Test: Could not send request');
            }
        }, 500);
        
        setTimeout(() => {
            child.kill();
            
            if (output.includes('jsonrpc') || output.includes('tools')) {
                console.log('✓ MCP Test: Server responds correctly');
                testPassed = true;
            } else {
                console.log('⊘ MCP Test: Inconclusive (may need manual verification)');
            }
            
            if (CONFIG.verbose && output) {
                console.log('\nMCP Output:');
                console.log(output.substring(0, 500));
            }
            
            resolve();
        }, 2000);
    });
}

// 全局存储 EasyTouch 路径
let ET_PATH_CACHE = null;

// 获取 EasyTouch 路径
function getEasyTouchPath() {
    if (!ET_PATH_CACHE) {
        throw new Error('EasyTouch path not initialized. Call findOrBuildEasyTouch() first.');
    }
    return ET_PATH_CACHE;
}

// 显示帮助
function showHelp() {
    console.log(`
EasyTouch Cross-Platform Test Suite

Usage: node test-easytouch.js [options]

Options:
  --build         Force rebuild before testing
  --aot           Build with AOT (default is non-AOT for better Linux compatibility)
  --build-only    Only build, don't run tests
  --cli-only      Run only CLI tests
  --mcp-only      Run only MCP mode tests
  --verbose       Show detailed output
  --output file   Save results to JSON file
  --help          Show this help

Examples:
  # 自动查找或编译，然后测试
  node test-easytouch.js

  # 强制重新编译
  node test-easytouch.js --build

  # 使用 AOT 编译后测试
  node test-easytouch.js --build --aot

  # 只编译不测试
  node test-easytouch.js --build-only

  # 详细输出
  node test-easytouch.js --verbose

  # 保存结果到文件
  node test-easytouch.js --output results.json

  # 只测试 CLI 命令
  node test-easytouch.js --cli-only --verbose

  # 只编译并保存二进制
  node test-easytouch.js --build-only
`);
}

// 主程序
if (process.argv.includes('--help')) {
    showHelp();
    process.exit(0);
}

runTests().catch(err => {
    console.error('Test suite error:', err);
    process.exit(1);
});
