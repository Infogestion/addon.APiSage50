using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Cabeceras de los albaranes de regularización de stock (tabla c_albare del ejercicio).</summary>
    internal static class CAlbareService
    {
        /// <summary>El usuario con el que interface.s50c firma los documentos que crea.</summary>
        private const string Usuario = "INTERFACES_S50C          ";

        public static ListaJson Select(string year, int page, int pagesize)
        {
            return Db.Lector.LeerEjercicioJson<CAlbare>(year, new Consulta { Origen = "{c_albare}" }.Pagina(page, pagesize));
        }

        /// <summary>
        /// Las cabeceras con fecha entre las dos dadas, de un almacén o de todos si no se indica. Una fecha anterior a 1753
        /// (la que llega cuando falta en la petición) es el mismo error que da SQL Server en interface.s50c.
        /// </summary>
        public static ListaJson Select(string year, DateTime datefrom, DateTime dateto, string warehouse)
        {
            Db.Lector.ComprobarEjercicio(year);
            if (datefrom.Year < 1753 || dateto.Year < 1753)
                throw new InvalidOperationException("SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");

            var filtro = new Filtro().Sql("[FECHA] >= CONVERT(datetime2, {0}, 126) AND [FECHA] <= CONVERT(datetime2, {1}, 126)",
                datefrom.ToString("yyyy-MM-ddTHH:mm:ss"), dateto.ToString("yyyy-MM-ddTHH:mm:ss"));
            if (!string.IsNullOrEmpty(warehouse))
                filtro.Igual("ALMACEN", warehouse);

            return Db.Lector.LeerEjercicioJson<CAlbare>(year, "c_albare", filtro);
        }

        public static CAlbare Select(string year, string empresa, string numero)
        {
            return Db.Lector.LeerEjercicio<CAlbare>(year, "c_albare", Clave(empresa, numero)).FirstOrDefault();
        }

        /// <summary>
        /// Crea la cabecera con el siguiente número de regularización, que reserva la clase de regularizaciones de Sage, y
        /// la devuelve como queda guardada. La clase de Sage no guarda una cabecera sin líneas (la borra), así que la fila
        /// se escribe por su capa de datos con lo que trae la petición, como en interface.s50c. La fecha se guarda sin hora
        /// (hoy si no viene), como la guarda Sage, que apunta el stock de las líneas en ese día.
        /// </summary>
        public static CAlbare Add(string year, CAlbare item)
        {
            Db.Lector.ComprobarEjercicio(year);
            ComprobarCodigos(year, item);

            string motivo = Contexto.Escritor.NuevoNumeroRegularizacion(year, item.Empresa.Trim(), out string numero);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            item.Numero = numero.Trim().PadLeft(10);
            item.Fecha = FilaSql.Falta(item.Fecha) ? DateTime.Today : item.Fecha.Date;
            var columnas = FilaSql.Columnas(item, nameof(CAlbare.Empresa), nameof(CAlbare.Numero));
            columnas["USUARIO"] = Usuario;
            FilaSql.Insertar(year, "c_albare", columnas);
            return Select(year, item.Empresa, item.Numero);
        }

        /// <summary>
        /// Cambia el almacén, la fecha, el operario, el tipo y las observaciones de la cabecera <paramref name="guardada"/>
        /// por los de <paramref name="item"/>, y la devuelve como queda. Si el albarán tiene líneas lo hace la clase de
        /// regularizaciones de Sage, que no deja cambiar la fecha si hay unidades, y el almacén no se puede cambiar (Sage
        /// no pasa al nuevo el stock de todas las líneas); si no tiene ninguna, Sage no lo guardaría (lo borra) y se escribe
        /// por su capa de datos. Lanza el motivo si no se puede.
        /// </summary>
        public static CAlbare Update(string year, CAlbare guardada, CAlbare item)
        {
            ComprobarCodigos(year, item);
            if (item.Fecha.Year < 1753)
                throw new InvalidOperationException("SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");

            if (DAlbareService.Select(year, item.Empresa, item.Numero, -1, -1).Count > 0)
            {
                if (guardada.Almacen.Trim() != item.Almacen.Trim())
                    throw new InvalidOperationException("No se puede cambiar el almacén de un albarán de regularización que tiene líneas.");

                string motivo = Contexto.Escritor.ModificarRegularizacion(year, item.Empresa.Trim(), item.Numero, item.Fecha.Date,
                    item.Operario, item.TipoReg ?? "", item.Observacio ?? "");
                if (motivo != null)
                    throw new InvalidOperationException(motivo);
            }
            else
            {
                FilaSql.Actualizar(year, "c_albare", Clave(item.Empresa, item.Numero), new Dictionary<string, object>
                {
                    ["ALMACEN"] = item.Almacen,
                    ["FECHA"] = item.Fecha.Date,
                    ["OPERARIO"] = item.Operario,
                    ["TIPO_REG"] = item.TipoReg ?? "",
                    ["OBSERVACIO"] = item.Observacio ?? "",
                    ["MODIFIED"] = DateTime.Now,
                });
            }
            return Select(year, item.Empresa, item.Numero);
        }

        /// <summary>
        /// Borra el albarán con la clase de regularizaciones de Sage: quita sus líneas devolviendo las unidades al stock y
        /// borra la cabecera. Lanza el motivo si Sage no lo permite.
        /// </summary>
        public static void Remove(string year, CAlbare item)
        {
            string motivo = Contexto.Escritor.BorrarRegularizacion(year, item.Empresa.Trim(), item.Numero);
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        /// <summary>Lo que Sage comprueba al teclear la cabecera: que existen el almacén, el operario y el tipo de regularización (si lleva).</summary>
        private static void ComprobarCodigos(string year, CAlbare item)
        {
            if (AlmacenesService.Select(year, item.Almacen) == null)
                throw new InvalidOperationException("El almacén no existe.");
            if (!Existe("operario", item.Operario))
                throw new InvalidOperationException("El operario no existe.");
            if (!string.IsNullOrWhiteSpace(item.TipoReg) && !Existe("tipo_reg", item.TipoReg))
                throw new InvalidOperationException("El tipo de regularización no existe.");
        }

        private static bool Existe(string tablaComun, string codigo)
        {
            return Db.Lector.Leer<CodigoNombre>(Db.Comunes, tablaComun, new Filtro { ["CODIGO"] = codigo }).Count > 0;
        }

        private static Filtro Clave(string empresa, string numero)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero };
        }
    }
}
