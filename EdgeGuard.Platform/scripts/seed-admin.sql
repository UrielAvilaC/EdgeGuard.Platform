-- ============================================================================
-- EdgeGuard Hub — alta manual de la cuenta de administrador
-- ============================================================================
--
-- CUÁNDO USAR ESTO
--
-- El camino normal es el sembrado del propio Hub: AdminUserSeed crea la cuenta
-- al arrancar leyendo EDGEGUARD_ADMIN_USERNAME y EDGEGUARD_ADMIN_PASSWORD del
-- entorno, y el instalador (setup\hub\install.ps1) las escribe a partir de
-- AdminUsername y AdminPassword del .psd1, verifica el resultado con un inicio
-- de sesión real y luego las retira.
--
-- Este script es para los casos en que ese camino ya no sirve:
--
--   · la cuenta existe pero se perdió su contraseña, y el sembrado no la
--     cambia —es idempotente: si el usuario existe, no hace nada—;
--   · el Hub no puede reiniciarse para volver a sembrar.
--
-- Es la última opción, no la primera. Si la instalación es nueva y aún no hay
-- ninguna cuenta, prefiere rellenar AdminPassword en hub-install.psd1 y
-- ejecutar '.\install.ps1 -Mode Repair'.
--
-- NOTA: revisiones de la documentación anteriores a 2026-08 describían un
-- "bootstrap token" de administrador emitido al arrancar y un endpoint
-- POST /api/auth/bootstrap. Ninguno de los dos existe en el código: AuthController
-- solo expone login, refresh, revoke, revoke-all y me. El bootstrap token que sí
-- existe sirve para registrar nodos.
--
-- ----------------------------------------------------------------------------
-- REQUISITO PREVIO: el hash BCrypt
-- ----------------------------------------------------------------------------
--
-- password_hash NO admite una contraseña en claro. El Hub verifica con
-- BCrypt.Net (ver Dicom.Edge.Security\Cryptography\PasswordHasher.cs), así que
-- hay que generar el hash aparte y pegarlo abajo.
--
-- Con el runtime de .NET que ya está en el servidor:
--
--   dotnet script eval "BCrypt.Net.BCrypt.HashPassword(\"TuContraseña\")"
--
-- O desde cualquier máquina con Python:
--
--   python -c "import bcrypt;print(bcrypt.hashpw(b'TuContrasena', bcrypt.gensalt(11)).decode())"
--
-- El resultado empieza por $2a$ o $2b$ y mide 60 caracteres. Cualquier otra
-- cosa hará que el inicio de sesión falle sin explicación útil.
--
-- ----------------------------------------------------------------------------
-- USO
-- ----------------------------------------------------------------------------
--
--   psql -h <host> -U <usuario> -d edgeguard_hub -f seed-admin.sql
--
-- Sustituye los tres valores de la sección :params antes de ejecutar.
-- El script es idempotente: si el usuario ya existe, actualiza su hash y se
-- asegura de que tenga el rol Admin.
--
-- ============================================================================

\set admin_username 'admin'
\set admin_fullname 'Administrador'
\set admin_password_hash '$2a$11$REEMPLAZA_ESTE_VALOR_POR_UN_HASH_BCRYPT_REAL_DE_60_CHARS'

BEGIN;

-- Los valores se pasan al bloque DO como parámetros de sesión, no con la
-- sustitución :'var' de psql: dentro de un literal $$...$$ esa sustitución NO
-- ocurre —el cuerpo del DO es una cadena para psql— y PostgreSQL recibiría los
-- dos puntos literales y fallaría con un error de sintaxis.
SET LOCAL edgeguard.admin_username      = :'admin_username';
SET LOCAL edgeguard.admin_fullname      = :'admin_fullname';
SET LOCAL edgeguard.admin_password_hash = :'admin_password_hash';

DO $$
DECLARE
    v_username   text := current_setting('edgeguard.admin_username');
    v_fullname   text := current_setting('edgeguard.admin_fullname');
    v_hash       text := current_setting('edgeguard.admin_password_hash');
    v_user_id    text;
    v_now        timestamptz := now();
BEGIN
    -- Rechaza el placeholder antes de escribir nada. Sin esta guarda quedaría
    -- una cuenta con un hash inválido: el usuario existe, el inicio de sesión
    -- falla siempre, y la causa no es evidente desde la SPA.
    IF v_hash LIKE '%REEMPLAZA_ESTE_VALOR%' THEN
        RAISE EXCEPTION
            'Sustituye admin_password_hash por un hash BCrypt real antes de ejecutar el script.';
    END IF;

    IF length(v_hash) <> 60 OR v_hash NOT LIKE '$2%$%' THEN
        RAISE EXCEPTION
            'El hash no parece BCrypt (se esperaban 60 caracteres empezando por $2a$ o $2b$; se recibieron % caracteres).',
            length(v_hash);
    END IF;

    -- ── Usuario ─────────────────────────────────────────────────────────────
    SELECT id INTO v_user_id FROM users WHERE username = lower(v_username);

    IF v_user_id IS NULL THEN
        -- El Hub genera ids con su propio IdGenerator; aquí basta con que sean
        -- únicos y quepan en los 50 caracteres de la columna.
        v_user_id := md5(random()::text || clock_timestamp()::text);

        INSERT INTO users (
            id, username, password_hash, full_name, is_active,
            failed_login_attempts, locked_until, password_changed_at,
            last_login_at, created_by, created_at, updated_at
        ) VALUES (
            v_user_id, lower(v_username), v_hash, v_fullname, true,
            0, NULL, v_now,
            NULL, 'seed-admin.sql', v_now, v_now
        );

        RAISE NOTICE 'Usuario % creado con id %', lower(v_username), v_user_id;
    ELSE
        UPDATE users
           SET password_hash        = v_hash,
               full_name            = v_fullname,
               is_active            = true,
               failed_login_attempts = 0,
               locked_until         = NULL,
               password_changed_at  = v_now,
               updated_at           = v_now
         WHERE id = v_user_id;

        RAISE NOTICE 'Usuario % ya existía (id %): contraseña restablecida y cuenta desbloqueada',
                     lower(v_username), v_user_id;
    END IF;

    -- ── Rol Admin ───────────────────────────────────────────────────────────
    -- Security.Authorization.Role se persiste como entero. Admin = 0.
    IF NOT EXISTS (SELECT 1 FROM user_roles WHERE user_id = v_user_id AND role = 0) THEN
        INSERT INTO user_roles (
            id, user_id, role, assigned_at, assigned_by, created_at, updated_at
        ) VALUES (
            md5(random()::text || clock_timestamp()::text), v_user_id, 0, v_now,
            'seed-admin.sql', v_now, v_now
        );
        RAISE NOTICE 'Rol Admin asignado';
    ELSE
        RAISE NOTICE 'El usuario ya tenía el rol Admin';
    END IF;
END $$;

COMMIT;

-- ============================================================================
-- Verificación
-- ============================================================================
SELECT u.username,
       u.full_name,
       u.is_active,
       u.failed_login_attempts,
       u.locked_until,
       r.role AS role_id
  FROM users u
  LEFT JOIN user_roles r ON r.user_id = u.id
 WHERE u.username = :'admin_username';

-- Después de iniciar sesión, cambia la contraseña desde la SPA: el hash de este
-- script pasó por el portapapeles y por el historial del shell.
