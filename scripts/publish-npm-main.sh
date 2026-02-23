#!/bin/bash

# EasyTouch NPM Publisher - Main Package
# Usage: ./publish-npm-main.sh <version>
# Example: ./publish-npm-main.sh 1.0.0

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
echo "║     EasyTouch NPM Publisher - Main Package               ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""

TEMP_DIR=$(mktemp -d /tmp/easytouch-npm-main-XXXXXX)
DIST_DIR="npm-dist-main"

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

# 1. Copy package template from npx/main
echo "📋 Copying package template from npx/main..."
if [ ! -f "$PROJECT_DIR/npx/main/package.json" ]; then
    echo "❌ Error: npx/main/package.json not found!"
    exit 1
fi

cp "$PROJECT_DIR/npx/main/package.json" "$TEMP_DIR/package.json"
cp "$PROJECT_DIR/npx/main/install.js" "$TEMP_DIR/install.js" 2>/dev/null || true
cp "$PROJECT_DIR/npx/main/test.js" "$TEMP_DIR/test.js" 2>/dev/null || true

# Update version
sed -i "s/\"version\": \"[^\"]*\"/\"version\": \"$VERSION\"/" "$TEMP_DIR/package.json"

# 2. Copy bin directory
echo "📋 Copying bin scripts..."
if [ -d "$PROJECT_DIR/npx/main/bin" ]; then
    cp -r "$PROJECT_DIR/npx/main/bin" "$TEMP_DIR/"
fi

# 3. Create README
echo "📋 Creating README.md..."
cp "$PROJECT_DIR/docs/NPM_TEST_GUIDE.md" "$TEMP_DIR/README.md" 2>/dev/null || true

# 4. Move to dist directory
echo "📦 Moving to distribution directory..."
if [ -d "$PROJECT_DIR/$DIST_DIR" ]; then
    rm -rf "$PROJECT_DIR/$DIST_DIR"
fi
mv "$TEMP_DIR" "$PROJECT_DIR/$DIST_DIR"

# Remove temp from cleanup since we moved it
trap - EXIT

echo ""
echo "╔════════════════════════════════════════════════════════════╗"
echo "║  ✅ NPM Main Package Ready!                                 ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "📁 Location: ./$DIST_DIR/"
echo "📦 Package: easytouch@$VERSION"
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
