using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
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
        // Sin esto, el proxy caduca tras unos minutos sin llamadas y el servicio pierde la sesión.
        public override object InitializeLifetimeService() => null;

        public TrabajadorSage()
        {
            // Sin esto, las llamadas entre AppDomains no encuentran este ensamblado (se cargó por ruta, no desde la carpeta de Sage).
            var propio = typeof(TrabajadorSage).Assembly;
            AppDomain.CurrentDomain.AssemblyResolve += (s, e) => new AssemblyName(e.Name).Name == propio.GetName().Name ? propio : null;
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
            return EnSage(() =>
            {
                string sql = "SELECT " + string.Join(", ", columnas.Select(c => "[" + c + "]")) + " FROM " + DB.SQLDatabase(baseDatos, tabla)
                    + (condicion == null ? "" : " WHERE " + condicion);
                var datos = new DataTable();
                var consultaParametros = (parametros ?? new Dictionary<string, string>())
                    .Select(p => new DB.QueryParams(p.Key, p.Value, SqlDbType.VarChar)).ToList();
                if (!DB.SQLExecParams(sql, ref datos, consultaParametros))
                    throw new InvalidOperationException($"Sage no ha podido ejecutar \"{sql}\": {DB.Error_Message}");

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
            catch (Exception ex)
            {
                var e = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                throw new InvalidOperationException(e.GetType().Name + ": " + e.Message);
            }
        }
    }
}
