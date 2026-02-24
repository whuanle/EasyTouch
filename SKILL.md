---
name: easytouch-mcp
description: Cross-platform desktop automation via EasyTouch. Use this skill when users need mouse/keyboard automation, screenshots, window control, system info, clipboard, audio, and browser automation through EasyTouch CLI/MCP.
---

# EasyTouch MCP Skill

Use EasyTouch as a local automation runtime for desktop tasks.

## When To Use

- GUI automation on Windows/Linux/macOS
- Mouse/keyboard actions
- Screenshots and pixel inspection
- Window discovery and activation
- System and process inspection
- Clipboard and audio operations
- Browser automation (`browser_*`)

## Prerequisites

- EasyTouch binary is installed and available in `PATH` as `et`
- For browser automation: Playwright runtime available for your platform

## Quick Checks

```bash
et --version
et mouse_position
et --mcp
```

## MCP Config Example

```json
{
  "mcpServers": {
    "easytouch": {
      "command": "et",
      "args": ["--mcp"]
    }
  }
}
```

## References

- Project overview: `README.md`
- Skill docs: `skills/SKILLS.md`
- Browser setup: `skills/BROWSER_SETUP.md`
