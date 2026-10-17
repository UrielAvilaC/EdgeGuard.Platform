# `setup/common/` — lo que comparten los instaladores

Piezas que usan tanto el instalador del Hub como el del nodo.

| Archivo | Qué es |
|---|---|
| `EdgeGuard.Setup.psm1` | La biblioteca común de los instaladores |
| `Seal-Version.ps1` | El sellado de versión: estampa, compila, empaqueta y firma |
| `New-DeliveryPackage.ps1` | La entrega al cliente: instalador + paquete sellado + checksums + manuales, con lista blanca |
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

`-Tag` comprueba **al inicio**, antes de compilar nada, tres cosas:

1. **Árbol limpio.** Una etiqueta creada sobre cambios sin confirmar apunta a un
   commit que **no contiene lo que se empaquetó**, que es exactamente lo que la
   etiqueta debería garantizar.
2. **El número ya estampado y confirmado.** Si el sellado lo estampara, el commit
   etiquetado llevaría el número anterior.
3. **La etiqueta no existe.** Sellar dos veces la misma versión deja dos paquetes
   distintos con el mismo número.

Se comprueba al inicio y no al final porque el sellado reescribe `version.json`,
que está versionado: medir el árbol al terminar lo daba siempre por sucio y `-Tag`
fallaba después de haber compilado y empaquetado todo. Por la misma razón
`arbolLimpio` y `commit` de `version.json` describen el árbol **de antes de
estampar**, no el que dejó el propio sellado.

El flujo de una versión nueva es, por tanto, en dos tiempos:

```powershell
# 1. En una rama: estampa el número y ensaya el sellado completo (sin -Tag)
.\Seal-Version.ps1 -Version 1.5.0
#    Confirma Directory.Build.props y el package.json del SPA, y llévalos a Production por PR.

# 2. Ya en Production, con el árbol limpio: sella de verdad y etiqueta
.\Seal-Version.ps1 -Tag
git push origin v1.5.0
#    version.json queda modificado: es el registro de este sellado. Confírmalo en un cambio aparte.
```

`version.json` nombra el commit que se empaquetó; nunca podrá nombrar el commit que
lo contiene a él. Es inherente, no un defecto: la etiqueta es la referencia exacta.

Sin `-Tag`, un sellado desde un árbol sucio se permite pero queda anotado en
`version.json` (`arbolLimpio: false`) y avisa por pantalla. Anotarlo es preferible
a prohibirlo: hace falta para probar, y silenciarlo sería peor que registrarlo.
`New-DeliveryPackage.ps1` se niega a entregar un sellado así.

---

## `New-DeliveryPackage.ps1`

Arma lo que se **entrega al cliente**: el instalador, el paquete sellado, sus
checksums y los manuales, en un solo `.zip`.

```powershell
.\New-DeliveryPackage.ps1 -Target Node
.\New-DeliveryPackage.ps1 -Target Hub -Version 1.5.0 -OutputPath D:\entregas
.\New-DeliveryPackage.ps1 -Target Node -DryRun     # valida sin escribir el zip
```

Sale en `artifacts\delivery\edgeguard-<hub|node>-<versión>-instalador.zip`, junto a
un `.sha256` con el hash del zip.

**Lista blanca, no copia de carpeta.** Solo viaja lo que el script nombra:
`install.ps1`, el README, el `*.example.psd1`, los `.ps1`/`.psm1` de `bin\`, el
paquete **de esta versión** (y el Hosting Bundle en el Hub) y los PDF. Un
`node-install.psd1` o `hub-install.psd1` de sede, una versión anterior que siga en
`data\` o un archivo suelto en `bin\` no pueden entrar. El motivo: un `install.zip`
armado a mano ya arrastró un `hub-install.psd1` real.

**Se niega a empaquetar si lo sellado no es de fiar:**

- `version.json` no es del número pedido, salió de un árbol sucio o no declara el
  hash del paquete que hay en `data\`.
- El paquete no coincide con `data\checksums.sha256`.
- La copia del módulo común en `bin\` no es idéntica a la de `setup\common\`.
- Un script que viaja no lleva BOM o no parsea.
- La auditoría final encuentra configuración de sede, certificados (`*.pfx`,
  `*.key`, `*.pem`), bases de datos o registros, también **dentro** del paquete
  sellado (y `appsettings.Development.json` en el Hub).

`-AllowUnsealed` permite ensayar con un sellado sucio; el paquete sale sin
`version.json` y marcado en rojo como no apto para un cliente.

El `checksums.sha256` que viaja se **reescribe** con lo que de verdad va en `data\`:
el de origen declara también versiones anteriores y un archivo declarado pero
ausente produce un aviso que siempre sale, hasta que nadie lo lee.

**Qué prueba el hash lateral.** Los checksums de dentro detectan un zip corrupto,
no uno alterado: viajan en el mismo paquete y quien lo modifique puede
regenerarlos. El `.sha256` del zip de entrega es para comunicarlo al cliente **por
otro canal** (correo, portal). Probar procedencia de verdad exige firma; ver
`docs/03-architecture/adr/ADR-001-autenticidad-de-paquetes-de-entrega.md`.

Las pruebas viven en `tests/Setup.Tests/` (Pester 3.4, el que trae Windows):

```powershell
Invoke-Pester .\tests\Setup.Tests
```

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
