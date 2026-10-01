using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace S50APIService.Sage
{
    /// <summary>
    /// Escribe las filas de un DataTable como array JSON, con el mismo resultado que serializar la lista de modelos
    /// con FormateadorJson.Opciones, y lo entrega en bloques de menos de 85.000 bytes.
    /// </summary>
    internal static class EscritorJson
    {
        private const int TamanoBloque = 64 * 1024;

        public static void Escribir(DataTable datos, CampoJson[] campos, BloquesJson destino)
        {
            var opciones = new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, SkipValidation = true };
            var nombres = campos.Select(c => JsonEncodedText.Encode(c.Nombre, opciones.Encoder)).ToArray();

            using (var memoria = new MemoryStream(TamanoBloque * 2))
            using (var json = new Utf8JsonWriter(memoria, opciones))
            {
                json.WriteStartArray();
                foreach (DataRow fila in datos.Rows)
                {
                    json.WriteStartObject();
                    for (int i = 0; i < campos.Length; i++)
                        EscribirCampo(json, nombres[i], fila.IsNull(i) ? null : fila[i], campos[i]);
                    json.WriteEndObject();

                    if (json.BytesPending >= TamanoBloque)
                        Entregar(json, memoria, destino);
                }
                json.WriteEndArray();
                Entregar(json, memoria, destino);
            }
        }

        private static void Entregar(Utf8JsonWriter json, MemoryStream memoria, BloquesJson destino)
        {
            json.Flush();
            destino.Anadir(memoria.ToArray());
            memoria.SetLength(0);
        }

        /// <summary>El valor de SQL con el tipo de la propiedad del modelo (p. ej. un smallint como int); null solo si la propiedad lo admite.</summary>
        private static void EscribirCampo(Utf8JsonWriter json, JsonEncodedText nombre, object valor, CampoJson campo)
        {
            if (valor == null && campo.AdmiteNull)
            {
                json.WriteNull(nombre);
                return;
            }

            switch (campo.Tipo)
            {
                case TypeCode.String:
                    json.WriteString(nombre, valor as string ?? Convert.ToString(valor));
                    break;
                case TypeCode.Boolean:
                    json.WriteBoolean(nombre, valor != null && Convert.ToBoolean(valor));
                    break;
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                    json.WriteNumber(nombre, valor == null ? 0 : Convert.ToInt32(valor));
                    break;
                case TypeCode.Int64:
                    json.WriteNumber(nombre, valor == null ? 0L : Convert.ToInt64(valor));
                    break;
                case TypeCode.Decimal:
                    json.WriteNumber(nombre, valor == null ? 0m : Convert.ToDecimal(valor));
                    break;
                case TypeCode.Double:
                    json.WriteNumber(nombre, valor == null ? 0d : Convert.ToDouble(valor));
                    break;
                case TypeCode.DateTime:
                    json.WriteString(nombre, valor == null ? default : Convert.ToDateTime(valor));
                    break;
                default:
                    throw new NotSupportedException($"El campo {campo.Nombre} es de un tipo ({campo.Tipo}) que el escritor de JSON no admite.");
            }
        }
    }
}
