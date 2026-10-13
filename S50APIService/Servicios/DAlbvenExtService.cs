using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// La extensión de las líneas de los albaranes de venta (tabla ext_d_albven del addon Nadilux SGA,
    /// <see cref="Contexto.AddonSga"/>): las unidades que la app lleva preparadas de cada línea.
    /// </summary>
    internal static class DAlbvenExtService
    {
        public static List<ExtDAlbven> Select(string ejercicio, string empresa, string numero, string letra)
        {
            return Db.Lector.LeerEjercicio<ExtDAlbven>(Contexto.AddonSga, "ext_d_albven", DelAlbaran(ejercicio, empresa, numero, letra));
        }

        public static ExtDAlbven SelectOne(string ejercicio, string empresa, string numero, string letra, int linea)
        {
            return Db.Lector.LeerEjercicio<ExtDAlbven>(Contexto.AddonSga, "ext_d_albven", Clave(ejercicio, empresa, numero, letra, linea)).FirstOrDefault();
        }

        /// <summary>
        /// Crea la extensión de una línea tal como llega y la devuelve. No comprueba que la línea exista y, sin ejercicio,
        /// la crea con el ejercicio en blanco, como interface.s50c.
        /// </summary>
        public static ExtDAlbven Add(ExtDAlbven item)
        {
            FilaSql.Insertar(Contexto.AddonSga, "ext_d_albven", FilaSql.Columnas(item));
            return item;
        }

        /// <summary>
        /// Apunta las unidades preparadas de la línea y devuelve su extensión; si la línea aún no tiene extensión, la crea
        /// en el ejercicio de la ruta.
        /// </summary>
        public static ExtDAlbven Update(string ejercicio, ExtDAlbven item)
        {
            var guardada = SelectOne(ejercicio, item.Empresa, item.Numero, item.Letra, item.Linea);
            if (guardada == null)
            {
                return Add(new ExtDAlbven
                {
                    Ejercicio = ejercicio,
                    Empresa = item.Empresa,
                    Numero = item.Numero,
                    Linea = item.Linea,
                    Unidadespreparadas = item.Unidadespreparadas,
                    Letra = item.Letra,
                });
            }

            guardada.Unidadespreparadas = item.Unidadespreparadas;
            FilaSql.Actualizar(Contexto.AddonSga, "ext_d_albven", Clave(ejercicio, guardada.Empresa, guardada.Numero, guardada.Letra, guardada.Linea),
                new Dictionary<string, object> { ["UNIDADESPREPARADAS"] = guardada.Unidadespreparadas, ["MODIFIED"] = DateTime.Now });
            return guardada;
        }

        public static void Remove(ExtDAlbven item)
        {
            var clave = Clave(item.Ejercicio, item.Empresa, item.Numero, item.Letra, item.Linea);
            FilaSql.Escribir(Contexto.AddonSga, "DELETE FROM {ext_d_albven} WHERE " + clave.Condicion, clave.Parametros);
        }

        private static Filtro Clave(string ejercicio, string empresa, string numero, string letra, int linea)
        {
            return DelAlbaran(ejercicio, empresa, numero, letra).Exacto("LINEA", linea.ToString(CultureInfo.InvariantCulture));
        }

        private static Filtro DelAlbaran(string ejercicio, string empresa, string numero, string letra)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero, ["EJERCICIO"] = ejercicio, ["LETRA"] = letra };
        }
    }
}
