using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using sage._50;
using sage.ew.articulo;
using sage.ew.db;
using sage.ew.docscompra;
using sage.ew.docventatpv;
using sage.ew.global;
using sage.ew.serie;
using sage.ew.stocks;

namespace S50APIService.Sage
{
    /// <summary>
    /// Vive dentro del AppDomain de Sage y es el único código que usa sus librerías.
    /// Solo intercambia tipos básicos (string, Dictionary...) con el resto del servicio,
    /// que así no necesita cargar ninguna librería de Sage.
    /// </summary>
    public sealed class TrabajadorSage : MarshalByRefObject
    {
        /// <summary>El proxy no caduca: el servicio lo usa mientras está en marcha.</summary>
        public override object InitializeLifetimeService() => null;

        /// <summary>Hace que el AppDomain de Sage encuentre este ensamblado y sus librerías, que están en la carpeta del servicio.</summary>
        public TrabajadorSage()
        {
            var propio = typeof(TrabajadorSage).Assembly;
            string carpeta = Path.GetDirectoryName(propio.Location);
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                string nombre = new AssemblyName(e.Name).Name;
                if (nombre == propio.GetName().Name)
                    return propio;
                bool loPideElServicio = e.RequestingAssembly != null && !e.RequestingAssembly.IsDynamic
                    && string.Equals(Path.GetDirectoryName(e.RequestingAssembly.Location), carpeta, StringComparison.OrdinalIgnoreCase);
                string ruta = Path.Combine(carpeta, nombre + ".dll");
                return loPideElServicio && File.Exists(ruta) ? Assembly.LoadFrom(ruta) : null;
            };
        }

        /// <summary>Arranque externo de Sage: sin pantalla de login, sin escritorio y sin consumir puesto de licencia.</summary>
        public void Conectar(string terminal, string grupo, string empresa)
        {
            EnSage(() =>
            {
                if (!main_s50._main_Sage_50_External_Entry(terminal, grupo, empresa))
                    throw new InvalidOperationException("Sage no ha podido conectar: " + main_s50._ErrorString);
                return 0;
            });
        }

        public Dictionary<string, string> Estado()
        {
            return EnSage(() => new Dictionary<string, string>
            {
                ["conectado"] = (!string.IsNullOrWhiteSpace(DB.Conexion)).ToString(),
                ["usuario"] = Global("wc_usuario"),
                ["grupo"] = DB.DbComunes,
                ["empresa"] = Global("wc_empresa"),
                ["ejercicio"] = Global("wc_any"),
                ["versionSage"] = typeof(main_s50).Assembly.GetName().Version.ToString(),
            });
        }

        /// <summary>
        /// Todas las filas de una tabla, con las columnas pedidas en ese orden y con su tipo de .NET
        /// (string, bool, decimal, DateTime...; null si la columna es NULL). Son tipos básicos, así que pasan sin problema
        /// al resto del servicio. <paramref name="baseDatos"/> es el nombre lógico de Sage ("COMUNES", "2025", o el de un
        /// addon como "FERRETERIATIA"): Sage lo traduce a la base de datos real del grupo de empresas conectado.
        /// Las columnas van entre corchetes porque algunas son palabras reservadas de SQL Server (p. ej. ANY en ejercici).
        /// <paramref name="condicion"/> es el WHERE (sin la palabra), con parámetros @nombre cuyos valores van en
        /// <paramref name="parametros"/> y se envían a SQL Server como varchar, nunca concatenados en la consulta.
        /// </summary>
        public List<object[]> LeerTabla(string baseDatos, string tabla, string[] columnas, string condicion = null, Dictionary<string, string> parametros = null)
        {
            return LeerConsulta(baseDatos, new Consulta { Origen = "{" + tabla + "}", Condicion = condicion, Parametros = parametros }, columnas);
        }

        /// <summary>Como <see cref="LeerTabla"/>, con las uniones y la paginación de <paramref name="consulta"/>.</summary>
        public List<object[]> LeerConsulta(string baseDatos, Consulta consulta, string[] columnas)
        {
            return EnSage(() =>
            {
                var datos = Consultar(baseDatos, consulta, columnas);
                var filas = new List<object[]>(datos.Rows.Count);
                foreach (DataRow fila in datos.Rows)
                {
                    var valores = new object[columnas.Length];
                    for (int i = 0; i < columnas.Length; i++)
                        valores[i] = fila.IsNull(i) ? null : fila[i];
                    filas.Add(valores);
                }
                return filas;
            });
        }

        /// <summary>
        /// Como <see cref="LeerConsulta"/>, pero escribe el array JSON de las filas y lo entrega en bloques a <paramref name="destino"/>.
        /// </summary>
        public void EscribirConsultaJson(string baseDatos, Consulta consulta, CampoJson[] campos, BloquesJson destino)
        {
            EnSage(() =>
            {
                var datos = Consultar(baseDatos, consulta, campos.Select(c => c.Columna).ToArray());
                EscritorJson.Escribir(datos, campos, destino);
                return 0;
            });
        }

        /// <summary>
        /// La paginación es la de EF Core sin OrderBy (ORDER BY (SELECT 1) OFFSET/FETCH), para que SQL Server devuelva
        /// las mismas filas que a interface.s50c. Si se piden 0 filas, EF no pagina: consulta con WHERE 0 = 1.
        /// </summary>
        private static DataTable Consultar(string baseDatos, Consulta consulta, string[] columnas)
        {
            bool sinFilas = consulta.Tomar == 0;
            string condicion = sinFilas ? "0 = 1" : consulta.Condicion;
            string prefijo = consulta.Alias == null ? "" : "[" + consulta.Alias + "].";
            string Columna(string c) => consulta.Expresiones != null && consulta.Expresiones.TryGetValue(c, out string expresion)
                ? expresion + " AS [" + c + "]"
                : prefijo + "[" + c + "]";
            string sql = "SELECT " + string.Join(", ", columnas.Select(Columna))
                + Regex.Replace(" FROM " + consulta.Origen + (condicion == null ? "" : " WHERE " + condicion),
                    @"\{(\w+)\}", m => DB.SQLDatabase(baseDatos, m.Groups[1].Value));
            var consultaParametros = sinFilas ? new List<DB.QueryParams>() : (consulta.Parametros ?? new Dictionary<string, string>())
                .Select(p => new DB.QueryParams(p.Key, p.Value, SqlDbType.VarChar)).ToList();
            bool paginada = consulta.Saltar != null && !sinFilas;
            if (consulta.Orden != null)
                sql += " ORDER BY " + prefijo + "[" + consulta.Orden + "]";
            else if (paginada)
                sql += " ORDER BY (SELECT 1)";
            if (paginada)
            {
                sql += " OFFSET @__saltar ROWS FETCH NEXT @__tomar ROWS ONLY";
                consultaParametros.Add(new DB.QueryParams("@__saltar", consulta.Saltar.Value.ToString(CultureInfo.InvariantCulture), SqlDbType.Int));
                consultaParametros.Add(new DB.QueryParams("@__tomar", consulta.Tomar.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), SqlDbType.Int));
            }
            var datos = new DataTable();
            if (!DB.SQLExecParams(sql, ref datos, consultaParametros))
                throw new ErrorSqlException(MensajeError(), sql);
            return datos;
        }

        /// <summary>
        /// Ejecuta una instrucción que no devuelve filas (INSERT, UPDATE...) y devuelve cuántas ha cambiado. Solo para las
        /// tablas que no tienen clase de negocio en Sage. Las tablas van entre llaves, como en <see cref="Consulta.Origen"/>,
        /// y los valores en <paramref name="parametros"/>, como en <see cref="LeerTabla"/>.
        /// </summary>
        public int Ejecutar(string baseDatos, string sql, Dictionary<string, string> parametros = null)
        {
            return EnSage(() =>
            {
                string instruccion = Regex.Replace(sql, @"\{(\w+)\}", m => DB.SQLDatabase(baseDatos, m.Groups[1].Value));
                var lista = (parametros ?? new Dictionary<string, string>())
                    .Select(p => new DB.QueryParams(p.Key, p.Value, SqlDbType.VarChar)).ToList();
                if (!DB.SQLExecParams(instruccion, out int filas, lista))
                    throw new ErrorSqlException(MensajeError(), instruccion);
                return filas;
            });
        }

        /// <summary>
        /// Cambia un documento de un addon con su clase de negocio (p. ej. Documento de GESTIONMERC, tablas c_doc y d_doc):
        /// lo carga, asigna las propiedades de <paramref name="cambios"/> y lo guarda. Devuelve null si se ha guardado y, si
        /// no, el motivo. La clase se usa sin compilar contra la librería del addon, que puede no estar.
        /// </summary>
        public string GuardarDocumento(CambiosDocumento cambios)
        {
            return EnSage(() =>
            {
                var tipo = TipoDeAddon(cambios.Libreria, cambios.Clase);
                dynamic documento = Activator.CreateInstance(tipo);
                // El número va el último: al asignarlo, Sage completa el ejercicio y la empresa que falten con los de la sesión.
                documento._Ejercicio = cambios.Ejercicio;
                documento._Empresa = cambios.Empresa;
                documento._Numero = cambios.Numero;
                if (!documento._Existe_Registro())
                    return "No se encontró el documento.";

                documento._Load();
                if (documento._EnUso)
                    return "El documento está abierto en Sage: no se ha guardado.";
                try
                {
                    if (Convert.ToString(documento._Ejercicio).Trim() != cambios.Ejercicio || Convert.ToString(documento._Empresa).Trim() != cambios.Empresa
                        || Convert.ToString(documento._Numero).Trim() != cambios.Numero)
                        return "Sage ha cargado otro documento: no se ha guardado.";

                    foreach (var cambio in cambios.Cabecera)
                        tipo.GetProperty(cambio.Key).SetValue(documento, cambio.Value);
                    if (cambios.Linea != null)
                    {
                        object linea = null;
                        foreach (dynamic candidata in documento._Detalle._Items)
                            if (candidata._Linea == cambios.Linea.Value)
                                linea = candidata;
                        if (linea == null)
                            return "No se encontró la línea del documento.";
                        foreach (var cambio in cambios.DeLinea)
                            linea.GetType().GetProperty(cambio.Key).SetValue(linea, cambio.Value);
                    }
                    if (documento._Save())
                        return null;
                    string motivo = Convert.ToString(documento._Mensaje_Error);
                    return string.IsNullOrWhiteSpace(motivo) ? "Sage no ha guardado el documento." : motivo;
                }
                finally
                {
                    documento._Bloquear_Documento(false);
                }
            });
        }

        /// <summary>
        /// Separa un reparto del addon FERRETERIATIA como su botón "separar reparto": crea el documento
        /// <paramref name="numeroNuevo"/> con la misma cabecera y le pasa de cada línea de <paramref name="lineas"/>
        /// (número de línea → unidades) esas unidades; la línea que se pasa entera se quita del original. Devuelve null si
        /// se ha hecho y, si no, el motivo.
        /// </summary>
        public string SepararReparto(string ejercicio, string empresa, string numero, string numeroNuevo, Dictionary<int, decimal> lineas)
        {
            return EnSage(() =>
            {
                const string libreria = "sage.addons.FerreteriaTia";
                var tipo = TipoDeAddon(libreria, "sage.addons.FerreteriaTia.Negocio.Documentos.DocumentoEntRep");
                var apuntarReparto = TipoDeAddon(libreria, "sage.addons.FerreteriaTia.Negocio.Clases.AddonDbExtAlbVenta").GetMethod("setJsonRepartoInDocumentByMove");

                dynamic origen = Activator.CreateInstance(tipo);
                origen._Ejercicio = ejercicio;
                origen._Empresa = empresa;
                origen._Numero = numero;
                if (!origen._Existe_Registro())
                    return "No se encontró el documento.";

                origen._Load();
                if (origen._EnUso)
                    return "El documento está abierto en Sage: no se ha separado.";
                try
                {
                    foreach (var pedida in lineas)
                    {
                        dynamic linea = origen._Detalle._GetItemByLinea(pedida.Key);
                        if (linea == null || pedida.Value <= 0 || pedida.Value > linea._Unidades)
                            return "No se encontró la línea del documento.";
                    }

                    dynamic nuevo = Activator.CreateInstance(tipo);
                    nuevo._Ejercicio = origen._Ejercicio;
                    nuevo._Empresa = origen._Empresa;
                    nuevo._Numero = numeroNuevo;
                    foreach (string propiedad in CabeceraReparto)
                        tipo.GetProperty(propiedad).SetValue(nuevo, tipo.GetProperty(propiedad).GetValue(origen));
                    if (!nuevo._Save())
                        return Motivo(nuevo);

                    foreach (var pedida in lineas)
                    {
                        dynamic linea = origen._Detalle._GetItemByLinea(pedida.Key);
                        dynamic lineaNueva = nuevo._Detalle._NewItem();
                        lineaNueva._Articulo = linea._Articulo;
                        lineaNueva._Linea = linea._Linea;
                        lineaNueva._Unidades = pedida.Value;
                        lineaNueva._Traspaso = linea._Traspaso;
                        lineaNueva._LineaAlb = linea._LineaAlb;
                        lineaNueva._Observaciones = linea._Observaciones;
                        lineaNueva._Almacen = linea._Almacen;
                        lineaNueva._Numero = nuevo._Numero;
                        lineaNueva._Ejercicio = nuevo._Ejercicio;
                        lineaNueva._Empresa = nuevo._Empresa;
                        if (pedida.Value == linea._Unidades)
                            origen._Detalle._DeleteItem(linea);
                        else
                            linea._Unidades -= pedida.Value;

                        var parametros = new List<DB.QueryParams>
                        {
                            new DB.QueryParams("@nuevo", Convert.ToString(lineaNueva._Numero), SqlDbType.VarChar),
                            new DB.QueryParams("@hoja", Convert.ToString(linea._Numero).Trim(), SqlDbType.VarChar),
                            new DB.QueryParams("@ejercicio", Convert.ToString(lineaNueva._Ejercicio).Trim(), SqlDbType.VarChar),
                            new DB.QueryParams("@empresa", Convert.ToString(lineaNueva._Empresa).Trim(), SqlDbType.VarChar),
                            new DB.QueryParams("@linea", Convert.ToString(linea._LineaAlb), SqlDbType.VarChar),
                        };
                        string instruccion = "UPDATE " + DB.SQLDatabase("FERRETERIATIA", "d_albv_adi") + " SET [hojaentrega] = @nuevo"
                            + " WHERE LTRIM(RTRIM([hojaentrega])) = @hoja AND LTRIM(RTRIM([EJERCICIO])) = @ejercicio"
                            + " AND LTRIM(RTRIM([EMPRESA])) = @empresa AND [LINEA] = @linea";
                        if (!DB.SQLExecParams(instruccion, out int _, parametros))
                            throw new ErrorSqlException(MensajeError(), instruccion);
                        apuntarReparto.Invoke(null, new object[] { linea, lineaNueva });
                    }

                    if (!nuevo._Save())
                        return Motivo(nuevo);
                    if (!origen._Save())
                        return Motivo(origen);
                    nuevo._Abandonar_Documento();
                    return null;
                }
                finally
                {
                    origen._Bloquear_Documento(false);
                }
            });
        }

        /// <summary>
        /// Registra una serie como vendida en una línea de un albarán de venta, como al teclearla en el albarán: Sage la
        /// apunta en venser, la da de baja en compras y anota la venta en su historial (hisserie). El albarán no se
        /// modifica. Devuelve null si se ha hecho y, si no, el motivo.
        /// </summary>
        public string VenderSerie(string ejercicio, string empresa, string albaran, string letra, int linea, string articulo, string serie)
        {
            return CambiarSerie(ejercicio, empresa, albaran, letra, linea, articulo, serie, vender: true);
        }

        /// <summary>
        /// Deshace <see cref="VenderSerie"/>: Sage quita la serie de venser, la vuelve a dar de alta en compras y lo anota
        /// en su historial. Devuelve null si se ha hecho y, si no, el motivo.
        /// </summary>
        public string AnularVentaSerie(string ejercicio, string empresa, string albaran, string letra, int linea, string articulo, string serie)
        {
            return CambiarSerie(ejercicio, empresa, albaran, letra, linea, articulo, serie, vender: false);
        }

        /// <summary>
        /// Carga el albarán de venta y vende o anula la serie en la línea pedida con la clase de series de los documentos
        /// de venta de Sage (SerieDocVenta), que solo trabaja en el ejercicio activo. Las clases de Sage no pueden aparecer
        /// en los parámetros de ningún método de esta clase: el resto del servicio no puede cargarlas.
        /// </summary>
        private string CambiarSerie(string ejercicio, string empresa, string albaran, string letra, int linea, string articulo, string serie, bool vender)
        {
            return EnSage(() =>
            {
                if (ejercicio != Global("wc_any"))
                    return $"Sage solo registra series en su ejercicio activo ({Global("wc_any")}).";

                var documento = new ewDocVentaTPV();
                string numero = albaran.Trim().PadLeft(10);
                if (!documento._Existe(empresa, numero, letra))
                    return "No se encontró el albarán.";

                documento._Load(empresa, numero, letra);
                if (documento._EnUso)
                    return "El albarán está abierto en Sage: no se ha cambiado la serie.";
                try
                {
                    ewDocVentaLinTPV lineaAlbaran = null;
                    foreach (var candidata in documento._Lineas)
                        if (candidata._LineaReal == linea)
                            lineaAlbaran = candidata;
                    if (lineaAlbaran == null)
                        return "No se encontró la línea del albarán.";
                    if (lineaAlbaran._Articulo.Trim() != articulo)
                        return "La línea del albarán es de otro artículo.";

                    var series = new SerieDocVenta();
                    if (vender)
                        return series._Save_NullToValue(serie, lineaAlbaran) ? null : "Sage no ha registrado la serie.";
                    return series._Delete(serie, lineaAlbaran, false) ? null : "Sage no ha anulado la venta de la serie.";
                }
                finally
                {
                    documento._Bloquear_Documento(false);
                }
            });
        }

        /// <summary>
        /// Reserva el siguiente número de albarán de traspaso con la clase de traspasos de Sage, que lo cuenta en la empresa
        /// y se salta los que ya existen. Devuelve null y el número en <paramref name="numero"/> o, si no se puede, el motivo.
        /// </summary>
        public string NuevoNumeroTraspaso(string ejercicio, string empresa, out string numero)
        {
            string reservado = null;
            string motivo = EnSage(() =>
            {
                string fuera = FueraDeLaSesion(ejercicio, empresa);
                if (fuera != null)
                    return fuera;

                var documento = new StockAlbTraspaso();
                documento._New();
                reservado = documento._Obten_Nuevo_Numero();
                return string.IsNullOrWhiteSpace(reservado) ? "Sage no ha dado un número de traspaso." : null;
            });
            numero = reservado;
            return motivo;
        }

        /// <summary>
        /// Añade una línea a un albarán de traspaso como al teclearla en Sage: guarda la línea con el nombre y el coste del
        /// artículo, mueve el stock del almacén de origen al de destino y traspasa cada serie (compras, hisserie y
        /// traspser). Las unidades de un artículo con series se ponen sin su setter, que abriría la pantalla de series.
        /// Devuelve null y el número de la línea en <paramref name="linea"/> o, si no se puede, el motivo.
        /// </summary>
        public string AnadirLineaTraspaso(string ejercicio, string empresa, string numero, string articulo, decimal unidades, string[] series, out int linea)
        {
            int nueva = 0;
            string motivo = EnSage(() =>
            {
                using (var avisos = new AvisosDeSage())
                {
                    var ficha = new Articulo(articulo, "", "");
                    if (!ficha._Existe_Registro())
                        return "No se encontró el artículo.";
                    if (ficha._UsaSeries && series.Length != unidades)
                        return "El artículo trabaja con series: hay que indicar una serie por unidad.";
                    if (!ficha._UsaSeries && series.Length > 0)
                        return "El artículo no trabaja con series.";

                    var documento = (StockAlbTraspaso)AbrirDocumentoStock(new StockAlbTraspaso(), "C_ALBATR", "traspaso", ejercicio, empresa, numero, out string cerrado);
                    if (documento == null)
                        return cerrado;
                    try
                    {
                        var lineaNueva = documento._AddLinea(-1);
                        lineaNueva._Articulo = articulo;
                        if (ficha._UsaSeries)
                            Serie._UpdateSilentLinea(lineaNueva, "_nUnidades", unidades);
                        else
                            lineaNueva._Unidades = unidades;

                        if (string.IsNullOrWhiteSpace(lineaNueva._Articulo) || lineaNueva._Unidades != unidades)
                        {
                            lineaNueva._Delete();
                            return avisos.Texto ?? "Sage no ha aceptado la línea.";
                        }
                        if (!documento._Save())
                            return avisos.Texto ?? Motivo(documento);

                        var traspasoDeSeries = new SerieDocStockTraspaso();
                        foreach (string serie in series)
                            if (!traspasoDeSeries._Save_NullToValue(serie, lineaNueva))
                                return $"Sage no ha traspasado la serie {serie}.";

                        nueva = lineaNueva._Linea;
                        return null;
                    }
                    finally
                    {
                        documento._Bloquear_Documento(false);
                    }
                }
            });
            linea = nueva;
            return motivo;
        }

        /// <summary>
        /// Borra un albarán de traspaso como el botón de borrar de Sage: quita cada línea devolviendo el stock al almacén
        /// de origen y deshaciendo el traspaso de sus series, y después la cabecera. Devuelve null si se ha borrado y, si
        /// no, el motivo.
        /// </summary>
        public string BorrarTraspaso(string ejercicio, string empresa, string numero)
        {
            return EnSage(() =>
            {
                using (var avisos = new AvisosDeSage())
                {
                    var documento = (StockAlbTraspaso)AbrirDocumentoStock(new StockAlbTraspaso(), "C_ALBATR", "traspaso", ejercicio, empresa, numero, out string cerrado);
                    if (documento == null)
                        return cerrado;
                    try
                    {
                        return documento._Delete() ? null : avisos.Texto ?? Motivo(documento);
                    }
                    finally
                    {
                        documento._Bloquear_Documento(false);
                    }
                }
            });
        }

        /// <summary>
        /// Reserva el siguiente número de albarán de regularización con la clase de regularizaciones de Sage, que lo cuenta
        /// en la empresa y se salta los que ya existen. Devuelve null y el número en <paramref name="numero"/> o, si no se
        /// puede, el motivo.
        /// </summary>
        public string NuevoNumeroRegularizacion(string ejercicio, string empresa, out string numero)
        {
            string reservado = null;
            string motivo = EnSage(() =>
            {
                string fuera = FueraDeLaSesion(ejercicio, empresa);
                if (fuera != null)
                    return fuera;

                var documento = new StockAlbRegulari();
                documento._New();
                reservado = documento._Obten_Nuevo_Numero();
                return string.IsNullOrWhiteSpace(reservado) ? "Sage no ha dado un número de regularización." : null;
            });
            numero = reservado;
            return motivo;
        }

        /// <summary>
        /// Cambia la fecha, el operario, el tipo y las observaciones de un albarán de regularización que tiene líneas, como
        /// al editarlo en Sage; la fecha solo se puede cambiar si ninguna línea tiene unidades. El almacén no se cambia
        /// aquí: Sage no pasa al almacén nuevo el stock de todas las líneas. Devuelve null si se ha guardado y, si no, el
        /// motivo.
        /// </summary>
        public string ModificarRegularizacion(string ejercicio, string empresa, string numero, DateTime fecha, string operario, string tipo, string observaciones)
        {
            return CambiarRegularizacion(ejercicio, empresa, numero, abierto =>
            {
                var documento = (StockAlbRegulari)abierto;
                if (documento._Fecha.Value.Date != fecha)
                    documento._Fecha = fecha;
                if (documento._Fecha.Value.Date != fecha)
                    return "Sage no permite cambiar la fecha de un albarán de regularización que tiene líneas con unidades.";

                documento._Operario = operario;
                documento._TipoRegularizacion = tipo;
                documento._Observaciones = observaciones;
                return documento._Save() ? null : Motivo(documento);
            });
        }

        /// <summary>
        /// Añade una línea a un albarán de regularización como al teclearla en Sage: guarda la línea con el coste del
        /// artículo según la valoración de la empresa y descuenta sus unidades del stock del almacén (las suma si son
        /// negativas). Devuelve null y el número de la línea en <paramref name="linea"/> o, si no se puede, el motivo.
        /// </summary>
        public string AnadirLineaRegularizacion(string ejercicio, string empresa, string numero, string articulo, decimal unidades, out int linea)
        {
            int nueva = 0;
            string motivo = CambiarRegularizacion(ejercicio, empresa, numero, abierto =>
            {
                var documento = (StockAlbRegulari)abierto;
                string noAdmitido = SoloEnSage(articulo, documento._Fecha, "regularizarlo");
                if (noAdmitido != null)
                    return noAdmitido;

                var lineaNueva = documento._AddLinea(-1);
                lineaNueva._Articulo = articulo;
                lineaNueva._Unidades = unidades;
                if (string.IsNullOrWhiteSpace(lineaNueva._Articulo) || lineaNueva._Unidades != unidades)
                {
                    lineaNueva._Delete();
                    return "Sage no ha aceptado la línea.";
                }
                if (!documento._Save())
                    return Motivo(documento);

                nueva = lineaNueva._Linea;
                return null;
            });
            linea = nueva;
            return motivo;
        }

        /// <summary>
        /// Cambia el artículo y las unidades de una línea de un albarán de regularización como al editarla en Sage, que
        /// ajusta el stock con la diferencia. Devuelve null si se ha guardado y, si no, el motivo.
        /// </summary>
        public string ModificarLineaRegularizacion(string ejercicio, string empresa, string numero, int linea, string articulo, decimal unidades)
        {
            return CambiarRegularizacion(ejercicio, empresa, numero, abierto =>
            {
                var documento = (StockAlbRegulari)abierto;
                var guardada = documento._DocumentLineas.FirstOrDefault(l => l._Linea == linea);
                if (guardada == null)
                    return "No se encontró la línea.";
                if (guardada._UsaSeries || !string.IsNullOrWhiteSpace(guardada._Escandal))
                    return LineaSoloEnSage;

                if (guardada._Articulo.Trim() != articulo)
                {
                    string noAdmitido = SoloEnSage(articulo, documento._Fecha, "regularizarlo");
                    if (noAdmitido != null)
                        return noAdmitido;
                    guardada._Articulo = articulo;
                }
                guardada._Unidades = unidades;
                if (guardada._Articulo.Trim() != articulo || guardada._Unidades != unidades)
                    return "Sage no ha aceptado el cambio.";

                return documento._Save() ? null : Motivo(documento);
            });
        }

        /// <summary>
        /// Borra una línea de un albarán de regularización como al borrarla en Sage, que devuelve sus unidades al stock.
        /// La cabecera se queda aunque fuera la última línea. Devuelve null si se ha borrado y, si no, el motivo.
        /// </summary>
        public string BorrarLineaRegularizacion(string ejercicio, string empresa, string numero, int linea)
        {
            return CambiarRegularizacion(ejercicio, empresa, numero, abierto =>
            {
                var documento = (StockAlbRegulari)abierto;
                var guardada = documento._DocumentLineas.FirstOrDefault(l => l._Linea == linea);
                if (guardada == null)
                    return "No se encontró la línea.";
                if (guardada._UsaSeries || !string.IsNullOrWhiteSpace(guardada._Escandal))
                    return LineaSoloEnSage;

                return guardada._Delete() ? null : Motivo(documento);
            });
        }

        /// <summary>
        /// Borra un albarán de regularización como el botón de borrar de Sage: quita cada línea devolviendo sus unidades al
        /// stock y después la cabecera. Devuelve null si se ha borrado y, si no, el motivo.
        /// </summary>
        public string BorrarRegularizacion(string ejercicio, string empresa, string numero)
        {
            return CambiarRegularizacion(ejercicio, empresa, numero, abierto =>
            {
                var documento = (StockAlbRegulari)abierto;
                return documento._Delete() ? null : Motivo(documento);
            });
        }

        /// <summary>
        /// Crea la cabecera de un albarán de compra como al darlo de alta en Sage, que le pone la forma de pago y los giros
        /// del proveedor. Sin <paramref name="numero"/>, Sage le da el siguiente del contador del proveedor. Devuelve null y
        /// el número en <paramref name="numeroNuevo"/> o, si no se puede, el motivo.
        /// </summary>
        public string NuevoAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero, string almacen, DateTime fecha, string observaciones, out string numeroNuevo)
        {
            string creado = null;
            string motivo = EnSage(() =>
            {
                string fuera = FueraDeLaSesion(ejercicio, empresa);
                if (fuera != null)
                    return fuera;

                using (var avisos = new AvisosDeSage())
                {
                    var documento = new ewDocCompraALBARAN { _MostrarMensajes = true };
                    if (!documento._New(empresa, numero, proveedor))
                        return avisos.Texto ?? Motivo(documento);

                    // Las observaciones van primero: Sage guarda la cabecera al cambiarle la fecha o el almacén.
                    documento._Cabecera._Observacio = observaciones;
                    documento._Cabecera._Fecha = fecha;
                    documento._Cabecera._Almacen = almacen;
                    creado = documento._Numero;
                    documento._DescargarDatos();
                    return null;
                }
            });
            numeroNuevo = creado;
            return motivo;
        }

        /// <summary>
        /// Cambia las observaciones de un albarán de compra que tiene líneas y, si se indica, su operario, como al editarlo
        /// en Sage. El almacén y la fecha, si se indican, tienen que ser los que ya tiene: cambiarlos movería el stock de
        /// sus líneas. Sage solo carga los albaranes que tienen líneas: si no tiene ninguna no cambia nada y lo dice en
        /// <paramref name="sinLineas"/>. Devuelve null si se ha guardado y, si no, el motivo.
        /// </summary>
        public string ModificarAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero, string almacen, DateTime? fecha, string operario,
            string observaciones, out bool sinLineas)
        {
            bool vacio = false;
            string motivo = EnSage(() =>
            {
                string fuera = FueraDeLaSesion(ejercicio, empresa);
                if (fuera != null)
                    return fuera;

                using (var avisos = new AvisosDeSage())
                {
                    var documento = (ewDocCompraALBARAN)AbrirAlbaranCompra(empresa, proveedor, numero, out vacio);
                    if (documento == null)
                        return "No se encontró el albarán de compra.";
                    if (vacio)
                        return null;
                    try
                    {
                        if (documento._Solo_Lectura || !string.IsNullOrWhiteSpace(documento._Cabecera._Factura))
                            return "El albarán de compra está facturado o es de solo lectura en Sage: no se ha cambiado.";
                        if (almacen != null && documento._Cabecera._Almacen.Trim() != almacen.Trim())
                            return "No se puede cambiar el almacén de un albarán de compra que tiene líneas.";
                        if (fecha != null && documento._Cabecera._Fecha.Date != fecha.Value.Date)
                            return "No se puede cambiar la fecha de un albarán de compra que tiene líneas.";

                        if (operario != null)
                            documento._Cabecera._Operario = operario;
                        documento._Cabecera._Observacio = observaciones;
                        return documento._Save() ? null : avisos.Texto ?? Motivo(documento);
                    }
                    finally
                    {
                        documento._DescargarDatos();
                    }
                }
            });
            sinLineas = vacio;
            return motivo;
        }

        /// <summary>
        /// Borra un albarán de compra como el botón de borrar de Sage, con sus mismas comprobaciones (facturado, traspasado,
        /// series vendidas): quita cada línea devolviendo sus unidades al stock y al pedido, y después la cabecera con sus
        /// giros y portes. Devuelve null si se ha borrado y, si no, el motivo.
        /// </summary>
        public string BorrarAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero)
        {
            return EnSage(() =>
            {
                string fuera = FueraDeLaSesion(ejercicio, empresa);
                if (fuera != null)
                    return fuera;

                using (var avisos = new AvisosDeSage())
                {
                    var documento = (ewDocCompraALBARAN)AbrirAlbaranCompra(empresa, proveedor, numero, out bool sinLineas);
                    if (documento == null)
                        return "No se encontró el albarán de compra.";

                    // La extensión del addon FERRETERIATIA pregunta en una ventana antes de borrar: se quita y su parte se hace abajo.
                    var deFerreteria = documento._Addons.FirstOrDefault(a => a.Value.GetType().FullName == ExtensionFerreteria);
                    if (deFerreteria.Key != null)
                        documento._Addons.Remove(deFerreteria.Key);

                    bool borrado = sinLineas
                        ? documento._Borrar_Capsalera()
                        : documento._Comprobaciones_Antes_Delete_Documento(false) && documento._Delete();
                    if (!borrado)
                        return avisos.Texto ?? Motivo(documento);

                    deFerreteria.Value?.GetType().Assembly.GetType(ExpedientesFerreteria, true).GetMethod("Delete")
                        .Invoke(null, new object[] { numero.Trim().PadLeft(10), empresa, proveedor, ejercicio });
                    return null;
                }
            });
        }

        /// <summary>
        /// Añade una línea a un albarán de compra como al teclearla en Sage: le pone el precio y los descuentos de la
        /// tarifa del proveedor y el IVA del artículo, suma sus unidades al stock del almacén del albarán y recalcula los
        /// totales. Sin <paramref name="definicion"/> queda el nombre del artículo. Devuelve null y el número de la línea
        /// en <paramref name="linea"/> o, si no se puede, el motivo.
        /// </summary>
        public string AnadirLineaAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero, string articulo, decimal unidades, string definicion,
            out int linea)
        {
            int nueva = 0;
            string motivo = CambiarAlbaranCompra(ejercicio, empresa, proveedor, numero, abierto =>
            {
                var documento = (ewDocCompraALBARAN)abierto;
                string noAdmitido = SoloEnSage(articulo, documento._Cabecera._Fecha, "comprarlo");
                if (noAdmitido != null)
                    return noAdmitido;

                var lineaNueva = documento._AddLinea(-1);
                lineaNueva._Articulo = articulo;
                lineaNueva._Unidades = unidades;
                if (string.IsNullOrWhiteSpace(lineaNueva._Articulo) || lineaNueva._Unidades != unidades)
                {
                    lineaNueva._Delete(false);
                    return "Sage no ha aceptado la línea.";
                }
                if (!string.IsNullOrWhiteSpace(definicion))
                    lineaNueva._Definicion = definicion;
                if (!documento._Save())
                    return Motivo(documento);

                nueva = lineaNueva._Linea;
                return null;
            });
            linea = nueva;
            return motivo;
        }

        /// <summary>
        /// Cambia las unidades de una línea de un albarán de compra y, si se indican, su precio, sus dos descuentos y su
        /// descripción, como al editarla en Sage, que ajusta el stock con la diferencia y recalcula el importe y los
        /// totales. Devuelve null si se ha guardado y, si no, el motivo.
        /// </summary>
        public string ModificarLineaAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero, int linea, decimal unidades, decimal? precio,
            decimal? dto1, decimal? dto2, string definicion)
        {
            return CambiarAlbaranCompra(ejercicio, empresa, proveedor, numero, abierto =>
            {
                var documento = (ewDocCompraALBARAN)abierto;
                var guardada = documento._Lineas.FirstOrDefault(l => l._Linea == linea);
                if (guardada == null)
                    return "No se encontró la línea.";
                if (guardada._UsaSeries || !string.IsNullOrWhiteSpace(guardada._Escandal))
                    return LineaSoloEnSage;

                // Al cambiar las unidades Sage vuelve a poner el precio de la tarifa: el que no se indica se deja como estaba.
                decimal precioAntes = guardada._Precio, dto1Antes = guardada._Dto1, dto2Antes = guardada._Dto2;
                guardada._Unidades = unidades;
                if (guardada._Unidades != unidades)
                    return "Sage no ha aceptado las unidades.";
                guardada._Precio = precio ?? precioAntes;
                guardada._Dto1 = dto1 ?? dto1Antes;
                guardada._Dto2 = dto2 ?? dto2Antes;
                if (definicion != null)
                    guardada._Definicion = definicion;

                return documento._Save() ? null : Motivo(documento);
            });
        }

        /// <summary>
        /// Borra una línea de un albarán de compra como al borrarla en Sage, que quita sus unidades del stock y recalcula
        /// los totales. La cabecera se queda aunque fuera la última línea. Devuelve null si se ha borrado y, si no, el
        /// motivo.
        /// </summary>
        public string BorrarLineaAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero, int linea)
        {
            return CambiarAlbaranCompra(ejercicio, empresa, proveedor, numero, abierto =>
            {
                var documento = (ewDocCompraALBARAN)abierto;
                var guardada = documento._Lineas.FirstOrDefault(l => l._Linea == linea);
                if (guardada == null)
                    return "No se encontró la línea.";
                if (guardada._UsaSeries || !string.IsNullOrWhiteSpace(guardada._Escandal))
                    return LineaSoloEnSage;
                if (!guardada._Delete(false))
                    return Motivo(documento);

                // Sage borra la cabecera al guardar un albarán sin líneas: si era la última, solo se guardan los totales.
                bool guardado = documento._Documento_Vacio()
                    ? (bool)typeof(ewDocCompraALBARAN).GetMethod("_Save_Capsalera", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(documento, new object[] { false })
                    : documento._Save();
                return guardado ? null : Motivo(documento);
            });
        }

        /// <summary>
        /// Abre el albarán de compra (ewDocCompraALBARAN), le hace el <paramref name="cambio"/> y lo descarga. Devuelve null
        /// si el cambio se ha hecho; si no, el aviso que Sage habría mostrado en pantalla o, si no ha habido ninguno, el
        /// motivo que da el propio cambio.
        /// </summary>
        private string CambiarAlbaranCompra(string ejercicio, string empresa, string proveedor, string numero, Func<object, string> cambio)
        {
            return EnSage(() =>
            {
                string fuera = FueraDeLaSesion(ejercicio, empresa);
                if (fuera != null)
                    return fuera;

                using (var avisos = new AvisosDeSage())
                {
                    var documento = (ewDocCompraALBARAN)AbrirAlbaranCompra(empresa, proveedor, numero, out _);
                    if (documento == null)
                        return "No se encontró el albarán de compra.";
                    try
                    {
                        if (documento._Solo_Lectura || !string.IsNullOrWhiteSpace(documento._Cabecera._Factura))
                            return "El albarán de compra está facturado o es de solo lectura en Sage: no se ha cambiado.";

                        string motivo = cambio(documento);
                        return motivo == null ? null : avisos.Texto ?? motivo;
                    }
                    finally
                    {
                        documento._DescargarDatos();
                    }
                }
            });
        }

        /// <summary>
        /// Crea la cabecera de un albarán de venta como al darlo de alta en Sage, que le da el siguiente número de la serie
        /// y le pone la forma de pago, el vendedor, los giros y los portes del cliente. El almacén, el vendedor, la forma
        /// de pago y el operario que se indican sustituyen a los que pone Sage. Devuelve null y el número en
        /// <paramref name="numero"/> o, si no se puede, el motivo.
        /// </summary>
        public string NuevoAlbaranVenta(string ejercicio, string empresa, string letra, string cliente, string almacen, string vendedor, string fpag, string operario,
            DateTime fecha, string observaciones, out string numero)
        {
            string creado = null;
            string motivo = EnSage(() =>
            {
                string fuera = FueraDeLaSesion(ejercicio, empresa);
                if (fuera != null)
                    return fuera;

                using (var avisos = new AvisosDeSage())
                {
                    var documento = new ewDocVentaTPV { _MostrarMensajes = true };
                    if (!documento._New(empresa, letra, ""))
                        return avisos.Texto ?? Motivo(documento);
                    try
                    {
                        documento._Cabecera._Cliente = cliente;
                        if (documento._Cabecera._Cliente.Trim() != cliente.Trim())
                        {
                            documento._Delete();
                            return avisos.Texto ?? "Sage no ha aceptado el cliente.";
                        }

                        if (!string.IsNullOrWhiteSpace(almacen))
                            documento._Cabecera._Almacen = almacen;
                        if (!string.IsNullOrWhiteSpace(vendedor))
                            documento._Cabecera._Vendedor = vendedor;
                        if (!string.IsNullOrWhiteSpace(fpag))
                            documento._Cabecera._FormaPago = fpag;
                        if (!string.IsNullOrWhiteSpace(operario))
                            documento._Cabecera._Operario = operario;
                        documento._Cabecera._Fecha = fecha;
                        documento._Cabecera._Observacio = observaciones;
                        GuardarCabeceraVenta(documento);
                        creado = documento._Numero;
                        return null;
                    }
                    finally
                    {
                        documento._DescargarDatos();
                    }
                }
            });
            numero = creado;
            return motivo;
        }

        /// <summary>
        /// Cambia las observaciones de un albarán de venta y, si se indican, su almacén, su fecha y su operario, como al
        /// editarlo en Sage. Si el albarán tiene líneas, el almacén y la fecha tienen que ser los que ya tiene: cambiarlos
        /// movería el stock de sus líneas. Devuelve null si se ha guardado y, si no, el motivo.
        /// </summary>
        public string ModificarAlbaranVenta(string ejercicio, string empresa, string numero, string letra, string almacen, DateTime? fecha, string operario,
            string observaciones)
        {
            return CambiarAlbaranVenta(ejercicio, empresa, numero, letra, abierto =>
            {
                var documento = (ewDocVentaTPV)abierto;
                bool conLineas = !documento._Documento_Vacio();
                if (almacen != null && documento._Cabecera._Almacen.Trim() != almacen.Trim())
                {
                    if (conLineas)
                        return "No se puede cambiar el almacén de un albarán de venta que tiene líneas.";
                    documento._Cabecera._Almacen = almacen;
                }
                if (fecha != null && documento._Cabecera._Fecha.Date != fecha.Value.Date)
                {
                    if (conLineas)
                        return "No se puede cambiar la fecha de un albarán de venta que tiene líneas.";
                    documento._Cabecera._Fecha = fecha.Value;
                }
                if (operario != null)
                    documento._Cabecera._Operario = operario;
                documento._Cabecera._Observacio = observaciones;

                // Sage borra la cabecera al guardar un albarán sin líneas: de ese solo se guarda la cabecera.
                if (conLineas)
                    return documento._Save() ? null : Motivo(documento);
                GuardarCabeceraVenta(documento);
                return null;
            });
        }

        /// <summary>
        /// Borra un albarán de venta como el botón de borrar de Sage, con sus mismas comprobaciones (facturado, cobrado, en
        /// un arqueo de caja): quita cada línea devolviendo sus unidades al stock y al pedido, y después la cabecera con
        /// sus giros y portes. Devuelve null si se ha borrado y, si no, el motivo.
        /// </summary>
        public string BorrarAlbaranVenta(string ejercicio, string empresa, string numero, string letra)
        {
            return CambiarAlbaranVenta(ejercicio, empresa, numero, letra, abierto =>
            {
                var documento = (ewDocVentaTPV)abierto;
                return documento._Comprobaciones_Antes_Delete_Documento(false) && documento._Delete() ? null : Motivo(documento);
            });
        }

        /// <summary>
        /// Abre el albarán de venta (ewDocVentaTPV), le hace el <paramref name="cambio"/> y lo descarga. Devuelve null si el
        /// cambio se ha hecho; si no, el aviso que Sage habría mostrado en pantalla o, si no ha habido ninguno, el motivo
        /// que da el propio cambio.
        /// </summary>
        private string CambiarAlbaranVenta(string ejercicio, string empresa, string numero, string letra, Func<object, string> cambio)
        {
            return EnSage(() =>
            {
                string fuera = FueraDeLaSesion(ejercicio, empresa);
                if (fuera != null)
                    return fuera;

                using (var avisos = new AvisosDeSage())
                {
                    var documento = new ewDocVentaTPV { _MostrarMensajes = true };
                    if (!documento._Load(empresa, numero.Trim().PadLeft(10), letra))
                        return avisos.Texto ?? "No se encontró el albarán de venta.";
                    try
                    {
                        if (documento._EnUso)
                            return "El albarán de venta está abierto en Sage: no se ha cambiado.";
                        if (documento._Solo_Lectura || !string.IsNullOrWhiteSpace(documento._Cabecera._Factura))
                            return "El albarán de venta está facturado o es de solo lectura en Sage: no se ha cambiado.";

                        string motivo = cambio(documento);
                        return motivo == null ? null : avisos.Texto ?? motivo;
                    }
                    finally
                    {
                        documento._DescargarDatos();
                    }
                }
            });
        }

        /// <summary>Guarda solo la cabecera del albarán de venta. El parámetro es object porque las clases de Sage no pueden aparecer en la firma de ningún método de esta clase.</summary>
        private static void GuardarCabeceraVenta(object documento)
        {
            typeof(ewDocVentaTPV).GetMethod("_Save_Capsalera", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(documento, null);
        }

        /// <summary>La extensión que el addon FERRETERIATIA engancha a los albaranes de compra, y su clase de expedientes (tabla con_albacom).</summary>
        private const string ExtensionFerreteria = "sage.addons.FerreteriaTia.Negocio.Documentos.ExtAlbCompra";
        private const string ExpedientesFerreteria = "sage.addons.FerreteriaTia.Negocio.Clases.AddonDbCon_Albacom";

        /// <summary>
        /// El albarán de compra cargado en la clase de Sage (ewDocCompraALBARAN), o null si no existe. Sage solo carga los
        /// albaranes que tienen líneas: a uno que aún no tiene ninguna se le da la cabecera leída de su tabla, y se avisa
        /// en <paramref name="sinLineas"/>.
        /// Devuelve object porque las clases de Sage no pueden aparecer en la firma de ningún método de esta clase.
        /// </summary>
        private static object AbrirAlbaranCompra(string empresa, string proveedor, string numero, out bool sinLineas)
        {
            numero = numero.Trim().PadLeft(10);
            var documento = new ewDocCompraALBARAN { _MostrarMensajes = true };
            sinLineas = !documento._Load(empresa, numero, proveedor);
            if (!sinLineas)
                return documento;

            var cabecera = new DataTable();
            string sql = "SELECT c.*, c.IMPORTE AS importecab, c.ASI AS asicab, c.GUID_ID AS guid_idcab, c.CREATED AS createdcab, c.MODIFIED AS modifiedcab FROM "
                + DB.SQLDatabase("GESTION", "C_ALBCOM") + " c WHERE c.EMPRESA = " + DB.SQLString(empresa) + " AND c.PROVEEDOR = " + DB.SQLString(proveedor)
                + " AND c.NUMERO = " + DB.SQLString(numero);
            if (!DB.SQLExec(sql, ref cabecera))
                throw new ErrorSqlException(MensajeError(), sql);
            if (cabecera.Rows.Count == 0)
                return null;

            documento._Empresa = empresa;
            documento._Numero = numero;
            documento._Cabecera._Load(cabecera.Rows[0]);
            typeof(ewDocCompraALBARAN).GetMethod("_Condicion_Consulta", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(documento, null);
            documento._Pie._Load(cabecera.Rows[0]);
            documento._Otros_Procesos_Load_Clases_Derivadas(empresa, numero, proveedor);
            return documento;
        }

        private const string LineaSoloEnSage = "La línea es de un artículo con series o de un escandallo: hay que cambiarla en Sage.";

        /// <summary>
        /// Abre el albarán de regularización (StockAlbRegulari), le hace el <paramref name="cambio"/> y lo desbloquea.
        /// Devuelve null si el cambio se ha hecho; si no, el aviso que Sage habría mostrado en pantalla o, si no ha habido
        /// ninguno, el motivo que da el propio cambio.
        /// </summary>
        private string CambiarRegularizacion(string ejercicio, string empresa, string numero, Func<object, string> cambio)
        {
            return EnSage(() =>
            {
                using (var avisos = new AvisosDeSage())
                {
                    var documento = (StockAlbRegulari)AbrirDocumentoStock(new StockAlbRegulari(), "C_ALBARE", "albarán de regularización",
                        ejercicio, empresa, numero, out string cerrado);
                    if (documento == null)
                        return cerrado;
                    try
                    {
                        string motivo = cambio(documento);
                        return motivo == null ? null : avisos.Texto ?? motivo;
                    }
                    finally
                    {
                        documento._Bloquear_Documento(false);
                    }
                }
            });
        }

        /// <summary>
        /// Por qué un artículo no se puede poner desde aquí en una línea de un documento, o null si se puede. Con series,
        /// tallas y colores, lotes o escandallo, Sage abre una pantalla para pedir el detalle, y aquí no hay quien la
        /// conteste. <paramref name="accion"/> es lo que hay que hacer en Sage con el artículo: "regularizarlo", "comprarlo".
        /// </summary>
        private static string SoloEnSage(string articulo, DateTime? fecha, string accion)
        {
            var ficha = new Articulo(articulo, "", "");
            if (!ficha._Existe_Registro())
                return "No se encontró el artículo.";
            if (ficha._UsaSeries)
                return $"El artículo trabaja con series: hay que {accion} en Sage.";
            if (Convert.ToBoolean(ficha._Campo("lcolor")))
                return $"El artículo trabaja con tallas y colores: hay que {accion} en Sage.";
            if (ficha._Con_Lotes(fecha))
                return $"El artículo trabaja con lotes: hay que {accion} en Sage.";
            if (Convert.ToInt32(ficha._Campo("tipo_escan")) != 0)
                return $"El artículo es un escandallo: hay que {accion} en Sage.";
            return null;
        }

        /// <summary>
        /// Un documento de stock de Sage (traspaso o regularización) cargado y bloqueado para cambiarlo, o null y el motivo.
        /// <paramref name="nuevo"/> es el documento recién creado de su clase, <paramref name="tabla"/> la de sus cabeceras
        /// y <paramref name="nombre"/> cómo llamarlo en los mensajes. Sage solo carga la cabecera de los documentos que
        /// tienen líneas; la de uno que aún no tiene ninguna se le da leída de su tabla, con la fecha sin hora, que es como
        /// Sage la guarda y como apunta el stock.
        /// Devuelve object porque las clases de Sage no pueden aparecer en la firma de ningún método de esta clase.
        /// </summary>
        private static object AbrirDocumentoStock(object nuevo, string tabla, string nombre, string ejercicio, string empresa, string numero, out string motivo)
        {
            motivo = FueraDeLaSesion(ejercicio, empresa);
            if (motivo != null)
                return null;

            var cabecera = new DataTable();
            string sql = "SELECT * FROM " + DB.SQLDatabase("GESTION", tabla) + " WHERE EMPRESA = " + DB.SQLString(empresa)
                + " AND NUMERO = " + DB.SQLString(numero.Trim().PadLeft(10));
            if (!DB.SQLExec(sql, ref cabecera))
                throw new ErrorSqlException(MensajeError(), sql);
            if (cabecera.Rows.Count == 0)
            {
                motivo = $"No se encontró el {nombre}.";
                return null;
            }

            dynamic documento = nuevo;
            documento._New();
            documento._Numero = numero;
            documento._Load();
            if (documento._EnUso)
            {
                motivo = $"El {nombre} está abierto en Sage: no se ha cambiado.";
                return null;
            }
            if (documento._DocumentLineas.Count == 0)
            {
                cabecera.Rows[0]["FECHA"] = Convert.ToDateTime(cabecera.Rows[0]["FECHA"]).Date;
                documento._Load_Cabecera(cabecera.Rows[0]);
            }
            return documento;
        }

        /// <summary>Por qué Sage no puede escribir un documento de ese ejercicio y empresa, o null si son los de su sesión.</summary>
        private static string FueraDeLaSesion(string ejercicio, string empresa)
        {
            if (ejercicio != Global("wc_any"))
                return $"Sage solo guarda documentos en su ejercicio activo ({Global("wc_any")}).";
            if (empresa != Global("wc_empresa"))
                return $"Sage está conectado a la empresa {Global("wc_empresa")}.";
            return null;
        }

        /// <summary>
        /// Mientras existe, los avisos que Sage mostraría en una ventana (no hay stock, el artículo no existe...) no se
        /// muestran: aquí no hay pantalla ni quien los cierre, y dejarían la sesión colgada. Sage los escribe entonces en
        /// la consola, de donde se recoge el último en <see cref="Texto"/>.
        /// </summary>
        private sealed class AvisosDeSage : IDisposable
        {
            private readonly TextWriter _consola = Console.Out;
            private readonly object _antes = EW_GLOBAL._GetVariable("wl_nomessagebox", false);
            private readonly StringWriter _avisos = new StringWriter();

            public AvisosDeSage()
            {
                EW_GLOBAL.ValorEnClave_VarGlob("wl_nomessagebox", true);
                Console.SetOut(_avisos);
            }

            /// <summary>El último aviso, sin el título de su ventana, o null si no ha habido ninguno.</summary>
            public string Texto
            {
                get
                {
                    string todos = _avisos.ToString();
                    int ultimo = todos.LastIndexOf("Mensaje: ", StringComparison.Ordinal);
                    if (ultimo < 0)
                        return null;

                    string aviso = todos.Substring(ultimo);
                    return Regex.Replace(aviso.Substring(aviso.IndexOf(" - ", StringComparison.Ordinal) + 3), @"\s+", " ").Trim();
                }
            }

            public void Dispose()
            {
                Console.SetOut(_consola);
                EW_GLOBAL.ValorEnClave_VarGlob("wl_nomessagebox", _antes);
            }
        }

        /// <summary>Lo que el botón "separar reparto" copia de la cabecera, más _EmailEnviado para que la parte nueva de un reparto ya avisado no avise otra vez al cliente.</summary>
        private static readonly string[] CabeceraReparto =
        {
            "_Fecha", "_Camion", "_Cliente", "_Albaran", "_Entregado", "_Observaciones", "_Tipo", "_Almacen", "_DireccionCliente",
            "_CpCliente", "_Poblacion", "_Provincia", "_Telefono", "_TfContacto", "_Contacto", "_FechaEntrega", "_Conductor",
            "_FechaEntregaPrev", "_Firma", "_LetraAlb", "_Pagado", "_CobrarDestino", "_ImporteCobrar", "_TramoHorario", "_ObsInt",
            "_Ruta", "_Zona", "_Incidencia", "_EmailEnviado",
        };

        private static string Motivo(dynamic documento)
        {
            string motivo = Convert.ToString(documento._Mensaje_Error);
            return string.IsNullOrWhiteSpace(motivo) ? "Sage no ha guardado el documento." : motivo;
        }

        /// <summary>La clase de un addon de Sage: de la librería que ya tenga cargada Sage o, si no, de su carpeta de librerías.</summary>
        private static Type TipoDeAddon(string libreria, string clase)
        {
            var ensamblado = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => string.Equals(a.GetName().Name, libreria, StringComparison.OrdinalIgnoreCase)) ?? Assembly.Load(libreria);
            return ensamblado.GetType(clase, true);
        }

        /// <summary>
        /// El mensaje de SQL Server del último error. Sage lo guarda entre comillas (y con las suyas dobladas) en Error_Message;
        /// tal cual está en la excepción, que solo se usa si es la de ese mismo error.
        /// </summary>
        private static string MensajeError()
        {
            string mensaje = DB.Error_Message ?? "";
            string original = DB.Error_Message_Exception?.Message;
            return !string.IsNullOrEmpty(original) && mensaje.Contains(original.Replace("'", "''")) ? original : mensaje;
        }

        /// <summary>
        /// True si Sage conoce <paramref name="baseDatos"/> (p. ej. el ejercicio "2025") y esa base de datos existe en el
        /// servidor. Sage puede tener el alias de un ejercicio cuya base de datos ya no existe.
        /// </summary>
        public bool ExisteBaseDatos(string baseDatos)
        {
            return EnSage(() => DB.SQLDatabaseExistStrict(baseDatos));
        }

        private static string Global(string variable) => Convert.ToString(EW_GLOBAL._GetVariable(variable)).Trim();

        /// <summary>
        /// Las excepciones de Sage no pueden viajar al resto del servicio (no tiene cargadas sus librerías):
        /// se convierten en una excepción estándar con el mismo mensaje.
        /// </summary>
        private static T EnSage<T>(Func<T> accion)
        {
            try { return accion(); }
            catch (ErrorSqlException) { throw; }
            catch (Exception ex)
            {
                var e = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;                throw new InvalidOperationException(e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
