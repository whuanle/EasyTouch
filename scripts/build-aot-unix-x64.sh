#!/bin/bash

set -e

echo "=========================================="
echo "EasyTouch Build Script - All Platforms"
echo "=========================================="
echo ""

BUILD_TYPE=${1:-all}
ARCH=${2:-all}

build_windows() {
    echo "[1/4] Building Windows version..."
    cd EasyTouch-Windows
    if [ -f build.bat ]; then
        cmd //c build.bat
    else
        dotnet publish -c Release -r win-x64 --self-contained true -p:PublishAot=true
    fi
    mkdir -p ../skills/windows
    cp bin/Release/net10.0/win-x64/publish/et.exe ../skills/windows/ 2>/dev/null || true
    cp EasyTouch-Windows/SKILL.md ../skills/windows/ 2>/dev/null || true
    cd ..
}

build_linux() {
    echo "[2/4] Building Linux version..."
    cd EasyTouch-Linux
    dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true
    mkdir -p ../skills/linux
    cp bin/Release/net10.0/linux-x64/publish/et ../skills/linux/et-x64
    cd ..
    cp EasyTouch-Linux/SKILL.md ../skills/linux/ 2>/dev/null || true
}

build_mac() {
    echo "[3/4] Building macOS versions..."
    cd EasyTouch-Mac
    
    # Build Intel (x64) version
    echo "Building macOS Intel (x64)..."
    dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishAot=true -o ../../skills/mac/x64_temp
    
    # Build Apple Silicon (arm64) version
    echo "Building macOS Apple Silicon (arm64)..."
    dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishAot=true -o ../../skills/mac/arm64_temp
    
    # Organize output
    mkdir -p ../../skills/mac
    mv ../../skills/mac/x64_temp/et ../../skills/mac/et-x64
    mv ../../skills/mac/arm64_temp/et ../../skills/mac/et-arm64
    rm -rf ../../skills/mac/x64_temp ../../skills/mac/arm64_temp
    
    cd ..
    cp EasyTouch-Mac/SKILL.md ../skills/mac/ 2>/dev/null || true
}

case $BUILD_TYPE in
    all)
        build_windows
        build_linux
        build_mac
        ;;
    windows)
        build_windows
        ;;
    linux)
        build_linux
        ;;
    mac)
        build_mac
        ;;
    *)
        echo "Usage: $0 [all|windows|linux|mac]"
        exit 1
        ;;
esac

echo ""
echo "=========================================="
echo "Build completed!"
echo ""
echo "Output structure:"
echo "  skills/windows/et.exe    (Windows x64)"
echo "  skills/linux/et-x64      (Linux x64)"
echo "  skills/mac/et-x64        (macOS Intel)"
echo "  skills/mac/et-arm64      (macOS Apple Silicon)"
echo "=========================================="
