using System;
using System.Collections.Generic;
using System.Linq;
using S50APIService.Api;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Servicios
{
    /// <summary>Documentos de mercancías de la app de reposición (tablas c_doc y d_doc del addon GESTIONMERC).</summary>
    internal static class DocMercanciasService
    {
        /// <summary>
        /// Los documentos que cumplen los filtros que vengan, con el nombre de su proveedor. Con operario solo salen los
        /// que no están cerrados. Como en interface.s50c, se descartan los documentos cuyo albarán no está en el ejercicio.
        /// </summary>
        public static List<DocMercancia> Select(string operario, DateTime? fecha, string proveedor, string albaran)
        {
            var filtro = new Filtro();
            if (operario != null)
                filtro.Igual("OPERARIO", operario).Distinto("STATE", "CERRADO");
            if (fecha != null)
                filtro.Dia("FECHA", fecha.Value);
            if (albaran != null)
                filtro.Igual("ALB_NUMERO", albaran);
            var documentos = MercanciasDb.DelAddon<CDoc>("c_doc", filtro);

            var numeros = new HashSet<string>(documentos.Select(d => d.alb_numero.Trim()));
            var albaranes = MercanciasDb.DelEjercicio<AlbaranCompra>("c_albcom")
                .Where(a => numeros.Contains(a.Numero.Trim()))
                .ToDictionary(a => a.Numero.Trim());
            var codigos = new HashSet<string>(documentos.Select(d => d.proveedor.Trim()));
            var proveedores = MercanciasDb.DelEjercicio<CodigoNombre>("proveed")
                .Where(p => codigos.Contains(p.Codigo.Trim()))
                .ToDictionary(p => p.Codigo.Trim(), p => p.Nombre);

            var resultado = new List<DocMercancia>();
            foreach (var documento in documentos)
            {
                if (!albaranes.TryGetValue(documento.alb_numero.Trim(), out var albaranCompra) || albaranCompra.Proveedor == null)
                    continue;
                proveedores.TryGetValue(albaranCompra.Proveedor.Trim(), out string nombre);
                if (proveedor != null && (nombre == null || !nombre.ToLower().Contains(proveedor)))
                    continue;
                resultado.Add(new DocMercancia { CDocMercancia = documento, Proveedor = nombre });
            }
            return resultado;
        }

        /// <summary>
        /// El documento con sus líneas y el nombre de su proveedor; null si no existe. Como en interface.s50c, las líneas son
        /// las de todos los documentos con ese número (de cualquier ejercicio y empresa), y si falla algo al leerlas el
        /// resultado es null, igual que si el documento no existiera.
        /// </summary>
        public static DocMercancia SelectOne(string numero, string ejercicio, string empresa)
        {
            var cabecera = Cabecera(numero, ejercicio, empresa);
            if (cabecera == null)
                return null;

            try
            {
                var lineas = MercanciasDb.DelAddon<DDoc>("d_doc", new Filtro { ["NUMERO"] = numero });
                var nombres = MercanciasDb.DelEjercicio<CodigoNombre>("articulo", new Filtro().En("CODIGO", lineas.Select(l => l.ARTICULO).Distinct()));

                return new DocMercancia
                {
                    CDocMercancia = cabecera,
                    Proveedor = NombreProveedor(cabecera.alb_numero),
                    Details = lineas.Select(l => new DDocMercanciaExt(l)
                    {
                        ArticuloNombre = nombres.SingleOrDefault(a => a.Codigo.Trim() == l.ARTICULO.Trim())?.Nombre,
                    }).ToList(),
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR al leer el documento de mercancías {numero}: {ex.Message}");
                return null;
            }
        }

        /// <summary>La línea del documento del artículo que tiene ese código de barras, con sus unidades solicitadas; null si no hay.</summary>
        public static DDocMercanciaExt GetDetailByBarcode(string numero, string ejercicio, string empresa, string barcode)
        {
            var documento = SelectOne(numero, ejercicio, empresa);
            if (documento?.Details == null || documento.Details.Count == 0)
                return null;

            var articulos = MercanciasDb.DelEjercicio<CodigoNombre>(new Consulta
            {
                Origen = "{articulo} AS [a] INNER JOIN {barras} AS [b] ON [a].[CODIGO] = [b].[ARTICULO]",
                Alias = "a",
                Filtro = new Filtro().Exacto("b.BARRAS", barcode.Trim()),
            });

            DDocMercanciaExt linea = null;
            foreach (var articulo in articulos)
                linea = Linea(documento, numero, ejercicio, empresa, articulo.Codigo) ?? linea;
            return linea;
        }

        /// <summary>La línea del documento de ese artículo, con sus unidades solicitadas; null si no hay.</summary>
        public static DDocMercanciaExt GetDetail(string numero, string ejercicio, string empresa, string code)
        {
            return Linea(SelectOne(numero, ejercicio, empresa), numero, ejercicio, empresa, code);
        }

        /// <summary>
        /// Los artículos del documento cuyo nombre contiene el texto; si no hay ninguno, los que tienen una referencia de
        /// proveedor que lo contiene.
        /// </summary>
        public static List<Articulo> GetDetailsSearch(string numero, string ejercicio, string empresa, string query)
        {
            var codigos = AlmaSolicitudService.GetDetails(numero, empresa, ejercicio).Select(s => s.Articulo).Distinct().ToList();
            if (codigos.Count == 0)
                return new List<Articulo>();

            var porNombre = MercanciasDb.DelEjercicio<Articulo>("articulo", new Filtro().En("CODIGO", codigos).Contiene("NOMBRE", query.Trim()));
            if (porNombre.Count > 0)
                return porNombre;

            return MercanciasDb.DelEjercicio<Articulo>(new Consulta
            {
                Origen = "{articulo} AS [a]",
                Alias = "a",
                Filtro = new Filtro().En("a.CODIGO", codigos).Sql(
                    "EXISTS (SELECT 1 FROM {referpro} AS [r] WHERE LTRIM(RTRIM([r].[ARTICULO])) = LTRIM(RTRIM([a].[CODIGO])) AND LOWER([r].[PROVEEDOR]) LIKE {0})",
                    "%" + query.Trim().ToLower() + "%"),
            });
        }

        /// <summary>
        /// Guarda las observaciones y el estado del documento con la clase de negocio del addon. Si no cambia nada no se
        /// guarda, porque Sage volvería a grabar el documento entero.
        /// </summary>
        public static ResultadoEscritura UpdateMercancia(string numero, string empresa, string ejercicio, string obs, string state)
        {
            try
            {
                var documento = Cabecera(numero, ejercicio, empresa);
                if (documento == null)
                    return ResultadoEscritura.Error("No se encontró el documento.");

                var cambios = new Dictionary<string, object>();
                if (obs != null && obs != documento.COMMENTS)
                    cambios["_Comments"] = obs;
                if (state.Trim() != documento.STATE.Trim())
                    cambios["_State"] = state.Trim();
                if (cambios.Count == 0)
                    return ResultadoEscritura.Guardado();
                if (state.Trim().Length > 20)
                    return ResultadoEscritura.Error(ResultadoEscritura.ErrorAlGuardar);

                string motivo = Contexto.Escritor.GuardarDocumento(new CambiosDocumento
                {
                    Libreria = "sage.addons.GestionMerc",
                    Clase = "sage.addons.GestionMerc.Negocio.Documentos.Documento",
                    Ejercicio = documento.EJERCICIO.Trim(),
                    Empresa = documento.EMPRESA.Trim(),
                    Numero = documento.NUMERO.Trim(),
                    Cabecera = cambios,
                });
                return motivo == null ? ResultadoEscritura.Guardado() : ResultadoEscritura.Error(motivo);
            }
            catch (Exception ex)
            {
                return ResultadoEscritura.Error(ex.Message);
            }
        }

        private static CDoc Cabecera(string numero, string ejercicio, string empresa)
        {
            return MercanciasDb.DelAddon<CDoc>("c_doc", new Filtro
            {
                ["NUMERO"] = numero,
                ["EJERCICIO"] = ejercicio,
                ["EMPRESA"] = empresa,
            }).FirstOrDefault();
        }

        private static DDocMercanciaExt Linea(DocMercancia documento, string numero, string ejercicio, string empresa, string articulo)
        {
            var linea = documento?.Details?.FirstOrDefault(d => d.ARTICULO.Trim() == articulo.Trim());
            if (linea != null)
                linea.ArticuloUnidades = AlmaSolicitudService.GetWarehouseDetail(numero, empresa, ejercicio, articulo);
            return linea;
        }

        /// <summary>
        /// El nombre del proveedor del albarán de compra con ese número; null si no hay albarán o proveedor. Si el número está
        /// en varios albaranes vale el primero por empresa y proveedor, que es el orden en que los lee interface.s50c.
        /// </summary>
        private static string NombreProveedor(string albaran)
        {
            var albaranCompra = MercanciasDb.DelEjercicio<AlbaranCompra>("c_albcom", new Filtro { ["NUMERO"] = albaran })
                .OrderBy(a => a.Empresa, StringComparer.Ordinal).ThenBy(a => a.Proveedor, StringComparer.Ordinal)
                .FirstOrDefault();
            if (albaranCompra == null)
                return null;

            return MercanciasDb.DelEjercicio<CodigoNombre>("proveed", new Filtro { ["CODIGO"] = albaranCompra.Proveedor }).FirstOrDefault()?.Nombre;
        }
    }
}
