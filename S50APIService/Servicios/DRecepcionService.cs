using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>
    /// Líneas de las recepciones de mercancías. Vale para las dos clases de <see cref="Recepcion"/>: hace de
    /// DRecepcionService y de DRecepcionSinEnviosService de interface.s50c.
    /// </summary>
    internal sealed class DRecepcionService<T> where T : new()
    {
        private readonly Recepcion _recepcion;

        public DRecepcionService(Recepcion recepcion)
        {
            _recepcion = recepcion;
        }

        /// <summary>
        /// Una página de las líneas del documento, por orden de línea. Con algún filtro solo salen las líneas de los
        /// artículos que cumplen al menos uno de ellos.
        /// </summary>
        public List<T> GetRecepcionDetails(string year, string numero, string ejercicio, string empresa, int page, int pageSize,
            string barra, string proveedor, string codArt, string descripcion)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 50 : pageSize;

            var filtro = Recepcion.Documento(numero, ejercicio, empresa);
            if (barra != null || proveedor != null || codArt != null || descripcion != null)
            {
                var codigos = CodigosFiltrados(year, numero, ejercicio, empresa, barra, proveedor, codArt, descripcion);
                if (codigos.Count == 0)
                    return new List<T>();
                filtro.En("CODIGO", codigos);
            }

            return Db.Lector.LeerEjercicio<T>(Recepcion.Addon, new Consulta
            {
                Origen = "{" + _recepcion.Lineas + "}",
                Filtro = filtro,
                Orden = "LINEA",
            }.Pagina(page, pageSize));
        }

        /// <summary>
        /// Los artículos del documento con ese código o con ese texto en la descripción, más los del ejercicio que tienen
        /// ese código de barras o son de ese proveedor.
        /// </summary>
        private List<string> CodigosFiltrados(string year, string numero, string ejercicio, string empresa,
            string barra, string proveedor, string codArt, string descripcion)
        {
            var codigos = new List<string>();

            if (codArt != null)
                codigos.AddRange(Lineas(Recepcion.Documento(numero, ejercicio, empresa).Igual("CODIGO", codArt)).Select(l => l.Codigo));

            if (descripcion != null)
                codigos.AddRange(Lineas(Recepcion.Documento(numero, ejercicio, empresa)
                    .Sql("CHARINDEX({0}, UPPER(LTRIM(RTRIM([DESCRIPCION])))) > 0", descripcion.Trim().ToUpper())).Select(l => l.Codigo));

            if (barra != null)
                codigos.AddRange(Db.Lector.LeerEjercicio<ArticuloRelacionado>(year, "barras", new Filtro { ["BARRAS"] = barra }).Select(b => b.Articulo));

            if (proveedor != null)
                codigos.AddRange(Db.Lector.LeerEjercicio<ArticuloRelacionado>(year, "referpro", new Filtro { ["PROVEEDOR"] = proveedor }).Select(r => r.Articulo));

            return codigos.Select(c => c.Trim()).Distinct().ToList();
        }

        /// <summary>
        /// Apunta en una línea las unidades traspasadas, con las mismas cuentas que interface.s50c, y la guarda con la clase
        /// de negocio del addon. Si después no queda ninguna línea con cantidad, el documento pasa a "Traspasado".
        /// </summary>
        public bool CambiarUnidadesTraspasadas(string num, int linea, decimal udstraspasadas, string ejercicio, string empresa, decimal udsTotalesTraspasadas)
        {
            try
            {
                var lineas = Lineas(Recepcion.Documento(num, ejercicio, empresa));
                var registro = lineas.FirstOrDefault(l => l.Linea == linea);
                var cabecera = _recepcion.Cabecera(num, ejercicio, empresa);
                if (registro == null || cabecera == null)
                    return false;

                decimal realmenteTraspasadas = Math.Min(registro.Stockorigen, udstraspasadas);

                registro.Traspasado = Guardable(registro.Traspasado + realmenteTraspasadas);
                registro.Stockorigen = Guardable(registro.Stockorigen - udsTotalesTraspasadas);
                registro.Stockdestino = Guardable(registro.Stockdestino + realmenteTraspasadas);
                registro.Cantidad = Guardable(registro.Cantidad - udsTotalesTraspasadas);

                var cambios = _recepcion.Cambios(cabecera);
                cambios.Linea = linea;
                cambios.DeLinea["_Traspasado"] = registro.Traspasado;
                cambios.DeLinea["_Stockorigen"] = registro.Stockorigen;
                cambios.DeLinea["_Stockdestino"] = registro.Stockdestino;
                cambios.DeLinea["_Cantidad"] = registro.Cantidad;
                if (cambios.DeLinea.Values.Any(NoCabe))
                    return false;
                if (!lineas.Any(l => l.Cantidad > 0))
                    cambios.Cabecera["_Estadoo"] = "Traspasado";

                return _recepcion.Guardar(cambios);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private List<LineaRecepcion> Lineas(Filtro filtro)
        {
            return Db.Lector.LeerEjercicio<LineaRecepcion>(Recepcion.Addon, _recepcion.Lineas, filtro);
        }

        /// <summary>Con los dos decimales de la columna, redondeado como lo guarda SQL Server en interface.s50c.</summary>
        private static decimal Guardable(decimal unidades)
        {
            return Math.Round(unidades, 2, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Las columnas son numeric(20,2): 18 cifras enteras. Con un valor mayor, interface.s50c no guarda nada; Sage dejaría
        /// la línea como estaba pero guardaría la cabecera.
        /// </summary>
        private static bool NoCabe(decimal unidades)
        {
            return Math.Abs(unidades) >= 1000000000000000000m;
        }
    }
}
