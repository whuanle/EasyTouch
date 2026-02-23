#!/bin/bash

# EasyTouch Cross-Platform Test Suite Wrapper
# Usage: ./test-easytouch.sh [options]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
node "$SCRIPT_DIR/test-easytouch.js" "$@"
