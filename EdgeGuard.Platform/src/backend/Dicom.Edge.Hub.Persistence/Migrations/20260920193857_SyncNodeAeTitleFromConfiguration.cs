using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Dicom.Edge.Hub.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SyncNodeAeTitleFromConfiguration : Migration
    {
        /// <summary>
        /// Resincroniza <c>nodes.ae_title</c> con el ajuste <c>dicom.ae_title</c>, que es
        /// la fuente única del AE del nodo y el que gobierna la asociación DICOM.
        ///
        /// <para>Sólo cambia el esquema de datos, no la estructura. La columna se fijaba en
        /// el alta y nadie volvía a tocarla, así que en las instalaciones existentes puede
        /// llevar el AE con el que el nodo se registró la primera vez mientras el ajuste ya
        /// tiene otro. Desde ahora el Hub la mantiene al día; esto arrastra lo acumulado.</para>
        ///
        /// <para>Se salta las filas que colisionarían con el índice único de AE: si dos
        /// nodos acabaran con el mismo, la migración fallaría entera. Esos casos son un
        /// error de configuración real —dos nodos no pueden compartir AE— y quedan
        /// deliberadamente sin tocar para que se resuelvan a mano; el Hub los registra en
        /// warning la próxima vez que se guarde ese ajuste.</para>
        /// </summary>
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                UPDATE nodes n
                   SET ae_title  = p.value,
                       updated_at = NOW()
                  FROM node_configuration_profiles p
                 WHERE p.node_id     = n.id
                   AND p.setting_key = 'dicom.ae_title'
                   AND p.value IS NOT NULL
                   AND p.value <> ''
                   AND p.value <> n.ae_title
                   AND NOT EXISTS (
                         SELECT 1
                           FROM nodes otro
                          WHERE otro.id <> n.id
                            AND UPPER(otro.ae_title) = UPPER(p.value)
                       );
                """);
        }

        /// <summary>
        /// No tiene reverso. Los valores anteriores de la columna eran precisamente los que
        /// estaban desactualizados: restaurarlos sería reintroducir la divergencia.
        /// </summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
