#!/usr/bin/env node
import fs from 'fs/promises';
import path from 'path';

async function exists(p) {
  try {
    await fs.access(p);
    return true;
  } catch (e) {
    return false;
  }
}

async function copyFile(src, dest) {
  await fs.mkdir(path.dirname(dest), { recursive: true });
  await fs.copyFile(src, dest);
  console.log(`Copied ${path.relative(process.cwd(), src)} -> ${path.relative(process.cwd(), dest)}`);
}

import { fileURLToPath } from 'url';

async function main() {
  const __filename = fileURLToPath(import.meta.url);
  const __dirname = path.dirname(__filename);
  const repoRoot = path.resolve(__dirname, '..', '..');

  // Node project is responsible for copying files into nodejs/
  const licenseSrc = path.join(repoRoot, 'LICENSE');
  const nodeLicenseTarget = path.join(repoRoot, 'nodejs', 'LICENSE');

  if (await exists(licenseSrc)) {
    try {
      await copyFile(licenseSrc, nodeLicenseTarget);
    } catch (err) {
      console.error(`Failed to copy LICENSE to ${nodeLicenseTarget}:`, err.message);
    }
  } else {
    console.warn('No LICENSE file found at repo root, skipping LICENSE copy.');
  }

  // Copy markdown files from root `resources` folder to nodejs/resources
  const rootResources = path.join(repoRoot, 'resources');
  const nodejsResDir = path.join(repoRoot, 'nodejs', 'resources');

  if (await exists(rootResources)) {
    const entries = await fs.readdir(rootResources, { withFileTypes: true });
    const mdFiles = entries.filter(e => e.isFile() && e.name.toLowerCase().endsWith('.md')).map(e => e.name);
    for (const name of mdFiles) {
      const src = path.join(rootResources, name);
      try {
        await copyFile(src, path.join(nodejsResDir, name));
      } catch (err) {
        console.error(`Failed to copy ${name} to nodejs resources:`, err.message);
      }
    }
    if (mdFiles.length === 0) console.log('No markdown files found in repo root resources folder.');
  } else {
    console.log('No root resources folder found; skipping markdown copy.');
  }
}

main().catch(err => {
  console.error('prebuild failed:', err);
  process.exitCode = 1;
});
