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
    cp SKILL.md ../skills/windows/SKILL.md 2>/dev/null || true
    cp ../README.md ../skills/README.md 2>/dev/null || true
    rm -f ../skills/windows/et.exe ../skills/windows/et-x64 ../skills/windows/et 2>/dev/null || true
    cd ..
}

build_linux() {
    echo "[2/4] Building Linux version..."
    cd EasyTouch-Linux
    dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishAot=true
    mkdir -p ../skills/linux
    cp SKILL.md ../skills/linux/SKILL.md 2>/dev/null || true
    cp ../README.md ../skills/README.md 2>/dev/null || true
    rm -f ../skills/linux/et ../skills/linux/et-x64 ../skills/linux/et.exe 2>/dev/null || true
    cd ..
}

build_mac() {
    echo "[3/4] Building macOS versions..."
    cd EasyTouch-Mac
    
    # Build Intel (x64) version
    echo "Building macOS Intel (x64)..."
    dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishAot=true
    
    # Build Apple Silicon (arm64) version
    echo "Building macOS Apple Silicon (arm64)..."
    dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishAot=true

    mkdir -p ../skills/mac
    cp SKILL.md ../skills/mac/SKILL.md 2>/dev/null || true
    cp ../README.md ../skills/README.md 2>/dev/null || true
    rm -f ../skills/mac/et ../skills/mac/et-x64 ../skills/mac/et-arm64 ../skills/mac/et.exe 2>/dev/null || true
    cd ..
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
echo "Skills docs synced:"
echo "  skills/README.md         (from root README.md)"
echo "  skills/windows/SKILL.md  (Windows docs)"
echo "  skills/linux/SKILL.md    (Linux docs)"
echo "  skills/mac/SKILL.md      (macOS docs)"
echo "=========================================="
