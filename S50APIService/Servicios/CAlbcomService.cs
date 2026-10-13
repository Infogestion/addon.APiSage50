using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Cabeceras de los albaranes de compra (tabla c_albcom del ejercicio).</summary>
    internal static class CAlbcomService
    {
        /// <summary>El usuario con el que interface.s50c firma los documentos que crea.</summary>
        private const string Usuario = "INTERFACES_S50C          ";

        public static List<CAlbcom> Select(string year, int page, int pagesize)
        {
            return Db.Lector.LeerEjercicio<CAlbcom>(year, new Consulta { Origen = "{c_albcom}" }.Pagina(page, pagesize));
        }

        /// <summary>Las cabeceras con fecha entre las dos dadas, de todas las empresas y almacenes.</summary>
        public static ListaJson Select(string year, DateTime datefrom, DateTime dateto)
        {
            Db.Lector.ComprobarEjercicio(year);
            return Db.Lector.LeerEjercicioJson<CAlbcom>(year, "c_albcom", EntreFechas(datefrom, dateto));
        }

        /// <summary>Las cabeceras de una empresa y un almacén con fecha entre las dos dadas.</summary>
        public static List<CAlbcom> SelectByBusiness(string year, DateTime datefrom, DateTime dateto, string business, string warehouse)
        {
            Db.Lector.ComprobarEjercicio(year);
            var filtro = EntreFechas(datefrom, dateto);
            filtro["EMPRESA"] = business;
            filtro["ALMACEN"] = warehouse;
            return Db.Lector.LeerEjercicio<CAlbcom>(year, "c_albcom", filtro);
        }

        public static CAlbcom Select(string year, string empresa, string proveedor, string numero)
        {
            return Db.Lector.LeerEjercicio<CAlbcom>(year, "c_albcom", Clave(empresa, proveedor, numero)).FirstOrDefault();
        }

        /// <summary>
        /// Crea la cabecera con la clase de albaranes de compra de Sage, que le pone la forma de pago y los giros del
        /// proveedor, y la devuelve como queda guardada. Sin número (vacío o negativo, como el "-1" que manda la app) Sage
        /// le da el siguiente del proveedor. La fecha se guarda sin hora (hoy si no viene).
        /// </summary>
        public static CAlbcom Add(string year, CAlbcom item)
        {
            Db.Lector.ComprobarEjercicio(year);
            if (!Existe(year, "proveed", item.Proveedor))
                throw new InvalidOperationException($"No se encontró el Proveedor por Código='{item.Proveedor}'");
            if (AlmacenesService.Select(year, item.Almacen) == null)
                throw new InvalidOperationException("El almacén no existe.");

            string numero = long.TryParse(item.Numero, out long negativo) && negativo < 0 ? "" : item.Numero.Trim();
            if (numero.Length > 10)
                throw new InvalidOperationException("El número del albarán no puede tener más de 10 caracteres.");
            if (numero != "" && Select(year, item.Empresa, item.Proveedor, numero) != null)
                throw new InvalidOperationException("Ya existe un albarán de compra con ese número para el proveedor.");

            DateTime fecha = FilaSql.Falta(item.Fecha) ? DateTime.Today : item.Fecha.Date;
            string motivo = Contexto.Escritor.NuevoAlbaranCompra(year, item.Empresa.Trim(), item.Proveedor.Trim(), numero, item.Almacen, fecha,
                item.Observacio ?? "", out string creado);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            FilaSql.Actualizar(year, "c_albcom", Clave(item.Empresa, item.Proveedor, creado), new Dictionary<string, object> { ["USUARIO"] = Usuario });
            return Select(year, item.Empresa, item.Proveedor, creado);
        }

        /// <summary>
        /// Cambia el almacén, la fecha, el operario y las observaciones de la cabecera por los de <paramref name="item"/>,
        /// y la devuelve como queda. Si el albarán tiene líneas, el almacén y la fecha no se pueden cambiar. Lanza el
        /// motivo si no se puede.
        /// </summary>
        public static CAlbcom Update(string year, CAlbcom item)
        {
            if (AlmacenesService.Select(year, item.Almacen) == null)
                throw new InvalidOperationException("El almacén no existe.");
            if (!string.IsNullOrWhiteSpace(item.Operario) && !Existe(Db.Comunes, "operario", item.Operario))
                throw new InvalidOperationException("El operario no existe.");
            if (item.Fecha.Year < 1753)
                throw new InvalidOperationException("SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");

            return Cambiar(year, item.Empresa, item.Proveedor, item.Numero, item.Almacen, item.Fecha.Date, item.Operario ?? "", item.Observacio ?? "");
        }

        /// <summary>Cambia solo las observaciones de la cabecera y la devuelve como queda.</summary>
        public static CAlbcom UpdateObs(string year, string empresa, string proveedor, string numero, string obs)
        {
            return Cambiar(year, empresa, proveedor, numero, null, null, null, obs);
        }

        /// <summary>
        /// Si el albarán tiene líneas lo cambia la clase de albaranes de compra de Sage; si no tiene ninguna, Sage no lo
        /// carga y se escribe por su capa de datos. Sin <paramref name="almacen"/> solo cambian las observaciones.
        /// </summary>
        private static CAlbcom Cambiar(string year, string empresa, string proveedor, string numero, string almacen, DateTime? fecha, string operario, string observaciones)
        {
            string motivo = Contexto.Escritor.ModificarAlbaranCompra(year, empresa.Trim(), proveedor.Trim(), numero, almacen, fecha, operario, observaciones,
                out bool sinLineas);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            if (sinLineas)
            {
                var columnas = new Dictionary<string, object> { ["OBSERVACIO"] = observaciones, ["MODIFIED"] = DateTime.Now };
                if (almacen != null)
                {
                    columnas["ALMACEN"] = almacen;
                    columnas["FECHA"] = fecha;
                    columnas["OPERARIO"] = operario;
                }
                FilaSql.Actualizar(year, "c_albcom", Clave(empresa, proveedor, numero), columnas);
            }
            return Select(year, empresa, proveedor, numero);
        }

        /// <summary>
        /// Borra el albarán con la clase de albaranes de compra de Sage: quita sus líneas devolviendo las unidades al stock
        /// y borra la cabecera con sus giros. Lanza el motivo si Sage no lo permite (facturado, traspasado...).
        /// </summary>
        public static void Remove(string year, CAlbcom item)
        {
            string motivo = Contexto.Escritor.BorrarAlbaranCompra(year, item.Empresa.Trim(), item.Proveedor.Trim(), item.Numero);
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        private static bool Existe(string baseDatos, string tabla, string codigo)
        {
            return Db.Lector.Leer<CodigoNombre>(baseDatos, tabla, new Filtro { ["CODIGO"] = codigo }).Count > 0;
        }

        /// <summary>
        /// La fecha del albarán está entre las dos dadas. Una fecha anterior a 1753 es el mismo error que da SQL Server en
        /// interface.s50c. Las fechas se redondean al milisegundo, como al pasar a SQL Server en interface.s50c: el último
        /// instante de un día se convierte en las 00:00 del día siguiente.
        /// </summary>
        private static Filtro EntreFechas(DateTime datefrom, DateTime dateto)
        {
            if (datefrom.Year < 1753 || dateto.Year < 1753)
                throw new InvalidOperationException("SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");

            return new Filtro().Sql("[FECHA] >= CONVERT(datetime, {0}, 126) AND [FECHA] <= CONVERT(datetime, {1}, 126)", AlMilisegundo(datefrom), AlMilisegundo(dateto));
        }

        private static string AlMilisegundo(DateTime fecha)
        {
            long milisegundos = (fecha.Ticks + TimeSpan.TicksPerMillisecond / 2) / TimeSpan.TicksPerMillisecond;
            return new DateTime(milisegundos * TimeSpan.TicksPerMillisecond).ToString("yyyy-MM-ddTHH:mm:ss.fff");
        }

        private static Filtro Clave(string empresa, string proveedor, string numero)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["PROVEEDOR"] = proveedor, ["NUMERO"] = numero };
        }
    }
}
