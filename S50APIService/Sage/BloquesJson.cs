using System;
using System.Collections.Generic;

namespace S50APIService.Sage
{
    /// <summary>Una columna de la tabla y cómo se escribe en el JSON: nombre del campo, tipo de la propiedad del modelo y si admite null.</summary>
    [Serializable]
    public sealed class CampoJson
    {
        public string Columna { get; set; }
        public string Nombre { get; set; }
        public TypeCode Tipo { get; set; }
        public bool AdmiteNull { get; set; }
    }

    /// <summary>Recibe, fuera del AppDomain de Sage, los bloques de JSON que escribe <see cref="TrabajadorSage.EscribirConsultaJson"/>.</summary>
    public sealed class BloquesJson : MarshalByRefObject
    {
        private List<byte[]> _bloques = new List<byte[]>();

        public void Anadir(byte[] bloque) => _bloques.Add(bloque);

        /// <summary>Entrega los bloques y deja de guardarlos.</summary>
        public List<byte[]> Tomar()
        {
            var bloques = _bloques;
            _bloques = new List<byte[]>();
            return bloques;
        }
    }
}
