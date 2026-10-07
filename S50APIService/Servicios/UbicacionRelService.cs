using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Qué artículo hay en cada ubicación (tabla rel_ubi_alm del addon Nadilux SGA).</summary>
    internal static class UbicacionRelService
    {
        private const string Clave = "[ARTICULO] = @articulo AND [LINEA] = @linea AND [ALMACEN] = @almacen AND [EMPRESA] = @empresa AND [UBICACION] = @ubicacion";

        /// <summary>Las relaciones de una ubicación.</summary>
        public static List<RelUbiAlm> SelectByCode(string code, string emp)
        {
            return Db.Lector.LeerEjercicio<RelUbiAlm>(Contexto.AddonSga, "rel_ubi_alm", new Filtro { ["UBICACION"] = code, ["EMPRESA"] = emp });
        }

        /// <summary>Las relaciones de un artículo.</summary>
        public static List<RelUbiAlm> SelectByCodeAr(string code, string emp)
        {
            return Db.Lector.LeerEjercicio<RelUbiAlm>(Contexto.AddonSga, "rel_ubi_alm", new Filtro { ["ARTICULO"] = code, ["EMPRESA"] = emp });
        }

        /// <summary>Las relaciones de una ubicación en cualquier empresa.</summary>
        public static List<RelUbiAlm> SearchCode(string code)
        {
            return Db.Lector.LeerEjercicio<RelUbiAlm>(Contexto.AddonSga, "rel_ubi_alm", new Filtro { ["UBICACION"] = code });
        }

        /// <summary>
        /// La relación del artículo en el almacén con esa ubicación. Si no la hay, o si la ubicación es el texto "NULL", la
        /// primera que tenga el artículo en el almacén; null si no tiene ninguna.
        /// </summary>
        public static RelUbiAlm SelectRelUbi(string ubicacion, string emp, string articulo, string almacen)
        {
            RelUbiAlm conUbicacion = null;
            if (ubicacion != "NULL")
                conUbicacion = Db.Lector.LeerEjercicio<RelUbiAlm>(Contexto.AddonSga, "rel_ubi_alm",
                    new Filtro { ["UBICACION"] = ubicacion, ["EMPRESA"] = emp, ["ARTICULO"] = articulo, ["ALMACEN"] = almacen }).FirstOrDefault();

            return conUbicacion ?? SelectRelUbiSinCod(emp, articulo, almacen).FirstOrDefault();
        }

        /// <summary>La línea que le toca a una relación nueva del artículo en el almacén: la última más uno.</summary>
        public static int LastRel(string emp, string articulo, string almacen)
        {
            var delAlmacen = SelectRelUbiSinCod(emp, articulo, almacen);
            return delAlmacen.Count == 0 ? 1 : delAlmacen.Max(r => r.Linea) + 1;
        }

        /// <summary>Crea la relación con los valores tal cual llegan, como el INSERT de EF en interface.s50c.</summary>
        public static void Add(RelUbiAlm rel)
        {
            // Columnas en el orden de EF (alfabético): si no caben varias, SQL Server avisa de la primera.
            UbicacionService.Escribir("INSERT INTO {rel_ubi_alm} ([ALMACEN], [ARTICULO], [EMPRESA], [LINEA], [UBICACION])"
                + " VALUES (@almacen, @articulo, @empresa, @linea, @ubicacion)", Parametros(rel));
        }

        /// <summary>Cambia la ubicación de la relación.</summary>
        public static void Update(RelUbiAlm rel, string nuevaUbicacion)
        {
            var parametros = Parametros(rel);
            parametros["@nueva"] = nuevaUbicacion;
            UbicacionService.Escribir("UPDATE {rel_ubi_alm} SET [UBICACION] = @nueva, [MODIFIED] = GETDATE() WHERE " + Clave, parametros);
            rel.Ubicacion = nuevaUbicacion;
        }

        public static void Remove(RelUbiAlm rel)
        {
            UbicacionService.Escribir("DELETE FROM {rel_ubi_alm} WHERE " + Clave, Parametros(rel));
        }

        private static List<RelUbiAlm> SelectRelUbiSinCod(string emp, string articulo, string almacen)
        {
            return Db.Lector.LeerEjercicio<RelUbiAlm>(Contexto.AddonSga, "rel_ubi_alm",
                new Filtro { ["EMPRESA"] = emp, ["ARTICULO"] = articulo, ["ALMACEN"] = almacen });
        }

        private static Dictionary<string, string> Parametros(RelUbiAlm rel)
        {
            return new Dictionary<string, string>
            {
                ["@articulo"] = rel.Articulo,
                ["@linea"] = rel.Linea.ToString(),
                ["@almacen"] = rel.Almacen,
                ["@empresa"] = rel.Empresa,
                ["@ubicacion"] = rel.Ubicacion,
            };
        }
    }
}
