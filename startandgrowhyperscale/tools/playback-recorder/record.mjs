// Records each Caldova app sequence (playback mode) to its own 1920x1080 MP4.
// Usage: node record.mjs [clipName ...]   (default: all clips, in order)
import { chromium } from 'playwright';
import { spawn } from 'node:child_process';
import { mkdirSync } from 'node:fs';
import path from 'node:path';
import ffmpegPath from 'ffmpeg-static';

const BASE = process.env.CALDOVA_URL ?? 'http://localhost:5096';
const OUT = process.env.CLIP_DIR ?? path.resolve('clips');
const FPS = 25;
const W = 1920, H = 1080;          // output pixels
const CW = 1536, CH = 864, DSF = 1.25; // CSS viewport at 125% zoom
mkdirSync(OUT, { recursive: true });

const cursorScript = () => {
  const install = () => {
    if (document.getElementById('__cursor')) return;
    const c = document.createElement('div');
    c.id = '__cursor';
    c.innerHTML = '<svg width="28" height="28" viewBox="0 0 28 28"><path d="M3 2 L3 22 L8.5 17 L12.5 26 L16 24.5 L12 15.5 L19.5 15.5 Z" fill="#111" stroke="#fff" stroke-width="1.6" stroke-linejoin="round"/></svg>';
    Object.assign(c.style, { position: 'fixed', left: '-100px', top: '-100px', zIndex: 2147483647, pointerEvents: 'none', width: '28px', height: '28px' });
    document.documentElement.appendChild(c);
    document.addEventListener('mousemove', e => { c.style.left = e.clientX - 3 + 'px'; c.style.top = e.clientY - 2 + 'px'; }, true);
    document.addEventListener('mousedown', e => {
      const r = document.createElement('div');
      Object.assign(r.style, { position: 'fixed', left: e.clientX - 18 + 'px', top: e.clientY - 18 + 'px', width: '36px', height: '36px',
        borderRadius: '50%', border: '3px solid rgba(197,138,22,.9)', zIndex: 2147483646, pointerEvents: 'none',
        transition: 'transform .45s ease-out, opacity .45s ease-out', transform: 'scale(.4)', opacity: '1' });
      document.documentElement.appendChild(r);
      requestAnimationFrame(() => { r.style.transform = 'scale(1.4)'; r.style.opacity = '0'; });
      setTimeout(() => r.remove(), 600);
    }, true);
  };
  if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', install); else install();
  new MutationObserver(install).observe(document, { childList: true, subtree: true });
};

const sleep = ms => new Promise(r => setTimeout(r, ms));
let mouse = { x: CW / 2, y: CH / 2 };

async function moveTo(page, locator, settle = 350) {
  await locator.waitFor({ state: 'visible' });
  for (let i = 0; i < 3; i++) {
    const b = await locator.boundingBox();
    if (b && b.y >= 90 && b.y + Math.min(b.height, CH / 2) <= CH - 30) break;
    if (b) await scrollBy(page, b.y - CH / 3);
  }
  const box = await locator.boundingBox();
  if (!box) throw new Error('Element not visible');
  const x = box.x + box.width / 2, y = box.y + box.height / 2;
  const steps = Math.max(12, Math.round(Math.hypot(x - mouse.x, y - mouse.y) / 18));
  await page.mouse.move(x, y, { steps });
  mouse = { x, y };
  await sleep(settle);
}

async function click(page, locator) {
  await moveTo(page, locator);
  await page.mouse.down();
  await sleep(90);
  await page.mouse.up();
  await sleep(250);
}

async function wander(page, points, pause = 600) {
  for (const [x, y] of points) {
    await page.mouse.move(x, y, { steps: 30 });
    mouse = { x, y };
    await sleep(pause);
  }
}

async function scrollBy(page, total, step = 60) {
  const n = Math.max(1, Math.round(Math.abs(total) / step));
  for (let i = 0; i < n; i++) { await page.mouse.wheel(0, Math.sign(total) * step); await sleep(28); }
  await sleep(400);
}

async function scrollToTop(page) {
  const y = await page.evaluate(() => window.scrollY);
  if (y > 0) await scrollBy(page, -y);
}

class Clip {
  constructor(page, name) { this.page = page; this.name = name; }
  async start() {
    this.file = path.join(OUT, `${this.name}.mp4`);
    this.ff = spawn(ffmpegPath, ['-y', '-loglevel', 'error', '-f', 'image2pipe', '-c:v', 'mjpeg', '-framerate', String(FPS), '-i', '-',
      '-vf', `scale=${W}:${H}:flags=lanczos,format=yuv420p`, '-c:v', 'libx264', '-preset', 'slow', '-crf', '16',
      '-movflags', '+faststart', '-r', String(FPS), this.file], { stdio: ['pipe', 'inherit', 'inherit'] });
    this.closed = new Promise(r => this.ff.on('close', r));
    this.cdp = await this.page.context().newCDPSession(this.page);
    this.latest = null;
    this.cdp.on('Page.screencastFrame', async f => {
      this.latest = Buffer.from(f.data, 'base64');
      try { await this.cdp.send('Page.screencastFrameAck', { sessionId: f.sessionId }); } catch { }
    });
    await this.cdp.send('Page.startScreencast', { format: 'jpeg', quality: 95, maxWidth: W, maxHeight: H, everyNthFrame: 1 });
    while (!this.latest) await sleep(20);
    this.t0 = Date.now();
    this.written = 0;
    this.timer = setInterval(() => this.pump(), 1000 / FPS / 2);
    console.log(`● recording ${this.name}`);
  }
  pump() {
    const due = Math.floor((Date.now() - this.t0) / 1000 * FPS);
    while (this.written < due) { this.ff.stdin.write(this.latest); this.written++; }
  }
  async stop() {
    this.pump();
    clearInterval(this.timer);
    await this.cdp.send('Page.stopScreencast').catch(() => { });
    await this.cdp.detach().catch(() => { });
    this.ff.stdin.end();
    await this.closed;
    console.log(`■ ${this.name}: ${(this.written / FPS).toFixed(1)} s → ${this.file}`);
  }
}

async function record(page, name, body) {
  const clip = new Clip(page, name);
  await clip.start();
  try { await body(); } finally { await clip.stop(); }
}

async function ready(page, url) {
  await page.goto(BASE + url);
  await page.waitForLoadState('networkidle');
  await sleep(2500); // Blazor circuit connects
  await page.mouse.move(mouse.x, mouse.y);
}

const text = (page, s) => page.getByText(s, { exact: false }).first();

const clips = {
  async 'p1-run'(page) {
    await ready(page, '/');
    await record(page, 'p1-run', async () => {
      await sleep(3000);
      await wander(page, [[560, 450], [1200, 500]], 700);
      await click(page, page.getByRole('button', { name: /Run/ }));
      await text(page, 'Measured run complete').waitFor({ timeout: 15 * 60_000 });
      await sleep(6000);
    });
  },
  async 'p1-paused'(page) {
    await text(page, 'Paused · no compute charges').waitFor({ timeout: 6 * 60_000 });
    await sleep(2000);
    await record(page, 'p1-paused', async () => {
      await sleep(3000);
      await wander(page, [[1200, 560], [1240, 720], [640, 640]], 1500);
      await sleep(5000);
    });
  },
  async 'p1-resume'(page) {
    await record(page, 'p1-resume', async () => {
      await sleep(2500);
      await click(page, page.getByRole('button', { name: /Sign in \(resume\)/ }));
      await text(page, 'First sign-in completed in').waitFor({ timeout: 90_000 });
      await sleep(7000);
    });
  },
  async 'p2-acquisitions'(page) {
    await ready(page, '/network');
    await record(page, 'p2-acquisitions', async () => {
      await sleep(3000);
      await wander(page, [[340, 300], [770, 300], [1200, 300]], 900);
      await click(page, page.getByRole('button', { name: 'Add these databases to a pool' }));
      await sleep(1200);
      await scrollBy(page, 260);
      await sleep(7000);
    });
  },
  async 'p2-verify'(page) {
    await ready(page, '/verify');
    await record(page, 'p2-verify', async () => {
      await sleep(3000);
      await moveTo(page, page.getByRole('link', { name: 'Open pool in Azure portal' }), 1500);
      await click(page, page.getByRole('button', { name: 'Check cross-database reads' }));
      await page.getByRole('status').waitFor({ timeout: 15_000 });
      await sleep(8000);
    });
  },
  async 'p2-scale'(page) {
    await ready(page, '/pool');
    const reset = page.getByRole('button', { name: /Reset/ });
    if (await reset.isEnabled()) await reset.click();
    await sleep(1000);
    await record(page, 'p2-scale', async () => {
      await sleep(3000);
      await click(page, page.getByRole('button', { name: /Start workload/ }));
      await sleep(25_000);
      await click(page, page.getByRole('button', { name: 'Scale pool to 16 vCores' }));
      await text(page, 'Scale complete').waitFor({ timeout: 120_000 });
      await sleep(12_000);
    });
  },
  async 'p2-growth'(page) {
    if (!page.url().endsWith('/pool')) throw new Error('Run p2-scale first; growth continues the same workload.');
    await record(page, 'p2-growth', async () => {
      await sleep(2000);
      await click(page, page.getByRole('button', { name: 'Load Northbrook encounter history' }));
      await sleep(1500);
      await moveTo(page, page.locator('table.pool-storage'), 500);
      await text(page, 'History loaded').waitFor({ timeout: 180_000 });
      await sleep(3000);
      await scrollBy(page, 400);
      await sleep(6000);
    });
  },
  async 'p2-search-book'(page, browser) {
    await ready(page, '/appointments');
    const colleague = await (await browser.newContext({ viewport: { width: CW, height: CH }, deviceScaleFactor: DSF })).newPage();
    await record(page, 'p2-search-book', async () => {
      await sleep(3000);
      await click(page, page.getByRole('button', { name: /Find openings/ }));
      await sleep(4500);
      await click(page, page.getByLabel('Search the entire network'));
      await click(page, page.getByRole('button', { name: /Find openings/ }));
      await sleep(4500);
      await moveTo(page, page.locator('table.slots'), 2500);
      const summary = page.getByText('Behind the view · native cross-database joins');
      await click(page, summary);
      await sleep(600);
      await moveTo(page, page.locator('details.panel pre.code'), 400);
      await sleep(7000);
      await scrollToTop(page);
      await moveTo(page, page.locator('table.slots'), 800);
      await click(page, page.locator('table.slots tbody tr').first().getByRole('button', { name: 'Book slot' }));
      await sleep(3000);
      await moveTo(page, page.getByRole('heading', { name: 'Booking · one conditional write' }), 400);
      await sleep(6000);
      // A colleague in another session books the next Ridgeview opening first.
      await colleague.goto(BASE + '/appointments');
      await colleague.waitForLoadState('networkidle');
      await sleep(2500);
      await colleague.getByLabel('Search the entire network').check();
      await colleague.getByRole('button', { name: /Find openings/ }).click();
      await colleague.locator('table.slots tbody tr').first().waitFor();
      await colleague.locator('table.slots tbody tr').first().getByRole('button', { name: 'Book slot' }).click();
      await sleep(1500);
      await scrollToTop(page);
      await moveTo(page, page.locator('table.slots'), 800);
      await click(page, page.locator('table.slots tbody tr').first().getByRole('button', { name: 'Book slot' }));
      await sleep(5000);
      await scrollToTop(page);
      await moveTo(page, page.locator('#specialty'), 500);
      await page.locator('#specialty').selectOption('Pediatrics');
      await sleep(800);
      await click(page, page.getByRole('button', { name: /Find openings/ }));
      await sleep(7000);
    });
    await colleague.context().close();
  },
};

const order = ['p1-run', 'p1-paused', 'p1-resume', 'p2-acquisitions', 'p2-verify', 'p2-scale', 'p2-growth', 'p2-search-book'];
const selected = process.argv.slice(2).length ? process.argv.slice(2) : order;
const browser = await chromium.launch({ channel: 'msedge', headless: true });
const context = await browser.newContext({ viewport: { width: CW, height: CH }, deviceScaleFactor: DSF });
await context.addInitScript(cursorScript);
const page = await context.newPage();
try {
  for (const name of selected) {
    if (!clips[name]) throw new Error(`Unknown clip ${name}`);
    await clips[name](page, browser);
  }
} finally {
  await browser.close();
}
