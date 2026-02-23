const { spawn } = require('child_process');
const path = require('path');

const etPath = path.join(__dirname, '..', 'EasyTouch-Windows', 'bin', 'Release', 'net10.0', 'win-x64', 'publish', 'et.exe');

function runCommand(args) {
    return new Promise((resolve) => {
        const child = spawn(etPath, args, {
            shell: true,
            windowsHide: true
        });

        let stdout = '';
        let stderr = '';

        child.stdout.on('data', (data) => {
            stdout += data.toString();
        });

        child.stderr.on('data', (data) => {
            stderr += data.toString();
        });

        child.on('close', (code) => {
            resolve({
                exitCode: code,
                output: stdout.trim(),
                error: stderr.trim()
            });
        });
    });
}

async function diagnose() {
    console.log('=== EasyTouch Audio Diagnostic ===\n');
    
    console.log('1. Testing volume_get:');
    const volResult = await runCommand(['volume_get']);
    console.log('Exit code:', volResult.exitCode);
    console.log('Output:', volResult.output);
    console.log('Error:', volResult.error || 'none');
    console.log();
    
    console.log('2. Testing volume_set:');
    const setResult = await runCommand(['volume_set', '--level', '50']);
    console.log('Exit code:', setResult.exitCode);
    console.log('Output:', setResult.output);
    console.log('Error:', setResult.error || 'none');
    console.log();
    
    console.log('3. Testing audio_devices:');
    const devResult = await runCommand(['audio_devices']);
    console.log('Exit code:', devResult.exitCode);
    console.log('Output:', devResult.output);
    console.log('Error:', devResult.error || 'none');
    console.log();
    
    // Try to parse JSON
    console.log('4. JSON Parsing test:');
    try {
        const data = JSON.parse(volResult.output);
        console.log('Parsed JSON:', JSON.stringify(data, null, 2));
        console.log('Has success:', data.success !== undefined || data.Success !== undefined);
        console.log('Has data:', data.data !== undefined || data.Data !== undefined);
        if (data.data || data.Data) {
            const inner = data.data || data.Data;
            console.log('Inner data:', inner);
            console.log('Level:', inner.level !== undefined ? inner.level : inner.Level);
        }
    } catch (e) {
        console.log('Parse error:', e.message);
    }
}

diagnose().catch(console.error);
