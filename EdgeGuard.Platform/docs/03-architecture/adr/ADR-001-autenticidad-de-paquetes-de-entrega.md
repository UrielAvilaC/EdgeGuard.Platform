# ADR-001 — Autenticidad de los paquetes de entrega

## Contexto

El instalador del Hub y el del nodo verifican, en el paso 01 y antes de tocar nada, los
SHA-256 declarados en `data\checksums.sha256` (`setup/hub/bin/01-Test-Prerequisites.ps1`,
`setup/node/bin/01-Test-Prerequisites.ps1`). Esos hashes los escribe `Seal-Version.ps1` y
**viajan en el mismo paquete** que protegen.

Eso detecta un zip corrupto o copiado a medias. **No prueba autenticidad ni integridad
frente a un adversario**: quien altere el zip, o los `.ps1` del instalador, puede regenerar
`checksums.sha256` con los hashes nuevos y el paso 01 pasará. Los `.ps1` ni siquiera están
cubiertos por los checksums: el instalador que se ejecuta con privilegios de administrador en
el servidor de la sede no se verifica contra nada.

El riesgo es real por el destino: el instalador corre elevado, registra un servicio y abre
puertos en un servidor que recibe estudios DICOM (datos de pacientes). Un paquete alterado en
tránsito (correo, descarga, unidad USB) obtendría ejecución como administrador en ese equipo.

`New-DeliveryPackage.ps1` entrega ya el hash del zip de entrega en un `.sha256` lateral para
comunicarlo por otro canal. Es una mitigación manual: depende de que alguien lo compare, no
cubre los `.ps1` una vez desempaquetado el zip, y no deja rastro verificable por el instalador.

## Decisión

**Propuesta, pendiente de decidir el certificado.** Firmar con **Authenticode** y verificar en
el paso 01:

1. `Seal-Version.ps1` / `New-DeliveryPackage.ps1` firman cada `.ps1`, `.psm1` y `.psd1` de
   ejemplo con `Set-AuthenticodeSignature`, y generan un **catálogo firmado** del paquete con
   `New-FileCatalog` (versión 2, SHA-256) que cubre el zip sellado, el Hosting Bundle y los
   manuales; el `.cat` se firma con el mismo certificado.
2. El paso 01 de cada instalador comprueba, antes de cualquier otra acción y también en
   `-DryRun`: `Get-AuthenticodeSignature` de cada script (estado `Valid` y huella del
   certificado igual a la esperada) y `Test-FileCatalog` del paquete.
3. La huella del certificado de publicación se comunica al cliente por un canal distinto del
   paquete (manual de instalación y entrega) y se fija en el instalador como valor esperado.
4. Periodo de adopción: hasta que todas las sedes reciban paquetes firmados, un paquete sin
   firma produce un **aviso** y un paquete con firma inválida produce **error**. Después,
   sin firma también es error (`-AllowUnsigned` solo para ensayos, nunca en producción).

La clave privada no vive en el repositorio ni en la máquina de desarrollo del día a día: se
firma desde un equipo o almacén de claves dedicado, y el certificado nunca se versiona.

## Justificación

El control que falta es el **ancla de confianza**: algo que el atacante no pueda regenerar al
alterar el paquete. Authenticode la da con lo que Windows PowerShell 5.1 ya trae, sin
herramientas nuevas en el servidor del cliente ni dependencias de producción:
`Get-AuthenticodeSignature`, `New-FileCatalog` y `Test-FileCatalog` son nativos desde 5.1.

Alternativas contrastadas:

- **Solo hash fuera de banda (lo que hay hoy).** Descartada como control único: es manual, no
  cubre los scripts desempaquetados y su cumplimiento depende de la persona. Se conserva como
  complemento barato.
- **Firma separada de `checksums.sha256` con una clave RSA y la clave pública embebida en el
  instalador (verificada con .NET).** Sin coste de certificado y sin herramientas externas,
  pero la clave pública viaja dentro del paquete: quien altere el paquete reemplaza también la
  clave. Solo ancla confianza si la huella de la clave se compara por otro canal, o sea, el
  mismo problema que se quiere resolver, y obliga a custodiar y rotar una PKI propia sin el
  almacén de confianza de Windows.
- **Firma con GPG/minisign/`ssh-keygen -Y`.** Descartada: exige instalar una herramienta en un
  servidor de sede que a veces no tiene salida a internet ni administrador a mano.
- **Firmar solo el `.zip` de entrega.** Descartada: el instalador desempaquetado, que es lo que
  se ejecuta, quedaría sin verificar y no habría rastro tras la descompresión.

Qué se rompe si cambian las condiciones: si no se consigue un certificado de firma de código
que el cliente acepte (CA pública, o la CA interna del cliente con la raíz instalada en sus
servidores), la decisión no se puede aplicar y la opción de la clave pública embebida pasa a
ser la mejor disponible, con su límite de confianza explícito.

## Implicaciones

- **Costo recurrente:** certificado de firma de código (compra y renovación) y un
  procedimiento de custodia de la clave. Un certificado vencido o revocado deja de poder
  firmar versiones nuevas; las firmadas siguen siendo válidas si se usa sello de tiempo
  (`-TimestampServer`), lo que exige salida a un servidor de sellado desde la máquina de firma.
- **Superficie nueva que sostener:** los pasos 01 de Hub y nodo ganan una verificación, y los
  scripts de empaquetado ganan una fase de firma. Cada edición de un `.ps1` posterior a la
  firma invalida la firma: la firma es el último paso del sellado, nunca antes.
- **Cambia el flujo de sellado y entrega** (`Seal-Version.ps1`, `New-DeliveryPackage.ps1`) y
  el contrato del paso 01; por eso requiere esta ADR antes del código.
- **Seguridad del servidor de sede:** la política de ejecución recomendada para estos
  servidores pasa a poder ser `AllSigned`, que hoy no es viable.
- **Documentación a actualizar al implementarla:** runbooks de instalación de Hub y nodo
  (`docs/07-operations/`), `setup/common/README.md`, `setup/*/README.md` y los manuales del
  cliente (huella esperada y cómo verificarla).
- **No se implementa en este cambio.** Queda pendiente de la decisión sobre el certificado.
  Hasta entonces rige la mitigación descrita en el Contexto.
- No cambia el modelo de PHI: no añade tratamiento de datos de pacientes.

## Estado

propuesta

## Fecha

2026-10-16

## Trazabilidad

- **Requisitos:** no hay requisito formal registrado en el repositorio (no existe matriz de
  trazabilidad). Origen: brecha 3 detectada al documentar el empaquetado de entrega.
- **Módulos:** `setup/common` (`Seal-Version.ps1`, `New-DeliveryPackage.ps1`),
  `setup/hub/bin/01-Test-Prerequisites.ps1`, `setup/node/bin/01-Test-Prerequisites.ps1`.
- **Pruebas:** pendientes con la implementación (se extenderían en `tests/Setup.Tests/`).
- **Reemplaza:** —
- **Reemplazada por:** —
