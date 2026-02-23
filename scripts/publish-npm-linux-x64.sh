#!/bin/bash

# EasyTouch NPM Publisher - Linux x64
# Usage: ./publish-npm-linux-x64.sh <version>
# Example: ./publish-npm-linux-x64.sh 1.0.0

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
echo "║     EasyTouch NPM Publisher - Linux x64                   ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

TEMP_DIR=$(mktemp -d /tmp/easytouch-npm-linux-x64-XXXXXX)
DIST_DIR="npm-dist-linux-x64"

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

# 1. Copy package template from npx/linux
echo "📋 Copying package template from npx/linux..."
if [ ! -f "$PROJECT_DIR/npx/linux/package.json" ]; then
    echo "❌ Error: npx/linux/package.json not found!"
    exit 1
fi

cp "$PROJECT_DIR/npx/linux/package.json" "$TEMP_DIR/package.json"
cp "$PROJECT_DIR/npx/linux/SKILL.md" "$TEMP_DIR/SKILL.md" 2>/dev/null || true

# Update version
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$VERSION\"/" "$TEMP_DIR/package.json"

# 2. Build AOT executable
echo "🔨 Building AOT executable for linux-x64..."
dotnet publish "$PROJECT_DIR/EasyTouch-Linux/EasyTouch-Linux.csproj" \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -p:PublishAot=true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=true \
    -p:TrimMode=full \
    -o "$TEMP_DIR"

# 3. Copy Playwright bridge script
echo "📋 Copying Playwright bridge script..."
if [ -f "$PROJECT_DIR/scripts/playwright-bridge.js" ]; then
    mkdir -p "$TEMP_DIR/scripts"
    cp "$PROJECT_DIR/scripts/playwright-bridge.js" "$TEMP_DIR/scripts/"
fi

# 4. Verify files
echo "✅ Verifying package contents..."
if [ ! -f "$TEMP_DIR/et" ]; then
    echo "❌ Error: 'et' binary not found after build!"
    exit 1
fi

# Make executable
chmod +x "$TEMP_DIR/et"

# 5. Move to dist directory
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
echo "📦 Package: easytouch-linux@$VERSION"
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
