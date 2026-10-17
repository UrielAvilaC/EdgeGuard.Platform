# `setup/common/` — lo que comparten los instaladores

Dos piezas que usan tanto el instalador del Hub como el del nodo.

| Archivo | Qué es |
|---|---|
| `EdgeGuard.Setup.psm1` | La biblioteca común de los instaladores |
| `Seal-Version.ps1` | El sellado de versión: estampa, compila, empaqueta y firma |
| `version.json` | Manifiesto del último sellado. Producto generado |

---

## `EdgeGuard.Setup.psm1`

No instala nada: da las herramientas que usan los pasos de ambos instaladores.
Trece funciones en tres grupos.

**Secretos.** `Register-SetupSecret`, `Protect-SetupSecret`, `Format-SetupMasked`.
Se registra la contraseña una vez y el registro la enmascara en todas partes, en
lugar de imprimirla.

**Registro y ejecución.** `Start-SetupLog`, `Get-SetupLogPath`, `Write-SetupLog`,
`Invoke-SetupStep`, `Invoke-SetupAction`, `Test-SetupDryRun`.

`Invoke-SetupAction` es la pieza central: cada acción con efectos se declara con
su descripción y el módulo decide si la ejecuta o solo la registra. Por eso
`-DryRun` recorre **la misma ruta de código** que la instalación real, en vez de
ser un camino paralelo que se desincroniza en silencio. Un paso que escriba
directamente, saltándose `Invoke-SetupAction`, rompe esa garantía para todo el
instalador.

**Utilidades.** `Test-SetupAdministrator`, `ConvertFrom-SetupSecureString`,
`Restore-SetupBackup`, `Get-SetupFileHashSafe`.

### Una fuente, dos copias

Cada instalador se distribuye como un zip autónomo y necesita su copia del módulo
en `bin/`. Esas copias son **artefacto, no fuente**: las genera `Seal-Version.ps1`
al empaquetar y están en `.gitignore`. Editar `setup/hub/bin/EdgeGuard.Setup.psm1`
es editar algo que el siguiente sellado sobrescribe.

Dos módulos editables acabarían divergiendo, y con ellos el significado de
`-DryRun` en cada instalador.

---

## `Seal-Version.ps1`

Una versión sellada es un conjunto de artefactos del que se puede decir, más
tarde y sin ambigüedad, de qué código salió.

```powershell
# Sellar una versión nueva, con etiqueta de git
.\Seal-Version.ps1 -Version 1.5.0 -Tag

# Reempaquetar sin cambiar el número (p. ej. tras un zip corrupto)
.\Seal-Version.ps1 -Target Node

# Ver qué haría, sin escribir
.\Seal-Version.ps1 -Version 1.5.0 -DryRun
```

### Los cinco pasos

**1 · Estampar.** El número vive en un solo sitio, `<Version>` de
`Directory.Build.props`: de ahí salen `AssemblyVersion`, `FileVersion` e
`InformationalVersion` de todos los proyectos, el `/api/info` del Hub y la versión
que el nodo reporta al registrarse. Lo único que el SDK no toca es el
`package.json` del SPA, así que se estampa también.

**2 · Publicar.** El Hub framework-dependent (IIS ya lleva su Hosting Bundle); el
nodo **self-contained**, por lo dicho en `setup/node/data/README.md`. En el Hub el
SPA se compila ANTES de publicar: `angular.json` escribe directamente en el
`wwwroot\` del proyecto de la API y el `.csproj` no dispara el build del front.
Publicar primero produce un paquete sin front que solo se nota al abrir el
navegador.

**3 · Empaquetar.** Con la versión en el nombre, que es como el paso 01 de cada
instalador elige el paquete más nuevo cuando hay varios. De paso, copia el módulo
común al `bin/` de cada instalador.

**4 · Firmar.** Calcula los SHA-256 y escribe el `checksums.sha256` que el paso 01
verifica antes de tocar nada. Se recalcula sobre **todo** lo que haya en `data/`,
no solo sobre el zip recién hecho: el Hosting Bundle que acompaña al Hub también
se verifica, y un archivo declarado de una versión anterior haría fallar la
instalación siguiente.

**5 · Manifiesto.** `version.json` con el número, el commit, la fecha, la máquina
y los hashes.

Este último paso es el que convierte esto en un sellado y no en un empaquetado:
sin el commit, un `.zip` de hace tres meses no dice de qué código salió, y cotejar
el binario contra el repositorio deja de ser posible.

### Sobre la etiqueta

`-Tag` exige un árbol limpio. Una etiqueta creada sobre cambios sin confirmar
apunta a un commit que **no contiene lo que se empaquetó**, que es exactamente lo
que la etiqueta debería garantizar. También rechaza una etiqueta que ya exista:
sellar dos veces la misma versión deja dos paquetes distintos con el mismo número.

Sin `-Tag`, un sellado desde un árbol sucio se permite pero queda anotado en
`version.json` (`arbolLimpio: false`) y avisa por pantalla. Anotarlo es preferible
a prohibirlo: hace falta para probar, y silenciarlo sería peor que registrarlo.

---

## Antes de editar cualquier `.ps1` de `setup/`

**Guárdalos siempre en UTF-8 CON BOM.** Windows PowerShell 5.1 —el que hay en un
Windows Server limpio, y el que estos scripts declaran con `#Requires -Version 5.1`—
lee un `.ps1` sin BOM como ANSI. Un guion largo `—` se decodifica entonces como
tres caracteres, uno de los cuales es una comilla doble que **cierra la cadena en
la que estaba** y corrompe el parseo del resto del archivo.

El síntoma no se parece a la causa: el script no falla al cargar, sino mucho más
abajo, con un error sobre una variable que "no se ha establecido" porque su
asignación acabó dentro de un literal de cadena.

Comprobarlo:

```powershell
Get-ChildItem setup -Recurse -Include *.ps1,*.psm1,*.psd1 | ForEach-Object {
    $b = [System.IO.File]::ReadAllBytes($_.FullName)
    if (-not ($b.Length -ge 3 -and $b[0] -eq 239 -and $b[1] -eq 187 -and $b[2] -eq 191)) {
        "SIN BOM: $($_.FullName)"
    }
}
```
