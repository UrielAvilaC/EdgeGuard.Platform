// Imprime un HTML local a PDF usando el motor de Microsoft Edge.
//
// Se habla el protocolo DevTools directamente en lugar de usar la bandera
// --print-to-pdf porque esa bandera imprime el encabezado y el pie que trae
// Chromium de fábrica: la ruta `file:///C:/...` del archivo y la fecha del
// sistema. En un entregable al cliente eso no se puede publicar. Por el
// protocolo sí se controlan las plantillas, y con ellas el pie de página con la
// numeración.
//
// Sin dependencias: Node 22+ trae `fetch` y `WebSocket` en el propio runtime.
//
// Uso:
//   node print-pdf.mjs --html=<ruta> --pdf=<ruta> [--footer=<texto>] [--header=<texto>]

import { spawn } from 'node:child_process';
import { existsSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, resolve } from 'node:path';
import { pathToFileURL } from 'node:url';

const args = Object.fromEntries(
  process.argv.slice(2).map((a) => {
    const i = a.indexOf('=');
    return i < 0 ? [a.replace(/^--/, ''), true] : [a.slice(2, i), a.slice(i + 1)];
  })
);

if (!args.html || !args.pdf) {
  console.error('Faltan argumentos: --html=<ruta> --pdf=<ruta>');
  process.exit(2);
}

const CANDIDATOS_EDGE = [
  'C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe',
  'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe',
  'C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe',
];

const navegador = args.browser || CANDIDATOS_EDGE.find((p) => existsSync(p));
if (!navegador) {
  console.error('No se encontró Microsoft Edge ni Google Chrome. Indique la ruta con --browser=<exe>.');
  process.exit(3);
}

const espera = (ms) => new Promise((r) => setTimeout(r, ms));

const perfil = mkdtempSync(join(tmpdir(), 'edgeguard-pdf-'));
const urlHtml = pathToFileURL(resolve(args.html)).href;

const proceso = spawn(navegador, [
  '--headless=new',
  '--disable-gpu',
  '--no-first-run',
  '--no-default-browser-check',
  '--disable-extensions',
  '--remote-debugging-port=0',
  `--user-data-dir=${perfil}`,
  urlHtml,
], { stdio: 'ignore' });

let ws = null;

async function puertoDevTools() {
  const archivo = join(perfil, 'DevToolsActivePort');
  for (let intento = 0; intento < 100; intento++) {
    if (existsSync(archivo)) {
      const contenido = readFileSync(archivo, 'utf8').split('\n');
      if (contenido[0]?.trim()) return contenido[0].trim();
    }
    await espera(100);
  }
  throw new Error('El navegador no publicó su puerto de depuración en 10 s.');
}

async function objetivoPagina(puerto) {
  for (let intento = 0; intento < 100; intento++) {
    const lista = await fetch(`http://127.0.0.1:${puerto}/json/list`).then((r) => r.json());
    const pagina = lista.find((t) => t.type === 'page' && t.url.startsWith('file:'));
    if (pagina?.webSocketDebuggerUrl) return pagina;
    await espera(100);
  }
  throw new Error('No apareció ninguna pestaña con el documento.');
}

function conectar(url) {
  return new Promise((cumplir, fallar) => {
    const socket = new WebSocket(url);
    socket.addEventListener('open', () => cumplir(socket), { once: true });
    socket.addEventListener('error', (e) => fallar(new Error(`WebSocket: ${e.message ?? 'error'}`)), { once: true });
  });
}

let siguienteId = 1;
const pendientes = new Map();

function enviar(metodo, params = {}) {
  const id = siguienteId++;
  ws.send(JSON.stringify({ id, method: metodo, params }));
  return new Promise((cumplir, fallar) => pendientes.set(id, { cumplir, fallar }));
}

// El encabezado y el pie se alinean con el margen lateral de la página. Si no
// coinciden, quedan desfasados del texto y se nota en cuanto se imprimen dos
// hojas una junto a otra.
function margenLateral() {
  return args.margen ? `${Number(args.margen)}in` : '1in';
}

function pieDePagina() {
  const izquierda = args.footer || '';
  const lateral = margenLateral();
  return `<div style="width:100%;box-sizing:border-box;padding:0 ${lateral};
      font-family:'Segoe UI',Arial,sans-serif;font-size:7.5pt;color:#6b7684;
      display:flex;justify-content:space-between;">
      <span>${izquierda}</span>
      <span>página <span class="pageNumber"></span> de <span class="totalPages"></span></span>
    </div>`;
}

function encabezado() {
  if (!args.header) return '<div></div>';
  return `<div style="width:100%;box-sizing:border-box;padding:0 ${margenLateral()};
      font-family:'Segoe UI',Arial,sans-serif;font-size:7.5pt;color:#9aa5b4;
      text-align:right;">${args.header}</div>`;
}

async function principal() {
  const puerto = await puertoDevTools();
  const pagina = await objetivoPagina(puerto);

  ws = await conectar(pagina.webSocketDebuggerUrl);
  ws.addEventListener('message', (evento) => {
    const mensaje = JSON.parse(evento.data);
    if (mensaje.id && pendientes.has(mensaje.id)) {
      const { cumplir, fallar } = pendientes.get(mensaje.id);
      pendientes.delete(mensaje.id);
      mensaje.error ? fallar(new Error(mensaje.error.message)) : cumplir(mensaje.result);
    }
  });

  await enviar('Page.enable');

  // El documento es local y no pide recursos externos, así que basta con
  // esperar a que el DOM termine de construirse.
  for (let intento = 0; intento < 150; intento++) {
    const { result } = await enviar('Runtime.evaluate', { expression: 'document.readyState' });
    if (result.value === 'complete') break;
    await espera(100);
  }

  // Márgenes de manual por omisión; la tarjeta desprendible los ajusta para
  // aprovechar sus dos únicas hojas.
  const margen = args.margen ? Number(args.margen) : null;

  const { data } = await enviar('Page.printToPDF', {
    printBackground: true,
    paperWidth: 8.5,
    paperHeight: 11,
    marginTop: margen ?? 1,
    marginBottom: margen ?? 0.9,
    marginLeft: margen ?? 1,
    marginRight: margen ?? 1,
    displayHeaderFooter: true,
    headerTemplate: encabezado(),
    footerTemplate: pieDePagina(),
    preferCSSPageSize: false,
    // Marcadores del PDF a partir de los encabezados del documento.
    generateDocumentOutline: true,
  });

  writeFileSync(resolve(args.pdf), Buffer.from(data, 'base64'));
  console.log(`PDF escrito: ${resolve(args.pdf)}`);
}

principal()
  .catch((e) => {
    console.error(`Error al imprimir: ${e.message}`);
    process.exitCode = 1;
  })
  .finally(() => {
    try { ws?.close(); } catch { /* el socket ya estaba cerrado */ }
    try { proceso.kill(); } catch { /* el navegador ya salió */ }
    // El perfil temporal a veces sigue bloqueado un instante tras cerrar.
    setTimeout(() => { try { rmSync(perfil, { recursive: true, force: true }); } catch { /* queda en %TEMP% */ } }, 300);
  });
