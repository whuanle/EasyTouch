#!/bin/bash

set -e

echo "=========================================="
echo "EasyTouch Windows - Build Script"
echo "=========================================="
echo ""

# Check if dotnet is installed
if ! command -v dotnet &> /dev/null; then
    echo "Error: .NET SDK is not installed or not in PATH"
    echo "Please install .NET 10 SDK from https://dotnet.microsoft.com/download"
    exit 1
fi

echo "Cleaning previous builds..."
dotnet clean EasyTouch-Windows.csproj -c Release
rm -rf bin obj

echo ""
echo "Restoring packages..."
dotnet restore EasyTouch-Windows.csproj

echo ""
echo "Building project (AOT compilation)..."
dotnet publish EasyTouch-Windows.csproj -c Release -r win-x64 --self-contained true -p:PublishAot=true

if [ $? -ne 0 ]; then
    echo ""
    echo "Build failed!"
    exit 1
fi

echo ""
echo "=========================================="
echo "Build successful!"
echo "=========================================="
echo "Output: bin/Release/net10.0/win-x64/publish/et.exe"

# Copy to dist folder
mkdir -p dist
cp bin/Release/net10.0/win-x64/publish/et.exe dist/
cp README.md dist/
cp SKILL.md dist/
cp package.json dist/

echo ""
echo "Distribution files copied to: dist/"
ls -lh dist/

echo ""
echo "Done!"
