#!/usr/bin/env node
import { spawnSync } from 'node:child_process';

// Ejecutamos siempre con el mismo node (process.execPath) invocando los .js
// directamente. Evitamos npx.cmd a proposito: en Windows falla con EINVAL
// (CVE-2024-27980) sin shell, y con shell resuelve mal el prefix de npm segun
// como este instalado node (fnm/nvm/instalador). Con node directo nada de eso aplica.
const tasks = [
  {
    name: 'Tests faltantes',
    command: process.execPath,
    args: ['scripts/testing/check-missing-tests.js', '--staged'],
    hint: 'Agrega el spec esperado o ajusta test-generator.config.json si el archivo no debe testearse.',
  },
  {
    name: 'ESLint, Prettier y Stylelint',
    command: process.execPath,
    args: ['node_modules/lint-staged/bin/lint-staged.js'],
    hint: 'Revisa la salida anterior: puede ser formato, TS/HTML lint o SCSS stylelint.',
  },
];

function runTask(task) {
  console.log(`\n== ${task.name} ==`);
  const result = spawnSync(task.command, task.args, { stdio: 'inherit' });

  if (result.error) {
    console.error(`No se pudo ejecutar ${task.name}: ${result.error.message}`);
    return false;
  }

  return result.status === 0;
}

const failed = tasks.filter(task => !runTask(task));

if (failed.length > 0) {
  console.error('\nPre-commit fallo por:');
  for (const task of failed) {
    console.error(`- ${task.name}: ${task.hint}`);
  }
  process.exit(1);
}

console.log('\nPre-commit OK.');
