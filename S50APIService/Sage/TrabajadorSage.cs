using System;
using System.Collections.Generic;
using System.Data;
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
        /// </summary>
        public List<object[]> LeerTabla(string baseDatos, string tabla, string[] columnas)
        {
            return EnSage(() =>
            {
                string sql = "SELECT " + string.Join(", ", columnas) + " FROM " + DB.SQLDatabase(baseDatos, tabla);
                var datos = new DataTable();
                if (!DB.SQLExec(sql, ref datos))
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
