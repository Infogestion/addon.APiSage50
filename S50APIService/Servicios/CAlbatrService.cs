using System;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Cabeceras de los albaranes de traspaso entre almacenes (tabla c_albatr del ejercicio).</summary>
    internal static class CAlbatrService
    {
        /// <summary>El usuario con el que interface.s50c firma los documentos que crea.</summary>
        private const string Usuario = "INTERFACES_S50C          ";

        public static CAlbatr Select(string year, string empresa, string numero)
        {
            if (empresa == null || numero == null)
                throw new InvalidOperationException("Object reference not set to an instance of an object.");

            return Db.Lector.LeerEjercicio<CAlbatr>(year, "c_albatr", new Filtro { ["EMPRESA"] = empresa, ["NUMERO"] = numero }).FirstOrDefault();
        }

        /// <summary>
        /// Crea la cabecera con el siguiente número de traspaso, que reserva la clase de traspasos de Sage y vuelve en
        /// <paramref name="item"/>. La clase de Sage no guarda una cabecera sin líneas (la borra), así que la fila se
        /// escribe por su capa de datos con lo que trae la petición, como en interface.s50c. La fecha se guarda sin hora
        /// (hoy si no viene), como la guarda Sage: con hora, Sage apuntaría el stock de la primera línea en un día y el de
        /// las siguientes en otro.
        /// </summary>
        public static void Add(string year, CAlbatr item)
        {
            Db.Lector.ComprobarEjercicio(year);

            string motivo = Contexto.Escritor.NuevoNumeroTraspaso(year, (item.Empresa ?? "").Trim(), out string numero);
            if (motivo != null)
                throw new InvalidOperationException(motivo);

            item.Usuario = Usuario;
            item.Numero = numero.Trim().PadLeft(10);
            item.Fecha = FilaSql.Falta(item.Fecha) ? DateTime.Today : item.Fecha.Date;
            FilaSql.Insertar(year, "c_albatr", FilaSql.Columnas(item, nameof(CAlbatr.Empresa), nameof(CAlbatr.Numero)));
        }

        /// <summary>
        /// Borra el traspaso con la clase de traspasos de Sage: quita sus líneas, devuelve el stock al almacén de origen,
        /// deshace el traspaso de sus series y borra la cabecera. Lanza el motivo si Sage no lo permite.
        /// </summary>
        public static void Remove(string year, CAlbatr item)
        {
            string motivo = Contexto.Escritor.BorrarTraspaso(year, item.Empresa.Trim(), item.Numero);
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }
    }
}
