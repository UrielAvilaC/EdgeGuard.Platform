using Microsoft.EntityFrameworkCore;

namespace Dicom.Edge.Hub.Persistence.Repositories;

/// <summary>
/// Invariante de escritura para las entidades que declaran un token de concurrencia
/// optimista: <c>Node</c> y <c>Study</c>, ambas con <c>UpdatedAt</c> como token.
///
/// <para>Para esas entidades, pasar una instancia <b>desconectada</b> a
/// <c>DbSet.Update()</c> es siempre incorrecto, y es incorrecto de una forma que nadie
/// ve leyendo el código. Al adjuntar el grafo, EF fija los valores originales iguales a
/// los actuales —no tiene de dónde sacar otros, porque la lectura con
/// <c>AsNoTracking()</c> descartó el snapshot—. Y como todo mutador de dominio reescribe
/// <c>UpdatedAt</c>, el <c>WHERE</c> del <c>UPDATE</c> termina llevando el timestamp
/// <i>nuevo</i> en lugar del que está en la base: no coincide con ninguna fila, EF cuenta
/// 0 afectadas y lanza <see cref="DbUpdateConcurrencyException"/>.</para>
///
/// <para>El costo de no tener esta guarda está medido. El evaluador de salud de nodos
/// vivió así en producción: cada minuto decidía correctamente que un nodo estaba caído,
/// cada minuto fallaba al guardarlo, y el nodo siguió figurando <c>Online</c> en el panel
/// durante más de veinte horas. Lo que hizo lento el diagnóstico es que la excepción
/// dice "concurrencia" cuando no había ningún segundo escritor: el mensaje apunta al
/// lugar equivocado. Fallar aquí —en el momento exacto, nombrando la causa— convierte
/// ese fallo silencioso en uno que la primera prueba detecta.</para>
///
/// <para>La corrección nunca es adjuntar mejor: es <b>leer rastreado</b> en los caminos
/// de lectura-modificación-escritura. El valor original sólo puede venir de la base de
/// datos, así que ningún truco sobre la entidad desconectada puede recuperarlo.</para>
/// </summary>
internal static class TrackedEntityGuard
{
    /// <summary>
    /// Verifica que <paramref name="entity"/> esté rastreada por <paramref name="context"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Si la entidad llega desconectada, con un mensaje que nombra el método de lectura
    /// rastreada que corresponde usar.
    /// </exception>
    public static void EnsureTracked<TEntity>(
        DbContext context,
        TEntity entity,
        string trackedReadHint)
        where TEntity : class
    {
        if (context.Entry(entity).State != EntityState.Detached)
            return;

        throw new InvalidOperationException(
            $"Se intentó actualizar una entidad {typeof(TEntity).Name} desconectada del DbContext. " +
            $"{typeof(TEntity).Name} declara UpdatedAt como token de concurrencia, así que EF " +
            "construiría el UPDATE con el valor ya mutado del token y no afectaría ninguna fila " +
            "(DbUpdateConcurrencyException engañosa, sin que exista concurrencia real). " +
            $"El camino correcto es leer rastreado: {trackedReadHint}.");
    }
}
