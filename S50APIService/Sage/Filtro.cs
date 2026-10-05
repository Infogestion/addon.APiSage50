using System;
using System.Collections.Generic;
using System.Linq;

namespace S50APIService.Sage
{
    /// <summary>
    /// Las condiciones de una lectura (el Where de EF en interface.s50c), sin escribir SQL: genera el WHERE y sus parámetros.
    /// Todas las condiciones se unen con AND. Una columna de una tabla con alias se escribe "a.CODIGO".
    /// <code>new Filtro { ["NUMERO"] = numero, ["EMPRESA"] = empresa }</code>
    /// </summary>
    public sealed class Filtro
    {
        private readonly List<string> _condiciones = new List<string>();

        /// <summary>Valores de los parámetros de <see cref="Condicion"/>.</summary>
        public Dictionary<string, string> Parametros { get; } = new Dictionary<string, string>();

        /// <summary>El WHERE (sin la palabra); null si no hay condiciones.</summary>
        public string Condicion => _condiciones.Count == 0 ? null : string.Join(" AND ", _condiciones.Select(c => "(" + c + ")"));

        /// <summary>Lo mismo que <see cref="Igual"/>, para escribir el filtro como una lista de columna y valor.</summary>
        public string this[string columna]
        {
            set => Igual(columna, value);
        }

        /// <summary>La columna es igual al valor, sin contar los espacios de alrededor de ninguno de los dos (x.Trim() == valor.Trim()).</summary>
        public Filtro Igual(string columna, string valor)
        {
            return Sql(Recortada(columna) + " = " + Parametro(valor.Trim()));
        }

        /// <summary>Como <see cref="Igual"/>, pero distinto.</summary>
        public Filtro Distinto(string columna, string valor)
        {
            return Sql(Recortada(columna) + " <> " + Parametro(valor.Trim()));
        }

        /// <summary>La columna es exactamente el valor: no se quitan los espacios de delante de la columna.</summary>
        public Filtro Exacto(string columna, string valor)
        {
            return Sql(Columna(columna) + " = " + Parametro(valor));
        }

        /// <summary>
        /// La columna, sin los espacios de alrededor, es uno de los valores. Con una lista vacía no hay filas. Los valores
        /// van escritos en la consulta, como hace EF, porque SQL Server no admite más de 2.100 parámetros.
        /// </summary>
        public Filtro En(string columna, IEnumerable<string> valores)
        {
            var textos = valores.Select(v => "'" + v.Trim().Replace("'", "''") + "'").ToList();
            return Sql(textos.Count == 0 ? "0 = 1" : Recortada(columna) + " IN (" + string.Join(", ", textos) + ")");
        }

        /// <summary>La columna contiene el texto, sin distinguir mayúsculas (x.ToLower() LIKE '%texto%').</summary>
        public Filtro Contiene(string columna, string texto)
        {
            return Sql("LOWER(" + Columna(columna) + ") LIKE " + Parametro("%" + texto.ToLower() + "%"));
        }

        /// <summary>La columna de fecha es de ese día, a cualquier hora.</summary>
        public Filtro Dia(string columna, DateTime dia)
        {
            return Sql("CONVERT(date, " + Columna(columna) + ") = " + Parametro(dia.ToString("yyyyMMdd")));
        }

        /// <summary>
        /// Una condición escrita en SQL, para lo que no cubren las demás. Las tablas van entre llaves, como en
        /// <see cref="Consulta.Origen"/>, y cada {0}, {1}... se sustituye por un parámetro con ese valor.
        /// </summary>
        public Filtro Sql(string condicion, params string[] valores)
        {
            for (int i = 0; i < valores.Length; i++)
                condicion = condicion.Replace("{" + i + "}", Parametro(valores[i]));
            _condiciones.Add(condicion);
            return this;
        }

        private string Parametro(string valor)
        {
            string nombre = "@f" + Parametros.Count;
            Parametros[nombre] = valor;
            return nombre;
        }

        private static string Columna(string columna) => "[" + columna.Replace(".", "].[") + "]";

        private static string Recortada(string columna) => "LTRIM(RTRIM(" + Columna(columna) + "))";
    }
}
