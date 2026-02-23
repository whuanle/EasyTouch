#!/usr/bin/env node

/**
 * EasyTouch Playwright Bridge
 * 
 * 这个脚本作为 EasyTouch 和 Playwright 之间的桥梁
 * EasyTouch 通过调用这个脚本来使用 Playwright 功能
 * 
 * 使用方法:
 *   node playwright-bridge.js <command> [options]
 */

const { chromium, firefox, webkit } = require('playwright');
const fs = require('fs');
const path = require('path');

// 存储浏览器实例
const browsers = new Map();

async function main() {
    const args = process.argv.slice(2);
    const command = args[0];

    try {
        switch (command) {
            case 'launch':
                await launchBrowser(args);
                break;
            case 'navigate':
                await navigate(args);
                break;
            case 'click':
                await clickElement(args);
                break;
            case 'fill':
                await fillElement(args);
                break;
            case 'find':
                await findElement(args);
                break;
            case 'get-text':
                await getText(args);
                break;
            case 'screenshot':
                await screenshot(args);
                break;
            case 'evaluate':
                await evaluateScript(args);
                break;
            case 'wait-for':
                await waitForElement(args);
                break;
            case 'page-info':
                await getPageInfo(args);
                break;
            case 'close':
                await closeBrowser(args);
                break;
            case 'status':
                await checkStatus(args);
                break;
            case 'go-back':
                await goBack(args);
                break;
            case 'go-forward':
                await goForward(args);
                break;
            case 'reload':
                await reload(args);
                break;
            case 'scroll':
                await scroll(args);
                break;
            case 'select':
                await selectOption(args);
                break;
            case 'upload':
                await uploadFile(args);
                break;
            case 'get-cookies':
                await getCookies(args);
                break;
            case 'set-cookie':
                await setCookie(args);
                break;
            case 'clear-cookies':
                await clearCookies(args);
                break;
            case 'route':
                await addRoute(args);
                break;
            case 'unroute':
                await removeRoute(args);
                break;
            case '--version':
            case '-v':
                console.log('EasyTouch Playwright Bridge v1.0.0');
                break;
            default:
                console.error(`ERROR: Unknown command: ${command}`);
                console.error('Usage: node playwright-bridge.js <command> [options]');
                process.exit(1);
        }
    } catch (error) {
        console.error(`ERROR: ${error.message}`);
        process.exit(1);
    }
}

async function launchBrowser(args) {
    const browserType = getArg(args, '--browser') || 'chromium';
    const browserId = getArg(args, '--browser-id');
    const headless = args.includes('--headless');
    const executablePath = getArg(args, '--executable');
    const userDataDir = getArg(args, '--user-data-dir');

    const browserTypeMap = {
        'chromium': chromium,
        'chrome': chromium,
        'firefox': firefox,
        'webkit': webkit,
        'safari': webkit
    };

    const browserLauncher = browserTypeMap[browserType.toLowerCase()] || chromium;
    
    const launchOptions = {
        headless: headless,
        ...(executablePath && { executablePath })
    };

    const browser = await browserLauncher.launch(launchOptions);
    const context = await browser.newContext({
        ...(userDataDir && { userDataDir })
    });
    const page = await context.newPage();

    browsers.set(browserId, { browser, context, page });

    console.log(browser.version());
}

async function navigate(args) {
    const browserId = getArg(args, '--browser-id');
    const url = getArg(args, '--url');
    const waitUntil = getArg(args, '--wait-until') || 'load';
    const timeout = parseInt(getArg(args, '--timeout')) || 30000;

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const response = await instance.page.goto(url, {
        waitUntil,
        timeout
    });

    const result = {
        Url: instance.page.url(),
        Title: await instance.page.title(),
        StatusCode: response?.status() || 0
    };

    console.log(JSON.stringify(result));
}

async function clickElement(args) {
    const browserId = getArg(args, '--browser-id');
    const selector = getArg(args, '--selector');
    const selectorType = getArg(args, '--selector-type') || 'css';
    const button = parseInt(getArg(args, '--button')) || 0;
    const clickCount = parseInt(getArg(args, '--click-count')) || 1;
    const timeout = parseInt(getArg(args, '--timeout')) || 30000;

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const locator = getLocator(instance.page, selector, selectorType);
    
    const buttonMap = ['left', 'middle', 'right'];
    
    await locator.click({
        button: buttonMap[button] || 'left',
        clickCount,
        timeout
    });

    console.log('OK');
}

async function fillElement(args) {
    const browserId = getArg(args, '--browser-id');
    const selector = getArg(args, '--selector');
    const value = getArg(args, '--value');
    const selectorType = getArg(args, '--selector-type') || 'css';
    const noClear = args.includes('--no-clear');
    const timeout = parseInt(getArg(args, '--timeout')) || 30000;

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const locator = getLocator(instance.page, selector, selectorType);

    if (!noClear) {
        await locator.clear({ timeout });
    }

    await locator.fill(value, { timeout });
    console.log('OK');
}

async function findElement(args) {
    const browserId = getArg(args, '--browser-id');
    const selector = getArg(args, '--selector');
    const selectorType = getArg(args, '--selector-type') || 'css';
    const timeout = parseInt(getArg(args, '--timeout')) || 5000;

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    try {
        const locator = getLocator(instance.page, selector, selectorType);
        
        const count = await locator.count();
        if (count === 0) {
            console.log(JSON.stringify({ Found: false }));
            return;
        }

        const element = locator.first;
        const tagName = await element.evaluate(el => el.tagName.toLowerCase());
        const text = await element.textContent();
        const value = await element.inputValue().catch(() => null);
        const bbox = await element.boundingBox();

        const result = {
            Found: true,
            TagName: tagName,
            Text: text,
            Value: value,
            BoundingBox: bbox ? {
                X: bbox.x,
                Y: bbox.y,
                Width: bbox.width,
                Height: bbox.height
            } : null
        };

        console.log(JSON.stringify(result));
    } catch (error) {
        console.log(JSON.stringify({ Found: false }));
    }
}

async function getText(args) {
    const browserId = getArg(args, '--browser-id');
    const selector = getArg(args, '--selector');
    const selectorType = getArg(args, '--selector-type') || 'css';

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    if (selector) {
        const locator = getLocator(instance.page, selector, selectorType);
        const text = await locator.textContent();
        console.log(text || '');
    } else {
        const html = await instance.page.content();
        console.log(html);
    }
}

async function screenshot(args) {
    const browserId = getArg(args, '--browser-id');
    const output = getArg(args, '--output');
    const type = getArg(args, '--type') || 'png';
    const selector = getArg(args, '--selector');
    const selectorType = getArg(args, '--selector-type') || 'css';
    const fullPage = args.includes('--full-page');
    const quality = parseInt(getArg(args, '--quality'));

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const options = {
        path: output,
        type: type === 'jpeg' ? 'jpeg' : 'png',
        ...(quality && type === 'jpeg' && { quality })
    };

    if (selector) {
        const locator = getLocator(instance.page, selector, selectorType);
        await locator.screenshot(options);
    } else {
        await instance.page.screenshot({
            ...options,
            fullPage
        });
    }

    console.log('OK');
}

async function evaluateScript(args) {
    const browserId = getArg(args, '--browser-id');
    const scriptFile = getArg(args, '--script-file');
    const scriptArgs = getArg(args, '--args');

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const script = fs.readFileSync(scriptFile, 'utf-8');
    const parsedArgs = scriptArgs ? JSON.parse(scriptArgs) : [];

    const result = await instance.page.evaluate(script, ...parsedArgs);
    console.log(JSON.stringify(result));
}

async function waitForElement(args) {
    const browserId = getArg(args, '--browser-id');
    const selector = getArg(args, '--selector');
    const selectorType = getArg(args, '--selector-type') || 'css';
    const state = getArg(args, '--state') || 'visible';
    const timeout = parseInt(getArg(args, '--timeout')) || 30000;

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const locator = getLocator(instance.page, selector, selectorType);

    const stateMap = {
        'visible': 'visible',
        'hidden': 'hidden',
        'attached': 'attached',
        'detached': 'detached'
    };

    await locator.waitFor({
        state: stateMap[state] || 'visible',
        timeout
    });

    console.log('OK');
}

async function getPageInfo(args) {
    const browserId = getArg(args, '--browser-id');

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const viewport = instance.page.viewportSize();
    const scrollX = await instance.page.evaluate(() => window.scrollX);
    const scrollY = await instance.page.evaluate(() => window.scrollY);
    const pageWidth = await instance.page.evaluate(() => document.documentElement.scrollWidth);
    const pageHeight = await instance.page.evaluate(() => document.documentElement.scrollHeight);

    const result = {
        Url: instance.page.url(),
        Title: await instance.page.title(),
        ScrollX: scrollX,
        ScrollY: scrollY,
        ViewportWidth: viewport?.width || 0,
        ViewportHeight: viewport?.height || 0,
        PageWidth: pageWidth,
        PageHeight: pageHeight
    };

    console.log(JSON.stringify(result));
}

async function closeBrowser(args) {
    const browserId = getArg(args, '--browser-id');
    const force = args.includes('--force');

    const instance = browsers.get(browserId);
    if (!instance) {
        console.log('OK');
        return;
    }

    try {
        await instance.page.close();
        await instance.context.close();
        await instance.browser.close();
    } catch (error) {
        if (!force) throw error;
    }

    browsers.delete(browserId);
    console.log('OK');
}

async function checkStatus(args) {
    const browserId = getArg(args, '--browser-id');
    
    const instance = browsers.get(browserId);
    if (!instance) {
        console.error('ERROR: Browser not found');
        process.exit(1);
    }

    const isConnected = instance.browser.isConnected();
    if (isConnected) {
        console.log('OK');
    } else {
        console.error('ERROR: Browser disconnected');
        process.exit(1);
    }
}

function getLocator(page, selector, selectorType) {
    switch (selectorType.toLowerCase()) {
        case 'xpath':
            return page.locator(`xpath=${selector}`);
        case 'text':
            return page.getByText(selector);
        case 'id':
            return page.locator(`#${selector}`);
        case 'css':
        default:
            return page.locator(selector);
    }
}

function getArg(args, flag) {
    const index = args.indexOf(flag);
    if (index !== -1 && index + 1 < args.length) {
        return args[index + 1];
    }
    return null;
}

// ==================== 新增功能 ====================

async function goBack(args) {
    const browserId = getArg(args, '--browser-id');
    const timeout = parseInt(getArg(args, '--timeout')) || 30000;

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    await instance.page.goBack({ timeout });
    console.log('OK');
}

async function goForward(args) {
    const browserId = getArg(args, '--browser-id');
    const timeout = parseInt(getArg(args, '--timeout')) || 30000;

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    await instance.page.goForward({ timeout });
    console.log('OK');
}

async function reload(args) {
    const browserId = getArg(args, '--browser-id');
    const timeout = parseInt(getArg(args, '--timeout')) || 30000;

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    await instance.page.reload({ timeout });
    console.log('OK');
}

async function scroll(args) {
    const browserId = getArg(args, '--browser-id');
    const x = parseInt(getArg(args, '--x')) || 0;
    const y = parseInt(getArg(args, '--y')) || 0;
    const selector = getArg(args, '--selector');
    const selectorType = getArg(args, '--selector-type') || 'css';
    const behavior = getArg(args, '--behavior') || 'auto';

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    if (selector) {
        const locator = getLocator(instance.page, selector, selectorType);
        await locator.scrollIntoViewIfNeeded();
    } else {
        await instance.page.evaluate(([scrollX, scrollY, scrollBehavior]) => {
            window.scrollBy({
                left: scrollX,
                top: scrollY,
                behavior: scrollBehavior
            });
        }, [x, y, behavior]);
    }

    console.log('OK');
}

async function selectOption(args) {
    const browserId = getArg(args, '--browser-id');
    const selector = getArg(args, '--selector');
    const selectorType = getArg(args, '--selector-type') || 'css';
    const values = getArg(args, '--values')?.split(',') || [];

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const locator = getLocator(instance.page, selector, selectorType);
    
    if (values.length === 1) {
        await locator.selectOption(values[0]);
    } else {
        await locator.selectOption(values);
    }

    console.log('OK');
}

async function uploadFile(args) {
    const browserId = getArg(args, '--browser-id');
    const selector = getArg(args, '--selector');
    const selectorType = getArg(args, '--selector-type') || 'css';
    const files = getArg(args, '--files')?.split(',') || [];

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const locator = getLocator(instance.page, selector, selectorType);
    await locator.setInputFiles(files);

    console.log('OK');
}

async function getCookies(args) {
    const browserId = getArg(args, '--browser-id');
    const url = getArg(args, '--url');

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const cookies = await instance.context.cookies(url ? [url] : undefined);
    console.log(JSON.stringify(cookies));
}

async function setCookie(args) {
    const browserId = getArg(args, '--browser-id');
    const name = getArg(args, '--name');
    const value = getArg(args, '--value');
    const domain = getArg(args, '--domain');
    const path = getArg(args, '--path') || '/';
    const expires = getArg(args, '--expires') ? parseInt(getArg(args, '--expires')) : undefined;
    const httpOnly = args.includes('--http-only');
    const secure = args.includes('--secure');
    const sameSite = getArg(args, '--same-site');

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const cookie = {
        name,
        value,
        domain: domain || '',
        path,
        ...(expires && { expires }),
        httpOnly,
        secure,
        ...(sameSite && { sameSite })
    };

    await instance.context.addCookies([cookie]);
    console.log('OK');
}

async function clearCookies(args) {
    const browserId = getArg(args, '--browser-id');

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    await instance.context.clearCookies();
    console.log('OK');
}

// 存储路由处理器
const routeHandlers = new Map();

async function addRoute(args) {
    const browserId = getArg(args, '--browser-id');
    const url = getArg(args, '--url');
    const action = getArg(args, '--action') || 'abort';
    const statusCode = getArg(args, '--status-code') ? parseInt(getArg(args, '--status-code')) : 200;
    const body = getArg(args, '--body');
    const contentType = getArg(args, '--content-type') || 'text/plain';

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const routeId = `route_${Date.now()}`;
    
    await instance.page.route(url, async (route) => {
        switch (action) {
            case 'abort':
                await route.abort();
                break;
            case 'fulfill':
                await route.fulfill({
                    status: statusCode,
                    body: body || '',
                    contentType
                });
                break;
            case 'continue':
            default:
                await route.continue();
                break;
        }
    });

    routeHandlers.set(routeId, { browserId, url });
    console.log(JSON.stringify({ RouteId: routeId }));
}

async function removeRoute(args) {
    const browserId = getArg(args, '--browser-id');
    const routeId = getArg(args, '--route-id');

    const instance = browsers.get(browserId);
    if (!instance) throw new Error('Browser not found');

    const handler = routeHandlers.get(routeId);
    if (handler) {
        await instance.page.unroute(handler.url);
        routeHandlers.delete(routeId);
    }

    console.log('OK');
}

// 进程退出时关闭所有浏览器
process.on('exit', async () => {
    for (const [id, instance] of browsers) {
        try {
            await instance.browser.close();
        } catch (e) {}
    }
});

process.on('SIGINT', async () => {
    for (const [id, instance] of browsers) {
        try {
            await instance.browser.close();
        } catch (e) {}
    }
    process.exit(0);
});

main().catch(error => {
    console.error(`ERROR: ${error.message}`);
    process.exit(1);
});
