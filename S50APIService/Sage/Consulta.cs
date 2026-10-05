using System;
using System.Collections.Generic;

namespace S50APIService.Sage
{
    /// <summary>
    /// Una lectura de Sage con uniones entre tablas y paginación (los join y el Skip/Take de EF en interface.s50c).
    /// </summary>
    [Serializable]
    public sealed class Consulta
    {
        /// <summary>
        /// El FROM, con cada tabla entre llaves para que Sage la traduzca a la de la base de datos:
        /// "{articulo} AS [a] INNER JOIN {barras} AS [b] ON [a].[CODIGO] = [b].[ARTICULO]".
        /// </summary>
        public string Origen { get; set; }

        /// <summary>Alias de la tabla de la que salen las columnas del modelo; null si <see cref="Origen"/> es una sola tabla sin alias.</summary>
        public string Alias { get; set; }

        /// <summary>
        /// Columnas del modelo que no salen de la tabla sino de una expresión SQL: nombre de la columna → expresión
        /// (p. ej. "STOCKBYALMACEN" → "CASE WHEN ... END"). Null si no hay ninguna.
        /// </summary>
        public Dictionary<string, string> Expresiones { get; set; }

        /// <summary>El WHERE (sin la palabra), con parámetros @nombre y las tablas entre llaves; null para todas las filas.</summary>
        public string Condicion { get; set; }

        /// <summary>Valores de los parámetros de <see cref="Condicion"/>; se envían a SQL Server como varchar.</summary>
        public Dictionary<string, string> Parametros { get; set; }

        /// <summary>Pone <see cref="Condicion"/> y <see cref="Parametros"/> con los de un <see cref="Sage.Filtro"/>.</summary>
        public Filtro Filtro
        {
            set
            {
                Condicion = value?.Condicion;
                Parametros = value?.Parametros;
            }
        }

        /// <summary>El Skip(pagesize * (page - 1)).Take(pagesize) de interface.s50c, con su mismo desbordamiento de int.</summary>
        public Consulta Pagina(int page, int pagesize)
        {
            Saltar = unchecked(pagesize * (page - 1));
            Tomar = pagesize;
            return this;
        }

        /// <summary>Filas que se saltan (Skip de EF); null si no se pagina. Va siempre con <see cref="Tomar"/>.</summary>
        public int? Saltar { get; set; }

        /// <summary>Filas que se devuelven (Take de EF).</summary>
        public int? Tomar { get; set; }
    }
}
