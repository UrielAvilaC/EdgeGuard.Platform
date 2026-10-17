using System.Linq;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <summary>
    /// Dos arreglos de datos sobre la configuración de nodos. No toca el esquema.
    ///
    /// <para><b>Cuota de almacenamiento.</b> <c>nodes.storage_limit_mb</c> es el valor que
    /// el Hub baja al nodo en cada sync, y <c>null</c> ahí significa "sin límite". Los
    /// nodos dados de alta por registro nunca recibían un valor —el alta no pregunta por
    /// la cuota— así que quedaban en <c>null</c> sin que nadie lo hubiera decidido y el
    /// sync les bajaba un <c>0</c>, dejándolos sin cuota mientras la pantalla seguía
    /// mostrando los 100 GB del catálogo. A partir de ahora nacen con el default (ver
    /// <c>EdgeNodeService</c>/<c>NodeService</c>); esto pone al día a los existentes, de
    /// modo que <c>null</c> vuelva a significar sólo lo que el operador vació a mano.</para>
    ///
    /// <para><b>Ajustes retirados.</b> Tres claves salieron del catálogo pero siguieron
    /// vivas en el perfil de los nodos que ya existían, porque el sembrado no borra nada.
    /// Se ven y se editan en la pantalla de configuración sin que el nodo lea ninguna:
    /// <list type="bullet">
    ///   <item><c>cleanup.max_storage_gb</c> — reemplazada por la cuota en MB del agregado.</item>
    ///   <item><c>storage.archive_path</c> — el nodo ya no mantiene carpeta de archivo.</item>
    ///   <item><c>sender.local_ae_title</c> — el Calling AE de salida se deriva en runtime
    ///   de <c>dicom.ae_title</c>; editar esta fila no cambiaba nada.</item>
    ///   <item><c>node.ae_title</c> — espejo de presentación del mismo AE canónico, que
    ///   se sembraba una vez y nunca se refrescaba: mostraba el valor viejo en cuanto
    ///   alguien cambiaba el AE de verdad.</item>
    ///   <item><c>storage.limit_mb</c> — la cuota vive en <c>nodes.storage_limit_mb</c> y
    ///   el sync sobrescribe con ese valor lo que traiga el perfil, así que la fila era un
    ///   campo editable sin efecto y metía la clave dos veces en el hash de versión.</item>
    /// </list>
    /// Retirar un ajuste es un acto deliberado, y por eso se borra aquí y no con un
    /// barrido automático contra el catálogo: ese barrido, ejecutado por una versión
    /// anterior del Hub, borraría los ajustes que una posterior acababa de añadir.</para>
    /// </summary>
    public partial class SeedNodeStorageLimitAndDropRetiredSettings : Migration
    {
        /// <summary>Debe coincidir con <c>SharedNodeSettingDefaults.DefaultStorageLimitMb</c>.</summary>
        private const long DefaultStorageLimitMb = 102_400;

        private static readonly string[] RetiredSettingKeys =
        [
            "cleanup.max_storage_gb",
            "storage.archive_path",
            "sender.local_ae_title",
            "node.ae_title",
            "storage.limit_mb",
        ];

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $"UPDATE nodes SET storage_limit_mb = {DefaultStorageLimitMb} " +
                "WHERE storage_limit_mb IS NULL;");

            var keys = string.Join(", ", RetiredSettingKeys.Select(k => $"'{k}'"));
            migrationBuilder.Sql(
                $"DELETE FROM node_configuration_profiles WHERE setting_key IN ({keys});");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Sin vuelta atrás a propósito. La cuota no se puede devolver a null sin
            // inventarse qué nodos la tenían así, y resembrar los ajustes retirados
            // volvería a poner en pantalla campos que no hacen nada.
        }
    }
}
