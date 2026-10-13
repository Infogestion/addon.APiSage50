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
    /// La extensión de las líneas de los albaranes de compra (tabla ext_d_albcom del addon Nadilux SGA,
    /// <see cref="Contexto.AddonSga"/>): las unidades que la app lleva recibidas de cada línea.
    /// </summary>
    internal static class DAlbcomExtService
    {
        public static List<ExtDAlbcom> Select(string ejercicio, string empresa, string numero, string proveed)
        {
            return Db.Lector.LeerEjercicio<ExtDAlbcom>(Contexto.AddonSga, "ext_d_albcom", DelAlbaran(ejercicio, empresa, numero, proveed));
        }

        public static ExtDAlbcom SelectOne(string ejercicio, string empresa, string numero, int linea, string proveed)
        {
            return Db.Lector.LeerEjercicio<ExtDAlbcom>(Contexto.AddonSga, "ext_d_albcom", Clave(ejercicio, empresa, numero, linea, proveed)).FirstOrDefault();
        }

        /// <summary>
        /// Crea la extensión de una línea tal como llega y la devuelve. No comprueba que la línea exista y, sin ejercicio,
        /// la crea con el ejercicio en blanco, como interface.s50c.
        /// </summary>
        public static ExtDAlbcom Add(ExtDAlbcom item)
        {
            FilaSql.Insertar(Contexto.AddonSga, "ext_d_albcom", FilaSql.Columnas(item));
            return item;
        }

        /// <summary>
        /// Apunta las unidades recibidas de la línea y devuelve su extensión; si la línea aún no tiene extensión, la crea
        /// en el ejercicio de la ruta.
        /// </summary>
        public static ExtDAlbcom Update(string ejercicio, ExtDAlbcom item)
        {
            var guardada = SelectOne(ejercicio, item.Empresa, item.Numero, item.Linea, item.Proveed);
            if (guardada == null)
            {
                return Add(new ExtDAlbcom
                {
                    Ejercicio = ejercicio,
                    Empresa = item.Empresa,
                    Numero = item.Numero,
                    Linea = item.Linea,
                    Unidadespreparadas = item.Unidadespreparadas,
                    Proveed = item.Proveed,
                });
            }

            guardada.Unidadespreparadas = item.Unidadespreparadas;
            FilaSql.Actualizar(Contexto.AddonSga, "ext_d_albcom", Clave(ejercicio, guardada.Empresa, guardada.Numero, guardada.Linea, guardada.Proveed),
                new Dictionary<string, object> { ["UNIDADESPREPARADAS"] = guardada.Unidadespreparadas, ["MODIFIED"] = DateTime.Now });
            return guardada;
        }

        public static void Remove(ExtDAlbcom item)
        {
            var clave = Clave(item.Ejercicio, item.Empresa, item.Numero, item.Linea, item.Proveed);
            FilaSql.Escribir(Contexto.AddonSga, "DELETE FROM {ext_d_albcom} WHERE " + clave.Condicion, clave.Parametros);
        }

        private static Filtro Clave(string ejercicio, string empresa, string numero, int linea, string proveed)
        {
            return DelAlbaran(ejercicio, empresa, numero, proveed).Exacto("LINEA", linea.ToString(CultureInfo.InvariantCulture));
        }

        private static Filtro DelAlbaran(string ejercicio, string empresa, string numero, string proveed)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero, ["EJERCICIO"] = ejercicio, ["PROVEED"] = proveed };
        }
    }
}
