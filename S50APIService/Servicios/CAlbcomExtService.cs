using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// La extensión de las cabeceras de los albaranes de compra (tabla ext_albcom del addon Nadilux SGA,
    /// <see cref="Contexto.AddonSga"/>): si el albarán se envía a la app, si se está preparando y si está finalizado.
    /// </summary>
    internal static class CAlbcomExtService
    {
        public static List<ExtAlbcom> Select()
        {
            return Db.Lector.LeerEjercicio<ExtAlbcom>(Contexto.AddonSga, "ext_albcom");
        }

        public static List<ExtAlbcom> Select(int page, int pagesize)
        {
            return Db.Lector.LeerEjercicio<ExtAlbcom>(Contexto.AddonSga, new Consulta { Origen = "{ext_albcom}" }.Pagina(page, pagesize));
        }

        public static ExtAlbcom Select(string ejercicio, string empresa, string numero, string proveed)
        {
            return Db.Lector.LeerEjercicio<ExtAlbcom>(Contexto.AddonSga, "ext_albcom", Clave(ejercicio, empresa, numero, proveed)).FirstOrDefault();
        }

        /// <summary>
        /// Apunta en la extensión de un albarán recién creado si se envía a la app, en cuyo caso queda también como "en
        /// preparación". Si el addon no está instalado no hace nada, como en interface.s50c.
        /// </summary>
        public static void Enviar(string ejercicio, CAlbcom albaran, bool enviarAPP)
        {
            try
            {
                var marcas = new Dictionary<string, object> { ["ENVIARAPP"] = enviarAPP, ["PREPARANDO"] = enviarAPP };
                if (Select(ejercicio, albaran.Empresa, albaran.Numero, albaran.Proveedor) != null)
                {
                    marcas["MODIFIED"] = DateTime.Now;
                    FilaSql.Actualizar(Contexto.AddonSga, "ext_albcom", Clave(ejercicio, albaran.Empresa, albaran.Numero, albaran.Proveedor), marcas);
                    return;
                }

                var fila = new Dictionary<string, object>
                {
                    ["EMPRESA"] = albaran.Empresa,
                    ["NUMERO"] = albaran.Numero,
                    ["EJERCICIO"] = ejercicio,
                    ["PROVEED"] = albaran.Proveedor,
                };
                FilaSql.Insertar(Contexto.AddonSga, "ext_albcom", fila.Concat(marcas));
            }
            catch (EjercicioNoEncontradoException)
            {
            }
        }

        /// <summary>
        /// Cambia las tres marcas de la extensión del albarán por las de <paramref name="item"/> y la devuelve. Si el
        /// albarán aún no tiene extensión, la crea con las tres apagadas (no con las que llegan), como interface.s50c.
        /// </summary>
        public static ExtAlbcom Update(string ejercicio, ExtAlbcom item)
        {
            var guardada = Select(ejercicio, item.Empresa, item.Numero, item.Proveed);
            if (guardada == null)
            {
                var nueva = new ExtAlbcom { Empresa = item.Empresa, Numero = item.Numero, Ejercicio = ejercicio, Proveed = item.Proveed };
                FilaSql.Insertar(Contexto.AddonSga, "ext_albcom", FilaSql.Columnas(nueva));
                return nueva;
            }

            guardada.Finalizado = item.Finalizado;
            guardada.Enviarapp = item.Enviarapp;
            guardada.Preparando = item.Preparando;
            FilaSql.Actualizar(Contexto.AddonSga, "ext_albcom", Clave(ejercicio, guardada.Empresa, guardada.Numero, guardada.Proveed), new Dictionary<string, object>
            {
                ["FINALIZADO"] = guardada.Finalizado,
                ["ENVIARAPP"] = guardada.Enviarapp,
                ["PREPARANDO"] = guardada.Preparando,
                ["MODIFIED"] = DateTime.Now,
            });
            return guardada;
        }

        /// <summary>Borra la extensión del albarán y la de sus líneas (ext_d_albcom). Si el addon no está instalado no hace nada.</summary>
        public static void Remove(string ejercicio, CAlbcom albaran)
        {
            try
            {
                Db.Lector.ComprobarEjercicio(Contexto.AddonSga);
                var clave = Clave(ejercicio, albaran.Empresa, albaran.Numero, albaran.Proveedor);
                FilaSql.Escribir(Contexto.AddonSga, "DELETE FROM {ext_albcom} WHERE " + clave.Condicion, clave.Parametros);
                FilaSql.Escribir(Contexto.AddonSga, "DELETE FROM {ext_d_albcom} WHERE " + clave.Condicion, clave.Parametros);
            }
            catch (EjercicioNoEncontradoException)
            {
            }
        }

        /// <summary>
        /// Cada cabecera con su extensión (null si no tiene), que se busca por empresa y número y, si
        /// <paramref name="mismoProveedor"/>, también por proveedor. Con <paramref name="enviarAPP"/> solo quedan los
        /// albaranes que se envían a la app y no están finalizados.
        /// </summary>
        public static List<AlbComComplete> Completar(List<CAlbcom> cabeceras, List<ExtAlbcom> extensiones, bool enviarAPP, bool mismoProveedor)
        {
            if (enviarAPP)
                extensiones = extensiones.Where(e => e.Enviarapp && !e.Finalizado).ToList();

            var porAlbaran = extensiones.ToLookup(e => (e.Numero.Trim(), e.Empresa.Trim(), mismoProveedor ? e.Proveed.Trim() : ""));
            return cabeceras
                .Select(c => new AlbComComplete
                {
                    cabeceraCom = c,
                    cabeceraComExt = porAlbaran[(c.Numero.Trim(), c.Empresa.Trim(), mismoProveedor ? c.Proveedor.Trim() : "")].FirstOrDefault(),
                })
                .Where(x => !enviarAPP || x.cabeceraComExt != null)
                .ToList();
        }

        private static Filtro Clave(string ejercicio, string empresa, string numero, string proveed)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero, ["EJERCICIO"] = ejercicio, ["PROVEED"] = proveed };
        }
    }
}
