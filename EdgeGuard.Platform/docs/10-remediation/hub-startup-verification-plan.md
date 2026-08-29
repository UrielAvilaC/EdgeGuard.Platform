# Fallo de arranque del Hub — análisis y plan de corrección

**Instalación:** D-VPNS · HUB-001 · `C:\inetpub\EdgeGuard\Hub` · IIS in-process (w3wp, PID 2256)
**Ventana del log:** 2026-08-29 00:49:13 → 00:50:38 UTC

---

## Estado de ejecución

Rama `fix/hub-startup-verification`, commit `16757a8`. Compila la solución completa; 17/17 tests pasan.

| # | Punto | Estado |
|---|---|---|
| 1 | Contrato de ruta | Hecho — **con corrección**, ver nota abajo |
| 2 | Instalador sondea `/health/live` | Hecho, probado en caliente |
| 3 | 404 terminal | Hecho, probado: corta al primer intento |
| 4 | `.psd1` de D-VPNS | **Pendiente — acción del operador**, no es código |
| 5 | Exigir `AdminPassword` en instalación nueva | Hecho |
| 6 | Postcondición del paso 07 | Hecho |
| 7 | Categoría de log propia | Hecho |
| 8 | Enumeración de endpoints | Hecho |
| 9 | Test de regresión | Hecho **con alcance reducido**, ver nota |
| 10 | Ruido de `__EFMigrationsHistory` | Hecho |
| 11 | `ProtectKeysWithDpapi()` | **No implementado** — decisión de operación |

**Corrección al punto 1.** Al implementarlo se vio que `HubApiRoutes.cs:63` no declara una ruta del Hub: la constante está dentro de `NodeApiRoutes`, que describe lo que expone el **nodo**, y no la usa nadie. No existía ningún contrato del Hub para `/health`; el desajuste era solo entre el instalador y la aplicación. La constante además era incorrecta para el propio nodo —sirve `/api/health`— y se corrigió como limpieza.

**Alcance del punto 9.** Un test que arranque el host real no es viable sin base de datos: `Program.cs` aplica migraciones antes de construir el pipeline, así que `WebApplicationFactory` fallaría sin PostgreSQL. Lo que se probó es el mecanismo donde estaba el defecto: que `IEndpointRouteBuilder.DataSources` sí ve los endpoints de controlador antes de `Run()`, y que el `EndpointDataSource` del contenedor no. Un test de host completo sigue siendo deseable y necesita una base de pruebas.

**Punto 11.** Cifrar el key ring con DPAPI lo ata a la máquina y a la cuenta que lo creó. Eso cambia el procedimiento de recuperación ante desastre y de migración de servidor, así que es una decisión de operación, no un defecto que corregir por cuenta propia.

---

## Veredicto

**El Hub arrancó correctamente. Lo que falló fue la verificación del instalador.**

El paso 10 sondea `http://localhost:<puerto>/health`. La aplicación nunca mapea esa ruta —solo `/health/live` y `/health/ready`— y `/health` está además en la lista de prefijos que el fallback de la SPA devuelve como 404 en lugar de servir `index.html`. El sondeo recibe 404 noventa segundos seguidos, agota el plazo y el instalador aborta con «El Hub no respondió en /health».

La huella en el log es inequívoca: 26 peticiones `GET /health` espaciadas exactamente 3 s (`HealthPollSeconds = 3`), desde 00:49:22 —un segundo después de `Application started`— hasta 00:50:38, que es donde termina el fragmento; el plazo es `HealthTimeoutSeconds = 90`.

Todo lo demás del arranque está sano: migraciones aplicadas, cuatro sembrados ejecutados, listener HL7 escuchando en 8001 con 4 workers, seis hosted services arriba, ciclo de retención completado en 2.5 s.

---

## Causa raíz

### 1. Contrato de ruta roto entre la aplicación y el instalador — **causa del fallo**

Tres archivos afirman tres cosas distintas sobre la misma ruta:

| Archivo | Qué dice |
|---|---|
| `HealthCheckConstants.cs:11,14` | Los únicos endpoints son `/health/live` y `/health/ready` |
| `HubApiRoutes.cs:63` | `HealthCheck = "/health"` — el contrato compartido declara `/health` |
| `10-Test-Installation.ps1:57` | Sondea `/health` |

`MapDiagnosticsEndpoints` ([PlatformAppBuilderExtensions.cs:48](src/shared/Dicom.Edge.Diagnostics/Extensions/PlatformAppBuilderExtensions.cs:48)) registra únicamente los dos primeros. `/health` no existe.

Y como `/health` figura en `BackendPrefixes` ([SpaProductionExtensions.cs:37](src/backend/Dicom.Edge.Hub.Api/Extensions/SpaProductionExtensions.cs:37)), el catch-all de la SPA lo excluye deliberadamente del `index.html` y responde 404 — que es justo la línea que se repite 26 veces en el log.

Agravante: `Wait-HubHealth` trata el 404 como «todavía no está listo» y reintenta. Un 404 no es un estado transitorio: significa que la ruta no existe. El instalador quema 90 s para llegar a una conclusión que el primer intento ya permitía sacar, y el mensaje final —«no respondió»— apunta al arranque en vez de a la ruta.

### 2. La cuenta de administrador no se sembró

```
Admin seed skipped: environment variable 'EDGEGUARD_ADMIN_PASSWORD' is not set.
```

`AdminUserSeed` exige esa variable y, si falta, se limita a un `LogWarning` ([AdminUserSeed.cs:52](src/backend/Dicom.Edge.Hub.Persistence/Seed/AdminUserSeed.cs:52)). El paso 07 solo la escribe en el app pool cuando `Config.AdminPassword` no está vacía ([07-Set-AppPoolConfig.ps1:87](setup/hub/bin/07-Set-AppPoolConfig.ps1:87)).

El `hub-install.psd1` de este servidor **no tiene el bloque de administrador**. El `hub-install.example.psd1` sí lo tiene (líneas 41-58, añadidas en el commit `6db8ccc`); el `.psd1` real está en `.gitignore` y quedó desfasado respecto al ejemplo.

Tres cosas dejan pasar esto en silencio:

- `AdminPassword` es `Required = $false` ([install.ps1:96](setup/hub/install.ps1:96)).
- Con un archivo de configuración presente, el instalador **no pregunta** por claves ausentes; solo `DbPassword` tiene un prompt especial ([install.ps1:454](setup/hub/install.ps1:454)).
- `Confirm-HubAdminAccount` solo emite un `Warn` cuando no hay contraseña, no un error.

Resultado: instalación «exitosa» sobre una base sin ninguna cuenta con la que entrar al SPA. La única salida sería `scripts\seed-admin.sql`.

### 3. El diagnóstico de arranque es invisible y, encima, miente

Dos defectos independientes en [StartupDiagnosticsExtensions.cs](src/backend/Dicom.Edge.Hub.Api/Extensions/StartupDiagnosticsExtensions.cs):

**a) Se silencia en producción.** Usa `ILogger<WebApplication>`, cuyo `SourceContext` es `Microsoft.AspNetCore.Builder.WebApplication`. La configuración de Serilog aplica `.MinimumLevel.Override("Microsoft", LogEventLevel.Warning)` ([PlatformDiagnosticsExtensions.cs:160](src/shared/Dicom.Edge.Diagnostics/Extensions/PlatformDiagnosticsExtensions.cs:160)). Todas las líneas `LogInformation` del bloque —direcciones enlazadas, listado de endpoints, ruta del key ring, cadena de conexión saneada, estado del JWT— desaparecen. En el log solo sobreviven dos `Warning`, sin el contexto que los haría interpretables. El bloque de diagnóstico se pierde exactamente en el entorno donde hace falta.

`SpaStaticFiles` y `SpaFallback` sí aparecen porque usan un `SourceContext` propio, fuera del override.

**b) El aviso de `api/auth/login` es un falso positivo.** La línea:

```
ENDPOINT NOT REGISTERED: api/auth/login — controller discovery failed or
IAuthenticationService DI registration is missing.
```

es incorrecta. `AuthController` está bien declarado (`[Route("api/auth")]` + `[HttpPost("login")]`) y `app.MapControllers()` se llama en [Program.cs:179](src/backend/Dicom.Edge.Hub.Api/Program.cs:179).

El problema es cómo se enumeran los endpoints. `app.Services.GetService<EndpointDataSource>()` ([línea 171](src/backend/Dicom.Edge.Hub.Api/Extensions/StartupDiagnosticsExtensions.cs:171)) devuelve un `CompositeEndpointDataSource` alimentado por los `EndpointDataSource` **registrados en DI**. Los que crea `MapControllers` / `MapGet` / `MapHub` van a `IEndpointRouteBuilder.DataSources`, una colección propia de `WebApplication` que solo se fusiona con la de DI cuando arranca el pipeline. Como `LogStartupDiagnostics()` corre **antes** de `app.Run()`, el composite está vacío.

Verificado en .NET 10.0.200 con una app mínima (`AddControllers` + `MapControllers` + un `MapGet`):

| Momento y vía de acceso | Endpoints vistos |
|---|---|
| `Services.GetService<EndpointDataSource>()` antes de `Run()` | **0** |
| `((IEndpointRouteBuilder)app).DataSources` antes de `Run()` | 2 ✓ |
| `Services.GetRequiredService<EndpointDataSource>()` tras `ApplicationStarted` | 2 ✓ |

Es decir: **el aviso se emite siempre**, en cualquier despliegue, diga lo que diga el estado real de los controladores. Y es peor que ruido — en una sesión de diagnóstico apunta al sitio equivocado.

### 4. Ruido benigno confirmado

- **`Failed executing DbCommand ... SELECT "MigrationId" FROM "__EFMigrationsHistory"`** (nivel `Error`, 4 ms). Es el primer arranque contra una base sin tabla de historial. EF continuó y aplicó las migraciones (8 s hasta el primer sembrado); los sembrados y el ciclo de retención posteriores confirman que la base quedó operativa. Molesto porque un `Error` en el arranque alarma al operador.
- **`No XML encryptor configured`**. Esperado al persistir el key ring en sistema de archivos sin DPAPI. `DataProtection__KeyPath` sí llegó al proceso y `PersistKeysToFileSystem` está activo ([SecurityServiceCollectionExtensions.cs:45](src/shared/Dicom.Edge.Security/Extensions/SecurityServiceCollectionExtensions.cs:45)); las llaves **no** son efímeras.
- **`CORS is restricted to localhost origins`**. Correcto y esperado: `CorsAllowedOrigins = @()` en el `.psd1` y la SPA se sirve desde el mismo sitio. Solo hará falta añadir orígenes si el SPA pasa a servirse desde otro host o puerto.

---

## Plan de corrección

### P0 — Desbloquear la instalación

**Endpoint elegido: `/health/live`, en los dos lados.** No se añade ninguna ruta nueva; se corrige el contrato para que diga lo que la aplicación realmente expone, y el instalador sondea esa misma ruta. Una sola afirmación, en un solo sitio.

**1. Alinear el contrato con la realidad (código).**
`HubApiRoutes.HealthCheck` ([HubApiRoutes.cs:63](src/shared/Dicom.Edge.Contracts/Edge/HubApiRoutes.cs:63)) declara `"/health"`, una ruta que `MapDiagnosticsEndpoints` nunca registró. Pasa a `"/health/live"`, el valor de `HealthCheckConstants.LivenessEndpoint`. Se deja constancia en el propio archivo de cuál es la fuente de verdad, para que la próxima divergencia se note al editar.

`/health` sigue en `BackendPrefixes` ([SpaProductionExtensions.cs:37](src/backend/Dicom.Edge.Hub.Api/Extensions/SpaProductionExtensions.cs:37)) y así debe quedarse: es una comparación por prefijo y cubre `/health/live` y `/health/ready`. Sin ella, un fallo del backend devolvería `index.html` con 200 y el sondeo daría por sana una instalación rota.

**2. Sondear `/health/live` desde el instalador.**
Cambiar `$url` en [10-Test-Installation.ps1:57](setup/hub/bin/10-Test-Installation.ps1:57) a `/health/live`. Es el sondeo correcto para lo que el paso necesita probar: las migraciones corren en `Program.cs` **antes** de que Kestrel acepte peticiones, así que cualquier respuesta HTTP ya demuestra que la base conectó y migró.

`/health/ready` se descartó por una razón concreta: agrega los checks con tag `ready` —`database`, `storage` y `hl7-listener`—, y `Hl7ListenerHealthCheck` devuelve **`Unhealthy`** (no `Degraded`) cuando el listener no está escuchando, lo que vuelve `Unhealthy` el reporte completo y responde **503**. Con `Hl7Enabled = $false` en el `.psd1` —una configuración legítima— el instalador reprobaría una instalación correcta. En modo `Update`, además, dispararía la reversión y borraría un despliegue sano.

Opcional y útil: tras el `live`, consultar `/health/ready` una vez y **volcar su cuerpo al log** como informativo, sin condicionar el resultado del paso. El operador ve el estado de base, disco y listener sin que ninguno de ellos pueda tumbar la instalación.

**3. Fallar rápido ante un 404.**
En `Wait-HubHealth`, tratar el 404 como terminal en vez de reintentable: un 404 significa ruta inexistente, no «aún no está listo». Debe abortar en el primer intento con un mensaje que nombre la causa —ruta no registrada— en lugar de consumir 90 s y culpar al arranque.

### P1 — Que la instalación no termine sin administrador

**4. Sincronizar el `.psd1` del servidor.**
Añadir el bloque `AdminUsername` / `AdminPassword` del `hub-install.example.psd1` (líneas 41-58) al `hub-install.psd1` de D-VPNS y ejecutar `install.ps1 -Mode Repair`. El sembrado es idempotente. Alternativa inmediata sin reinstalar: `scripts\seed-admin.sql`.

**5. Cerrar el hueco en el instalador.**
Con archivo de configuración presente y `AdminPassword` ausente o vacía, `install.ps1` debe **preguntar** por ella —igual que ya hace con `DbPassword` en las líneas 450-454— salvo en `-NonInteractive`, o bien exigirla en la validación cuando el modo sea instalación nueva. Hoy la ausencia atraviesa la validación, el paso 07 y el paso 10 sin que nada la detenga.

**6. Verificar la postcondición del paso 07.**
Cuando `Config.AdminPassword` viene informada, incluir `EDGEGUARD_ADMIN_PASSWORD` en la comprobación de variables escritas ([07-Set-AppPoolConfig.ps1:158](setup/hub/bin/07-Set-AppPoolConfig.ps1:158)). Hoy solo se validan `EDGEGUARD_HUB_CONNECTIONSTRING`, `Jwt__SecretKey` y `DataProtection__KeyPath`.

### P2 — Que el diagnóstico sirva para diagnosticar

**7. Sacar el diagnóstico del override de Serilog.**
Sustituir `ILogger<WebApplication>` por un logger con categoría propia —`"EdgeGuard.Startup"`, siguiendo el patrón ya establecido por `SpaStaticFiles` y `SpaFallback`— en [StartupDiagnosticsExtensions.cs:16](src/backend/Dicom.Edge.Hub.Api/Extensions/StartupDiagnosticsExtensions.cs:16). Sin este cambio, los seis puntos anteriores siguen siendo invisibles en producción.

**8. Corregir la enumeración de endpoints.**
En [línea 171](src/backend/Dicom.Edge.Hub.Api/Extensions/StartupDiagnosticsExtensions.cs:171), leer `((IEndpointRouteBuilder)app).DataSources` en lugar de resolver `EndpointDataSource` desde DI —o mover el bloque a `app.Lifetime.ApplicationStarted`—. Ambas vías quedaron verificadas arriba. Mientras no se corrija, el aviso de `api/auth/login` es ruido garantizado en todos los despliegues.

**9. Añadir una prueba de regresión.**
Un test que arranque el host y afirme que `api/auth/login` figura entre los endpoints registrados. El falso positivo actual existe precisamente porque nada comprueba al verificador.

### P3 — Ruido

**10. Silenciar el `Error` de `__EFMigrationsHistory`.**
Envolver `MigrateHubAsync` ([HubPersistenceServiceCollectionExtensions.cs:114](src/backend/Dicom.Edge.Hub.Persistence/Extensions/HubPersistenceServiceCollectionExtensions.cs:114)) con un filtro que degrade a `Debug` el `CommandError` de la consulta de historial durante el primer arranque, o registrar una línea explicativa antes de migrar. Un `Error` en el arranque de una instalación nueva provoca llamadas de soporte evitables.

**11. Considerar `ProtectKeysWithDpapi()`** para el key ring, y así retirar el aviso `No XML encryptor configured`. Decisión de operación, no defecto.

---

## Orden de ejecución

Los puntos **1, 2 y 3** desbloquean la instalación y son independientes entre sí. El **7** conviene aplicarlo en el mismo lote: sin él, cualquier diagnóstico posterior seguirá a ciegas. El **4** se puede aplicar hoy mismo en D-VPNS sin tocar código.
