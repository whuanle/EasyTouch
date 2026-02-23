# EasyTouch NPM 发布指南

## 发布流程概览

EasyTouch 使用平台特定的 NPM 包策略：
- `easytouch-windows` - Windows 平台
- `easytouch-linux` - Linux 平台
- `easytouch-macos` - macOS 平台
- `easytouch` (可选) - 通用包装器，自动安装对应平台包

## 环境准备

### 1. 注册 NPM 账号

如果没有 NPM 账号，先注册：

```bash
npm adduser
```

或登录：

```bash
npm login
```

### 2. 验证登录

```bash
npm whoami
```

## 构建和发布

### Windows 平台

**在 Windows 机器上执行：**

```cmd
# 构建包
scripts\build-npm.bat 1.0.0

# 进入包目录
cd npm-package-windows

# 发布（公开访问）
npm publish --access public
```

### Linux 平台

**在 Linux 机器上执行：**

```bash
# 构建包
./scripts/build-npm-linux-macos.sh 1.0.0 linux

# 进入包目录
cd npm-package-linux

# 发布
npm publish --access public
```

### macOS 平台

**在 macOS 机器上执行：**

```bash
# 构建包  
./scripts/build-npm-linux-macos.sh 1.0.0 macos

# 进入包目录
cd npm-package-macos

# 发布
npm publish --access public
```

## 版本号规范

使用语义化版本号（SemVer）：

- `MAJOR.MINOR.PATCH`
- 例如：`1.0.0`, `1.0.1`, `1.1.0`, `2.0.0`

版本更新规则：
- **MAJOR**: 破坏性变更
- **MINOR**: 新功能，向后兼容
- **PATCH**: Bug 修复

## 发布前检查清单

- [ ] 更新版本号
- [ ] 运行测试确保功能正常
- [ ] 更新 CHANGELOG.md
- [ ] 更新文档（如有变更）
- [ ] 构建成功无错误
- [ ] 在本地测试安装的包

## 本地测试

发布前先在本地测试：

```bash
# Windows
cd npm-package-windows
npm link
et --version

# Linux/macOS
cd npm-package-linux  # 或 npm-package-macos
npm link
et --version
```

测试完成后取消链接：

```bash
npm unlink -g easytouch-windows  # 对应包名
```

## 发布通用包（可选）

可以创建一个通用包 `easytouch`，它会根据平台自动安装对应的包：

```json
{
  "name": "easytouch",
  "version": "1.0.0",
  "optionalDependencies": {
    "easytouch-windows": "1.0.0",
    "easytouch-linux": "1.0.0",
    "easytouch-macos": "1.0.0"
  },
  "bin": {
    "et": "./bin/et.js"
  }
}
```

## GitHub Releases

除了 NPM，还应该将二进制文件发布到 GitHub Releases：

1. 创建新标签：
   ```bash
   git tag -a v1.0.0 -m "Release version 1.0.0"
   git push origin v1.0.0
   ```

2. 在 GitHub 上创建 Release，上传以下文件：
   - `et-windows-x64.exe`
   - `et-linux-x64`
   - `et-macos-x64`

3. NPM 包的安装脚本会从 GitHub Releases 下载对应文件

## 常见问题

### 1. 发布失败：权限错误

确保已登录并有权限：
```bash
npm login
npm whoami
```

### 2. 包名已被占用

如果包名已被占用，需要选择新的包名或在 package.json 中使用 scope：
```json
{
  "name": "@yourusername/easytouch-windows"
}
```

### 3. 版本冲突

如果该版本已存在，需要先更新版本号：
```bash
npm version patch  # 或 minor, major
```

## 自动化发布（CI/CD）

可以使用 GitHub Actions 自动化发布流程：

```yaml
# .github/workflows/release.yml
name: Release
on:
  push:
    tags:
      - 'v*'
jobs:
  build-and-publish:
    runs-on: ${{ matrix.os }}
    strategy:
      matrix:
        os: [windows-latest, ubuntu-latest, macos-latest]
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
      - name: Setup Node
        uses: actions/setup-node@v2
        with:
          registry-url: 'https://registry.npmjs.org'
      - name: Build and Publish
        run: |
          # 根据 OS 执行对应的构建和发布脚本
        env:
          NODE_AUTH_TOKEN: ${{ secrets.NPM_TOKEN }}
```

## 相关链接

- [NPM 文档](https://docs.npmjs.com/)
- [语义化版本](https://semver.org/)
- [package.json 规范](https://docs.npmjs.com/cli/v8/configuring-npm/package-json)

## 支持

如有问题，请提交 Issue 到：[GitHub Issues](https://github.com/yourusername/easytouch/issues)
