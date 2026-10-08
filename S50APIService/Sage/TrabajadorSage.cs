using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using sage._50;
using sage.ew.db;
using sage.ew.docventatpv;
using sage.ew.global;
using sage.ew.serie;

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
                var e = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                throw new InvalidOperationException(e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
