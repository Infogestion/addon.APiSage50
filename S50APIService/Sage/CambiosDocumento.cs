using System;
using System.Collections.Generic;

namespace S50APIService.Sage
{
    /// <summary>
    /// Lo que hay que cambiar en un documento de un addon de Sage que tiene clase de negocio: propiedades de la cabecera
    /// y, si hace falta, de una de sus líneas. Lo guarda <see cref="EscritorSage.GuardarDocumento"/>.
    /// </summary>
    [Serializable]
    public sealed class CambiosDocumento
    {
        /// <summary>Librería del addon, sin la extensión: "sage.addons.GestionMerc".</summary>
        public string Libreria { get; set; }

        /// <summary>Clase de negocio del documento, con su espacio de nombres.</summary>
        public string Clase { get; set; }

        public string Ejercicio { get; set; }
        public string Empresa { get; set; }
        public string Numero { get; set; }

        /// <summary>Propiedades de la cabecera que cambian, con su valor nuevo (p. ej. "_Estadoo" → "Cerrado").</summary>
        public Dictionary<string, string> Cabecera { get; set; } = new Dictionary<string, string>();

        /// <summary>La línea que cambia; null si solo cambia la cabecera.</summary>
        public int? Linea { get; set; }

        /// <summary>Propiedades de esa línea que cambian, con su valor nuevo (p. ej. "_Traspasado" → 5).</summary>
        public Dictionary<string, decimal> DeLinea { get; set; } = new Dictionary<string, decimal>();
    }
}
