#!/bin/bash

# EasyTouch NPM Publisher - macOS x64 & arm64
# Usage: ./publish-npm-macos-x64.sh <version>
# Example: ./publish-npm-macos-x64.sh 1.0.0

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
echo "║     EasyTouch NPM Publisher - macOS x64 & arm64          ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

TEMP_DIR=$(mktemp -d /tmp/easytouch-npm-macos-XXXXXX)
DIST_DIR="npm-dist-macos"

echo "📦 Version: $VERSION"
echo "📁 Temp directory: $TEMP_DIR"
echo ""

# Cleanup function
cleanup() {
    if [ -d "$TEMP_DIR" ]; then
        rm -rf "$TEMP_DIR"
    fi
}
trap cleanup EXIT

# 1. Copy package template from npx/mac
echo "📋 Copying package template from npx/mac..."
if [ ! -f "$PROJECT_DIR/npx/mac/package.json" ]; then
    echo "❌ Error: npx/mac/package.json not found!"
    exit 1
fi

cp "$PROJECT_DIR/npx/mac/package.json" "$TEMP_DIR/package.json"
cp "$PROJECT_DIR/npx/mac/install.js" "$TEMP_DIR/install.js" 2>/dev/null || true

# Update version
sed -i '' "s/\"version\": \"[^\"]*\"/\"version\": \"$VERSION\"/" "$TEMP_DIR/package.json" 2>/dev/null || \
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$VERSION\"/" "$TEMP_DIR/package.json"

# 2. Build AOT executables for both architectures
echo "🔨 Building AOT executable for macOS x64..."
dotnet publish "$PROJECT_DIR/EasyTouch-Mac/EasyTouch-Mac.csproj" \
    -c Release \
    -r osx-x64 \
    --self-contained true \
    -p:PublishAot=true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=full \
    -o "$TEMP_DIR/bin-x64"

echo "🔨 Building AOT executable for macOS arm64..."
dotnet publish "$PROJECT_DIR/EasyTouch-Mac/EasyTouch-Mac.csproj" \
    -c Release \
    -r osx-arm64 \
    --self-contained true \
    -p:PublishAot=true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=full \
    -o "$TEMP_DIR/bin-arm64"

# 3. Setup bin directory with architecture-specific binaries
echo "📋 Setting up binaries..."
mkdir -p "$TEMP_DIR/bin"
mv "$TEMP_DIR/bin-x64/et" "$TEMP_DIR/bin/et-x64"
mv "$TEMP_DIR/bin-arm64/et" "$TEMP_DIR/bin/et-arm64"
rm -rf "$TEMP_DIR/bin-x64" "$TEMP_DIR/bin-arm64"

# Make executables
chmod +x "$TEMP_DIR/bin/et-x64"
chmod +x "$TEMP_DIR/bin/et-arm64"

# 4. Copy Playwright bridge script
echo "📋 Copying Playwright bridge script..."
if [ -f "$PROJECT_DIR/scripts/playwright-bridge.js" ]; then
    mkdir -p "$TEMP_DIR/scripts"
    cp "$PROJECT_DIR/scripts/playwright-bridge.js" "$TEMP_DIR/scripts/"
fi

# 5. Copy SKILL.md if exists
if [ -f "$PROJECT_DIR/npx/mac/SKILL.md" ]; then
    cp "$PROJECT_DIR/npx/mac/SKILL.md" "$TEMP_DIR/SKILL.md"
fi

# 6. Verify files
echo "✅ Verifying package contents..."
if [ ! -f "$TEMP_DIR/bin/et-x64" ]; then
    echo "❌ Error: 'et-x64' binary not found after build!"
    exit 1
fi
if [ ! -f "$TEMP_DIR/bin/et-arm64" ]; then
    echo "❌ Error: 'et-arm64' binary not found after build!"
    exit 1
fi

# 7. Move to dist directory
echo "📦 Moving to distribution directory..."
if [ -d "$PROJECT_DIR/$DIST_DIR" ]; then
    rm -rf "$PROJECT_DIR/$DIST_DIR"
fi
mv "$TEMP_DIR" "$PROJECT_DIR/$DIST_DIR"

# Remove temp from cleanup since we moved it
trap - EXIT

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║  ✅ NPM Package Ready!                                      ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "📁 Location: ./$DIST_DIR/"
echo "📦 Package: easytouch-macos@$VERSION"
echo ""
echo "🚀 To publish to NPM:"
echo "   cd $DIST_DIR"
echo "   npm publish --access public"
echo ""
echo "🧪 To test locally:"
echo "   cd $DIST_DIR"
echo "   npm link"
echo "   et --help"
echo ""
