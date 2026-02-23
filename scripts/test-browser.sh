#!/bin/bash

# EasyTouch Browser Automation Tests Wrapper
# Usage: ./test-browser.sh [options]

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
node "$SCRIPT_DIR/test-browser.js" "$@"
