using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Cabeceras de los albaranes de venta (tabla c_albven del ejercicio).</summary>
    internal static class CAlbvenService
    {
        public static List<CAlbven> Select(string year, int page, int pagesize)
        {
            return Db.Lector.LeerEjercicio<CAlbven>(year, new Consulta { Origen = "{c_albven}" }.Pagina(page, pagesize));
        }

        /// <summary>Las cabeceras con fecha entre las dos dadas, de todas las empresas y almacenes.</summary>
        public static ListaJson Select(string year, DateTime datefrom, DateTime dateto)
        {
            Db.Lector.ComprobarEjercicio(year);
            return Db.Lector.LeerEjercicioJson<CAlbven>(year, "c_albven", CAlbcomService.EntreFechas(datefrom, dateto));
        }

        /// <summary>Las cabeceras de una empresa y un almacén con fecha entre las dos dadas.</summary>
        public static List<CAlbven> SelectByBusiness(string year, DateTime datefrom, DateTime dateto, string business, string warehouse)
        {
            Db.Lector.ComprobarEjercicio(year);
            var filtro = CAlbcomService.EntreFechas(datefrom, dateto);
            filtro["EMPRESA"] = business;
            filtro["ALMACEN"] = warehouse;
            return Db.Lector.LeerEjercicio<CAlbven>(year, "c_albven", filtro);
        }

        public static CAlbven Select(string year, string empresa, string numero, string letra)
        {
            return Db.Lector.LeerEjercicio<CAlbven>(year, "c_albven", Clave(empresa, numero, letra)).FirstOrDefault();
        }

        /// <summary>
        /// Crea la cabecera con la clase de albaranes de venta de Sage, que le da el siguiente número de la serie (el que
        /// llegue se ignora) y le pone la forma de pago, el vendedor, los giros y los portes del cliente, y la devuelve
        /// como queda guardada. Sin forma de pago en la petición ni en el cliente se pone la de por defecto
        /// (<see cref="Contexto.FormaPagoPorDefecto"/>), como en interface.s50c. Del resto de la petición solo se usan
        /// el almacén, el vendedor, la forma de pago y el operario (si vienen), la fecha (hoy si no viene) y las
        /// observaciones.
        /// </summary>
        public static CAlbven Add(string year, CAlbven item)
        {
            Db.Lector.ComprobarEjercicio(year);
            var cliente = Db.Lector.LeerEjercicio<Clientes>(year, "clientes", new Filtro { ["CODIGO"] = item.Cliente }).FirstOrDefault();
            if (cliente == null)
                throw new InvalidOperationException($"No se encontró el cliente='{item.Cliente}'");
            string fpag = item.Fpag;
            if (string.IsNullOrWhiteSpace(fpag) && string.IsNullOrWhiteSpace(cliente.Fpag))
                fpag = Contexto.FormaPagoPorDefecto;
            if (string.IsNullOrWhiteSpace(fpag) && string.IsNullOrWhiteSpace(cliente.Fpag))
                throw new InvalidOperationException("El campo Fpag no puede estar vacío");
            if (!string.IsNullOrWhiteSpace(item.Vendedor) && !Existe(year, "vendedor", item.Vendedor))
                throw new InvalidOperationException($"No se encontró Vendedor para Código='{item.Vendedor}'");
            if (!string.IsNullOrWhiteSpace(item.Almacen) && AlmacenesService.Select(year, item.Almacen) == null)
                throw new InvalidOperationException("El almacén no existe.");
            if (!string.IsNullOrWhiteSpace(item.Operario) && !Existe(Db.Comunes, "operario", item.Operario))
                throw new InvalidOperationException("El operario no existe.");

            DateTime fecha = FilaSql.Falta(item.Fecha) ? DateTime.Today : item.Fecha.Date;
            string motivo = Contexto.Escritor.NuevoAlbaranVenta(year, item.Empresa.Trim(), item.Letra, item.Cliente.Trim(), item.Almacen, item.Vendedor, fpag,
                item.Operario, fecha, item.Observacio ?? "", out string creado);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            FilaSql.Actualizar(year, "c_albven", Clave(item.Empresa, creado, item.Letra), new Dictionary<string, object> { ["USUARIO"] = CAlbcomService.Usuario });
            return Select(year, item.Empresa, creado, item.Letra);
        }

        /// <summary>
        /// Cambia el almacén, la fecha, el operario y las observaciones de la cabecera por los de <paramref name="item"/>,
        /// con la clase de albaranes de venta de Sage, y la devuelve como queda. Si el albarán tiene líneas, el almacén y
        /// la fecha no se pueden cambiar. Lanza el motivo si no se puede.
        /// </summary>
        public static CAlbven Update(string year, CAlbven item)
        {
            if (string.IsNullOrWhiteSpace(item.Almacen) || AlmacenesService.Select(year, item.Almacen) == null)
                throw new InvalidOperationException("El almacén no existe.");
            if (!string.IsNullOrWhiteSpace(item.Operario) && !Existe(Db.Comunes, "operario", item.Operario))
                throw new InvalidOperationException("El operario no existe.");
            if (item.Fecha.Year < 1753)
                throw new InvalidOperationException("SqlDateTime overflow. Must be between 1/1/1753 12:00:00 AM and 12/31/9999 11:59:59 PM.");

            return Cambiar(year, item.Empresa, item.Numero, item.Letra, item.Almacen, item.Fecha.Date, item.Operario ?? "", item.Observacio ?? "");
        }

        /// <summary>Cambia solo las observaciones de la cabecera y la devuelve como queda.</summary>
        public static CAlbven UpdateObs(string year, string empresa, string numero, string letra, string obs)
        {
            return Cambiar(year, empresa, numero, letra, null, null, null, obs);
        }

        private static CAlbven Cambiar(string year, string empresa, string numero, string letra, string almacen, DateTime? fecha, string operario, string observaciones)
        {
            string motivo = Contexto.Escritor.ModificarAlbaranVenta(year, empresa.Trim(), numero, letra, almacen, fecha, operario, observaciones);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            return Select(year, empresa, numero, letra);
        }

        /// <summary>
        /// Borra el albarán con la clase de albaranes de venta de Sage: quita sus líneas devolviendo las unidades al stock
        /// y borra la cabecera con sus giros y portes. Lanza el motivo si Sage no lo permite (facturado, cobrado...).
        /// </summary>
        public static void Remove(string year, CAlbven item)
        {
            string motivo = Contexto.Escritor.BorrarAlbaranVenta(year, item.Empresa.Trim(), item.Numero, item.Letra);
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        private static bool Existe(string baseDatos, string tabla, string codigo)
        {
            return Db.Lector.Leer<CodigoNombre>(baseDatos, tabla, new Filtro { ["CODIGO"] = codigo }).Count > 0;
        }

        private static Filtro Clave(string empresa, string numero, string letra)
        {
            return new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero ?? "", ["LETRA"] = letra };
        }
    }
}
