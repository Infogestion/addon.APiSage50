using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;
using S50APIService.Api.Autenticacion;
using S50APIService.Api.Modelos;
using S50APIService.Sage;

namespace S50APIService.Api.Controladores
{
    /// <summary>
    /// Documentos de mercancías de la app de reposición: tablas c_doc, d_doc, alma_solicitud y alma_envio del addon
    /// GESTIONMERC. Los albaranes, proveedores y artículos se leen del ejercicio del año en curso, porque la ruta no lleva {year}.
    /// </summary>
    [RoutePrefix("api/nadilux-mercancias/DocumentosMercancias")]
    [Autorizar]
    public sealed class DocumentosMercanciasController : ApiController
    {
        private const string Addon = "GESTIONMERC";

        private static string Ejercicio => DateTime.Now.Year.ToString();

        /// <summary>
        /// Permite obtener todos los documentos de gestión de mercancías
        /// </summary>
        [HttpGet]
        [Route("")]
        [ResponseType(typeof(List<DocMercancia>))]
        public HttpResponseMessage Get(string operario = null, DateTime? fecha = null, string proveedor = null, string albaran = null)
        {
            var condiciones = new List<string>();
            var parametros = new Dictionary<string, string>();
            if (operario != null)
            {
                condiciones.Add("(LTRIM(RTRIM([OPERARIO])) = @operario) AND (LTRIM(RTRIM([STATE])) <> 'CERRADO')");
                parametros["@operario"] = operario.Trim();
            }
            if (fecha != null)
            {
                condiciones.Add("CONVERT(date, [FECHA]) = @fecha");
                parametros["@fecha"] = fecha.Value.ToString("yyyyMMdd");
            }
            if (albaran != null)
            {
                condiciones.Add("LTRIM(RTRIM([ALB_NUMERO])) = @albaran");
                parametros["@albaran"] = albaran.Trim();
            }
            var documentos = Contexto.Lector.LeerEjercicio<CDoc>(Addon, "c_doc",
                condiciones.Count == 0 ? null : string.Join(" AND ", condiciones.Select(c => "(" + c + ")")), parametros);

            var numeros = new HashSet<string>(documentos.Select(d => d.alb_numero.Trim()));
            var codigos = new HashSet<string>(documentos.Select(d => d.proveedor.Trim()));
            var albaranes = Contexto.Lector.LeerEjercicio<AlbaranCompra>(Ejercicio, "c_albcom")
                .Where(a => numeros.Contains(a.Numero.Trim())).ToDictionary(a => a.Numero.Trim());
            var proveedores = Contexto.Lector.LeerEjercicio<CodigoNombre>(Ejercicio, "proveed")
                .Where(p => codigos.Contains(p.Codigo.Trim())).ToDictionary(p => p.Codigo.Trim(), p => p.Nombre);

            // interface.s50c descarta, sin avisar, los documentos cuyo albarán no está en el ejercicio y, si se filtra por proveedor, los que no tienen nombre de proveedor.
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
            return Respuestas.Json(resultado);
        }

        [HttpGet]
        [Route("by-number")]
        [ResponseType(typeof(DocMercancia))]
        public HttpResponseMessage Get([Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null)
        {
            return JsonONada(Documento(numero, ejercicio, empresa));
        }

        [HttpGet]
        [Route("detail-by-barcode")]
        [ResponseType(typeof(DDocMercanciaExt))]
        public HttpResponseMessage GetDetailByBarcode([Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null, [Obligatorio] string barcode = null)
        {
            var documento = Documento(numero, ejercicio, empresa);
            if (documento?.Details == null || documento.Details.Count == 0)
                return JsonONada(null);

            var articulos = Contexto.Lector.LeerEjercicio<Articulo>(Ejercicio, new Consulta
            {
                Origen = "{articulo} AS [a] INNER JOIN {barras} AS [b] ON [a].[CODIGO] = [b].[ARTICULO]",
                Alias = "a",
                Condicion = "[b].[BARRAS] = @codigo",
                Parametros = new Dictionary<string, string> { ["@codigo"] = barcode.Trim() },
            });

            DDocMercanciaExt linea = null;
            foreach (var articulo in articulos)
            {
                var encontrada = documento.Details.FirstOrDefault(d => d.ARTICULO.Trim() == articulo.Codigo.Trim());
                if (encontrada == null)
                    continue;
                linea = encontrada;
                linea.ArticuloUnidades = Unidades("alma_solicitud", numero, empresa, ejercicio, articulo.Codigo);
            }
            return JsonONada(linea);
        }

        [HttpGet]
        [Route("detail")]
        [ResponseType(typeof(DDocMercanciaExt))]
        public HttpResponseMessage GetDetail([Obligatorio] string numero = null, [Obligatorio] string ejercicio = null, [Obligatorio] string empresa = null, [Obligatorio] string code = null)
        {
            var linea = Documento(numero, ejercicio, empresa)?.Details?.FirstOrDefault(d => d.ARTICULO.Trim() == code.Trim());
            if (linea != null)
                linea.ArticuloUnidades = Unidades("alma_solicitud", numero, empresa, ejercicio, code);
            return JsonONada(linea);
        }

        [HttpGet]
        [Route("detail-sends")]
        [ResponseType(typeof(string))]
        public HttpResponseMessage GetDetailSends([Obligatorio] string numero = null, [Obligatorio] string empresa = null, [Obligatorio] string ejercicio = null, [Obligatorio] string code = null)
        {
            return Respuestas.Texto(Unidades("alma_envio", numero, empresa, ejercicio, code));
        }

        [HttpGet]
        [Route("details-search")]
        [ResponseType(typeof(List<Articulo>))]
        public HttpResponseMessage GetDetailsSearch([Obligatorio] string numero = null, [Obligatorio] string empresa = null, [Obligatorio] string ejercicio = null, [Obligatorio] string query = null)
        {
            var codigos = Contexto.Lector.LeerEjercicio<AlmaUnidades>(Addon, "alma_solicitud",
                    "((LTRIM(RTRIM([NUMERO])) = @numero) AND (LTRIM(RTRIM([EMPRESA])) = @empresa)) AND (LTRIM(RTRIM([EJERCICIO])) = @ejercicio)",
                    new Dictionary<string, string> { ["@numero"] = numero.Trim(), ["@empresa"] = empresa.Trim(), ["@ejercicio"] = ejercicio.Trim() })
                .Select(s => s.Articulo.Trim()).Distinct().ToList();
            if (codigos.Count == 0)
                return Respuestas.Json(new List<Articulo>());

            // Los códigos van como literales, igual que los escribe EF: con parámetros SQL Server elige otro plan y los artículos salen en otro orden.
            var parametros = new Dictionary<string, string> { ["@patron"] = "%" + query.Trim().ToLower() + "%" };
            var literales = codigos.Select(c => "'" + c.Replace("'", "''") + "'").ToList();
            string lista = literales.Count == 1 ? " = " + literales[0] : " IN (" + string.Join(", ", literales) + ")";
            string EnDocumento(string alias) => "LTRIM(RTRIM([" + alias + "].[CODIGO]))" + lista;

            try
            {
                var porNombre = Contexto.Lector.LeerEjercicioJson<Articulo>(Ejercicio, new Consulta
                {
                    Origen = "{articulo} AS [a]",
                    Alias = "a",
                    Condicion = "(" + EnDocumento("a") + ") AND (LOWER([a].[NOMBRE]) LIKE @patron)",
                    Parametros = parametros,
                });
                if (porNombre.Sum(b => b.Length) > 2)
                    return Respuestas.JsonBloques(porNombre);

                // Es la consulta que genera EF para el GroupBy(Codigo).Select(g => g.First()) de interface.s50c: un artículo por código.
                return Respuestas.JsonBloques(Contexto.Lector.LeerEjercicioJson<Articulo>(Ejercicio, new Consulta
                {
                    Origen = "(SELECT [a].[CODIGO] FROM {referpro} AS [r]"
                        + " INNER JOIN {articulo} AS [a] ON LTRIM(RTRIM([r].[ARTICULO])) = LTRIM(RTRIM([a].[CODIGO]))"
                        + " WHERE (" + EnDocumento("a") + ") AND (LOWER([r].[PROVEEDOR]) LIKE @patron)"
                        + " GROUP BY [a].[CODIGO]) AS [t]"
                        + " LEFT JOIN (SELECT [t1].* FROM (SELECT [a0].*, ROW_NUMBER() OVER(PARTITION BY [a0].[CODIGO]"
                        + " ORDER BY [r0].[PROVEEDOR], [r0].[ARTICULO], [r0].[TALLA], [r0].[COLOR], [r0].[MONEDA], [a0].[CODIGO]) AS [row]"
                        + " FROM {referpro} AS [r0]"
                        + " INNER JOIN {articulo} AS [a0] ON LTRIM(RTRIM([r0].[ARTICULO])) = LTRIM(RTRIM([a0].[CODIGO]))"
                        + " WHERE (" + EnDocumento("a0") + ") AND (LOWER([r0].[PROVEEDOR]) LIKE @patron)) AS [t1]"
                        + " WHERE [t1].[row] <= 1) AS [t0] ON [t].[CODIGO] = [t0].[CODIGO]",
                    Alias = "t0",
                    Parametros = parametros,
                }));
            }
            catch (EjercicioNoEncontradoException)
            {
                // interface.s50c no convierte aquí el ejercicio inexistente en 404: responde 500 sin cuerpo.
                return Request.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        /// <summary>
        /// El documento con sus líneas y el nombre de su proveedor; null si no existe. Como en interface.s50c, las líneas son
        /// las de todos los documentos con ese número (de cualquier ejercicio y empresa), y si falla algo al leerlas el
        /// resultado es null, igual que si el documento no existiera.
        /// </summary>
        private static DocMercancia Documento(string numero, string ejercicio, string empresa)
        {
            numero = numero.Trim();
            var cabecera = Contexto.Lector.LeerEjercicio<CDoc>(Addon, "c_doc",
                "((LTRIM(RTRIM([NUMERO])) = @numero) AND (LTRIM(RTRIM([EJERCICIO])) = @ejercicio)) AND (LTRIM(RTRIM([EMPRESA])) = @empresa)",
                new Dictionary<string, string> { ["@numero"] = numero, ["@ejercicio"] = ejercicio.Trim(), ["@empresa"] = empresa.Trim() }).FirstOrDefault();
            if (cabecera == null)
                return null;

            try
            {
                var lineas = Contexto.Lector.LeerEjercicio<DDoc>(Addon, "d_doc", "LTRIM(RTRIM([NUMERO])) = @numero",
                    new Dictionary<string, string> { ["@numero"] = numero }).Where(l => l.NUMERO.Trim() == numero).ToList();

                var articulos = new List<CodigoNombre>();
                var codigos = lineas.Select(l => l.ARTICULO.Trim()).Distinct().ToList();
                if (codigos.Count > 0)
                {
                    var parametros = new Dictionary<string, string>();
                    for (int i = 0; i < codigos.Count; i++)
                        parametros["@c" + i] = codigos[i];
                    articulos = Contexto.Lector.LeerEjercicio<CodigoNombre>(Ejercicio, "articulo",
                        "LTRIM(RTRIM([CODIGO])) IN (" + string.Join(", ", parametros.Keys) + ")", parametros);
                }

                var detalles = lineas.Select(l => new DDocMercanciaExt
                {
                    EJERCICIO = l.EJERCICIO,
                    EMPRESA = l.EMPRESA,
                    NUMERO = l.NUMERO,
                    LINEA = l.LINEA,
                    ARTICULO = l.ARTICULO,
                    REC = l.REC,
                    PTE = l.PTE,
                    GUID_ID = l.GUID_ID,
                    CREATED = l.CREATED,
                    MODIFIED = l.MODIFIED,
                    ArticuloNombre = articulos.SingleOrDefault(a => a.Codigo.Trim() == l.ARTICULO.Trim())?.Nombre,
                }).ToList();

                return new DocMercancia { CDocMercancia = cabecera, Proveedor = NombreProveedor(cabecera.alb_numero.Trim()), Details = detalles };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} ERROR al leer el documento de mercancías {numero}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// El nombre del proveedor del albarán de compra con ese número; null si no hay albarán o proveedor. Si el número está
        /// en varios albaranes vale el primero por empresa y proveedor, que es el orden en que los lee interface.s50c.
        /// </summary>
        private static string NombreProveedor(string albaran)
        {
            var albaranCompra = Contexto.Lector.LeerEjercicio<AlbaranCompra>(Ejercicio, "c_albcom", "LTRIM(RTRIM([NUMERO])) = @numero",
                    new Dictionary<string, string> { ["@numero"] = albaran })
                .Where(a => a.Numero.Trim() == albaran)
                .OrderBy(a => a.Empresa, StringComparer.Ordinal).ThenBy(a => a.Proveedor, StringComparer.Ordinal)
                .FirstOrDefault();
            if (albaranCompra == null)
                return null;

            string codigo = albaranCompra.Proveedor.Trim();
            return Contexto.Lector.LeerEjercicio<CodigoNombre>(Ejercicio, "proveed", "LTRIM(RTRIM([CODIGO])) = @codigo",
                    new Dictionary<string, string> { ["@codigo"] = codigo })
                .FirstOrDefault(p => p.Codigo.Trim() == codigo)?.Nombre;
        }

        /// <summary>El texto JSON con las unidades por almacén de un artículo del documento, o "[]" si no tiene.</summary>
        private static string Unidades(string tabla, string numero, string empresa, string ejercicio, string articulo)
        {
            var fila = Contexto.Lector.LeerEjercicio<AlmaUnidades>(Addon, tabla,
                "(((LTRIM(RTRIM([NUMERO])) = @numero) AND (LTRIM(RTRIM([ARTICULO])) = @articulo)) AND (LTRIM(RTRIM([EMPRESA])) = @empresa)) AND (LTRIM(RTRIM([EJERCICIO])) = @ejercicio)",
                new Dictionary<string, string>
                {
                    ["@numero"] = numero.Trim(),
                    ["@articulo"] = articulo.Trim(),
                    ["@empresa"] = empresa.Trim(),
                    ["@ejercicio"] = ejercicio.Trim(),
                }).FirstOrDefault();
            return fila == null ? "[]" : fila.Unidades;
        }

        /// <summary>Ok(valor) de ASP.NET Core: 204 sin cuerpo si el valor es null.</summary>
        private HttpResponseMessage JsonONada(object valor)
        {
            return valor == null ? Request.CreateResponse(HttpStatusCode.NoContent) : Respuestas.Json(valor);
        }
    }
}
