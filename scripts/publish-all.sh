#!/bin/bash

# EasyTouch NPM Publisher - All Packages
# Usage: ./publish-all.sh <version>
# Example: ./publish-all.sh 1.0.0

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_DIR="$(dirname "$SCRIPT_DIR")"
VERSION="${1:-}"

if [ -z "$VERSION" ]; then
    echo "Usage: $0 <version>"
    echo "Example: $0 1.0.0"
    exit 1
fi

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║     EasyTouch NPM Publisher - All Packages               ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "📦 Publishing version: $VERSION"
echo ""

# Detect platform
PLATFORM=$(uname -s)
echo "🖥️  Platform: $PLATFORM"
echo ""

# Build all platform binaries first
echo "🔨 Building platform binaries..."
echo ""

cd "$PROJECT_DIR"

# Build Windows (if on Windows or cross-compilation available)
if [[ "$PLATFORM" == MINGW* ]] || [[ "$PLATFORM" == MSYS* ]] || [[ "$PLATFORM" == CYGWIN* ]]; then
    echo "Building Windows binary..."
    "$SCRIPT_DIR/publish-npm-win-x64.bat" "$VERSION"
fi

# Build Linux (if on Linux)
if [[ "$PLATFORM" == Linux ]]; then
    echo "Building Linux binary..."
    "$SCRIPT_DIR/publish-npm-linux-x64.sh" "$VERSION"
fi

# Build macOS (if on macOS)
if [[ "$PLATFORM" == Darwin ]]; then
    echo "Building macOS binary..."
    "$SCRIPT_DIR/publish-npm-macos-x64.sh" "$VERSION"
fi

echo ""
echo "📦 Building platform packages..."
echo ""

# Build platform-specific packages
if [ -f "$SCRIPT_DIR/publish-npm-win-x64.bat" ] && [[ "$PLATFORM" == MINGW* || "$PLATFORM" == MSYS* || "$PLATFORM" == CYGWIN* ]]; then
    echo "📦 Building Windows package..."
    "$SCRIPT_DIR/publish-npm-win-x64.bat" "$VERSION"
fi

if [ -f "$SCRIPT_DIR/publish-npm-linux-x64.sh" ] && [[ "$PLATFORM" == Linux ]]; then
    echo "📦 Building Linux package..."
    "$SCRIPT_DIR/publish-npm-linux-x64.sh" "$VERSION"
fi

if [ -f "$SCRIPT_DIR/publish-npm-macos-x64.sh" ] && [[ "$PLATFORM" == Darwin ]]; then
    echo "📦 Building macOS package..."
    "$SCRIPT_DIR/publish-npm-macos-x64.sh" "$VERSION"
fi

# Build main package (platform-agnostic)
echo ""
echo "📦 Building main package..."
"$SCRIPT_DIR/publish-npm-main.sh" "$VERSION"

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║  ✅ All packages built successfully!                        ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "📁 Distribution directories:"
echo "   - npm-dist-main/          (Main package: easytouch)"
echo "   - npm-dist-win-x64/       (Windows: easytouch-windows)"
echo "   - npm-dist-linux-x64/     (Linux: easytouch-linux)"
echo "   - npm-dist-macos/         (macOS: easytouch-macos)"
echo ""
echo "🚀 To publish all packages to NPM:"
echo ""
echo "   # 1. Publish platform packages first"
echo "   cd npm-dist-win-x64 && npm publish --access public && cd .."
echo "   cd npm-dist-linux-x64 && npm publish --access public && cd .."
echo "   cd npm-dist-macos && npm publish --access public && cd .."
echo ""
echo "   # 2. Then publish main package"
echo "   cd npm-dist-main && npm publish --access public"
echo ""
echo "🧪 To test locally before publishing:"
echo "   cd npm-dist-main && npm link"
echo "   et --help"
echo ""
