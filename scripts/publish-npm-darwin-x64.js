const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

const version = process.argv[2];
if (!version) {
    console.log('Usage: node publish-npm-darwin-x64.js <version>');
    console.log('Example: node publish-npm-darwin-x64.js 1.0.0');
    process.exit(1);
}

const projectDir = path.resolve(__dirname, '..');
const tempDir = path.join(require('os').tmpdir(), `easytouch-npm-darwin-x64-${Math.floor(Math.random() * 10000)}`);
const distDir = path.join(projectDir, 'npm-dist-darwin-x64');

console.log('\n╔════════════════════════════════════════════════════════════╗');
console.log('║     EasyTouch NPM Publisher - macOS x64                   ║');
console.log('╚════════════════════════════════════════════════════════════╝\n');
console.log(`📦 Version: ${version}`);
console.log(`📁 Project: ${projectDir}`);
console.log(`📁 Temp: ${tempDir}\n`);

// 1. 创建临时目录
if (fs.existsSync(tempDir)) {
    fs.rmSync(tempDir, { recursive: true });
}
fs.mkdirSync(tempDir, { recursive: true });

// 2. 复制 package.json
try {
    const pkgJson = fs.readFileSync(path.join(projectDir, 'npx', 'darwin', 'package.json'), 'utf8');
    const pkg = JSON.parse(pkgJson);
    pkg.version = version;
    // 添加 scope
    pkg.name = 'easytouch-darwin';
    fs.writeFileSync(path.join(tempDir, 'package.json'), JSON.stringify(pkg, null, 2));
    
    // SKILL.md 是可选的
    const skillMdPath = path.join(projectDir, 'npx', 'darwin', 'SKILL.md');
    if (fs.existsSync(skillMdPath)) {
        const skillMd = fs.readFileSync(skillMdPath, 'utf8');
        fs.writeFileSync(path.join(tempDir, 'SKILL.md'), skillMd);
    }
    const readmePath = path.join(projectDir, 'README.md');
    if (fs.existsSync(readmePath)) {
        fs.copyFileSync(readmePath, path.join(tempDir, 'README.md'));
    }
    console.log('📋 Copied package template');
} catch (e) {
    console.error('❌ Error copying package template:', e.message);
    process.exit(1);
}

// 3. 构建可执行文件（Playwright 与 NativeAOT 不兼容，禁用 AOT）
console.log('🔨 Building executable for darwin-x64 (AOT disabled for Playwright compatibility)...');
try {
    const csprojPath = path.join(projectDir, 'EasyTouch-macOS', 'EasyTouch-macOS.csproj');
    execSync(
        `dotnet publish "${csprojPath}" -c Release -r darwin-x64 --self-contained true ` +
        `-p:PublishAot=false -p:PublishSingleFile=true -p:PublishTrimmed=false ` +
        `-o "${tempDir}"`,
        { stdio: 'inherit', cwd: projectDir }
    );
} catch (e) {
    console.error('❌ Build failed!');
    fs.rmSync(tempDir, { recursive: true, force: true });
    process.exit(1);
}

// 4. 验证文件
console.log('✅ Verifying package contents...');
if (!fs.existsSync(path.join(tempDir, 'et'))) {
    console.error('❌ Error: et binary not found after build!');
    fs.rmSync(tempDir, { recursive: true, force: true });
    process.exit(1);
}

// 5. 移除 Playwright 文件（如果有）
const playwrightDir = path.join(tempDir, '.playwright');
if (fs.existsSync(playwrightDir)) {
    fs.rmSync(playwrightDir, { recursive: true, force: true });
    console.log('🗑️  Removed .playwright directory');
}

// 6. 移动到 dist 目录（跨盘符需要复制+删除）
console.log('📦 Moving to distribution directory...');
if (fs.existsSync(distDir)) {
    fs.rmSync(distDir, { recursive: true, force: true });
}

// 使用递归复制（支持跨盘符）
function copyRecursive(src, dest) {
    const stat = fs.statSync(src);
    if (stat.isDirectory()) {
        fs.mkdirSync(dest, { recursive: true });
        fs.readdirSync(src).forEach(child => {
            copyRecursive(path.join(src, child), path.join(dest, child));
        });
    } else {
        fs.copyFileSync(src, dest);
    }
}

copyRecursive(tempDir, distDir);
fs.rmSync(tempDir, { recursive: true, force: true });

// 7. 设置可执行权限（macOS/macOS）
try {
    fs.chmodSync(path.join(distDir, 'et'), 0o755);
} catch (e) {
    console.log('⚠️  Could not set executable permission (Windows limitation)');
}

console.log('\n╔════════════════════════════════════════════════════════════╗');
console.log('║  ✅ NPM Package Ready!                                      ║');
console.log('╚════════════════════════════════════════════════════════════╝\n');
console.log(`📁 Location: .\\npm-dist-darwin-x64\\`);
console.log(`📦 Package: easytouch-darwin@${version}\n`);
console.log('🚀 To publish to NPM:');
console.log('   cd npm-dist-darwin-x64');
console.log('   npm publish --access public\n');
console.log('🧪 To test locally:');
console.log('   cd npm-dist-darwin-x64');
console.log('   chmod +x et');
console.log('   ./et --help\n');
