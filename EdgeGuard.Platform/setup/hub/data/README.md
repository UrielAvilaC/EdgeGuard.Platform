# `data/` — artefactos del instalador

Esta carpeta lleva los binarios que el instalador despliega. **Su contenido no se
versiona** (ver `.gitignore`): son decenas de MB que no tienen por qué entrar al
historial de git. Se depositan aquí al armar el paquete.

## Contenido esperado

| Archivo | Obligatorio | Descripción |
|---|---|---|
| `edgeguard-hub-<versión>.zip` | Sí | Backend + SPA, publicados juntos |
| `dotnet-hosting-10.x.x-win.exe` | No | ASP.NET Core Hosting Bundle |
| `checksums.sha256` | Sí | SHA-256 de cada artefacto |

Si hay más de un `.zip`, el instalador toma el de versión mayor y lo indica en el
resumen. `-PackagePath` fuerza otro.

## Cómo armar el zip

El orden importa, y equivocarse produce un paquete que parece correcto.

```powershell
# 1. El SPA PRIMERO
cd src\frontend\dicomedge-ui
npm ci
npm run build
```

`angular.json` tiene `outputPath` apuntando directamente a
`src/backend/Dicom.Edge.Hub.Api/wwwroot`, y el `.csproj` **no** tiene ningún
target que dispare el build del SPA: es pura convención. Si `dotnet publish`
corre antes que `npm run build`, el paquete sale sin front y el sitio publica
solo la API — algo que únicamente se nota al abrir el navegador.

El paso 01 del instalador verifica que el zip contenga `wwwroot/index.html`
precisamente por esto, y aborta si falta.

```powershell
# 2. Publicar
dotnet publish src\backend\Dicom.Edge.Hub.Api `
  --configuration Release `
  --runtime win-x64 `
  --self-contained false `
  --output .\publish

# 3. Comprimir
Compress-Archive -Path .\publish\* -DestinationPath .\edgeguard-hub-1.2.0.zip

# 4. Checksums
Get-FileHash .\edgeguard-hub-1.2.0.zip -Algorithm SHA256 |
    ForEach-Object { "$($_.Hash)  $(Split-Path $_.Path -Leaf)" } |
    Out-File .\checksums.sha256 -Encoding utf8
```

## Formato de `checksums.sha256`

Una línea por artefacto, hash en mayúsculas y nombre de archivo separados por dos
espacios:

```
A3F5...9C  edgeguard-hub-1.2.0.zip
7B21...4E  dotnet-hosting-10.0.8-win.exe
```

El paso 01 los verifica **antes de cualquier acción**, incluido en `-DryRun`:
detectar ahí un paquete corrupto es justo el punto donde sale barato.
