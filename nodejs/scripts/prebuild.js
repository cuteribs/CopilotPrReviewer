#!/usr/bin/env node
import fs from 'fs';

async function copyFiles() {
    if (!fs.existsSync('resources')) {
        fs.mkdirSync('resources');
    }
    fs.cpSync('../resources', 'resources', { recursive: true });
    fs.cpSync('../LICENSE', 'LICENSE');
}

copyFiles();