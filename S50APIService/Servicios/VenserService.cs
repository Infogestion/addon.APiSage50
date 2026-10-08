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
    /// Series vendidas en los albaranes de venta (tabla venser del ejercicio). Registrar y anular la venta de una serie
    /// se hace con la clase de series de Sage; guardar una fila tal cual llega (Add y Update) no tiene equivalente en Sage
    /// y va por su capa de datos.
    /// </summary>
    internal static class VenserService
    {
        public static ListaJson Select(string year, string empresa, string albaran)
        {
            return Db.Lector.LeerEjercicioJson<Venser>(year, "venser", new Filtro
            {
                ["EMPRESA"] = empresa,
                ["ALBARAN"] = albaran,
            });
        }

        public static Venser Select(string year, string empresa, string albaran, int linea, string serie)
        {
            return Db.Lector.LeerEjercicio<Venser>(year, "venser", new Filtro
            {
                ["EMPRESA"] = empresa,
                ["ALBARAN"] = albaran,
                ["SERIE"] = serie,
            }.Exacto("LINEA", linea.ToString(CultureInfo.InvariantCulture))).FirstOrDefault();
        }

        /// <summary>
        /// Registra una serie como vendida en la línea del albarán con la clase de series de Sage, que además de apuntarla
        /// en venser la da de baja en compras y anota la venta en su historial. Lanza el motivo si no se puede.
        /// </summary>
        public static void Vender(string year, AltaVenserRequest request, string serie)
        {
            Db.Lector.ComprobarEjercicio(year);
            if (ComprasService.SelectBySerie(request.Empresa, request.Articulo, serie) == null)
                throw new InvalidOperationException($"La serie {serie} no existe para el artículo {request.Articulo.Trim()}.");
            if (Select(year, request.Empresa, request.Albaran, request.Linea, serie) != null)
                throw new InvalidOperationException($"La serie {serie} ya está en el albarán.");

            string motivo = Contexto.Escritor.VenderSerie(year, request.Empresa.Trim(), request.Albaran, request.Letra.Trim(), request.Linea,
                request.Articulo.Trim(), serie.Trim());
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        /// <summary>
        /// Anula la venta de una serie con la clase de series de Sage, que además de quitarla de venser la devuelve al
        /// stock en compras y lo anota en su historial. Sage no reconoce las series guardadas con el número de albarán sin
        /// alinear a la derecha (las que creaba interface.s50c). Lanza el motivo si no se puede.
        /// </summary>
        public static void AnularVenta(string year, Venser item)
        {
            if (item.Albaran != item.Albaran.Trim().PadLeft(item.Albaran.Length))
                throw new InvalidOperationException("La serie está guardada con el número de albarán sin alinear y Sage no la reconoce: no se ha anulado.");

            string motivo = Contexto.Escritor.AnularVentaSerie(year, item.Empresa.Trim(), item.Albaran, item.Letra.Trim(), item.Linea,
                item.Articulo.Trim(), item.Serie.TrimEnd());
            if (motivo != null)
                throw new InvalidOperationException(motivo);
        }

        /// <summary>
        /// Crea la serie como el INSERT de EF en interface.s50c: los campos que no vienen se quedan con el valor por defecto
        /// de su columna y vuelven rellenos en <paramref name="item"/>. Si a la clave le falta algún campo no guarda nada:
        /// EF intentaba releer la clave generada y SQL Server lo rechazaba con ese mensaje.
        /// </summary>
        public static void Add(string year, Venser item)
        {
            Db.Lector.ComprobarEjercicio(year);
            if (Clave(item).Any(v => v.Value == null))
                throw new InvalidOperationException("No se puede resolver el conflicto de intercalación entre 'Modern_Spanish_CI_AI' y 'Modern_Spanish_CS_AI' de la operación equal to.");

            FilaSql.Insertar(year, "venser", Clave(item).Concat(Resto(item)));

            var guardada = Guardada(year, item);
            foreach (var campo in typeof(Venser).GetProperties().Where(c => FilaSql.Falta(c.GetValue(item))))
                campo.SetValue(item, campo.GetValue(guardada));
        }

        /// <summary>
        /// Cambia todos los campos de la serie que tiene esa clave (empresa, albarán, letra, artículo, línea y serie), como
        /// el UPDATE de EF en interface.s50c. Si a la clave le falta algún campo, EF la da por nueva e intenta crearla.
        /// </summary>
        public static void Update(string year, Venser item)
        {
            if (Clave(item).Any(v => v.Value == null))
            {
                Add(year, item);
                return;
            }
            if (Guardada(year, item) == null)
                throw new InvalidOperationException("Value cannot be null. (Parameter 'propertyValues')");

            var clave = FiltroClave(item);
            var parametros = new Dictionary<string, string>(clave.Parametros);
            var cambios = Resto(item).Select(v => $"[{v.Key}] = {FilaSql.Valor(v.Value, parametros)}").ToList();
            FilaSql.Escribir(year, $"UPDATE {{venser}} SET {string.Join(", ", cambios)} WHERE {clave.Condicion}", parametros);
        }

        private static Venser Guardada(string year, Venser item)
        {
            return Db.Lector.LeerEjercicio<Venser>(year, "venser", FiltroClave(item)).FirstOrDefault();
        }

        /// <summary>Las columnas de la clave con su valor, en el orden en que EF las escribe (el del nombre de la propiedad).</summary>
        private static Dictionary<string, object> Clave(Venser item) => new Dictionary<string, object>
        {
            ["ALBARAN"] = item.Albaran,
            ["ARTICULO"] = item.Articulo,
            ["EMPRESA"] = item.Empresa,
            ["LETRA"] = item.Letra,
            ["LINEA"] = item.Linea,
            ["SERIE"] = item.Serie,
        };

        /// <summary>Las demás columnas con su valor, en el orden en que EF las escribe (el del nombre de la propiedad).</summary>
        private static Dictionary<string, object> Resto(Venser item) => new Dictionary<string, object>
        {
            ["CREATED"] = item.Created,
            ["FECHALOG"] = item.Fechalog,
            ["GUID_ID"] = item.GuidId,
            ["LOTE"] = item.Lote,
            ["MODELO"] = item.Modelo,
            ["MODIFIED"] = item.Modified,
            ["NUMERO"] = item.Numero,
            ["UBICA"] = item.Ubica,
            ["VISTA"] = item.Vista,
        };

        /// <summary>La fila con exactamente esa clave: los espacios de delante cuentan.</summary>
        private static Filtro FiltroClave(Venser item)
        {
            var filtro = new Filtro();
            foreach (var columna in Clave(item))
                filtro.Exacto(columna.Key, Convert.ToString(columna.Value ?? "", CultureInfo.InvariantCulture));
            return filtro;
        }
    }
}
