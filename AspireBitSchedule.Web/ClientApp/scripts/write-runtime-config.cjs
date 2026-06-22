const fs = require('fs');
const path = require('path');

const outputPath = path.join(__dirname, '..', 'public', 'runtime-config.json');
const generatedConfigPath = path.join(__dirname, '..', 'src', 'app', 'core', 'config', 'app-runtime-config.ts');
const backendBaseUrl = (process.env.BACKEND_BASE_URL || '').trim().replace(/\/+$/, '');
const apiBaseUrl = backendBaseUrl ? `${backendBaseUrl}/api` : '/api';

console.log(`[runtime-config] BACKEND_BASE_URL=${backendBaseUrl || '(not set)'}`);
console.log(`[runtime-config] apiBaseUrl=${apiBaseUrl}`);

fs.mkdirSync(path.dirname(outputPath), { recursive: true });
fs.writeFileSync(
  outputPath,
  `${JSON.stringify({ apiBaseUrl }, null, 2)}\n`,
  'utf8');

fs.mkdirSync(path.dirname(generatedConfigPath), { recursive: true });
fs.writeFileSync(
  generatedConfigPath,
  [
    'export interface AppRuntimeConfig {',
    '  apiBaseUrl: string;',
    '}',
    '',
    'export const appRuntimeConfig: AppRuntimeConfig = {',
    `  apiBaseUrl: '${apiBaseUrl}'`,
    '};',
    ''
  ].join('\n'),
  'utf8');
