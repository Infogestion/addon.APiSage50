using System.Collections.Generic;
using System.Linq;

namespace S50APIService.Sage
{
    /// <summary>
    /// Una lista de filas ya escrita como JSON (un array de modelos), en bloques de bytes. Es lo que devuelven los listados
    /// en vez de una lista de objetos: ver <see cref="LectorSage.LeerJson{T}(string, Consulta)"/>.
    /// </summary>
    public sealed class ListaJson
    {
        public List<byte[]> Bloques { get; }

        public ListaJson(List<byte[]> bloques)
        {
            Bloques = bloques;
        }

        /// <summary>True si no tiene ninguna fila (el JSON es "[]").</summary>
        public bool Vacia => Bloques.Sum(b => b.Length) == 2;
    }
}
