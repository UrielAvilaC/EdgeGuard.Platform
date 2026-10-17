# `data/` — artefactos del instalador del nodo

Esta carpeta lleva el paquete que el instalador despliega. **Su contenido no se
versiona** (ver `.gitignore`): son decenas de MB que no tienen por qué entrar al
historial de git.

## Contenido esperado

| Archivo | Obligatorio | Descripción |
|---|---|---|
| `edgeguard-node-<versión>.zip` | Sí | El nodo publicado **self-contained** para win-x64 |
| `checksums.sha256` | Sí | SHA-256 de cada artefacto |

Si hay más de un `.zip`, el instalador toma el de versión mayor y lo indica en el
resumen. `-PackagePath` fuerza otro.

## Cómo se genera

No a mano. Lo produce el sellado de versión:

```powershell
cd setup\common
.\Seal-Version.ps1 -Target Node
```

Eso publica el nodo, comprime con la versión en el nombre, calcula los hashes y
escribe el `checksums.sha256` que el paso 01 verifica. Para sellar una versión
nueva, con etiqueta de git incluida:

```powershell
.\Seal-Version.ps1 -Version 1.5.0 -Tag
```

Ver `setup/common/README.md` para el detalle.

## Por qué self-contained

El paquete pesa ~53 MB en lugar de ~10 porque **se lleva el runtime de .NET
dentro**. Es deliberado: un nodo vive en el servidor de una sede, sin
administrador a mano y a veces sin salida a internet. Llevarse el runtime elimina
la clase entera de fallos *"la versión de .NET del servidor no es la que toca"*,
que se manifiestan como un servicio que se registra bien y no arranca, con un
error del cargador que no menciona en ningún momento la palabra «runtime».

El paso 01 lo verifica: si el zip no trae `hostfxr.dll`, se publicó
framework-dependent y aborta antes de desplegar nada.

## Qué comprueba el paso 01 sobre este contenido

1. **Que exista un `.zip`** y, si hay varios, elige el de versión mayor dejando
   constancia — nadie debería descubrir por accidente que instaló otra versión.
2. **Que los hashes cuadren.** Un zip a medio copiar produce un despliegue que
   parece correcto y falla al arrancar, con un error que no señala la causa.
3. **Que el zip traiga `Dicom.Edge.Node.exe` y `appsettings.Production.json` en
   la raíz**, no dentro de una subcarpeta. Comprimir desde el directorio
   equivocado es el error de empaquetado más fácil de cometer.
4. **Que traiga el runtime**, como se explica arriba.
