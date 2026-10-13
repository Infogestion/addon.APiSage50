using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// La extensión de las cabeceras de los albaranes de venta (tabla ext_albven del addon Nadilux SGA,
    /// <see cref="Contexto.AddonSga"/>): si el albarán se envía a la app, si se está preparando y si está finalizado.
    /// </summary>
    internal static class CAlbvenExtService
    {
        public static List<ExtAlbven> Select()
        {
            return Db.Lector.LeerEjercicio<ExtAlbven>(Contexto.AddonSga, "ext_albven");
        }

        public static List<ExtAlbven> Select(int page, int pagesize)
        {
            return Db.Lector.LeerEjercicio<ExtAlbven>(Contexto.AddonSga, new Consulta { Origen = "{ext_albven}" }.Pagina(page, pagesize));
        }

        public static ExtAlbven Select(string ejercicio, string empresa, string numero, string letra)
        {
            return Db.Lector.LeerEjercicio<ExtAlbven>(Contexto.AddonSga, "ext_albven", Clave(ejercicio, empresa, numero, letra)).FirstOrDefault();
        }

        /// <summary>
        /// Cambia las tres marcas de la extensión del albarán por las de <paramref name="item"/> y la devuelve. Si el
        /// albarán aún no tiene extensión, la crea con las tres apagadas (no con las que llegan), como interface.s50c.
        /// </summary>
        public static ExtAlbven Update(string ejercicio, ExtAlbven item)
        {
            var guardada = Select(ejercicio, item.Empresa, item.Numero, item.Letra);
            if (guardada == null)
            {
                var nueva = new ExtAlbven { Ejercicio = ejercicio, Empresa = item.Empresa, Numero = item.Numero, Letra = item.Letra };
                FilaSql.Insertar(Contexto.AddonSga, "ext_albven", FilaSql.Columnas(nueva));
                return nueva;
            }

            guardada.Finalizado = item.Finalizado;
            guardada.Enviarapp = item.Enviarapp;
            guardada.Preparando = item.Preparando;
            FilaSql.Actualizar(Contexto.AddonSga, "ext_albven", Clave(ejercicio, guardada.Empresa, guardada.Numero, guardada.Letra), new Dictionary<string, object>
            {
                ["FINALIZADO"] = guardada.Finalizado,
                ["ENVIARAPP"] = guardada.Enviarapp,
                ["PREPARANDO"] = guardada.Preparando,
                ["MODIFIED"] = DateTime.Now,
            });
            return guardada;
        }

        /// <summary>
        /// Borra la extensión del albarán, para que no la herede otro albarán que reciba después el mismo número. Si el
        /// addon no está instalado no hace nada.
        /// </summary>
        public static void Remove(string ejercicio, CAlbven albaran)
        {
            try
            {
                Db.Lector.ComprobarEjercicio(Contexto.AddonSga);
                var clave = Clave(ejercicio, albaran.Empresa, albaran.Numero, albaran.Letra);
                FilaSql.Escribir(Contexto.AddonSga, "DELETE FROM {ext_albven} WHERE " + clave.Condicion, clave.Parametros);
            }
            catch (EjercicioNoEncontradoException)
            {
            }
        }

        /// <summary>
        /// Cada cabecera con su extensión (null si no tiene), que se busca por empresa, número y letra. Con
        /// <paramref name="enviarAPP"/> solo quedan los albaranes que se envían a la app y no están finalizados.
        /// </summary>
        public static List<AlbVenComplete> Completar(List<CAlbven> cabeceras, List<ExtAlbven> extensiones, bool enviarAPP)
        {
            if (enviarAPP)
                extensiones = extensiones.Where(e => e.Enviarapp && !e.Finalizado).ToList();

            var porAlbaran = extensiones.ToLookup(e => (e.Numero.Trim(), e.Letra.Trim(), e.Empresa.Trim()));
            return cabeceras
                .Select(c => new AlbVenComplete
                {
                    cabeceraVen = c,
                    cabeceraVenExt = porAlbaran[(c.Numero.Trim(), c.Letra.Trim(), c.Empresa.Trim())].FirstOrDefault(),
                })
                .Where(x => !enviarAPP || x.cabeceraVenExt != null)
                .ToList();
        }

        private static Filtro Clave(string ejercicio, string empresa, string numero, string letra)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero, ["LETRA"] = letra, ["EJERCICIO"] = ejercicio };
        }
    }
}
