import { HttpContext, HttpContextToken } from '@angular/common/http';

/**
 * Marca una petición cuyo fallo NO es un error para el usuario, y que por tanto no debe
 * pintar el toast rojo genérico del `errorInterceptor`.
 *
 * Existe porque hay respuestas de error HTTP que son parte del flujo normal. El caso que
 * lo motivó: aplicar configuración a un nodo. Si el nodo no responde en ese momento, el
 * Hub devuelve 400, pero la configuración ya quedó guardada y el nodo la recibirá por el
 * reintento del outbox o por su propio pull. La pantalla lo explica con un aviso
 * informativo; el interceptor, que no puede saberlo, pintaba encima «Ocurrió un error
 * inesperado» y dejaba al operador creyendo que había perdido el cambio.
 *
 * Quien lo usa se hace cargo de informar del fallo en su propio `error:`.
 */
export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

/** Contexto listo para pasar como opción de `HttpClient`. */
export const skipErrorToast = () => new HttpContext().set(SKIP_ERROR_TOAST, true);
