#!/usr/bin/env node

const net = require('net');
const fs = require('fs');
const os = require('os');
const path = require('path');
const { execSync, spawn } = require('child_process');
let chromium;
let firefox;
let webkit;

function loadPlaywright() {
  try {
    return require('playwright');
  } catch {
    try {
      const globalRoot = execSync('npm root -g', {
        encoding: 'utf8',
        windowsHide: true
      }).trim();
      return require(path.join(globalRoot, 'playwright'));
    } catch {
      throw new Error(
        'Playwright package not found. Run: npm install -g playwright && npx playwright install chromium'
      );
    }
  }
}

const playwright = loadPlaywright();
chromium = playwright.chromium;
firefox = playwright.firefox;
webkit = playwright.webkit;

const DAEMON_FILE = path.join(os.tmpdir(), 'easytouch-playwright-daemon.json');
const browserTypeMap = {
  chromium,
  chrome: chromium,
  firefox,
  webkit,
  safari: webkit
};

const browsers = new Map();

function parseArg(args, key) {
  const idx = args.indexOf(key);
  if (idx >= 0 && idx + 1 < args.length) {
    return args[idx + 1];
  }
  return null;
}

function safeJsonParse(text) {
  try {
    return JSON.parse(text);
  } catch {
    return null;
  }
}

function writeJsonLine(socket, obj) {
  socket.write(JSON.stringify(obj) + '\n');
}

async function ensureDaemon() {
  if (fs.existsSync(DAEMON_FILE)) {
    const existing = safeJsonParse(fs.readFileSync(DAEMON_FILE, 'utf8'));
    if (existing && existing.port) {
      try {
        await sendRequest(existing.port, { command: 'ping', args: [] }, 800);
        return existing;
      } catch {}
    }
  }

  const child = spawn(process.execPath, [__filename, 'daemon'], {
    detached: true,
    stdio: 'ignore',
    windowsHide: true
  });
  child.unref();

  const deadline = Date.now() + 5000;
  while (Date.now() < deadline) {
    if (fs.existsSync(DAEMON_FILE)) {
      const created = safeJsonParse(fs.readFileSync(DAEMON_FILE, 'utf8'));
      if (created && created.port) {
        try {
          await sendRequest(created.port, { command: 'ping', args: [] }, 800);
          return created;
        } catch {}
      }
    }
    await new Promise((r) => setTimeout(r, 100));
  }

  throw new Error('Failed to start Playwright bridge daemon');
}

function sendRequest(port, payload, timeoutMs = 30000) {
  return new Promise((resolve, reject) => {
    const socket = net.createConnection({ host: '127.0.0.1', port }, () => {
      writeJsonLine(socket, payload);
    });

    let buffer = '';
    let done = false;

    const timer = setTimeout(() => {
      if (done) return;
      done = true;
      socket.destroy();
      reject(new Error('Bridge request timeout'));
    }, timeoutMs);

    socket.on('data', (chunk) => {
      buffer += chunk.toString();
      const lineEnd = buffer.indexOf('\n');
      if (lineEnd === -1) return;
      const line = buffer.slice(0, lineEnd).trim();
      if (!line || done) return;
      done = true;
      clearTimeout(timer);
      socket.end();
      const parsed = safeJsonParse(line);
      if (!parsed) {
        reject(new Error('Invalid bridge response'));
        return;
      }
      resolve(parsed);
    });

    socket.on('error', (err) => {
      if (done) return;
      done = true;
      clearTimeout(timer);
      reject(err);
    });
  });
}

function getLauncher(type) {
  const launcher = browserTypeMap[(type || 'chromium').toLowerCase()];
  return launcher || chromium;
}

async function getPrimaryPage(browser) {
  let context = browser.contexts()[0];
  if (!context) {
    context = await browser.newContext();
  }
  let page = context.pages()[0];
  if (!page) {
    page = await context.newPage();
  }
  return page;
}

async function runLaunch(args) {
  const browserId = parseArg(args, '--browser-id');
  if (!browserId) throw new Error('Missing --browser-id');

  const browserType = (parseArg(args, '--browser') || 'chromium').toLowerCase();
  const headless = args.includes('--headless');
  const executablePath = parseArg(args, '--executable');
  const url = parseArg(args, '--url');

  const launcher = getLauncher(browserType);
  const browser = await launcher.launch({
    headless,
    ...(executablePath ? { executablePath } : {})
  });

  const page = await getPrimaryPage(browser);
  if (url) {
    await page.goto(url, { waitUntil: 'load', timeout: 30000 });
  }

  browsers.set(browserId, {
    id: browserId,
    type: browserType,
    browser,
    createdAt: Date.now()
  });

  return {
    BrowserId: browserId,
    BrowserType: browserType,
    Version: browser.version()
  };
}

function getBrowserOrThrow(browserId) {
  const instance = browsers.get(browserId);
  if (!instance) throw new Error(`Browser not found: ${browserId}`);
  return instance;
}

async function runNavigate(args) {
  const browserId = parseArg(args, '--browser-id');
  const url = parseArg(args, '--url');
  if (!browserId || !url) throw new Error('Missing --browser-id or --url');

  const waitUntil = parseArg(args, '--wait-until') || 'load';
  const timeout = Number.parseInt(parseArg(args, '--timeout') || '30000', 10);

  const instance = getBrowserOrThrow(browserId);
  const page = await getPrimaryPage(instance.browser);
  const response = await page.goto(url, { waitUntil, timeout });

  return {
    Url: page.url(),
    Title: await page.title(),
    StatusCode: response ? response.status() : 0
  };
}

async function runClick(args) {
  const browserId = parseArg(args, '--browser-id');
  const selector = parseArg(args, '--selector');
  if (!browserId || !selector) throw new Error('Missing --browser-id or --selector');

  const selectorType = parseArg(args, '--selector-type') || 'css';
  const button = Number.parseInt(parseArg(args, '--button') || '0', 10);
  const clickCount = Number.parseInt(parseArg(args, '--click-count') || '1', 10);
  const timeout = Number.parseInt(parseArg(args, '--timeout') || '30000', 10);

  const buttonMap = ['left', 'middle', 'right'];
  const instance = getBrowserOrThrow(browserId);
  const page = await getPrimaryPage(instance.browser);
  const locator = getLocator(page, selector, selectorType);

  await locator.click({
    button: buttonMap[button] || 'left',
    clickCount,
    timeout
  });
  return { Message: 'OK' };
}

async function runFill(args) {
  const browserId = parseArg(args, '--browser-id');
  const selector = parseArg(args, '--selector');
  const value = parseArg(args, '--value') ?? '';
  if (!browserId || !selector) throw new Error('Missing --browser-id or --selector');

  const selectorType = parseArg(args, '--selector-type') || 'css';
  const timeout = Number.parseInt(parseArg(args, '--timeout') || '30000', 10);
  const noClear = args.includes('--no-clear');

  const instance = getBrowserOrThrow(browserId);
  const page = await getPrimaryPage(instance.browser);
  const locator = getLocator(page, selector, selectorType);

  if (!noClear) {
    await locator.clear({ timeout });
  }
  await locator.fill(value, { timeout });
  return { Message: 'OK' };
}

async function runFind(args) {
  const browserId = parseArg(args, '--browser-id');
  const selector = parseArg(args, '--selector');
  if (!browserId || !selector) throw new Error('Missing --browser-id or --selector');

  const selectorType = parseArg(args, '--selector-type') || 'css';
  const timeout = Number.parseInt(parseArg(args, '--timeout') || '5000', 10);

  const instance = getBrowserOrThrow(browserId);
  const page = await getPrimaryPage(instance.browser);
  const locator = getLocator(page, selector, selectorType);

  await locator.first.waitFor({ timeout, state: 'attached' }).catch(() => {});
  const count = await locator.count();
  if (count === 0) return { Found: false };

  const element = locator.first;
  const tagName = await element.evaluate((el) => el.tagName.toLowerCase());
  const text = await element.textContent();
  const value = await element.inputValue().catch(() => null);
  const bbox = await element.boundingBox();

  return {
    Found: true,
    TagName: tagName,
    Text: text,
    Value: value,
    BoundingBox: bbox
      ? { X: bbox.x, Y: bbox.y, Width: bbox.width, Height: bbox.height }
      : null
  };
}

async function runGetText(args) {
  const browserId = parseArg(args, '--browser-id');
  if (!browserId) throw new Error('Missing --browser-id');

  const selector = parseArg(args, '--selector');
  const selectorType = parseArg(args, '--selector-type') || 'css';

  const instance = getBrowserOrThrow(browserId);
  const page = await getPrimaryPage(instance.browser);
  if (!selector) {
    return { Text: await page.content(), Selector: null };
  }

  const locator = getLocator(page, selector, selectorType);
  return { Text: (await locator.textContent()) || '', Selector: selector };
}

async function runScreenshot(args) {
  const browserId = parseArg(args, '--browser-id');
  if (!browserId) throw new Error('Missing --browser-id');

  const output = parseArg(args, '--output');
  if (!output) throw new Error('Missing --output');

  const selector = parseArg(args, '--selector');
  const selectorType = parseArg(args, '--selector-type') || 'css';
  const type = (parseArg(args, '--type') || 'png').toLowerCase();
  const fullPage = args.includes('--full-page');
  const quality = parseArg(args, '--quality');

  const instance = getBrowserOrThrow(browserId);
  const page = await getPrimaryPage(instance.browser);

  const options = {
    path: output,
    type: type === 'jpeg' ? 'jpeg' : 'png'
  };
  if (quality && options.type === 'jpeg') {
    options.quality = Number.parseInt(quality, 10);
  }

  if (selector) {
    const locator = getLocator(page, selector, selectorType);
    await locator.screenshot(options);
  } else {
    await page.screenshot({ ...options, fullPage });
  }

  return { ImagePath: output, Width: 0, Height: 0 };
}

async function runEvaluate(args) {
  const browserId = parseArg(args, '--browser-id');
  const scriptFile = parseArg(args, '--script-file');
  if (!browserId || !scriptFile) throw new Error('Missing --browser-id or --script-file');

  const script = fs.readFileSync(scriptFile, 'utf8');
  const instance = getBrowserOrThrow(browserId);
  const page = await getPrimaryPage(instance.browser);
  const result = await page.evaluate(script);

  return {
    Result: result,
    ResultType: typeof result
  };
}

async function runWaitFor(args) {
  const browserId = parseArg(args, '--browser-id');
  const selector = parseArg(args, '--selector');
  if (!browserId || !selector) throw new Error('Missing --browser-id or --selector');

  const selectorType = parseArg(args, '--selector-type') || 'css';
  const state = parseArg(args, '--state') || 'visible';
  const timeout = Number.parseInt(parseArg(args, '--timeout') || '30000', 10);

  const instance = getBrowserOrThrow(browserId);
  const page = await getPrimaryPage(instance.browser);
  const locator = getLocator(page, selector, selectorType);
  await locator.waitFor({ state, timeout });

  return { Message: 'OK' };
}

async function runList() {
  const list = [];
  for (const [id, instance] of browsers) {
    let currentUrl = '';
    let currentTitle = '';
    let connected = false;
    try {
      connected = instance.browser.isConnected();
      if (connected) {
        const page = await getPrimaryPage(instance.browser);
        currentUrl = page.url();
        currentTitle = await page.title();
      }
    } catch {
      connected = false;
    }

    list.push({
      Id: id,
      Type: instance.type,
      Version: instance.browser.version(),
      CurrentUrl: currentUrl,
      CurrentTitle: currentTitle,
      IsConnected: connected
    });
  }
  return { Browsers: list };
}

async function runClose(args) {
  const browserId = parseArg(args, '--browser-id');
  if (!browserId) throw new Error('Missing --browser-id');

  const instance = browsers.get(browserId);
  if (!instance) {
    return { Message: 'OK' };
  }

  try {
    await instance.browser.close();
  } catch {}
  browsers.delete(browserId);
  return { Message: 'OK' };
}

function getLocator(page, selector, selectorType) {
  switch ((selectorType || 'css').toLowerCase()) {
    case 'xpath':
      return page.locator(`xpath=${selector}`);
    case 'text':
      return page.getByText(selector);
    case 'id':
      return page.locator(`#${selector}`);
    default:
      return page.locator(selector);
  }
}

async function executeCommand(command, args) {
  switch (command) {
    case 'ping':
      return { Message: 'pong' };
    case 'launch':
      return runLaunch(args);
    case 'list':
      return runList();
    case 'navigate':
      return runNavigate(args);
    case 'click':
      return runClick(args);
    case 'fill':
      return runFill(args);
    case 'find':
      return runFind(args);
    case 'get-text':
      return runGetText(args);
    case 'screenshot':
      return runScreenshot(args);
    case 'evaluate':
      return runEvaluate(args);
    case 'wait-for':
      return runWaitFor(args);
    case 'close':
      return runClose(args);
    default:
      throw new Error(`Unknown command: ${command}`);
  }
}

async function runDaemon() {
  const server = net.createServer((socket) => {
    let buffer = '';
    socket.on('data', async (chunk) => {
      buffer += chunk.toString();
      const lineEnd = buffer.indexOf('\n');
      if (lineEnd === -1) return;

      const line = buffer.slice(0, lineEnd).trim();
      buffer = '';
      const req = safeJsonParse(line);
      if (!req || !req.command) {
        writeJsonLine(socket, { ok: false, error: 'Invalid request' });
        return;
      }

      try {
        const data = await executeCommand(req.command, req.args || []);
        writeJsonLine(socket, { ok: true, data });
      } catch (err) {
        writeJsonLine(socket, { ok: false, error: err.message || String(err) });
      }
    });
  });

  server.listen(0, '127.0.0.1', () => {
    const port = server.address().port;
    fs.writeFileSync(DAEMON_FILE, JSON.stringify({ pid: process.pid, port }));
  });

  const shutdown = async () => {
    for (const instance of browsers.values()) {
      try {
        await instance.browser.close();
      } catch {}
    }
    try {
      fs.unlinkSync(DAEMON_FILE);
    } catch {}
    process.exit(0);
  };

  process.on('SIGINT', shutdown);
  process.on('SIGTERM', shutdown);
}

async function runClient(command, args) {
  const daemon = await ensureDaemon();
  const result = await sendRequest(daemon.port, { command, args });
  if (!result.ok) {
    throw new Error(result.error || 'Bridge command failed');
  }

  const data = result.data;
  if (data === undefined || data === null) {
    return;
  }
  if (typeof data === 'string') {
    console.log(data);
    return;
  }
  console.log(JSON.stringify(data));
}

async function main() {
  const args = process.argv.slice(2);
  const command = args[0];

  if (!command) {
    console.error('ERROR: Missing command');
    process.exit(1);
  }

  if (command === 'daemon') {
    await runDaemon();
    return;
  }

  if (command === '--version' || command === '-v') {
    console.log('EasyTouch Playwright Bridge v2.0.0');
    return;
  }

  try {
    await runClient(command, args.slice(1));
  } catch (err) {
    console.error(`ERROR: ${err.message || String(err)}`);
    process.exit(1);
  }
}

main();
