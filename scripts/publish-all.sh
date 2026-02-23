#!/bin/bash

# EasyTouch NPM Publisher - Platform Packages
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
echo "║   EasyTouch NPM Publisher - Platform Packages            ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "📦 Publishing version: $VERSION"
echo ""

# Detect platform
PLATFORM=$(uname -s)
echo "🖥️  Platform: $PLATFORM"
echo ""

cd "$PROJECT_DIR"

echo "📦 Building platform package..."
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

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║  ✅ Platform package build successful!                      ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "📁 Distribution directories:"
echo "   - npm-dist-win-x64/       (Windows: easytouch-windows)"
echo "   - npm-dist-linux-x64/     (Linux: easytouch-linux)"
echo "   - npm-dist-macos/         (macOS: easytouch-macos)"
echo ""
echo "🚀 To publish to NPM (current platform package):"
echo ""
echo "   cd npm-dist-win-x64 && npm publish --access public && cd .."
echo "   cd npm-dist-linux-x64 && npm publish --access public && cd .."
echo "   cd npm-dist-macos && npm publish --access public && cd .."
echo ""
echo "🧪 To test locally before publishing:"
echo "   cd npm-dist-linux-x64 && npm link"
echo "   et --help"
echo ""
