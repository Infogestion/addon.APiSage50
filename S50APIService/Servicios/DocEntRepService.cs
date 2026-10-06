using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Documentos de entrega y reparto de la app de repartos (tablas c_doc_ent_rep y d_doc_ent_rep del addon FERRETERIATIA).</summary>
    internal static class DocEntRepService
    {
        public static ListaJson Select()
        {
            return FerreTiasDb.LeerJson<CDocEntRep>("c_doc_ent_rep");
        }

        public static ListaJson SelectType(string type)
        {
            return FerreTiasDb.LeerJson<CDocEntRep>("c_doc_ent_rep", new Filtro { ["TIPO"] = type });
        }

        /// <summary>
        /// Los repartos de un conductor, por fecha de entrega prevista, cada uno con su cliente, su camión, su incidencia y,
        /// si se piden, sus líneas. Como en interface.s50c, el reparto del que no se puede leer algo se queda fuera: con un
        /// {year} sin base de datos no sale ninguno. Los del mismo día salen por número; en interface.s50c, en un orden
        /// distinto en cada llamada.
        /// </summary>
        public static List<DocReparto> SelectRepartosByConductor(string year, string conductor, DateTime? fechaEntregaPrev,
            bool returnLineas, bool onlyNotDelivered, bool onlyLast, string dateLastSync)
        {
            var filtro = new Filtro { ["TIPO"] = "R", ["CONDUCTOR"] = conductor };
            if (fechaEntregaPrev != null)
                filtro.Dia("FECHAENTREGAPREV", fechaEntregaPrev.Value);
            filtro.Sql("[ENTREGADO] = {0}", onlyNotDelivered ? "0" : "1");
            if (onlyLast)
            {
                if (!DateTime.TryParse(dateLastSync, out DateTime desde))
                    return new List<DocReparto>();
                filtro.Sql("[MODIFIED] >= {0}", desde.ToString("yyyy-MM-ddTHH:mm:ss.fff"));
            }

            var repartos = new List<DocReparto>();
            var documentos = FerreTiasDb.Leer<CDocEntRep>("c_doc_ent_rep", filtro)
                .OrderBy(d => d.Fechaentregaprev.Date).ThenBy(d => d.Numero, StringComparer.Ordinal);
            foreach (var documento in documentos)
            {
                try
                {
                    repartos.Add(new DocReparto
                    {
                        CDocEntRep = documento,
                        Mantecamiones = FerreTiasDb.Primero<Mantecamiones>("mantecamiones", "CODIGO", documento.Camion),
                        Details = returnLineas ? Lineas(year, documento.Numero) : null,
                        Cliente = Db.Lector.LeerEjercicio<Clientes>(year, "clientes", new Filtro { ["CODIGO"] = documento.Cliente }).FirstOrDefault(),
                        inciden = IncidenciService.Select(documento.Incidencia),
                        rep_app_reparto = FerreTiasDb.Primero<rep_app_reparto>("rep_app_reparto", "REPENT", documento.Codigo),
                        Ruta = "",
                        Zona = "",
                    });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing item {documento.Numero}: {ex.Message}");
                }
            }
            return repartos;
        }

        /// <summary>
        /// Las líneas de los documentos con ese número (de cualquier ejercicio y empresa), con el nombre de su artículo en el
        /// ejercicio.
        /// </summary>
        private static List<DDocEntRepExt> Lineas(string year, string numero)
        {
            var lineas = FerreTiasDb.Leer<DDocEntRep>("d_doc_ent_rep", new Filtro { ["NUMERO"] = numero });
            var articulos = Db.Lector.LeerEjercicio<CodigoNombre>(year, "articulo", new Filtro().En("CODIGO", lineas.Select(l => l.Articulo)));

            return lineas.Select(l => new DDocEntRepExt(l)
            {
                ArticuloNombre = articulos.SingleOrDefault(a => a.Codigo.Trim() == l.Articulo.Trim())?.Nombre,
            }).ToList();
        }
    }
}
