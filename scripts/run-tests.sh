#!/bin/bash

# EasyTouch Cross-Platform Test Runner
# Works on Linux and macOS

set -e

echo "================================================"
echo "    EasyTouch Cross-Platform Test Runner"
echo "================================================"
echo ""

# 检测操作系统
PLATFORM="Unknown"
TEST_PROJECT=""
MAIN_PROJECT=""
RUNTIME_ID=""

if [[ "$OSTYPE" == "linux-gnu"* ]]; then
    PLATFORM="Linux"
    TEST_PROJECT="EasyTouch.Tests.Linux"
    MAIN_PROJECT="EasyTouch-Linux"
    RUNTIME_ID="linux-x64"
elif [[ "$OSTYPE" == "darwin"* ]]; then
    PLATFORM="Mac"
    TEST_PROJECT="EasyTouch.Tests.Mac"
    MAIN_PROJECT="EasyTouch-Mac"
    # 检测架构
    if [[ $(uname -m) == "arm64" ]]; then
        RUNTIME_ID="osx-arm64"
    else
        RUNTIME_ID="osx-x64"
    fi
else
    echo "Error: Unsupported operating system: $OSTYPE"
    echo "This script supports Linux and macOS only."
    echo "For Windows, please use run-tests.bat"
    exit 1
fi

echo "Platform detected: $PLATFORM"
echo "Architecture: $(uname -m)"
echo "Test project: $TEST_PROJECT"
echo "Main project: $MAIN_PROJECT"
echo "Runtime ID: $RUNTIME_ID"
echo ""

# 检查 .NET SDK
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK not found. Please install .NET 10.0 SDK."
    exit 1
fi

echo ".NET SDK version:"
dotnet --version
echo ""

# 设置配置
CONFIG="${1:-Release}"

echo "Configuration: $CONFIG"
echo ""

# 获取脚本所在目录的绝对路径
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR/.."

# 构建主项目
echo "[1/3] Building main project: $MAIN_PROJECT..."
dotnet build "$MAIN_PROJECT/$MAIN_PROJECT.csproj" -c "$CONFIG"
echo "    ✓ Build successful"
echo ""

# 发布 AOT 版本（测试需要）
echo "[2/3] Publishing AOT executable..."
if dotnet publish "$MAIN_PROJECT/$MAIN_PROJECT.csproj" -c "$CONFIG" -r "$RUNTIME_ID" --self-contained true -p:PublishAot=true; then
    echo "    ✓ AOT publish successful"
else
    echo "    ⚠ Warning: AOT publish failed, tests may use Debug build"
fi
echo ""

# 运行测试
echo "[3/3] Running tests: $TEST_PROJECT..."
if dotnet test "$TEST_PROJECT/$TEST_PROJECT.csproj" -c "$CONFIG" --verbosity normal; then
    echo ""
    echo "================================================"
    echo "    All tests PASSED ✓"
    echo "================================================"
else
    echo ""
    echo "================================================"
    echo "    Tests FAILED"
    echo "================================================"
    exit 1
fi
