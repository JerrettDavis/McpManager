#!/usr/bin/env node
/**
 * Captures screenshots of all key MCPManager pages using Playwright.
 * Usage: node scripts/capture-screenshots.mjs [--base-url http://localhost:5000]
 *
 * The script starts the McpManager.Web server, waits for it to be ready,
 * captures screenshots of each page, then shuts down the server.
 */

import { chromium } from 'playwright';
import { spawn } from 'child_process';
import { resolve, dirname } from 'path';
import { fileURLToPath } from 'url';
import { existsSync, mkdirSync } from 'fs';

const __dirname = dirname(fileURLToPath(import.meta.url));
const projectRoot = resolve(__dirname, '..');
const outputDir = resolve(projectRoot, 'docs', 'screenshots');

const pages = [
  { name: 'dashboard', path: '/', title: 'Dashboard' },
  { name: 'browse-servers', path: '/servers/browse', title: 'Browse Servers' },
  { name: 'installed-servers', path: '/servers/installed', title: 'Installed Servers' },
  { name: 'agents', path: '/agents', title: 'Agents' },
  { name: 'health', path: '/health', title: 'Server Health' },
  { name: 'conflicts', path: '/conflicts', title: 'Conflicts' },
];

const args = process.argv.slice(2);
const baseUrlArg = args.indexOf('--base-url');
let baseUrl = baseUrlArg >= 0 ? args[baseUrlArg + 1] : null;
let serverProcess = null;

async function startServer() {
  const port = 5177;
  baseUrl = `http://localhost:${port}`;

  console.log(`Starting McpManager.Web on port ${port}...`);
  serverProcess = spawn('dotnet', [
    'run',
    '--project', resolve(projectRoot, 'src', 'McpManager.Web'),
    '--urls', baseUrl,
  ], {
    stdio: ['ignore', 'pipe', 'pipe'],
    cwd: projectRoot,
  });

  serverProcess.stderr.on('data', (data) => {
    const line = data.toString();
    if (line.includes('error') || line.includes('Error')) {
      console.error(`[server stderr] ${line.trim()}`);
    }
  });

  // Wait for server to be ready
  const maxWait = 30_000;
  const start = Date.now();
  while (Date.now() - start < maxWait) {
    try {
      const resp = await fetch(baseUrl);
      if (resp.ok || resp.status === 200) {
        console.log('Server is ready.');
        return;
      }
    } catch {
      // not ready yet
    }
    await new Promise(r => setTimeout(r, 500));
  }
  throw new Error(`Server did not start within ${maxWait / 1000}s`);
}

async function main() {
  if (!existsSync(outputDir)) {
    mkdirSync(outputDir, { recursive: true });
  }

  // Start server if no base URL provided
  if (!baseUrl) {
    // Build first
    console.log('Building McpManager.Web...');
    const build = spawn('dotnet', ['build', resolve(projectRoot, 'src', 'McpManager.Web')], {
      stdio: 'inherit',
      cwd: projectRoot,
    });
    await new Promise((resolve, reject) => {
      build.on('close', (code) => code === 0 ? resolve() : reject(new Error(`Build failed with code ${code}`)));
    });
    await startServer();
  }

  console.log(`Capturing screenshots from ${baseUrl}...`);

  const browser = await chromium.launch();
  const context = await browser.newContext({
    viewport: { width: 1280, height: 800 },
    deviceScaleFactor: 2,
  });

  try {
    for (const page of pages) {
      const tab = await context.newPage();
      const url = `${baseUrl}${page.path}`;
      console.log(`  ${page.title} (${url})...`);

      await tab.goto(url, { waitUntil: 'networkidle', timeout: 15_000 });
      // Give Blazor a moment to render
      await tab.waitForTimeout(1000);

      const outputPath = resolve(outputDir, `${page.name}.png`);
      await tab.screenshot({ path: outputPath, fullPage: true });
      console.log(`    -> ${outputPath}`);
      await tab.close();
    }
  } finally {
    await browser.close();
    if (serverProcess) {
      console.log('Stopping server...');
      serverProcess.kill('SIGTERM');
      // Give it a moment to clean up
      await new Promise(r => setTimeout(r, 1000));
      if (!serverProcess.killed) serverProcess.kill('SIGKILL');
    }
  }

  console.log(`\nDone! ${pages.length} screenshots saved to docs/screenshots/`);
}

main().catch((err) => {
  console.error(err);
  if (serverProcess) serverProcess.kill('SIGKILL');
  process.exit(1);
});
