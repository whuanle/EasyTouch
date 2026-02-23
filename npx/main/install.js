#!/usr/bin/env node

const fs = require('fs');
const path = require('path');
const { execSync } = require('child_process');
const os = require('os');

const platform = os.platform();
const arch = os.arch();

console.log('EasyTouch Post-Install Script');
console.log('=============================');
console.log(`Platform: ${platform}`);
console.log(`Architecture: ${arch}`);
console.log();

const binDir = path.join(__dirname, 'bin');
const platformDir = path.join(__dirname, 'platforms');

// 创建 bin 目录
if (!fs.existsSync(binDir)) {
    fs.mkdirSync(binDir, { recursive: true });
}

// 根据平台设置可执行文件路径
let sourcePath;
let binaryName = platform === 'win32' ? 'et.exe' : 'et';

try {
    switch (platform) {
        case 'win32':
            if (arch === 'x64') {
                sourcePath = path.join(platformDir, 'windows', 'et.exe');
                // 检查 optional dependency
                const winPkg = path.join(__dirname, 'node_modules', 'easytouch-windows', 'et.exe');
                if (fs.existsSync(winPkg)) {
                    sourcePath = winPkg;
                }
            } else {
                throw new Error('Windows x86 not supported. Please use x64.');
            }
            break;
            
        case 'linux':
            if (arch === 'x64') {
                sourcePath = path.join(platformDir, 'linux', 'et');
                // 检查 optional dependency
                const linuxPkg = path.join(__dirname, 'node_modules', 'easytouch-linux', 'et');
                if (fs.existsSync(linuxPkg)) {
                    sourcePath = linuxPkg;
                }
            } else {
                throw new Error('Linux architecture not supported. Please use x64.');
            }
            break;
            
        case 'darwin':
            sourcePath = path.join(platformDir, 'macos', 'et');
            // macOS 使用 install.js 逻辑选择正确的架构
            const macPkg = path.join(__dirname, 'node_modules', 'easytouch-macos', 'bin');
            if (fs.existsSync(macPkg)) {
                // 让 easytouch-macos 的 postinstall 处理
                console.log('✓ Using easytouch-macos package');
                process.exit(0);
            }
            break;
            
        default:
            throw new Error(`Unsupported platform: ${platform}`);
    }

    // 检查源文件是否存在
    if (!fs.existsSync(sourcePath)) {
        console.error(`❌ Binary not found: ${sourcePath}`);
        console.log('\nPlease install the platform-specific package:');
        console.log(`  npm install -g easytouch-${platform === 'win32' ? 'windows' : platform === 'darwin' ? 'macos' : 'linux'}`);
        process.exit(1);
    }

    // 复制或链接可执行文件
    const targetPath = path.join(binDir, binaryName);
    
    try {
        fs.copyFileSync(sourcePath, targetPath);
        console.log(`✓ Copied binary: ${targetPath}`);
    } catch (e) {
        // 如果复制失败，尝试创建符号链接
        try {
            fs.symlinkSync(sourcePath, targetPath);
            console.log(`✓ Created symlink: ${targetPath} -> ${sourcePath}`);
        } catch (e2) {
            console.error('❌ Failed to setup binary:', e2.message);
            process.exit(1);
        }
    }

    // 设置执行权限（Unix 系统）
    if (platform !== 'win32') {
        fs.chmodSync(targetPath, 0o755);
    }

    console.log('\n✅ EasyTouch installed successfully!');
    console.log('Run: et --help');

} catch (error) {
    console.error('❌ Installation failed:', error.message);
    process.exit(1);
}
