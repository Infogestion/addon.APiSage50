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
using sage.ew.global;

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
                + " FROM " + Regex.Replace(consulta.Origen, @"\{(\w+)\}", m => DB.SQLDatabase(baseDatos, m.Groups[1].Value))
                + (condicion == null ? "" : " WHERE " + condicion);
            var consultaParametros = sinFilas ? new List<DB.QueryParams>() : (consulta.Parametros ?? new Dictionary<string, string>())
                .Select(p => new DB.QueryParams(p.Key, p.Value, SqlDbType.VarChar)).ToList();
            if (consulta.Saltar != null && !sinFilas)
            {
                sql += " ORDER BY (SELECT 1) OFFSET @__saltar ROWS FETCH NEXT @__tomar ROWS ONLY";
                consultaParametros.Add(new DB.QueryParams("@__saltar", consulta.Saltar.Value.ToString(CultureInfo.InvariantCulture), SqlDbType.Int));
                consultaParametros.Add(new DB.QueryParams("@__tomar", consulta.Tomar.GetValueOrDefault().ToString(CultureInfo.InvariantCulture), SqlDbType.Int));
            }
            var datos = new DataTable();
            if (!DB.SQLExecParams(sql, ref datos, consultaParametros))
                throw new ErrorSqlException(MensajeError(), sql);
            return datos;
        }

        /// <summary>
        /// El mensaje de SQL Server del último error. Sage lo guarda entre comillas en Error_Message; sin ellas está en
        /// la excepción, que solo se usa si es la de ese mismo error.
        /// </summary>
        private static string MensajeError()
        {
            string mensaje = DB.Error_Message ?? "";
            string original = DB.Error_Message_Exception?.Message;
            return !string.IsNullOrEmpty(original) && mensaje.Contains(original) ? original : mensaje;
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
